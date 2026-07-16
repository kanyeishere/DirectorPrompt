using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using DirectorPrompt.Localization;
using DirectorPrompt.ViewModels;

namespace DirectorPrompt.Views;

public partial class SettingsWindow : Window
{
    private readonly SettingsViewModel viewModel;

    public SettingsWindow()
    {
        viewModel = null!;
        AvaloniaXamlLoader.Load(this);
    }

    public SettingsWindow(SettingsViewModel viewModel)
    {
        this.viewModel = viewModel;
        DataContext    = viewModel;
        AvaloniaXamlLoader.Load(this);
    }

    private void OnNavSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ProvidersPanel is null ||
            ModelsPanel is null    ||
            PromptsPanel is null   ||
            TasksPanel is null     ||
            EmbeddingPanel is null ||
            MemoryPanel is null    ||
            RetrievalPanel is null ||
            LanguagePanel is null)
            return;

        if (NavList.SelectedItem is not ListBoxItem item)
            return;

        var tag = item.Tag as string;

        ProvidersPanel.IsVisible = tag == "providers";
        ModelsPanel.IsVisible = tag == "models";
        PromptsPanel.IsVisible = tag == "prompts";
        TasksPanel.IsVisible = tag == "tasks";
        EmbeddingPanel.IsVisible = tag == "embedding";
        MemoryPanel.IsVisible = tag == "memory";
        RetrievalPanel.IsVisible = tag == "retrieval";
        LanguagePanel.IsVisible = tag == "language";
    }

    private async void OnRemoveProvider(object sender, RoutedEventArgs e)
    {
        if (sender is not Control { Tag: ProviderSettingViewModel provider })
            return;

        if (!await PromptDialog.ConfirmAsync(this, Loc.Get("Common.Remove"), Loc.Get("Dialog.ConfirmRemoveProvider", provider.DisplayName), true))
            return;

        viewModel.RemoveProviderCommand.Execute(provider);
    }

    private async void OnRemoveModel(object sender, RoutedEventArgs e)
    {
        if (sender is not Control { Tag: ModelSettingViewModel model })
            return;

        if (!await PromptDialog.ConfirmAsync(this, Loc.Get("Common.Remove"), Loc.Get("Dialog.ConfirmRemoveModel", model.DisplayName), true))
            return;

        viewModel.RemoveModelCommand.Execute(model);
    }

    private async void OnRemovePrompt(object sender, RoutedEventArgs e)
    {
        if (sender is not Control { Tag: PromptSettingViewModel prompt })
            return;

        if (!await PromptDialog.ConfirmAsync(this, Loc.Get("Common.Remove"), Loc.Get("Dialog.ConfirmRemovePrompt", prompt.DisplayName), true))
            return;

        viewModel.RemovePromptCommand.Execute(prompt);
    }

    private async void OnSaveClick(object sender, RoutedEventArgs e)
    {
        await viewModel.SaveCommand.ExecuteAsync(null);

        if (viewModel.SaveSuccess)
        {
            Close(true);
        }
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        Close(false);
    }
}
