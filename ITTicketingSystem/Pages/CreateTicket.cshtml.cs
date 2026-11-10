using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using System.ComponentModel.DataAnnotations;
using System.Data;

namespace ITTicketingSystem.Pages
{
    public class CreateTicketModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public CreateTicketModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [BindProperty]
        public TicketForm Input { get; set; } = new();

        public List<SelectListItem> Employees { get; set; } = new();
        public List<SelectListItem> Categories { get; set; } = new();

        public string ErrorMessage { get; set; } = "";

        public class TicketForm
        {
            [Range(1, int.MaxValue, ErrorMessage = "Select an employee.")]
            public int EmployeeID { get; set; }

            [Range(1, int.MaxValue, ErrorMessage = "Select a category.")]
            public int CategoryID { get; set; }

            [Required]
            [StringLength(150)]
            public string Title { get; set; } = "";

            [Required]
            public string Description { get; set; } = "";

            [Required]
            [RegularExpression(
                "^(Low|Medium|High|Critical)$",
                ErrorMessage = "Select a valid priority.")]
            public string Priority { get; set; } = "Medium";
        }

        private string GetConnectionString()
        {
            return _configuration.GetConnectionString("HelpDeskConnection")
                ?? throw new InvalidOperationException(
                    "Database connection is not configured.");
        }

        private void LoadDropdowns()
        {
            using SqlConnection connection =
                new SqlConnection(GetConnectionString());

            connection.Open();

            using (SqlCommand command = new SqlCommand(
                @"SELECT EmployeeID, FirstName, LastName
                  FROM IT_HelpDesk.Employees
                  ORDER BY LastName, FirstName;", connection))
            using (SqlDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    Employees.Add(new SelectListItem
                    {
                        Value = reader["EmployeeID"].ToString(),
                        Text = reader["FirstName"] + " " +
                               reader["LastName"]
                    });
                }
            }

            using (SqlCommand command = new SqlCommand(
                @"SELECT CategoryID, CategoryName
                  FROM IT_HelpDesk.Categories
                  ORDER BY CategoryName;", connection))
            using (SqlDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    Categories.Add(new SelectListItem
                    {
                        Value = reader["CategoryID"].ToString(),
                        Text = reader["CategoryName"].ToString()
                    });
                }
            }
        }

        public void OnGet()
        {
            try
            {
                LoadDropdowns();
            }
            catch (Exception)
            {
                ErrorMessage = "Unable to load ticket form data.";
            }
        }

        public IActionResult OnPost()
        {
            if (string.IsNullOrWhiteSpace(Input.Title))
            {
                ModelState.AddModelError(
                    "Input.Title", "Ticket title is required.");
            }

            if (string.IsNullOrWhiteSpace(Input.Description))
            {
                ModelState.AddModelError(
                    "Input.Description", "Description is required.");
            }

            if (!ModelState.IsValid)
            {
                try
                {
                    LoadDropdowns();
                }
                catch (Exception)
                {
                    ErrorMessage = "Unable to load ticket form data.";
                }

                return Page();
            }

            try
            {
                using SqlConnection connection =
                    new SqlConnection(GetConnectionString());

                connection.Open();

                using SqlCommand command =
                    new SqlCommand(
                        "IT_HelpDesk.CreateTicket", connection);

                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.Add(
                    "@EmployeeID", SqlDbType.Int).Value =
                    Input.EmployeeID;

                command.Parameters.Add(
                    "@CategoryID", SqlDbType.Int).Value =
                    Input.CategoryID;

                command.Parameters.Add(
                    "@Title", SqlDbType.NVarChar, 150).Value =
                    Input.Title.Trim();

                command.Parameters.Add(
                    "@Description", SqlDbType.NVarChar, -1).Value =
                    Input.Description.Trim();

                command.Parameters.Add(
                    "@Priority", SqlDbType.VarChar, 10).Value =
                    Input.Priority;

                int newTicketID;

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        throw new InvalidOperationException(
                            "Ticket creation returned no result.");
                    }

                    newTicketID = Convert.ToInt32(
                        reader["TicketID"]);
                }

                // Open the newly created ticket's details
                return RedirectToPage(
                    "/TicketDetails",
                    new { id = newTicketID });
            }
            catch (Exception)
            {
                ErrorMessage =
                    "Unable to create the ticket. Please check your information.";
            }

            try
            {
                LoadDropdowns();
            }
            catch (Exception)
            {
                ErrorMessage = "Unable to load ticket form data.";
            }

            return Page();
        }
    }
}