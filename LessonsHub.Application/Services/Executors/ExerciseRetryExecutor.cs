using System.Text.Json;
using LessonsHub.Application.Abstractions.Services;
using LessonsHub.Application.Models.Jobs;
using LessonsHub.Domain.Entities;

namespace LessonsHub.Application.Services.Executors;

public sealed record ExerciseRetryPayload(int LessonId, string Difficulty, string? Comment, string Review);

public sealed class ExerciseRetryExecutor : IJobExecutor
{
    private readonly IExerciseService _exercises;
    public ExerciseRetryExecutor(IExerciseService exercises) { _exercises = exercises; }

    public string Type => JobType.ExerciseRetry;

    public async Task<object?> ExecuteAsync(Job job, CancellationToken ct)
    {
        var payload = JsonSerializer.Deserialize<ExerciseRetryPayload>(job.PayloadJson)
                      ?? throw new InvalidOperationException("Empty payload for ExerciseRetry job.");

        var result = await _exercises.RetryAsync(payload.LessonId, payload.Difficulty, payload.Comment, payload.Review, ct);
        if (!result.IsSuccess)
            throw new ApplicationException(result.Message ?? $"Retry failed: {result.Error}");
        return result.Value;
    }
}
