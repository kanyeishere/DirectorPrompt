using DirectorPrompt.Domain.Enums;

namespace DirectorPrompt.Domain.Models;

public record Effect
{
    public EffectType Type { get; init; }

    public string Target { get; init; } = string.Empty;
}
