using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace ITTicketingSystem.Pages
{
    public class TicketsModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public List<TicketInfo> Tickets { get; set; } = new();

        public string SelectedStatus { get; set; } = "All";

        public string SelectedPriority { get; set; } = "All";

        public string ErrorMessage { get; set; } = "";

        public TicketsModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void OnGet(string? status, string? priority)
        {
            string[] validStatuses =
            {
                "Open",
                "In Progress",
                "Escalated",
                "Resolved",
                "Closed"
            };

            SelectedStatus = validStatuses.Contains(status)
                ? status!
                : "All";

            string[] validPriorities =
            {
                "Low",
                "Medium",
                "High",
                "Critical"
            };

            SelectedPriority = validPriorities.Contains(priority)
                ? priority!
                : "All";
            try
            {
                string connectionString =
                    _configuration.GetConnectionString("HelpDeskConnection")
                    ?? throw new InvalidOperationException(
                        "HelpDeskConnection is not configured.");

                using SqlConnection connection =
                    new SqlConnection(connectionString);

                connection.Open();

                string query = @"
                    SELECT
                        t.TicketID,
                        t.Title,
                        t.Priority,
                        t.Status,
                        t.CreatedAt,
                        COALESCE(
                            tech.FirstName + ' ' + tech.LastName,
                            'Unassigned'
                        ) AS AssignedTechnician
                    FROM IT_HelpDesk.Tickets AS t
                    LEFT JOIN IT_HelpDesk.Technicians AS tech
                        ON t.AssignedTechnicianID = tech.TechnicianID
                    WHERE (@Status = 'All' OR t.Status = @Status)
                    AND (@Priority = 'All' OR t.Priority = @Priority)
                    ORDER BY t.CreatedAt DESC, t.TicketID DESC;
                ";

                using SqlCommand command = new SqlCommand(query, connection);

                command.Parameters.Add(
                    "@Status", System.Data.SqlDbType.VarChar, 20
                ).Value = SelectedStatus;
                command.Parameters.Add(
                    "@Priority", System.Data.SqlDbType.VarChar, 20
                ).Value = SelectedPriority;

                using SqlDataReader reader =
                    command.ExecuteReader();

                while (reader.Read())
                {
                    Tickets.Add(new TicketInfo
                    {
                        TicketID = Convert.ToInt32(reader["TicketID"]),
                        Title = reader["Title"].ToString() ?? "",
                        Priority = reader["Priority"].ToString() ?? "",
                        Status = reader["Status"].ToString() ?? "",
                        CreatedAt = Convert.ToDateTime(reader["CreatedAt"]),
                        AssignedTechnician =
                            reader["AssignedTechnician"].ToString() ?? ""
                    });
                }
            }
            catch (Exception)
            {
                ErrorMessage =
                    "Unable to load tickets. Please contact IT support.";
            }
        }

        public class TicketInfo
        {
            public int TicketID { get; set; }
            public string Title { get; set; } = "";
            public string Priority { get; set; } = "";
            public string Status { get; set; } = "";
            public DateTime CreatedAt { get; set; }
            public string AssignedTechnician { get; set; } = "";
        }
    }
}