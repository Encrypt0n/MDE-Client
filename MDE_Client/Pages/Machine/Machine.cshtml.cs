using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MDE_Client.Application.Services;
using MDE_Client.Domain.Models;
using System.Collections.ObjectModel;
using System.Diagnostics;
using MDE_Client.Application;
using MDE_Client.Application.Interfaces;

namespace MDE_Client.Pages.Machine
{
    public class MachineModel : PageModel
    {
        private readonly MachineService _machineService;
        private readonly DashboardService _dashboardService;
        private readonly IUserActivityService _userActivityService;
        private readonly AuthSession _authSession;
        


        public MachineModel(MachineService machineService, DashboardService dashboardService, IUserActivityService userActivityService, AuthSession authSession)
        {
            _machineService = machineService;
            _dashboardService = dashboardService;
            _userActivityService = userActivityService;
            _authSession = authSession;
        }

        [BindProperty(SupportsGet = true)]
        public int MachineId { get; set; }

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



            var url = await _dashboardService.GetFirstDashboardPageUrlAsync(MachineId);
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
            // Implement VPN launch logic if applicable
            // e.g. await _vpnService.StartVpnForMachine(MachineId);
            return RedirectToPage(new { machineId = MachineId });
        }
    }
}
