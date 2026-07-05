namespace DirectorPrompt.Domain.Configurations;

public record OrchestratorConfig
{
    public List<AgentDefinition> Agents { get; init; } = [];

    public AuditConfig AuditConfig { get; init; } = new();

    public MemoryConfig MemoryConfig { get; init; } = new();

    public KnowledgeRetrievalConfig KnowledgeConfig { get; init; } = new();

    /// <summary>
    ///     快照间隔, 每隔多少轮自动快照, 0 表示按场景边界
    /// </summary>
    public int SnapshotInterval { get; init; }
}
