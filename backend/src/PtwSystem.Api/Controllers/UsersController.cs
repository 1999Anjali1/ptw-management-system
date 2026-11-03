using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PtwSystem.Api.Dtos;
using PtwSystem.Api.Services;

namespace PtwSystem.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Roles = "Admin")]
public class UsersController(IUserService users) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserDto>>> GetAll() => Ok(await users.GetAllAsync());

    [HttpPost]
    public async Task<ActionResult<UserDto>> Create(CreateUserRequest request) =>
        Ok(await users.CreateAsync(request));

    [HttpPost("{id:int}/activate")]
    public async Task<ActionResult<UserDto>> Activate(int id) => Ok(await users.SetActiveAsync(id, true));

    [HttpPost("{id:int}/deactivate")]
    public async Task<ActionResult<UserDto>> Deactivate(int id) => Ok(await users.SetActiveAsync(id, false));
}
