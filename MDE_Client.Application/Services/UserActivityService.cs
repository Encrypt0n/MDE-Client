using System.Collections.ObjectModel;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using MDE_Client.Application.Interfaces;
using MDE_Client.Domain.Models;
using Microsoft.Extensions.Configuration;

namespace MDE_Client.Application.Services
{
    public class UserActivityService : IUserActivityService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly AuthSession _authSession;

        public UserActivityService(HttpClient httpClient, IConfiguration config, AuthSession authSession)
        {
            _httpClient = httpClient;
            _config = config;
            _httpClient.BaseAddress = new Uri(_config["Api:BaseUrl"]);
            _authSession = authSession;

            // Attach token to Authorization header
            if (!string.IsNullOrEmpty(_authSession.Token))
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", _authSession.Token);
            }
        }

        public async Task LogActivityAsync(UserActivityLog log)
        {
            await _httpClient.PostAsJsonAsync("api/activity", log);
        }

        public async Task<ObservableCollection<UserActivityLog>> GetActivitiesForMachineAsync(int machineId)
        {
            var response = await _httpClient.GetFromJsonAsync<ObservableCollection<UserActivityLog>>($"api/activity/{machineId}");
            return response ?? new ObservableCollection<UserActivityLog>();
        }

    }
}
