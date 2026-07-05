using DirectorPrompt.Domain.Models;

namespace DirectorPrompt.Domain.Repositories;

public interface IDirectiveRepository
{
    Task<IReadOnlyList<ActiveDirective>> GetActiveAsync(long projectID, CancellationToken cancellationToken = default);

    Task<ActiveDirective> AddAsync(ActiveDirective directive, CancellationToken cancellationToken = default);

    Task RemoveAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    ///     递减所有带 TTL 的指令, 移除已过期的, 返回被移除的列表
    /// </summary>
    Task<IReadOnlyList<ActiveDirective>> DecrementTTLAsync(long projectID, CancellationToken cancellationToken = default);
}
