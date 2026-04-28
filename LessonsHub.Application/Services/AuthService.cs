using LessonsHub.Application.Abstractions;
using LessonsHub.Application.Abstractions.Repositories;
using LessonsHub.Application.Abstractions.Services;
using LessonsHub.Application.Interfaces;
using LessonsHub.Application.Models.Requests;
using LessonsHub.Application.Models.Responses;
using LessonsHub.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace LessonsHub.Application.Services;

public sealed class AuthService : IAuthService
{
    private readonly IGoogleTokenValidator _googleValidator;
    private readonly IUserRepository _users;
    private readonly ITokenService _tokens;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IGoogleTokenValidator googleValidator,
        IUserRepository users,
        ITokenService tokens,
        ILogger<AuthService> logger)
    {
        _googleValidator = googleValidator;
        _users = users;
        _tokens = tokens;
        _logger = logger;
    }

    public async Task<ServiceResult<LoginResponseDto>> LoginWithGoogleAsync(GoogleLoginRequest request, CancellationToken ct = default)
    {
        try
        {
            var payload = await _googleValidator.ValidateAsync(request.IdToken, ct);
            if (payload == null)
                return ServiceResult<LoginResponseDto>.Unauthorized("Invalid Google token.");

            var user = await _users.GetByGoogleIdAsync(payload.Subject, ct);
            if (user == null)
            {
                user = new User
                {
                    GoogleId = payload.Subject,
                    Email = payload.Email,
                    Name = payload.Name ?? string.Empty,
                    PictureUrl = payload.Picture,
                    CreatedAt = DateTime.UtcNow
                };
                _users.Add(user);
                await _users.SaveChangesAsync(ct);
                _logger.LogInformation("New user registered: {Email}", user.Email);
            }

            var token = _tokens.CreateToken(user);
            return ServiceResult<LoginResponseDto>.Ok(new LoginResponseDto
            {
                Token = token,
                User = new LoginUserDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    Name = user.Name,
                    PictureUrl = user.PictureUrl
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Google login");
            return ServiceResult<LoginResponseDto>.Internal("Authentication failed.");
        }
    }
}
