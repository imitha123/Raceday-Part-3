using System.ComponentModel.DataAnnotations;

namespace RaceDay_part_3.ViewModels
{
    public class CategoryCreateViewModel
    {
        [Required(ErrorMessage = "Category name is required.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Category description is required.")]
        public string Description { get; set; } = string.Empty;
    }
}
