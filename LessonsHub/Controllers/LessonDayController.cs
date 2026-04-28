using LessonsHub.Application.Abstractions.Services;
using LessonsHub.Application.Models.Requests;
using LessonsHub.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LessonsHub.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LessonDayController : ControllerBase
{
    private readonly ILessonDayService _days;

    public LessonDayController(ILessonDayService days)
    {
        _days = days;
    }

    [HttpGet("plans")]
    public async Task<IActionResult> GetLessonPlans() =>
        (await _days.GetUserPlansAsync()).ToActionResult();

    [HttpGet("plans/{lessonPlanId}/lessons")]
    public async Task<IActionResult> GetAvailableLessons(int lessonPlanId) =>
        (await _days.GetAvailableLessonsAsync(lessonPlanId)).ToActionResult();

    [HttpGet("{year}/{month}")]
    public async Task<IActionResult> GetLessonDaysByMonth(int year, int month) =>
        (await _days.GetLessonDaysByMonthAsync(year, month)).ToActionResult();

    [HttpPost("assign")]
    public async Task<IActionResult> AssignLesson([FromBody] AssignLessonRequestDto request) =>
        (await _days.AssignLessonAsync(request)).ToActionResult();

    [HttpDelete("unassign/{lessonId}")]
    public async Task<IActionResult> UnassignLesson(int lessonId) =>
        (await _days.UnassignLessonAsync(lessonId)).ToActionResult();

    [HttpGet("date/{date}")]
    public async Task<IActionResult> GetLessonDayByDate(DateTime date) =>
        (await _days.GetLessonDayByDateAsync(date)).ToActionResult();
}
