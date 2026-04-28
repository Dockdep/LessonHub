using LessonsHub.Application.Abstractions.Repositories;
using LessonsHub.Domain.Entities;
using LessonsHub.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LessonsHub.Infrastructure.Repositories;

public sealed class DocumentRepository : RepositoryBase, IDocumentRepository
{
    public DocumentRepository(LessonsHubDbContext db) : base(db) { }

    public Task<Document?> GetForUserAsync(int id, int userId, CancellationToken ct = default) =>
        _db.Documents.FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId, ct);

    public Task<List<Document>> ListForUserAsync(int userId, CancellationToken ct = default) =>
        _db.Documents
            .AsNoTracking()
            .Where(d => d.UserId == userId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(ct);

    public void Add(Document doc) => _db.Documents.Add(doc);

    public void Remove(Document doc) => _db.Documents.Remove(doc);
}
