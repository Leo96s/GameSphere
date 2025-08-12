using GameSphere_backend.Interfaces;
using GameSphere_backend.Models.BackendModels;
using GameSphere_backend.Models.FrontendModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;

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
    public class UserController : BaseCrudController<UserDto>
    {
        private readonly IUserService _userService;

        /// <summary>
        /// Initializes a new instance of the UserController class.
        /// </summary>
        /// <param name="userService">The user service for handling business logic.</param>
        /// <param name="configuration">The application configuration.</param>
        /// <exception cref="ArgumentNullException">Thrown when userService is null.</exception>
        public UserController(IUserService userService, IConfiguration configuration) : base(configuration)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
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
        [Authorize]
        [HttpGet("by-id/{id}")]
        public override async Task<IActionResult> GetEntityById(int id)
        {
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
        public override async Task<IActionResult> CreateEntity(UserDto user)
        {
            var serviceResponse = await _userService.CreateNewUserAsync(user);

            if (!serviceResponse.Success)
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
        [Authorize]
        [HttpDelete("{id}")]
        [Authorize]
        public override async Task<IActionResult> DeleteEntity(int id)
        {
            {
                var serviceResponse = await _userService.DeleteUserAsync(id);

                return HandleResponse(serviceResponse);
            }
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
        [Authorize]
        [HttpPut("{id}")]
        [Authorize]
        public override async Task<IActionResult> UpdateEntity(int id, UserDto updatedUser)
        {
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
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var serviceResponse = await _userService.LoginAsync(request);

            return HandleResponse(serviceResponse);
        }

        /// <summary>
        /// Checks if an email address is available for registration.
        /// </summary>
        /// <param name="email">The email address to check.</param>
        /// <returns>
        /// Returns an IActionResult representing the HTTP response:
        /// - 200 OK with availability status if check was successful
        /// - 400 Bad Request if input is invalid
        /// - 401 Unauthorized if not authenticated
        /// - 404 Not Found if required resources are missing
        /// </returns>
        [Authorize]
        [HttpGet("get-email-availability/{email}")]
        public async Task<IActionResult> GetEmailAvailability(string email)
        {
            var serviceResponse = await _userService.CheckEmailAvailabilityAsync(email);

            return HandleResponse(serviceResponse);
        }


        /// <summary>
        /// Checks if a user exists with the specified external provider UID and email.
        /// </summary>
        /// <param name="uid">The unique identifier from the external authentication provider.</param>
        /// <param name="email">The email address associated with the external account.</param>
        /// <returns>
        /// Returns an IActionResult representing the HTTP response:
        /// - 200 OK with existence status if check was successful
        /// - 400 Bad Request if input is invalid
        /// - 404 Not Found if required resources are missing
        /// </returns>
        [HttpGet("user-exist/{uid}/{email}")]
        public async Task<IActionResult> GetUserExist(string uid, string email)
        {
            var serviceResponse = await _userService.CheckExistsUserExtern(uid,email);

            return HandleResponse(serviceResponse);
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
        [Authorize]
        [HttpGet("by-email/{email}")]
        public async Task<IActionResult> GetEntityByEmail(string email)
        {
            var serviceResponse = await _userService.GetUserByEmailAsync(email);

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
        public async Task<IActionResult> ResetPassword([FromBody] Models.FrontendModels.ResetPasswordRequest request)
        {
            var response = await _userService.ResetPassword(request.Email, request.ResetCode, request.NewPassword);
            return HandleResponse(response);
        }

        /// <summary>
        /// Authenticates a user using external provider credentials (UID and email).
        /// </summary>
        /// <param name="uid">The unique identifier from the external authentication provider.</param>
        /// <param name="email">The email address associated with the external account.</param>
        /// <returns>
        /// Returns an IActionResult representing the HTTP response:
        /// - 200 OK with JWT token and user data if authentication succeeds
        /// - 400 Bad Request if input is invalid
        /// - 401 Unauthorized if credentials are invalid
        /// - 404 Not Found if the user doesn't exist
        /// </returns>
        [HttpPost("social-login/{uid}/{email}")]
        public async Task<IActionResult> SocialLogin(string uid, string email)
        {
            var serviceResponse = await _userService.SocialLoginAsync(uid, email);

            return HandleResponse(serviceResponse);
        }

    }
}
