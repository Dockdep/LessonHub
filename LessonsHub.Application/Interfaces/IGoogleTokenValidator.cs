namespace LessonsHub.Application.Interfaces;

public sealed record GoogleTokenPayload(
    string Subject,
    string Email,
    string? Name,
    string? Picture);

public interface IGoogleTokenValidator
{
    /// <summary>
    /// Validates a Google ID token and returns the resolved payload.
    /// Returns null if the token is invalid (expired, wrong audience, malformed, etc.).
    /// </summary>
    Task<GoogleTokenPayload?> ValidateAsync(string idToken, CancellationToken ct = default);
}
