using MDE_Client.Application.Services;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace MDE_Client.Pages.Auth
{
    public class RegisterModel : PageModel
    {
        private readonly ILogger<RegisterModel> _logger;
        private readonly AuthenticationService _authService;
        public RegisterModel(ILogger<RegisterModel> logger, AuthenticationService authService)
        {
            _logger = logger;
            _authService = authService;
        }

        [BindProperty]
        public string Username { get; set; }

        [BindProperty]
        public string Password { get; set; }

        public string Message { get; set; }
        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
            {
                Message = "❌ Please enter both username and password.";
                return Page();
            }
            else
            {
                _authService.RegisterAsync(Username, Password);
            }

            var user = _authService.LoginAsync(Username, Password);



            Message = "❌ Invalid login credentials.";
            return Page();



        }
    }
}
