using GameSphere_backend.Enums;
using Microsoft.Win32;
using System;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace GameSphere_backend.Models
{
    public class User
    {
        [Key]
        public int id { get; set; }

        public string? hashedPassword { get; set; }

        public int? externalId { get; set; }

        [Required(ErrorMessage = "The field 'FirstName' is required.")]
        [MaxLength(100, ErrorMessage = "The field 'FirstName' has to be less than 100 characters.")]
        public string firstName { get; set; }

        [Required(ErrorMessage = "The field 'LastName' is required.")]
        [MaxLength(100, ErrorMessage = "The field 'LastName' has to be less than 100 characters.")]
        public string lastName { get; set; }

        [Required(ErrorMessage = "The campo 'Email' is required.")]
        [EmailAddress(ErrorMessage = "The email has an invalid format.")]
        public string email { get; set; }

        [Required(ErrorMessage = "The field 'RegistrationDate' is required.")]
        public DateTime registrationDate { get; set; }

        [Required(ErrorMessage = "The field 'Gender' is required.")]
        public Gender gender { get; set; }

        [Required(ErrorMessage = "The field 'Image' is required.")]
        public byte[] image { get; set; }


        public int level { get; set; }

        public long totalPoints { get; set; }

    }
}
