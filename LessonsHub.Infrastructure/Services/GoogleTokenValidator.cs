using Google.Apis.Auth;
using LessonsHub.Application.Interfaces;
using LessonsHub.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;

namespace LessonsHub.Infrastructure.Services;

public sealed class GoogleTokenValidator : IGoogleTokenValidator
{
    private readonly GoogleAuthSettings _settings;
    private readonly ILogger<GoogleTokenValidator> _logger;

    public GoogleTokenValidator(GoogleAuthSettings settings, ILogger<GoogleTokenValidator> logger)
    {
        _settings = settings;
        _logger = logger;
    }

    public async Task<GoogleTokenPayload?> ValidateAsync(string idToken, CancellationToken ct = default)
    {
        try
        {
            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { _settings.ClientId }
            };
            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);
            return new GoogleTokenPayload(payload.Subject, payload.Email, payload.Name, payload.Picture);
        }
        catch (InvalidJwtException ex)
        {
            _logger.LogWarning(ex, "Invalid Google ID token");
            return null;
        }
    }
}
