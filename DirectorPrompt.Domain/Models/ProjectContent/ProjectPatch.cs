namespace DirectorPrompt.Domain.Models;

public sealed class ProjectPatch
{
    public string? Name { get; set; }

    public string? Description { get; set; }

    public string? OpeningMessage { get; set; }
}
