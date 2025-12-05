# IT-Project-25/26 - Ticket Masala

![Logo](IT-Project2526/docs/visual/logo-green.png)

## 📌 Info
- **Team**: Charlotte Schröer, Maarten Görtz, Wito De Schrijver, Juan Benjumea
- **Concept**: Ticketing, Case, and Project Management with AI support
- **Tech Stack**: Fullstack .NET 8 (MVC), EF Core, Python (AI Microservice)

---

## 🚀 Quick Start Guide

### 1. Run the Application
```bash
cd IT-Project2526
dotnet run
```
The database will be automatically created and seeded on the first run.

### 2. Login
Navigate to `https://localhost:[YOUR_PORT]` (usually 5001 or 5054).

### 🔑 Test Accounts
**Default Passwords:**
- **Admins**: `Admin123!`
- **Employees**: `Employee123!`
- **Customers**: `Customer123!`

| Role | Email | Name | Password |
|------|-------|------|----------|
| **Admin** | `admin@ticketmasala.com` | John Administrator | `Admin123!` |
| **Project Manager** | `mike.pm@ticketmasala.com` | Mike Johnson | `Employee123!` |
| **Support** | `david.support@ticketmasala.com` | David Martinez | `Employee123!` |
| **Customer** | `alice.customer@example.com` | Alice Smith | `Customer123!` |

*(See `Data/DbSeeder.cs` for full seed data details)*

---

## 🏗️ Project Structure
Ticket Masala is a lightweight management system with 4 integrated layers:

1.  **Ticketing**: Entry point for issues and requests.
2.  **Case Management**: Groups tickets into cases for tracking.
3.  **Project Management**: Bundles cases into projects (e.g., per customer).
4.  **AI Helper (GERDA)**: Cross-cutting layer providing context, suggestions, and automation.

![ERD-model](IT-Project2526/docs/architecture/erd-dark.drawio.png)

---

## 🛠️ Tech Stack & Requirements
-   **Frontend**: ASP.NET Core MVC (Razor Views), Bootstrap
-   **Backend**: .NET 8, C#
-   **Database**: SQL Server / SQLite (EF Core + Migrations)
-   **Auth**: ASP.NET Identity (Role-based: Admin, Employee, Customer)
-   **AI**: ML.NET + Python Microservice capabilities
-   **Hosting**: Docker support ready (`fly.toml`)

---

## 🗺️ Roadmap
- [x] **Core**: Role-based Auth, Multi-tenancy
- [x] **Ticketing**: Create, Edit, Detail, Index, Batch Operations
- [x] **Projects**: Overview, Create, Detail
- [x] **AI**: GERDA Dispatching, Forecasting, Spam Detection
- [x] **Collaboration**: Rich-text Chat, Notifications, Document Management
- [ ] **Advanced**: Mobile App, Outlook Integration, 2FA

---

## ❓ Troubleshooting

### Login Failed?
1.  **Check Logs**: Look for "Database already contains users" in the console.
2.  **Reset Database**: If passwords don't work, drop the database and restart:
    ```bash
    dotnet ef database drop
    dotnet run
    ```
3.  **Manual Seed**: Navigate to `/Seed/TestAccounts` in the browser to trigger seeding manually.

### App Won't Start?
- Check `appsettings.json` connection string.
- Ensure SQL Server is running (if not using SQLite).
