using GameSphere_backend.Enums;
using Microsoft.Win32;
using System;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace GameSphere_backend.Models.BackendModels
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        public string? HashedPassword { get; set; }

        public int? ExternalId { get; set; }

        [Required(ErrorMessage = "The field 'FirstName' is required.")]
        [MaxLength(100, ErrorMessage = "The field 'FirstName' has to be less than 100 characters.")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "The field 'LastName' is required.")]
        [MaxLength(100, ErrorMessage = "The field 'LastName' has to be less than 100 characters.")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "The campo 'Email' is required.")]
        [EmailAddress(ErrorMessage = "The email has an invalid format.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "The field 'RegistrationDate' is required.")]
        public DateTime RegistrationDate { get; set; }

        [Required(ErrorMessage = "The field 'Gender' is required.")]
        public Gender Gender { get; set; }

        [Required(ErrorMessage = "The field 'Image' is required.")]
        public byte[] Image { get; set; }


        public int Level { get; set; }

        public long TotalPoints { get; set; }

        public ICollection<Quizz> Quizzs { get; set; }

        public ICollection<Game> Games { get; set; }

        public ICollection<Achievements> Achievements { get; set; }

        public ICollection<GlobalRanking> GlobalRanking { get; set; }

        public ICollection<Score> Scores { get; set; }

    }
}
