using LessonsHub.Application.Models.Responses;

namespace LessonsHub.Application.Abstractions.Services;

public interface ILessonPlanShareService
{
    Task<ServiceResult<List<LessonPlanShareDto>>> GetSharesAsync(int planId, CancellationToken ct = default);
    Task<ServiceResult<LessonPlanShareDto>> AddShareAsync(int planId, string? email, CancellationToken ct = default);
    Task<ServiceResult> RemoveShareAsync(int planId, int shareUserId, CancellationToken ct = default);
}
