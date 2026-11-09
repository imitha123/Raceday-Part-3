using Microsoft.AspNetCore.Mvc;
using RaceDay_part_3.Services;
using RaceDay_part_3.ViewModels;
using System.Net.Http.Json;
using System.IO;

namespace RaceDay_part_3.Controllers
{
    public class EventController : Controller
    {
        private readonly ApiService _apiService;
        private readonly IWebHostEnvironment _environment;

        public EventController(
            ApiService apiService,
            IWebHostEnvironment environment)
        {
            _apiService = apiService;
            _environment = environment;
        }

        // CHECK ORGANIZER ACCESS
        private bool IsOrganizer()
        {
            return HttpContext.Session.GetString("Role") == "Organizer";
        }

        // CHECK PARTICIPANT ACCESS
        private bool IsParticipant()
        {
            return HttpContext.Session.GetString("Role") == "Participant";
        }

        // CHECK LOGIN
        private bool IsLoggedIn()
        {
            return !string.IsNullOrEmpty(
                HttpContext.Session.GetString("Token"));
        }

        // REDIRECT TO THE CORRECT LOGIN PAGE
        private IActionResult RedirectToLogin()
        {
            if (IsParticipant())
                return RedirectToAction("Login", "Participant");

            return RedirectToAction("Login", "Organizer");
        }

        // LOAD CATEGORIES FOR DROPDOWN
        private async Task LoadCategoriesAsync(
            string token, int selectedCategoryId = 0)
        {
            var response =
                await _apiService.GetCategoriesAsync(token);

            if (response.IsSuccessStatusCode)
            {
                var categories =
                    await response.Content
                    .ReadFromJsonAsync<List<CategoryViewModel>>();

                ViewBag.Categories =
                    categories ?? new List<CategoryViewModel>();

                ViewBag.SelectedCategoryId = selectedCategoryId;
            }
            else
            {
                ViewBag.Categories = new List<CategoryViewModel>();

                ViewBag.Error = "Could not load categories.";
            }
        }

        // ==========================================
        // GET: Event/Index
        // BOTH PARTICIPANTS AND ORGANIZERS
        // ==========================================

        public async Task<IActionResult> Index()
        {
            if (!IsLoggedIn())
                return RedirectToLogin();

            if (!IsOrganizer() && !IsParticipant())
                return RedirectToLogin();

            string token =
                HttpContext.Session.GetString("Token") ?? "";

            var response =
                await _apiService.GetEventsAsync(token);

            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Error =
                    await response.Content.ReadAsStringAsync();

                return View(new List<EventViewModel>());
            }

            var events =
                await response.Content
                .ReadFromJsonAsync<List<EventViewModel>>();

            return View(events ?? new List<EventViewModel>());
        }

        // ==========================================
        // GET: Event/Details/5
        // BOTH PARTICIPANTS AND ORGANIZERS
        // ==========================================

        public async Task<IActionResult> Details(int id)
        {
            if (!IsLoggedIn())
                return RedirectToLogin();

            if (!IsOrganizer() && !IsParticipant())
                return RedirectToLogin();

            string token =
                HttpContext.Session.GetString("Token") ?? "";

            var response =
                await _apiService.GetEventByIdAsync(id, token);

            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = "Could not retrieve event.";

                return RedirectToAction("Index");
            }

            var eventDetails =
                await response.Content
                .ReadFromJsonAsync<EventViewModel>();

            if (eventDetails == null)
                return RedirectToAction("Index");

            return View(eventDetails);
        }

        // ==========================================
        // GET: Event/Create
        // ORGANIZER ONLY
        // ==========================================

        public async Task<IActionResult> Create()
        {
            if (!IsLoggedIn())
                return RedirectToLogin();

            if (!IsOrganizer())
                return Forbid();

            string token =
                HttpContext.Session.GetString("Token") ?? "";

            await LoadCategoriesAsync(token);

            return View(new EventCreateViewModel());
        }

        // ==========================================
        // POST: Event/Create
        // ORGANIZER ONLY
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EventCreateViewModel model)
        {
            if (!IsLoggedIn())
                return RedirectToLogin();

            if (!IsOrganizer())
                return Forbid();

            string token =
                HttpContext.Session.GetString("Token") ?? "";

            string organizerIdString =
                HttpContext.Session.GetString("UserId") ?? "";

            if (!int.TryParse(organizerIdString, out int organizerId)
                || organizerId <= 0)
            {
                return RedirectToAction("Login", "Organizer");
            }

            if (!ModelState.IsValid)
            {
                await LoadCategoriesAsync(token, model.CategoryID);
                return View(model);
            }

            string? savedImagePath = null;

            try
            {
                savedImagePath = await SaveBannerAsync(model.BannerImage);

                model.BannerImagePath = savedImagePath;
                model.OrganizerID = organizerId;

                var response = await _apiService.CreateEventAsync(
                    model, organizerId, token);

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Event created successfully!";
                    return RedirectToAction("Index");
                }

                // Remove uploaded file if API creation fails.
                DeleteBanner(savedImagePath);

                ViewBag.Error =
                    await response.Content.ReadAsStringAsync();
            }
            catch (InvalidOperationException ex)
            {
                DeleteBanner(savedImagePath);
                ModelState.AddModelError("BannerImage", ex.Message);
            }
            catch (Exception)
            {
                DeleteBanner(savedImagePath);
                ViewBag.Error = "An error occurred while creating the event.";
            }

            await LoadCategoriesAsync(token, model.CategoryID);

            return View(model);
        }

        // ==========================================
        // GET: Event/Edit/5
        // ORGANIZER ONLY
        // ==========================================

        public async Task<IActionResult> Edit(int id)
        {
            if (!IsLoggedIn())
                return RedirectToLogin();

            if (!IsOrganizer())
                return Forbid();

            string token =
                HttpContext.Session.GetString("Token") ?? "";

            var response =
                await _apiService.GetEventByIdAsync(id, token);

            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = "Could not retrieve event.";

                return RedirectToAction("Index");
            }

            var eventDetails =
                await response.Content
                .ReadFromJsonAsync<EventViewModel>();

            if (eventDetails == null)
                return RedirectToAction("Index");

            var model = new EventCreateViewModel
            {
                EventName = eventDetails.EventName,
                EventLocation = eventDetails.EventLocation,
                EventDate = eventDetails.EventDate,
                CategoryID = eventDetails.CategoryID,
                OrganizerID = eventDetails.OrganizerID,
                BannerImagePath = eventDetails.BannerImagePath
            };

            await LoadCategoriesAsync(
                token, model.CategoryID);

            ViewBag.EventId = id;

            return View(model);
        }

        // ==========================================
        // POST: Event/Edit/5
        // ORGANIZER ONLY
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
    int id, EventCreateViewModel model)
        {
            if (!IsLoggedIn())
                return RedirectToLogin();

            if (!IsOrganizer())
                return Forbid();

            string token =
                HttpContext.Session.GetString("Token") ?? "";

            if (!int.TryParse(
                HttpContext.Session.GetString("UserId"),
                out int organizerId) || organizerId <= 0)
            {
                return RedirectToAction("Login", "Organizer");
            }

            // Retrieve the existing event so we can preserve its image.
            var existingResponse =
                await _apiService.GetEventByIdAsync(id, token);

            if (!existingResponse.IsSuccessStatusCode)
            {
                TempData["Error"] = "Could not retrieve the existing event.";
                return RedirectToAction("Index");
            }

            var existingEvent =
                await existingResponse.Content.ReadFromJsonAsync<EventViewModel>();

            if (existingEvent == null)
                return RedirectToAction("Index");

            model.OrganizerID = organizerId;

            if (!ModelState.IsValid)
            {
                model.BannerImagePath = existingEvent.BannerImagePath;

                await LoadCategoriesAsync(token, model.CategoryID);
                ViewBag.EventId = id;

                return View(model);
            }

            string? newImagePath = null;

            try
            {
                // Keep the existing image unless a replacement is uploaded.
                model.BannerImagePath = existingEvent.BannerImagePath;

                if (model.BannerImage != null && model.BannerImage.Length > 0)
                {
                    newImagePath = await SaveBannerAsync(model.BannerImage);
                    model.BannerImagePath = newImagePath;
                }

                var response = await _apiService.UpdateEventAsync(
                    id, model, token);

                if (response.IsSuccessStatusCode)
                {
                    // Delete old image only after the API update succeeds.
                    if (newImagePath != null)
                    {
                        DeleteBanner(existingEvent.BannerImagePath);
                    }

                    TempData["Success"] = "Event updated successfully!";
                    return RedirectToAction("Index");
                }

                // The API update failed, so remove the newly uploaded file.
                DeleteBanner(newImagePath);

                ViewBag.Error =
                    await response.Content.ReadAsStringAsync();
            }
            catch (InvalidOperationException ex)
            {
                DeleteBanner(newImagePath);
                ModelState.AddModelError("BannerImage", ex.Message);
            }
            catch (Exception)
            {
                DeleteBanner(newImagePath);
                ViewBag.Error = "An error occurred while updating the event.";
            }

            model.BannerImagePath = existingEvent.BannerImagePath;

            await LoadCategoriesAsync(token, model.CategoryID);
            ViewBag.EventId = id;

            return View(model);
        }

        // ==========================================
        // POST: Event/Delete/5
        // ORGANIZER ONLY
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            if (!IsLoggedIn())
                return RedirectToLogin();

            if (!IsOrganizer())
                return Forbid();

            string token =
                HttpContext.Session.GetString("Token") ?? "";

            var eventResponse =
                await _apiService.GetEventByIdAsync(id, token);

            EventViewModel? eventDetails = null;

            if (eventResponse.IsSuccessStatusCode)
            {
                eventDetails =
                    await eventResponse.Content.ReadFromJsonAsync<EventViewModel>();
            }

            var response =
                await _apiService.DeleteEventAsync(id, token);

            if (response.IsSuccessStatusCode)
            {
                DeleteBanner(eventDetails?.BannerImagePath);

                TempData["Success"] = "Event deleted successfully!";
            }
            else
            {
                TempData["Error"] =
                    "Could not delete event: " +
                    await response.Content.ReadAsStringAsync();
            }

            return RedirectToAction("Index");
        }

        // ==========================================
        // SAVE EVENT BANNER
        // ==========================================

        private async Task<string?> SaveBannerAsync(IFormFile? file)
        {
            if (file == null || file.Length == 0)
                return null;

            string[] allowedExtensions =
            {
        ".jpg", ".jpeg", ".png", ".webp"
    };

            string extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
                throw new InvalidOperationException(
                    "Only JPG, JPEG, PNG and WebP images are allowed.");

            if (file.Length > 5 * 1024 * 1024)
                throw new InvalidOperationException(
                    "The banner image cannot exceed 5 MB.");

            string folder = Path.Combine(
                _environment.WebRootPath,
                "uploads",
                "event-banners");

            Directory.CreateDirectory(folder);

            string fileName = $"banner_{Guid.NewGuid():N}{extension}";

            string filePath = Path.Combine(folder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return $"/uploads/event-banners/{fileName}";
        }



        // ==========================================
        // DELETE EVENT BANNER FROM LOCAL STORAGE
        // ==========================================

        private void DeleteBanner(string? imagePath)
        {
            if (string.IsNullOrWhiteSpace(imagePath))
                return;

            string fileName = Path.GetFileName(imagePath);

            if (string.IsNullOrWhiteSpace(fileName))
                return;

            string folder = Path.Combine(
                _environment.WebRootPath,
                "uploads",
                "event-banners");

            string fullPath = Path.Combine(folder, fileName);

            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
            }
        }
    }
}