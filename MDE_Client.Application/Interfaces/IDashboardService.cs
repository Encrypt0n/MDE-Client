using MDE_Client.Domain.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MDE_Client.Application.Interfaces
{
    public interface IDashboardService
    {
        Task<ObservableCollection<DashboardPage>> GetDashboardPagesAsync(int machineId);
        Task AddDashboardPageAsync(int machineId, string pageName, string pageUrl);
        Task DeleteDashboardPageAsync(int pageId);
        Task<string> GetFirstDashboardPageUrlAsync(int machineId);

    }
}
