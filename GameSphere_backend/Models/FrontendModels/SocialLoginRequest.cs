using System.ComponentModel.DataAnnotations;

namespace GameSphere_backend.Models.FrontendModels;

public sealed class SocialLoginRequest
{
    [StringLength(8192)]
    public string? IdToken { get; set; }
}
