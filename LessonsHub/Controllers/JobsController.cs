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
}
