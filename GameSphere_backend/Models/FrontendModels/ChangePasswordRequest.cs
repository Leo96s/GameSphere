using System.ComponentModel.DataAnnotations;

namespace GameSphere_backend.Models.FrontendModels;

public sealed class ChangePasswordRequest
{
    public string? CurrentPassword { get; set; }

    public string? FirebaseIdToken { get; set; }

    [Required, StringLength(128, MinimumLength = 8)]
    public required string NewPassword { get; set; }
}
