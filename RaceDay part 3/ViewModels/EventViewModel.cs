namespace RaceDay_part_3.ViewModels
{
    public class EventViewModel
    {
        public int eventId { get; set; }

        public string EventName { get; set; } = string.Empty;

        public string EventLocation { get; set; } = string.Empty;

        public string EventDate { get; set; } = string.Empty;

        public int CategoryID { get; set; }

        public int OrganizerID { get; set; }

        public string? BannerImagePath { get; set; }
    }
}