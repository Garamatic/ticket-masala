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

## 🔴 Current Build Errors

### 1. Missing Properties in ViewModels

**Error:**

```
CS1061: 'TicketSearchViewModel' does not contain a definition for 'ProjectId'
CS1061: 'TicketSearchViewModel' does not contain a definition for 'AssignedToId'
CS1061: 'TicketSearchViewModel' does not contain a definition for 'IsOverdue'
CS1061: 'TicketSearchViewModel' does not contain a definition for 'IsDueSoon'
```

**Location:**

- `Controllers/TicketController.cs` (lines 96, 97, 99, 100, 121, 122, 124, 125)
- `Views/Ticket/Index.cshtml` (lines 129, 130)

**Solution:**
Add missing properties to `TicketSearchViewModel`:

```csharp
public class TicketSearchViewModel
{
    // Existing properties...
    
    // Add these properties:
    public Guid? ProjectId { get; set; }
    public string? AssignedToId { get; set; }
    public bool IsOverdue { get; set; }
    public bool IsDueSoon { get; set; }
}
```

---

### 2. Missing DepartmentId Property

**Error:**

```
CS1061: 'ApplicationUser' does not contain a definition for 'DepartmentId'
```

**Location:** `Services/TicketService.cs` (line 82)

**Solution:**
Either:

- **Option A:** Add `DepartmentId` property to `ApplicationUser` model
- **Option B:** Remove department-based filtering if not needed
- **Option C:** Use a different approach (e.g., through roles or claims)

```csharp
// Option A: Add to ApplicationUser
public class ApplicationUser : IdentityUser
{
    // Existing properties...
    public Guid? DepartmentId { get; set; }
}

// Option B: Remove the line or replace with alternative logic
// Remove: user.DepartmentId
```

---

### 3. Missing Service Injection

**Error:**

```
CS0103: The name '_anticipationService' does not exist in the current context
```

**Location:** `Controllers/ManagerController.cs` (line 76)

**Solution:**

1. Add the service field and inject it in the constructor:

```csharp
public class ManagerController : Controller
{
    private readonly IAnticipationService _anticipationService;
    
    public ManagerController(
        // ... other parameters
        IAnticipationService anticipationService)
    {
        // ... other assignments
        _anticipationService = anticipationService;
    }
}
```

2. Register the service in `Program.cs`:

```csharp
builder.Services.AddScoped<IAnticipationService, AnticipationService>();
```

---

### 4. Type Conversion Issues

**Error:**

```
CS0029: Cannot implicitly convert type 'List<string>' to 'List<TicketComment>'
CS0019: Operator '??' cannot be applied to operands of type 'List<TicketComment>' and 'List<string>'
```

**Location:**

- `Services/TicketService.cs` (line 180)
- `Controllers/ManagerController.cs` (line 134)

**Solution:**
Fix the type mismatch:

```csharp
// Wrong:
ticket.Comments = new List<string>(); // or ticket.Comments ?? new List<string>()

// Correct:
ticket.Comments = new List<TicketComment>();
// or
ticket.Comments ??= new List<TicketComment>();
```

---

### 5. Missing Enum Value

**Error:**

```
CS0117: 'TicketType' does not contain a definition for 'Subtask'
```

**Location:** `Data/DbSeeder.cs` (lines 451, 468, 484, 510)

**Solution:**
Add `Subtask` to the `TicketType` enum:

```csharp
public enum TicketType
{
    Bug,
    Feature,
    Support,
    Subtask  // Add this
}
```

Or replace with an existing enum value:

```csharp
// Replace:
TicketType = TicketType.Subtask,
// With:
TicketType = TicketType.Support,
```

---

### 6. Missing Model Properties

**Error:**

```
CS0117: 'Ticket' does not contain a definition for 'CreatedDate'
CS0117: 'Ticket' does not contain a definition for 'DepartmentId'
```

**Location:** `Services/CsvImportService.cs` (lines 48, 50)

**Solution:**
Use the correct property names from the `Ticket` model:

```csharp
// Replace:
CreatedDate = DateTime.UtcNow,  // Wrong
DepartmentId = departmentId,     // Wrong

// With:
CreationDate = DateTime.UtcNow,  // Correct
// Remove DepartmentId or add to model
```

---

### 7. Required Members Not Set

**Error:**

```
CS9035: Required member 'Ticket.Description' must be set in the object initializer
CS9035: Required member 'Ticket.Customer' must be set in the object initializer
CS9035: Required member 'ApplicationUser.Phone' must be set in the object initializer
```

**Location:**

- `Services/CsvImportService.cs` (line 45)
- `Services/EmailIngestionService.cs` (line 83)

**Solution:**
Set all required properties in object initializers:

```csharp
// For Ticket:
var ticket = new Ticket
{
    Description = row["description"] ?? "No description",  // Add this
    Customer = customer,                                    // Add this
    // ... other properties
};

// For ApplicationUser:
var user = new ApplicationUser
{
    Email = email,
    UserName = email,
    Phone = phone ?? string.Empty,  // Add this
    // ... other properties
};
```

---

### 8. Missing Method

**Error:**

```
CS1061: 'IFileService' does not contain a definition for 'GetFileAsync'
```

**Location:** `Controllers/TicketController.cs` (lines 440, 452)

**Solution:**
Either:

- **Option A:** Add the method to `IFileService` interface and implementation
- **Option B:** Use the correct method name

```csharp
// Option A: Add to IFileService
public interface IFileService
{
    Task<byte[]?> GetFileAsync(Guid fileId);
    // ... other methods
}

// Option B: Check if method exists with different name
var file = await _fileService.GetFile(fileId);  // or similar
```

---

### 9. Undefined Variable

**Error:**

```
CS0103: The name 'ticket' does not exist in the current context
```

**Location:** `Controllers/TicketController.cs` (line 234)

**Solution:**
Ensure the variable is declared before use:

```csharp
// Check if 'ticket' is declared earlier in the method
var ticket = await _context.Tickets.FindAsync(id);
if (ticket == null)
{
    return NotFound();
}
// Now you can use 'ticket'
```

---

### 10. Header Dictionary Issue

**Error:**

```
ASP0019: Use IHeaderDictionary.Append or the indexer to append or set headers
```

**Location:** `Controllers/TicketController.cs` (line 456)

**Solution:**
Use the correct method to add headers:

```csharp
// Wrong:
Response.Headers.Add("Content-Disposition", "attachment; filename=file.pdf");

// Correct:
Response.Headers.Append("Content-Disposition", "attachment; filename=file.pdf");
// or
Response.Headers["Content-Disposition"] = "attachment; filename=file.pdf";
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
