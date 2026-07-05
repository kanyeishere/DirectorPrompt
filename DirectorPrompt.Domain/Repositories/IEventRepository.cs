using DirectorPrompt.Domain.Models;

namespace DirectorPrompt.Domain.Repositories;

public interface IEventRepository
{
    Task<PlaythroughEvent> AppendAsync(PlaythroughEvent eventItem, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PlaythroughEvent>> GetByProjectAsync(long projectID, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PlaythroughEvent>> GetByRoundAsync(long roundID, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PlaythroughEvent>> GetAfterAsync(long projectID, long eventID, CancellationToken cancellationToken = default);

    Task RemoveByRoundAsync(long roundID, CancellationToken cancellationToken = default);

    Task<long> GetLatestRoundIDAsync(long projectID, CancellationToken cancellationToken = default);
}
