namespace DirectorPrompt.Domain.Models;

public record Project
{
    public long ID { get; init; }

    public string Name { get; init; } = string.Empty;

    public string WorldOverview { get; init; } = string.Empty;

    public string NarrativeStyle { get; init; } = string.Empty;

    public string[] PermanentConstraints { get; init; } = [];

    public DateTime CreatedAt { get; init; }

    public DateTime UpdatedAt { get; init; }
}
