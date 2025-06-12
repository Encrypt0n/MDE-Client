using System.Collections.ObjectModel;

using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;
using MDE_Client.Application.Interfaces;
using System.Diagnostics;
using System.Text.Json;
using MDE_Client.Domain.Models;
using System.Net.Http.Headers;

namespace MDE_Client.Application.Services
{
    public class UserService: IUserService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly AuthSession _authSession;

        public UserService(HttpClient httpClient, IConfiguration config, AuthSession authSession)
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

        // ✅ Get all users
        public async Task<ObservableCollection<User>> GetAllUsersAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/users");
                if (!response.IsSuccessStatusCode)
                {
                    Debug.WriteLine($"[UserService] HTTP request failed: {response.StatusCode}");
                    return new ObservableCollection<User>();
                }

                var json = await response.Content.ReadAsStringAsync();
                Debug.WriteLine($"[UserService] Raw JSON: {json}");

                var users = JsonSerializer.Deserialize<ObservableCollection<User>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                Debug.WriteLine($"[UserService] Deserialized {users?.Count} users");
                return users ?? new ObservableCollection<User>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[UserService] Exception: {ex.Message}");
                return new ObservableCollection<User>();
            }
        }


        // ✅ Get user by ID (optional)
        public async Task<User?> GetUserByIdAsync(int userId)
        {
            var response = await _httpClient.GetFromJsonAsync<User>($"api/users/{userId}");
            return response;
        }
    }
}
