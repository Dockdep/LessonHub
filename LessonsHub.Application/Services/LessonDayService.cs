using LessonsHub.Application.Abstractions;
using LessonsHub.Application.Abstractions.Repositories;
using LessonsHub.Application.Abstractions.Services;
using LessonsHub.Application.Models.Requests;
using LessonsHub.Application.Models.Responses;
using LessonsHub.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace LessonsHub.Application.Services;

public sealed class LessonDayService : ILessonDayService
{
    private readonly ILessonPlanRepository _plans;
    private readonly ILessonRepository _lessons;
    private readonly ILessonDayRepository _days;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<LessonDayService> _logger;

    public LessonDayService(
        ILessonPlanRepository plans,
        ILessonRepository lessons,
        ILessonDayRepository days,
        ICurrentUser currentUser,
        ILogger<LessonDayService> logger)
    {
        _plans = plans;
        _lessons = lessons;
        _days = days;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<ServiceResult<List<LessonPlanSummaryDto>>> GetUserPlansAsync(CancellationToken ct = default)
    {
        var plans = await _plans.GetOwnedWithLessonCountAsync(_currentUser.Id, ct);
        var dtos = plans
            .Select(lp => new LessonPlanSummaryDto
            {
                Id = lp.Id,
                Name = lp.Name,
                Topic = lp.Topic,
                Description = lp.Description,
                CreatedDate = lp.CreatedDate,
                LessonsCount = lp.Lessons.Count,
                IsOwner = true
            })
            .OrderByDescending(p => p.CreatedDate)
            .ToList();
        return ServiceResult<List<LessonPlanSummaryDto>>.Ok(dtos);
    }

    public async Task<ServiceResult<List<AvailableLessonDto>>> GetAvailableLessonsAsync(int planId, CancellationToken ct = default)
    {
        var plan = await _plans.GetOwnedWithLessonsAsync(planId, _currentUser.Id, ct);
        if (plan == null)
            return ServiceResult<List<AvailableLessonDto>>.NotFound("Lesson plan not found.");

        var dtos = plan.Lessons
            .OrderBy(l => l.LessonNumber)
            .Select(l => new AvailableLessonDto
            {
                Id = l.Id,
                LessonNumber = l.LessonNumber,
                Name = l.Name,
                ShortDescription = l.ShortDescription,
                LessonPlanId = plan.Id,
                LessonPlanName = plan.Name,
                IsAssigned = l.LessonDayId != null
            })
            .ToList();
        return ServiceResult<List<AvailableLessonDto>>.Ok(dtos);
    }

    public async Task<ServiceResult<List<LessonDayDto>>> GetLessonDaysByMonthAsync(int year, int month, CancellationToken ct = default)
    {
        var startDate = DateTime.SpecifyKind(new DateTime(year, month, 1), DateTimeKind.Utc);
        var endDate = startDate.AddMonths(1);

        var days = await _days.GetByMonthAsync(_currentUser.Id, startDate, endDate, ct);
        return ServiceResult<List<LessonDayDto>>.Ok(days.Select(ToDto).ToList());
    }

    public async Task<ServiceResult<LessonDayDto?>> GetLessonDayByDateAsync(DateTime date, CancellationToken ct = default)
    {
        var searchDate = DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);
        var day = await _days.GetByDateWithLessonsAsync(_currentUser.Id, searchDate, ct);
        return ServiceResult<LessonDayDto?>.Ok(day == null ? null : ToDto(day));
    }

    public async Task<ServiceResult> AssignLessonAsync(AssignLessonRequestDto request, CancellationToken ct = default)
    {
        var userId = _currentUser.Id;

        var lesson = await _lessons.GetWithPlanAsync(request.LessonId, ct);
        if (lesson == null || lesson.LessonPlan?.UserId != userId)
            return ServiceResult.NotFound("Lesson not found.");

        if (!DateTime.TryParse(request.Date, out var date))
            return ServiceResult.BadRequest("Invalid date format.");

        var utcDate = DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);
        var day = await _days.GetByDateAsync(userId, utcDate, ct);

        if (day == null)
        {
            day = new LessonDay
            {
                Date = utcDate,
                Name = request.DayName,
                ShortDescription = request.DayDescription,
                UserId = userId
            };
            _days.Add(day);
        }
        else
        {
            day.Name = request.DayName;
            day.ShortDescription = request.DayDescription;
        }

        // EF resolves the FK from the navigation when day.Id is still 0
        // (for the insert path), so a single SaveChanges suffices.
        lesson.LessonDay = day;
        await _days.SaveChangesAsync(ct);

        return new ServiceResult(ServiceErrorKind.None, "Lesson assigned successfully.");
    }

    public async Task<ServiceResult> UnassignLessonAsync(int lessonId, CancellationToken ct = default)
    {
        var userId = _currentUser.Id;

        var lesson = await _lessons.GetWithPlanAsync(lessonId, ct);
        if (lesson == null || lesson.LessonPlan?.UserId != userId)
            return ServiceResult.NotFound("Lesson not found.");

        var dayId = lesson.LessonDayId;
        lesson.LessonDayId = null;
        await _days.SaveChangesAsync(ct);

        if (dayId != null)
        {
            var day = await _days.GetByIdWithLessonsAsync(dayId.Value, ct);
            if (day != null && day.Lessons.Count == 0)
            {
                _days.Remove(day);
                await _days.SaveChangesAsync(ct);
            }
        }

        return new ServiceResult(ServiceErrorKind.None, "Lesson unassigned successfully.");
    }

    private static LessonDayDto ToDto(LessonDay day) => new()
    {
        Id = day.Id,
        Date = day.Date,
        Name = day.Name,
        ShortDescription = day.ShortDescription,
        Lessons = day.Lessons
            .Select(l => new AssignedLessonDto
            {
                Id = l.Id,
                LessonNumber = l.LessonNumber,
                Name = l.Name,
                ShortDescription = l.ShortDescription,
                LessonPlanId = l.LessonPlanId,
                LessonPlanName = l.LessonPlan!.Name,
                IsCompleted = l.IsCompleted
            })
            .ToList()
    };
}
