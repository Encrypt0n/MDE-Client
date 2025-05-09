using System;
using Microsoft.Data.SqlClient;

namespace MDE_Client.Services
{
    public class DatabaseHelper
    {
        private static string connectionString = "Server=10.8.0.1,1433;Database=master;User Id=vpnuser;Password=MdeAutomation423!;TrustServerCertificate=True;";

        public static SqlConnection GetConnection()
        {
            return new SqlConnection(connectionString);
        }
    }
}
