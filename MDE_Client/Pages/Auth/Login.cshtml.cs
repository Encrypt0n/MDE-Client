using MDE_Client.Domain.Models;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using MDE_Client.Application.Services;
using MDE_Client.Application;
using MDE_Client.Application.Interfaces;

namespace MDE_Client.Pages.Auth
{
    public class LoginModel : PageModel
    {
        private readonly ILogger<LoginModel> _logger;
        private readonly IAuthenticationService _authService;
        private readonly AuthSession _authSession;

        public LoginModel(ILogger<LoginModel> logger, IAuthenticationService authService, AuthSession authSession)
        {
            _logger = logger;
            _authService = authService;
            _authSession = authSession;
        }

        [BindProperty]
        public string Username { get; set; }

        [BindProperty]
        public string Password { get; set; }

        public string Message { get; set; }

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
            {
                Message = "❌ Please enter both username and password.";
                return Page();
            }
            await _authService.Logout();
            var success = await _authService.LoginAsync(Username, Password);
            if (success)
            {
                _authSession.ParseToken();
                // now _authSession.UserId is set, and you can redirect or load user-specific data
                _logger.LogInformation($"✅ User {Username} successfully logged in.");

                return RedirectToPage("/Machine/Machines");
            }
            

            Message = "❌ Invalid login credentials.";
            return Page();
        }
    }
}
