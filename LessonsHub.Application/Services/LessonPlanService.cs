using LessonsHub.Application.Abstractions;
using LessonsHub.Application.Abstractions.Repositories;
using LessonsHub.Application.Abstractions.Services;
using LessonsHub.Application.Interfaces;
using LessonsHub.Application.Models.Requests;
using LessonsHub.Application.Models.Responses;
using LessonsHub.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace LessonsHub.Application.Services;

public sealed class LessonPlanService : ILessonPlanService
{
    private readonly ILessonPlanRepository _plans;
    private readonly ILessonRepository _lessons;
    private readonly ILessonDayRepository _days;
    private readonly ILessonsAiApiClient _ai;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<LessonPlanService> _logger;

    public LessonPlanService(
        ILessonPlanRepository plans,
        ILessonRepository lessons,
        ILessonDayRepository days,
        ILessonsAiApiClient ai,
        ICurrentUser currentUser,
        ILogger<LessonPlanService> logger)
    {
        _plans = plans;
        _lessons = lessons;
        _days = days;
        _ai = ai;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<ServiceResult<LessonPlanDetailDto>> GetDetailAsync(int planId, CancellationToken ct = default)
    {
        var userId = _currentUser.Id;
        var plan = await _plans.GetForReadWithLessonsAsync(planId, userId, ct);
        if (plan == null)
            return ServiceResult<LessonPlanDetailDto>.NotFound("Lesson plan not found.");

        return ServiceResult<LessonPlanDetailDto>.Ok(ToDetailDto(plan, userId));
    }

    public async Task<ServiceResult<List<LessonPlanSummaryDto>>> GetSharedWithMeAsync(CancellationToken ct = default)
    {
        var plans = await _plans.GetSharedWithUserAsync(_currentUser.Id, ct);
        var dtos = plans
            .Select(lp => new LessonPlanSummaryDto
            {
                Id = lp.Id,
                Name = lp.Name,
                Topic = lp.Topic,
                Description = lp.Description,
                CreatedDate = lp.CreatedDate,
                LessonsCount = lp.Lessons.Count,
                IsOwner = false,
                OwnerName = lp.User?.Name
            })
            .OrderByDescending(p => p.CreatedDate)
            .ToList();
        return ServiceResult<List<LessonPlanSummaryDto>>.Ok(dtos);
    }

    public async Task<ServiceResult> DeleteAsync(int planId, CancellationToken ct = default)
    {
        var userId = _currentUser.Id;
        var plan = await _plans.GetOwnedWithLessonsAsync(planId, userId, ct);
        if (plan == null) return ServiceResult.NotFound("Lesson plan not found.");

        var affectedDayIds = plan.Lessons
            .Where(l => l.LessonDayId != null)
            .Select(l => l.LessonDayId!.Value)
            .Distinct()
            .ToList();

        _plans.Remove(plan);
        await _plans.SaveChangesAsync(ct);

        // Cascade has already removed the Lessons; clean up any orphaned LessonDays.
        if (affectedDayIds.Count > 0)
        {
            var emptyDays = await _days.GetEmptyAmongAsync(affectedDayIds, ct);
            if (emptyDays.Count > 0)
            {
                _days.RemoveRange(emptyDays);
                await _days.SaveChangesAsync(ct);
            }
        }

        return new ServiceResult(ServiceErrorKind.None, "Lesson plan deleted successfully.");
    }

    public async Task<ServiceResult<LessonPlanDetailDto>> UpdateAsync(int planId, UpdateLessonPlanRequestDto request, CancellationToken ct = default)
    {
        var userId = _currentUser.Id;
        var plan = await _plans.GetOwnedWithLessonsAsync(planId, userId, ct);
        if (plan == null)
            return ServiceResult<LessonPlanDetailDto>.NotFound("Lesson plan not found.");

        plan.Name = request.Name;
        plan.Topic = request.Topic;
        plan.Description = request.Description;
        plan.NativeLanguage = request.NativeLanguage;
        plan.LanguageToLearn = request.LanguageToLearn;
        plan.UseNativeLanguage = request.UseNativeLanguage;

        var incomingIds = request.Lessons
            .Where(l => l.Id.HasValue)
            .Select(l => l.Id!.Value)
            .ToHashSet();

        var toRemove = plan.Lessons.Where(l => !incomingIds.Contains(l.Id)).ToList();
        _lessons.RemoveRange(toRemove);

        foreach (var dto in request.Lessons)
        {
            if (dto.Id.HasValue)
            {
                var existing = plan.Lessons.FirstOrDefault(l => l.Id == dto.Id.Value);
                if (existing != null)
                {
                    existing.LessonNumber = dto.LessonNumber;
                    existing.Name = dto.Name;
                    existing.ShortDescription = dto.ShortDescription;
                    existing.LessonTopic = dto.LessonTopic;
                    existing.KeyPoints = dto.KeyPoints;
                }
            }
            else
            {
                _lessons.Add(new Lesson
                {
                    LessonNumber = dto.LessonNumber,
                    Name = dto.Name,
                    ShortDescription = dto.ShortDescription,
                    Content = string.Empty,
                    LessonType = plan.Lessons.FirstOrDefault()?.LessonType ?? string.Empty,
                    LessonPlanId = plan.Id,
                    LessonTopic = dto.LessonTopic,
                    KeyPoints = dto.KeyPoints,
                    LessonDayId = null
                });
            }
        }

        await _plans.SaveChangesAsync(ct);

        // Reload to project the post-save state.
        var updated = await _plans.GetOwnedWithLessonsAsync(planId, userId, ct);
        return ServiceResult<LessonPlanDetailDto>.Ok(ToDetailDto(updated!, userId));
    }

    public Task<ServiceResult> ValidateGenerateAsync(LessonPlanRequestDto request, CancellationToken ct = default)
    {
        if (request is null)
            return Task.FromResult(ServiceResult.BadRequest("Request body is required."));
        if (string.IsNullOrWhiteSpace(request.PlanName) || string.IsNullOrWhiteSpace(request.Topic))
            return Task.FromResult(ServiceResult.BadRequest("Invalid input. Please provide plan name and topic."));
        return Task.FromResult(ServiceResult.Ok());
    }

    public async Task<ServiceResult<LessonPlanResponseDto>> GenerateAsync(LessonPlanRequestDto request, CancellationToken ct = default)
    {
        var validation = await ValidateGenerateAsync(request, ct);
        if (!validation.IsSuccess)
            return new ServiceResult<LessonPlanResponseDto>(default, validation.Error, validation.Message);

        try
        {
            var aiRequest = new AiLessonPlanRequest
            {
                LessonType = request.LessonType,
                Topic = request.Topic,
                NumberOfLessons = request.NumberOfDays,
                Description = request.Description,
                Language = request.NativeLanguage,
                NativeLanguage = request.NativeLanguage,
                LanguageToLearn = request.LanguageToLearn,
                UseNativeLanguage = request.UseNativeLanguage,
                BypassDocCache = request.BypassDocCache,
                DocumentId = request.DocumentId?.ToString(),
            };

            var aiResponse = await _ai.GenerateLessonPlanAsync(aiRequest);
            if (aiResponse == null)
                return ServiceResult<LessonPlanResponseDto>.Internal("Failed to get lesson plan from AI API.");

            return ServiceResult<LessonPlanResponseDto>.Ok(new LessonPlanResponseDto
            {
                PlanName = request.PlanName,
                Topic = aiResponse.Topic,
                Lessons = aiResponse.Lessons.Select(l => new GeneratedLessonDto
                {
                    LessonNumber = l.LessonNumber,
                    Name = l.Name,
                    ShortDescription = l.ShortDescription,
                    LessonTopic = l.LessonTopic,
                    KeyPoints = l.KeyPoints
                }).ToList()
            });
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "Timeout generating lesson plan");
            return ServiceResult<LessonPlanResponseDto>.Timeout();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating lesson plan");
            return ServiceResult<LessonPlanResponseDto>.Internal($"An error occurred while generating the lesson plan. {ex.Message}");
        }
    }

    public async Task<ServiceResult<SaveLessonPlanResponseDto>> SaveAsync(SaveLessonPlanRequestDto request, CancellationToken ct = default)
    {
        if (request?.LessonPlan?.Lessons == null || !request.LessonPlan.Lessons.Any())
            return ServiceResult<SaveLessonPlanResponseDto>.BadRequest("Invalid lesson plan data.");

        var userId = _currentUser.Id;

        var plan = new LessonPlan
        {
            Name = request.LessonPlan.PlanName,
            Topic = request.LessonPlan.Topic,
            Description = request.Description ?? string.Empty,
            NativeLanguage = request.NativeLanguage,
            LanguageToLearn = request.LanguageToLearn,
            UseNativeLanguage = request.UseNativeLanguage,
            CreatedDate = DateTime.UtcNow,
            UserId = userId,
            DocumentId = request.DocumentId,
        };
        _plans.Add(plan);
        await _plans.SaveChangesAsync(ct);

        foreach (var dto in request.LessonPlan.Lessons)
        {
            _lessons.Add(new Lesson
            {
                LessonNumber = dto.LessonNumber,
                Name = dto.Name,
                ShortDescription = dto.ShortDescription,
                Content = string.Empty,
                LessonType = request.LessonType ?? string.Empty,
                LessonPlanId = plan.Id,
                LessonTopic = dto.LessonTopic,
                KeyPoints = dto.KeyPoints ?? new(),
                LessonDayId = null
            });
        }
        await _plans.SaveChangesAsync(ct);

        return ServiceResult<SaveLessonPlanResponseDto>.Ok(new SaveLessonPlanResponseDto
        {
            Message = "Lesson plan saved successfully.",
            LessonPlanId = plan.Id
        });
    }

    private static LessonPlanDetailDto ToDetailDto(LessonPlan plan, int userId) => new()
    {
        Id = plan.Id,
        Name = plan.Name,
        Topic = plan.Topic,
        Description = plan.Description,
        NativeLanguage = plan.NativeLanguage,
        LanguageToLearn = plan.LanguageToLearn,
        UseNativeLanguage = plan.UseNativeLanguage,
        CreatedDate = plan.CreatedDate,
        IsOwner = plan.UserId == userId,
        OwnerName = plan.User?.Name,
        Lessons = plan.Lessons
            .OrderBy(l => l.LessonNumber)
            .Select(l => new PlanLessonDto
            {
                Id = l.Id,
                LessonNumber = l.LessonNumber,
                Name = l.Name,
                ShortDescription = l.ShortDescription,
                LessonTopic = l.LessonTopic,
                IsCompleted = l.IsCompleted
            })
            .ToList()
    };
}
