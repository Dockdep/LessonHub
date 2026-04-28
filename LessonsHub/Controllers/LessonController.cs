using LessonsHub.Application.Abstractions.Services;
using LessonsHub.Application.Models.Requests;
using LessonsHub.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LessonsHub.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class LessonController : ControllerBase
{
    private readonly ILessonService _lessons;
    private readonly IExerciseService _exercises;

    public LessonController(ILessonService lessons, IExerciseService exercises)
    {
        _lessons = lessons;
        _exercises = exercises;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetLesson(int id) =>
        (await _lessons.GetDetailAsync(id)).ToActionResult();

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateLesson(int id, [FromBody] UpdateLessonInfoDto request) =>
        (await _lessons.UpdateAsync(id, request)).ToActionResult();

    [HttpPost("{id}/regenerate-content")]
    public async Task<IActionResult> RegenerateContent(int id, [FromQuery] bool bypassDocCache = false) =>
        (await _lessons.RegenerateContentAsync(id, bypassDocCache)).ToActionResult();

    [HttpPost("{id}/generate-exercise")]
    public async Task<IActionResult> GenerateExercise(int id, [FromQuery] string difficulty = "medium", [FromQuery] string? comment = null) =>
        (await _exercises.GenerateAsync(id, difficulty, comment)).ToActionResult();

    [HttpPost("{id}/retry-exercise")]
    public async Task<IActionResult> RetryExercise(int id, [FromQuery] string difficulty = "medium", [FromQuery] string? comment = null, [FromQuery] string review = "") =>
        (await _exercises.RetryAsync(id, difficulty, comment, review)).ToActionResult();

    [HttpPost("exercise/{exerciseId}/check")]
    public async Task<IActionResult> CheckExerciseReview(int exerciseId, [FromBody] string answer) =>
        (await _exercises.CheckAnswerAsync(exerciseId, answer)).ToActionResult();

    [HttpGet("{id}/siblings")]
    public async Task<IActionResult> GetSiblingLessonIds(int id) =>
        (await _lessons.GetSiblingLessonIdsAsync(id)).ToActionResult();

    [HttpPatch("{id}/complete")]
    public async Task<IActionResult> ToggleLessonComplete(int id) =>
        (await _lessons.ToggleCompleteAsync(id)).ToActionResult();
}
