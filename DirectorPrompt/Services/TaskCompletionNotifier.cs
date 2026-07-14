using System.Windows;
using CommunityToolkit.WinUI.Notifications;

namespace DirectorPrompt.Services;

public sealed class TaskCompletionNotifier : IDisposable
{
    private bool disposed;

    public TaskCompletionNotifier() =>
        ToastNotificationManagerCompat.OnActivated += OnNotificationActivated;

    public void NotifyIfApplicationInBackground(string title, string message)
    {
        if (disposed || Application.Current.Windows.Cast<Window>().Any(window => window.IsActive))
            return;

        new ToastContentBuilder()
            .AddText(title)
            .AddText(message)
            .Show();
    }

    public void Dispose()
    {
        if (disposed)
            return;

        disposed = true;
        ToastNotificationManagerCompat.OnActivated -= OnNotificationActivated;
    }

    private static void OnNotificationActivated(ToastNotificationActivatedEventArgsCompat args) =>
        Application.Current.Dispatcher.BeginInvoke
        (() =>
            {
                var window = Application.Current.MainWindow;

                if (window is null)
                    return;

                if (window.WindowState == WindowState.Minimized)
                    window.WindowState = WindowState.Normal;

                window.Show();
                window.Activate();
            }
        );
}
