using DirectorPrompt.Domain.Enums;

namespace DirectorPrompt.Domain.Configurations;

public record AgentDefinition
{
    public string Name { get; init; } = string.Empty;

    public AgentRole Role { get; init; }

    public ModelConfig ModelConfig { get; init; } = new();

    public string SystemPrompt { get; init; } = string.Empty;

    public float Temperature { get; init; }

    public string[] Tools { get; init; } = [];

    public bool Enabled { get; init; } = true;

    /// <summary>
    ///     最大重试次数, 仅 Audit Agent 使用
    /// </summary>
    public int? MaxRetries { get; init; }
}
