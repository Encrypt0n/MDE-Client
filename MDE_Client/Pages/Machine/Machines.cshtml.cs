using Microsoft.AspNetCore.Mvc.RazorPages;
using MDE_Client.Application.Services;
using MDE_Client.Domain.Models;
using System.Collections.Generic;
using MDE_Client.Application;
using System.Net.Http.Headers;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc;
using System.Reflection.PortableExecutable;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace MDE_Client.Pages.Machine
{
    public class MachinesModel : PageModel
    {
        private readonly MachineService _machineService;
        private readonly AuthSession _authSession;

        public MachinesModel(MachineService machineService, AuthSession authSession)
        {
            _machineService = machineService;
            _authSession = authSession;
        }
        [BindProperty]
        public ObservableCollection<Domain.Models.Machine> Machines { get; set; } = new ObservableCollection<Domain.Models.Machine>();

        public async Task OnGetAsync()
        {

            
            Debug.WriteLine("userrrr ", _authSession.Token);
            Machines = await _machineService.GetMachinesForUserAsync(int.Parse(_authSession.UserId));
            Debug.WriteLine("machines: " + Machines?.Count);
        }

    }
}



