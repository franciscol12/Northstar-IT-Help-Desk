using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System.Data;

namespace ITTicketingSystem.Pages
{
    public class TechnicianWorkloadModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public TechnicianWorkloadModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public List<TechnicianInfo> Technicians { get; set; } = new();

        public string ErrorMessage { get; set; } = "";

        public int TotalTechnicians => Technicians.Count;

        public int ActiveTechnicians =>
            Technicians.Count(t => t.IsActive);

        public int TotalActiveTickets =>
            Technicians.Sum(t => t.ActiveTickets);

        public int TotalAssignedTickets =>
            Technicians.Sum(t => t.TotalAssignedTickets);

        public int SupportTiers =>
            Technicians.Select(t => t.SupportTier).Distinct().Count();

        public void OnGet()
        {
            try
            {
                string connectionString =
                    _configuration.GetConnectionString("HelpDeskConnection")
                    ?? throw new InvalidOperationException(
                        "Database connection is not configured.");

                using SqlConnection connection =
                    new SqlConnection(connectionString);

                connection.Open();

                string query = @"
                    SELECT
                        TechnicianID,
                        TechnicianName,
                        SupportTier,
                        IsActive,
                        TotalAssignedTickets,
                        ActiveTickets,
                        ResolvedTickets,
                        ClosedTickets
                    FROM IT_HelpDesk.vw_TechnicianWorkload
                    ORDER BY SupportTier, TechnicianName;
                ";

                using SqlCommand command =
                    new SqlCommand(query, connection);

                using SqlDataReader reader =
                    command.ExecuteReader();

                while (reader.Read())
                {
                    Technicians.Add(new TechnicianInfo
                    {
                        TechnicianID = Convert.ToInt32(reader["TechnicianID"]),
                        TechnicianName = Convert.ToString(reader["TechnicianName"]) ?? "",
                        SupportTier = Convert.ToInt32(reader["SupportTier"]),
                        IsActive = Convert.ToBoolean(reader["IsActive"]),
                        TotalAssignedTickets = reader["TotalAssignedTickets"] == DBNull.Value
                            ? 0 : Convert.ToInt32(reader["TotalAssignedTickets"]),
                        ActiveTickets = reader["ActiveTickets"] == DBNull.Value
                            ? 0 : Convert.ToInt32(reader["ActiveTickets"]),
                        ResolvedTickets = reader["ResolvedTickets"] == DBNull.Value
                            ? 0 : Convert.ToInt32(reader["ResolvedTickets"]),
                        ClosedTickets = reader["ClosedTickets"] == DBNull.Value
                            ? 0 : Convert.ToInt32(reader["ClosedTickets"])
                    });
                }
            }
            catch (Exception)
            {
                ErrorMessage =
                    "Unable to load technician workload data. Please check the database connection.";
            }
        }

        public class TechnicianInfo
        {
            public int TechnicianID { get; set; }
            public string TechnicianName { get; set; } = "";
            public int SupportTier { get; set; }
            public bool IsActive { get; set; }
            public int TotalAssignedTickets { get; set; }
            public int ActiveTickets { get; set; }
            public int ResolvedTickets { get; set; }
            public int ClosedTickets { get; set; }
        }
    }
}
