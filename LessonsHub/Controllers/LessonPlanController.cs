using LessonsHub.Application.Abstractions.Services;
using LessonsHub.Application.Models.Requests;
using LessonsHub.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LessonsHub.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LessonPlanController : ControllerBase
{
    private readonly ILessonPlanService _plans;

    public LessonPlanController(ILessonPlanService plans)
    {
        _plans = plans;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetLessonPlanDetail(int id) =>
        (await _plans.GetDetailAsync(id)).ToActionResult();

    [HttpGet("shared-with-me")]
    public async Task<IActionResult> GetSharedWithMe() =>
        (await _plans.GetSharedWithMeAsync()).ToActionResult();

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteLessonPlan(int id) =>
        (await _plans.DeleteAsync(id)).ToActionResult();

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateLessonPlan(int id, [FromBody] UpdateLessonPlanRequestDto request) =>
        (await _plans.UpdateAsync(id, request)).ToActionResult();

    [HttpPost("generate")]
    public async Task<IActionResult> GenerateLessonPlan([FromBody] LessonPlanRequestDto request) =>
        (await _plans.GenerateAsync(request)).ToActionResult();

    [HttpPost("save")]
    public async Task<IActionResult> SaveLessonPlan([FromBody] SaveLessonPlanRequestDto request) =>
        (await _plans.SaveAsync(request)).ToActionResult();
}
