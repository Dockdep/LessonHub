using System.Net.Http.Headers;
using Google.Apis.Auth.OAuth2;
using LessonsHub.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;

namespace LessonsHub.Infrastructure.Services;

/// <summary>
/// Attaches a Google-issued OIDC ID token to outbound requests so the receiver
/// (the Python AI service running as a private Cloud Run service with
/// <c>--ingress=internal</c>) can authenticate the caller.
///
/// On Cloud Run, Application Default Credentials resolve to the workload's
/// service account automatically — no key files involved. Locally there are
/// no GCP credentials to pick up, so the call simply proceeds without an
/// Authorization header (the local Python container is unauthenticated, so
/// this is fine).
/// </summary>
public class IamAuthHandler : DelegatingHandler
{
    private readonly ILogger<IamAuthHandler> _logger;
    private readonly string? _audience;
    private OidcToken? _oidcToken;

    public IamAuthHandler(LessonsAiApiSettings settings, ILogger<IamAuthHandler> logger)
    {
        _logger = logger;
        if (!string.IsNullOrWhiteSpace(settings.BaseUrl))
        {
            _audience = new Uri(settings.BaseUrl).GetLeftPart(UriPartial.Authority);
        }
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(_audience))
        {
            var token = await TryGetIdTokenAsync(cancellationToken);
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }
        return await base.SendAsync(request, cancellationToken);
    }

    private async Task<string?> TryGetIdTokenAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (_oidcToken == null)
            {
                var credential = await GoogleCredential.GetApplicationDefaultAsync(cancellationToken);
                _oidcToken = await credential.GetOidcTokenAsync(
                    OidcTokenOptions.FromTargetAudience(_audience!),
                    cancellationToken);
            }
            return await _oidcToken.GetAccessTokenAsync(cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            // No ADC available (local dev) or token request failed.
            // Proceeding without auth lets local docker-compose work; in Cloud
            // Run the Python service will reject the unauth'd request via IAM.
            _logger.LogDebug(ex, "IAM auth header skipped (no Google credentials available)");
            return null;
        }
    }
}
