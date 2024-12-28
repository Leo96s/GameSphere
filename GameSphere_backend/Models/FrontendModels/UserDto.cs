using GameSphere_backend.Enums;

namespace GameSphere_backend.Models.FrontendModels
{
    public class UserDto
    {
        public int Id { get; set; }

        public string HashedPassword { get; set; }

        public int? ExternalId { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string Email { get; set; }

        public Gender Gender { get; set; }

        public byte[] Image { get; set; }

        public int Level { get; set; }

        public long TotalPoints { get; set; }

        public DateTime RegistrationDate { get; set; }



    }
}
