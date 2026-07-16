using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using DirectorPrompt;

[assembly: AvaloniaTestApplication(typeof(DirectorPrompt.Tests.AvaloniaTestApp))]

namespace DirectorPrompt.Tests;

public static class AvaloniaTestApp
{
    public static AppBuilder BuildAvaloniaApp()
    {
        typeof(Design).GetProperty(nameof(Design.IsDesignMode))!.SetValue(null, true);

        return AppBuilder.Configure<App>()
                         .UseHeadless(new AvaloniaHeadlessPlatformOptions());
    }
}
