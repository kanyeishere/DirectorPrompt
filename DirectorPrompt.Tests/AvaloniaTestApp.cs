using Avalonia;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using DirectorPrompt;

[assembly: AvaloniaTestApplication(typeof(DirectorPrompt.Tests.AvaloniaTestApp))]

namespace DirectorPrompt.Tests;

public static class AvaloniaTestApp
{
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
                  .UseHeadless(new AvaloniaHeadlessPlatformOptions());
}
