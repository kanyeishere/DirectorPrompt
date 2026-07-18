namespace DirectorPrompt.Domain.Models;

public sealed class KnowledgeEntryPatch
{
    public string? Remarks { get; set; }

    public string? Content { get; set; }

    public string[]? Keywords { get; set; }

    public long? GroupID { get; set; }

    public bool? MoveToUngrouped { get; set; }

    public bool? Active { get; set; }
}
