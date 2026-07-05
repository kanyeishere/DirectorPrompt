using DirectorPrompt.Domain.Enums;

namespace DirectorPrompt.Domain.Configurations;

public record AuditConfig
{
    public AuditMode Mode { get; init; } = AuditMode.Blocking;

    public int MaxRetries { get; init; } = 2;

    public List<AuditDimension> Dimensions { get; init; } = [];
}
