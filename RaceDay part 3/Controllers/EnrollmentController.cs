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

        // CHECK LOGIN
        private bool IsLoggedIn()
        {
            return !string.IsNullOrEmpty(HttpContext.Session.GetString("Token"));
        }

        // CHECK ORGANIZER
        private bool IsOrganizer()
        {
            return HttpContext.Session.GetString("Role") == "Organizer";
        }

        // CHECK PARTICIPANT
        private bool IsParticipant()
        {
            return HttpContext.Session.GetString("Role") == "Participant";
        }

        // GET EVENTS FOR DROPDOWNS
        private async Task LoadEvents(string token, int? selectedEventId = null)
        {
            var response = await _apiService.GetEventsAsync(token);

            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Events = new List<SelectListItem>();
                return;
            }

            var json = await response.Content.ReadAsStringAsync();

            var events = JsonSerializer.Deserialize<List<EventViewModel>>(
                json, _jsonOptions
            ) ?? new List<EventViewModel>();

            ViewBag.Events = new SelectList(
                events,
                "EventID",
                "EventName",
                selectedEventId
            );
        }

        // ==========================================
        // INDEX - VIEW ENROLLMENTS
        // ==========================================

        public async Task<IActionResult> Index()
        {
            if (!IsLoggedIn())
                return RedirectToAction("Login", "Participant");

            string token = HttpContext.Session.GetString("Token")!;
            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;

            var response = await _apiService.GetEnrollmentsAsync(token);

            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = "Unable to retrieve enrollments.";
                return View(new List<EnrollmentViewModel>());
            }

            var json = await response.Content.ReadAsStringAsync();

            var enrollments = JsonSerializer.Deserialize<List<EnrollmentViewModel>>(
                json, _jsonOptions
            ) ?? new List<EnrollmentViewModel>();

            // Participants only see their own enrollments
            if (IsParticipant())
            {
                enrollments = enrollments
                    .Where(e => e.ParticipantID == userId)
                    .ToList();
            }

            return View(enrollments);
        }

        // ==========================================
        // DETAILS
        // ==========================================

        public async Task<IActionResult> Details(int eventId, int participantId)
        {
            if (!IsLoggedIn())
                return RedirectToAction("Login", "Participant");

            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;

            // Participants cannot view another participant's enrollment
            if (IsParticipant() && participantId != userId)
                return Forbid();

            string token = HttpContext.Session.GetString("Token")!;

            var response = await _apiService.GetEnrollmentAsync(
                eventId, participantId, token
            );

            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = "Enrollment not found.";
                return RedirectToAction(nameof(Index));
            }

            var json = await response.Content.ReadAsStringAsync();

            var enrollment = JsonSerializer.Deserialize<EnrollmentViewModel>(
                json, _jsonOptions
            );

            if (enrollment == null)
                return RedirectToAction(nameof(Index));

            return View(enrollment);
        }

        // ==========================================
        // CREATE - PARTICIPANT ENROLLMENT
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            if (!IsLoggedIn())
                return RedirectToAction("Login", "Participant");

            if (!IsParticipant())
                return Forbid();

            string token = HttpContext.Session.GetString("Token")!;

            await LoadEvents(token);

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EnrollmentCreateViewModel model)
        {
            if (!IsLoggedIn())
                return RedirectToAction("Login", "Participant");

            if (!IsParticipant())
                return Forbid();

            string token = HttpContext.Session.GetString("Token")!;
            int participantId = HttpContext.Session.GetInt32("UserId") ?? 0;

            if (!ModelState.IsValid || participantId <= 0)
            {
                await LoadEvents(token, model.EventID);
                return View(model);
            }

            var response = await _apiService.CreateEnrollmentAsync(
                model.EventID,
                participantId,
                token
            );

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "You have successfully enrolled in the event!";
                return RedirectToAction(nameof(Index));
            }

            string error = await response.Content.ReadAsStringAsync();

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
        public async Task<IActionResult> Edit(int eventId, int participantId)
        {
            if (!IsLoggedIn())
                return RedirectToAction("Login", "Organizer");

            if (!IsOrganizer())
                return Forbid();

            string token = HttpContext.Session.GetString("Token")!;

            var response = await _apiService.GetEnrollmentAsync(
                eventId, participantId, token
            );

            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = "Enrollment not found.";
                return RedirectToAction(nameof(Index));
            }

            var json = await response.Content.ReadAsStringAsync();

            var enrollment = JsonSerializer.Deserialize<EnrollmentViewModel>(
                json, _jsonOptions
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
        public async Task<IActionResult> Edit(EnrollmentEditViewModel model)
        {
            if (!IsLoggedIn())
                return RedirectToAction("Login", "Organizer");

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
                TempData["Success"] = "Enrollment updated successfully!";
                return RedirectToAction(nameof(Index));
            }

            string error = await response.Content.ReadAsStringAsync();

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
        // DELETE - ORGANIZER ONLY
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> Delete(int eventId, int participantId)
        {
            if (!IsLoggedIn())
                return RedirectToAction("Login", "Organizer");

            if (!IsOrganizer())
                return Forbid();

            string token = HttpContext.Session.GetString("Token")!;

            var response = await _apiService.GetEnrollmentAsync(
                eventId, participantId, token
            );

            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = "Enrollment not found.";
                return RedirectToAction(nameof(Index));
            }

            var json = await response.Content.ReadAsStringAsync();

            var enrollment = JsonSerializer.Deserialize<EnrollmentViewModel>(
                json, _jsonOptions
            );

            return View(enrollment);
        }

        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(
            int eventId, int participantId)
        {
            if (!IsLoggedIn())
                return RedirectToAction("Login", "Organizer");

            if (!IsOrganizer())
                return Forbid();

            string token = HttpContext.Session.GetString("Token")!;

            var response = await _apiService.DeleteEnrollmentAsync(
                eventId, participantId, token
            );

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Enrollment deleted successfully!";
            }
            else
            {
                TempData["Error"] = "Unable to delete enrollment.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}