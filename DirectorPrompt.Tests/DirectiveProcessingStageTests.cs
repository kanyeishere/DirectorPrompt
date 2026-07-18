using DirectorPrompt.Agents;
using DirectorPrompt.Agents.Config;
using DirectorPrompt.Agents.Pipeline;
using DirectorPrompt.Agents.Tools;
using DirectorPrompt.Domain.Configurations;
using DirectorPrompt.Domain.Enums;
using DirectorPrompt.Infrastructure.Repositories;
using Microsoft.Extensions.AI;

namespace DirectorPrompt.Tests;

public sealed class DirectiveProcessingStageTests
{
    [Fact]
    public async Task FirstSceneChangeIsProcessedOnlyOnce()
    {
        await using var context            = await DatabaseTestContext.CreateAsync();
        var             sceneRepository    = new SceneRepository(context.Scheduler);
        var             sceneTools         = new SceneTools(sceneRepository, new TimelineCalculator());
        var             chatClient         = new SceneCreatingChatClient();
        var             orchestratorConfig = new OrchestratorConfig();
        var             provider           = new ProviderConfig { ID = "provider" };
        var             model              = new ModelConfig { ID    = "model", ProviderID = provider.ID, ModelName = "test" };

        orchestratorConfig.Providers.Add(provider);
        orchestratorConfig.Models.Add(model);
        orchestratorConfig.AgentTasks.Add
        (
            new AgentTaskConfig
            {
                TaskType      = AgentTaskType.Scene,
                ModelConfigID = model.ID
            }
        );

        var stage = new DirectiveProcessingStage
        (
            new TestChatClientFactory(chatClient),
            new AgentConfigResolver(orchestratorConfig),
            new SceneToolResolver(sceneTools),
            sceneRepository,
            new DirectiveRepository(context.Scheduler),
            new TimelineCalculator(),
            orchestratorConfig
        );

        await stage.ExecuteAsync
        (
            new DirectiveBatch
            (
                1,
                [new DirectiveItem(DirectiveType.SceneChange, "首次", 1)]
            ),
            1,
            1,
            null,
            new ResolvedEmbeddingConfig(),
            TestContext.Current.CancellationToken
        );
        var scenes = await sceneRepository.GetBySessionAsync(1, TestContext.Current.CancellationToken);

        Assert.Equal(1, chatClient.RequestCount);
        Assert.Single(scenes);
        Assert.Equal(SceneStatus.Active, scenes[0].Status);
    }

    private sealed class TestChatClientFactory
    (
        IChatClient chatClient
    ) : IChatClientFactory
    {
        public IChatClient Create(ProviderConfig provider, ModelConfig model) => chatClient;

        public void Reset()
        {
        }
    }

    private sealed class SceneToolResolver
    (
        SceneTools sceneTools
    ) : IAgentToolResolver
    {
        public Task<IReadOnlyList<AIFunction>> ResolveAsync
        (
            AgentTaskType        taskType,
            ToolExecutionContext context,
            CancellationToken    cancellationToken = default
        ) => Task.FromResult((IReadOnlyList<AIFunction>)sceneTools.Create(context).ToList());
    }

    private sealed class SceneCreatingChatClient : IChatClient
    {
        public int RequestCount { get; private set; }

        public async Task<ChatResponse> GetResponseAsync
        (
            IEnumerable<ChatMessage> messages,
            ChatOptions?             options           = null,
            CancellationToken        cancellationToken = default
        )
        {
            RequestCount++;

            var createScene = options!.Tools!.OfType<AIFunction>().Single(tool => tool.Name == "create_scene");
            await createScene.InvokeAsync
            (
                new AIFunctionArguments { ["timeLabel"] = "首次" },
                cancellationToken
            );

            return new ChatResponse(new ChatMessage(ChatRole.Assistant, "已创建"));
        }

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync
        (
            IEnumerable<ChatMessage> messages,
            ChatOptions?             options           = null,
            CancellationToken        cancellationToken = default
        ) => throw new NotSupportedException();

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose()
        {
        }
    }
}
