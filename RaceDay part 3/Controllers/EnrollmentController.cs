using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using RaceDay_part_3.Services;
using RaceDay_part_3.ViewModels;
using System.Text.Json;

namespace RaceDay_part_3.Controllers
{
    public class EnrollmentController : Controller
    {
        private readonly ApiService _apiService;

        private readonly JsonSerializerOptions _jsonOptions =
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

        public EnrollmentController(ApiService apiService)
        {
            _apiService = apiService;
        }

        // ==========================================
        // SESSION AND ROLE HELPERS
        // ==========================================

        private bool IsLoggedIn()
        {
            return !string.IsNullOrEmpty(
                HttpContext.Session.GetString("Token"));
        }

        private bool IsOrganizer()
        {
            return HttpContext.Session.GetString("Role") == "Organizer";
        }

        private bool IsParticipant()
        {
            return HttpContext.Session.GetString("Role") == "Participant";
        }

        private bool HasValidRole()
        {
            return IsOrganizer() || IsParticipant();
        }

        private int? GetUserId()
        {
            string? userIdString =
                HttpContext.Session.GetString("UserId");

            if (int.TryParse(userIdString, out int userId) && userId > 0)
            {
                return userId;
            }

            return null;
        }

        private IActionResult RedirectToLogin()
        {
            if (IsOrganizer())
                return RedirectToAction("Login", "Organizer");

            return RedirectToAction("Login", "Participant");
        }

        // ==========================================
        // LOAD EVENTS FOR DROPDOWNS
        // ==========================================

        // ==========================================
        // LOAD EVENTS FOR DROPDOWNS
        // ==========================================

        private async Task LoadEvents(
            string token,
            int? selectedEventId = null)
        {
            // Always initialise the dropdown.
            ViewBag.Events = new List<SelectListItem>();

            var response = await _apiService.GetEventsAsync(token);

            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = "Unable to load available events.";
                return;
            }

            var json = await response.Content.ReadAsStringAsync();

            var events = JsonSerializer.Deserialize<List<EventViewModel>>(
                json,
                _jsonOptions
            ) ?? new List<EventViewModel>();

            var eventOptions = new List<SelectListItem>();

            foreach (var item in events)
            {
                // Ignore null entries.
                if (item == null)
                    continue;

                eventOptions.Add(new SelectListItem
                {
                    Value = item.eventId.ToString(),

                    Text = string.IsNullOrWhiteSpace(item.EventName)
                        ? $"Event {item.eventId}"
                        : item.EventName,

                    Selected = selectedEventId.HasValue &&
                               item.eventId == selectedEventId.Value
                });
            }

            ViewBag.Events = eventOptions;
        }

        // ==========================================
        // INDEX - VIEW ENROLLMENTS
        // ==========================================

        public async Task<IActionResult> Index()
        {
            if (!IsLoggedIn())
                return RedirectToLogin();

            if (!HasValidRole())
                return Forbid();

            string token = HttpContext.Session.GetString("Token")!;

            int? userId = GetUserId();

            if (IsParticipant() && !userId.HasValue)
                return RedirectToLogin();

            var response = await _apiService.GetEnrollmentsAsync(token);

            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = "Unable to retrieve enrollments.";

                return View(new List<EnrollmentViewModel>());
            }

            var json = await response.Content.ReadAsStringAsync();

            var enrollments =
                JsonSerializer.Deserialize<List<EnrollmentViewModel>>(
                    json,
                    _jsonOptions
                ) ?? new List<EnrollmentViewModel>();

            // Participants only see their own enrollments.
            if (IsParticipant())
            {
                enrollments = enrollments
                    .Where(e => e.ParticipantID == userId!.Value)
                    .ToList();
            }

            return View(enrollments);
        }

        // ==========================================
        // DETAILS
        // ==========================================

        public async Task<IActionResult> Details(
            int eventId,
            int participantId)
        {
            if (!IsLoggedIn())
                return RedirectToLogin();

            if (!HasValidRole())
                return Forbid();

            int? userId = GetUserId();

            // Participants cannot access another participant's enrollment.
            if (IsParticipant())
            {
                if (!userId.HasValue)
                    return RedirectToLogin();

                if (participantId != userId.Value)
                    return Forbid();
            }

            string token = HttpContext.Session.GetString("Token")!;

            var response = await _apiService.GetEnrollmentAsync(
                eventId,
                participantId,
                token
            );

            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = "Enrollment not found.";

                return RedirectToAction(nameof(Index));
            }

            var json = await response.Content.ReadAsStringAsync();

            var enrollment =
                JsonSerializer.Deserialize<EnrollmentViewModel>(
                    json,
                    _jsonOptions
                );

            if (enrollment == null)
            {
                TempData["Error"] = "Unable to read enrollment details.";

                return RedirectToAction(nameof(Index));
            }

            return View(enrollment);
        }

        // ==========================================
        // CREATE - PARTICIPANT ONLY
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            if (!IsLoggedIn())
                return RedirectToLogin();

            if (!IsParticipant())
                return Forbid();

            string token = HttpContext.Session.GetString("Token")!;

            await LoadEvents(token);

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            EnrollmentCreateViewModel model)
        {
            if (!IsLoggedIn())
                return RedirectToLogin();

            if (!IsParticipant())
                return Forbid();

            string token = HttpContext.Session.GetString("Token")!;

            int? participantId = GetUserId();

            if (!participantId.HasValue)
                return RedirectToLogin();

            if (!ModelState.IsValid)
            {
                await LoadEvents(token, model.EventID);

                return View(model);
            }

            var response = await _apiService.CreateEnrollmentAsync(
                model.EventID,
                participantId.Value,
                token
            );

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] =
                    "You have successfully enrolled in the event!";

                return RedirectToAction(nameof(Index));
            }

            string error =
                await response.Content.ReadAsStringAsync();

            ModelState.AddModelError(
                "",
                string.IsNullOrWhiteSpace(error)
                    ? "Unable to create enrollment."
                    : error
            );

            await LoadEvents(token, model.EventID);

            return View(model);
        }

        // ==========================================
        // EDIT - ORGANIZER ONLY
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> Edit(
            int eventId,
            int participantId)
        {
            if (!IsLoggedIn())
                return RedirectToLogin();

            if (!IsOrganizer())
                return Forbid();

            string token = HttpContext.Session.GetString("Token")!;

            var response = await _apiService.GetEnrollmentAsync(
                eventId,
                participantId,
                token
            );

            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = "Enrollment not found.";

                return RedirectToAction(nameof(Index));
            }

            var json = await response.Content.ReadAsStringAsync();

            var enrollment =
                JsonSerializer.Deserialize<EnrollmentViewModel>(
                    json,
                    _jsonOptions
                );

            if (enrollment == null)
                return RedirectToAction(nameof(Index));

            var model = new EnrollmentEditViewModel
            {
                EventID = enrollment.EventID,
                ParticipantID = enrollment.ParticipantID,
                NewEventID = enrollment.EventID,
                NewParticipantID = enrollment.ParticipantID
            };

            await LoadEvents(token, model.NewEventID);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            EnrollmentEditViewModel model)
        {
            if (!IsLoggedIn())
                return RedirectToLogin();

            if (!IsOrganizer())
                return Forbid();

            string token = HttpContext.Session.GetString("Token")!;

            if (!ModelState.IsValid)
            {
                await LoadEvents(token, model.NewEventID);

                return View(model);
            }

            var response = await _apiService.UpdateEnrollmentAsync(
                model.EventID,
                model.ParticipantID,
                model.NewEventID,
                model.NewParticipantID,
                token
            );

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] =
                    "Enrollment updated successfully!";

                return RedirectToAction(nameof(Index));
            }

            string error =
                await response.Content.ReadAsStringAsync();

            ModelState.AddModelError(
                "",
                string.IsNullOrWhiteSpace(error)
                    ? "Unable to update enrollment."
                    : error
            );

            await LoadEvents(token, model.NewEventID);

            return View(model);
        }
        // ==========================================
        // CONFIRM ENROLLMENT - ORGANIZER ONLY
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(
            int eventId,
            int participantId,
            string status)
        {
            if (!IsLoggedIn())
                return RedirectToLogin();

            if (!IsOrganizer())
                return Forbid();

            if (eventId <= 0 || participantId <= 0)
            {
                TempData["Error"] = "Invalid enrollment details.";
                return RedirectToAction(nameof(Index));
            }

            // The MVC interface only allows organizers to confirm.
            if (status != "Confirmed")
            {
                TempData["Error"] = "Invalid enrollment status.";
                return RedirectToAction(nameof(Index));
            }

            string token = HttpContext.Session.GetString("Token")!;

            try
            {
                var response = await _apiService.UpdateEnrollmentStatusAsync(
                    eventId,
                    participantId,
                    status,
                    token
                );

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] =
                        "Enrollment successfully confirmed!";
                }
                else if (response.StatusCode ==
                         System.Net.HttpStatusCode.NotFound)
                {
                    TempData["Error"] = "Enrollment could not be found.";
                }
                else
                {
                    TempData["Error"] =
                        "Unable to update enrollment status. Please try again.";
                }
            }
            catch (HttpRequestException)
            {
                TempData["Error"] =
                    "The RaceDay service is currently unavailable.";
            }
            catch (TaskCanceledException)
            {
                TempData["Error"] =
                    "The request timed out. Please try again.";
            }

            return RedirectToAction(nameof(Index));
        }

        // ==========================================
        // DELETE - ORGANIZER ONLY
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> Delete(
            int eventId,
            int participantId)
        {
            if (!IsLoggedIn())
                return RedirectToLogin();

            if (!IsOrganizer())
                return Forbid();

            string token = HttpContext.Session.GetString("Token")!;

            var response = await _apiService.GetEnrollmentAsync(
                eventId,
                participantId,
                token
            );

            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = "Enrollment not found.";

                return RedirectToAction(nameof(Index));
            }

            var json = await response.Content.ReadAsStringAsync();

            var enrollment =
                JsonSerializer.Deserialize<EnrollmentViewModel>(
                    json,
                    _jsonOptions
                );

            if (enrollment == null)
                return RedirectToAction(nameof(Index));

            return View(enrollment);
        }

        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(
            int eventId,
            int participantId)
        {
            if (!IsLoggedIn())
                return RedirectToLogin();

            if (!IsOrganizer())
                return Forbid();

            string token = HttpContext.Session.GetString("Token")!;

            var response = await _apiService.DeleteEnrollmentAsync(
                eventId,
                participantId,
                token
            );

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] =
                    "Enrollment deleted successfully!";
            }
            else
            {
                TempData["Error"] =
                    "Unable to delete enrollment.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}