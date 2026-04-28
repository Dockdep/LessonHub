using LessonsHub.Application.Models.Requests;
using LessonsHub.Application.Models.Responses;

namespace LessonsHub.Application.Abstractions.Services;

public interface ILessonDayService
{
    Task<ServiceResult<List<LessonPlanSummaryDto>>> GetUserPlansAsync(CancellationToken ct = default);
    Task<ServiceResult<List<AvailableLessonDto>>> GetAvailableLessonsAsync(int planId, CancellationToken ct = default);
    Task<ServiceResult<List<LessonDayDto>>> GetLessonDaysByMonthAsync(int year, int month, CancellationToken ct = default);
    Task<ServiceResult<LessonDayDto?>> GetLessonDayByDateAsync(DateTime date, CancellationToken ct = default);
    Task<ServiceResult> AssignLessonAsync(AssignLessonRequestDto request, CancellationToken ct = default);
    Task<ServiceResult> UnassignLessonAsync(int lessonId, CancellationToken ct = default);
}
