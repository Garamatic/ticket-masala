# Test Accounts for Ticket Masala

This document contains the test accounts that are automatically seeded into the database when the application starts for the first time.

## ?? Default Password for All Accounts

All accounts use role-specific passwords:
- **Admin accounts**: `Admin123!`
- **Employee accounts**: `Employee123!`
- **Customer accounts**: `Customer123!`

---

## ?? Admin Accounts (Full System Access)

These accounts have full administrative privileges and can access all features.

| Name | Email | Role | Team | Level |
|------|-------|------|------|-------|
| John Administrator | admin@ticketmasala.com | Admin | Management | Admin |
| Sarah Wilson | sarah.admin@ticketmasala.com | Admin | Management | CEO |

**Permissions**: 
- Full access to all projects and tickets
- User management
- System configuration
- Manager dashboard access

---

## ????? Employee Accounts (Project & Ticket Management)

These accounts can manage projects, tickets, and customers but have limited administrative access.

### Project Managers

| Name | Email | Team | Level |
|------|-------|------|-------|
| Mike Johnson | mike.pm@ticketmasala.com | Project Management | ProjectManager |
| Lisa Chen | lisa.pm@ticketmasala.com | Project Management | ProjectManager |

### Technical Support

| Name | Email | Team | Level |
|------|-------|------|-------|
| David Martinez | david.support@ticketmasala.com | Technical Support | Support |
| Emma Taylor | emma.support@ticketmasala.com | Technical Support | Support |

### Finance

| Name | Email | Team | Level |
|------|-------|------|-------|
| Robert Anderson | robert.finance@ticketmasala.com | Finance | Finance |

**Permissions**:
- View and manage projects
- Create and update tickets
- Assign tasks
- View customer information
- Cannot access admin-only features

---

## ????? Customer Accounts (Limited Access)

These accounts represent external customers who can view their own projects and tickets.

| Name | Email | Customer Code | Phone |
|------|-------|---------------|-------|
| Alice Smith | alice.customer@example.com | CUST001 | +1-555-1000 |
| Bob Jones | bob.jones@example.com | CUST002 | +1-555-1001 |
| Carol White | carol.white@techcorp.com | CUST003 | +1-555-1002 |
| Daniel Brown | daniel.brown@startup.io | CUST004 | +1-555-1003 |
| Emily Davis | emily.davis@enterprise.net | CUST005 | +1-555-1004 |

**Permissions**:
- View their own projects
- View and comment on their tickets
- Submit new ticket requests
- Cannot access other customers' data

---

## ?? Sample Data

The seeder also creates sample data to help you test the system:

### Projects

1. **Website Redesign** 
   - Status: In Progress
   - Customer: Alice Smith
   - Project Manager: Mike Johnson
   - Has 3 tickets

2. **Mobile App Development**
   - Status: In Progress
   - Customer: Carol White
   - Project Manager: Lisa Chen
   - Has 2 tickets

3. **Cloud Migration**
   - Status: Pending
   - Customer: Daniel Brown
   - Project Manager: Mike Johnson

4. **CRM Integration**
   - Status: Completed
   - Customer: Alice Smith
   - Project Manager: Lisa Chen

### Sample Tickets

- Various ticket statuses (Pending, Assigned, In Progress, Completed)
- Assigned to different support staff
- Associated with different projects
- Contains comments and updates

---

## ?? Getting Started

### First Time Setup

1. **Delete your existing database** (if you want fresh seed data):
   - In SQL Server Management Studio, delete the `IT-Project2526` database
   - Or use: `DROP DATABASE [IT-Project2526]`

2. **Run the application**:
   ```bash
   dotnet run
   ```
   
3. **Database seeding will happen automatically** on first startup

### Login Examples

**To test as Admin:**
```
Email: admin@ticketmasala.com
Password: Admin123!
```

**To test as Project Manager:**
```
Email: mike.pm@ticketmasala.com
Password: Employee123!
```

**To test as Customer:**
```
Email: alice.customer@example.com
Password: Customer123!
```

---

## ?? Testing Scenarios

### Scenario 1: Admin Management
1. Login as `admin@ticketmasala.com`
2. Access Manager Dashboard
3. View all projects and tickets
4. Create new projects
5. Assign project managers

### Scenario 2: Project Manager Workflow
1. Login as `mike.pm@ticketmasala.com`
2. View assigned projects
3. Manage project tickets
4. Assign tasks to support staff
5. Update project status

### Scenario 3: Customer Portal
1. Login as `alice.customer@example.com`
2. View your projects
3. Check ticket status
4. Add comments to tickets

### Scenario 4: Support Staff
1. Login as `david.support@ticketmasala.com`
2. View assigned tickets
3. Update ticket status
4. Add progress comments
5. Mark tickets as completed

---

## ?? Security Notes

?? **Important**: These are test accounts for development purposes only!

- **DO NOT** use these credentials in production
- **DO NOT** commit actual production passwords to source control
- Change all default passwords before deploying to production
- The seed data will only be created if the database is empty

---

## ??? Customization

To modify the seed data, edit the file:
```
IT-Project2526/Data/DbSeeder.cs
```

You can customize:
- User accounts and their details
- Projects and descriptions
- Tickets and statuses
- Relationships between entities

After making changes, delete the database and restart the application to see your changes.

---

## ?? Support

If you encounter any issues with the test accounts:
1. Check the application logs for errors
2. Verify the database connection string in `appsettings.json`
3. Ensure all migrations have been applied
4. Try deleting the database and restarting the application

---

**Last Updated**: 2025
**Version**: 1.0
