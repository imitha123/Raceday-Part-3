namespace RaceDay_part_3.ViewModels
{
    public class EnrollmentViewModel
    {
        public int EventID { get; set; }

        public int ParticipantID { get; set; }

        // Additional display information
        public string? EventName { get; set; }

        public string? ParticipantName { get; set; }
    }
}