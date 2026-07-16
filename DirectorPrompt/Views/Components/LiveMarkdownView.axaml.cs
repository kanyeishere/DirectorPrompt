using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Avalonia.LogicalTree;
using LiveMarkdown.Avalonia;

namespace DirectorPrompt.Views.Components;

public sealed partial class LiveMarkdownView : UserControl
{
    public static readonly StyledProperty<string> MarkdownProperty =
        AvaloniaProperty.Register<LiveMarkdownView, string>(nameof(Markdown), string.Empty);

    private readonly ObservableStringBuilder markdownBuilder = new();
    private string renderedMarkdown = string.Empty;
    private bool updateScheduled;

    private MarkdownRenderer RendererControl =>
        this.GetLogicalDescendants().OfType<MarkdownRenderer>().First(control => control.Name == "Renderer");

    public string Markdown
    {
        get => GetValue(MarkdownProperty);
        set => SetValue(MarkdownProperty, value);
    }

    internal ObservableStringBuilder MarkdownBuilder => markdownBuilder;

    static LiveMarkdownView() =>
        MarkdownProperty.Changed.AddClassHandler<LiveMarkdownView>(static (view, _) => view.ScheduleMarkdownUpdate());

    public LiveMarkdownView()
    {
        AvaloniaXamlLoader.Load(this);
        RendererControl.MarkdownBuilder = markdownBuilder;
    }

    private void ScheduleMarkdownUpdate()
    {
        if (updateScheduled)
            return;

        updateScheduled = true;
        Dispatcher.UIThread.Post
        (() =>
            {
                updateScheduled = false;
                UpdateMarkdown();
            },
            DispatcherPriority.Background
        );
    }

    internal void UpdateMarkdown()
    {
        if (!Dispatcher.UIThread.CheckAccess())
            throw new InvalidOperationException("Markdown updates must run on the UI thread");

        var markdown = Markdown ?? string.Empty;

        if (markdown == renderedMarkdown)
            return;

        if (markdown.StartsWith(renderedMarkdown, StringComparison.Ordinal))
            markdownBuilder.Append(markdown[renderedMarkdown.Length..]);
        else
        {
            markdownBuilder.Clear();
            markdownBuilder.Append(markdown);
        }

        renderedMarkdown = markdown;
    }
}
