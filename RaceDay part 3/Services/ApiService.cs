
using RaceDay_part_3.ViewModels;
using System.Net.Http.Json;
using System.Net.Http.Headers;

namespace RaceDay_part_3.Services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;

        public ApiService(IHttpClientFactory factory)
        {
            _httpClient = factory.CreateClient("RaceDayApi");
        }

        // ORGANIZER LOGIN
        public async Task<HttpResponseMessage> OrganizerLoginAsync(
            OrganizerLoginViewModel model)
        {
            var request = new
            {
                email = model.Email,
                password = model.Password
            };

            return await _httpClient.PostAsJsonAsync(
                "api/Organizer/login", request);
        }

        // PARTICIPANT LOGIN
        public async Task<HttpResponseMessage> ParticipantLoginAsync(
            ParticipantLoginViewModel model)
        {
            var request = new
            {
                email = model.Email,
                password = model.Password
            };

            return await _httpClient.PostAsJsonAsync(
                "api/Participant/Login", request);
        }

        // ORGANIZER REGISTRATION
        public async Task<HttpResponseMessage> OrganizerRegisterAsync(
            OrganizerRegisterViewModel model)
        {
            var request = new
            {
                organizername = model.OrganizerName,
                organizerEmail = model.OrganizerEmail,
                password = model.Password,
                organizerSurname = model.OrganizerSurname
            };

            return await _httpClient.PostAsJsonAsync(
                "api/Organizer/Register", request);
        }

        // PARTICIPANT REGISTRATION
        public async Task<HttpResponseMessage> ParticipantRegisterAsync(
            ParticipantRegisterViewModel model)
        {
            string url = "api/Participant/Register" +
                $"?participantName={Uri.EscapeDataString(model.ParticipantName)}" +
                $"&participantSurname={Uri.EscapeDataString(model.ParticipantSurname)}" +
                $"&email={Uri.EscapeDataString(model.Email)}" +
                $"&password={Uri.EscapeDataString(model.Password)}";

            return await _httpClient.PostAsync(url, null);
        }

        // HELPER: CREATE AUTHORIZED REQUEST
        private HttpRequestMessage CreateAuthorizedRequest(
            HttpMethod method, string url, string token)
        {
            var request = new HttpRequestMessage(method, url);

            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            return request;
        }

        // ==============================
        // CATEGORY MANAGEMENT
        // ==============================

        // GET ALL CATEGORIES
        public async Task<HttpResponseMessage> GetCategoriesAsync(
            string token)
        {
            var request = CreateAuthorizedRequest(
                HttpMethod.Get, "api/Category", token);

            return await _httpClient.SendAsync(request);
        }

        // GET CATEGORY BY ID
        public async Task<HttpResponseMessage> GetCategoryByIdAsync(
            int id, string token)
        {
            var request = CreateAuthorizedRequest(
                HttpMethod.Get, $"api/Category/{id}", token);

            return await _httpClient.SendAsync(request);
        }

        // CREATE CATEGORY
        public async Task<HttpResponseMessage> CreateCategoryAsync(
            CategoryCreateViewModel model, string token)
        {
            var request = CreateAuthorizedRequest(
                HttpMethod.Post, "api/Category", token);

            request.Content = JsonContent.Create(new
            {
                name = model.Name,
                description = model.Description
            });

            return await _httpClient.SendAsync(request);
        }

        // UPDATE CATEGORY
        public async Task<HttpResponseMessage> UpdateCategoryAsync(
            int id, CategoryCreateViewModel model, string token)
        {
            var request = CreateAuthorizedRequest(
                HttpMethod.Put, $"api/Category/{id}", token);

            request.Content = JsonContent.Create(new
            {
                name = model.Name,
                description = model.Description
            });

            return await _httpClient.SendAsync(request);
        }

        // DELETE CATEGORY
        public async Task<HttpResponseMessage> DeleteCategoryAsync(
            int id, string token)
        {
            var request = CreateAuthorizedRequest(
                HttpMethod.Delete, $"api/Category/{id}", token);

            return await _httpClient.SendAsync(request);
        }

        // ==============================
        // EVENT MANAGEMENT
        // ==============================

        // GET ALL EVENTS
        public async Task<HttpResponseMessage> GetEventsAsync(
            string token)
        {
            var request = CreateAuthorizedRequest(
                HttpMethod.Get, "api/Event", token);

            return await _httpClient.SendAsync(request);
        }

        // GET EVENT BY ID
        public async Task<HttpResponseMessage> GetEventByIdAsync(
            int id, string token)
        {
            var request = CreateAuthorizedRequest(
                HttpMethod.Get, $"api/Event/{id}", token);

            return await _httpClient.SendAsync(request);
        }

        // CREATE EVENT
        public async Task<HttpResponseMessage> CreateEventAsync(
            EventCreateViewModel model, string token)
        {
            var request = CreateAuthorizedRequest(
                HttpMethod.Post, "api/Event", token);

            request.Content = JsonContent.Create(new
            {
                eventName = model.EventName,
                eventLocation = model.EventLocation,
                eventDate = model.EventDate,
                categoryId = model.CategoryID
            });

            return await _httpClient.SendAsync(request);
        }

        // UPDATE EVENT
        public async Task<HttpResponseMessage> UpdateEventAsync(
            int id, EventCreateViewModel model, string token)
        {
            var request = CreateAuthorizedRequest(
                HttpMethod.Put, $"api/Event/{id}", token);

            request.Content = JsonContent.Create(new
            {
                eventName = model.EventName,
                eventLocation = model.EventLocation,
                eventDate = model.EventDate,
                categoryId = model.CategoryID
            });

            return await _httpClient.SendAsync(request);
        }

        // DELETE EVENT
        public async Task<HttpResponseMessage> DeleteEventAsync(
            int id, string token)
        {
            var request = CreateAuthorizedRequest(
                HttpMethod.Delete, $"api/Event/{id}", token);

            return await _httpClient.SendAsync(request);
        }
    }
}
