using DirectorPrompt.Domain.Models;

namespace DirectorPrompt.Domain.Repositories;

public interface IStateSnapshotRepository
{
    Task<StateSnapshot?> GetLatestAsync(long projectID, long beforeRoundID, CancellationToken cancellationToken = default);

    Task<StateSnapshot> CreateAsync(StateSnapshot snapshot, CancellationToken cancellationToken = default);

    Task<StateSnapshot?> GetBySceneAsync(long sceneID, CancellationToken cancellationToken = default);
}
