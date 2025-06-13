using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Configuration;
using MDE_Client.Application.Interfaces;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;

namespace MDE_Client.Application.Services
{
    public class AuthenticationService: IAuthenticationService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private string _token;
        private readonly ILogger<AuthenticationService> _logger;

        public AuthenticationService(HttpClient httpClient, IConfiguration config, ILogger<AuthenticationService> logger)
        {
            _httpClient = httpClient;
            _config = config;
            _httpClient.BaseAddress = new Uri(_config["Api:BaseUrl"]);
            _logger = logger;
        }

        public async Task Logout()
        {
            _token = null;
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }


        public async Task<bool> LoginAsync(string username, string password)
        {
            // Clear old token just to be safe
            _token = null;
            _httpClient.DefaultRequestHeaders.Authorization = null;

            var credentials = new { Username = username, Password = password };
            var response = await _httpClient.PostAsJsonAsync("api/auth/login", credentials);
            _logger.LogInformation($"responseeee {response}");

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<TokenResponse>();
                if (result?.Token == null)
                {
                    _logger.LogWarning("No token returned from server.");
                    return false;
                }

                _token = result.Token;

                // Set Authorization header immediately after receiving the token
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);

                _logger.LogInformation($"Token received: {_token}");

                await Task.Delay(200);
                var principle = ValidateToken(_token);

                // Now check token validity
                if (principle != null)
                {
                    Debug.WriteLine("✅ Token is valid.");
                    return true;
                }
                else
                {
                    Debug.WriteLine("❌ Token is invalid.");
                }
            }

            return false;
        }



        public async Task<bool> RegisterAsync(string username, string password, int companyId)
        {
            var data = new { Username = username, Password = password, CompanyID = companyId };
            var response = await _httpClient.PostAsJsonAsync("api/auth/register", data);
            return response.IsSuccessStatusCode;
        }

        public string GetToken() => _token;

        public ClaimsPrincipal? ValidateToken(string token)
        {
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

            var handler = new JwtSecurityTokenHandler();
            try
            {
                var parameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = _config["Jwt:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = _config["Jwt:Audience"],
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new RsaSecurityKey(rsa),
                    ValidateLifetime = true,
                    // ClockSkew = TimeSpan.FromMinutes(2)
                };

                var principal = handler.ValidateToken(token, parameters, out _);
                return principal;
            }
            catch
            {
                return null;
            }
        }

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

                //var handler = new JwtSecurityTokenHandler();
                //handler.ReadToken(_token);
                var jwt = handler.ReadToken(_token);
               // Console.WriteLine("nbf: " + jwt.);
                Debug.WriteLine("iat: " + jwt.ValidFrom);
                Debug.WriteLine("exp: " + jwt.ValidTo);


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
                    ValidateLifetime = true,

                    ClockSkew = TimeSpan.FromMinutes(2)
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
