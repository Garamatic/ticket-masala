# ?? Quick Login Reference

## ?? TROUBLESHOOTING - If Logins Don't Work

### Option 1: Use the Test Accounts Page (EASIEST)
1. Navigate to: `https://localhost:[PORT]/Seed/TestAccounts`
2. Click the "?? Run Database Seeder" button
3. Wait for success message
4. Go back and login

### Option 2: Check Application Logs
- Look for seeding messages in the console output
- If you see "Database already contains X users. Skipping seed" - users already exist, try the passwords below
- If you see errors, check the error details

### Option 3: Manual Seeder Trigger
Navigate to: `https://localhost:[PORT]/Seed/Index` directly in your browser

---

## Admin Accounts
```
Email: admin@ticketmasala.com
Password: Admin123!
```

```
Email: sarah.admin@ticketmasala.com
Password: Admin123!
```

## Employee Accounts - Project Managers
```
Email: mike.pm@ticketmasala.com
Password: Employee123!
```

```
Email: lisa.pm@ticketmasala.com
Password: Employee123!
```

## Employee Accounts - Support
```
Email: david.support@ticketmasala.com
Password: Employee123!
```

```
Email: emma.support@ticketmasala.com
Password: Employee123!
```

## Employee Accounts - Finance
```
Email: robert.finance@ticketmasala.com
Password: Employee123!
```

## Customer Accounts
```
Email: alice.customer@example.com
Password: Customer123!
```

```
Email: bob.jones@example.com
Password: Customer123!
```

```
Email: carol.white@techcorp.com
Password: Customer123!
```

```
Email: daniel.brown@startup.io
Password: Customer123!
```

```
Email: emily.davis@enterprise.net
Password: Customer123!
```

---

## ?? Common Login Issues

### Issue: "Invalid login attempt"
**Solutions:**
1. Make sure you're using the correct password (case-sensitive!)
2. Check if the database was seeded (look for seeding logs on app startup)
3. Try the /Seed/TestAccounts page to re-run the seeder
4. Check that caps lock is OFF

### Issue: Database already has users but passwords don't work
**Solutions:**
1. If you created users manually before, those passwords won't match these
2. Delete the database and restart the app to recreate it
3. Or create new users through the registration page

### Issue: Application won't start
**Solutions:**
1. Check database connection string in appsettings.json
2. Ensure SQL Server is running
3. Check application logs for errors

---

## API Testing (if using ProjectsApiController)

**Base URL**: `https://localhost:5001/api/v1/Projects`

**Authentication**: Login with any Admin or Employee account first to get a session cookie.

**Example Endpoints**:
- GET `/api/v1/Projects` - Get all projects
- GET `/api/v1/Projects/{id}` - Get specific project
- GET `/api/v1/Projects/customer/{customerId}` - Get customer's projects
- GET `/api/v1/Projects/search?query=Website` - Search projects
- POST `/api/v1/Projects` - Create new project (Admin/Employee only)

---

## ?? Quick Start Steps

1. **Start the application**
2. **Open browser** to `https://localhost:[YOUR_PORT]/Seed/TestAccounts`
3. **Click "Run Database Seeder"** if database is empty
4. **Go to login page** and use any account above
5. **Test different roles** to see different permissions

---

See `TEST_ACCOUNTS.md` for detailed documentation.
