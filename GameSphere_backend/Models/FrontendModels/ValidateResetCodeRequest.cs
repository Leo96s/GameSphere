namespace GameSphere_backend.Models.FrontendModels
{
    public class ValidateResetCodeRequest
    {
        public string Email { get; set; }
        public string ResetCode { get; set; }
    }
}
