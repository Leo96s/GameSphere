using Microsoft.AspNetCore.Authorization;

namespace GameSphere_backend.Authorization;

public sealed class ActiveAdminRequirement : IAuthorizationRequirement
{
    public const string PolicyName = "ActiveAdmin";
}
