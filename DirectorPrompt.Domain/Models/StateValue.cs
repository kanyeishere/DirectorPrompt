namespace DirectorPrompt.Domain.Models;

public record StateValue
{
    public long AttributeID { get; init; }

    public string Value { get; init; } = string.Empty;

    public DateTime UpdatedAt { get; init; }
}
