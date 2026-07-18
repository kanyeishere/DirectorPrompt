using Dapper;
using DirectorPrompt.Domain.Enums;
using DirectorPrompt.Domain.Models;
using DirectorPrompt.Infrastructure.Repositories;

namespace DirectorPrompt.Tests;

public sealed class SceneRepositoryTests
{
    [Fact]
    public async Task CreateAsyncMakesSceneAvailableAsActive()
    {
        await using var context    = await DatabaseTestContext.CreateAsync();
        var             repository = new SceneRepository(context.Scheduler);

        var created = await repository.CreateAsync
                      (
                          new Scene
                          {
                              ProjectID        = 1,
                              SessionID        = 1,
                              TimelinePosition = 0,
                              TimeLabel        = "初始场景",
                              Status           = SceneStatus.Active
                          },
                          1,
                          1
                      );
        var active = await repository.GetActiveSceneAsync(1);
        var status = await context.Scheduler.ExecuteAsync
                     (async (connection, token) =>
                          await connection.ExecuteScalarAsync<string>
                          (
                              new CommandDefinition
                              (
                                  "SELECT status FROM scenes WHERE id = @id",
                                  new { id = created.ID },
                                  cancellationToken: token
                              )
                          )
                     );

        Assert.Equal("Active", status);
        Assert.NotNull(active);
        Assert.Equal(created.ID, active.ID);
    }

    [Fact]
    public async Task UpdateAsyncStoresSceneStatusAsText()
    {
        await using var context    = await DatabaseTestContext.CreateAsync();
        var             repository = new SceneRepository(context.Scheduler);

        var created = await repository.CreateAsync
                      (
                          new Scene
                          {
                              ProjectID        = 1,
                              SessionID        = 1,
                              TimelinePosition = 0,
                              TimeLabel        = "初始场景",
                              Status           = SceneStatus.Active
                          },
                          1,
                          1
                      );
        await repository.UpdateAsync(created with { Status = SceneStatus.Completed }, 1, 2);
        var status = await context.Scheduler.ExecuteAsync
                     (async (connection, token) =>
                          await connection.ExecuteScalarAsync<string>
                          (
                              new CommandDefinition
                              (
                                  "SELECT status FROM scenes WHERE id = @id",
                                  new { id = created.ID },
                                  cancellationToken: token
                              )
                          )
                     );

        Assert.Equal("Completed", status);
    }
}
