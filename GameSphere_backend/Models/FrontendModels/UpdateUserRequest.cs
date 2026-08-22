using GameSphere_backend.Enums;
using System.ComponentModel.DataAnnotations;

namespace GameSphere_backend.Models.FrontendModels;

public sealed class UpdateUserRequest
{
    [Required, StringLength(100)]
    public required string FirstName { get; set; }

    [StringLength(100)]
    public string? LastName { get; set; }

    [Required, EmailAddress, StringLength(320)]
    public required string Email { get; set; }

    public Gender Gender { get; set; }

    public string? Image { get; set; }

    [StringLength(128, MinimumLength = 8)]
    public string? Password { get; set; }
}
