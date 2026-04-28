namespace LessonsHub.Domain.Entities;

/// <summary>
/// Background work unit. Lifecycle: Pending → Running → (Completed | Failed).
/// Created by the controller (returns 202 + Id), pumped by JobBackgroundService,
/// completion pushed to the user's SignalR group.
///
/// Persisted so that:
///   - Idempotent retries (same UserId+Type+IdempotencyKey returns existing Id)
///   - Survives instance restarts (Pending re-enqueued; Running marked Failed)
///   - Multi-tab consistency (any tab can fetch by Id and learn current state)
/// </summary>
public class Job
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int UserId { get; set; }
    public User? User { get; set; }

    /// <summary>
    /// Discriminator: matches one of the JobType string constants
    /// (LessonPlanGenerate, LessonContentGenerate, …). The executor registry
    /// uses this to dispatch to the right IJobExecutor.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    public JobStatus Status { get; set; } = JobStatus.Pending;

    /// <summary>JSON-serialized request payload. Schema is per-Type.</summary>
    public string PayloadJson { get; set; } = string.Empty;

    /// <summary>JSON-serialized response when Status=Completed.</summary>
    public string? ResultJson { get; set; }

    /// <summary>Truncated exception message when Status=Failed.</summary>
    public string? Error { get; set; }

    /// <summary>
    /// Optional client-supplied dedupe key. Combined with (UserId, Type) as a
    /// unique constraint so double-click / network retry returns the same Job
    /// instead of creating a duplicate one.
    /// </summary>
    public string? IdempotencyKey { get; set; }

    /// <summary>
    /// Free-form pointer to the entity this job mutates (e.g. "Lesson:42").
    /// Used by the multi-tab UX to decide which views to refresh on completion.
    /// </summary>
    public string? RelatedEntityType { get; set; }
    public int? RelatedEntityId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public enum JobStatus
{
    Pending = 0,
    Running = 1,
    Completed = 2,
    Failed = 3,
}
