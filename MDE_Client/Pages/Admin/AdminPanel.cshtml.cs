using MDE_Client.Application;
using MDE_Client.Application.Interfaces;
using MDE_Client.Application.Services;
using MDE_Client.Domain.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Text;

namespace MDE_Client.Pages.Admin
{
    public class AdminPanelModel : PageModel
    {
        private readonly IUserService _userService;
        private readonly IMachineService _machineService;
        private readonly ICompanyService _companyService;
        private readonly AuthSession _authSession;

        public AdminPanelModel(IUserService userService, IMachineService machineService, ICompanyService companyService, AuthSession authSession)
        {
            _userService = userService;
            _machineService = machineService;
            _companyService = companyService;
            _authSession = authSession;
        }

        [BindProperty]
        public int SelectedCompanyId { get; set; }

        [BindProperty]
        public string MachineName { get; set; } = string.Empty;

        [BindProperty]
        public string CompanyName { get; set; } = string.Empty;

        [BindProperty]
        public string CompanyDescription { get; set; } = string.Empty;


        [BindProperty]
        public string Description { get; set; } = string.Empty;

        [BindProperty]
        public string? OvpnContent { get; set; }

        public List<SelectListItem> Companies { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {

            if (_authSession.Role != "1")
            {
                return Forbid(); // or RedirectToPage("/AccessDenied")
            }
            await LoadCompaniesAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostLogoutAsync()
        {
            // Clear your session, cookie, or auth logic here
            _authSession.Clear(); // or HttpContext.SignOutAsync(...) if you're using ASP.NET Identity

            return RedirectToPage("/Auth/Login"); // or your login/home page
        }


        public async Task<IActionResult> OnPostCreateCompanyAsync()
        {
            if (_authSession.Role != "1")
            {
                return Forbid(); // or RedirectToPage("/AccessDenied")
            }

            await LoadCompaniesAsync();

            if (string.IsNullOrWhiteSpace(CompanyName))
            {
                ModelState.AddModelError("", "Company name is required.");
                return Page();
            }

           

            try
            {
                await _companyService.AddCompanyAsync(CompanyName, CompanyDescription);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Failed to create company: {ex.Message}");
                return Page();
            }

            // Optionally clear the fields after successful creation
            CompanyName = string.Empty;
            CompanyDescription = string.Empty;

            return RedirectToPage(); // Reload the page
        }


        

        public async Task<IActionResult> OnPostDownloadGeneratedAsync()
        {
            if (_authSession.Role == "1")
            {



                await LoadCompaniesAsync();

                if (SelectedCompanyId == 0 || string.IsNullOrEmpty(MachineName))
                {
                    ModelState.AddModelError("", "User and machine name must be provided.");
                    return Page();
                }
                //User user = await _userService.GetUserByIdAsync(SelectedCompanyId);
                Company company = await _companyService.GetCompanyByIdAsync(SelectedCompanyId);
                MachineName = MachineName.Replace(" ", "-");
                string ovpn = GenerateOvpnConfig(company.Name, MachineName, company.CompanyID, Description);

                try
                {


                    
                    var (fileContent, fileName) = await _machineService.GenerateConfigAsync(MachineName, ovpn, company, false);
                    return File(fileContent, "application/zip", fileName);
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Failed to generate VPN config: {ex.Message}");
                    return Page();
                }
            } else
            {
                return Forbid(); // or RedirectToPage("/AccessDenied")
            }
        }

        private async Task LoadCompaniesAsync()
        {

            var companies = await _companyService.GetAllCompaniesAsync();
            Companies = companies.Select(u => new SelectListItem
            {
                Value = u.CompanyID.ToString(),
                Text = u.Name
            }).ToList();
        }

        private string GenerateOvpnConfig(string companyName, string machineName, int companyId, string description)
        {
            return $@"client
dev tun
proto udp4
remote 217.63.76.110 1195
resolv-retry infinite
nobind
persist-key
persist-tun
ca ""C:\\Program Files\\OpenVPN\\config\\ca.crt""
cert ""C:\\Program Files\\OpenVPN\\config\\client.crt""
key ""C:\\Program Files\\OpenVPN\\config\\client.key""
tls-auth ""C:\\Program Files\\OpenVPN\\config\\ta.key"" 1
key-direction 1
cipher AES-256-GCM
auth SHA256
tls-version-min 1.2
remote-cert-tls server
setenv UV_CLIENT_COMPANY_ID {companyId}
setenv UV_CLIENT_NAME ""{companyName}-{machineName}""
setenv UV_CLIENT_DESCRIPTION ""{description}""
push-peer-info
auth-user-pass ""C:\\Program Files\\OpenVPN\\config\\auth.txt""
route 10.8.0.0 255.255.255.0
verb 3".Trim();
        }
    }
}
