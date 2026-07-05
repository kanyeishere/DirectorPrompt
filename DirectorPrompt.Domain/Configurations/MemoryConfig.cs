namespace DirectorPrompt.Domain.Configurations;

public record MemoryConfig
{
    public int RecallTopK { get; init; } = 10;

    public int TokenBudget { get; init; } = 1500;

    public float MinRelevance { get; init; }

    public float TimeDecayLambda { get; init; }
}
