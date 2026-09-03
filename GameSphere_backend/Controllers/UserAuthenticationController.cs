using GameSphere_backend.Interfaces;
using GameSphere_backend.Models.FrontendModels;
using GameSphere_backend.Services;
using GameSphere_backend.ServicesResponses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GameSphere_backend.Controllers;

[Route("api/User")]
[ApiController]
public sealed class UserAuthenticationController : ResponseController
{
    private readonly IPasswordLoginService _passwordLoginService;
    private readonly ISocialLoginService _socialLoginService;
    private readonly IPasswordRecoveryService _passwordRecoveryService;
    private readonly IAccountRecoveryService _accountRecoveryService;
    private readonly IFirebaseTokenVerifier _firebaseTokenVerifier;
    private readonly AuthenticationCookieService _cookieService;

    public UserAuthenticationController(
        IPasswordLoginService passwordLoginService,
        ISocialLoginService socialLoginService,
        IPasswordRecoveryService passwordRecoveryService,
        IAccountRecoveryService accountRecoveryService,
        IFirebaseTokenVerifier firebaseTokenVerifier,
        AuthenticationCookieService cookieService,
        IConfiguration configuration,
        IWebHostEnvironment environment) : base(configuration, environment)
    {
        _passwordLoginService = passwordLoginService ?? throw new ArgumentNullException(nameof(passwordLoginService));
        _socialLoginService = socialLoginService ?? throw new ArgumentNullException(nameof(socialLoginService));
        _passwordRecoveryService = passwordRecoveryService ?? throw new ArgumentNullException(nameof(passwordRecoveryService));
        _accountRecoveryService = accountRecoveryService ?? throw new ArgumentNullException(nameof(accountRecoveryService));
        _firebaseTokenVerifier = firebaseTokenVerifier ?? throw new ArgumentNullException(nameof(firebaseTokenVerifier));
        _cookieService = cookieService ?? throw new ArgumentNullException(nameof(cookieService));
    }

    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Login([FromBody] Microsoft.AspNetCore.Identity.Data.LoginRequest request)
    {
        var serviceResponse = await _passwordLoginService.LoginAsync(request);
        return HandleAuthenticationResponse(serviceResponse);
    }

    [HttpPost("social-login")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> SocialLogin([FromBody] SocialLoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.IdToken))
        {
            return Unauthorized("The social login token is invalid.");
        }

        var firebaseUser = await _firebaseTokenVerifier.VerifyAsync(
            request.IdToken,
            HttpContext.RequestAborted);

        if (firebaseUser is null)
        {
            return Unauthorized("The social login token is invalid.");
        }

        var serviceResponse = await _socialLoginService.SocialLoginAsync(firebaseUser);
        return HandleAuthenticationResponse(serviceResponse);
    }

    [HttpPost("send-reset-code")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> SendResetCode([FromBody] string email)
    {
        var response = await _passwordRecoveryService.SendPasswordResetCode(email);
        return HandleResponse(response);
    }

    [HttpPost("validate-reset-code")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ValidateResetCode([FromBody] ValidateResetCodeRequest request)
    {
        var response = await _passwordRecoveryService.ValidateResetCode(request.Email, request.ResetCode);
        return HandleResponse(response);
    }

    [HttpPost("reset-password")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ResetPassword([FromBody] Models.FrontendModels.ResetPasswordRequest request)
    {
        var response = await _passwordRecoveryService.ResetPassword(request.Email, request.ResetCode, request.NewPassword);
        return HandleResponse(response);
    }

    [HttpPost("request-account-recovery")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> RequestAccountRecovery([FromBody] string email)
    {
        var response = await _accountRecoveryService.RequestRecoveryCode(email);
        return HandleResponse(response);
    }

    [HttpPost("recover-account")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> RecoverAccount([FromBody] RecoverAccountRequest request)
    {
        var response = await _accountRecoveryService.RecoverAccount(request.Email, request.Code);
        return HandleResponse(response);
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        _cookieService.DeleteAccessCookie(Response);
        return NoContent();
    }

    private IActionResult HandleAuthenticationResponse(ServiceResponse<LoginResponse> serviceResponse)
    {
        if (serviceResponse.Success && serviceResponse.Data is not null)
        {
            _cookieService.AppendAccessCookie(Response, serviceResponse.Data);
        }

        return HandleResponse(serviceResponse);
    }
}
