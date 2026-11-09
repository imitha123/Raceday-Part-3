using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace RaceDay_part_3.ViewModels
{
    public class EventCreateViewModel
    {
        [Required]
        public string EventName { get; set; } = string.Empty;

        [Required]
        public string EventLocation { get; set; } = string.Empty;

        [Required]
        public string EventDate { get; set; } = string.Empty;

        [Required]
        public int CategoryID { get; set; }

        public int OrganizerID { get; set; }

        public IFormFile? BannerImage { get; set; }

        public string? BannerImagePath { get; set; }
    }
}