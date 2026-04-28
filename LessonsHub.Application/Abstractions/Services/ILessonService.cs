using LessonsHub.Application.Models.Requests;
using LessonsHub.Application.Models.Responses;

namespace LessonsHub.Application.Abstractions.Services;

public interface ILessonService
{
    Task<ServiceResult<LessonDetailDto>> GetDetailAsync(int lessonId, CancellationToken ct = default);
    Task<ServiceResult<LessonDetailDto>> UpdateAsync(int lessonId, UpdateLessonInfoDto request, CancellationToken ct = default);
    Task<ServiceResult<LessonDetailDto>> RegenerateContentAsync(int lessonId, bool bypassDocCache, CancellationToken ct = default);
    Task<ServiceResult<LessonDetailDto>> ToggleCompleteAsync(int lessonId, CancellationToken ct = default);
    Task<ServiceResult<SiblingLessonsDto>> GetSiblingLessonIdsAsync(int lessonId, CancellationToken ct = default);
}
