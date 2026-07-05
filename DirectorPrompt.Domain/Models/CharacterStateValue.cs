namespace DirectorPrompt.Domain.Models;

public record CharacterStateValue
{
    public long CharacterID { get; init; }

    public long AttributeID { get; init; }

    public string Value { get; init; } = string.Empty;

    public DateTime UpdatedAt { get; init; }
}
