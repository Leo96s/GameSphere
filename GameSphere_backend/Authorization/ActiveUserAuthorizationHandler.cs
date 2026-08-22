using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using GameSphere_backend.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace GameSphere_backend.Authorization;

public sealed class ActiveUserAuthorizationHandler : AuthorizationHandler<ActiveUserRequirement>
{
    private readonly AppDbContext _context;

    public ActiveUserAuthorizationHandler(AppDbContext context)
    {
        _context = context;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ActiveUserRequirement requirement)
    {
        var userIdClaim = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? context.User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (!int.TryParse(userIdClaim, out var userId))
        {
            return;
        }

        var user = await _context.Users.AsNoTracking().SingleOrDefaultAsync(candidate => candidate.Id == userId);
        if (user is null || !user.isActive)
        {
            return;
        }

        var tokenVersionClaim = context.User.FindFirstValue("gamesphere_auth_version");
        if (tokenVersionClaim is not null &&
            (!int.TryParse(tokenVersionClaim, out var tokenVersion) || tokenVersion != user.AuthVersion))
        {
            return;
        }

        context.Succeed(requirement);
    }
}
