using LessonsHub.Application.Abstractions;
using LessonsHub.Application.Models.Requests;
using LessonsHub.Application.Models.Responses;

namespace LessonsHub.Application.Abstractions.Services;

public interface ILessonService
{
    Task<ServiceResult<LessonDetailDto>> GetDetailAsync(int lessonId, CancellationToken ct = default);
    Task<ServiceResult<LessonDetailDto>> UpdateAsync(int lessonId, UpdateLessonInfoDto request, CancellationToken ct = default);

    /// <summary>Read-access caller (any sharer) — content gen on demand for a lesson with empty Content.</summary>
    Task<ServiceResult> ValidateGenerateContentAsync(int lessonId, CancellationToken ct = default);
    Task<ServiceResult<LessonDetailDto>> GenerateContentAsync(int lessonId, CancellationToken ct = default);

    /// <summary>Owner-only — overwrites existing Content.</summary>
    Task<ServiceResult> ValidateRegenerateContentAsync(int lessonId, CancellationToken ct = default);
    Task<ServiceResult<LessonDetailDto>> RegenerateContentAsync(int lessonId, bool bypassDocCache, CancellationToken ct = default);

    Task<ServiceResult<LessonDetailDto>> ToggleCompleteAsync(int lessonId, CancellationToken ct = default);
    Task<ServiceResult<SiblingLessonsDto>> GetSiblingLessonIdsAsync(int lessonId, CancellationToken ct = default);
}
