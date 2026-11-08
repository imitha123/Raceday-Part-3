using System.ComponentModel.DataAnnotations;

namespace RaceDay_part_3.ViewModels
{
    public class OrganizerRegisterViewModel
    {
        [Required]
        public string OrganizerName { get; set; }

        [Required]
        public string OrganizerSurname { get; set; }

        [Required]
        [EmailAddress]
        public string OrganizerEmail { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [MinLength(8)]
        public string Password { get; set; }
    }
}
