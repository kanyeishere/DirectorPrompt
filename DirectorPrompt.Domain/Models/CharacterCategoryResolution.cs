namespace DirectorPrompt.Domain.Models;

public record CharacterCategoryResolution
{
    public long CharacterID { get; init; }

    public long[] CategoryIDs { get; init; } = [];

    public long[] AttributeIDs { get; init; } = [];
}
