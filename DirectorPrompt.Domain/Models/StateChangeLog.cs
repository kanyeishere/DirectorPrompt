using DirectorPrompt.Domain.Enums;

namespace DirectorPrompt.Domain.Models;

public record StateChangeLog
{
    public long ID { get; init; }

    public long AttributeID { get; init; }

    public long SceneID { get; init; }

    public long? RoundID { get; init; }

    public string OldValue { get; init; } = string.Empty;

    public string NewValue { get; init; } = string.Empty;

    public StateChangeSource Source { get; init; }

    public string Reason { get; init; } = string.Empty;

    public DateTime CreatedAt { get; init; }
}
