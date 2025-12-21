# Ticket Masala: Architecture Improvement Checklist

**Quick Reference:** Actionable items for architectural improvements and enhanced maintainability.

---

## 🔴 Critical (High Priority Improvements)

### 1. Extract Domain Models
- [ ] Create `src/TicketMasala.Domain/TicketMasala.Domain.csproj`
- [ ] Move `Models/` → `Domain/Entities/`
- [ ] Move `MasalaDbContext` → `Domain/Data/`
- [ ] Update all namespace references
- [ ] Update `TicketMasala.Web.csproj` to reference domain project

**Estimated Effort:** 4-6 hours

### 2. Abstract Database Provider Code ✅ COMPLETE
- [x] Create `DatabaseProviderHelper` class
- [x] Extract SQLite-specific computed column SQL
- [x] Add SQL Server support for computed columns
- [x] Add PostgreSQL support
- [x] Update `MasalaDbContext.OnModelCreating()` to use helper
- [ ] Test with SQL Server (pending)

**Files Modified:**
- `src/TicketMasala.Web/Data/DatabaseProviderHelper.cs` (NEW)
- `src/TicketMasala.Web/Data/MasalaDbContext.cs` (UPDATED)

**Status:** ✅ Implemented - Ready for testing

### 3. Standardize Plugin Interface ✅ COMPLETE
- [x] Create `IStandardPlugin` interface (standardized plugin interface)
- [x] Create adapter: `ITenantPlugin` → `IStandardPlugin`
- [x] Add extension methods for conversion
- [x] Document migration path for existing plugins

**Files Created:**
- `src/TicketMasala.Web/Tenancy/PluginAdapter.cs` (NEW)

**Status:** ✅ Implemented - Ready for testing

**Note:** The adapter enables interoperability with various plugin-based systems

---

## 🟡 Important (Improve Integration Quality)

### 4. Configuration Validation
- [ ] Create `MasalaOptions` class
- [ ] Create `MasalaConfigurationValidator`
- [ ] Add validation in `Program.cs` startup
- [ ] Add helpful error messages

**Estimated Effort:** 2-3 hours

### 5. API Naming Consistency
- [ ] Create `WorkItemsController` (alias for `TicketsController`)
- [ ] Add mapping: `Ticket` → `WorkItemDto`
- [ ] Update API documentation
- [ ] Add deprecation warnings for old endpoints

**Estimated Effort:** 3-4 hours

### 6. Add API Versioning
- [ ] Install `Microsoft.AspNetCore.Mvc.Versioning` package
- [ ] Configure versioning in `Program.cs`
- [ ] Add `[ApiVersion]` attributes to controllers
- [ ] Update Swagger configuration

**Estimated Effort:** 2 hours

---

## 🟢 Nice to Have (Enhancement)

### 7. Observability
- [ ] Add OpenTelemetry
- [ ] Add structured logging (Serilog)
- [ ] Add metrics endpoints
- [ ] Add health checks

**Estimated Effort:** 4-6 hours

### 8. Test Infrastructure
- [ ] Create `IntegrationTestBase` class
- [ ] Add test data builders
- [ ] Add API contract tests
- [ ] Improve test coverage

**Estimated Effort:** 6-8 hours

### 9. Documentation
- [ ] Generate OpenAPI/Swagger docs
- [ ] Add architecture decision records (ADRs)
- [ ] Create developer onboarding guide
- [ ] Add API usage examples

**Estimated Effort:** 4-6 hours

---

## 📋 Quick Wins (Low Effort, High Value)

### 10. Code Cleanup
- [ ] Remove unused `using` statements
- [ ] Fix compiler warnings
- [ ] Add XML documentation comments
- [ ] Standardize code formatting

**Estimated Effort:** 2-3 hours

### 11. Configuration Hot Reload
- [ ] Add `FileSystemWatcher` for config changes
- [ ] Implement `ReloadConfiguration()` endpoint
- [ ] Add admin-only authorization
- [ ] Document usage

**Estimated Effort:** 3-4 hours

### 12. Error Handling
- [ ] Create global exception handler middleware
- [ ] Add consistent error response format
- [ ] Add error logging
- [ ] Add user-friendly error messages

**Estimated Effort:** 3-4 hours

---

## 🎯 Priority Order

**Week 1:**
1. Extract Domain Models (#1) - ⏳ Pending
2. Abstract Database Provider (#2) - ✅ Complete  
3. Standardize Plugin Interface (#3) - ✅ Complete

**Week 2:**
4. Configuration Validation (#4)
5. API Naming Consistency (#5)
6. Add API Versioning (#6)

**Week 3+:**
7-12. Enhancement items as needed

---

## 📝 Notes

- All estimates assume familiarity with the codebase
- Test after each change
- Create feature branches for each item
- Update documentation as you go

---

**Last Updated:** January 2025

