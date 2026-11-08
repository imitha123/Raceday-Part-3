using System.ComponentModel.DataAnnotations;

namespace RaceDay_part_3.ViewModels
{
    public class EnrollmentCreateViewModel
    {
        [Required(ErrorMessage = "Please select an event.")]
        [Range(1, int.MaxValue,
            ErrorMessage = "Please select a valid event.")]
        public int EventID { get; set; }
    }
}