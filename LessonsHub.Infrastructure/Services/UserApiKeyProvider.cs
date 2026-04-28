using LessonsHub.Application.Abstractions;
using LessonsHub.Application.Interfaces;
using LessonsHub.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LessonsHub.Infrastructure.Services;

/// <summary>
/// Reads <see cref="Domain.Entities.User.GoogleApiKey"/> for the JWT-authenticated
/// caller. Throws when no key is set so AI calls fail loudly with a clear message
/// the SPA can surface in a profile-redirect prompt.
///
/// Resolves the user via <see cref="ICurrentUser"/> (not the raw HttpContext)
/// so this works inside both HTTP request scopes AND background-job scopes —
/// the JobBackgroundService sets <c>UserContext.UserId</c> before resolving
/// the executor, and CurrentUser falls through to that when no HttpContext
/// is available.
/// </summary>
public class UserApiKeyProvider : IUserApiKeyProvider
{
    private readonly LessonsHubDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public UserApiKeyProvider(LessonsHubDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<string> GetCurrentUserKeyAsync()
    {
        if (!_currentUser.IsAuthenticated)
            throw new InvalidOperationException("No authenticated user in context.");

        var userId = _currentUser.Id;
        var apiKey = await _dbContext.Users
            .Where(u => u.Id == userId)
            .Select(u => u.GoogleApiKey)
            .FirstOrDefaultAsync();

        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("Google API key is not set. Please set it in your profile.");

        return apiKey;
    }
}
