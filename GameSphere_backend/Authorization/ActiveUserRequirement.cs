namespace GameSphere_backend.Authorization;

public sealed class ActiveUserRequirement : Microsoft.AspNetCore.Authorization.IAuthorizationRequirement
{
    public const string PolicyName = "ActiveUser";
}
