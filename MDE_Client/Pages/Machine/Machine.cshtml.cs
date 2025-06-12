using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MDE_Client.Application.Services;
using MDE_Client.Domain.Models;
using System.Collections.ObjectModel;
using System.Diagnostics;
using MDE_Client.Application;
using MDE_Client.Application.Interfaces;
using System.Text.Json;
using System.Text;

namespace MDE_Client.Pages.Machine
{
    public class MachineModel : PageModel
    {
        private readonly IMachineService _machineService;
        private readonly IDashboardService _dashboardService;
        private readonly IUserActivityService _userActivityService;
        private readonly IUserService _userService;
        private readonly ICompanyService _companyService;
        private readonly AuthSession _authSession;
        


        public MachineModel(IMachineService machineService, IDashboardService dashboardService, IUserActivityService userActivityService, ICompanyService companyService, IUserService userService, AuthSession authSession)
        {
            _machineService = machineService;
            _dashboardService = dashboardService;
            _userActivityService = userActivityService;
            _companyService = companyService;
            _userService = userService;
            _authSession = authSession;
        }

        [BindProperty(SupportsGet = true)]
        public int MachineId { get; set; }

        [BindProperty]
        public string SelectedPageUrl { get; set; }


        [BindProperty]
        public ObservableCollection<UserActivityLog> ActivityLogs { get; set; } = new();

        [BindProperty]
        public ObservableCollection<DashboardPage> DashboardPages { get; set; } = new();

        [BindProperty]
        public string PageName { get; set; }

        [BindProperty]
        public string PageURL { get; set; }

        public Domain.Models.Machine? Machine { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            ActivityLogs = await _userActivityService.GetActivitiesForMachineAsync(MachineId);


            Machine = await _machineService.GetMachineByIdAsync(MachineId);
            if (Machine == null)
                return NotFound();

            DashboardPages = await _dashboardService.GetDashboardPagesAsync(MachineId);
            return Page();
        }

        public async Task<IActionResult> OnPostSelectDashboardPageAsync()
        {
            if (_authSession.Role == "1" || _authSession.Role == "2" || _authSession.Role == "4")
            {
                await _machineService.UpdateMachineDashboardUrlAsync(MachineId, SelectedPageUrl);

                ActivityLogs = await _userActivityService.GetActivitiesForMachineAsync(MachineId); // 🔁 Add this
                DashboardPages = await _dashboardService.GetDashboardPagesAsync(MachineId);
                Machine = await _machineService.GetMachineByIdAsync(MachineId);

                return Page();
            } else
            {
                return Forbid(); // or RedirectToPage("/AccessDenied");
            }

            
        }



        public async Task<IActionResult> OnPostOpenDashboardAsync()
        {
            await _userActivityService.LogActivityAsync(new UserActivityLog
            {
                UserId = int.Parse(_authSession.UserId),
                MachineId = MachineId,
                Action = "OpenDashboard",
                Target = "Dashboard:FirstPage",
                IpAddress = Request.HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = Request.Headers["User-Agent"]
            });



            var dashboardUrl = await _dashboardService.GetFirstDashboardPageUrlAsync(MachineId);
            var url = $"{dashboardUrl}?token={_authSession.Token}&machineId={MachineId}";
            if (!string.IsNullOrWhiteSpace(url))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }

            return RedirectToPage(new { machineId = MachineId });
        }

        public async Task<IActionResult> OnPostAddPageAsync()
        {
            if (!string.IsNullOrWhiteSpace(PageName) && !string.IsNullOrWhiteSpace(PageURL))
            {
                await _dashboardService.AddDashboardPageAsync(MachineId, PageName, PageURL);
            }

            DashboardPages = await _dashboardService.GetDashboardPagesAsync(MachineId);
            Machine = await _machineService.GetMachineByIdAsync(MachineId);
            return Page();
        }

        public async Task<IActionResult> OnPostDeletePageAsync(int pageId)
        {
            if (pageId > 0)
            {
                await _dashboardService.DeleteDashboardPageAsync(pageId);
            }

            DashboardPages = await _dashboardService.GetDashboardPagesAsync(MachineId);
            Machine = await _machineService.GetMachineByIdAsync(MachineId);
            return Page();
        }

        public async Task<IActionResult> OnPostStartVpnAsync()
        {
            if (_authSession.Role == "1" || _authSession.Role == "2" || _authSession.Role == "4")
            {

                Machine = await _machineService.GetMachineByIdAsync(MachineId);
                if (Machine == null || string.IsNullOrWhiteSpace(Machine.IP))
                    return BadRequest("Invalid machine or IPC IP.");

                string ovpnConfig = GenerateOvpnConfig(Machine.Name, int.Parse(_authSession.UserId), Machine.Description ?? "", Machine.IP);

                byte[] zipBytes;
                string zipFileName;

                try
                {
                    Company company = await _companyService.GetCompanyByIdAsync(int.Parse(_authSession.CompanyId));
                    User user = await _userService.GetUserByIdAsync(int.Parse(_authSession.UserId));
                    var (fileContent, fileName) = await _machineService.GenerateConfigAsync(user.Username, ovpnConfig, company, true);

                    zipBytes = fileContent;
                    zipFileName = fileName;

                    var vpnCommand = new
                    {
                        Command = "start",
                        Username = _authSession.UserId,
                        ZipFileName = zipFileName,
                        ZipContentBase64 = Convert.ToBase64String(zipBytes),
                        TargetIPC = Machine.IP
                    };

                    return new JsonResult(vpnCommand);
                }
                catch (Exception ex)
                {
                    return BadRequest(new { error = $"Failed to generate VPN config: {ex.Message}" });
                }
            } else
            {
                return Forbid();
            }
        }




        private string GenerateOvpnConfig(string machineName, int userid, string description, string machineIp)
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
auth-user-pass ""C:\\Program Files\\OpenVPN\\config\\auth.txt""
route-nopull
route 192.168.0.0 255.255.255.0 {machineIp}
verb 3".Trim();
        }

    }
}
