namespace DirectorPrompt.Domain.Models;

public sealed class KnowledgeGroupPatch
{
    public string? Name { get; set; }

    public string? Description { get; set; }

    public bool? Active { get; set; }
}
