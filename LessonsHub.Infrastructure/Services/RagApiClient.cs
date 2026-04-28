using System.Net.Http.Json;
using LessonsHub.Application.Interfaces;
using LessonsHub.Application.Models.Requests;
using LessonsHub.Application.Models.Responses;
using Microsoft.Extensions.Logging;

namespace LessonsHub.Infrastructure.Services;

/// <summary>
/// HTTP client for the Python RAG endpoints. Shares the same HttpClient
/// configuration as <see cref="LessonsAiApiClient"/> (BaseAddress, timeout,
/// IamAuthHandler) so requests carry the right S2S IAM token in production.
/// </summary>
public class RagApiClient : IRagApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<RagApiClient> _logger;

    public RagApiClient(HttpClient httpClient, ILogger<RagApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public Task<RagIngestResponse> IngestAsync(RagIngestRequest request, CancellationToken cancellationToken = default)
        => SendAsync<RagIngestRequest, RagIngestResponse>("/api/rag/ingest", request, cancellationToken);

    public Task<RagSearchResponse> SearchAsync(RagSearchRequest request, CancellationToken cancellationToken = default)
        => SendAsync<RagSearchRequest, RagSearchResponse>("/api/rag/search", request, cancellationToken);

    private async Task<TResponse> SendAsync<TRequest, TResponse>(
        string endpoint,
        TRequest request,
        CancellationToken cancellationToken)
        where TResponse : class
    {
        try
        {
            _logger.LogInformation("POST {Endpoint}", endpoint);
            var response = await _httpClient.PostAsJsonAsync(endpoint, request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("RAG API Error: {StatusCode} - {Error}", response.StatusCode, error);
                throw new HttpRequestException($"RAG API Error: {response.StatusCode} - {error}");
            }

            var data = await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken: cancellationToken);
            return data ?? throw new InvalidOperationException("RAG API returned an empty response.");
        }
        catch (TaskCanceledException)
        {
            _logger.LogError("RAG API request timed out for {Endpoint}", endpoint);
            throw new TimeoutException("The RAG API request timed out. Please try again.");
        }
    }
}
