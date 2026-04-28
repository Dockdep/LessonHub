using System.Security.Claims;
using LessonsHub.Application.Abstractions;
using Microsoft.AspNetCore.Http;

namespace LessonsHub.Infrastructure.Auth;

public sealed class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public bool IsAuthenticated =>
        _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated == true;

    public int Id
    {
        get
        {
            var raw = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(raw))
                throw new InvalidOperationException("No authenticated user on the current request.");
            return int.Parse(raw);
        }
    }
}
