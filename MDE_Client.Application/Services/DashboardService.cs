using System.Collections.ObjectModel;
using Microsoft.Data.SqlClient;
using MDE_Client.Domain.Models;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;
using MDE_Client.Application.Interfaces;
using System.Net.Http.Headers;

namespace MDE_Client.Application.Services
{
    public class DashboardService: IDashboardService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly AuthSession _authSession;

        public DashboardService(HttpClient httpClient, IConfiguration config, AuthSession authSession)
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

        // ✅ Get all dashboard pages
        public async Task<ObservableCollection<DashboardPage>> GetDashboardPagesAsync(int machineId)
        {
            var response = await _httpClient.GetFromJsonAsync<ObservableCollection<DashboardPage>>($"api/dashboard/{machineId}");
            return response ?? new ObservableCollection<DashboardPage>();
        }

        // ✅ Add a new dashboard page
        public async Task AddDashboardPageAsync(int machineId, string pageName, string pageUrl)
        {
            var payload = new
            {
                MachineId = machineId,
                PageName = pageName,
                PageUrl = pageUrl
            };

            var response = await _httpClient.PostAsJsonAsync("api/dashboard/", payload);
            response.EnsureSuccessStatusCode();
        }

        // ✅ Delete a dashboard page
        public async Task DeleteDashboardPageAsync(int pageId)
        {
            var response = await _httpClient.DeleteAsync($"api/dashboard/{pageId}");
            response.EnsureSuccessStatusCode();
        }

        // ✅ Get the default dashboard page URL
        public async Task<string> GetFirstDashboardPageUrlAsync(int machineId)
        {
            var url = $"api/dashboard/default-url/{machineId}";
            var response = await _httpClient.GetStringAsync(url);
            return response ?? string.Empty;
        }
    }
}
