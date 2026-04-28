using LessonsHub.Application.Abstractions.Services;
using LessonsHub.Application.Models.Responses;
using LessonsHub.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LessonsHub.Controllers;

/// <summary>
/// Share-management endpoints for lesson plans. Lives on the same
/// <c>/api/lessonplan</c> route prefix as <see cref="LessonPlanController"/>
/// so existing URLs are unchanged.
/// </summary>
[ApiController]
[Route("api/lessonplan")]
[Authorize]
public class LessonPlanShareController : ControllerBase
{
    private readonly ILessonPlanShareService _shares;

    public LessonPlanShareController(ILessonPlanShareService shares)
    {
        _shares = shares;
    }

    [HttpGet("{id}/shares")]
    public async Task<IActionResult> GetShares(int id) =>
        (await _shares.GetSharesAsync(id)).ToActionResult();

    [HttpPost("{id}/shares")]
    public async Task<IActionResult> AddShare(int id, [FromBody] AddShareRequestDto request) =>
        (await _shares.AddShareAsync(id, request?.Email)).ToActionResult();

    [HttpDelete("{id}/shares/{shareUserId}")]
    public async Task<IActionResult> RemoveShare(int id, int shareUserId) =>
        (await _shares.RemoveShareAsync(id, shareUserId)).ToActionResult();
}
