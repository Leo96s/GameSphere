using GameSphere_backend.Models.BackendModels;
using GameSphere_backend.Models.FrontendModels;
using GameSphere_backend.ServicesResponses;
using Microsoft.AspNetCore.Identity.Data;

namespace GameSphere_backend.Interfaces
{
    /// <summary>
    /// Defines the contract for user-related operations including authentication,
    /// registration, profile management, and password recovery.
    /// </summary>
    /// <remarks>
    /// This interface provides all necessary methods for user management in the GameSphere system,
    /// following the service-response pattern for consistent error handling and messaging.
    /// </remarks>
    public interface IUserService
    {
        /// <summary>
        /// Retrieves a user's information by their unique identifier.
        /// </summary>
        /// <param name="id">The ID of the user to retrieve.</param>
        /// <returns>
        /// A ServiceResponse containing:
        /// - Success: true if operation succeeded, false otherwise
        /// - Data: UserDto with user information if successful
        /// - Message: Error message if operation failed
        /// - Type: Response type indicator
        /// </returns>
        Task<ServiceResponse<UserDto>> GetUserByIdAsync(int id);

        /// <summary>
        /// Creates a new user account in the system.
        /// </summary>
        /// <param name="user">UserDto containing registration information.</param>
        /// <returns>
        /// A ServiceResponse containing:
        /// - Success: true if user was created successfully
        /// - Data: Created UserDto if successful
        /// - Message: Error message if validation or creation failed
        /// - Type: Response type indicator
        /// </returns>
        Task<ServiceResponse<UserDto>> CreateNewUserAsync(RegisterUserRequest user);

        /// <summary>
        /// Deletes a user account from the system.
        /// </summary>
        /// <param name="id">The ID of the user to delete.</param>
        /// <returns>
        /// A ServiceResponse containing:
        /// - Success: true if deletion succeeded
        /// - Data: Success message if operation succeeded
        /// - Message: Error message if operation failed
        /// - Type: Response type indicator
        /// </returns>
        Task<ServiceResponse<string>> DeleteUserAsync(int id);

        /// <summary>
        /// Updates an existing user's profile information.
        /// </summary>
        /// <param name="id">The ID of the user to update.</param>
        /// <param name="updatedUser">UserDto containing updated information.</param>
        /// <returns>
        /// A ServiceResponse containing:
        /// - Success: true if update succeeded
        /// - Data: Updated UserDto if successful
        /// - Message: Error message if validation or update failed
        /// - Type: Response type indicator
        /// </returns>
        Task<ServiceResponse<UserDto>> EditUserAsync(int id, UpdateUserRequest updatedUser);

        /// <summary>
        /// Authenticates a user with email and password credentials.
        /// </summary>
        /// <param name="request">LoginRequest containing authentication credentials.</param>
        /// <returns>
        /// A ServiceResponse containing:
        /// - Success: true if authentication succeeded
        /// - Data: LoginResponse with JWT token and user data if successful
        /// - Message: Error message if authentication failed
        /// - Type: Response type indicator
        /// </returns>
        Task<ServiceResponse<LoginResponse>> LoginAsync(LoginRequest request);

        /// <summary>
        /// Retrieves a user's information by their email address.
        /// </summary>
        /// <param name="email">Email address of the user to retrieve.</param>
        /// <returns>
        /// A ServiceResponse containing:
        /// - Success: true if operation succeeded
        /// - Data: UserDto with user information if found
        /// - Message: Error message if user not found
        /// - Type: Response type indicator
        /// </returns>
        Task<ServiceResponse<UserDto>> GetUserByEmailAsync(string email);

        /// <summary>
        /// Initiates the password reset process by sending a reset code to the user's email.
        /// </summary>
        /// <param name="email">Email address of the user requesting password reset.</param>
        /// <returns>
        /// A ServiceResponse containing:
        /// - Success: true if reset code was sent successfully
        /// - Data: boolean indicating operation status
        /// - Message: Status message about operation result
        /// - Type: Response type indicator
        /// </returns>
        Task<ServiceResponse<bool>> SendPasswordResetCode(string email);

        /// <summary>
        /// Validates a password reset code for a user.
        /// </summary>
        /// <param name="email">Email address of the user.</param>
        /// <param name="resetCode">Reset code to validate.</param>
        /// <returns>
        /// A ServiceResponse containing:
        /// - Success: true if code is valid and not expired
        /// - Data: boolean indicating validation status
        /// - Message: Status message about validation result
        /// - Type: Response type indicator
        /// </returns>
        Task<ServiceResponse<bool>> ValidateResetCode(string email, string resetCode);

        /// <summary>
        /// Resets a user's password using a valid reset code.
        /// </summary>
        /// <param name="email">Email address of the user.</param>
        /// <param name="resetCode">Valid reset code previously sent to the user.</param>
        /// <param name="newPassword">New password to set for the user.</param>
        /// <returns>
        /// A ServiceResponse containing:
        /// - Success: true if password was reset successfully
        /// - Data: boolean indicating operation status
        /// - Message: Status message about operation result
        /// - Type: Response type indicator
        /// </returns>
        Task<ServiceResponse<bool>> ResetPassword(string email, string resetCode, string newPassword);

        /// <summary>
        /// Authenticates a user using a verified Firebase ID token.
        /// </summary>
        /// <param name="firebaseUser">The identity extracted from the verified Firebase token.</param>
        /// <returns>
        /// A ServiceResponse containing:
        /// - Success: true if authentication succeeded
        /// - Data: LoginResponse with JWT token and user data if successful
        /// - Message: Error message if authentication failed
        /// - Type: Response type indicator
        /// </returns>
        Task<ServiceResponse<LoginResponse>> SocialLoginAsync(FirebaseUserInfo firebaseUser);
    }
}
