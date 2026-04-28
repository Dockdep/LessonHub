using LessonsHub.Application.Models.Requests;
using LessonsHub.Application.Models.Responses;

namespace LessonsHub.Application.Abstractions.Services;

public interface IUserProfileService
{
    Task<ServiceResult<UserProfileDto>> GetProfileAsync(CancellationToken ct = default);
    Task<ServiceResult<UserProfileDto>> UpdateProfileAsync(UpdateUserProfileRequest request, CancellationToken ct = default);
}
