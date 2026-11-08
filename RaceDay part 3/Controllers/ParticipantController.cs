using Microsoft.AspNetCore.Mvc;
using RaceDay_part_3.Services;
using RaceDay_part_3.ViewModels;
using System.Text.Json;

namespace RaceDay_part_3.Controllers
{
    public class ParticipantController : Controller
    {
        private readonly ApiService _apiService;

        public ParticipantController(ApiService apiService)
        {
            _apiService = apiService;
        }

        // GET: Participant/Login
        public IActionResult Login()
        {
            return View();
        }

        // POST: Participant/Login
        [HttpPost]
        public async Task<IActionResult> Login(
            ParticipantLoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var response =
                await _apiService.ParticipantLoginAsync(model);

            string result =
                await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Error = "Invalid email or password.";
                return View(model);
            }

            using JsonDocument document =
                JsonDocument.Parse(result);

            JsonElement data = document.RootElement;

            string GetValue(string property)
            {
                return data.TryGetProperty(
                    property, out JsonElement value)
                    ? value.ToString()
                    : "";
            }

            string token = GetValue("token");

            if (string.IsNullOrWhiteSpace(token))
            {
                ViewBag.Error =
                    "Login response did not contain a token.";

                return View(model);
            }

            HttpContext.Session.SetString(
                "UserId", GetValue("participantId"));

            HttpContext.Session.SetString(
                "UserName", GetValue("participantName"));

            HttpContext.Session.SetString(
                "Email", GetValue("participantEmail"));

            HttpContext.Session.SetString(
                "Role", "Participant");

            HttpContext.Session.SetString(
                "Token", token);

            return RedirectToAction("Dashboard");
        }

        // GET: Participant/Register
        public IActionResult Register()
        {
            return View();
        }

        // POST: Participant/Register
        [HttpPost]
        public async Task<IActionResult> Register(
            ParticipantRegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var response =
                await _apiService.ParticipantRegisterAsync(model);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] =
                    "Registration successful! Please log in.";

                return RedirectToAction("Login");
            }

            ViewBag.Error =
                await response.Content.ReadAsStringAsync();

            return View(model);
        }

        // GET: Participant/Dashboard
        public IActionResult Dashboard()
        {
            if (HttpContext.Session.GetString("Role")
                != "Participant")
            {
                return RedirectToAction("Login");
            }

            return View();
        }
        // POST: Participant/Logout
        [HttpPost]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();

            return RedirectToAction("Index", "Home");
        }
    }
}
