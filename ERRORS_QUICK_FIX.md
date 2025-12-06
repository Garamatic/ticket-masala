# Quick Fix Guide - Priority Errors

> **Build Status:** 29 errors blocking compilation  
> **Priority:** Fix these in order for fastest resolution

---

## 🚨 Critical Fixes (Must Fix First)

### 1. Replace `Customer` with `ApplicationUser` (5 errors)

**Files to fix:**

- `src/TicketMasala.Web/Data/DbSeeder.cs` (lines 346, 356, 366, 376, 386)
- `src/TicketMasala.Web/Engine/GERDA/Tickets/TicketService.cs` (line 703)

**Find:** `Customer`  
**Replace:** `ApplicationUser`

```bash
# Quick fix with sed (backup first!)
cd src/TicketMasala.Web
find . -name "*.cs" -exec sed -i.bak 's/\bCustomer\b customer/ApplicationUser customer/g' {} \;
```

---

### 2. Add Missing ViewModel Properties (3 errors)

**File:** `src/TicketMasala.Web/ViewModels/Tickets/TicketDetailsViewModel.cs`

```csharp
public class TicketDetailsViewModel
{
    // ... existing properties
    
    // ADD THIS:
    public List<QualityReview> QualityReviews { get; set; } = new();
}
```

---

### 3. Fix Project DepartmentId (6 errors)

**File:** `src/TicketMasala.Web/Models/Project.cs`

```csharp
public class Project
{
    // ... existing properties
    
    // ADD THIS (or remove all DepartmentId references):
    public Guid? DepartmentId { get; set; }
}
```

---

### 4. Fix IEnumerable to List Conversions (2 errors)

**Files:**

- `src/TicketMasala.Web/Controllers/ProjectsController.cs` (line 191)
- `src/TicketMasala.Web/Services/Projects/ProjectService.cs` (line 148)

**Add `.ToList()` at the end of the LINQ query:**

```csharp
// Before:
List<SelectListItem> items = query.Select(...);

// After:
List<SelectListItem> items = query.Select(...).ToList();
```

---

### 5. Fix GERDA Namespace (1 error)

**File:** `src/TicketMasala.Web/Controllers/TicketController.cs` (line 320)

```csharp
// Wrong:
using TicketMasala.Web.Services.GERDA;

// Correct:
using TicketMasala.Web.Engine.GERDA;
```

---

### 6. Add QualityReview.Reviewer Property (1 error)

**File:** `src/TicketMasala.Web/Models/QualityReview.cs`

```csharp
public class QualityReview
{
    // ... existing properties
    
    // ADD THIS:
    public ApplicationUser? Reviewer { get; set; }
}
```

---

### 7. Fix Undefined Variables

**File:** `src/TicketMasala.Web/Observers/NotificationProjectObserver.cs` (lines 114, 119)

Check the method signature - likely the parameter is named differently:

```csharp
// If parameter is 'proj', either:
// Option A: Rename parameter
public async Task OnProjectUpdated(Project project) { ... }

// Option B: Use correct name
public async Task OnProjectUpdated(Project proj) 
{
    // Use 'proj' instead of 'project'
}
```

---

**File:** `src/TicketMasala.Web/Engine/Ingestion/CsvImportService.cs` (line 138)

```csharp
// Add variable declaration:
var title = row.ContainsKey("Title") ? row["Title"] : row["Description"];
```

---

### 8. Fix Type Conversions

**File:** `src/TicketMasala.Web/Engine/GERDA/Tickets/TicketService.cs` (line 95)

```csharp
// Wrong:
Guid? projectId = someString;

// Correct:
Guid? projectId = Guid.TryParse(someString, out var guid) ? guid : null;
```

---

**File:** `src/TicketMasala.Web/Engine/GERDA/Dispatching/MatrixFactorizationDispatchingStrategy.cs`

Line 176 - Add `CreatorGuid` to projection:

```csharp
var data = tickets.Select(t => new
{
    t.ResponsibleId,
    t.CustomerId,
    t.Status,
    t.CompletionDate,
    t.CreationDate,
    t.CreatorGuid  // ADD THIS
});
```

Line 177 - Parse Status enum:

```csharp
// Wrong:
Status status = statusString;

// Correct:
Status status = Enum.Parse<Status>(statusString);
```

---

### 9. Add TicketDispatchInfo.Status Property (3 errors)

**File:** Find the `TicketDispatchInfo` class (likely in ViewModels)

```csharp
public class TicketDispatchInfo
{
    // ... existing properties
    
    // ADD THIS:
    public Status Status { get; set; }
}
```

---

## ⚠️ Warnings to Address Later (38 warnings)

### Duplicate Using Directives (4 warnings)

Remove duplicate `using` statements in:

- `Engine/GERDA/GerdaService.cs` (lines 7-8)
- `Engine/GERDA/Ranking/WeightedShortestJobFirstStrategy.cs` (line 3)
- `Engine/GERDA/Tickets/TicketService.cs` (line 9)

### Non-nullable Properties (20+ warnings)

Add `required` modifier or make nullable:

```csharp
// Option 1: Make required
public required string Description { get; set; }

// Option 2: Make nullable
public string? Description { get; set; }

// Option 3: Initialize
public string Description { get; set; } = string.Empty;
```

---

## 🔄 Build & Test

After making fixes:

```bash
# Clean and rebuild
dotnet clean
dotnet build

# If successful, run the app
dotnet run
```

---

## 📊 Progress Tracker

- [ ] Fix Customer → ApplicationUser (5 errors)
- [ ] Add TicketDetailsViewModel.QualityReviews (3 errors)
- [ ] Add Project.DepartmentId (6 errors)
- [ ] Fix .ToList() conversions (2 errors)
- [ ] Fix GERDA namespace (1 error)
- [ ] Add QualityReview.Reviewer (1 error)
- [ ] Fix undefined variables (2 errors)
- [ ] Fix type conversions (3 errors)
- [ ] Add TicketDispatchInfo.Status (3 errors)
- [ ] Address warnings (38 warnings)

**Total:** 29 errors → 0 errors ✅

---

*Estimated time to fix all critical errors: 15-30 minutes*
