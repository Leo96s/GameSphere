using GameSphere_backend.Data;
using GameSphere_backend.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace GameSphere_backend.Authorization;

public sealed class ActiveAdminAuthorizationHandler : AuthorizationHandler<ActiveAdminRequirement>
{
    private readonly AppDbContext _context;

    public ActiveAdminAuthorizationHandler(AppDbContext context)
    {
        _context = context;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ActiveAdminRequirement requirement)
    {
        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
            context.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return;
        }

        var isActiveAdmin = await _context.Users
            .AsNoTracking()
            .AnyAsync(user => user.Id == userId && user.Role == UserRole.Admin && user.isActive);

        if (isActiveAdmin)
        {
            context.Succeed(requirement);
        }
    }
}
