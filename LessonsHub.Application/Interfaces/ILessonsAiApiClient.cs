using LessonsHub.Application.Models.Requests;
using LessonsHub.Application.Models.Responses;

namespace LessonsHub.Application.Interfaces;

public interface ILessonsAiApiClient
{
    Task<AiLessonPlanResponse?> GenerateLessonPlanAsync(AiLessonPlanRequest request);
    Task<AiLessonContentResponse?> GenerateLessonContentAsync(AiLessonContentRequest request);
    Task<AiLessonExerciseResponse?> GenerateLessonExerciseAsync(AiLessonExerciseRequest request);
    Task<AiLessonExerciseResponse?> RetryLessonExerciseAsync(AiExerciseRetryRequest request);
    Task<AiExerciseReviewResponse?> CheckExerciseReviewAsync(AiExerciseReviewRequest request);
    Task<AiLessonResourcesResponse?> GenerateLessonResourcesAsync(AiLessonResourcesRequest request);
    Task<bool> HealthCheckAsync();
}
