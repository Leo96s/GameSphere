using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using GameSphere_backend.Authorization;
using GameSphere_backend.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameSphere_backend.Controllers;

[Route("api/User")]
[ApiController]
public sealed class UserLookupController : ResponseController
{
    private readonly IUserQueryService _userQueryService;

    public UserLookupController(
        IUserQueryService userQueryService,
        IConfiguration configuration,
        IWebHostEnvironment environment) : base(configuration, environment)
    {
        _userQueryService = userQueryService ?? throw new ArgumentNullException(nameof(userQueryService));
    }

    [Authorize(Policy = ActiveUserRequirement.PolicyName)]
    [HttpGet("by-id/{id}")]
    public async Task<IActionResult> GetEntityById(int id)
    {
        if (!IsCurrentUser(id))
        {
            return Forbid();
        }

        var serviceResponse = await _userQueryService.GetUserByIdAsync(id);
        return HandleResponse(serviceResponse);
    }

    [Authorize(Policy = ActiveUserRequirement.PolicyName)]
    [HttpGet("by-email/{email}")]
    public async Task<IActionResult> GetEntityByEmail(string email)
    {
        var serviceResponse = await _userQueryService.GetUserByEmailAsync(email);

        if (!serviceResponse.Success || serviceResponse.Data == null)
        {
            return HandleResponse(serviceResponse);
        }

        if (!IsCurrentUser(serviceResponse.Data.Id))
        {
            return Forbid();
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
