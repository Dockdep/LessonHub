using System.Text.Json;
using LessonsHub.Application.Abstractions.Services;
using LessonsHub.Application.Models.Jobs;
using LessonsHub.Domain.Entities;

namespace LessonsHub.Application.Services.Executors;

public sealed record LessonContentRegeneratePayload(int LessonId, bool BypassDocCache);

public sealed class LessonContentRegenerateExecutor : IJobExecutor
{
    private readonly ILessonService _lessons;
    public LessonContentRegenerateExecutor(ILessonService lessons) { _lessons = lessons; }

    public string Type => JobType.LessonContentRegenerate;

    public async Task<object?> ExecuteAsync(Job job, CancellationToken ct)
    {
        var payload = JsonSerializer.Deserialize<LessonContentRegeneratePayload>(job.PayloadJson)
                      ?? throw new InvalidOperationException("Empty payload for LessonContentRegenerate job.");

        var result = await _lessons.RegenerateContentAsync(payload.LessonId, payload.BypassDocCache, ct);
        if (!result.IsSuccess)
            throw new ApplicationException(result.Message ?? $"Regeneration failed: {result.Error}");
        return result.Value;
    }
}
