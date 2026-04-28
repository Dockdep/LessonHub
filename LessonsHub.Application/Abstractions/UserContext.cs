namespace LessonsHub.Application.Abstractions;

/// <summary>
/// Mutable, scoped holder for the acting user's id. Two callers populate it:
///
///   - In an HTTP request scope, the holder stays empty; <see cref="ICurrentUser"/>
///     reads from <c>IHttpContextAccessor</c> as usual.
///
///   - In a job execution scope (created by the background service per job),
///     the worker sets <see cref="UserId"/> from <c>Job.UserId</c> before
///     resolving the executor — so services that depend on <c>ICurrentUser</c>
///     see the job's owner instead of throwing for a missing HttpContext.
///
/// Registered as <c>Scoped</c> so each request / job has its own instance.
/// </summary>
public sealed class UserContext
{
    public int? UserId { get; set; }
}
