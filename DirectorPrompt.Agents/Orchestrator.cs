using System.Text.Json;
using DirectorPrompt.Agents.Pipeline;
using DirectorPrompt.Agents.Prompts;
using DirectorPrompt.Agents.Tools;
using DirectorPrompt.Domain.Configurations;
using DirectorPrompt.Domain.Enums;
using DirectorPrompt.Domain.Models;
using DirectorPrompt.Domain.Repositories;
using DirectorPrompt.Domain.Services;
using Microsoft.Extensions.AI;

namespace DirectorPrompt.Agents;

public sealed class Orchestrator
{
    private readonly IChatClientFactory       chatClientFactory;
    private readonly ISceneRepository         sceneRepository;
    private readonly IStateRepository         stateRepository;
    private readonly ICharacterRepository     characterRepository;
    private readonly IDirectiveRepository     directiveRepository;
    private readonly IEventRepository         eventRepository;
    private readonly IStateSnapshotRepository snapshotRepository;
    private readonly IProjectRepository       projectRepository;
    private readonly IKnowledgeRepository     knowledgeRepository;
    private readonly IMemoryRepository        memoryRepository;
    private readonly IEmbeddingService        embeddingService;
    private readonly ITimelineCalculator      timelineCalculator;
    private readonly OrchestratorConfig       config;

    private readonly SceneTools     sceneTools;
    private readonly KnowledgeTools knowledgeTools;
    private readonly StateTools     stateTools;
    private readonly MemoryTools    memoryTools;
    private readonly CharacterTools characterTools;
    private readonly AuditTools     auditTools;

    private readonly RetrievalStage      retrievalStage;
    private readonly GenerationStage     generationStage;
    private readonly AuditStage          auditStage;
    private readonly PostProcessingStage postProcessingStage;

    public Orchestrator
    (
        IChatClientFactory       chatClientFactory,
        ISceneRepository         sceneRepository,
        IStateRepository         stateRepository,
        ICharacterRepository     characterRepository,
        IDirectiveRepository     directiveRepository,
        IEventRepository         eventRepository,
        IStateSnapshotRepository snapshotRepository,
        IProjectRepository       projectRepository,
        IKnowledgeRepository     knowledgeRepository,
        IMemoryRepository        memoryRepository,
        IEmbeddingService        embeddingService,
        ITimelineCalculator      timelineCalculator,
        OrchestratorConfig       config
    )
    {
        this.chatClientFactory   = chatClientFactory;
        this.sceneRepository     = sceneRepository;
        this.stateRepository     = stateRepository;
        this.characterRepository = characterRepository;
        this.directiveRepository = directiveRepository;
        this.eventRepository     = eventRepository;
        this.snapshotRepository  = snapshotRepository;
        this.projectRepository   = projectRepository;
        this.knowledgeRepository = knowledgeRepository;
        this.memoryRepository    = memoryRepository;
        this.embeddingService    = embeddingService;
        this.timelineCalculator  = timelineCalculator;
        this.config              = config;

        sceneTools     = new SceneTools(sceneRepository, timelineCalculator);
        knowledgeTools = new KnowledgeTools(knowledgeRepository, embeddingService);
        stateTools     = new StateTools(stateRepository);
        memoryTools    = new MemoryTools(memoryRepository, embeddingService);
        characterTools = new CharacterTools(characterRepository, stateRepository);
        auditTools     = new AuditTools();

        retrievalStage = new RetrievalStage
        (
            chatClientFactory,
            sceneRepository,
            stateRepository,
            characterRepository,
            directiveRepository,
            knowledgeTools,
            memoryTools,
            config
        );

        generationStage = new GenerationStage
        (
            chatClientFactory,
            knowledgeTools,
            config
        );

        auditStage = new AuditStage
        (
            chatClientFactory,
            sceneTools,
            knowledgeTools,
            stateTools,
            memoryTools,
            characterTools,
            auditTools,
            config
        );

        postProcessingStage = new PostProcessingStage
        (
            chatClientFactory,
            memoryTools,
            stateTools,
            characterTools,
            config
        );
    }

    public async Task<NarrationResult> ProcessBatchAsync
    (
        DirectiveBatch    batch,
        CancellationToken cancellationToken = default
    )
    {
        var project = await projectRepository.GetByIDAsync(batch.ProjectID, cancellationToken);

        if (project is null)
            throw new ArgumentException($"项目 {batch.ProjectID} 不存在");

        var roundID          = await eventRepository.GetLatestRoundIDAsync(batch.ProjectID, cancellationToken) + 1;
        var activeScene      = await sceneRepository.GetActiveSceneAsync(batch.ProjectID, cancellationToken);
        var timelinePosition = activeScene?.TimelinePosition ?? 0;

        await ProcessDirectivesAsync(batch, roundID, activeScene, cancellationToken);

        var context = new PipelineContext
        {
            DirectiveBatch          = batch,
            RoundID                 = roundID,
            CurrentSceneID          = activeScene?.ID,
            CurrentTimelinePosition = timelinePosition
        };

        await retrievalStage.ExecuteAsync(context, cancellationToken);

        await generationStage.ExecuteAsync(context, cancellationToken);

        await RecordEventAsync
        (
            batch.ProjectID,
            roundID,
            EventType.DirectorInput,
            JsonSerializer.Serialize
            (
                batch.Directives.Select
                (d => new
                    {
                        type    = d.Type.ToString(),
                        content = d.Content,
                        order   = d.Order
                    }
                )
            ),
            cancellationToken
        );

        await RecordEventAsync
        (
            batch.ProjectID,
            roundID,
            EventType.NarrativeOutput,
            context.NarrativeOutput ?? string.Empty,
            cancellationToken
        );

        await RunAuditLoopAsync(context, cancellationToken);

        if (context.AuditPassed)
            await postProcessingStage.ExecuteAsync(context, cancellationToken);

        await directiveRepository.DecrementTTLAsync(batch.ProjectID, cancellationToken);

        return new NarrationResult
        (
            context.NarrativeOutput ?? string.Empty,
            roundID,
            context.Violations,
            context.AuditPassed
        );
    }

    public async Task DeleteRoundAsync(long projectID, long roundID, CancellationToken cancellationToken = default) =>
        await eventRepository.RemoveByRoundAsync(roundID, cancellationToken);

    public async Task<NarrationResult> RewriteAsync
    (
        DirectiveBatch    batch,
        CancellationToken cancellationToken = default
    )
    {
        var latestRound = await eventRepository.GetLatestRoundIDAsync(batch.ProjectID, cancellationToken);

        if (latestRound > 0)
            await eventRepository.RemoveByRoundAsync(latestRound, cancellationToken);

        return await ProcessBatchAsync(batch, cancellationToken);
    }

    public async Task<NarrationResult> CorrectAsync
    (
        DirectiveBatch    originalBatch,
        string            correctionGuidance,
        CancellationToken cancellationToken = default
    )
    {
        var latestRound       = await eventRepository.GetLatestRoundIDAsync(originalBatch.ProjectID, cancellationToken);
        var events            = await eventRepository.GetByRoundAsync(latestRound, cancellationToken);
        var narrativeEvent    = events.FirstOrDefault(e => e.Type == EventType.NarrativeOutput);
        var originalNarrative = narrativeEvent?.Data ?? string.Empty;

        var correctedDirectives = originalBatch.Directives.ToList();
        correctedDirectives.Add(new DirectiveItem(DirectiveType.Plot, correctionGuidance, correctedDirectives.Count + 1));

        var correctedBatch = new DirectiveBatch(originalBatch.ProjectID, correctedDirectives);

        await eventRepository.RemoveByRoundAsync(latestRound, cancellationToken);

        return await ProcessBatchAsync(correctedBatch, cancellationToken);
    }

    private async Task ProcessDirectivesAsync
    (
        DirectiveBatch    batch,
        long              roundID,
        Scene?            activeScene,
        CancellationToken cancellationToken
    )
    {
        foreach (var directive in batch.Directives)
        {
            if (directive.Type is DirectiveType.Tone or DirectiveType.TemporaryConstraint)
            {
                var ttl = directive.Type == DirectiveType.Tone ?
                              5 :
                              (int?)null;

                await directiveRepository.AddAsync
                (
                    new ActiveDirective
                    {
                        ProjectID = batch.ProjectID,
                        Type      = directive.Type,
                        Content   = directive.Content,
                        TTL       = ttl,
                        CreatedAt = DateTime.UtcNow
                    },
                    cancellationToken
                );
            }

            if (directive.Type == DirectiveType.SceneChange)
                await CreateSceneViaAgentAsync(batch.ProjectID, directive.Content, activeScene, cancellationToken);
        }
    }

    private async Task CreateSceneViaAgentAsync
    (
        long              projectID,
        string            description,
        Scene?            currentScene,
        CancellationToken cancellationToken
    )
    {
        var sceneAgent = config.Agents.FirstOrDefault(a => a.Role == AgentRole.Scene);

        if (sceneAgent is null || !sceneAgent.Enabled)
            return;

        var toolContext = new ToolExecutionContext
        (
            projectID,
            currentScene?.ID,
            currentScene?.TimelinePosition ?? 0,
            0
        );

        var client = chatClientFactory.Create(sceneAgent.ModelConfig);
        var tools  = sceneTools.Create(toolContext);

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, SceneAgentPrompt.System),
            new(ChatRole.User, description)
        };

        var options = new ChatOptions
        {
            Temperature = sceneAgent.Temperature,
            ModelId     = sceneAgent.ModelConfig.ModelName,
            Tools       = [.. tools]
        };

        await client.GetResponseAsync(messages, options, cancellationToken);
    }

    private async Task RunAuditLoopAsync(PipelineContext context, CancellationToken cancellationToken)
    {
        var maxRetries = config.AuditConfig.MaxRetries;

        for (var attempt = 0; attempt <= maxRetries; attempt++)
        {
            await auditStage.ExecuteAsync(context, cancellationToken);
            context.AuditRetryCount = attempt;

            if (context.AuditPassed)
                return;

            if (attempt < maxRetries && config.AuditConfig.Mode == AuditMode.Blocking)
                await generationStage.RetryWithFeedbackAsync(context, context.Violations, cancellationToken);
            else
            {
                context.AuditPassed = true;
                return;
            }
        }
    }

    private async Task RecordEventAsync
    (
        long              projectID,
        long              roundID,
        EventType         type,
        string            data,
        CancellationToken cancellationToken
    )
    {
        var eventItem = new PlaythroughEvent
        {
            ProjectID = projectID,
            RoundID   = roundID,
            Type      = type,
            Data      = data,
            CreatedAt = DateTime.UtcNow
        };

        await eventRepository.AppendAsync(eventItem, cancellationToken);
    }
}
