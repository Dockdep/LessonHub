using LessonsHub.Domain.Entities;

namespace LessonsHub.Application.Abstractions.Repositories;

public interface IDocumentRepository : IRepository
{
    Task<Document?> GetForUserAsync(int id, int userId, CancellationToken ct = default);
    Task<List<Document>> ListForUserAsync(int userId, CancellationToken ct = default);
    void Add(Document doc);
    void Remove(Document doc);
}
