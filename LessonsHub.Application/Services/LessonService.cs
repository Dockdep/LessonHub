using LessonsHub.Application.Abstractions;
using LessonsHub.Application.Abstractions.Repositories;
using LessonsHub.Application.Abstractions.Services;
using LessonsHub.Application.Interfaces;
using LessonsHub.Application.Mappers;
using LessonsHub.Application.Models.Requests;
using LessonsHub.Application.Models.Responses;
using LessonsHub.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace LessonsHub.Application.Services;

public sealed class LessonService : ILessonService
{
    private readonly ILessonRepository _lessons;
    private readonly ILessonPlanRepository _plans;
    private readonly ILessonsAiApiClient _ai;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<LessonService> _logger;

    public LessonService(
        ILessonRepository lessons,
        ILessonPlanRepository plans,
        ILessonsAiApiClient ai,
        ICurrentUser currentUser,
        ILogger<LessonService> logger)
    {
        _lessons = lessons;
        _plans = plans;
        _ai = ai;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<ServiceResult<LessonDetailDto>> GetDetailAsync(int lessonId, CancellationToken ct = default)
    {
        var userId = _currentUser.Id;
        var lesson = await _lessons.GetWithDetailsAsync(lessonId, userId, ct);
        if (lesson == null || !await _plans.HasReadAccessAsync(lesson.LessonPlanId, userId, ct))
            return ServiceResult<LessonDetailDto>.NotFound();

        // Read is now pure read. When Content is empty the frontend triggers
        // POST /api/lesson/{id}/generate-content explicitly — that endpoint
        // enqueues a job and the result streams in via SignalR.
        return ServiceResult<LessonDetailDto>.Ok(lesson.ToDetailDto(userId));
    }

    public async Task<ServiceResult> ValidateGenerateContentAsync(int lessonId, CancellationToken ct = default)
    {
        var userId = _currentUser.Id;
        var lesson = await _lessons.GetWithDetailsAsync(lessonId, userId, ct);
        if (lesson == null || !await _plans.HasReadAccessAsync(lesson.LessonPlanId, userId, ct))
            return ServiceResult.NotFound();
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult<LessonDetailDto>> GenerateContentAsync(int lessonId, CancellationToken ct = default)
    {
        var validation = await ValidateGenerateContentAsync(lessonId, ct);
        if (!validation.IsSuccess)
            return new ServiceResult<LessonDetailDto>(default, validation.Error, validation.Message);

        var userId = _currentUser.Id;
        var lesson = (await _lessons.GetWithDetailsAsync(lessonId, userId, ct))!;

        // No-op when content is already there — keeps the executor idempotent
        // for double-fires from idempotency-key dedupe.
        if (!string.IsNullOrWhiteSpace(lesson.Content))
            return ServiceResult<LessonDetailDto>.Ok(lesson.ToDetailDto(userId));

        try
        {
            _logger.LogInformation("Generating content for Lesson {Id}...", lessonId);
            var contentRequest = await BuildContentRequestAsync(lesson, bypassDocCache: false, ct);
            var contentResponse = await _ai.GenerateLessonContentAsync(contentRequest);
            if (contentResponse == null || string.IsNullOrWhiteSpace(contentResponse.Content))
                return ServiceResult<LessonDetailDto>.Internal("Failed to generate lesson content.");

            lesson.Content = contentResponse.Content;
            await _lessons.SaveChangesAsync(ct);
            return ServiceResult<LessonDetailDto>.Ok(lesson.ToDetailDto(userId));
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "Timeout generating content for Lesson {Id}", lessonId);
            return ServiceResult<LessonDetailDto>.Timeout();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating content for Lesson {Id}", lessonId);
            return ServiceResult<LessonDetailDto>.Internal($"An error occurred while generating the lesson. {ex.Message}");
        }
    }

    public async Task<ServiceResult<LessonDetailDto>> UpdateAsync(int lessonId, UpdateLessonInfoDto request, CancellationToken ct = default)
    {
        var userId = _currentUser.Id;
        var lesson = await _lessons.GetWithDetailsAsync(lessonId, userId, ct);
        if (lesson == null || lesson.LessonPlan?.UserId != userId)
            return ServiceResult<LessonDetailDto>.NotFound();

        lesson.Name = request.Name;
        lesson.ShortDescription = request.ShortDescription;
        lesson.LessonTopic = request.LessonTopic;
        lesson.KeyPoints = request.KeyPoints;
        await _lessons.SaveChangesAsync(ct);

        _logger.LogInformation("Lesson {Id} info updated", lessonId);
        return ServiceResult<LessonDetailDto>.Ok(lesson.ToDetailDto(userId));
    }

    public async Task<ServiceResult> ValidateRegenerateContentAsync(int lessonId, CancellationToken ct = default)
    {
        var userId = _currentUser.Id;
        var lesson = await _lessons.GetWithDetailsAsync(lessonId, userId, ct);
        // Regeneration overwrites shared state — owner-only.
        if (lesson == null || lesson.LessonPlan?.UserId != userId)
            return ServiceResult.NotFound();
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult<LessonDetailDto>> RegenerateContentAsync(int lessonId, bool bypassDocCache, CancellationToken ct = default)
    {
        var validation = await ValidateRegenerateContentAsync(lessonId, ct);
        if (!validation.IsSuccess)
            return new ServiceResult<LessonDetailDto>(default, validation.Error, validation.Message);

        var userId = _currentUser.Id;
        var lesson = (await _lessons.GetWithDetailsAsync(lessonId, userId, ct))!;

        try
        {
            _logger.LogInformation("Regenerating content for Lesson {Id}...", lessonId);
            var contentRequest = await BuildContentRequestAsync(lesson, bypassDocCache, ct);
            var contentResponse = await _ai.GenerateLessonContentAsync(contentRequest);

            if (contentResponse == null || string.IsNullOrWhiteSpace(contentResponse.Content))
                return ServiceResult<LessonDetailDto>.Internal("Failed to regenerate lesson content.");

            lesson.Content = contentResponse.Content;
            await _lessons.SaveChangesAsync(ct);

            _logger.LogInformation("Content regenerated for Lesson {Id}", lessonId);
            return ServiceResult<LessonDetailDto>.Ok(lesson.ToDetailDto(userId));
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "Timeout regenerating content for Lesson {Id}", lessonId);
            return ServiceResult<LessonDetailDto>.Timeout();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error regenerating content for Lesson {Id}", lessonId);
            return ServiceResult<LessonDetailDto>.Internal($"An error occurred while regenerating the lesson. {ex.Message}");
        }
    }

    public async Task<ServiceResult<LessonDetailDto>> ToggleCompleteAsync(int lessonId, CancellationToken ct = default)
    {
        var userId = _currentUser.Id;
        var lesson = await _lessons.GetWithDetailsAsync(lessonId, userId, ct);
        // Completion is shared state — owner-only.
        if (lesson == null || lesson.LessonPlan?.UserId != userId)
            return ServiceResult<LessonDetailDto>.NotFound();

        lesson.IsCompleted = !lesson.IsCompleted;
        lesson.CompletedAt = lesson.IsCompleted ? DateTime.UtcNow : null;
        await _lessons.SaveChangesAsync(ct);

        _logger.LogInformation("Lesson {Id} marked as {Status}", lessonId, lesson.IsCompleted ? "completed" : "incomplete");
        return ServiceResult<LessonDetailDto>.Ok(lesson.ToDetailDto(userId));
    }

    public async Task<ServiceResult<SiblingLessonsDto>> GetSiblingLessonIdsAsync(int lessonId, CancellationToken ct = default)
    {
        var userId = _currentUser.Id;
        var current = await _lessons.GetByIdAsync(lessonId, ct);
        if (current == null || !await _plans.HasReadAccessAsync(current.LessonPlanId, userId, ct))
            return ServiceResult<SiblingLessonsDto>.NotFound();

        var (prev, next) = await _lessons.GetAdjacentAsync(current.LessonPlanId, current.LessonNumber, ct);
        return ServiceResult<SiblingLessonsDto>.Ok(new SiblingLessonsDto
        {
            PrevLessonId = prev?.Id,
            NextLessonId = next?.Id
        });
    }

    private async Task<AiLessonContentRequest> BuildContentRequestAsync(Lesson lesson, bool bypassDocCache, CancellationToken ct)
    {
        var planTopic = lesson.LessonPlan?.Topic ?? lesson.LessonPlan?.Name ?? "General Course";
        var planDescription = lesson.LessonPlan?.Description ?? "";

        var (prev, next) = await _lessons.GetAdjacentAsync(lesson.LessonPlanId, lesson.LessonNumber, ct);
        return new AiLessonContentRequest
        {
            Topic = planTopic,
            LessonType = lesson.LessonType,
            LessonTopic = lesson.LessonTopic,
            KeyPoints = lesson.KeyPoints ?? new(),
            PlanDescription = planDescription,
            LessonNumber = lesson.LessonNumber,
            LessonName = lesson.Name,
            LessonDescription = lesson.ShortDescription ?? "",
            Language = lesson.LessonPlan?.NativeLanguage,
            NativeLanguage = lesson.LessonPlan?.NativeLanguage,
            LanguageToLearn = lesson.LessonPlan?.LanguageToLearn,
            UseNativeLanguage = lesson.LessonPlan?.UseNativeLanguage ?? true,
            PreviousLesson = prev == null ? null : new AdjacentLesson { Name = prev.Name, Description = prev.ShortDescription ?? "" },
            NextLesson = next == null ? null : new AdjacentLesson { Name = next.Name, Description = next.ShortDescription ?? "" },
            BypassDocCache = bypassDocCache,
            DocumentId = lesson.LessonPlan?.DocumentId?.ToString(),
        };
    }
}
