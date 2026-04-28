using LessonsHub.Domain.Entities;

namespace LessonsHub.Application.Abstractions.Repositories;

public interface IUserRepository : IRepository
{
    Task<User?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<User?> GetByGoogleIdAsync(string googleId, CancellationToken ct = default);
    void Add(User user);
}
