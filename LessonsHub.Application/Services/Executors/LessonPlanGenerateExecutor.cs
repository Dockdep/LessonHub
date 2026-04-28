using System.Text.Json;
using LessonsHub.Application.Abstractions.Services;
using LessonsHub.Application.Models.Jobs;
using LessonsHub.Application.Models.Requests;
using LessonsHub.Domain.Entities;

namespace LessonsHub.Application.Services.Executors;

/// <summary>
/// Job-side counterpart to LessonPlanController.GenerateLessonPlan. Reads the
/// JSON-serialized LessonPlanRequestDto, calls the existing service method,
/// and unwraps the ServiceResult — successful values become the Job.ResultJson,
/// error states throw so the framework marks the Job Failed.
///
/// We re-run validation defensively because state can change between enqueue
/// and execution (in practice rare for plan generation, but the pattern lets
/// later executors handle "lesson got deleted while job was queued" cleanly).
/// </summary>
public sealed class LessonPlanGenerateExecutor : IJobExecutor
{
    private readonly ILessonPlanService _plans;

    public LessonPlanGenerateExecutor(ILessonPlanService plans)
    {
        _plans = plans;
    }

    public string Type => JobType.LessonPlanGenerate;

    public async Task<object?> ExecuteAsync(Job job, CancellationToken ct)
    {
        var request = JsonSerializer.Deserialize<LessonPlanRequestDto>(job.PayloadJson)
                      ?? throw new InvalidOperationException("Empty payload for LessonPlanGenerate job.");

        var result = await _plans.GenerateAsync(request, ct);
        if (!result.IsSuccess)
            throw new ApplicationException(result.Message ?? $"Generation failed: {result.Error}");

        return result.Value;
    }
}
