using LessonsHub.Application.Abstractions;
using LessonsHub.Application.Models.Requests;
using LessonsHub.Application.Models.Responses;

namespace LessonsHub.Application.Abstractions.Services;

public interface ILessonPlanService
{
    Task<ServiceResult<LessonPlanDetailDto>> GetDetailAsync(int planId, CancellationToken ct = default);
    Task<ServiceResult<List<LessonPlanSummaryDto>>> GetSharedWithMeAsync(CancellationToken ct = default);
    Task<ServiceResult> DeleteAsync(int planId, CancellationToken ct = default);
    Task<ServiceResult<LessonPlanDetailDto>> UpdateAsync(int planId, UpdateLessonPlanRequestDto request, CancellationToken ct = default);

    /// <summary>
    /// Cheap synchronous pre-check called by the controller before enqueueing
    /// a generate job — returns 4xx immediately if the request is malformed,
    /// the user lacks access, or prerequisites aren't met. Same checks run at
    /// the top of GenerateAsync, so calling Generate without Validate first
    /// is also safe (it'll just do the work twice).
    /// </summary>
    Task<ServiceResult> ValidateGenerateAsync(LessonPlanRequestDto request, CancellationToken ct = default);

    Task<ServiceResult<LessonPlanResponseDto>> GenerateAsync(LessonPlanRequestDto request, CancellationToken ct = default);
    Task<ServiceResult<SaveLessonPlanResponseDto>> SaveAsync(SaveLessonPlanRequestDto request, CancellationToken ct = default);
}

public class SaveLessonPlanResponseDto
{
    public string Message { get; set; } = string.Empty;
    public int LessonPlanId { get; set; }
}
