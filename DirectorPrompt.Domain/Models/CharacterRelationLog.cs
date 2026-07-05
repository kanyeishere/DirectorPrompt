using DirectorPrompt.Domain.Enums;

namespace DirectorPrompt.Domain.Models;

public record CharacterRelationLog
{
    public long ID { get; init; }

    public long RelationID { get; init; }

    public string? OldType { get; init; }

    public string NewType { get; init; } = string.Empty;

    public string? OldDescription { get; init; }

    public string? NewDescription { get; init; }

    public RelationChangeSource Source { get; init; }

    public string Reason { get; init; } = string.Empty;

    public long SceneID { get; init; }

    public DateTime CreatedAt { get; init; }
}
