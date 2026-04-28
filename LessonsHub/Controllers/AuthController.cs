using LessonsHub.Application.Abstractions.Services;
using LessonsHub.Application.Models.Requests;
using LessonsHub.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace LessonsHub.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth)
    {
        _auth = auth;
    }

    [HttpPost("google")]
    public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request) =>
        (await _auth.LoginWithGoogleAsync(request)).ToActionResult();
}
