using DirectorPrompt.Agents.Prompts;
using DirectorPrompt.Agents.Tools;
using DirectorPrompt.Domain.Configurations;
using DirectorPrompt.Domain.Enums;
using Microsoft.Extensions.AI;

namespace DirectorPrompt.Agents.Pipeline;

public sealed class AuditStage
{
    private readonly IChatClientFactory chatClientFactory;
    private readonly SceneTools         sceneTools;
    private readonly KnowledgeTools     knowledgeTools;
    private readonly StateTools         stateTools;
    private readonly MemoryTools        memoryTools;
    private readonly CharacterTools     characterTools;
    private readonly AuditTools         auditTools;
    private readonly OrchestratorConfig orchestratorConfig;

    public AuditStage
    (
        IChatClientFactory chatClientFactory,
        SceneTools         sceneTools,
        KnowledgeTools     knowledgeTools,
        StateTools         stateTools,
        MemoryTools        memoryTools,
        CharacterTools     characterTools,
        AuditTools         auditTools,
        OrchestratorConfig orchestratorConfig
    )
    {
        this.chatClientFactory  = chatClientFactory;
        this.sceneTools         = sceneTools;
        this.knowledgeTools     = knowledgeTools;
        this.stateTools         = stateTools;
        this.memoryTools        = memoryTools;
        this.characterTools     = characterTools;
        this.auditTools         = auditTools;
        this.orchestratorConfig = orchestratorConfig;
    }

    public async Task ExecuteAsync(PipelineContext context, CancellationToken cancellationToken = default)
    {
        var auditConfig = orchestratorConfig.AuditConfig;

        if (auditConfig.Mode == AuditMode.Disabled)
        {
            context.AuditPassed = true;
            return;
        }

        var auditAgent = orchestratorConfig.Agents.FirstOrDefault(a => a.Role == AgentRole.Audit);

        if (auditAgent is null || !auditAgent.Enabled)
        {
            context.AuditPassed = true;
            return;
        }

        var dimensions = auditConfig.Dimensions.Count > 0 ?
                             auditConfig.Dimensions.ToList() :
                             Enum.GetValues<AuditDimension>().ToList();

        var dimensionTasks = dimensions
                             .Select(dim => AuditDimensionAsync(context, auditAgent, dim, cancellationToken))
                             .ToList();

        await Task.WhenAll(dimensionTasks);

        var allViolations = auditTools.Violations
                                      .Where(v => v.Severity != AuditSeverity.General)
                                      .ToList();

        context.Violations.Clear();
        context.Violations.AddRange(allViolations);
        context.AuditPassed = allViolations.Count == 0;
    }

    private async Task AuditDimensionAsync
    (
        PipelineContext   context,
        AgentDefinition   auditAgent,
        AuditDimension    dimension,
        CancellationToken cancellationToken
    )
    {
        auditTools.Reset();

        var (prompt, tools) = GetDimensionConfig(dimension, context.ToolContext);
        var client = chatClientFactory.Create(auditAgent.ModelConfig);

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, prompt),
            new(ChatRole.User, context.NarrativeOutput ?? string.Empty)
        };

        var options = new ChatOptions
        {
            Temperature = auditAgent.Temperature,
            ModelId     = auditAgent.ModelConfig.ModelName,
            Tools       = [.. tools]
        };

        await client.GetResponseAsync(messages, options, cancellationToken);
    }

    private (string prompt, IList<AIFunction> tools) GetDimensionConfig
    (
        AuditDimension       dimension,
        ToolExecutionContext context
    ) =>
        dimension switch
        {
            AuditDimension.Setting => (
                                          AuditAgentPrompt.Setting,
                                          knowledgeTools.Create(context)
                                      ),
            AuditDimension.State => (
                                        AuditAgentPrompt.State,
                                        [.. stateTools.Create(context), .. characterTools.Create(context)]
                                    ),
            AuditDimension.Character => (
                                            AuditAgentPrompt.Character,
                                            characterTools.Create(context)
                                        ),
            AuditDimension.Time => (
                                       AuditAgentPrompt.Time,
                                       sceneTools.Create(context)
                                   ),
            AuditDimension.Memory => (
                                         AuditAgentPrompt.Memory,
                                         memoryTools.Create(context)
                                     ),
            _ => throw new ArgumentOutOfRangeException(nameof(dimension))
        };
}
