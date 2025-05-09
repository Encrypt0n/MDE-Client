namespace MDE_Client.Domain.Models
{
    public class User
    {
        public int UserID { get; set; }
        public string Username { get; set; }
        public string PasswordHash { get; set; } // Stored as BCrypt hash
    }
}
