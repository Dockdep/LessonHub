using LessonsHub.Application.Abstractions;
using LessonsHub.Application.Abstractions.Repositories;
using LessonsHub.Application.Abstractions.Services;
using LessonsHub.Application.Models.Requests;
using LessonsHub.Application.Models.Responses;
using LessonsHub.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace LessonsHub.Application.Services;

public sealed class UserProfileService : IUserProfileService
{
    private readonly IUserRepository _users;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<UserProfileService> _logger;

    public UserProfileService(
        IUserRepository users,
        ICurrentUser currentUser,
        ILogger<UserProfileService> logger)
    {
        _users = users;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<ServiceResult<UserProfileDto>> GetProfileAsync(CancellationToken ct = default)
    {
        var user = await _users.GetByIdAsync(_currentUser.Id, ct);
        if (user == null) return ServiceResult<UserProfileDto>.NotFound();
        return ServiceResult<UserProfileDto>.Ok(ToDto(user));
    }

    public async Task<ServiceResult<UserProfileDto>> UpdateProfileAsync(UpdateUserProfileRequest request, CancellationToken ct = default)
    {
        var user = await _users.GetByIdAsync(_currentUser.Id, ct);
        if (user == null) return ServiceResult<UserProfileDto>.NotFound();

        user.GoogleApiKey = string.IsNullOrWhiteSpace(request.GoogleApiKey) ? null : request.GoogleApiKey.Trim();
        await _users.SaveChangesAsync(ct);

        _logger.LogInformation("Updated GoogleApiKey for user {UserId}", user.Id);
        return ServiceResult<UserProfileDto>.Ok(ToDto(user));
    }

    private static UserProfileDto ToDto(User user) => new()
    {
        Email = user.Email,
        Name = user.Name,
        PictureUrl = user.PictureUrl,
        GoogleApiKey = user.GoogleApiKey
    };
}
