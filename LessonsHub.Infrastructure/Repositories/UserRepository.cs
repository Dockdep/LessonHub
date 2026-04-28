using LessonsHub.Application.Abstractions.Repositories;
using LessonsHub.Domain.Entities;
using LessonsHub.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LessonsHub.Infrastructure.Repositories;

public sealed class UserRepository : RepositoryBase, IUserRepository
{
    public UserRepository(LessonsHubDbContext db) : base(db) { }

    public Task<User?> GetByIdAsync(int id, CancellationToken ct = default) =>
        _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        _db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

    public Task<User?> GetByGoogleIdAsync(string googleId, CancellationToken ct = default) =>
        _db.Users.FirstOrDefaultAsync(u => u.GoogleId == googleId, ct);

    public void Add(User user) => _db.Users.Add(user);
}
