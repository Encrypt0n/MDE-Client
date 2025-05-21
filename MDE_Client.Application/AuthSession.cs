using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using Microsoft.IdentityModel.JsonWebTokens;
using System.Diagnostics;
using MDE_Client.Application.Services;

namespace MDE_Client.Application
{
    public class AuthSession
    {
        private readonly AuthenticationService _authService;
        private readonly IConfiguration _config;

        public string? Token => _authService.GetToken();
        public string? UserId { get; private set; }
        public bool IsAuthenticated => !string.IsNullOrEmpty(UserId);

        public AuthSession(AuthenticationService authService, IConfiguration config)
        {
            _authService = authService;
            _config = config;
            ParseToken();
        }

        public void ParseToken()
        {
            var token = _authService.GetToken();
            if (string.IsNullOrWhiteSpace(token)) return;

            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jwt = handler.ReadJwtToken(token);

                UserId = 
                      jwt.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;

                Debug.WriteLine("Parsed userId from token: " + UserId);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error parsing token: " + ex.Message);
                UserId = null;
            }
        }

        public void Clear()
        {
            UserId = null;
        }
    }
}
