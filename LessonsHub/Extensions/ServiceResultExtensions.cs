using LessonsHub.Application.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LessonsHub.Extensions;

public static class ServiceResultExtensions
{
    public static IActionResult ToActionResult<T>(this ServiceResult<T> result) =>
        result.Error switch
        {
            ServiceErrorKind.None       => new OkObjectResult(result.Value),
            ServiceErrorKind.NotFound   => result.Message == null
                ? (IActionResult)new NotFoundResult()
                : new NotFoundObjectResult(new { message = result.Message }),
            ServiceErrorKind.BadRequest => new BadRequestObjectResult(new { message = result.Message }),
            ServiceErrorKind.Conflict   => new ConflictObjectResult(new { message = result.Message }),
            ServiceErrorKind.Unauthorized => new ObjectResult(new { message = result.Message ?? "Unauthorized." }) { StatusCode = StatusCodes.Status401Unauthorized },
            ServiceErrorKind.Forbidden  => new ObjectResult(new { message = result.Message ?? "Forbidden." }) { StatusCode = StatusCodes.Status403Forbidden },
            ServiceErrorKind.Timeout    => new ObjectResult(new { message = result.Message ?? "The AI service is taking too long. Please try again." }) { StatusCode = StatusCodes.Status504GatewayTimeout },
            ServiceErrorKind.Internal   => new ObjectResult(new { message = result.Message ?? "An internal error occurred." }) { StatusCode = StatusCodes.Status500InternalServerError },
            _ => throw new InvalidOperationException($"Unhandled ServiceErrorKind: {result.Error}")
        };

    public static IActionResult ToActionResult(this ServiceResult result) =>
        result.Error switch
        {
            ServiceErrorKind.None       => new OkObjectResult(new { message = result.Message }),
            ServiceErrorKind.NotFound   => result.Message == null
                ? (IActionResult)new NotFoundResult()
                : new NotFoundObjectResult(new { message = result.Message }),
            ServiceErrorKind.BadRequest => new BadRequestObjectResult(new { message = result.Message }),
            ServiceErrorKind.Conflict   => new ConflictObjectResult(new { message = result.Message }),
            ServiceErrorKind.Unauthorized => new ObjectResult(new { message = result.Message ?? "Unauthorized." }) { StatusCode = StatusCodes.Status401Unauthorized },
            ServiceErrorKind.Forbidden  => new ObjectResult(new { message = result.Message ?? "Forbidden." }) { StatusCode = StatusCodes.Status403Forbidden },
            ServiceErrorKind.Timeout    => new ObjectResult(new { message = result.Message ?? "The AI service is taking too long. Please try again." }) { StatusCode = StatusCodes.Status504GatewayTimeout },
            ServiceErrorKind.Internal   => new ObjectResult(new { message = result.Message ?? "An internal error occurred." }) { StatusCode = StatusCodes.Status500InternalServerError },
            _ => throw new InvalidOperationException($"Unhandled ServiceErrorKind: {result.Error}")
        };
}
