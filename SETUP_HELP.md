# ?? FIRST TIME SETUP - READ THIS FIRST!

## If Login Isn't Working, Follow These Steps:

### Step 1: Find Your Application Port
When you run the application, look for a line like:
```
Now listening on: https://localhost:5001
```
Note down this port number (5001 in this example).

### Step 2: Open the Test Accounts Page
In your web browser, navigate to:
```
https://localhost:5001/Seed/TestAccounts
```
(Replace 5001 with your actual port number)

### Step 3: Run the Seeder
Click the **"?? Run Database Seeder"** button on the page.

Wait for the success message.

### Step 4: Login
Click the **"?? Go to Login Page"** button or navigate to `/Identity/Account/Login`

Use any of these accounts:
- **Admin**: `admin@ticketmasala.com` / `Admin123!`
- **Employee**: `mike.pm@ticketmasala.com` / `Employee123!`
- **Customer**: `alice.customer@example.com` / `Customer123!`

---

## Still Not Working?

### Check 1: Is the database empty?
Look at the console output when you run the app. You should see:
```
========== DATABASE SEEDING STARTED ==========
Current user count in database: 0
Database is empty. Starting to create test accounts...
```

If you see `Current user count in database: 5` (or any number > 0), it means users already exist.

### Check 2: See any errors?
Look for error messages in the console. They will tell you what's wrong.

### Check 3: Did seeding complete successfully?
You should see at the end:
```
========== DATABASE SEEDING COMPLETED SUCCESSFULLY! ==========
You can now login with any of the test accounts
```

### Check 4: Database Connection
Make sure SQL Server is running and the connection string in `appsettings.json` is correct.

---

## Nuclear Option: Start Fresh

If nothing works, start completely fresh:

### Windows (PowerShell):
```powershell
# Stop the application (Ctrl+C)
cd IT-Project2526
dotnet ef database drop --force
dotnet run
```

### Then:
1. Wait for app to start
2. Go to `/Seed/TestAccounts`
3. Click "Run Database Seeder"
4. Login!

---

## Manual Check: Are Users in the Database?

Connect to your SQL Server database and run:
```sql
SELECT UserName, Email, Discriminator 
FROM AspNetUsers;
```

You should see users listed. If not, the seeder didn't run.

---

## Quick Debug Checklist

- [ ] Application is running (console shows "Now listening on...")
- [ ] Database exists (check in SQL Server Management Studio)
- [ ] Navigated to `/Seed/TestAccounts` page
- [ ] Clicked "Run Database Seeder" button
- [ ] Saw success message
- [ ] Using correct password (case-sensitive, with !)
- [ ] Caps Lock is OFF

---

## Contact Developer

If you're still stuck, check the application logs for detailed error messages.
The logs show exactly what's failing during the seeding process.
