using RaceDay_part_3.ViewModels;

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
    }
}
