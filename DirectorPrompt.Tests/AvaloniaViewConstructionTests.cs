using Avalonia.Headless.XUnit;
using DirectorPrompt.Views;
using DirectorPrompt.Views.Components;

namespace DirectorPrompt.Tests;

public sealed class AvaloniaViewConstructionTests
{
    [AvaloniaFact]
    public void PrimaryViewsAndControlsConstruct()
    {
        Assert.NotNull(new MainWindow());
        Assert.NotNull(new ProjectEditWindow());
        Assert.NotNull(new SettingsWindow());
        Assert.NotNull(new PromptDialog());
        Assert.NotNull(new UpdateWindow());
        Assert.NotNull(new ChangelogWindow());
        Assert.NotNull(new DirectiveInputControl());
        Assert.NotNull(new PhaseEditControl());
        Assert.NotNull(new MessageRail());
    }
}
