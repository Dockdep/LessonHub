using System.Net.Http.Json;
using LessonsHub.Application.Interfaces;
using LessonsHub.Application.Models.Requests;
using LessonsHub.Application.Models.Responses;
using Microsoft.Extensions.Logging;

namespace LessonsHub.Infrastructure.Services;

/// <summary>
/// HTTP client for the Python AI service. Single concern: serialise request,
/// inject the user's API key (via <see cref="IUserApiKeyProvider"/>), POST,
/// deserialise, and forward usage telemetry to <see cref="IAiCostLogger"/>.
/// </summary>
public class LessonsAiApiClient : ILessonsAiApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<LessonsAiApiClient> _logger;
    private readonly IUserApiKeyProvider _keyProvider;
    private readonly IAiCostLogger _costLogger;

    public LessonsAiApiClient(
        HttpClient httpClient,
        ILogger<LessonsAiApiClient> logger,
        IUserApiKeyProvider keyProvider,
        IAiCostLogger costLogger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _keyProvider = keyProvider;
        _costLogger = costLogger;
    }

    public Task<AiLessonPlanResponse?> GenerateLessonPlanAsync(AiLessonPlanRequest request)
        => SendAsync<AiLessonPlanRequest, AiLessonPlanResponse>("/api/lesson-plan/generate", request);

    public Task<AiLessonContentResponse?> GenerateLessonContentAsync(AiLessonContentRequest request)
        => SendAsync<AiLessonContentRequest, AiLessonContentResponse>("/api/lesson-content/generate", request);

    public Task<AiLessonExerciseResponse?> GenerateLessonExerciseAsync(AiLessonExerciseRequest request)
        => SendAsync<AiLessonExerciseRequest, AiLessonExerciseResponse>("/api/lesson-exercise/generate", request);

    public Task<AiLessonExerciseResponse?> RetryLessonExerciseAsync(AiExerciseRetryRequest request)
        => SendAsync<AiExerciseRetryRequest, AiLessonExerciseResponse>("/api/lesson-exercise/retry", request);

    public Task<AiExerciseReviewResponse?> CheckExerciseReviewAsync(AiExerciseReviewRequest request)
        => SendAsync<AiExerciseReviewRequest, AiExerciseReviewResponse>("/api/exercise-review/check", request);

    public Task<AiLessonResourcesResponse?> GenerateLessonResourcesAsync(AiLessonResourcesRequest request)
        => SendAsync<AiLessonResourcesRequest, AiLessonResourcesResponse>("/api/lesson-resources/generate", request);

    public async Task<bool> HealthCheckAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/health");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private async Task<TResponse?> SendAsync<TRequest, TResponse>(string endpoint, TRequest request)
        where TRequest : IAiRequestWithApiKey
        where TResponse : class, Application.Models.Responses.IModelUsageCarrier
    {
        try
        {
            request.GoogleApiKey = await _keyProvider.GetCurrentUserKeyAsync();

            _logger.LogInformation("POST {Endpoint}", endpoint);
            var response = await _httpClient.PostAsJsonAsync(endpoint, request);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("AI API Error: {StatusCode} - {Error}", response.StatusCode, error);
                throw new HttpRequestException($"AI API Error: {response.StatusCode} - {error}");
            }

            var data = await response.Content.ReadFromJsonAsync<TResponse>();
            if (data?.Usage != null)
            {
                var correlationId = Guid.TryParse(data.CorrelationId, out var cid) ? cid : Guid.NewGuid();
                await _costLogger.LogAsync(data.Usage, correlationId);
            }
            return data;
        }
        catch (TaskCanceledException)
        {
            _logger.LogError("AI API request timed out for {Endpoint}", endpoint);
            throw new TimeoutException("The AI API request timed out. Please try again.");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to connect to AI API");
            throw new Exception($"Failed to connect to AI API: {ex.Message}");
        }
    }
}
