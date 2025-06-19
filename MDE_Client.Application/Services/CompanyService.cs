using MDE_Client.Application.Interfaces;
using MDE_Client.Domain.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace MDE_Client.Application.Services
{
    

    public class CompanyService: ICompanyService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly AuthSession _authSession;

        public CompanyService(HttpClient httpClient, IConfiguration config, AuthSession authSession)
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

        public async Task<List<Company>> GetAllCompaniesAsync()
        {
            var response = await _httpClient.GetFromJsonAsync<List<Company>>("api/companies");
     
            return response ?? new List<Company>();
        }

        public async Task<Company> GetCompanyByIdAsync(int companyId)
        {
            var response = await _httpClient.GetFromJsonAsync<Company>($"api/companies/{companyId}");
            return response;
        }

        public async Task AddCompanyAsync(string name, string description)
        {
            var payload = new
            {
                Name = name,
                Description = description
              
            };

            var response = await _httpClient.PostAsJsonAsync("api/companies/", payload);
            response.EnsureSuccessStatusCode();
        }


    }

}
