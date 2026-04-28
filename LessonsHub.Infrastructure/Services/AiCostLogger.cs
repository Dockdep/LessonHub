using LessonsHub.Application.Abstractions;
using LessonsHub.Application.Interfaces;
using LessonsHub.Application.Models.Responses;
using LessonsHub.Domain.Entities;
using LessonsHub.Infrastructure.Configuration;
using LessonsHub.Infrastructure.Data;

namespace LessonsHub.Infrastructure.Services;

/// <summary>
/// Persists one <see cref="AiRequestLog"/> per <see cref="ModelUsage"/> entry,
/// computing pricing via <see cref="ModelPricingResolver"/>. Single concern.
///
/// Reads the acting user via <see cref="ICurrentUser"/> so cost logs land
/// with the right <c>UserId</c> in both HTTP-request and background-job
/// scopes (the JobBackgroundService populates <see cref="UserContext"/> from
/// <c>Job.UserId</c>). When unauthenticated (e.g. anonymous endpoints), the
/// row is saved with <c>UserId=null</c>.
/// </summary>
public class AiCostLogger : IAiCostLogger
{
    private readonly LessonsHubDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly ModelPricingResolver _pricing;

    public AiCostLogger(
        LessonsHubDbContext dbContext,
        ICurrentUser currentUser,
        LessonsAiApiSettings aiApiSettings)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _pricing = new ModelPricingResolver(aiApiSettings.Pricing);
    }

    public async Task LogAsync(IEnumerable<ModelUsage> usage, Guid correlationId)
    {
        if (usage == null) return;
        var list = usage as IList<ModelUsage> ?? usage.ToList();
        if (list.Count == 0) return;

        int? userId = _currentUser.IsAuthenticated ? _currentUser.Id : null;

        foreach (var entry in list)
        {
            var (pricePerIn, pricePerOut) = _pricing.Resolve(entry.ModelName ?? string.Empty, entry.InputTokens);
            _dbContext.AiRequestLogs.Add(new AiRequestLog
            {
                UserId = userId,
                CorrelationId = correlationId,
                RequestType = entry.RequestType ?? string.Empty,
                ModelName = entry.ModelName ?? string.Empty,
                InputTokens = entry.InputTokens,
                OutputTokens = entry.OutputTokens,
                LatencyMs = entry.LatencyMs,
                IsSuccess = entry.IsSuccess,
                FinishReason = entry.FinishReason ?? string.Empty,
                PricePerIn = pricePerIn,
                PricePerOut = pricePerOut,
                TotalCost = (entry.InputTokens * pricePerIn) + (entry.OutputTokens * pricePerOut),
                CreatedAt = DateTime.UtcNow,
            });
        }

        await _dbContext.SaveChangesAsync();
    }
}
