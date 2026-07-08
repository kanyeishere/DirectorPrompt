using DirectorPrompt.Localization;
using Wpf.Ui.Controls;

namespace DirectorPrompt.Views;

public partial class UpdateWindow : FluentWindow
{
    public UpdateWindow() =>
        InitializeComponent();

    public void UpdateStatus(string status) =>
        StatusText.Text = status;

    public void UpdateProgress(int progress)
    {
        ProgressRing.IsIndeterminate = false;
        ProgressRing.Progress        = progress;
    }
}
