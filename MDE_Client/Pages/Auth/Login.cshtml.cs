using MDE_Client.Models;
using MDE_Client.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using MDE_Client.Services;

namespace MDE_Client.Pages.Auth
{
    public class LoginModel : PageModel
    {
        private readonly ILogger<LoginModel> _logger;
        private readonly AuthenticationService _authService;

        public LoginModel(ILogger<LoginModel> logger, AuthenticationService authService)
        {
            _logger = logger;
            _authService = authService;
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

            var user = _authService.LoginAsync(Username, Password);

            if (user != null)
            {
                _logger.LogInformation($"✅ User {Username} successfully logged in.");
                return RedirectToPage("/Machine/Machine");
            }

            Message = "❌ Invalid login credentials.";
            return Page();
        }
    }
}
