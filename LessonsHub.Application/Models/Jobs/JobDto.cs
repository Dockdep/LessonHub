using LessonsHub.Domain.Entities;

namespace LessonsHub.Application.Models.Jobs;

/// <summary>HTTP polling shape — `GET /api/jobs/{id}`.</summary>
public sealed record JobDto(
    Guid Id,
    string Type,
    JobStatus Status,
    string? Result,
    string? Error,
    string? RelatedEntityType,
    int? RelatedEntityId,
    DateTime CreatedAt,
    DateTime? StartedAt,
    DateTime? CompletedAt
);

/// <summary>SignalR push shape — emitted to user-{id} group on every transition.</summary>
public sealed record JobEvent(
    Guid Id,
    string Type,
    JobStatus Status,
    string? Result,
    string? Error,
    string? RelatedEntityType,
    int? RelatedEntityId,
    DateTime Timestamp
);

/// <summary>Controller response: `202 Accepted` body.</summary>
public sealed record JobAcceptedResponse(Guid JobId);
