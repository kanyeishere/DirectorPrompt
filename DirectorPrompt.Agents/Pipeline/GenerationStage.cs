using System.Text;
using DirectorPrompt.Agents.Prompts;
using DirectorPrompt.Agents.Tools;
using DirectorPrompt.Domain.Configurations;
using DirectorPrompt.Domain.Enums;
using DirectorPrompt.Domain.Models;
using Microsoft.Extensions.AI;

namespace DirectorPrompt.Agents.Pipeline;

public sealed class GenerationStage
{
    private readonly IChatClientFactory chatClientFactory;
    private readonly KnowledgeTools     knowledgeTools;
    private readonly OrchestratorConfig orchestratorConfig;

    public GenerationStage
    (
        IChatClientFactory chatClientFactory,
        KnowledgeTools     knowledgeTools,
        OrchestratorConfig orchestratorConfig
    )
    {
        this.chatClientFactory  = chatClientFactory;
        this.knowledgeTools     = knowledgeTools;
        this.orchestratorConfig = orchestratorConfig;
    }

    public async Task ExecuteAsync(PipelineContext context, CancellationToken cancellationToken = default)
    {
        var narratorAgent = orchestratorConfig.Agents.FirstOrDefault(a => a.Role == AgentRole.Narrator);

        if (narratorAgent is null)
            throw new InvalidOperationException("未配置 Narrator Agent");

        var client      = chatClientFactory.Create(narratorAgent.ModelConfig);
        var tools       = knowledgeTools.Create(context.ToolContext);
        var userMessage = BuildNarratorInput(context);

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, NarratorPrompt.System),
            new(ChatRole.User, userMessage)
        };

        var options = new ChatOptions
        {
            Temperature = narratorAgent.Temperature,
            ModelId     = narratorAgent.ModelConfig.ModelName,
            Tools       = [.. tools]
        };

        var response = await client.GetResponseAsync(messages, options, cancellationToken);

        context.NarrativeOutput = response.Messages.LastOrDefault()?.Text ?? string.Empty;
    }

    public async Task RetryWithFeedbackAsync
    (
        PipelineContext          context,
        IReadOnlyList<Violation> violations,
        CancellationToken        cancellationToken = default
    )
    {
        var narratorAgent = orchestratorConfig.Agents.FirstOrDefault(a => a.Role == AgentRole.Narrator);

        if (narratorAgent is null)
            throw new InvalidOperationException("未配置 Narrator Agent");

        var client = chatClientFactory.Create(narratorAgent.ModelConfig);
        var tools  = knowledgeTools.Create(context.ToolContext);

        var sb = new StringBuilder();
        sb.AppendLine("## 上一次输出存在以下问题, 请修正后重新生成:");
        sb.AppendLine();

        foreach (var violation in violations)
        {
            sb.AppendLine($"- [{violation.Severity}] {violation.Description}");

            if (!string.IsNullOrWhiteSpace(violation.Suggestion))
                sb.AppendLine($"  建议: {violation.Suggestion}");
        }

        sb.AppendLine();
        sb.AppendLine("## 原始输出:");
        sb.AppendLine(context.NarrativeOutput);

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, NarratorPrompt.System),
            new(ChatRole.User, BuildNarratorInput(context)),
            new(ChatRole.Assistant, context.NarrativeOutput ?? string.Empty),
            new(ChatRole.User, sb.ToString())
        };

        var options = new ChatOptions
        {
            Temperature = narratorAgent.Temperature,
            ModelId     = narratorAgent.ModelConfig.ModelName,
            Tools       = [.. tools]
        };

        var response = await client.GetResponseAsync(messages, options, cancellationToken);

        context.NarrativeOutput = response.Messages.LastOrDefault()?.Text ?? string.Empty;
    }

    private static string BuildNarratorInput(PipelineContext context)
    {
        var sb = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(context.SystemInjection))
            sb.AppendLine(context.SystemInjection);

        if (!string.IsNullOrWhiteSpace(context.KnowledgeContext))
        {
            sb.AppendLine("## 知识上下文");
            sb.AppendLine(context.KnowledgeContext);
            sb.AppendLine();
        }

        if (!string.IsNullOrWhiteSpace(context.MemoryContext))
        {
            sb.AppendLine("## 记忆上下文");
            sb.AppendLine(context.MemoryContext);
            sb.AppendLine();
        }

        sb.AppendLine("## 导演指令");
        foreach (var item in context.DirectiveBatch.Directives)
            sb.AppendLine($"{item.Order}. [{item.Type}] {item.Content}");

        return sb.ToString();
    }
}
