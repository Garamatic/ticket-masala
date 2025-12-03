# Database Re-seeding Instructions

## Quick Reset & Reseed

To test the GERDA Dispatch feature with fresh bogus data including 15 unassigned tickets:

### Option 1: SQL Server Management Studio (Recommended)
1. Open SQL Server Management Studio
2. Connect to your LocalDB instance: `(localdb)\MSSQLLocalDB`
3. Open the file: `SQL/ClearDatabase.sql`
4. Execute the script (F5)
5. Run the application: `dotnet run`
6. The seeder will automatically create all test data including unassigned tickets

### Option 2: Command Line
```bash
# Navigate to project directory
cd IT-Project2526

# Execute the clear script
sqlcmd -S "(localdb)\MSSQLLocalDB" -i ../SQL/ClearDatabase.sql

# Run the application (will auto-seed)
dotnet run
```

### Option 3: Drop and Recreate Database (Nuclear option)
```bash
# In the IT-Project2526 directory
dotnet ef database drop --force
dotnet ef database update
dotnet run
```

## What Gets Created

After re-seeding, you'll have:

### Users
- **2 Admins** (admin@ticketmasala.com, sarah.admin@ticketmasala.com)
- **5 Employees** across different teams (Support, Finance, PM)
- **5 Customers** 

### Projects
- 4 sample projects in different states

### Tickets
- **5 assigned tickets** (for existing functionality)
- **15 unassigned tickets** for GERDA Dispatch testing, including:
  - 3 Hardware Support tickets
  - 2 Network Troubleshooting tickets
  - 3 Software Troubleshooting tickets
  - 2 Password Reset tickets
  - 2 Payroll tickets
  - 1 Refund Request ticket
  - 2 DevOps/Infrastructure tickets

All unassigned tickets have:
- Different priority scores
- Estimated effort points
- GERDA tags for categorization
- Various completion targets (urgent to long-term)

## Test GERDA Dispatch

1. Login as admin: **admin@ticketmasala.com** / **Admin123!**
2. Click **"GERDA Dispatch"** in the sidebar
3. You should see 15 unassigned tickets with AI recommendations
4. Test single assignment by clicking an agent recommendation
5. Test batch assignment:
   - Select multiple tickets using checkboxes
   - Click "Auto-Assign Selected (GERDA)" for AI assignment
   - Or click "Manual Batch Assign" to assign all to one agent

## Login Credentials

All passwords are the same pattern: `[Role]123!`

- Admin: `admin@ticketmasala.com` / `Admin123!`
- Employee: `mike.pm@ticketmasala.com` / `Employee123!`
- Customer: `alice.customer@example.com` / `Customer123!`
