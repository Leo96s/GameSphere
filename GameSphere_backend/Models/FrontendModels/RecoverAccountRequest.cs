using System.ComponentModel.DataAnnotations;

namespace GameSphere_backend.Models.FrontendModels;

public sealed class RecoverAccountRequest
{
    [Required(ErrorMessage = "Email address is required")]
    [EmailAddress(ErrorMessage = "Invalid email address format")]
    public required string Email { get; set; }

    [Required(ErrorMessage = "Recovery code is required")]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "Recovery code must be 6 digits")]
    public required string Code { get; set; }
}
