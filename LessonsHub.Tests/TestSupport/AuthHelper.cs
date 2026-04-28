using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LessonsHub.Tests.TestSupport;

public static class AuthHelper
{
    /// <summary>
    /// Creates a ClaimsPrincipal whose NameIdentifier matches the given user id.
    /// </summary>
    public static ClaimsPrincipal PrincipalFor(int userId)
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        }, authenticationType: "Test");
        return new ClaimsPrincipal(identity);
    }

    /// <summary>
    /// Builds a ControllerContext with HttpContext.User set, so controllers can read the
    /// current user from claims as they do at runtime.
    /// </summary>
    public static ControllerContext ContextFor(int userId)
    {
        var httpContext = new DefaultHttpContext { User = PrincipalFor(userId) };
        return new ControllerContext { HttpContext = httpContext };
    }

    /// <summary>
    /// Attaches a current-user identity to a controller. Returns the controller for chaining.
    /// </summary>
    public static T As<T>(this T controller, int userId) where T : ControllerBase
    {
        controller.ControllerContext = ContextFor(userId);
        return controller;
    }
}
