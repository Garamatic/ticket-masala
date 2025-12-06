# Ticket Masala - Troubleshooting Guide

This document provides comprehensive solutions to common errors and issues encountered in the Ticket Masala project.

---

## 📋 Table of Contents

1. [Current Build Errors](#current-build-errors)
2. [Security Vulnerabilities](#security-vulnerabilities)
3. [Common Runtime Errors](#common-runtime-errors)
4. [Database Issues](#database-issues)
5. [Authentication & Authorization](#authentication--authorization)
6. [Quick Reference](#quick-reference)

---

## 🔴 Current Build Errors (29 Errors, 38 Warnings)

> **Last Updated:** 2025-12-06 20:47  
> **Build Status:** ❌ FAILED  
> **Project:** TicketMasala.Web (net10.0)

### 1. Missing `QualityReviews` Property in ViewModel

**Error:**

```
CS1061: 'TicketDetailsViewModel' does not contain a definition for 'QualityReviews'
CS0117: 'TicketDetailsViewModel' does not contain a definition for 'QualityReviews'
```

**Locations:**

- `Controllers/TicketController.cs` (line 309)
- `Views/Ticket/Detail.cshtml` (lines 141, 145)
- `Engine/GERDA/Tickets/TicketService.cs` (line 295)

**Solution:**
Add the `QualityReviews` property to `TicketDetailsViewModel`:

```csharp
public class TicketDetailsViewModel
{
    // Existing properties...
    
    public List<QualityReview> QualityReviews { get; set; } = new();
}
```

---

### 2. Missing `Reviewer` Property in QualityReview

**Error:**

```
CS1061: 'QualityReview' does not contain a definition for 'Reviewer'
```

**Location:** `Controllers/TicketController.cs` (line 310)

**Solution:**
Add navigation property to `QualityReview` model:

```csharp
public class QualityReview
{
    // Existing properties...
    
    public ApplicationUser? Reviewer { get; set; }
}
```

---

### 3. Missing GERDA Namespace

**Error:**

```
CS0234: The type or namespace name 'GERDA' does not exist in the namespace 'TicketMasala.Web.Services'
```

**Location:** `Controllers/TicketController.cs` (line 320)

**Solution:**
Update the namespace reference:

```csharp
// Wrong:
using TicketMasala.Web.Services.GERDA;

// Correct:
using TicketMasala.Web.Engine.GERDA;
```

---

### 4. Missing `DepartmentId` in Project Model

**Error:**

```
CS1061: 'Project' does not contain a definition for 'DepartmentId'
```

**Locations:** `Repositories/EfCoreTicketRepository.cs` (lines 49, 66, 81, 124, 140, 158)

**Solution:**
Either add the property or remove department filtering:

```csharp
// Option A: Add to Project model
public class Project
{
    // Existing properties...
    public Guid? DepartmentId { get; set; }
}

// Option B: Remove department filtering from queries
// Remove: && p.DepartmentId == departmentId
```

---

### 5. Type Conversion - IEnumerable to List

**Error:**

```
CS0266: Cannot implicitly convert type 'IEnumerable<SelectListItem>' to 'List<SelectListItem>'
```

**Locations:**

- `Controllers/ProjectsController.cs` (line 191)
- `Services/Projects/ProjectService.cs` (line 148)

**Solution:**
Add `.ToList()` to convert:

```csharp
// Wrong:
List<SelectListItem> items = _context.Users.Select(u => new SelectListItem { ... });

// Correct:
List<SelectListItem> items = _context.Users
    .Select(u => new SelectListItem { ... })
    .ToList();
```

---

### 6. Undefined Variable `project`

**Error:**

```
CS0103: The name 'project' does not exist in the current context
```

**Location:** `Observers/NotificationProjectObserver.cs` (lines 114, 119)

**Solution:**
Declare the variable or check the method parameter name:

```csharp
// Ensure project is defined in the method
public async Task OnProjectUpdated(Project proj)
{
    // Either rename parameter to 'project' or use 'proj' in the method
}
```

---

### 7. Missing `Customer` Type

**Error:**

```
CS0246: The type or namespace name 'Customer' could not be found
```

**Locations:**

- `Data/DbSeeder.cs` (lines 346, 356, 366, 376, 386)
- `Engine/GERDA/Tickets/TicketService.cs` (line 703)

**Solution:**
Replace `Customer` with `ApplicationUser`:

```csharp
// Wrong:
Customer customer = new Customer { ... };

// Correct:
ApplicationUser customer = new ApplicationUser { ... };
```

---

### 8. Undefined Variable `title`

**Error:**

```
CS0103: The name 'title' does not exist in the current context
```

**Location:** `Engine/Ingestion/CsvImportService.cs` (line 138)

**Solution:**
Define the variable or use the correct property name:

```csharp
// Check if it should be:
var title = row["Title"] ?? row["Description"];
// or
var description = row["Description"];
```

---

### 9. Type Conversion - string to Guid?

**Error:**

```
CS0029: Cannot implicitly convert type 'string' to 'System.Guid?'
```

**Location:** `Engine/GERDA/Tickets/TicketService.cs` (line 95)

**Solution:**
Parse the string to Guid:

```csharp
// Wrong:
Guid? projectId = "some-string";

// Correct:
Guid? projectId = Guid.TryParse(stringValue, out var guid) ? guid : null;
// or
Guid? projectId = string.IsNullOrEmpty(stringValue) ? null : Guid.Parse(stringValue);
```

---

### 10. Missing `CreatorGuid` Property

**Error:**

```
CS1061: '<anonymous type>' does not contain a definition for 'CreatorGuid'
```

**Location:** `Engine/GERDA/Dispatching/MatrixFactorizationDispatchingStrategy.cs` (line 176)

**Solution:**
Add `CreatorGuid` to the anonymous type projection:

```csharp
var data = tickets.Select(t => new
{
    t.ResponsibleId,
    t.CustomerId,
    t.Status,
    t.CompletionDate,
    t.CreationDate,
    t.CreatorGuid  // Add this
});
```

---

### 11. Type Conversion - string to Status Enum

**Error:**

```
CS1503: Argument 1: cannot convert from 'string' to 'TicketMasala.Web.Models.Status'
```

**Location:** `Engine/GERDA/Dispatching/MatrixFactorizationDispatchingStrategy.cs` (line 177)

**Solution:**
Parse the string to enum:

```csharp
// Wrong:
Status status = "InProgress";

// Correct:
Status status = Enum.Parse<Status>(statusString);
// or with safety:
Status status = Enum.TryParse<Status>(statusString, out var s) ? s : Status.Pending;
```

---

### 12. Missing `Status` Property in TicketDispatchInfo

**Error:**

```
CS1061: 'TicketDispatchInfo' does not contain a definition for 'Status'
```

**Location:** `Views/Manager/DispatchBacklog.cshtml` (lines 425, 429, 435)

**Solution:**
Add `Status` property to `TicketDispatchInfo`:

```csharp
public class TicketDispatchInfo
{
    // Existing properties...
    
    public Status Status { get; set; }
}
```

---

## 🔒 Security Vulnerabilities

### Package Vulnerabilities

**Warnings:**

```
NU1902: Package 'BouncyCastle.Cryptography' 2.2.1 has known moderate severity vulnerabilities
NU1903: Package 'MimeKit' 4.3.0 has a known high severity vulnerability
```

**Solution:**
Update vulnerable packages to their latest secure versions:

```bash
# Update BouncyCastle.Cryptography
dotnet add package BouncyCastle.Cryptography --version 2.4.0

# Update MimeKit
dotnet add package MimeKit --version 4.8.0

# Or update all packages
dotnet list package --outdated
dotnet add package <PackageName> --version <LatestVersion>
```

**Prevention:**

- Regularly run `dotnet list package --vulnerable` to check for vulnerabilities
- Set up automated dependency updates (e.g., Dependabot on GitHub)
- Subscribe to security advisories for critical packages

---

## ⚠️ Common Runtime Errors

### 1. NullReferenceException

**Symptoms:** Application crashes with "Object reference not set to an instance of an object"

**Common Causes:**

- Accessing properties on null objects
- Missing null checks
- Uninitialized collections

**Solution:**

```csharp
// Use null-conditional operators
var name = user?.FirstName ?? "Unknown";

// Check for null before accessing
if (project != null)
{
    var tasks = project.Tasks;
}

// Initialize collections
public List<Task> Tasks { get; set; } = new();
```

---

### 2. EntityFramework Tracking Issues

**Symptoms:** "The instance of entity type cannot be tracked because another instance with the same key value is already being tracked"

**Solution:**

```csharp
// Use AsNoTracking() for read-only queries
var projects = await _context.Projects
    .AsNoTracking()
    .ToListAsync();

// Or detach the entity
_context.Entry(entity).State = EntityState.Detached;
```

---

### 3. Circular Reference in JSON Serialization

**Symptoms:** "A possible object cycle was detected"

**Solution:**
Add to `Program.cs`:

```csharp
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = 
            System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });
```

---

## 💾 Database Issues

### 1. Connection Refused

**Error:** `Microsoft.Data.SqlClient.SqlException: Connection refused`

**Solution:**

1. Check if SQL Server is running:

```bash
# For Docker:
docker ps | grep sql

# Start SQL Server container:
docker-compose up -d
```

2. Verify connection string in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=TicketMasala;User Id=sa;Password=YourPassword;TrustServerCertificate=True;"
  }
}
```

3. Test connection:

```bash
dotnet ef database update
```

---

### 2. Migration Errors

**Error:** "Pending model changes" or migration conflicts

**Solution:**

```bash
# Remove last migration
dotnet ef migrations remove

# Create new migration
dotnet ef migrations add YourMigrationName

# Apply migrations
dotnet ef database update

# Reset database (WARNING: deletes all data)
dotnet ef database drop
dotnet ef database update
```

---

### 3. Seed Data Not Loading

**Symptoms:** Database is empty after first run

**Solution:**

1. Check logs for "Database already contains users"
2. Manually trigger seeding by navigating to `/Seed/TestAccounts`
3. Verify `DbSeeder.cs` is called in `Program.cs`:

```csharp
using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<DbSeeder>();
    await seeder.SeedAsync();
}
```

---

## 🔐 Authentication & Authorization

### 1. Login Failed

**Symptoms:** Valid credentials rejected

**Solutions:**

1. **Reset password:**

```bash
dotnet ef database drop
dotnet run
```

2. **Check default passwords:**

- Admins: `Admin123!`
- Employees: `Employee123!`
- Customers: `Customer123!`

3. **Verify user exists in database:**

```sql
SELECT * FROM AspNetUsers WHERE Email = 'admin@ticketmasala.com';
```

---

### 2. Unauthorized Access (403)

**Symptoms:** User logged in but cannot access certain pages

**Solution:**

1. Check role assignment:

```csharp
var roles = await _userManager.GetRolesAsync(user);
```

2. Verify `[Authorize]` attributes:

```csharp
[Authorize(Roles = Constants.RoleEmployee + "," + Constants.RoleAdmin)]
```

3. Ensure roles are seeded:

```csharp
// In DbSeeder.cs
await _roleManager.CreateAsync(new IdentityRole(Constants.RoleAdmin));
await _userManager.AddToRoleAsync(user, Constants.RoleAdmin);
```

---

## 📚 Quick Reference

### Build Commands

```bash
# Clean build
dotnet clean
dotnet build

# Run application
dotnet run

# Run with specific environment
dotnet run --environment Development

# Watch mode (auto-reload)
dotnet watch run
```

### Database Commands

```bash
# Create migration
dotnet ef migrations add MigrationName

# Update database
dotnet ef database update

# Drop database
dotnet ef database drop

# List migrations
dotnet ef migrations list

# Generate SQL script
dotnet ef migrations script
```

### Package Management

```bash
# List packages
dotnet list package

# Check for outdated packages
dotnet list package --outdated

# Check for vulnerable packages
dotnet list package --vulnerable

# Add package
dotnet add package PackageName

# Remove package
dotnet remove package PackageName
```

### Testing

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true

# Run specific test
dotnet test --filter "FullyQualifiedName~TestName"
```

---

## 🆘 Getting Help

If you encounter an error not covered in this guide:

1. **Check the logs:** Look in the console output for detailed error messages
2. **Search the codebase:** Use `grep` or IDE search to find similar patterns
3. **Check conversation history:** Reference previous solutions in conversation summaries
4. **Build logs:** Review `build_log.txt` and `run_log.txt` for detailed error traces
5. **Documentation:** Check official .NET and EF Core documentation

---

## 📝 Contributing to This Guide

When you encounter and fix a new error:

1. Document the error code and message
2. Note the file and line number
3. Explain the root cause
4. Provide the solution with code examples
5. Add prevention tips if applicable

---

*Last Updated: 2025-12-06*
