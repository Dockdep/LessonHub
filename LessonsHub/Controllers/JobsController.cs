using LessonsHub.Application.Abstractions.Services;
using LessonsHub.Domain.Entities;
using LessonsHub.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LessonsHub.Controllers;

[Route("api/jobs")]
[ApiController]
[Authorize]
public class JobsController : ControllerBase
{
    private readonly IJobService _jobs;

    public JobsController(IJobService jobs)
    {
        _jobs = jobs;
    }

    /// <summary>Polling fallback when the SignalR connection isn't available.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct) =>
        (await _jobs.GetForCurrentUserAsync(id, ct)).ToActionResult();

    /// <summary>Lets the UI repopulate "in-flight" state after a navigation.</summary>
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] JobStatus? status, CancellationToken ct) =>
        (await _jobs.ListForCurrentUserAsync(status, ct)).ToActionResult();

    /// <summary>
    /// Returns the in-flight job (Pending/Running) matching the given coords,
    /// or 200 with `null` body if none. The UI calls this on page load to
    /// resume tracking work that started before navigation.
    /// </summary>
    [HttpGet("in-flight")]
    public async Task<IActionResult> InFlight(
        [FromQuery] string type,
        [FromQuery] string? relatedEntityType,
        [FromQuery] int? relatedEntityId,
        CancellationToken ct) =>
        (await _jobs.FindInFlightForCurrentUserAsync(type, relatedEntityType, relatedEntityId, ct)).ToActionResult();

    /// <summary>
    /// Lists every in-flight job tied to a single entity. Lets the UI restore
    /// all active banners on a page load with one query (vs. one per type).
    /// </summary>
    [HttpGet("in-flight-for-entity")]
    public async Task<IActionResult> InFlightForEntity(
        [FromQuery] string relatedEntityType,
        [FromQuery] int relatedEntityId,
        CancellationToken ct) =>
        (await _jobs.ListInFlightForEntityAsync(relatedEntityType, relatedEntityId, ct)).ToActionResult();
}
