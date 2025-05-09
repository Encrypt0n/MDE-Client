using MDE_Client.Controllers;
using MDE_Client.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace MDE_Client.Pages.Auth
{
    public class RegisterModel : PageModel
    {
        private readonly ILogger<RegisterModel> _logger;
        private readonly AuthController _authController;
        public RegisterModel(ILogger<RegisterModel> logger, AuthController authController) {
            _logger = logger;
            _authController = authController;
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
            } else
            {
                _authController.Register(Username, Password);
            }

            var user = _authController.Login(Username, Password);

            

            Message = "❌ Invalid login credentials.";
            return Page();
        }
    }
}
