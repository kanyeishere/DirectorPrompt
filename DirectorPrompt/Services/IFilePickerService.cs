namespace DirectorPrompt.Services;

public interface IFilePickerService
{
    Task<string?> OpenAsync(string displayName, string pattern);

    Task<string?> SaveAsync(string displayName, string pattern, string suggestedFileName);
}
