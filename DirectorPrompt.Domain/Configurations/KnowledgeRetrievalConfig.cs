namespace DirectorPrompt.Domain.Configurations;

public record KnowledgeRetrievalConfig
{
    public int SemanticTopK { get; init; } = 8;

    public int TokenBudget { get; init; } = 2000;

    public float MinRelevance { get; init; }
}
