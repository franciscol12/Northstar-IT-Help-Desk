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

### SQL Database Source

The database schema and SQL logic are included in the `Database` folder.

**File:** `Database/Northstar_HelpDesk_Database.sql`

This script contains the exported definitions for five relational tables, seven stored procedures, four reporting views, and associated database constraints.

The SQL script was generated from the completed Northstar prototype using SQL Server Management Studio (SSMS). It contains schema definitions rather than actual employee or support ticket records.

The script is provided as technical documentation of the database implementation. It has not been independently tested as a standalone database installation script.

## System Testing

The prototype was evaluated using simulated IT support scenarios. Testing included ticket creation, technician assignment, troubleshooting documentation, escalation, resolution, closure, and validation of selected invalid operations.

A complete test scenario involving Ticket #1003 demonstrated the progression of an incident from creation through final closure, including the preservation of its activity history.

## Development Environment and Limitations

The prototype was developed and tested using a WSU-hosted Microsoft SQL Server database that requires authorized university access.

The repository contains the ASP.NET Core application source code with placeholder database connection settings. The original WSU database connection is not included.

To run the database-dependent functionality, a developer would need access to a compatible SQL Server database containing the required tables, stored procedures, views, and test records, along with an appropriately configured connection string.

This is an academic prototype rather than a production-ready help desk platform. Additional development would be needed for production deployment, including user authentication, role-based access controls, and independent hosting.

## Project Documentation

Additional project documentation includes:

- Industry Research & System Requirements
- Technical Documentation & System Demonstration
- Entity Relationship Diagram (ERD)
- Ticket Lifecycle Flowchart
- System Architecture Diagram
- Website screenshots and functional testing evidence

## Author

**Francisco Ledesma**  
Management Information Systems  
Washington State University  
November 2026
