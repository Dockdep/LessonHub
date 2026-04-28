using System.Security.Claims;
using LessonsHub.Application.Interfaces;
using LessonsHub.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace LessonsHub.Infrastructure.Services;

/// <summary>
/// Reads <see cref="Domain.Entities.User.GoogleApiKey"/> for the JWT-authenticated
/// caller. Throws when no key is set so AI calls fail loudly with a clear message
/// the SPA can surface in a profile-redirect prompt.
/// </summary>
public class UserApiKeyProvider : IUserApiKeyProvider
{
    private readonly LessonsHubDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserApiKeyProvider(LessonsHubDbContext dbContext, IHttpContextAccessor httpContextAccessor)
    {
        _dbContext = dbContext;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<string> GetCurrentUserKeyAsync()
    {
        var userIdString = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdString, out var userId))
            throw new InvalidOperationException("No authenticated user in context.");

        var apiKey = await _dbContext.Users
            .Where(u => u.Id == userId)
            .Select(u => u.GoogleApiKey)
            .FirstOrDefaultAsync();

        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("Google API key is not set. Please set it in your profile.");

        return apiKey;
    }
}
