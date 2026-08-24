using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using GameSphere_backend.Authorization;
using GameSphere_backend.Interfaces;
using GameSphere_backend.Models.FrontendModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameSphere_backend.Controllers;

[Route("api/User")]
[ApiController]
public sealed class UserProfileController : ResponseController
{
    private readonly IUserProfileService _userProfileService;
    private readonly IUserDeletionService _userDeletionService;

    public UserProfileController(
        IUserProfileService userProfileService,
        IUserDeletionService userDeletionService,
        IConfiguration configuration,
        IWebHostEnvironment environment) : base(configuration, environment)
    {
        _userProfileService = userProfileService ?? throw new ArgumentNullException(nameof(userProfileService));
        _userDeletionService = userDeletionService ?? throw new ArgumentNullException(nameof(userDeletionService));
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
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteEntity(int id)
    {
        if (!IsCurrentUser(id))
        {
            return Forbid();
        }

        var serviceResponse = await _userDeletionService.DeleteUserAsync(id);
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
