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
using Microsoft.AspNetCore.Components;

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
        [BindProperty]
        public string DashboardUrlWithToken { get; set; }

        public string DashboardBaseUrl { get; set; }
        public Dictionary<string, string> DashboardQueryParams { get; set; } = new();

        public Domain.Models.Machine? Machine { get; set; }

        public string JwtToken => _authSession?.Token ?? "";


        public async Task<IActionResult> OnGetAsync()
        {
            ActivityLogs = await _userActivityService.GetActivitiesForMachineAsync(MachineId);


            Machine = await _machineService.GetMachineByIdAsync(MachineId);
            if (Machine == null)
                return NotFound();

            // ↓ Laat de property nooit op null eindigen
            var pages = await _dashboardService.GetDashboardPagesAsync(MachineId);
            DashboardPages = pages ?? new ObservableCollection<DashboardPage>();




            //Debug.WriteLine("urllllll ", DashboardUrlWithToken);
            return Page();
        }

        public async Task<IActionResult> OnPostSelectDashboardPageAsync()
        {
            if (_authSession.Role == "1" || _authSession.Role == "2" || _authSession.Role == "4")
            {
                await _machineService.UpdateMachineDashboardUrlAsync(MachineId, SelectedPageUrl);

                ActivityLogs = await _userActivityService.GetActivitiesForMachineAsync(MachineId); // 🔁 Add this
                var pages = await _dashboardService.GetDashboardPagesAsync(MachineId);
                DashboardPages = pages ?? new ObservableCollection<DashboardPage>();
                Machine = await _machineService.GetMachineByIdAsync(MachineId);

                return Page();
            } else
            {
                return Forbid(); // or RedirectToPage("/AccessDenied");
            }

            
        }

        public async Task<IActionResult> OnPostLogoutAsync()
        {
            // Clear your session, cookie, or auth logic here
            _authSession.Clear(); // or HttpContext.SignOutAsync(...) if you're using ASP.NET Identity

            return RedirectToPage("/Auth/Login"); // or your login/home page
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
            //var url = $"{dashboardUrl}";
            var url = $"{dashboardUrl}?token={Uri.EscapeDataString(_authSession.Token)}";
            if (!string.IsNullOrWhiteSpace(url))
            {
                //return Redirect(url);
                // Inject NavigationManager as _navigationManager

                // var token = "your_jwt_token_here";


                var encodedToken = Uri.EscapeDataString(_authSession.Token);
                var finalUrl = $"{dashboardUrl}?token={encodedToken}";
                return new JsonResult(new { url = finalUrl });
                //HttpContext.Response.Redirect($"{dashboardUrl}?token={_authSession.Token}", false);

                //return new EmptyResult();


            }



            return null;
           // return RedirectToPage(new { machineId = MachineId });
        }

        public async Task<IActionResult> OnPostOpenVNCAsync()
        {
            await _userActivityService.LogActivityAsync(new UserActivityLog
            {
                UserId = int.Parse(_authSession.UserId),
                MachineId = MachineId,
                Action = "OpenVNC",
                Target = "VNC:MainPage",
                IpAddress = Request.HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = Request.Headers["User-Agent"]
            });


            var Url = await _dashboardService.GetFirstDashboardPageUrlAsync(MachineId);
            var uri = new Uri(Url);

            var hostParts = uri.Host.Split('.', 2); // "machine1", "mde-portal.site"
            var newHost = $"{hostParts[0]}-vnc.{hostParts[1]}";

            // Rebuild the stripped and modified URL
            var strippedUrl = $"{uri.Scheme}://{newHost}:{uri.Port}/";
            Debug.WriteLine(strippedUrl);

            //var cameraUrl = $"{strippedUrl}?token={Uri.EscapeDataString(_authSession.Token)}";
            if (!string.IsNullOrWhiteSpace(strippedUrl))
            {
                //return Redirect(url);
                // Inject NavigationManager as _navigationManager

                // var token = "your_jwt_token_here";


                var encodedToken = Uri.EscapeDataString(_authSession.Token);
                var finalUrl = $"{strippedUrl}vnc.html?token={encodedToken}&host={newHost}/&port={uri.Port}&path=websockify?token={encodedToken}";
                return new JsonResult(new { url = finalUrl });
                //HttpContext.Response.Redirect($"{dashboardUrl}?token={_authSession.Token}", false);

                //return new EmptyResult();


            }



            return null;
            // return RedirectToPage(new { machineId = MachineId });
        }

        public async Task<IActionResult> OnPostOpenCameraAsync()
        {
            await _userActivityService.LogActivityAsync(new UserActivityLog
            {
                UserId = int.Parse(_authSession.UserId),
                MachineId = MachineId,
                Action = "OpenCamera",
                Target = "Camera:MainPage",
                IpAddress = Request.HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = Request.Headers["User-Agent"]
            });


            var Url = await _dashboardService.GetFirstDashboardPageUrlAsync(MachineId);
            var uri = new Uri(Url);

            var hostParts = uri.Host.Split('.', 2); // "machine1", "mde-portal.site"
            var newHost = $"{hostParts[0]}-camera.{hostParts[1]}";

            // Rebuild the stripped and modified URL
            var strippedUrl = $"{uri.Scheme}://{newHost}:{uri.Port}/";
            Debug.WriteLine(strippedUrl);

            //var cameraUrl = $"{strippedUrl}?token={Uri.EscapeDataString(_authSession.Token)}";
            if (!string.IsNullOrWhiteSpace(strippedUrl))
            {
                //return Redirect(url);
                // Inject NavigationManager as _navigationManager

                // var token = "your_jwt_token_here";


                var encodedToken = Uri.EscapeDataString(_authSession.Token);
                var finalUrl = $"{strippedUrl}?token={encodedToken}";
                return new JsonResult(new { url = finalUrl });

                //HttpContext.Response.Redirect($"{dashboardUrl}?token={_authSession.Token}", false);

                //return new EmptyResult();


            }



            return null;
            // return RedirectToPage(new { machineId = MachineId });
        }

        public async Task<IActionResult> OnPostAddPageAsync()
        {
            if (!string.IsNullOrWhiteSpace(PageName) && !string.IsNullOrWhiteSpace(PageURL))
            {
                await _dashboardService.AddDashboardPageAsync(MachineId, PageName, PageURL);
            }

            var pages = await _dashboardService.GetDashboardPagesAsync(MachineId);
            DashboardPages = pages ?? new ObservableCollection<DashboardPage>();
            Machine = await _machineService.GetMachineByIdAsync(MachineId);
            return Page();
        }

        public async Task<IActionResult> OnPostDeletePageAsync(int pageId)
        {
            if (pageId > 0)
            {
                await _dashboardService.DeleteDashboardPageAsync(pageId);
            }

            var pages = await _dashboardService.GetDashboardPagesAsync(MachineId);
            DashboardPages = pages ?? new ObservableCollection<DashboardPage>();
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
            var octets = machineIp.Split('.');
            if (octets.Length != 4)
                throw new ArgumentException($"Invalid IPv4 address: {machineIp}");

            int thirdOctet = int.Parse(octets[2]);   // e.g. 20
            int fourthOctet = int.Parse(octets[3]);   // e.g. 12

            return $@"client
dev tun
proto udp4
route {thirdOctet}.{fourthOctet}.1.0 255.255.255.0 {machineIp}
route 192.168.0.0 255.255.255.0 {machineIp}
route {thirdOctet}.{fourthOctet}.2.0 255.255.255.0 {machineIp}
route 192.168.250.0 255.255.255.0 {machineIp}
remote 217.63.76.110 1195
resolv-retry infinite
nobind
persist-key
ca ""C:\\OpenVPN\\config\\ca.crt""
cert ""C:\\OpenVPN\\config\\client.crt""
key ""C:\\OpenVPN\\config\\client.key""
tls-auth ""C:\\OpenVPN\\config\\ta.key"" 1
key-direction 1
cipher AES-256-GCM
auth SHA256
tls-version-min 1.2
remote-cert-tls server
auth-user-pass ""C:\\OpenVPN\\config\\auth.txt""
verb 3".Trim();
        }

    }
}
