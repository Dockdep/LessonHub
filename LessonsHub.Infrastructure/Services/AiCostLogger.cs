using System.Security.Claims;
using LessonsHub.Application.Interfaces;
using LessonsHub.Application.Models.Responses;
using LessonsHub.Domain.Entities;
using LessonsHub.Infrastructure.Configuration;
using LessonsHub.Infrastructure.Data;
using Microsoft.AspNetCore.Http;

namespace LessonsHub.Infrastructure.Services;

/// <summary>
/// Persists one <see cref="AiRequestLog"/> per <see cref="ModelUsage"/> entry,
/// computing pricing via <see cref="ModelPricingResolver"/>. Single concern.
/// </summary>
public class AiCostLogger : IAiCostLogger
{
    private readonly LessonsHubDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ModelPricingResolver _pricing;

    public AiCostLogger(
        LessonsHubDbContext dbContext,
        IHttpContextAccessor httpContextAccessor,
        LessonsAiApiSettings aiApiSettings)
    {
        _dbContext = dbContext;
        _httpContextAccessor = httpContextAccessor;
        _pricing = new ModelPricingResolver(aiApiSettings.Pricing);
    }

    public async Task LogAsync(IEnumerable<ModelUsage> usage, Guid correlationId)
    {
        if (usage == null) return;
        var list = usage as IList<ModelUsage> ?? usage.ToList();
        if (list.Count == 0) return;

        var userIdString = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        int? userId = int.TryParse(userIdString, out var parsedId) ? parsedId : null;

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
