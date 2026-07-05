using DirectorPrompt.Domain.Models;

namespace DirectorPrompt.Domain.Repositories;

public interface ISceneRepository
{
    Task<Scene?> GetByIDAsync(long id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Scene>> GetByProjectAsync(long projectID, CancellationToken cancellationToken = default);

    Task<Scene?> GetActiveSceneAsync(long projectID, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Scene>> GetOrderedByTimelineAsync(long projectID, CancellationToken cancellationToken = default);

    Task<Scene> CreateAsync(Scene scene, CancellationToken cancellationToken = default);

    Task UpdateAsync(Scene scene, CancellationToken cancellationToken = default);
}
