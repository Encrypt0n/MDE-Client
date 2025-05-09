using System.Collections.ObjectModel;
using Microsoft.Data.SqlClient;
using System.Reflection.PortableExecutable;
using MDE_Client.Domain.Models;
using System.Diagnostics;
using System.Windows;
using System.Security.Cryptography;

using System.Reflection;

namespace MDE_Client.Application.Services
{
    public class MachineService
    {
        public ObservableCollection<Domain.Models.Machine> GetMachinesForUser(int userId)
        {
            var machines = new ObservableCollection<Domain.Models.Machine>();

            using (SqlConnection con = DatabaseHelper.GetConnection())
            {
                string query = "SELECT MachineID, Name, Description, IP FROM Machines WHERE UserID = @UserID";
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@UserID", userId);

                con.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    machines.Add(new MDE_Client.Domain.Models.Machine
                    {
                        MachineID = reader.GetInt32(0),
                        Name = reader.GetString(1),
                        Description = reader.GetString(2),
                        IP = reader.GetString(3)
                    });
                }
            }
            return machines;
        }

        public Domain.Models.Machine GetMachineById(int machineId)
        {
            using (SqlConnection con = DatabaseHelper.GetConnection())
            {
                string query = "SELECT * FROM Machines WHERE MachineID = @MachineID";
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@MachineID", machineId);

                con.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())  // ✅ Only create object if a record exists
                    {
                        return new Domain.Models.Machine
                        {
                            MachineID = reader.GetInt32(0),
                            Name = reader.GetString(1),
                            Description = reader.GetString(2),
                            IP = reader.GetString(4)
                        };
                    }
                }
            }

            return null; // ✅ Return null if no machine is found
        }



        // Map alle apparaten binnen het VPN-bereik automatisch naar 192.168.250.X
        public void MapAllDevices(string vpnBaseIP, int port)
        {
            List<string> results = new List<string>();
            string[] parts = vpnBaseIP.Split('.');

            if (parts.Length != 4)
            {
                results.Add("❌ Ongeldig VPN IP-adres: " + vpnBaseIP);
                //return results;
            }

            // Stel de externe IP-basis samen (eerste drie octetten)
            string externalBase = $"{parts[0]}.{parts[1]}.{parts[2]}.";

            // Loop over alle mogelijke apparaten (1-254, exclusief 0 en 255)
            for (int i = 1; i <= 254; i++)
            {
                string externalIP = externalBase + i.ToString();
                string localIP = $"192.168.250.{i}";

                // Verwijder bestaande mapping als die bestaat
                RunNetshCommand($"interface portproxy delete v4tov4 listenaddress={externalIP} listenport={port}");

                // Voeg nieuwe mapping toe
                bool addOk = RunNetshCommand($"interface portproxy add v4tov4 listenaddress={externalIP} listenport={port} connectaddress={localIP} connectport={port}");

                if (addOk)
                    results.Add($"✅ Mapping: {externalIP}:{port} -> {localIP}:{port}");
                else
                    results.Add($"❌ Fout bij mapping van {externalIP}:{port} naar {localIP}:{port}");
            }

            // return results;
        }
        private static bool RunNetshCommand(string arguments)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = arguments,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                Process proc = Process.Start(psi);
                proc.WaitForExit();
                // U kunt de output eventueel controleren:
                string output = proc.StandardOutput.ReadToEnd();
                string error = proc.StandardError.ReadToEnd();
                return proc.ExitCode == 0;
            }
            catch (Exception ex)
            {
               // MessageBox.Show("Fout bij uitvoeren van netsh: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

    }
}
