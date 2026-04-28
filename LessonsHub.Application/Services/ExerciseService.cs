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

public sealed class ExerciseService : IExerciseService
{
    private readonly ILessonRepository _lessons;
    private readonly ILessonPlanRepository _plans;
    private readonly IExerciseRepository _exercises;
    private readonly IExerciseAnswerRepository _answers;
    private readonly ILessonsAiApiClient _ai;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<ExerciseService> _logger;

    public ExerciseService(
        ILessonRepository lessons,
        ILessonPlanRepository plans,
        IExerciseRepository exercises,
        IExerciseAnswerRepository answers,
        ILessonsAiApiClient ai,
        ICurrentUser currentUser,
        ILogger<ExerciseService> logger)
    {
        _lessons = lessons;
        _plans = plans;
        _exercises = exercises;
        _answers = answers;
        _ai = ai;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<ServiceResult> ValidateGenerateAsync(int lessonId, string difficulty, string? comment, CancellationToken ct = default)
    {
        var userId = _currentUser.Id;
        var lesson = await _lessons.GetWithPlanAsync(lessonId, ct);
        if (lesson == null || !await _plans.HasReadAccessAsync(lesson.LessonPlanId, userId, ct))
            return ServiceResult.NotFound();
        if (string.IsNullOrWhiteSpace(lesson.Content))
            return ServiceResult.BadRequest("Lesson content must be generated first.");
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult<ExerciseDto>> GenerateAsync(int lessonId, string difficulty, string? comment, CancellationToken ct = default)
    {
        var validation = await ValidateGenerateAsync(lessonId, difficulty, comment, ct);
        if (!validation.IsSuccess)
            return new ServiceResult<ExerciseDto>(default, validation.Error, validation.Message);

        var userId = _currentUser.Id;
        var lesson = (await _lessons.GetWithPlanAsync(lessonId, ct))!;

        try
        {
            var (prev, next) = await _lessons.GetAdjacentAsync(lesson.LessonPlanId, lesson.LessonNumber, ct);
            var exerciseRequest = new AiLessonExerciseRequest
            {
                LessonType = lesson.LessonType,
                LessonTopic = lesson.LessonTopic,
                LessonNumber = lesson.LessonNumber,
                LessonName = lesson.Name,
                LessonDescription = lesson.ShortDescription ?? "",
                KeyPoints = lesson.KeyPoints ?? new(),
                Difficulty = difficulty,
                Comment = comment,
                NativeLanguage = lesson.LessonPlan?.NativeLanguage,
                LanguageToLearn = lesson.LessonPlan?.LanguageToLearn,
                UseNativeLanguage = lesson.LessonPlan?.UseNativeLanguage ?? true,
                PreviousLesson = ToAdjacent(prev),
                NextLesson = ToAdjacent(next),
                DocumentId = lesson.LessonPlan?.DocumentId?.ToString()
            };

            var aiResponse = await _ai.GenerateLessonExerciseAsync(exerciseRequest);
            if (aiResponse == null || string.IsNullOrWhiteSpace(aiResponse.Exercise))
                return ServiceResult<ExerciseDto>.Internal("Failed to generate exercise from AI API.");

            var exercise = new Exercise
            {
                ExerciseText = aiResponse.Exercise,
                Difficulty = difficulty,
                LessonId = lesson.Id,
                UserId = userId
            };
            _exercises.Add(exercise);
            await _exercises.SaveChangesAsync(ct);

            _logger.LogInformation("Exercise generated and saved for Lesson {Id}", lessonId);
            return ServiceResult<ExerciseDto>.Ok(exercise.ToDto());
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "Timeout generating exercise for Lesson {Id}", lessonId);
            return ServiceResult<ExerciseDto>.Timeout();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating exercise for Lesson {Id}", lessonId);
            return ServiceResult<ExerciseDto>.Internal($"An error occurred while generating the exercise. {ex.Message}");
        }
    }

    public async Task<ServiceResult> ValidateRetryAsync(int lessonId, string difficulty, string? comment, string review, CancellationToken ct = default)
    {
        var userId = _currentUser.Id;
        var lesson = await _lessons.GetWithPlanAsync(lessonId, ct);
        if (lesson == null || !await _plans.HasReadAccessAsync(lesson.LessonPlanId, userId, ct))
            return ServiceResult.NotFound();
        if (string.IsNullOrWhiteSpace(lesson.Content))
            return ServiceResult.BadRequest("Lesson content must be generated first.");
        if (string.IsNullOrWhiteSpace(review))
            return ServiceResult.BadRequest("Review text is required for retry.");
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult<ExerciseDto>> RetryAsync(int lessonId, string difficulty, string? comment, string review, CancellationToken ct = default)
    {
        var validation = await ValidateRetryAsync(lessonId, difficulty, comment, review, ct);
        if (!validation.IsSuccess)
            return new ServiceResult<ExerciseDto>(default, validation.Error, validation.Message);

        var userId = _currentUser.Id;
        var lesson = (await _lessons.GetWithPlanAsync(lessonId, ct))!;

        try
        {
            var (prev, next) = await _lessons.GetAdjacentAsync(lesson.LessonPlanId, lesson.LessonNumber, ct);
            var retryRequest = new AiExerciseRetryRequest
            {
                LessonType = lesson.LessonType,
                LessonTopic = lesson.LessonTopic,
                LessonNumber = lesson.LessonNumber,
                LessonName = lesson.Name,
                LessonDescription = lesson.ShortDescription ?? "",
                KeyPoints = lesson.KeyPoints ?? new(),
                Difficulty = difficulty,
                Review = review,
                Comment = comment,
                NativeLanguage = lesson.LessonPlan?.NativeLanguage,
                LanguageToLearn = lesson.LessonPlan?.LanguageToLearn,
                UseNativeLanguage = lesson.LessonPlan?.UseNativeLanguage ?? true,
                PreviousLesson = ToAdjacent(prev),
                NextLesson = ToAdjacent(next),
                DocumentId = lesson.LessonPlan?.DocumentId?.ToString()
            };

            var aiResponse = await _ai.RetryLessonExerciseAsync(retryRequest);
            if (aiResponse == null || string.IsNullOrWhiteSpace(aiResponse.Exercise))
                return ServiceResult<ExerciseDto>.Internal("Failed to generate exercise from AI API.");

            var exercise = new Exercise
            {
                ExerciseText = aiResponse.Exercise,
                Difficulty = difficulty,
                LessonId = lesson.Id,
                UserId = userId
            };
            _exercises.Add(exercise);
            await _exercises.SaveChangesAsync(ct);

            _logger.LogInformation("Retry exercise generated and saved for Lesson {Id}", lessonId);
            return ServiceResult<ExerciseDto>.Ok(exercise.ToDto());
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "Timeout retrying exercise for Lesson {Id}", lessonId);
            return ServiceResult<ExerciseDto>.Timeout();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrying exercise for Lesson {Id}", lessonId);
            return ServiceResult<ExerciseDto>.Internal($"An error occurred while generating the exercise. {ex.Message}");
        }
    }

    public async Task<ServiceResult> ValidateCheckAnswerAsync(int exerciseId, string? answer, CancellationToken ct = default)
    {
        var userId = _currentUser.Id;
        var exercise = await _exercises.GetForUserWithLessonAsync(exerciseId, userId, ct);
        if (exercise == null) return ServiceResult.NotFound();
        if (string.IsNullOrWhiteSpace(answer))
            return ServiceResult.BadRequest("Answer cannot be empty.");
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult<ExerciseAnswerDto>> CheckAnswerAsync(int exerciseId, string? answer, CancellationToken ct = default)
    {
        var validation = await ValidateCheckAnswerAsync(exerciseId, answer, ct);
        if (!validation.IsSuccess)
            return new ServiceResult<ExerciseAnswerDto>(default, validation.Error, validation.Message);

        var userId = _currentUser.Id;
        var exercise = (await _exercises.GetForUserWithLessonAsync(exerciseId, userId, ct))!;

        try
        {
            var reviewRequest = new AiExerciseReviewRequest
            {
                LessonType = exercise.Lesson.LessonType,
                LessonContent = exercise.Lesson.Content,
                ExerciseContent = exercise.ExerciseText,
                Difficulty = exercise.Difficulty,
                Answer = answer,
                Language = exercise.Lesson.LessonPlan?.NativeLanguage,
                NativeLanguage = exercise.Lesson.LessonPlan?.NativeLanguage,
                LanguageToLearn = exercise.Lesson.LessonPlan?.LanguageToLearn,
                UseNativeLanguage = exercise.Lesson.LessonPlan?.UseNativeLanguage ?? true,
            };

            var aiResponse = await _ai.CheckExerciseReviewAsync(reviewRequest);
            if (aiResponse == null)
                return ServiceResult<ExerciseAnswerDto>.Internal("Failed to get review from AI API.");

            var answerEntity = new ExerciseAnswer
            {
                UserResponse = answer,
                SubmittedAt = DateTime.UtcNow,
                AccuracyLevel = aiResponse.AccuracyLevel,
                ReviewText = aiResponse.ExamReview,
                ExerciseId = exerciseId
            };
            _answers.Add(answerEntity);
            await _answers.SaveChangesAsync(ct);

            _logger.LogInformation("Exercise review saved for Exercise {ExerciseId}", exerciseId);
            return ServiceResult<ExerciseAnswerDto>.Ok(answerEntity.ToDto());
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "Timeout checking exercise review for Exercise {ExerciseId}", exerciseId);
            return ServiceResult<ExerciseAnswerDto>.Timeout();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking exercise review for Exercise {ExerciseId}", exerciseId);
            return ServiceResult<ExerciseAnswerDto>.Internal($"An error occurred while checking the exercise. {ex.Message}");
        }
    }

    private static AdjacentLesson? ToAdjacent(Lesson? l) =>
        l == null ? null : new AdjacentLesson { Name = l.Name, Description = l.ShortDescription ?? "" };
}
