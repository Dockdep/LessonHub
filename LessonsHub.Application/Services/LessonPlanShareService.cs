using LessonsHub.Application.Abstractions;
using LessonsHub.Application.Abstractions.Repositories;
using LessonsHub.Application.Abstractions.Services;
using LessonsHub.Application.Models.Responses;
using LessonsHub.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace LessonsHub.Application.Services;

public sealed class LessonPlanShareService : ILessonPlanShareService
{
    private readonly ILessonPlanShareRepository _shares;
    private readonly ILessonPlanRepository _plans;
    private readonly IUserRepository _users;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<LessonPlanShareService> _logger;

    public LessonPlanShareService(
        ILessonPlanShareRepository shares,
        ILessonPlanRepository plans,
        IUserRepository users,
        ICurrentUser currentUser,
        ILogger<LessonPlanShareService> logger)
    {
        _shares = shares;
        _plans = plans;
        _users = users;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<ServiceResult<List<LessonPlanShareDto>>> GetSharesAsync(int planId, CancellationToken ct = default)
    {
        var userId = _currentUser.Id;

        if (!await _plans.IsOwnerAsync(planId, userId, ct))
            return ServiceResult<List<LessonPlanShareDto>>.NotFound("Lesson plan not found.");

        var shares = await _shares.GetByPlanAsync(planId, ct);
        var dtos = shares.Select(ToDto).ToList();
        return ServiceResult<List<LessonPlanShareDto>>.Ok(dtos);
    }

    public async Task<ServiceResult<LessonPlanShareDto>> AddShareAsync(int planId, string? email, CancellationToken ct = default)
    {
        var userId = _currentUser.Id;

        if (!await _plans.IsOwnerAsync(planId, userId, ct))
            return ServiceResult<LessonPlanShareDto>.NotFound("Lesson plan not found.");

        if (string.IsNullOrWhiteSpace(email))
            return ServiceResult<LessonPlanShareDto>.BadRequest("Email is required.");

        var trimmed = email.Trim();
        var target = await _users.GetByEmailAsync(trimmed, ct);
        if (target == null)
            return ServiceResult<LessonPlanShareDto>.NotFound("No user found with that email. Ask them to sign in once first.");

        if (target.Id == userId)
            return ServiceResult<LessonPlanShareDto>.BadRequest("You already own this plan.");

        if (await _shares.ExistsAsync(planId, target.Id, ct))
            return ServiceResult<LessonPlanShareDto>.Conflict("Already shared with this user.");

        var share = new LessonPlanShare
        {
            LessonPlanId = planId,
            UserId = target.Id,
            SharedAt = DateTime.UtcNow
        };
        _shares.Add(share);
        await _shares.SaveChangesAsync(ct);

        _logger.LogInformation("Plan {PlanId} shared with user {TargetId} by {OwnerId}", planId, target.Id, userId);

        return ServiceResult<LessonPlanShareDto>.Ok(new LessonPlanShareDto
        {
            Id = share.Id,
            UserId = target.Id,
            Email = target.Email,
            Name = target.Name,
            SharedAt = share.SharedAt
        });
    }

    public async Task<ServiceResult> RemoveShareAsync(int planId, int shareUserId, CancellationToken ct = default)
    {
        var userId = _currentUser.Id;

        if (!await _plans.IsOwnerAsync(planId, userId, ct))
            return ServiceResult.NotFound("Lesson plan not found.");

        var share = await _shares.GetAsync(planId, shareUserId, ct);
        if (share == null)
            return ServiceResult.NotFound("Share not found.");

        _shares.Remove(share);
        await _shares.SaveChangesAsync(ct);

        _logger.LogInformation("Plan {PlanId} unshared from user {TargetId}", planId, shareUserId);
        return new ServiceResult(ServiceErrorKind.None, "Share removed.");
    }

    private static LessonPlanShareDto ToDto(LessonPlanShare s) => new()
    {
        Id = s.Id,
        UserId = s.UserId,
        Email = s.User!.Email,
        Name = s.User.Name,
        SharedAt = s.SharedAt
    };
}
