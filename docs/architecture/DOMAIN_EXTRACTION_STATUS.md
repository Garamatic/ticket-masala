# Domain Model Extraction - Status

**Date:** January 2025  
**Status:** 🟡 In Progress (70% Complete)

---

## ✅ Completed

1. **Created TicketMasala.Domain Project**
   - Project file created
   - Directory structure established (`Entities/`, `Common/`)

2. **Moved Common Types**
   - `BaseModel.cs` → `Domain/Common/BaseModel.cs`
   - `Enums.cs` → `Domain/Common/Enums.cs`

3. **Moved Entity Models**
   - `Notification.cs` → `Domain/Entities/Notification.cs`
   - `Document.cs` → `Domain/Entities/Document.cs`
   - `TicketComment.cs` → `Domain/Entities/TicketComment.cs`
   - `TimeLog.cs` → `Domain/Entities/TimeLog.cs`
   - `AuditLogEntry.cs` → `Domain/Entities/AuditLogEntry.cs`
   - `KnowledgeBaseArticle.cs` → `Domain/Entities/KnowledgeBaseArticle.cs`
   - `QualityReview.cs` → `Domain/Entities/QualityReview.cs`
   - `SavedFilter.cs` → `Domain/Entities/SavedFilter.cs`
   - `Resource.cs` → `Domain/Entities/Resource.cs`
   - `Setting.cs` → `Domain/Entities/Setting.cs`
   - `ProjectTemplate.cs` → `Domain/Entities/ProjectTemplate.cs`
   - `TemplateTicket.cs` → `Domain/Entities/TemplateTicket.cs`
   - `DomainConfigVersion.cs` → `Domain/Entities/DomainConfigVersion.cs`
   - `Ticket.cs` → `Domain/Entities/Ticket.cs`
   - `Project.cs` → `Domain/Entities/Project.cs`

4. **Updated Project References**
   - Added Domain project reference to Web project
   - Updated solution file to include Domain project

5. **Updated DbContext**
   - `MasalaDbContext.cs` now references `TicketMasala.Domain.Entities`
   - Added EF Core configuration for ApplicationUser relationships
   - Updated `DatabaseProviderHelper.cs` to use Domain entities

---

## 🟡 Remaining Work

### Namespace Updates Required

**Approximately 169 files** need namespace updates from:
- `using TicketMasala.Web.Models;` 
- `namespace TicketMasala.Web.Models;`

To:
- `using TicketMasala.Domain.Entities;`
- `using TicketMasala.Domain.Common;`
- `namespace TicketMasala.Domain.Entities;` (for model files)

### Files That Need Updates

**Key areas:**
1. **Repositories** (~10 files)
   - `Repositories/ITicketRepository.cs`
   - `Repositories/EfCoreTicketRepository.cs`
   - `Repositories/IProjectRepository.cs`
   - `Repositories/EfCoreProjectRepository.cs`
   - `Repositories/IUserRepository.cs`
   - `Repositories/EfCoreUserRepository.cs`
   - `Repositories/Specifications/TicketSpecifications.cs`

2. **Services** (~20 files)
   - `Engine/Projects/ProjectService.cs`
   - `Engine/GERDA/Tickets/TicketService.cs`
   - `Engine/Ingestion/*.cs`

3. **Controllers** (~15 files)
   - `Controllers/TicketController.cs`
   - `Controllers/ProjectsController.cs`
   - All controller files

4. **ViewModels** (~10 files)
   - `ViewModels/Tickets/*.cs`
   - `ViewModels/Projects/*.cs`

5. **Observers** (~5 files)
   - `Observers/*.cs`

6. **Views** (Razor files)
   - `Views/**/*.cshtml`
   - `Areas/**/Views/**/*.cshtml`

7. **Other**
   - `Extensions/*.cs`
   - `TagHelpers/*.cs`
   - `Program.cs`

---

## 🔧 Migration Strategy

### Option 1: Automated Find & Replace (Recommended)

Use IDE find & replace with regex:

**Find:** `using TicketMasala\.Web\.Models;`  
**Replace:** `using TicketMasala.Domain.Entities;\nusing TicketMasala.Domain.Common;`

**Find:** `TicketMasala\.Web\.Models\.(Status|TicketType|Priority|ReviewStatus|Category|SubCategory|EmployeeType)`  
**Replace:** `TicketMasala.Domain.Common.$1`

**Find:** `TicketMasala\.Web\.Models\.(Ticket|Project|Notification|Document|TicketComment|TimeLog|AuditLogEntry|KnowledgeBaseArticle|QualityReview|SavedFilter|Resource|Setting|ProjectTemplate|TemplateTicket|DomainConfigVersion)`  
**Replace:** `TicketMasala.Domain.Entities.$1`

### Option 2: Manual Update by Category

1. Update Common types first (Enums, BaseModel)
2. Update Entity types
3. Update ApplicationUser references (stays in Web.Models)

### Option 3: Use Global Usings

Add to `TicketMasala.Web.csproj`:
```xml
<ItemGroup>
  <Using Include="TicketMasala.Domain.Entities" />
  <Using Include="TicketMasala.Domain.Common" />
</ItemGroup>
```

Then remove individual `using` statements.

---

## 📝 Notes

### ApplicationUser Stays in Web

`ApplicationUser`, `Employee`, and `Guest` remain in `TicketMasala.Web.Models` because:
- They extend `IdentityUser` (ASP.NET Identity concern)
- They are infrastructure/presentation concerns, not domain concerns
- Domain entities reference users via `string UserId` properties
- EF Core configures relationships in `MasalaDbContext`

### Validation Attributes

Domain models use standard `DataAnnotations`:
- `[Required]`, `[StringLength]`, `[MaxLength]`, `[Range]`

Web-specific validation attributes (`SafeStringLength`, `NoHtml`) are removed from Domain models. These can be added back in:
- ViewModels (for presentation validation)
- Fluent API configuration (for database constraints)

### Navigation Properties

Domain models use **string IDs** for user references instead of navigation properties:
- `CustomerId` instead of `Customer`
- `ResponsibleId` instead of `Responsible`
- `UserId` instead of `User`

EF Core configures these relationships in `MasalaDbContext.ConfigureUserRelationships()`.

---

## ✅ Next Steps

1. **Run automated find & replace** for namespace updates
2. **Build solution** to identify any remaining issues
3. **Update Razor views** (`@using` directives)
4. **Test compilation** and fix any errors
5. **Run tests** to ensure nothing broke
6. **Update documentation** with new structure

---

## 🎯 Completion Checklist

- [x] Create Domain project
- [x] Move BaseModel and Enums
- [x] Move all entity models
- [x] Update project references
- [x] Update solution file
- [x] Update DbContext
- [ ] Update all namespace references (~169 files)
- [ ] Update Razor views
- [ ] Build and test
- [ ] Update tests project references
- [ ] Remove old Models directory

---

**Estimated Remaining Time:** 2-3 hours for namespace updates and testing

