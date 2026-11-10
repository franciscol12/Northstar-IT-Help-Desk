using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System.Data;

namespace ITTicketingSystem.Pages
{
    public class TicketDetailsModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public TicketDetailsModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public TicketInfo? Ticket { get; set; }

        public List<HistoryInfo> History { get; set; } = new();

        [BindProperty]
        public int SelectedTechnicianID { get; set; }

        public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>
            AvailableTechnicians
        { get; set; } = new();

        [BindProperty]
        public string TroubleshootingNote { get; set; } = "";

        public int? AssignedTechnicianID { get; set; }

        [BindProperty]
        public int EscalationTechnicianID { get; set; }

        [BindProperty]
        public string EscalationReason { get; set; } = "";

        [BindProperty]
        public string ResolutionNotes { get; set; } = "";

        public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>
            EscalationTechnicians
        { get; set; } = new();

        public string ErrorMessage { get; set; } = "";

        public IActionResult OnGet(int? id)
        {
            if (!id.HasValue || id.Value <= 0)
            {
                return BadRequest("A valid Ticket ID is required.");
            }

            try
            {
                string connectionString =
                    _configuration.GetConnectionString("HelpDeskConnection")
                    ?? throw new InvalidOperationException(
                        "Database connection is not configured.");

                using SqlConnection connection =
                    new SqlConnection(connectionString);

                connection.Open();

                // Retrieve the selected ticket
                string ticketQuery = @"
                    SELECT
                        t.TicketID,
                        t.Title,
                        t.Description,
                        t.Priority,
                        t.Status,
                        t.CurrentSupportTier,
                        t.AssignedTechnicianID,
                        t.CreatedAt,
                        t.ResolvedAt,
                        t.ClosedAt,
                        t.ResolutionNotes,
                        e.FirstName + ' ' + e.LastName AS RequestedBy,
                        c.CategoryName,
                        COALESCE(
                            tech.FirstName + ' ' + tech.LastName,
                            'Unassigned'
                        ) AS AssignedTechnician
                    FROM IT_HelpDesk.Tickets AS t
                    INNER JOIN IT_HelpDesk.Employees AS e
                        ON t.EmployeeID = e.EmployeeID
                    INNER JOIN IT_HelpDesk.Categories AS c
                        ON t.CategoryID = c.CategoryID
                    LEFT JOIN IT_HelpDesk.Technicians AS tech
                        ON t.AssignedTechnicianID = tech.TechnicianID
                    WHERE t.TicketID = @TicketID;
                ";

                using (SqlCommand command =
                    new SqlCommand(ticketQuery, connection))
                {
                    command.Parameters.AddWithValue("@TicketID", id.Value);

                    using SqlDataReader reader = command.ExecuteReader();

                    if (!reader.Read())
                    {
                        return NotFound();
                    }

                    Ticket = new TicketInfo
                    {
                        TicketID = Convert.ToInt32(reader["TicketID"]),
                        Title = reader["Title"].ToString() ?? "",
                        Description = reader["Description"].ToString() ?? "",
                        Priority = reader["Priority"].ToString() ?? "",
                        Status = reader["Status"].ToString() ?? "",
                        CurrentSupportTier =
                            Convert.ToInt32(reader["CurrentSupportTier"]),
                        AssignedTechnicianID = reader["AssignedTechnicianID"] == DBNull.Value? null: Convert.ToInt32(reader["AssignedTechnicianID"]),
                        CreatedAt = Convert.ToDateTime(reader["CreatedAt"]),
                        ResolvedAt = reader["ResolvedAt"] == DBNull.Value
                            ? null
                            : Convert.ToDateTime(reader["ResolvedAt"]),
                        ClosedAt = reader["ClosedAt"] == DBNull.Value
                            ? null
                            : Convert.ToDateTime(reader["ClosedAt"]),
                        ResolutionNotes =
                            reader["ResolutionNotes"] == DBNull.Value
                                ? ""
                                : reader["ResolutionNotes"].ToString() ?? "",
                        RequestedBy = reader["RequestedBy"].ToString() ?? "",
                        CategoryName = reader["CategoryName"].ToString() ?? "",
                        AssignedTechnician =
                            reader["AssignedTechnician"].ToString() ?? ""
                    };
                }

                // Retrieve the ticket's complete activity history
                string historyQuery = @"
                    SELECT
                        h.HistoryID,
                        h.ActivityType,
                        h.ActivityDescription,
                        h.ActivityTimestamp,
                        COALESCE(
                            tech.FirstName + ' ' + tech.LastName,
                            'Unassigned'
                        ) AS Technician
                    FROM IT_HelpDesk.TicketHistory AS h
                    LEFT JOIN IT_HelpDesk.Technicians AS tech
                        ON h.TechnicianID = tech.TechnicianID
                    WHERE h.TicketID = @TicketID
                    ORDER BY h.HistoryID;
                ";

                using (SqlCommand command =
                    new SqlCommand(historyQuery, connection))
                {
                    command.Parameters.AddWithValue("@TicketID", id.Value);

                    using SqlDataReader reader = command.ExecuteReader();

                    while (reader.Read())
                    {
                        History.Add(new HistoryInfo
                        {
                            HistoryID =
                                Convert.ToInt32(reader["HistoryID"]),
                            ActivityType =
                                reader["ActivityType"].ToString() ?? "",
                            ActivityDescription =
                                reader["ActivityDescription"].ToString() ?? "",
                            ActivityTimestamp =
                                Convert.ToDateTime(reader["ActivityTimestamp"]),
                            Technician =
                                reader["Technician"].ToString() ?? ""
                        });
                    }
                }
                // Load eligible technicians for open, unassigned tickets
                if (Ticket != null &&
                    Ticket.Status == "Open" &&
                    Ticket.AssignedTechnician == "Unassigned")
                {
                    LoadAvailableTechnicians(
                        connection,
                        Ticket.CurrentSupportTier);
                }
                // Load technicians eligible to receive an escalated ticket
                if (Ticket != null &&
                    Ticket.Status == "In Progress" &&
                    Ticket.AssignedTechnicianID.HasValue &&
                    Ticket.CurrentSupportTier < 3)
                {
                    LoadEscalationTechnicians(
                        connection,
                        Ticket.CurrentSupportTier);
                }
            }
            catch (Exception)
            {
                ErrorMessage = "Unable to load ticket details.";
            }

            return Page();
        }

        private void LoadAvailableTechnicians(
    SqlConnection connection,
    int supportTier)
        {
            AvailableTechnicians.Clear();

            string query = @"
        SELECT
            TechnicianID,
            FirstName,
            LastName,
            SupportTier
        FROM IT_HelpDesk.Technicians
        WHERE IsActive = 1
          AND SupportTier = @SupportTier
        ORDER BY LastName, FirstName;
    ";

            using SqlCommand command =
                new SqlCommand(query, connection);

            command.Parameters.Add(
                "@SupportTier", System.Data.SqlDbType.Int
            ).Value = supportTier;

            using SqlDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
                AvailableTechnicians.Add(
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                    {
                        Value = reader["TechnicianID"].ToString(),
                        Text = reader["FirstName"] + " " +
                               reader["LastName"] +
                               " (Tier " + reader["SupportTier"] + ")"
                    }
                );
            }
        }

        private void LoadEscalationTechnicians(
    SqlConnection connection,
    int currentSupportTier)
        {
            EscalationTechnicians.Clear();

            // Tier 3 is the highest support tier
            if (currentSupportTier >= 3)
            {
                return;
            }

            string query = @"
        SELECT
            TechnicianID,
            FirstName,
            LastName,
            SupportTier
        FROM IT_HelpDesk.Technicians
        WHERE IsActive = 1
          AND SupportTier = @NextTier
        ORDER BY LastName, FirstName;
    ";

            using SqlCommand command =
                new SqlCommand(query, connection);

            command.Parameters.Add(
                "@NextTier", System.Data.SqlDbType.Int
            ).Value = currentSupportTier + 1;

            using SqlDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
                EscalationTechnicians.Add(
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                    {
                        Value = reader["TechnicianID"].ToString(),
                        Text = reader["FirstName"] + " " +
                               reader["LastName"] +
                               " (Tier " + reader["SupportTier"] + ")"
                    }
                );
            }
        }

        public IActionResult OnPostAssign(int id)
        {
            if (id <= 0)
            {
                return BadRequest("A valid Ticket ID is required.");
            }

            if (SelectedTechnicianID <= 0)
            {
                TempData["AssignmentError"] =
                    "Please select a technician.";

                return RedirectToPage("/TicketDetails", new { id });
            }

            try
            {
                string connectionString =
                    _configuration.GetConnectionString("HelpDeskConnection")
                    ?? throw new InvalidOperationException(
                        "Database connection is not configured.");

                using SqlConnection connection =
                    new SqlConnection(connectionString);

                connection.Open();

                using SqlCommand command =
                    new SqlCommand("IT_HelpDesk.AssignTicket", connection);

                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.Add(
                    "@TicketID", SqlDbType.Int
                ).Value = id;

                command.Parameters.Add(
                    "@TechnicianID", SqlDbType.Int
                ).Value = SelectedTechnicianID;

                command.ExecuteNonQuery();

                TempData["AssignmentSuccess"] =
                    "Technician assigned successfully.";
            }
            catch (SqlException)
            {
                TempData["AssignmentError"] =
                    "Unable to assign the technician. Check that the ticket is open and the technician is eligible.";
            }
            catch (Exception)
            {
                TempData["AssignmentError"] =
                    "An unexpected error occurred while assigning the technician.";
            }

            return RedirectToPage("/TicketDetails", new { id });
        }

        public IActionResult OnPostAddNote(int id)
        {
            if (id <= 0)
            {
                return BadRequest("A valid Ticket ID is required.");
            }

            if (string.IsNullOrWhiteSpace(TroubleshootingNote))
            {
                TempData["NoteError"] =
                    "Please enter a troubleshooting note.";

                return RedirectToPage("/TicketDetails", new { id });
            }

            if (TroubleshootingNote.Length > 4000)
            {
                TempData["NoteError"] =
                    "Troubleshooting notes cannot exceed 4000 characters.";

                return RedirectToPage("/TicketDetails", new { id });
            }

            try
            {
                string connectionString =
                    _configuration.GetConnectionString("HelpDeskConnection")
                    ?? throw new InvalidOperationException(
                        "Database connection is not configured.");

                using SqlConnection connection =
                    new SqlConnection(connectionString);

                connection.Open();

                // Retrieve the current assigned technician from SQL Server
                string technicianQuery = @"
            SELECT AssignedTechnicianID
            FROM IT_HelpDesk.Tickets
            WHERE TicketID = @TicketID
              AND Status = 'In Progress';
        ";

                int technicianID;

                using (SqlCommand lookupCommand =
                    new SqlCommand(technicianQuery, connection))
                {
                    lookupCommand.Parameters.Add(
                        "@TicketID", SqlDbType.Int).Value = id;

                    object? result = lookupCommand.ExecuteScalar();

                    if (result == null || result == DBNull.Value)
                    {
                        TempData["NoteError"] =
                            "This ticket must be In Progress and assigned to a technician.";

                        return RedirectToPage("/TicketDetails", new { id });
                    }

                    technicianID = Convert.ToInt32(result);
                }

                // Execute the existing troubleshooting stored procedure
                using SqlCommand command =
                    new SqlCommand(
                        "IT_HelpDesk.AddTroubleshootingNote",
                        connection);

                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.Add(
                    "@TicketID", SqlDbType.Int).Value = id;

                command.Parameters.Add(
                    "@TechnicianID", SqlDbType.Int).Value = technicianID;

                command.Parameters.Add(
                    "@Note", SqlDbType.NVarChar, -1).Value =
                    TroubleshootingNote.Trim();

                command.ExecuteNonQuery();

                TempData["NoteSuccess"] =
                    "Troubleshooting note saved successfully.";
            }
            catch (SqlException)
            {
                TempData["NoteError"] =
                    "Unable to save the note. Verify that the ticket is eligible for troubleshooting.";
            }
            catch (Exception)
            {
                TempData["NoteError"] =
                    "An unexpected error occurred while saving the note.";
            }

            return RedirectToPage("/TicketDetails", new { id });
        }

        public IActionResult OnPostEscalate(int id)
        {
            if (id <= 0)
            {
                return BadRequest("A valid Ticket ID is required.");
            }

            if (EscalationTechnicianID <= 0 ||
                string.IsNullOrWhiteSpace(EscalationReason))
            {
                TempData["EscalationError"] =
                    "Select a receiving technician and provide an escalation reason.";

                return RedirectToPage("/TicketDetails", new { id });
            }

            if (EscalationReason.Length > 4000)
            {
                TempData["EscalationError"] =
                    "Escalation reason cannot exceed 4000 characters.";

                return RedirectToPage("/TicketDetails", new { id });
            }

            try
            {
                string connectionString =
                    _configuration.GetConnectionString("HelpDeskConnection")
                    ?? throw new InvalidOperationException(
                        "Database connection is not configured.");

                using SqlConnection connection =
                    new SqlConnection(connectionString);

                connection.Open();

                // Retrieve the currently assigned technician
                string lookupQuery = @"
            SELECT AssignedTechnicianID
            FROM IT_HelpDesk.Tickets
            WHERE TicketID = @TicketID
              AND Status = 'In Progress'
              AND CurrentSupportTier < 3;
        ";

                int currentTechnicianID;

                using (SqlCommand lookupCommand =
                    new SqlCommand(lookupQuery, connection))
                {
                    lookupCommand.Parameters.Add(
                        "@TicketID", SqlDbType.Int).Value = id;

                    object? result = lookupCommand.ExecuteScalar();

                    if (result == null || result == DBNull.Value)
                    {
                        TempData["EscalationError"] =
                            "This ticket is not eligible for escalation.";

                        return RedirectToPage("/TicketDetails", new { id });
                    }

                    currentTechnicianID = Convert.ToInt32(result);
                }

                // Execute the existing escalation stored procedure
                using SqlCommand command =
                    new SqlCommand(
                        "IT_HelpDesk.EscalateTicket",
                        connection);

                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.Add(
                    "@TicketID", SqlDbType.Int).Value = id;

                command.Parameters.Add(
                    "@CurrentTechnicianID", SqlDbType.Int).Value =
                    currentTechnicianID;

                command.Parameters.Add(
                    "@NewTechnicianID", SqlDbType.Int).Value =
                    EscalationTechnicianID;

                command.Parameters.Add(
                    "@EscalationReason", SqlDbType.NVarChar, -1).Value =
                    EscalationReason.Trim();

                command.ExecuteNonQuery();

                TempData["EscalationSuccess"] =
                    "Ticket escalated successfully.";
            }
            catch (SqlException)
            {
                TempData["EscalationError"] =
                    "Unable to escalate the ticket. Verify the receiving technician and support tier.";
            }
            catch (Exception)
            {
                TempData["EscalationError"] =
                    "An unexpected error occurred during escalation.";
            }

            return RedirectToPage("/TicketDetails", new { id });
        }

        public IActionResult OnPostAccept(int id)
        {
            if (id <= 0)
            {
                return BadRequest("A valid Ticket ID is required.");
            }

            try
            {
                string connectionString =
                    _configuration.GetConnectionString("HelpDeskConnection")
                    ?? throw new InvalidOperationException(
                        "Database connection is not configured.");

                using SqlConnection connection =
                    new SqlConnection(connectionString);

                connection.Open();

                // Retrieve the technician currently assigned
                // to the escalated ticket.
                string lookupQuery = @"
            SELECT AssignedTechnicianID
            FROM IT_HelpDesk.Tickets
            WHERE TicketID = @TicketID
              AND Status = 'Escalated';
        ";

                int technicianID;

                using (SqlCommand lookupCommand =
                    new SqlCommand(lookupQuery, connection))
                {
                    lookupCommand.Parameters.Add(
                        "@TicketID", SqlDbType.Int).Value = id;

                    object? result = lookupCommand.ExecuteScalar();

                    if (result == null || result == DBNull.Value)
                    {
                        TempData["AcceptanceError"] =
                            "This ticket is not eligible for acceptance.";

                        return RedirectToPage("/TicketDetails", new { id });
                    }

                    technicianID = Convert.ToInt32(result);
                }

                // Execute the acceptance stored procedure.
                using SqlCommand command =
                    new SqlCommand(
                        "IT_HelpDesk.AcceptEscalatedTicket",
                        connection);

                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.Add(
                    "@TicketID", SqlDbType.Int).Value = id;

                command.Parameters.Add(
                    "@TechnicianID", SqlDbType.Int).Value =
                    technicianID;

                command.ExecuteNonQuery();

                TempData["AcceptanceSuccess"] =
                    "Escalated ticket accepted successfully.";
            }
            catch (SqlException)
            {
                TempData["AcceptanceError"] =
                    "Unable to accept the ticket. Verify that it is still escalated and assigned to an eligible technician.";
            }
            catch (Exception)
            {
                TempData["AcceptanceError"] =
                    "An unexpected error occurred while accepting the ticket.";
            }

            return RedirectToPage("/TicketDetails", new { id });
        }

        public IActionResult OnPostResolve(int id)
        {
            if (id <= 0)
            {
                return BadRequest("A valid Ticket ID is required.");
            }

            if (string.IsNullOrWhiteSpace(ResolutionNotes))
            {
                TempData["ResolutionError"] =
                    "Please enter resolution notes.";

                return RedirectToPage("/TicketDetails", new { id });
            }

            if (ResolutionNotes.Length > 4000)
            {
                TempData["ResolutionError"] =
                    "Resolution notes cannot exceed 4000 characters.";

                return RedirectToPage("/TicketDetails", new { id });
            }

            try
            {
                string connectionString =
                    _configuration.GetConnectionString("HelpDeskConnection")
                    ?? throw new InvalidOperationException(
                        "Database connection is not configured.");

                using SqlConnection connection =
                    new SqlConnection(connectionString);

                connection.Open();

                // Retrieve the currently assigned technician
                // for an In Progress ticket.
                string lookupQuery = @"
            SELECT AssignedTechnicianID
            FROM IT_HelpDesk.Tickets
            WHERE TicketID = @TicketID
              AND Status = 'In Progress';
        ";

                int technicianID;

                using (SqlCommand lookupCommand =
                    new SqlCommand(lookupQuery, connection))
                {
                    lookupCommand.Parameters.Add(
                        "@TicketID", SqlDbType.Int).Value = id;

                    object? result = lookupCommand.ExecuteScalar();

                    if (result == null || result == DBNull.Value)
                    {
                        TempData["ResolutionError"] =
                            "This ticket must be In Progress and assigned to a technician.";

                        return RedirectToPage("/TicketDetails", new { id });
                    }

                    technicianID = Convert.ToInt32(result);
                }

                // Execute the existing resolution stored procedure.
                using SqlCommand command =
                    new SqlCommand("IT_HelpDesk.ResolveTicket", connection);

                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.Add(
                    "@TicketID", SqlDbType.Int).Value = id;

                command.Parameters.Add(
                    "@TechnicianID", SqlDbType.Int).Value = technicianID;

                command.Parameters.Add(
                    "@ResolutionNotes", SqlDbType.NVarChar, -1).Value =
                    ResolutionNotes.Trim();

                command.ExecuteNonQuery();

                TempData["ResolutionSuccess"] =
                    "Ticket resolved successfully.";
            }
            catch (SqlException)
            {
                TempData["ResolutionError"] =
                    "Unable to resolve the ticket. Verify that it is In Progress and assigned to an eligible technician.";
            }
            catch (Exception)
            {
                TempData["ResolutionError"] =
                    "An unexpected error occurred while resolving the ticket.";
            }

            return RedirectToPage("/TicketDetails", new { id });
        }

        public IActionResult OnPostClose(int id)
        {
            if (id <= 0)
            {
                return BadRequest("A valid Ticket ID is required.");
            }

            try
            {
                string connectionString =
                    _configuration.GetConnectionString("HelpDeskConnection")
                    ?? throw new InvalidOperationException(
                        "Database connection is not configured.");

                using SqlConnection connection =
                    new SqlConnection(connectionString);

                connection.Open();

                // Retrieve the assigned technician for a resolved ticket
                string lookupQuery = @"
            SELECT AssignedTechnicianID
            FROM IT_HelpDesk.Tickets
            WHERE TicketID = @TicketID
              AND Status = 'Resolved';
        ";

                int technicianID;

                using (SqlCommand lookupCommand =
                    new SqlCommand(lookupQuery, connection))
                {
                    lookupCommand.Parameters.Add(
                        "@TicketID", SqlDbType.Int).Value = id;

                    object? result = lookupCommand.ExecuteScalar();

                    if (result == null || result == DBNull.Value)
                    {
                        TempData["ClosureError"] =
                            "This ticket must be resolved and assigned to a technician before closure.";

                        return RedirectToPage("/TicketDetails", new { id });
                    }

                    technicianID = Convert.ToInt32(result);
                }

                // Execute the existing SQL closure stored procedure
                using SqlCommand command =
                    new SqlCommand("IT_HelpDesk.CloseTicket", connection);

                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.Add(
                    "@TicketID", SqlDbType.Int).Value = id;

                command.Parameters.Add(
                    "@TechnicianID", SqlDbType.Int).Value =
                    technicianID;

                command.ExecuteNonQuery();

                TempData["ClosureSuccess"] =
                    "Ticket closed successfully.";
            }
            catch (SqlException)
            {
                TempData["ClosureError"] =
                    "Unable to close the ticket. Verify that it has been resolved.";
            }
            catch (Exception)
            {
                TempData["ClosureError"] =
                    "An unexpected error occurred while closing the ticket.";
            }

            return RedirectToPage("/TicketDetails", new { id });
        }

        public class TicketInfo
        {
            public int TicketID { get; set; }
            public string Title { get; set; } = "";
            public string Description { get; set; } = "";
            public string Priority { get; set; } = "";
            public string Status { get; set; } = "";
            public int CurrentSupportTier { get; set; }
            public int? AssignedTechnicianID { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime? ResolvedAt { get; set; }
            public DateTime? ClosedAt { get; set; }
            public string ResolutionNotes { get; set; } = "";
            public string RequestedBy { get; set; } = "";
            public string CategoryName { get; set; } = "";
            public string AssignedTechnician { get; set; } = "";
        }

        public class HistoryInfo
        {
            public int HistoryID { get; set; }
            public string ActivityType { get; set; } = "";
            public string ActivityDescription { get; set; } = "";
            public DateTime ActivityTimestamp { get; set; }
            public string Technician { get; set; } = "";
        }
    }
}