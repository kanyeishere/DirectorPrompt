using DirectorPrompt.Domain.Models;

namespace DirectorPrompt.Agents;

public record NarrationResult
(
    string                   Narrative,
    long                     RoundID,
    IReadOnlyList<Violation> Violations,
    bool                     AuditPassed
);
