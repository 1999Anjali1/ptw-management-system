using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PtwSystem.Api.Common;
using PtwSystem.Api.Dtos;
using PtwSystem.Api.Services;

namespace PtwSystem.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService auth, ICurrentUser currentUser) : ControllerBase
{
    /// <summary>Log in and receive a JWT bearer token.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        var result = await auth.LoginAsync(request);
        return result is null
            ? Unauthorized(new ProblemDetails { Status = 401, Title = "Invalid email or password." })
            : Ok(result);
    }

    /// <summary>Profile of the currently logged-in user.</summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserDto>> Me() => Ok(await auth.GetUserAsync(currentUser.Id));
}
