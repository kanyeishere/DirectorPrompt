using DirectorPrompt.Domain.Models;
using DirectorPrompt.Views;
using Microsoft.Extensions.DependencyInjection;

namespace DirectorPrompt.Services;

public sealed class WindowService(IServiceProvider serviceProvider) : IWindowService
{
    public Task<string?> InputAsync(string title, string prompt, string defaultValue) =>
        PromptDialog.InputAsync(App.GetActiveWindow(), title, prompt, defaultValue);

    public async Task<bool> EditProjectAsync(Project project)
    {
        var window = serviceProvider.GetRequiredService<ProjectEditWindow>();
        await window.ViewModel.LoadFromProjectAsync(project);
        var owner = App.GetActiveWindow();

        return owner is not null && await window.ShowDialog<bool>(owner);
    }

    public async Task ShowSettingsAsync()
    {
        var window = serviceProvider.GetRequiredService<SettingsWindow>();
        var owner = App.GetActiveWindow();

        if (owner is not null)
            await window.ShowDialog(owner);
        else
            window.Show();
    }
}
