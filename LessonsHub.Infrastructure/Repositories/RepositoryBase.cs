using LessonsHub.Application.Abstractions.Repositories;
using LessonsHub.Infrastructure.Data;

namespace LessonsHub.Infrastructure.Repositories;

public abstract class RepositoryBase : IRepository
{
    protected readonly LessonsHubDbContext _db;

    protected RepositoryBase(LessonsHubDbContext db)
    {
        _db = db;
    }

    public Task<int> SaveChangesAsync(CancellationToken ct = default) =>
        _db.SaveChangesAsync(ct);
}
