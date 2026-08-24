using GameSphere_backend.Interfaces;
using GameSphere_backend.Models.FrontendModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GameSphere_backend.Controllers;

[Route("api/User")]
[ApiController]
public sealed class UserRegistrationController : ResponseController
{
    private readonly IUserRegistrationService _userRegistrationService;

    public UserRegistrationController(
        IUserRegistrationService userRegistrationService,
        IConfiguration configuration,
        IWebHostEnvironment environment) : base(configuration, environment)
    {
        _userRegistrationService = userRegistrationService ?? throw new ArgumentNullException(nameof(userRegistrationService));
    }

    [HttpPost]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> CreateEntity([FromBody] RegisterUserRequest user)
    {
        var serviceResponse = await _userRegistrationService.CreateNewUserAsync(user);

        if (!serviceResponse.Success || serviceResponse.Data == null)
        {
            return HandleResponse(serviceResponse);
        }

        return CreatedAtAction(
            nameof(UserLookupController.GetEntityById),
            "UserLookup",
            new { id = serviceResponse.Data.Id },
            serviceResponse.Data);
    }
}
