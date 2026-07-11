namespace DirectorPrompt.Domain.Configurations;

public sealed record EnumAttributeConfig
{
    public List<string> Options { get; init; } = [];

    public string? Trigger { get; init; }

    public List<EnumTransitionConfig> Transitions { get; init; } = [];
}
