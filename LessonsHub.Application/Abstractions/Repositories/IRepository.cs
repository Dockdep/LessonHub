namespace LessonsHub.Application.Abstractions.Repositories;

/// <summary>
/// Common base for all repositories. Persistence (save) lives on the repo
/// itself — there is no separate UnitOfWork. All repositories in a request
/// share the same DbContext via DI scope, so calling SaveChangesAsync on any
/// of them flushes pending changes for the whole graph.
/// </summary>
public interface IRepository
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
