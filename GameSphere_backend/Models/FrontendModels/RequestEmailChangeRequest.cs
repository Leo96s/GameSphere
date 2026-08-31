using System.ComponentModel.DataAnnotations;

namespace GameSphere_backend.Models.FrontendModels;

public sealed class RequestEmailChangeRequest
{
    [Required, EmailAddress, StringLength(320)]
    public required string NewEmail { get; set; }

    public string? CurrentPassword { get; set; }

    public string? FirebaseIdToken { get; set; }
}
