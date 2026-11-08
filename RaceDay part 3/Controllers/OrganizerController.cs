using Microsoft.AspNetCore.Mvc;
using RaceDay_part_3.Services;
using RaceDay_part_3.ViewModels;
using System.Text.Json;

namespace RaceDay_part_3.Controllers
{
    public class OrganizerController : Controller
    {
       

        private readonly ApiService _apiService;

        public OrganizerController(ApiService apiService)
        {
            _apiService = apiService;
        }

        // GET: Organizer/Login
        public IActionResult Login()
        {
            return View();
        }

        // POST: Organizer/Login
        [HttpPost]
        public async Task<IActionResult> Login(
            OrganizerLoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var response = await _apiService.OrganizerLoginAsync(model);

            string result = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Error = "Invalid email or password.";
                return View(model);
            }

            using JsonDocument document = JsonDocument.Parse(result);

            JsonElement data = document.RootElement;

            string GetValue(string property)
            {
                return data.TryGetProperty(property, out JsonElement value)
                    ? value.ToString()
                    : "";
            }

            string token = GetValue("token");

            if (string.IsNullOrWhiteSpace(token))
            {
                ViewBag.Error = "Login response did not contain a token.";
                return View(model);
            }

            HttpContext.Session.SetString(
                "UserId", GetValue("organizerId"));

            HttpContext.Session.SetString(
                "UserName", GetValue("organizerName"));

            HttpContext.Session.SetString(
                "Email", GetValue("organizerEmail"));

            HttpContext.Session.SetString(
                "Role", "Organizer");

            HttpContext.Session.SetString(
                "Token", token);

            return RedirectToAction("Dashboard");
        }

        // GET: Organizer/Register
        public IActionResult Register()
        {
            return View();
        }

        // POST: Organizer/Register
        [HttpPost]
        public async Task<IActionResult> Register(
            OrganizerRegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var response = await _apiService.OrganizerRegisterAsync(model);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] =
                    "Registration successful! Please log in.";

                return RedirectToAction("Login");
            }

            ViewBag.Error = await response.Content.ReadAsStringAsync();

            return View(model);
        }

        // GET: Organizer/Dashboard
        public IActionResult Dashboard()
        {
            if (HttpContext.Session.GetString("Role") != "Organizer")
            {
                return RedirectToAction("Login");
            }

            return View();
        }
        // POST: Organizer/Logout
        [HttpPost]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();

            return RedirectToAction("Index", "Home");
        }
    }
}
