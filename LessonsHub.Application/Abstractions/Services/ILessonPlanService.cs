using LessonsHub.Application.Models.Requests;
using LessonsHub.Application.Models.Responses;

namespace LessonsHub.Application.Abstractions.Services;

public interface ILessonPlanService
{
    Task<ServiceResult<LessonPlanDetailDto>> GetDetailAsync(int planId, CancellationToken ct = default);
    Task<ServiceResult<List<LessonPlanSummaryDto>>> GetSharedWithMeAsync(CancellationToken ct = default);
    Task<ServiceResult> DeleteAsync(int planId, CancellationToken ct = default);
    Task<ServiceResult<LessonPlanDetailDto>> UpdateAsync(int planId, UpdateLessonPlanRequestDto request, CancellationToken ct = default);
    Task<ServiceResult<LessonPlanResponseDto>> GenerateAsync(LessonPlanRequestDto request, CancellationToken ct = default);
    Task<ServiceResult<SaveLessonPlanResponseDto>> SaveAsync(SaveLessonPlanRequestDto request, CancellationToken ct = default);
}

public class SaveLessonPlanResponseDto
{
    public string Message { get; set; } = string.Empty;
    public int LessonPlanId { get; set; }
}
