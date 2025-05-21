using MDE_Client.Domain.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MDE_Client.Application.Interfaces
{
    
        public interface IUserActivityService
        {
            Task LogActivityAsync(UserActivityLog log);
        Task<ObservableCollection<UserActivityLog>> GetActivitiesForMachineAsync(int machineId);
        }
    


}
