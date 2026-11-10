using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace ITTicketingSystem.Pages
{
    public class DashboardModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public int TotalTickets { get; set; }
        public int OpenTickets { get; set; }
        public int InProgressTickets { get; set; }
        public int EscalatedTickets { get; set; }
        public int ResolvedTickets { get; set; }
        public int ClosedTickets { get; set; }

        public string ConnectionStatus { get; set; } = "";

        public DashboardModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void OnGet()
        {
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
                        TotalTickets,
                        OpenTickets,
                        InProgressTickets,
                        EscalatedTickets,
                        ResolvedTickets,
                        ClosedTickets
                    FROM IT_HelpDesk.vw_TicketStatusSummary;
                ";

                using SqlCommand command =
                    new SqlCommand(query, connection);

                using SqlDataReader reader =
                    command.ExecuteReader();

                if (reader.Read())
                {
                    TotalTickets = Convert.ToInt32(reader["TotalTickets"]);
                    OpenTickets = Convert.ToInt32(reader["OpenTickets"]);
                    InProgressTickets = Convert.ToInt32(reader["InProgressTickets"]);
                    EscalatedTickets = Convert.ToInt32(reader["EscalatedTickets"]);
                    ResolvedTickets = Convert.ToInt32(reader["ResolvedTickets"]);
                    ClosedTickets = Convert.ToInt32(reader["ClosedTickets"]);

                    ConnectionStatus = "Connected successfully!";
                }
                else
                {
                    ConnectionStatus = "No reporting data was returned.";
                }
            }
            catch (Exception)
            {
                ConnectionStatus =
                    "Unable to load ticket statistics. Please contact IT support.";
            }
        }
    }
}