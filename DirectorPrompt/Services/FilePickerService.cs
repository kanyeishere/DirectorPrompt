using Avalonia.Platform.Storage;

namespace DirectorPrompt.Services;

public sealed class FilePickerService : IFilePickerService
{
    public async Task<string?> OpenAsync(string displayName, string pattern)
    {
        var storageProvider = App.GetActiveWindow()?.StorageProvider;

        if (storageProvider is null)
            return null;

        var files = await storageProvider.OpenFilePickerAsync
                    (
                        new FilePickerOpenOptions
                        {
                            AllowMultiple = false,
                            FileTypeFilter = [CreateFileType(displayName, pattern)]
                        }
                    );

        return files.Count == 0 ? null : files[0].TryGetLocalPath();
    }

    public async Task<string?> SaveAsync(string displayName, string pattern, string suggestedFileName)
    {
        var storageProvider = App.GetActiveWindow()?.StorageProvider;

        if (storageProvider is null)
            return null;

        var file = await storageProvider.SaveFilePickerAsync
                   (
                       new FilePickerSaveOptions
                       {
                           SuggestedFileName = suggestedFileName,
                           FileTypeChoices = [CreateFileType(displayName, pattern)]
                       }
                   );

        return file?.TryGetLocalPath();
    }

    private static FilePickerFileType CreateFileType(string displayName, string pattern) =>
        new(displayName) { Patterns = [pattern] };
}
