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
using MDE_Client.Application.Interfaces;

namespace MDE_Client.Pages.Machine
{
    public class MachinesModel : PageModel
    {
        private readonly IMachineService _machineService;
        private readonly AuthSession _authSession;

        public MachinesModel(IMachineService machineService, AuthSession authSession)
        {
            _machineService = machineService;
            _authSession = authSession;
        }
        [BindProperty]
        public ObservableCollection<Domain.Models.Machine> Machines { get; set; } = new ObservableCollection<Domain.Models.Machine>();

        public async Task OnGetAsync()
        {

            
            Debug.WriteLine("userrrr ", _authSession.Token);
            Machines = await _machineService.GetMachinesForCompanyAsync(int.Parse(_authSession.CompanyId));
            Debug.WriteLine("machines: " + Machines?.Count);
        }

    }
}



