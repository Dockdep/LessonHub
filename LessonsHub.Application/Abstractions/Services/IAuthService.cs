using LessonsHub.Application.Models.Requests;
using LessonsHub.Application.Models.Responses;

namespace LessonsHub.Application.Abstractions.Services;

public interface IAuthService
{
    Task<ServiceResult<LoginResponseDto>> LoginWithGoogleAsync(GoogleLoginRequest request, CancellationToken ct = default);
}
