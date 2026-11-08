using RaceDay_part_3.ViewModels;
using System.Net.Http.Json;

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

        // GET ALL CATEGORIES
        public async Task<HttpResponseMessage> GetCategoriesAsync(
            string token)
        {
            var request = new HttpRequestMessage(
                HttpMethod.Get, "api/Category");

            request.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Bearer", token);

            return await _httpClient.SendAsync(request);
        }

        // GET CATEGORY BY ID
        public async Task<HttpResponseMessage> GetCategoryByIdAsync(
            int id, string token)
        {
            var request = new HttpRequestMessage(
                HttpMethod.Get, $"api/Category/{id}");

            request.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Bearer", token);

            return await _httpClient.SendAsync(request);
        }

        // CREATE CATEGORY
        public async Task<HttpResponseMessage> CreateCategoryAsync(
            CategoryCreateViewModel model, string token)
        {
            var request = new HttpRequestMessage(
                HttpMethod.Post, "api/Category");

            request.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Bearer", token);

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
            var request = new HttpRequestMessage(
                HttpMethod.Put, $"api/Category/{id}");

            request.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Bearer", token);

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
            var request = new HttpRequestMessage(
                HttpMethod.Delete, $"api/Category/{id}");

            request.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Bearer", token);

            return await _httpClient.SendAsync(request);
        }
    }
}