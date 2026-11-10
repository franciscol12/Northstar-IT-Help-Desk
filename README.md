# Northstar IT Help Desk

**An IT Help Desk Ticketing System developed as an independent Management Information Systems project at Washington State University.**

## Project Overview

Northstar IT Help Desk is a functional prototype designed to support IT incident management for Northstar Business Solutions, a fictional professional services organization with approximately 40 employees.

The system demonstrates how a centralized ticketing platform can help organizations track technical support requests, assign technicians, document troubleshooting activities, manage escalations, and monitor ticket resolution.

The project was developed as part of my High Impact Learning Experience (HILE) to apply database design, systems analysis, software development, and IT support concepts in a practical business scenario.

## Technologies Used

- **Backend:** C# and ASP.NET Core Razor Pages
- **Database:** Microsoft SQL Server
- **Database Management:** SQL Server Management Studio (SSMS)
- **Frontend:** HTML, CSS, JavaScript, and Bootstrap
- **Development Environment:** Microsoft Visual Studio
- **Database Access:** ADO.NET and SQL stored procedures

## Key Features

- Create and categorize IT support tickets
- Assign technicians based on support tiers
- Record troubleshooting activities
- Escalate unresolved incidents to higher support levels
- Accept escalated tickets
- Document resolutions and close completed tickets
- Maintain chronological ticket activity histories
- Filter support requests by status and priority
- Monitor ticket statistics through a dashboard
- Review technician workloads and reporting information

## Database Design

The database consists of five interconnected tables:

1. Employees
2. Technicians
3. Categories
4. Tickets
5. TicketHistory

The backend also includes seven stored procedures for ticket management and four reporting views.

## System Testing

The prototype was evaluated using simulated IT support scenarios. Testing included ticket creation, technician assignment, troubleshooting documentation, escalation, resolution, closure, and validation of selected invalid operations.

A complete test scenario involving Ticket #1003 demonstrated the progression of an incident from creation through final closure, including the preservation of its activity history.

## Development Environment and Limitations

The prototype was developed and tested
