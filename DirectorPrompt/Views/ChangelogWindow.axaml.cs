using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.LogicalTree;
using DirectorPrompt.Views.Components;
using DirectorPrompt.Localization;
using FluentAvalonia.UI.Windowing;

namespace DirectorPrompt.Views;

public partial class ChangelogWindow : FAAppWindow
{
    public ChangelogWindow() =>
        AvaloniaXamlLoader.Load(this);

    public ChangelogWindow(string changelog, string version)
    {
        AvaloniaXamlLoader.Load(this);
        Title = $"{Loc.Get("Changelog.Title")} {version}";
        this.GetLogicalDescendants()
            .OfType<LiveMarkdownView>()
            .First(control => control.Name == "ChangelogViewer")
            .Markdown = changelog;
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e) =>
        Close();
}
