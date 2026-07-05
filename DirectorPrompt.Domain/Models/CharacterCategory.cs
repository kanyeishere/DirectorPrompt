namespace DirectorPrompt.Domain.Models;

public record CharacterCategory
{
    public long ID { get; init; }

    public long ProjectID { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }

    public long[] ParentCategoryIDs { get; init; } = [];
}
