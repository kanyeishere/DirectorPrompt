using DirectorPrompt.Domain.Models;

namespace DirectorPrompt.Domain.Services;

public interface IProjectPortService
{
    Task ExportAsync(long projectID, string filePath, CancellationToken cancellationToken = default);

    Task<ProjectImportResult> ImportAsync(string filePath, CancellationToken cancellationToken = default);

    Task<ProjectImportResult> ImportSillyTavernAsync(string filePath, CancellationToken cancellationToken = default);
}
