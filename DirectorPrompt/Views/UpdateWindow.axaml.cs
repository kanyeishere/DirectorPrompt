using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using DirectorPrompt.Localization;

namespace DirectorPrompt.Views;

public partial class UpdateWindow : Window
{
    private TaskCompletionSource? closeCompletion;

    public UpdateWindow()
    {
        AvaloniaXamlLoader.Load(this);
        Title = Loc.Get("Update.Title");
    }

    public void UpdateStatus(string status) =>
        Dispatcher.UIThread.Post(() => StatusText.Text = status);

    public void UpdateProgress(int progress) =>
        Dispatcher.UIThread.Post(() =>
        {
            ProgressBar.IsIndeterminate = false;
            ProgressBar.Value = progress;
            ProgressText.IsVisible = true;
            ProgressText.Text = $"{progress}%";
        });

    public void ShowError(string message)
    {
        ProgressPanel.IsVisible = false;
        ErrorPanel.IsVisible = true;
        ErrorText.Text = message;
        CloseButton.Content = Loc.Get("Common.Close");
    }

    public Task WaitForCloseAsync()
    {
        closeCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        return closeCompletion.Task;
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e) =>
        closeCompletion?.TrySetResult();
}
