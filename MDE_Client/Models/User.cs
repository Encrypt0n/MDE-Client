namespace MDE_Client.Models
{
    public class User
    {
        public int UserID { get; set; }
        public string Username { get; set; }
        public string PasswordHash { get; set; } // Stored as BCrypt hash
    }
}
