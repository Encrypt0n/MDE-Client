using MDE_Client.Application.Interfaces;
using MDE_Client.Application.Services;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Security.AccessControl;

namespace MDE_Client.Pages.Auth
{
    public class RegisterModel : PageModel
    {
        private readonly ILogger<RegisterModel> _logger;
        private readonly IAuthenticationService _authService;
        private readonly CompanyService _companyService;
        public RegisterModel(ILogger<RegisterModel> logger, IAuthenticationService authService, CompanyService companyService)
        {
            _logger = logger;
            _authService = authService;
            _companyService = companyService;
        }

        [BindProperty]
        public string Username { get; set; }

        [BindProperty]
        public string Password { get; set; }

        [BindProperty]
        public int SelectedCompanyId { get; set; }

        public List<SelectListItem> CompanyOptions { get; set; } = new();

        public string Message { get; set; }
        public async Task OnGet()
        {
            var companies = await _companyService.GetAllCompaniesAsync();
            CompanyOptions = companies.Select(c => new SelectListItem
            {
                Value = c.CompanyID.ToString(),
                Text = c.Name
            }).ToList();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var companies = await _companyService.GetAllCompaniesAsync();
            CompanyOptions = companies.Select(c => new SelectListItem
            {
                Value = c.CompanyID.ToString(),
                Text = c.Name
            }).ToList();

            if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
            {
                Message = "❌ Please enter both username and password.";
                return Page();
            }
            else
            {
                _authService.RegisterAsync(Username, Password, SelectedCompanyId);
            }

            var user = _authService.LoginAsync(Username, Password);



            Message = "❌ Invalid login credentials.";
            return Page();



        }
    }
}
