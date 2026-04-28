using LessonsHub.Application.Abstractions.Services;
using LessonsHub.Application.Models.Jobs;
using LessonsHub.Application.Models.Requests;
using LessonsHub.Application.Services.Executors;
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
    private readonly IJobService _jobs;

    public LessonController(ILessonService lessons, IExerciseService exercises, IJobService jobs)
    {
        _lessons = lessons;
        _exercises = exercises;
        _jobs = jobs;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetLesson(int id) =>
        (await _lessons.GetDetailAsync(id)).ToActionResult();

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateLesson(int id, [FromBody] UpdateLessonInfoDto request) =>
        (await _lessons.UpdateAsync(id, request)).ToActionResult();

    /// <summary>
    /// Lazy/explicit content generation. Frontend calls this when GetLesson
    /// returns a lesson with empty Content. Returns 202 + jobId; result lands
    /// via SignalR.
    /// </summary>
    [HttpPost("{id}/generate-content")]
    public async Task<IActionResult> GenerateContent(
        int id,
        [FromHeader(Name = "X-Idempotency-Key")] string? idempotencyKey,
        CancellationToken ct)
    {
        var validation = await _lessons.ValidateGenerateContentAsync(id, ct);
        if (!validation.IsSuccess) return validation.ToActionResult();

        var jobId = await _jobs.EnqueueAsync(
            JobType.LessonContentGenerate,
            new LessonContentGeneratePayload(id),
            idempotencyKey: idempotencyKey,
            relatedEntityType: "Lesson",
            relatedEntityId: id,
            ct: ct);

        return Accepted(new JobAcceptedResponse(jobId));
    }

    /// <summary>Owner-only regenerate. Same job pattern as generate-content.</summary>
    [HttpPost("{id}/regenerate-content")]
    public async Task<IActionResult> RegenerateContent(
        int id,
        [FromQuery] bool bypassDocCache,
        [FromHeader(Name = "X-Idempotency-Key")] string? idempotencyKey,
        CancellationToken ct)
    {
        var validation = await _lessons.ValidateRegenerateContentAsync(id, ct);
        if (!validation.IsSuccess) return validation.ToActionResult();

        var jobId = await _jobs.EnqueueAsync(
            JobType.LessonContentRegenerate,
            new LessonContentRegeneratePayload(id, bypassDocCache),
            idempotencyKey: idempotencyKey,
            relatedEntityType: "Lesson",
            relatedEntityId: id,
            ct: ct);

        return Accepted(new JobAcceptedResponse(jobId));
    }

    [HttpPost("{id}/generate-exercise")]
    public async Task<IActionResult> GenerateExercise(
        int id,
        [FromQuery] string difficulty,
        [FromQuery] string? comment,
        [FromHeader(Name = "X-Idempotency-Key")] string? idempotencyKey,
        CancellationToken ct)
    {
        difficulty = string.IsNullOrWhiteSpace(difficulty) ? "medium" : difficulty;

        var validation = await _exercises.ValidateGenerateAsync(id, difficulty, comment, ct);
        if (!validation.IsSuccess) return validation.ToActionResult();

        var jobId = await _jobs.EnqueueAsync(
            JobType.ExerciseGenerate,
            new ExerciseGeneratePayload(id, difficulty, comment),
            idempotencyKey: idempotencyKey,
            relatedEntityType: "Lesson",
            relatedEntityId: id,
            ct: ct);

        return Accepted(new JobAcceptedResponse(jobId));
    }

    [HttpPost("{id}/retry-exercise")]
    public async Task<IActionResult> RetryExercise(
        int id,
        [FromQuery] string difficulty,
        [FromQuery] string? comment,
        [FromQuery] string review,
        [FromHeader(Name = "X-Idempotency-Key")] string? idempotencyKey,
        CancellationToken ct)
    {
        difficulty = string.IsNullOrWhiteSpace(difficulty) ? "medium" : difficulty;
        review ??= "";

        var validation = await _exercises.ValidateRetryAsync(id, difficulty, comment, review, ct);
        if (!validation.IsSuccess) return validation.ToActionResult();

        var jobId = await _jobs.EnqueueAsync(
            JobType.ExerciseRetry,
            new ExerciseRetryPayload(id, difficulty, comment, review),
            idempotencyKey: idempotencyKey,
            relatedEntityType: "Lesson",
            relatedEntityId: id,
            ct: ct);

        return Accepted(new JobAcceptedResponse(jobId));
    }

    [HttpPost("exercise/{exerciseId}/check")]
    public async Task<IActionResult> CheckExerciseReview(
        int exerciseId,
        [FromBody] string answer,
        [FromHeader(Name = "X-Idempotency-Key")] string? idempotencyKey,
        CancellationToken ct)
    {
        var validation = await _exercises.ValidateCheckAnswerAsync(exerciseId, answer, ct);
        if (!validation.IsSuccess) return validation.ToActionResult();

        var jobId = await _jobs.EnqueueAsync(
            JobType.ExerciseReview,
            new ExerciseReviewPayload(exerciseId, answer),
            idempotencyKey: idempotencyKey,
            relatedEntityType: "Exercise",
            relatedEntityId: exerciseId,
            ct: ct);

        return Accepted(new JobAcceptedResponse(jobId));
    }

    [HttpGet("{id}/siblings")]
    public async Task<IActionResult> GetSiblingLessonIds(int id) =>
        (await _lessons.GetSiblingLessonIdsAsync(id)).ToActionResult();

    [HttpPatch("{id}/complete")]
    public async Task<IActionResult> ToggleLessonComplete(int id) =>
        (await _lessons.ToggleCompleteAsync(id)).ToActionResult();
}
