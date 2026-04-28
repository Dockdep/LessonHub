using System.Security.Claims;
using LessonsHub.Application.Abstractions;
using Microsoft.AspNetCore.Http;

namespace LessonsHub.Infrastructure.Auth;

/// <summary>
/// Resolves the acting user from one of two sources:
///   1. <see cref="UserContext"/>: populated by the JobBackgroundService when
///      executing a Job — its <c>UserId</c> overrides everything.
///   2. <see cref="IHttpContextAccessor"/>: the JWT claim on the current HTTP
///      request, used in normal request scopes.
/// </summary>
public sealed class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly UserContext _userContext;

    public CurrentUser(IHttpContextAccessor httpContextAccessor, UserContext userContext)
    {
        _httpContextAccessor = httpContextAccessor;
        _userContext = userContext;
    }

    public bool IsAuthenticated =>
        _userContext.UserId.HasValue
        || _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated == true;

    public int Id
    {
        get
        {
            if (_userContext.UserId.HasValue)
                return _userContext.UserId.Value;

            var raw = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(raw))
                throw new InvalidOperationException("No authenticated user on the current request.");
            return int.Parse(raw);
        }
    }
}
