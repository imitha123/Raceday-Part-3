using Microsoft.AspNetCore.Mvc;
using RaceDay_part_3.Services;
using RaceDay_part_3.ViewModels;
using System.Net.Http.Json;

namespace RaceDay_part_3.Controllers
{
    public class EventController : Controller
    {
        private readonly ApiService _apiService;

        public EventController(ApiService apiService)
        {
            _apiService = apiService;
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
        public async Task<IActionResult> Create(
            EventCreateViewModel model)
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

            var response = await _apiService.CreateEventAsync(
                model, organizerId, token);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Event created successfully!";
                return RedirectToAction("Index");
            }

            ViewBag.Error =
                await response.Content.ReadAsStringAsync();

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
                OrganizerID = eventDetails.OrganizerID
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

            model.OrganizerID = organizerId;

            if (!ModelState.IsValid)
            {
                await LoadCategoriesAsync(
                    token, model.CategoryID);

                ViewBag.EventId = id;

                return View(model);
            }

            var response =
                await _apiService.UpdateEventAsync(
                    id, model, token);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] =
                    "Event updated successfully!";

                return RedirectToAction("Index");
            }

            ViewBag.Error =
                await response.Content.ReadAsStringAsync();

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

            var response =
                await _apiService.DeleteEventAsync(id, token);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] =
                    "Event deleted successfully!";
            }
            else
            {
                TempData["Error"] =
                    "Could not delete event: " +
                    await response.Content.ReadAsStringAsync();
            }

            return RedirectToAction("Index");
        }
    }
}