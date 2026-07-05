namespace DirectorPrompt.Domain.Models;

public record KnowledgeEntityIndex
{
    public long EntryID { get; init; }

    public string EntityName { get; init; } = string.Empty;
}
