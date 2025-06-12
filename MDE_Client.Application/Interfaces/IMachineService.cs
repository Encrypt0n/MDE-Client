using MDE_Client.Domain.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MDE_Client.Application.Interfaces
{
    public interface IMachineService
    {
        Task<ObservableCollection<Domain.Models.Machine>> GetMachinesForCompanyAsync(int companyId);
        Task<Domain.Models.Machine> GetMachineByIdAsync(int machineId);
       
        Task<(byte[] fileContent, string fileName)> GenerateConfigAsync(string machineName, string ovpn, Company company, bool user);
        Task UpdateMachineDashboardUrlAsync(int machineId, string dashboardUrl);

    }
}
