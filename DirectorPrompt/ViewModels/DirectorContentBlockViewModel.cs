using System.ComponentModel;
using System.Windows.Documents;
using DirectorPrompt.Domain.Enums;
using DirectorPrompt.Localization;
using DirectorPrompt.Markdown;

namespace DirectorPrompt.ViewModels;

public sealed class DirectorContentBlockViewModel : INotifyPropertyChanged
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

    public FlowDocument? Document
    {
        get;
        private set
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Document)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void RenderMarkdown() =>
        Document = MarkdownRenderer.Render(Content);
}
