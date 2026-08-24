using GameSphere_backend.ServicesResponses;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace GameSphere_backend.Controllers;

public abstract class AdminControllerBase : ControllerBase
{
    protected async Task<ActionResult<T>> RunAdminCommandAsync<T>(
        Func<int, Task<ServiceResponse<T>>> command,
        Func<T, ActionResult<T>> successResult)
    {
        if (!TryGetAdminId(out var adminId))
        {
            return Unauthorized();
        }

        var result = await command(adminId);
        if (!result.Success || result.Data is null)
        {
            return ToErrorResult(result);
        }

        return successResult(result.Data);
    }

    protected async Task<IActionResult> RunAdminDeleteAsync(
        Func<int, Task<ServiceResponse<bool>>> command)
    {
        if (!TryGetAdminId(out var adminId))
        {
            return Unauthorized();
        }

        var result = await command(adminId);
        if (result.Success)
        {
            return NoContent();
        }

        return ToErrorResult(result);
    }

    protected bool TryGetAdminId(out int adminId)
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier) ??
            User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return int.TryParse(claim, out adminId);
    }

    protected ActionResult ToErrorResult<T>(ServiceResponse<T> response) =>
        AdminErrorResultMapper.ToActionResult(this, response);
}
