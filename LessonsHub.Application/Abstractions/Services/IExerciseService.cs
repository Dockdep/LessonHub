using LessonsHub.Application.Abstractions;
using LessonsHub.Application.Models.Responses;

namespace LessonsHub.Application.Abstractions.Services;

public interface IExerciseService
{
    Task<ServiceResult> ValidateGenerateAsync(int lessonId, string difficulty, string? comment, CancellationToken ct = default);
    Task<ServiceResult<ExerciseDto>> GenerateAsync(int lessonId, string difficulty, string? comment, CancellationToken ct = default);

    Task<ServiceResult> ValidateRetryAsync(int lessonId, string difficulty, string? comment, string review, CancellationToken ct = default);
    Task<ServiceResult<ExerciseDto>> RetryAsync(int lessonId, string difficulty, string? comment, string review, CancellationToken ct = default);

    Task<ServiceResult> ValidateCheckAnswerAsync(int exerciseId, string? answer, CancellationToken ct = default);
    Task<ServiceResult<ExerciseAnswerDto>> CheckAnswerAsync(int exerciseId, string? answer, CancellationToken ct = default);
}
