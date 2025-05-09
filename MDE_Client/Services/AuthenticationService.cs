using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace MDE_Client.Services
{
    public class AuthenticationService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private string _token;

        public AuthenticationService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _config = config;
            _httpClient.BaseAddress = new Uri(_config["Api:BaseUrl"]);
        }

        public async Task<bool> LoginAsync(string username, string password)
        {
            var credentials = new { Username = username, Password = password };
            var response = await _httpClient.PostAsJsonAsync("api/auth/login", credentials);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<TokenResponse>();
                _token = result?.Token;

                if (IsTokenValid())
                {
                    _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);
                    return true;
                }
            }
            return false;
        }

        public async Task<bool> RegisterAsync(string username, string password)
        {
            var data = new { Username = username, Password = password };
            var response = await _httpClient.PostAsJsonAsync("api/auth/register", data);
            return response.IsSuccessStatusCode;
        }

        public string GetToken() => _token;

        public bool IsTokenValid()
        {
            try
            {
                var publicKeyPath = "Keys/public.key";

                if (!File.Exists(publicKeyPath))
                    throw new FileNotFoundException("Public key not found.");

                var publicKeyPem = File.ReadAllText(publicKeyPath);

                using var rsa = RSA.Create();
                rsa.ImportFromPem(publicKeyPem);

                var handler = new JsonWebTokenHandler();
                var result = handler.ValidateToken(_token, new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidIssuer = _config["Jwt:Issuer"],
                    ValidAudience = _config["Jwt:Audience"],
                    IssuerSigningKey = new RsaSecurityKey(rsa),
                    ValidateLifetime = true
                });

                return result.IsValid;
            }
            catch
            {
                return false;
            }
        }
    }

    public class TokenResponse
    {
        public string Token { get; set; }
    }
}
