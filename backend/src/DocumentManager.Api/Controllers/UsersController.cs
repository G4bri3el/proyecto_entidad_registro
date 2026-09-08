using DocumentManager.Application.Interfaces;
using DocumentManager.Application.Users;
using DocumentManager.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocumentManager.Api.Controllers;

[ApiController]
[Authorize(Policy = AppPolicies.CanManageUsers)]
[Route("api/users")]
public sealed class UsersController(IUserService userService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] UserQuery query, CancellationToken cancellationToken)
    {
        return Ok(await userService.ListAsync(query, cancellationToken));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id, CancellationToken cancellationToken)
    {
        return Ok(await userService.GetAsync(id, cancellationToken));
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateUserRequest request, CancellationToken cancellationToken)
    {
        var user = await userService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = user.Id }, user);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, UpdateUserRequest request, CancellationToken cancellationToken)
    {
        return Ok(await userService.UpdateAsync(id, request, cancellationToken));
    }

    [HttpPatch("{id}/status")]
    public async Task<IActionResult> SetStatus(string id, SetUserStatusRequest request, CancellationToken cancellationToken)
    {
        return Ok(await userService.SetStatusAsync(id, request, cancellationToken));
    }

    [HttpPost("{id}/roles")]
    public async Task<IActionResult> AssignRole(string id, AssignRoleRequest request, CancellationToken cancellationToken)
    {
        await userService.AssignRoleAsync(id, request, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id}/roles/{role}")]
    public async Task<IActionResult> RemoveRole(string id, string role, CancellationToken cancellationToken)
    {
        await userService.RemoveRoleAsync(id, role, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id}/2fa/disable")]
    public async Task<IActionResult> DisableTwoFactorForUser(string id, CancellationToken cancellationToken)
    {
        await userService.DisableTwoFactorForUserAsync(id, cancellationToken);
        return NoContent();
    }
}
