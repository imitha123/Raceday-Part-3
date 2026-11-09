using Microsoft.AspNetCore.Mvc;
using RaceDay_part_3.Services;
using Microsoft.AspNetCore.Hosting;
using System.Net.Http.Json;
using System.IO;
using RaceDay_part_3.ViewModels;
using System.Text.Json;

namespace RaceDay_part_3.Controllers
{
    public class ParticipantController : Controller
    {
        private readonly ApiService _apiService;

        private readonly IWebHostEnvironment _environment;

        public ParticipantController(
     ApiService apiService,
     IWebHostEnvironment environment)
        {
            _apiService = apiService;
            _environment = environment;
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
      "UserName",
      GetValue("name") + " " + GetValue("surname"));

            HttpContext.Session.SetString(
                "Email",
                GetValue("email"));

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

        private async Task<string?> SaveProfileImageAsync(IFormFile? image)
        {
            if (image == null || image.Length == 0)
            {
                return null;
            }

            string[] allowedExtensions =
            {
        ".jpg", ".jpeg", ".png", ".webp"
    };

            string extension =
                Path.GetExtension(image.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
            {
                throw new InvalidOperationException(
                    "Only JPG, JPEG, PNG and WEBP images are allowed.");
            }

            if (image.Length > 5 * 1024 * 1024)
            {
                throw new InvalidOperationException(
                    "Profile pictures cannot exceed 5 MB.");
            }

            string folder = Path.Combine(
                _environment.WebRootPath,
                "uploads",
                "profile-pictures");

            Directory.CreateDirectory(folder);

            string fileName =
                Guid.NewGuid().ToString() + extension;

            string filePath = Path.Combine(folder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await image.CopyToAsync(stream);
            }

            return "/uploads/profile-pictures/" + fileName;
        }

        // GET: Participant/Profile
        public async Task<IActionResult> Profile()
        {
            if (HttpContext.Session.GetString("Role") != "Participant")
            {
                return RedirectToAction("Login");
            }

            string token = HttpContext.Session.GetString("Token")!;

            var response =
                await _apiService.GetParticipantProfileAsync(token);

            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = "Unable to retrieve your profile.";
                return RedirectToAction("Dashboard");
            }

            var profile =
                await response.Content.ReadFromJsonAsync<ParticipantProfileViewModel>();

            if (profile == null)
            {
                return RedirectToAction("Dashboard");
            }

            return View(profile);
        }


        // POST: Participant/Profile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(
            ParticipantProfileViewModel model)
        {
            if (HttpContext.Session.GetString("Role") != "Participant")
            {
                return RedirectToAction("Login");
            }

            string token = HttpContext.Session.GetString("Token")!;

            // Profile picture is optional
            ModelState.Remove(nameof(model.ProfileImage));

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string? oldImagePath = model.ProfileImagePath;
            string? newImagePath = null;

            try
            {
                if (model.ProfileImage != null)
                {
                    newImagePath =
                        await SaveProfileImageAsync(model.ProfileImage);

                    model.ProfileImagePath = newImagePath;
                }

                var response =
                    await _apiService.UpdateParticipantProfileAsync(
                        model, token);

                if (!response.IsSuccessStatusCode)
                {
                    // Remove newly uploaded image if the API update fails
                    if (newImagePath != null)
                    {
                        DeleteProfileImage(newImagePath);
                    }

                    ModelState.AddModelError(
                        "",
                        "Unable to update your profile. Please check your information.");

                    model.ProfileImagePath = oldImagePath;

                    return View(model);
                }

                // Remove previous picture after successful replacement
                if (newImagePath != null && !string.IsNullOrEmpty(oldImagePath))
                {
                    DeleteProfileImage(oldImagePath);
                }

                HttpContext.Session.SetString(
                    "UserName", model.ParticipantName + " " + model.ParticipantSurname);

                HttpContext.Session.SetString("Email", model.Email);

                TempData["Success"] = "Your profile has been updated successfully!";

                HttpContext.Session.SetString(
    "ProfileImagePath",
    model.ProfileImagePath ?? "");

                return RedirectToAction("Profile");
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(nameof(model.ProfileImage), ex.Message);
                model.ProfileImagePath = oldImagePath;

                return View(model);
            }
        }


        // DELETE OLD PROFILE PICTURE
        private void DeleteProfileImage(string? imagePath)
        {
            if (string.IsNullOrWhiteSpace(imagePath))
            {
                return;
            }

            string fileName = Path.GetFileName(imagePath);

            string fullPath = Path.Combine(
                _environment.WebRootPath,
                "uploads",
                "profile-pictures",
                fileName);

            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
            }
        }
    }
}
