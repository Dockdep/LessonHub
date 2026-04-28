using LessonsHub.Application.Abstractions.Services;
using LessonsHub.Application.Models.Requests;
using LessonsHub.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LessonsHub.Controllers;

[ApiController]
[Route("api/user/profile")]
[Authorize]
public class UserProfileController : ControllerBase
{
    private readonly IUserProfileService _profile;

    public UserProfileController(IUserProfileService profile)
    {
        _profile = profile;
    }

    [HttpGet]
    public async Task<IActionResult> GetProfile() =>
        (await _profile.GetProfileAsync()).ToActionResult();

    [HttpPut]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateUserProfileRequest request) =>
        (await _profile.UpdateProfileAsync(request)).ToActionResult();
}
