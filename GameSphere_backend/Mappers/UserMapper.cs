using GameSphere_backend.Models.BackendModels;
using GameSphere_backend.Models.FrontendModels;
using GameSphere_backend.Utils;

namespace GameSphere_backend.Mappers
{
    public class UserMapper
    {
        public static UserDto? UserToDto(User user)
        {
            if(user == null) return null;

            var registrationDate = user.RegistrationDate == default ? DateTime.Now : user.RegistrationDate;

            return new UserDto
            {
                Id = user.Id,
                UID = user.UID,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                HashedPassword = null,
                RegistrationDate = registrationDate,
                isActive = user.isActive,
                Image = user.Image,
                TotalPoints = user.TotalPoints,
                Gender = user.Gender,
                Level = user.Level,
            };
        }

        public static User? UserToModel(UserDto user)
        {
            if (user == null) return null;

            if(user.RegistrationDate == default) user.RegistrationDate = DateTime.Now;

            var userModel = new User
            {
                Id = user.Id,
                UID = user.UID,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                HashedPassword = user.HashedPassword,
                RegistrationDate = user.RegistrationDate,
                isActive=user.isActive,
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
