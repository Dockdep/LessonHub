using LessonsHub.Application.Abstractions.Services;
using LessonsHub.Application.Models.Jobs;
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
    private readonly IJobService _jobs;

    public LessonPlanController(ILessonPlanService plans, IJobService jobs)
    {
        _plans = plans;
        _jobs = jobs;
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

    /// <summary>
    /// Enqueues lesson-plan generation as a background job and returns 202 +
    /// jobId. The browser subscribes to the GenerationHub user group to receive
    /// the result via SignalR; HTTP polling on /api/jobs/{id} is the fallback.
    /// Idempotency: clients pass a header X-Idempotency-Key (UUID per click) so
    /// double-submits don't double-bill.
    /// </summary>
    [HttpPost("generate")]
    public async Task<IActionResult> GenerateLessonPlan(
        [FromBody] LessonPlanRequestDto request,
        [FromHeader(Name = "X-Idempotency-Key")] string? idempotencyKey,
        CancellationToken ct)
    {
        var validation = await _plans.ValidateGenerateAsync(request, ct);
        if (!validation.IsSuccess) return validation.ToActionResult();

        var jobId = await _jobs.EnqueueAsync(
            JobType.LessonPlanGenerate,
            request,
            idempotencyKey: idempotencyKey,
            ct: ct);

        return Accepted(new JobAcceptedResponse(jobId));
    }

    [HttpPost("save")]
    public async Task<IActionResult> SaveLessonPlan([FromBody] SaveLessonPlanRequestDto request) =>
        (await _plans.SaveAsync(request)).ToActionResult();
}
