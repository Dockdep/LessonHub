using System.Text.Json;
using LessonsHub.Application.Abstractions.Services;
using LessonsHub.Application.Models.Jobs;
using LessonsHub.Domain.Entities;

namespace LessonsHub.Application.Services.Executors;

public sealed record LessonContentGeneratePayload(int LessonId);

public sealed class LessonContentGenerateExecutor : IJobExecutor
{
    private readonly ILessonService _lessons;
    public LessonContentGenerateExecutor(ILessonService lessons) { _lessons = lessons; }

    public string Type => JobType.LessonContentGenerate;

    public async Task<object?> ExecuteAsync(Job job, CancellationToken ct)
    {
        var payload = JsonSerializer.Deserialize<LessonContentGeneratePayload>(job.PayloadJson)
                      ?? throw new InvalidOperationException("Empty payload for LessonContentGenerate job.");

        var result = await _lessons.GenerateContentAsync(payload.LessonId, ct);
        if (!result.IsSuccess)
            throw new ApplicationException(result.Message ?? $"Generation failed: {result.Error}");
        return result.Value;
    }
}
