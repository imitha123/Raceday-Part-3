using Microsoft.AspNetCore.Mvc;

namespace RaceDay_part_3.Controllers
{
    public class EnrollmentController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
