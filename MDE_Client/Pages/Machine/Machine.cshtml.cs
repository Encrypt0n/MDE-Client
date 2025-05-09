using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.ObjectModel;
using System.Diagnostics;
using MDE_Client.Domain.Models;
using MDE_Client.Application.Services;
using System.Net.Http;

namespace MDE_Client.Pages.Machine
{
    public class MachineModel : PageModel
    {
        private readonly MachineService _machineService;
        private readonly DashboardService _dashboardService;

        private string machineIP;

        public MachineModel(MachineService machineService, DashboardService dashboardService)
        {
            _machineService = machineService;
            _dashboardService = dashboardService;
        }

        [BindProperty]
        public ObservableCollection<Domain.Models.Machine> Machines { get; set; } = new ObservableCollection<Domain.Models.Machine>();

        [BindProperty]
        public ObservableCollection<DashboardPage> DashboardPages { get; set; } = new ObservableCollection<DashboardPage>();

        [BindProperty]
        public int SelectedMachineId { get; set; }

        [BindProperty]
        public string PageName { get; set; }

        [BindProperty]
        public string PageURL { get; set; }


        public void OnGet(int userId)
        {
            Machines = _machineService.GetMachinesForUser(userId);
        }

        public IActionResult OnPostSelectMachine()
        {
            Machines = _machineService.GetMachinesForUser(1); // Reload Machines list

            if (SelectedMachineId > 0)
            {
                var selectedMachine = _machineService.GetMachineById(SelectedMachineId);
                machineIP = selectedMachine.IP;

                if (selectedMachine != null)
                {
                    DashboardPages = _dashboardService.GetDashboardPages(SelectedMachineId);
                }
            }

            return Page();
        }

        public IActionResult OnPostOpenDashboard()
        {
            Debug.WriteLine("goginggggg");
            Debug.WriteLine("machineid" + SelectedMachineId);
            if (SelectedMachineId > 0)
            {
                string firstPageUrl = _dashboardService.GetFirstDashboardPageUrl(SelectedMachineId);
                Debug.WriteLine("firstpage" + firstPageUrl);

                if (!string.IsNullOrWhiteSpace(firstPageUrl))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = firstPageUrl,
                        UseShellExecute = true
                    });
                }
            }

            return Page();
        }

        public async Task<IActionResult> OnPostStartVpnAsync()
        {
            /*Machines = _machineService.GetMachinesForUser(1); // Reload Machines list

            var selectedMachine = _machineService.GetMachineById(SelectedMachineId);
            if (selectedMachine == null)
                return Page();*/

            var vpnPayload = new
            {
                ovpnPath = @"C:\vpn\config.ovpn", // static path as per your setup
                ipcVpnIP = machineIP,    // use machine's IP as IPC target
                localAliasIP = "192.168.100.10",  // static alias IP or generate dynamically
                portForwards = new[]
                {
            new { from = 1883, to = 1883 },
            new { from = 2222, to = 22 }
        }
            };

            try
            {
                using var http = new HttpClient();
                var response = await http.PostAsJsonAsync("http://localhost:5099/connect-to-ipc", vpnPayload);
                if (!response.IsSuccessStatusCode)
                {
                    Debug.WriteLine("VPN connection failed: " + await response.Content.ReadAsStringAsync());
                }
                else
                {
                    Debug.WriteLine("VPN connected successfully.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("VPN error: " + ex.Message);
            }

           // DashboardPages = _dashboardService.GetDashboardPages(SelectedMachineId);
            return Page();
        }


        public IActionResult OnPostAddPage()
        {
            Machines = _machineService.GetMachinesForUser(1); // Reload Machines list

            if (SelectedMachineId > 0 && !string.IsNullOrWhiteSpace(PageName) && !string.IsNullOrWhiteSpace(PageURL))
            {
                var selectedMachine = _machineService.GetMachineById(SelectedMachineId);
                string ip = selectedMachine.IP;
                PageURL = "http://" + ip + ":1880/" + PageURL;
                _dashboardService.AddDashboardPage(SelectedMachineId, PageName, PageURL);
                DashboardPages = _dashboardService.GetDashboardPages(SelectedMachineId);
            }

            return Page();
        }

        public IActionResult OnPostDeletePage(int pageId)
        {
            if (pageId > 0)
            {
                _dashboardService.DeleteDashboardPage(pageId);
                DashboardPages = _dashboardService.GetDashboardPages(SelectedMachineId);
            }

            return Page();
        }
    }
}
