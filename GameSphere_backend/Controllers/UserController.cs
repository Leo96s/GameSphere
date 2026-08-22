using GameSphere_backend.Interfaces;
using GameSphere_backend.Models.BackendModels;
using GameSphere_backend.Models.FrontendModels;
using GameSphere_backend.ServicesResponses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.RateLimiting;
using GameSphere_backend.Authorization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace GameSphere_backend.Controllers
{
    /// <summary>
    /// Controller responsible for handling all user-related HTTP endpoints including
    /// CRUD operations, authentication, password management, and email verification.
    /// </summary>
    /// <remarks>
    /// This controller inherits from BaseCrudController to provide common CRUD functionality
    /// and adds specific user-related endpoints for authentication and account management.
    /// </remarks>
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ResponseController
    {
        private readonly IUserService _userService;
        private readonly IFirebaseTokenVerifier _firebaseTokenVerifier;
        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;
        private const string AccessCookieName = "gamesphere_access_token";

        /// <summary>
        /// Initializes a new instance of the UserController class.
        /// </summary>
        /// <param name="userService">The user service for handling business logic.</param>
        /// <param name="firebaseTokenVerifier">The verifier for Firebase ID tokens.</param>
        /// <param name="configuration">The application configuration.</param>
        /// <param name="environment">The current hosting environment.</param>
        /// <exception cref="ArgumentNullException">Thrown when userService is null.</exception>
        public UserController(
            IUserService userService,
            IFirebaseTokenVerifier firebaseTokenVerifier,
            IConfiguration configuration,
            IWebHostEnvironment environment) : base(configuration, environment)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _firebaseTokenVerifier = firebaseTokenVerifier ?? throw new ArgumentNullException(nameof(firebaseTokenVerifier));
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        /// <summary>
        /// Retrieves a user by their unique identifier.
        /// </summary>
        /// <param name="id">The ID of the user to retrieve.</param>
        /// <returns>
        /// Returns an IActionResult representing the HTTP response:
        /// - 200 OK with the UserDto if found
        /// - 404 Not Found if the user doesn't exist
        /// - 401 Unauthorized if not authenticated
        /// </returns>
        [Authorize(Policy = ActiveUserRequirement.PolicyName)]
        [HttpGet("by-id/{id}")]
        public async Task<IActionResult> GetEntityById(int id)
        {
            if (!IsCurrentUser(id))
            {
                return Forbid();
            }

            var serviceResponse = await _userService.GetUserByIdAsync(id);

            return HandleResponse(serviceResponse);
        }

        /// <summary>
        /// Creates a new user in the system.
        /// </summary>
        /// <param name="user">The UserDto containing the user information to create.</param>
        /// <returns>
        /// Returns an IActionResult representing the HTTP response:
        /// - 201 Created with the location header pointing to the new user if successful
        /// - 400 Bad Request if the input is invalid
        /// - 404 Not Found if required resources are missing
        /// </returns>
        [HttpPost]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> CreateEntity([FromBody] RegisterUserRequest user)
        {
            var serviceResponse = await _userService.CreateNewUserAsync(user);

            if (!serviceResponse.Success || serviceResponse.Data == null)
            {
                return HandleResponse(serviceResponse);
            }

            return HandleCreatedAtAction(serviceResponse, nameof(GetEntityById), new { id = serviceResponse.Data.Id });
        }

        /// <summary>
        /// Deletes a user from the system by their ID.
        /// </summary>
        /// <param name="id">The ID of the user to delete.</param>
        /// <returns>
        /// Returns an IActionResult representing the HTTP response:
        /// - 204 No Content if deletion was successful
        /// - 400 Bad Request if the operation failed
        /// - 401 Unauthorized if not authenticated
        /// - 404 Not Found if the user doesn't exist
        /// </returns>
        [Authorize(Policy = ActiveUserRequirement.PolicyName)]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEntity(int id)
        {
            if (!IsCurrentUser(id))
            {
                return Forbid();
            }

            var serviceResponse = await _userService.DeleteUserAsync(id);

            return HandleResponse(serviceResponse);
        }

        /// <summary>
        /// Updates an existing user's information.
        /// </summary>
        /// <param name="id">The ID of the user to update.</param>
        /// <param name="updatedUser">The UserDto containing updated user information.</param>
        /// <returns>
        /// Returns an IActionResult representing the HTTP response:
        /// - 200 OK with the updated UserDto if successful
        /// - 400 Bad Request if the input is invalid
        /// - 401 Unauthorized if not authenticated
        /// - 404 Not Found if the user doesn't exist
        /// </returns>
        [Authorize(Policy = ActiveUserRequirement.PolicyName)]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateEntity(int id, [FromBody] UpdateUserRequest updatedUser)
        {
            if (!IsCurrentUser(id))
            {
                return Forbid();
            }

            var serviceResponse = await _userService.EditUserAsync(id, updatedUser);

            return HandleResponse(serviceResponse);
        }

        /// <summary>
        /// Authenticates a user with email and password credentials.
        /// </summary>
        /// <param name="request">The LoginRequest containing authentication credentials.</param>
        /// <returns>
        /// Returns an IActionResult representing the HTTP response:
        /// - 200 OK with JWT token and user data if authentication succeeds
        /// - 400 Bad Request if input is invalid
        /// - 401 Unauthorized if credentials are invalid
        /// - 404 Not Found if required resources are missing
        /// </returns>
        [HttpPost("login")]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var serviceResponse = await _userService.LoginAsync(request);

            return HandleAuthenticationResponse(serviceResponse);
        }

        /// <summary>
        /// Retrieves a user by their email address.
        /// </summary>
        /// <param name="email">The email address of the user to retrieve.</param>
        /// <returns>
        /// Returns an IActionResult representing the HTTP response:
        /// - 200 OK with the UserDto if found
        /// - 401 Unauthorized if not authenticated
        /// - 404 Not Found if the user doesn't exist
        /// </returns>
        [Authorize(Policy = ActiveUserRequirement.PolicyName)]
        [HttpGet("by-email/{email}")]
        public async Task<IActionResult> GetEntityByEmail(string email)
        {
            var serviceResponse = await _userService.GetUserByEmailAsync(email);

            if (!serviceResponse.Success || serviceResponse.Data == null)
            {
                return HandleResponse(serviceResponse);
            }

            if (!IsCurrentUser(serviceResponse.Data.Id))
            {
                return Forbid();
            }

            return HandleResponse(serviceResponse);
        }

        /// <summary>
        /// Sends a password reset code to the specified email address.
        /// </summary>
        /// <param name="email">The email address to send the reset code to.</param>
        /// <returns>
        /// Returns an IActionResult representing the HTTP response:
        /// - 200 OK if the reset code was sent successfully
        /// - 400 Bad Request if the operation failed
        /// - 404 Not Found if the user doesn't exist
        /// </returns>
        [HttpPost("send-reset-code")]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> SendResetCode([FromBody] string email)
        {
            var response = await _userService.SendPasswordResetCode(email);
            return HandleResponse(response);
        }

        /// <summary>
        /// Validates a password reset code for a user.
        /// </summary>
        /// <param name="request">The ValidateResetCodeRequest containing email and reset code.</param>
        /// <returns>
        /// Returns an IActionResult representing the HTTP response:
        /// - 200 OK if the reset code is valid
        /// - 400 Bad Request if the code is invalid or expired
        /// - 404 Not Found if the user doesn't exist
        /// </returns>
        [HttpPost("validate-reset-code")]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> ValidateResetCode([FromBody] ValidateResetCodeRequest request)
        {
            var response = await _userService.ValidateResetCode(request.Email, request.ResetCode);
            return HandleResponse(response);
        }

        /// <summary>
        /// Resets a user's password using a valid reset code.
        /// </summary>
        /// <param name="request">The ResetPasswordRequest containing email, reset code, and new password.</param>
        /// <returns>
        /// Returns an IActionResult representing the HTTP response:
        /// - 200 OK if the password was reset successfully
        /// - 400 Bad Request if the operation failed
        /// - 404 Not Found if the user doesn't exist
        /// </returns>
        [HttpPost("reset-password")]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> ResetPassword([FromBody] Models.FrontendModels.ResetPasswordRequest request)
        {
            var response = await _userService.ResetPassword(request.Email, request.ResetCode, request.NewPassword);
            return HandleResponse(response);
        }

        /// <summary>Authenticates a user with a verified Firebase ID token.</summary>
        /// <param name="request">The Firebase ID token request.</param>
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

            var serviceResponse = await _userService.SocialLoginAsync(firebaseUser);

            return HandleAuthenticationResponse(serviceResponse);
        }

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            Response.Cookies.Delete(AccessCookieName, new CookieOptions
            {
                Path = "/",
                Secure = !_environment.IsDevelopment(),
                SameSite = SameSiteMode.Lax,
                HttpOnly = true
            });

            return NoContent();
        }

        private IActionResult HandleAuthenticationResponse(ServiceResponse<LoginResponse> serviceResponse)
        {
            if (serviceResponse.Success && serviceResponse.Data is not null)
            {
                var expirationMinutes = _configuration.GetValue("JwtSettings:ExpirationMinutes", 60);
                Response.Cookies.Append(AccessCookieName, serviceResponse.Data.token, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = !_environment.IsDevelopment(),
                    SameSite = SameSiteMode.Lax,
                    IsEssential = true,
                    Path = "/",
                    Expires = DateTimeOffset.UtcNow.AddMinutes(expirationMinutes)
                });
            }

            return HandleResponse(serviceResponse);
        }

        private bool IsCurrentUser(int userId)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

            return int.TryParse(userIdClaim, out var authenticatedUserId)
                && authenticatedUserId == userId;
        }

    }
}
