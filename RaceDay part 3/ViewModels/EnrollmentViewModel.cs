using System.ComponentModel.DataAnnotations;

namespace RaceDay_part_3.ViewModels
{
    public class EnrollmentViewModel
    {
        public int EventID { get; set; }

        public int ParticipantID { get; set; }

        public string? EventName { get; set; }

        public string? ParticipantName { get; set; }

        public string Status { get; set; } = "Pending";
    }
}