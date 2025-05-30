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
using System.IO.Compression;

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
        public async Task<ObservableCollection<Domain.Models.Machine>> GetMachinesForCompanyAsync(int companyId)
        {
            var response = await _httpClient.GetFromJsonAsync<ObservableCollection<Domain.Models.Machine>>($"api/machines/company/{companyId}");
            return response ?? new ObservableCollection<Domain.Models.Machine>();
        }

        // ✅ Get a specific machine by its ID
        public async Task<Domain.Models.Machine> GetMachineByIdAsync(int machineId)
        {
            var response = await _httpClient.GetFromJsonAsync<Domain.Models.Machine>($"api/machines/{machineId}");
            return response;
        }

        public async Task<(byte[] fileContent, string fileName)> GenerateUserConfigAsync(string machineName, string ovpn, Company company)
        {
            var requestUri = $"api/vpn/generate-user/{machineName}/{company.Name}/{company.Subnet}";
            var response = await _httpClient.GetAsync(requestUri);

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Failed to generate client config: {response.StatusCode}");

            var zipBytes = await response.Content.ReadAsByteArrayAsync();
            var fileName = response.Content.Headers.ContentDisposition?.FileName?.Trim('"') ?? $"{machineName}.zip";

            using var zipStream = new MemoryStream();
            zipStream.Write(zipBytes, 0, zipBytes.Length);
            zipStream.Position = 0;

            using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Update, leaveOpen: true))
            {
                // Replace client.ovpn
                var existingOvpnEntry = archive.GetEntry("client.ovpn");
                existingOvpnEntry?.Delete();

                var ovpnEntry = archive.CreateEntry("client.ovpn");
                using (var entryStream = ovpnEntry.Open())
                using (var writer = new StreamWriter(entryStream))
                {
                    await writer.WriteAsync(ovpn);
                    await writer.FlushAsync();
                }


            }

            zipStream.Position = 0;
            var updatedBytes = zipStream.ToArray();

            return (updatedBytes, fileName);
        }

        public async Task<(byte[] fileContent, string fileName)> GenerateClientConfigAsync(string machineName, string ovpn, Company company)
        {
            var requestUri = $"api/vpn/generate-machine/{machineName}/{company.Name}/{company.Subnet}";
            var response = await _httpClient.GetAsync(requestUri);

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Failed to generate client config: {response.StatusCode}");

            var zipBytes = await response.Content.ReadAsByteArrayAsync();
            var fileName = response.Content.Headers.ContentDisposition?.FileName?.Trim('"') ?? $"{machineName}.zip";

            using var zipStream = new MemoryStream();
            zipStream.Write(zipBytes, 0, zipBytes.Length);
            zipStream.Position = 0;

            using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Update, leaveOpen: true))
            {
                // Replace client.ovpn
                var existingOvpnEntry = archive.GetEntry("client.ovpn");
                existingOvpnEntry?.Delete();

                var ovpnEntry = archive.CreateEntry("client.ovpn");
                using (var entryStream = ovpnEntry.Open())
                using (var writer = new StreamWriter(entryStream))
                {
                    await writer.WriteAsync(ovpn);
                    await writer.FlushAsync();
                }

                
            }

            zipStream.Position = 0;
            var updatedBytes = zipStream.ToArray();

            return (updatedBytes, fileName);
        }





        public async Task UpdateMachineDashboardUrlAsync(int machineId, string dashboardUrl)
        {
            var content = JsonContent.Create(new { DashboardUrl = dashboardUrl });
            var response = await _httpClient.PostAsync($"api/machines/{machineId}/dashboard-url", content);
            response.EnsureSuccessStatusCode();
        }


    }
}
