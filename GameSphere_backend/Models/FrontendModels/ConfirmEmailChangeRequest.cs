using System.ComponentModel.DataAnnotations;

namespace GameSphere_backend.Models.FrontendModels;

public sealed class ConfirmEmailChangeRequest
{
    [Required, StringLength(6, MinimumLength = 6)]
    public required string Code { get; set; }
}
