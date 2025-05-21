using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Configuration;

namespace MDE_Client.Application.Services
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
            Debug.WriteLine($"user: {username}, password: {password}");

            var credentials = new { Username = username, Password = password };
            var response = await _httpClient.PostAsJsonAsync("api/auth/login", credentials);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<TokenResponse>();
                _token = result?.Token;
                Debug.WriteLine("tokennn ", _token);

                if (IsTokenValid())
                {
                    Debug.WriteLine("token is valid");
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
                // Debugging if the token is being checked
                Debug.WriteLine("Starting token validation...");

                var publicKeyPath = "Keys/public.key";

                // Check if the public key file exists and log it
                if (!File.Exists(publicKeyPath))
                {
                    Debug.WriteLine("Public key file not found.");
                    throw new FileNotFoundException("Public key not found.");
                }

                // Read the public key content
                var publicKeyPem = File.ReadAllText(publicKeyPath);
                Debug.WriteLine("Public key loaded from: " + publicKeyPath);

                // Create RSA instance and load the public key
                using var rsa = RSA.Create();
                rsa.ImportFromPem(publicKeyPem);
                Debug.WriteLine("Public key successfully imported.");

                // Initialize the JWT handler
                var handler = new JsonWebTokenHandler();

                // Log the token being validated (this can be sensitive, be careful in production environments)
                Debug.WriteLine("Validating token: " + _token.Substring(0, 50) + "...");  // Show the first 50 chars for visibility

                // Validate the token with the defined parameters
                var result = handler.ValidateToken(_token, new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidIssuer = _config["Jwt:Issuer"],
                    ValidAudience = _config["Jwt:Audience"],
                    IssuerSigningKey = new RsaSecurityKey(rsa),
                    ValidateLifetime = true
                });

                // Log result of validation
                Debug.WriteLine("Token validation completed. Valid: " + result.IsValid);

                // Return the validity of the token
                return result.IsValid;
            }
            catch (Exception ex)
            {
                // Log the exception for debugging purposes
                Debug.WriteLine("Error during token validation: " + ex.Message);
                Debug.WriteLine("Stack Trace: " + ex.StackTrace);
                return false;
            }
        }

    }
    public class TokenResponse
    {
        public string Token { get; set; }
    }
}
