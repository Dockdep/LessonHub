using System.Net;
using System.Net.Http.Json;
using LessonsHub.Application.Models.Requests;
using LessonsHub.Application.Models.Responses;
using LessonsHub.Infrastructure.Configuration;
using LessonsHub.Infrastructure.Services;
using LessonsHub.Tests.TestSupport;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LessonsHub.Tests.Services;

public class LessonsAiApiClientTests
{
    /// <summary>
    /// Captures the outgoing request so we can assert the user's API key was injected.
    /// </summary>
    private sealed class CapturingHandler : HttpMessageHandler
    {
        public AiLessonPlanRequest? CapturedPlanRequest { get; private set; }
        public Func<HttpResponseMessage>? RespondWith { get; set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.Content != null)
            {
                CapturedPlanRequest = await request.Content.ReadFromJsonAsync<AiLessonPlanRequest>(cancellationToken: cancellationToken);
            }
            return RespondWith?.Invoke() ?? new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new AiLessonPlanResponse
                {
                    Topic = "t",
                    Lessons = new(),
                    Usage = new(),
                    CorrelationId = Guid.NewGuid().ToString()
                })
            };
        }
    }

    private static (LessonsAiApiClient Client, CapturingHandler Handler) Build(TestDb db, int actingAs)
    {
        var handler = new CapturingHandler();
        var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:8000") };
        var currentUser = new TestCurrentUser(actingAs);
        var settings = new LessonsAiApiSettings();
        var keyProvider = new UserApiKeyProvider(db.Context, currentUser);
        var costLogger = new AiCostLogger(db.Context, currentUser, settings);
        var client = new LessonsAiApiClient(
            http,
            NullLogger<LessonsAiApiClient>.Instance,
            keyProvider,
            costLogger);
        return (client, handler);
    }

    [Fact]
    public async Task Generates_request_with_users_api_key_attached()
    {
        using var db = new TestDb();
        var (client, handler) = Build(db, db.Owner.Id);

        await client.GenerateLessonPlanAsync(new AiLessonPlanRequest { Topic = "x" });

        Assert.NotNull(handler.CapturedPlanRequest);
        Assert.Equal("owner-key", handler.CapturedPlanRequest!.GoogleApiKey);
    }

    [Fact]
    public async Task Different_users_keys_are_attached_independently()
    {
        using var db = new TestDb();

        var (ownerClient, ownerHandler) = Build(db, db.Owner.Id);
        await ownerClient.GenerateLessonPlanAsync(new AiLessonPlanRequest { Topic = "x" });

        var (borrowerClient, borrowerHandler) = Build(db, db.Borrower.Id);
        await borrowerClient.GenerateLessonPlanAsync(new AiLessonPlanRequest { Topic = "x" });

        Assert.Equal("owner-key", ownerHandler.CapturedPlanRequest!.GoogleApiKey);
        Assert.Equal("borrower-key", borrowerHandler.CapturedPlanRequest!.GoogleApiKey);
    }

    [Fact]
    public async Task Throws_when_user_has_no_api_key_set()
    {
        using var db = new TestDb();
        // Stranger has no GoogleApiKey set in TestDb seed.
        var (client, _) = Build(db, db.Stranger.Id);

        var ex = await Assert.ThrowsAnyAsync<Exception>(() =>
            client.GenerateLessonPlanAsync(new AiLessonPlanRequest { Topic = "x" }));

        // The controller wraps non-Http exceptions; assert the inner cause carries the right message.
        Assert.Contains("Google API key", ex.Message + " " + (ex.InnerException?.Message ?? ""), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task HealthCheck_does_not_attach_key_or_query_db()
    {
        using var db = new TestDb();
        var (client, handler) = Build(db, db.Stranger.Id);
        handler.RespondWith = () => new HttpResponseMessage(HttpStatusCode.OK);

        var ok = await client.HealthCheckAsync();

        Assert.True(ok);
    }
}
