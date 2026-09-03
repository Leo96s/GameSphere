using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using GameSphere_backend.Authorization;
using GameSphere_backend.Interfaces;
using GameSphere_backend.Models.FrontendModels;
using GameSphere_backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameSphere_backend.Controllers;

[Route("api/User")]
[ApiController]
public sealed class UserProfileController : ResponseController
{
    private readonly IUserProfileService _userProfileService;
    private readonly IAccountDeactivationService _accountDeactivationService;
    private readonly AuthenticationCookieService _cookieService;

    public UserProfileController(
        IUserProfileService userProfileService,
        IAccountDeactivationService accountDeactivationService,
        AuthenticationCookieService cookieService,
        IConfiguration configuration,
        IWebHostEnvironment environment) : base(configuration, environment)
    {
        _userProfileService = userProfileService ?? throw new ArgumentNullException(nameof(userProfileService));
        _accountDeactivationService = accountDeactivationService ?? throw new ArgumentNullException(nameof(accountDeactivationService));
        _cookieService = cookieService ?? throw new ArgumentNullException(nameof(cookieService));
    }

    [Authorize(Policy = ActiveUserRequirement.PolicyName)]
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateEntity(int id, [FromBody] UpdateUserRequest updatedUser)
    {
        if (!IsCurrentUser(id))
        {
            return Forbid();
        }

        var serviceResponse = await _userProfileService.EditUserAsync(id, updatedUser);
        return HandleResponse(serviceResponse);
    }

    [Authorize(Policy = ActiveUserRequirement.PolicyName)]
    [HttpPost("{id}/deactivate")]
    public async Task<IActionResult> DeactivateAccount(int id, [FromBody] DeactivateAccountRequest request)
    {
        if (!IsCurrentUser(id))
        {
            return Forbid();
        }

        var serviceResponse = await _accountDeactivationService.DeactivateAsync(id, request);
        if (serviceResponse.Success)
        {
            _cookieService.DeleteAccessCookie(Response);
        }

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
