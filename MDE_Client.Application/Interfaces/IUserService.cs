using MDE_Client.Domain.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MDE_Client.Application.Interfaces
{
    public interface IUserService
    {
        Task<ObservableCollection<User>> GetAllUsersAsync();
        Task<User?> GetUserByIdAsync(int userId);
    }

   

}
