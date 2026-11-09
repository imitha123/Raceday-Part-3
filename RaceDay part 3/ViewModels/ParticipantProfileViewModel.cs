using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace RaceDay_part_3.ViewModels
{
    public class ParticipantProfileViewModel
    {
        public int ParticipantID { get; set; }

        [Required]
        [Display(Name = "First Name")]
        public string ParticipantName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Surname")]
        public string ParticipantSurname { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        public IFormFile? ProfileImage { get; set; }

        public string? ProfileImagePath { get; set; }
    }
}