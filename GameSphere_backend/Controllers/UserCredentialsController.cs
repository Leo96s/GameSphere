using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using GameSphere_backend.Authorization;
using GameSphere_backend.Interfaces;
using GameSphere_backend.Models.FrontendModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GameSphere_backend.Controllers;

[Route("api/User")]
[ApiController]
[Authorize(Policy = ActiveUserRequirement.PolicyName)]
[EnableRateLimiting("auth")]
public sealed class UserCredentialsController : ResponseController
{
    private readonly IPasswordChangeService _passwordChangeService;
    private readonly IEmailChangeService _emailChangeService;

    public UserCredentialsController(
        IPasswordChangeService passwordChangeService,
        IEmailChangeService emailChangeService,
        IConfiguration configuration,
        IWebHostEnvironment environment) : base(configuration, environment)
    {
        _passwordChangeService = passwordChangeService ?? throw new ArgumentNullException(nameof(passwordChangeService));
        _emailChangeService = emailChangeService ?? throw new ArgumentNullException(nameof(emailChangeService));
    }

    [HttpPost("{id:int}/password")]
    public async Task<IActionResult> ChangePassword(int id, [FromBody] ChangePasswordRequest request)
    {
        if (!IsCurrentUser(id))
        {
            return Forbid();
        }

        var serviceResponse = await _passwordChangeService.ChangePasswordAsync(id, request);
        return HandleResponse(serviceResponse);
    }

    [HttpPost("{id:int}/email/request")]
    public async Task<IActionResult> RequestEmailChange(int id, [FromBody] RequestEmailChangeRequest request)
    {
        if (!IsCurrentUser(id))
        {
            return Forbid();
        }

        var serviceResponse = await _emailChangeService.RequestEmailChangeAsync(id, request);
        return HandleResponse(serviceResponse);
    }

    [HttpPost("{id:int}/email/confirm")]
    public async Task<IActionResult> ConfirmEmailChange(int id, [FromBody] ConfirmEmailChangeRequest request)
    {
        if (!IsCurrentUser(id))
        {
            return Forbid();
        }

        var serviceResponse = await _emailChangeService.ConfirmEmailChangeAsync(id, request);
        return HandleResponse(serviceResponse);
    }

    private bool IsCurrentUser(int userId)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        return int.TryParse(userIdClaim, out var authenticatedUserId)
            && authenticatedUserId == userId;
    }
}
