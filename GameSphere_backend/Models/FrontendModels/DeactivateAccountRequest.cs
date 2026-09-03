namespace GameSphere_backend.Models.FrontendModels;

public sealed class DeactivateAccountRequest
{
    public string? CurrentPassword { get; set; }

    public string? FirebaseIdToken { get; set; }
}
