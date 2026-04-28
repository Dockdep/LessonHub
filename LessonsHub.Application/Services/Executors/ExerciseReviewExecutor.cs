using System.Text.Json;
using LessonsHub.Application.Abstractions.Services;
using LessonsHub.Application.Models.Jobs;
using LessonsHub.Domain.Entities;

namespace LessonsHub.Application.Services.Executors;

public sealed record ExerciseReviewPayload(int ExerciseId, string Answer);

public sealed class ExerciseReviewExecutor : IJobExecutor
{
    private readonly IExerciseService _exercises;
    public ExerciseReviewExecutor(IExerciseService exercises) { _exercises = exercises; }

    public string Type => JobType.ExerciseReview;

    public async Task<object?> ExecuteAsync(Job job, CancellationToken ct)
    {
        var payload = JsonSerializer.Deserialize<ExerciseReviewPayload>(job.PayloadJson)
                      ?? throw new InvalidOperationException("Empty payload for ExerciseReview job.");

        var result = await _exercises.CheckAnswerAsync(payload.ExerciseId, payload.Answer, ct);
        if (!result.IsSuccess)
            throw new ApplicationException(result.Message ?? $"Review failed: {result.Error}");
        return result.Value;
    }
}
