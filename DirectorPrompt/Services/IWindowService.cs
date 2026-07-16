using DirectorPrompt.Domain.Models;

namespace DirectorPrompt.Services;

public interface IWindowService
{
    Task<string?> InputAsync(string title, string prompt, string defaultValue);

    Task<bool> EditProjectAsync(Project project);

    Task ShowSettingsAsync();
}
