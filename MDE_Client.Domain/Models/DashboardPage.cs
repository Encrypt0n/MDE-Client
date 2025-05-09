namespace MDE_Client.Domain.Models
{
    public class DashboardPage
    {
        public int PageID { get; set; }
        public string PageName { get; set; }
        public string PageURL { get; set; }

        public int PageOrder { get; set; }

        public override string ToString()
        {
            return PageName;
        }
    }
}
