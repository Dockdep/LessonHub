namespace LessonsHub.Application.Interfaces;

/// <summary>
/// Resolves the current user's per-user Gemini API key. Implementations decide
/// where the key lives (DB column today; could be a vault tomorrow).
/// </summary>
public interface IUserApiKeyProvider
{
    /// <summary>
    /// Returns the current user's Gemini API key.
    /// Throws InvalidOperationException when the user has no key set.
    /// </summary>
    Task<string> GetCurrentUserKeyAsync();
}
