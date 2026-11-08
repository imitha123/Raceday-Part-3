using System.ComponentModel.DataAnnotations;

namespace RaceDay_part_3.ViewModels
{
    public class OrganizerLoginViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
}
