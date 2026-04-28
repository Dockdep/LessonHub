namespace LessonsHub.Application.Models.Responses;

/// <summary>
/// Marker for AI response DTOs that ship per-call cost telemetry alongside the payload.
/// Lets the HTTP client log usage generically without knowing each response shape.
/// </summary>
public interface IModelUsageCarrier
{
    string? CorrelationId { get; }
    List<ModelUsage>? Usage { get; }
}
