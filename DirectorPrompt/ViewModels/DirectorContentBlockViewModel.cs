using DirectorPrompt.Domain.Enums;
using DirectorPrompt.Localization;

namespace DirectorPrompt.ViewModels;

public sealed class DirectorContentBlockViewModel
{
    public DirectiveType Type { get; init; }

    public string Content { get; init; } = string.Empty;

    public string TypeDisplay => Type switch
    {
        DirectiveType.Plot                => Loc.Get("Directive.Type.Plot"),
        DirectiveType.Tone                => Loc.Get("Directive.Type.Tone"),
        DirectiveType.TemporaryConstraint => Loc.Get("Directive.Type.TemporaryConstraint"),
        DirectiveType.SceneChange         => Loc.Get("Directive.Type.SceneChange"),
        _                                 => Type.ToString()
    };
}
