using System.Text;
using DirectorPrompt.Agents.Prompts;
using DirectorPrompt.Agents.Tools;
using DirectorPrompt.Domain.Configurations;
using DirectorPrompt.Domain.Enums;
using DirectorPrompt.Domain.Models;
using DirectorPrompt.Domain.Repositories;
using Microsoft.Extensions.AI;

namespace DirectorPrompt.Agents.Pipeline;

public sealed class RetrievalStage
{
    private readonly IChatClientFactory   chatClientFactory;
    private readonly ISceneRepository     sceneRepository;
    private readonly IStateRepository     stateRepository;
    private readonly ICharacterRepository characterRepository;
    private readonly IDirectiveRepository directiveRepository;
    private readonly KnowledgeTools       knowledgeTools;
    private readonly MemoryTools          memoryTools;
    private readonly OrchestratorConfig   orchestratorConfig;

    public RetrievalStage
    (
        IChatClientFactory   chatClientFactory,
        ISceneRepository     sceneRepository,
        IStateRepository     stateRepository,
        ICharacterRepository characterRepository,
        IDirectiveRepository directiveRepository,
        KnowledgeTools       knowledgeTools,
        MemoryTools          memoryTools,
        OrchestratorConfig   orchestratorConfig
    )
    {
        this.chatClientFactory   = chatClientFactory;
        this.sceneRepository     = sceneRepository;
        this.stateRepository     = stateRepository;
        this.characterRepository = characterRepository;
        this.directiveRepository = directiveRepository;
        this.knowledgeTools      = knowledgeTools;
        this.memoryTools         = memoryTools;
        this.orchestratorConfig  = orchestratorConfig;
    }

    public async Task ExecuteAsync(PipelineContext context, CancellationToken cancellationToken = default)
    {
        var toolContext   = context.ToolContext;
        var knowledgeTask = RetrieveKnowledgeAsync(context, cancellationToken);
        var memoryTask    = RetrieveMemoryAsync(context, cancellationToken);
        var injectionTask = BuildSystemInjectionAsync(toolContext, cancellationToken);

        await Task.WhenAll(knowledgeTask, memoryTask, injectionTask);

        context.KnowledgeContext = await knowledgeTask;
        context.MemoryContext    = await memoryTask;
        context.SystemInjection  = await injectionTask;
    }

    private async Task<string> RetrieveKnowledgeAsync(PipelineContext context, CancellationToken cancellationToken)
    {
        var knowledgeAgent = orchestratorConfig.Agents.FirstOrDefault(a => a.Role == AgentRole.Knowledge);

        if (knowledgeAgent is null || !knowledgeAgent.Enabled)
            return string.Empty;

        var client        = chatClientFactory.Create(knowledgeAgent.ModelConfig);
        var tools         = knowledgeTools.Create(context.ToolContext);
        var directorInput = BuildDirectorInput(context.DirectiveBatch);

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, KnowledgeAgentPrompt.System),
            new(ChatRole.User, directorInput)
        };

        var options = new ChatOptions
        {
            Temperature = knowledgeAgent.Temperature,
            ModelId     = knowledgeAgent.ModelConfig.ModelName,
            Tools       = [.. tools]
        };

        var response = await client.GetResponseAsync(messages, options, cancellationToken);

        return response.Messages.LastOrDefault()?.Text ?? string.Empty;
    }

    private async Task<string> RetrieveMemoryAsync(PipelineContext context, CancellationToken cancellationToken)
    {
        var memoryAgent = orchestratorConfig.Agents.FirstOrDefault(a => a.Role == AgentRole.Memory);

        if (memoryAgent is null || !memoryAgent.Enabled)
            return string.Empty;

        var client        = chatClientFactory.Create(memoryAgent.ModelConfig);
        var tools         = memoryTools.Create(context.ToolContext);
        var directorInput = BuildDirectorInput(context.DirectiveBatch);

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, MemorySubAgentPrompt.Recall),
            new(ChatRole.User, directorInput)
        };

        var options = new ChatOptions
        {
            Temperature = memoryAgent.Temperature,
            ModelId     = memoryAgent.ModelConfig.ModelName,
            Tools       = [.. tools]
        };

        var response = await client.GetResponseAsync(messages, options, cancellationToken);

        return response.Messages.LastOrDefault()?.Text ?? string.Empty;
    }

    private async Task<string> BuildSystemInjectionAsync(ToolExecutionContext context, CancellationToken cancellationToken)
    {
        var sb = new StringBuilder();

        var sceneTask = context.SceneID is not null ?
                            sceneRepository.GetByIDAsync(context.SceneID.Value, cancellationToken) :
                            Task.FromResult<Scene?>(null);

        var stateTask      = stateRepository.GetAttributesAsync(context.ProjectID, StateScope.Global, cancellationToken);
        var flagsTask      = stateRepository.GetFlagsAsync(context.ProjectID, cancellationToken);
        var directivesTask = directiveRepository.GetActiveAsync(context.ProjectID, cancellationToken);

        await Task.WhenAll(sceneTask, stateTask, flagsTask, directivesTask);

        var scene = await sceneTask;

        if (scene is not null)
        {
            sb.AppendLine("## 场景信息");
            sb.AppendLine($"时间标签: {scene.TimeLabel}");
            sb.AppendLine($"状态: {scene.Status}");
            sb.AppendLine();
        }

        var attributes = await stateTask;

        if (attributes.Count > 0)
        {
            sb.AppendLine("## 全局状态");

            foreach (var attr in attributes)
            {
                var value = await stateRepository.GetStateValueAsync(attr.ID, cancellationToken);
                sb.AppendLine($"- {attr.DisplayName}: {value?.Value ?? "未设置"}");
            }

            sb.AppendLine();
        }

        var flags = await flagsTask;

        if (flags.Count > 0)
        {
            sb.AppendLine("## 标记");

            foreach (var flag in flags)
            {
                if (flag.Value)
                    sb.AppendLine($"- {flag.DisplayName}: 已触发");
            }

            sb.AppendLine();
        }

        var directives = await directivesTask;

        if (directives.Count > 0)
        {
            sb.AppendLine("## 生效指令");

            foreach (var directive in directives)
            {
                var ttl = directive.TTL.HasValue ?
                              $" (剩余 {directive.TTL} 轮)" :
                              " (永久)";
                sb.AppendLine($"- [{directive.Type}]{directive.Content}{ttl}");
            }

            sb.AppendLine();
        }

        if (context.SceneID is not null)
        {
            var presence = await characterRepository.GetPresenceAsync(context.SceneID.Value, cancellationToken);

            if (presence.Count > 0)
            {
                sb.AppendLine("## 在场人物");

                foreach (var p in presence)
                {
                    var character = await characterRepository.GetByIDAsync(p.CharacterID, cancellationToken);
                    if (character is not null)
                        sb.AppendLine($"- {character.Name}: {character.Description}");
                }

                sb.AppendLine();
            }
        }

        return sb.ToString();
    }

    private static string BuildDirectorInput(DirectiveBatch batch)
    {
        var sb = new StringBuilder();
        sb.AppendLine("## 导演指令");
        foreach (var item in batch.Directives)
            sb.AppendLine($"{item.Order}. [{item.Type}] {item.Content}");
        return sb.ToString();
    }
}
