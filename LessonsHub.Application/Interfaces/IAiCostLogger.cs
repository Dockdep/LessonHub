using LessonsHub.Application.Models.Responses;

namespace LessonsHub.Application.Interfaces;

/// <summary>
/// Persists per-call cost + token telemetry returned by the AI service.
/// Decoupled from the AI client itself so the HTTP layer doesn't know about
/// pricing or DB writes.
/// </summary>
public interface IAiCostLogger
{
    Task LogAsync(IEnumerable<ModelUsage> usage, Guid correlationId);
}
