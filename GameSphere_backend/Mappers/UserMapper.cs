using GameSphere_backend.Models.BackendModels;
using GameSphere_backend.Models.FrontendModels;
using GameSphere_backend.Utils;

namespace GameSphere_backend.Mappers
{
    /// <summary>
    /// Provides static methods for mapping between User and UserDto objects.
    /// </summary>
    /// <remarks>
    /// This mapper handles bidirectional conversion between database models (User)
    /// and data transfer objects (UserDto), ensuring proper data transformation
    /// and validation.
    /// </remarks>
    public static class UserMapper
    {
        /// <summary>
        /// Converts a User entity to a UserDto.
        /// </summary>
        /// <param name="user">The User entity to convert. Can be null.</param>
        /// <returns>
        /// A UserDto containing the user data, or null if the input is null.
        /// The returned DTO will never contain the hashed password for security.
        /// </returns>
        /// <remarks>
        /// This method:
        /// - Handles null input gracefully
        /// - Sets default registration date if not specified
        /// - Explicitly excludes sensitive data (hashed password)
        /// - Maintains all other user properties
        /// </remarks>
        public static UserDto? UserToDto(User? user)
        {
            if (user == null) return null;

            var registrationDate = user.RegistrationDate == default
                ? DateTime.Now
                : user.RegistrationDate;

            return new UserDto
            {
                Id = user.Id,
                UID = user.UID,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                HashedPassword = null, // Explicitly null for security
                RegistrationDate = registrationDate,
                isActive = user.isActive,
                Image = user.Image,
                TotalPoints = user.TotalPoints,
                Gender = user.Gender,
                Level = user.Level,
            };
        }

        /// <summary>
        /// Converts a UserDto to a User entity.
        /// </summary>
        /// <param name="user">The UserDto to convert. Can be null.</param>
        /// <returns>
        /// A validated User entity, or null if the input is null.
        /// </returns>
        /// <exception cref="ValidationException">
        /// Thrown if the resulting User entity fails validation.
        /// </exception>
        /// <remarks>
        /// This method:
        /// - Handles null input gracefully
        /// - Sets default registration date if not specified
        /// - Includes full model validation
        /// - Preserves all user properties including password
        /// </remarks>
        public static User? UserToModel(UserDto? user)
        {
            if (user == null) return null;

            if (user.RegistrationDate == default)
            {
                user.RegistrationDate = DateTime.Now;
            }

            var userModel = new User
            {
                Id = user.Id,
                UID = user.UID,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                HashedPassword = user.HashedPassword,
                RegistrationDate = user.RegistrationDate,
                isActive = user.isActive,
                Image = user.Image,
                TotalPoints = user.TotalPoints,
                Gender = user.Gender,
                Level = user.Level,
            };

            ConversionValidate.ValidateModel(userModel);
            return userModel;
        }
    }
}
