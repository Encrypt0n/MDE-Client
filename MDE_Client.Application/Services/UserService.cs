using System.Collections.Generic;
using System.Linq;
using BCrypt.Net;
using MDE_Client.Domain.Models;

namespace MDE_Client.Services
{
    public class UserService
    {
        private static List<User> users = new List<User>
        {
            new User { UserID = 1, Username = "admin", PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123") }
        };

        public User ValidateUser(string username, string password)
        {
            var user = users.FirstOrDefault(u => u.Username == username);
            if (user != null && BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                return user;
            }
            return null;
        }

        public bool RegisterUser(string username, string password)
        {
            if (users.Any(u => u.Username == username))
                return false;

            users.Add(new User
            {
                UserID = users.Count + 1,
                Username = username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password)
            });
            return true;
        }
    }
}
