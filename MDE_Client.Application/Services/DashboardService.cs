using System.Collections.ObjectModel;
using Microsoft.Data.SqlClient;
using MDE_Client.Domain.Models;

namespace MDE_Client.Application.Services
{
    public class DashboardService
    {
        // Get all dashboard pages for a machine (ordered by PageID, first added is default)
        public ObservableCollection<DashboardPage> GetDashboardPages(int machineId)
        {
            var pages = new ObservableCollection<DashboardPage>();

            using (SqlConnection con = DatabaseHelper.GetConnection())
            {
                string query = "SELECT PageID, PageName, PageURL FROM DashboardPages WHERE MachineID = @MachineID ORDER BY PageID ASC";
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@MachineID", machineId);

                con.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    pages.Add(new DashboardPage
                    {
                        PageID = reader.GetInt32(0),
                        PageName = reader.GetString(1),
                        PageURL = reader.GetString(2)
                    });
                }
            }
            return pages;
        }

        // Add a new dashboard page
        public void AddDashboardPage(int machineId, string pageName, string pageUrl)
        {
            using (SqlConnection con = DatabaseHelper.GetConnection())
            {
                string query = "INSERT INTO DashboardPages (MachineID, PageName, PageURL) VALUES (@MachineID, @PageName, @PageURL)";
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@MachineID", machineId);
                cmd.Parameters.AddWithValue("@PageName", pageName);
                cmd.Parameters.AddWithValue("@PageURL", pageUrl);

                con.Open();
                cmd.ExecuteNonQuery();
            }
        }

        // Delete a dashboard page
        public void DeleteDashboardPage(int pageId)
        {
            using (SqlConnection con = DatabaseHelper.GetConnection())
            {
                string query = "DELETE FROM DashboardPages WHERE PageID = @PageID";
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@PageID", pageId);

                con.Open();
                cmd.ExecuteNonQuery();
            }
        }

        // Get the first page URL (default dashboard page)
        public string GetFirstDashboardPageUrl(int machineId)
        {
            using (SqlConnection con = DatabaseHelper.GetConnection())
            {
                string query = "SELECT TOP 1 PageURL FROM DashboardPages WHERE MachineID = @MachineID ORDER BY PageID ASC";
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@MachineID", machineId);

                con.Open();
                object result = cmd.ExecuteScalar();

                return result != null ? result.ToString() : string.Empty;
            }
        }
    }
}
