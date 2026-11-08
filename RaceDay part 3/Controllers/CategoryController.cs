using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;
using RaceDay_part_3.Services;
using RaceDay_part_3.ViewModels;

namespace RaceDay_part_3.Controllers
{
    public class CategoryController : Controller
    {
        private readonly ApiService _apiService;

        public CategoryController(ApiService apiService)
        {
            _apiService = apiService;
        }

        // CHECK ORGANIZER ACCESS
        private bool IsOrganizer()
        {
            return HttpContext.Session.GetString("Role")
                == "Organizer";
        }

        // GET: Category/Index
        public async Task<IActionResult> Index()
        {
            if (!IsOrganizer())
            {
                return RedirectToAction("Login", "Organizer");
            }

            string token =
                HttpContext.Session.GetString("Token") ?? "";

            var response =
                await _apiService.GetCategoriesAsync(token);

            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Error =
                    await response.Content.ReadAsStringAsync();

                return View(new List<CategoryViewModel>());
            }

            var categories =
                await response.Content
                .ReadFromJsonAsync<List<CategoryViewModel>>();

            return View(categories ?? new List<CategoryViewModel>());
        }

        // GET: Category/Create
        public IActionResult Create()
        {
            if (!IsOrganizer())
            {
                return RedirectToAction("Login", "Organizer");
            }

            return View();
        }

        // POST: Category/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            CategoryCreateViewModel model)
        {
            if (!IsOrganizer())
            {
                return RedirectToAction("Login", "Organizer");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string token =
                HttpContext.Session.GetString("Token") ?? "";

            var response =
                await _apiService.CreateCategoryAsync(model, token);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] =
                    "Category created successfully!";

                return RedirectToAction("Index");
            }

            ViewBag.Error =
                await response.Content.ReadAsStringAsync();

            return View(model);
        }

        // GET: Category/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            if (!IsOrganizer())
            {
                return RedirectToAction("Login", "Organizer");
            }

            string token =
                HttpContext.Session.GetString("Token") ?? "";

            var response =
                await _apiService.GetCategoryByIdAsync(id, token);

            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = "Could not retrieve category.";

                return RedirectToAction("Index");
            }

            var category =
                await response.Content
                .ReadFromJsonAsync<CategoryViewModel>();

            if (category == null)
            {
                return RedirectToAction("Index");
            }

            var model = new CategoryCreateViewModel
            {
                Name = category.Name,
                Description = category.Description
            };

            ViewBag.CategoryId = id;

            return View(model);
        }

        // POST: Category/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id, CategoryCreateViewModel model)
        {
            if (!IsOrganizer())
            {
                return RedirectToAction("Login", "Organizer");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.CategoryId = id;
                return View(model);
            }

            string token =
                HttpContext.Session.GetString("Token") ?? "";

            var response =
                await _apiService.UpdateCategoryAsync(
                    id, model, token);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] =
                    "Category updated successfully!";

                return RedirectToAction("Index");
            }

            ViewBag.Error =
                await response.Content.ReadAsStringAsync();

            ViewBag.CategoryId = id;

            return View(model);
        }

        // POST: Category/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            if (!IsOrganizer())
            {
                return RedirectToAction("Login", "Organizer");
            }

            string token =
                HttpContext.Session.GetString("Token") ?? "";

            var response =
                await _apiService.DeleteCategoryAsync(id, token);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] =
                    "Category deleted successfully!";
            }
            else
            {
                TempData["Error"] =
                    "Could not delete category: " +
                    await response.Content.ReadAsStringAsync();
            }

            return RedirectToAction("Index");
        }
    }
}