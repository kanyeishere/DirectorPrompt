using DirectorPrompt.Domain.Enums;

namespace DirectorPrompt.Domain.Models;

public record CompositeItem
{
    public long ID { get; init; }

    public long AttributeID { get; init; }

    public string Description { get; init; } = string.Empty;

    public float Current { get; init; }

    public float Target { get; init; }

    public CompositeItemStatus Status { get; init; }
}
