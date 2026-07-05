using DirectorPrompt.Domain.Enums;

namespace DirectorPrompt.Domain.Models;

public record Violation
{
    public string Type { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public AuditSeverity Severity { get; init; }

    public string? Suggestion { get; init; }
}
