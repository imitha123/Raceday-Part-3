using System.ComponentModel.DataAnnotations;

namespace RaceDay_part_3.ViewModels
{
    public class ResultCreateViewModel
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Enter a valid participant ID.")]
        public int ParticipantID { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Position must be greater than zero.")]
        public int Position { get; set; }

        [Required]
        [Range(0, int.MaxValue, ErrorMessage = "Score cannot be negative.")]
        public int Score { get; set; }
    }
}