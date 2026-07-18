namespace DirectorPrompt.Domain.Models;

public sealed class CharacterCategoryPatch
{
    public string? Name { get; set; }

    public string? Description { get; set; }

    public long[]? ParentCategoryIDs { get; set; }
}
