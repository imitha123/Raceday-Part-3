using Microsoft.AspNetCore.Mvc;
using RaceDay_part_3.Services;
using RaceDay_part_3.ViewModels;
using System.Text.Json;

namespace RaceDay_part_3.Controllers
{
    public class ResultController : Controller
    {
        private readonly ApiService _apiService;

        public ResultController(ApiService apiService)
        {
            _apiService = apiService;
        }

        // GET USER ID - SUPPORTS STRING AND INTEGER SESSION VALUES
        private int? GetUserId()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");

            if (userId.HasValue)
                return userId.Value;

            string? userIdString =
                HttpContext.Session.GetString("UserId");

            if (int.TryParse(userIdString, out int parsedId))
                return parsedId;

            return null;
        }

        // CHECK LOGIN
        private bool IsLoggedIn()
        {
            return !string.IsNullOrEmpty(
                HttpContext.Session.GetString("Token"));
        }

        // CHECK ORGANIZER
        private bool IsOrganizer()
        {
            return HttpContext.Session.GetString("Role") == "Organizer";
        }

        // GET TOKEN
        private string GetToken()
        {
            return HttpContext.Session.GetString("Token") ?? "";
        }

        // ==============================
        // INDEX - VIEW RESULTS
        // ==============================

        public async Task<IActionResult> Index()
        {
            if (!IsLoggedIn())
                return RedirectToAction("Login", "Home");

            string token = GetToken();

            var response = await _apiService.GetResultsAsync(token);

            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = "Unable to retrieve results.";
                return View(new List<ResultViewModel>());
            }

            var apiResponse = await response.Content.ReadAsStringAsync();

            Console.WriteLine("========== RESULTS API DEBUG ==========");
            Console.WriteLine("API RESPONSE:");
            Console.WriteLine(apiResponse);

            var results =
                JsonSerializer.Deserialize<List<ResultViewModel>>(
                    apiResponse,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }) ?? new List<ResultViewModel>();

            Console.WriteLine($"TOTAL RESULTS RECEIVED: {results.Count}");

            foreach (var result in results)
            {
                Console.WriteLine(
                    $"ResultID: {result.ResultID}, " +
                    $"ParticipantID: {result.ParticipantID}, " +
                    $"Position: {result.Position}, " +
                    $"Score: {result.Score}");
            }

            Console.WriteLine("=======================================");

            // PARTICIPANTS CAN ONLY SEE THEIR OWN RESULTS
            if (!IsOrganizer())
            {
                int? participantId = GetUserId();

                if (participantId == null)
                    return RedirectToAction("Login", "Participant");

                results = results
                    .Where(r => r.ParticipantID == participantId)
                    .ToList();
            }

            return View(results);
        }

        // ==============================
        // DETAILS
        // ==============================

        public async Task<IActionResult> Details(int id)
        {
            if (!IsLoggedIn())
                return RedirectToAction("Login", "Participant");

            var response = await _apiService.GetResultByIdAsync(
                id, GetToken());

            if (!response.IsSuccessStatusCode)
                return NotFound();

            var result =
                await response.Content.ReadFromJsonAsync<ResultViewModel>();

            if (result == null)
                return NotFound();

            // PARTICIPANTS CAN ONLY VIEW THEIR OWN RESULTS
            if (!IsOrganizer())
            {
                int? participantId = GetUserId();

                if (participantId == null ||
                    result.ParticipantID != participantId)
                {
                    return Forbid();
                }
            }

            return View(result);
        }

        // ==============================
        // CREATE RESULT
        // ==============================

        [HttpGet]
        public IActionResult Create()
        {
            if (!IsOrganizer())
                return Forbid();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ResultCreateViewModel model)
        {
            if (!IsOrganizer())
                return Forbid();

            if (!ModelState.IsValid)
                return View(model);

            var response = await _apiService.CreateResultAsync(
                model, GetToken());

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Result created successfully!";
                return RedirectToAction(nameof(Index));
            }

            string error = await response.Content.ReadAsStringAsync();

            ModelState.AddModelError(
                "", string.IsNullOrWhiteSpace(error)
                    ? "Unable to create result."
                    : error);

            return View(model);
        }

        // ==============================
        // EDIT RESULT
        // ==============================

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            if (!IsOrganizer())
                return Forbid();

            var response = await _apiService.GetResultByIdAsync(
                id, GetToken());

            if (!response.IsSuccessStatusCode)
                return NotFound();

            var result =
                await response.Content.ReadFromJsonAsync<ResultViewModel>();

            if (result == null)
                return NotFound();

            var model = new ResultEditViewModel
            {
                ResultID = result.ResultID,
                ParticipantID = result.ParticipantID ?? 0,
                Position = result.Position ?? 0,
                Score = result.Score ?? 0
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id, ResultEditViewModel model)
        {
            if (!IsOrganizer())
                return Forbid();

            if (id != model.ResultID)
                return BadRequest();

            if (!ModelState.IsValid)
                return View(model);

            var response = await _apiService.UpdateResultAsync(
                id, model, GetToken());

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Result updated successfully!";
                return RedirectToAction(nameof(Index));
            }

            string error = await response.Content.ReadAsStringAsync();

            ModelState.AddModelError(
                "", string.IsNullOrWhiteSpace(error)
                    ? "Unable to update result."
                    : error);

            return View(model);
        }

        // ==============================
        // DELETE RESULT
        // ==============================

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            if (!IsOrganizer())
                return Forbid();

            var response = await _apiService.GetResultByIdAsync(
                id, GetToken());

            if (!response.IsSuccessStatusCode)
                return NotFound();

            var result =
                await response.Content.ReadFromJsonAsync<ResultViewModel>();

            if (result == null)
                return NotFound();

            return View(result);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (!IsOrganizer())
                return Forbid();

            var response = await _apiService.DeleteResultAsync(
                id, GetToken());

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Result deleted successfully!";
            }
            else
            {
                TempData["Error"] = "Unable to delete result.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}