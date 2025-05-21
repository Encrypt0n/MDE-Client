using System.Collections.ObjectModel;
using Microsoft.Data.SqlClient;
using System.Reflection.PortableExecutable;
using MDE_Client.Domain.Models;
using System.Diagnostics;
using System.Windows;
using System.Security.Cryptography;

using System.Reflection;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;

namespace MDE_Client.Application.Services
{
    public class MachineService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;

        public MachineService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _config = config;
            _httpClient.BaseAddress = new Uri(_config["Api:BaseUrl"]);
        }

        // ✅ Get all machines for a user
        public async Task<ObservableCollection<Domain.Models.Machine>> GetMachinesForUserAsync(int userId)
        {
            var response = await _httpClient.GetFromJsonAsync<ObservableCollection<Domain.Models.Machine>>($"api/machines/user/{userId}");
            return response ?? new ObservableCollection<Domain.Models.Machine>();
        }

        // ✅ Get a specific machine by its ID
        public async Task<Domain.Models.Machine> GetMachineByIdAsync(int machineId)
        {
            var response = await _httpClient.GetFromJsonAsync<Domain.Models.Machine>($"api/machines/{machineId}");
            return response;
        }
    }
}
