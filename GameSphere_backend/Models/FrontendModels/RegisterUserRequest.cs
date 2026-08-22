using GameSphere_backend.Enums;
using System.ComponentModel.DataAnnotations;

namespace GameSphere_backend.Models.FrontendModels;

public sealed class RegisterUserRequest
{
    [Required, StringLength(100)]
    public required string FirstName { get; set; }

    [StringLength(100)]
    public string? LastName { get; set; }

    [Required, EmailAddress, StringLength(320)]
    public required string Email { get; set; }

    [Required, StringLength(128, MinimumLength = 8)]
    public required string Password { get; set; }

    public Gender Gender { get; set; }
}
