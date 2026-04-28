using System.Text.Json;
using LessonsHub.Application.Abstractions.Services;
using LessonsHub.Application.Models.Jobs;
using LessonsHub.Domain.Entities;

namespace LessonsHub.Application.Services.Executors;

public sealed record ExerciseGeneratePayload(int LessonId, string Difficulty, string? Comment);

public sealed class ExerciseGenerateExecutor : IJobExecutor
{
    private readonly IExerciseService _exercises;
    public ExerciseGenerateExecutor(IExerciseService exercises) { _exercises = exercises; }

    public string Type => JobType.ExerciseGenerate;

    public async Task<object?> ExecuteAsync(Job job, CancellationToken ct)
    {
        var payload = JsonSerializer.Deserialize<ExerciseGeneratePayload>(job.PayloadJson)
                      ?? throw new InvalidOperationException("Empty payload for ExerciseGenerate job.");

        var result = await _exercises.GenerateAsync(payload.LessonId, payload.Difficulty, payload.Comment, ct);
        if (!result.IsSuccess)
            throw new ApplicationException(result.Message ?? $"Generation failed: {result.Error}");
        return result.Value;
    }
}
