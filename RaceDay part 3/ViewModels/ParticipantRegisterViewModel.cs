using System.ComponentModel.DataAnnotations;

namespace RaceDay_part_3.ViewModels
{
    public class ParticipantRegisterViewModel
    {
        [Required]
        public string ParticipantName { get; set; }

        [Required]
        public string ParticipantSurname { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [MinLength(8)]
        public string Password { get; set; }
    }
}
