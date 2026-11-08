using System.ComponentModel.DataAnnotations;

namespace RaceDay_part_3.ViewModels
{
    public class EnrollmentEditViewModel
    {
        public int EventID { get; set; }

        public int ParticipantID { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int NewEventID { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int NewParticipantID { get; set; }
    }
}