# Ticket Masala: Architecture Improvement Checklist

**Quick Reference:** Actionable items for architectural improvements and enhanced maintainability.

---

## 🔴 Critical (High Priority Improvements)

### 1. Extract Domain Models COMPLETE
- [x] Create `TicketMasala.Domain` project
- [x] Move entities (`Ticket`, `Project`, etc.) to Domain
- [x] Move `MasalaDbContext` to Domain
- [x] Update namespaces in Web project
- [x] Remove shim files (`MasalaDbContext.cs`, etc.) in Web project

**Estimated Effort:** 4-6 hours

### 2. Abstract Database Provider Code COMPLETE
- [x] Create `DatabaseProviderHelper` class
- [x] Extract SQLite-specific computed column SQL
- [x] Add SQL Server support for computed columns
- [x] Add PostgreSQL support
- [x] Update `MasalaDbContext.OnModelCreating()` to use helper
- [ ] Test with SQL Server (pending)

**Files Modified:**
- `src/TicketMasala.Web/Data/DatabaseProviderHelper.cs` (NEW)
- `src/TicketMasala.Web/Data/MasalaDbContext.cs` (UPDATED)

**Status:** Implemented - Ready for testing

### 3. Standardize Plugin Interface COMPLETE
- [x] Create `IStandardPlugin` interface (standardized plugin interface)
- [x] Create adapter: `ITenantPlugin` → `IStandardPlugin`
- [x] Add extension methods for conversion
- [x] Document migration path for existing plugins

**Files Created:**
- `src/TicketMasala.Web/Tenancy/PluginAdapter.cs` (NEW)

**Status:** Implemented - Ready for testing

**Note:** The adapter enables interoperability with various plugin-based systems

---

## 🟡 Important (Improve Integration Quality)

### 4. Configuration Validation COMPLETE
- [x] Create `MasalaOptions` class
- [x] Create `MasalaConfigurationValidator`
- [x] Add validation in `Program.cs` startup
- [x] Add helpful error messages

**Estimated Effort:** 2-3 hours

### 5. API Naming Consistency COMPLETE
- [x] Create `WorkItemsController` (alias for `TicketsController`)
- [x] Add mapping: `Ticket` → `WorkItemDto`
- [x] Update API documentation
- [x] Add deprecation warnings for old endpoints

**Estimated Effort:** 3-4 hours

### 6. Add API Versioning COMPLETE
- [x] Install `Microsoft.AspNetCore.Mvc.Versioning` package
- [x] Configure versioning in `Program.cs`
- [x] Add `[ApiVersion]` attributes to controllers
- [x] Update Swagger configuration

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

### 10. Code Cleanup COMPLETE
- [x] Remove unused `using` statements
- [x] Fix compiler warnings
- [ ] Add XML documentation comments
- [x] Standardize code formatting

**Estimated Effort:** 2-3 hours

### 11. Configuration Hot Reload COMPLETE
- [x] Add `FileSystemWatcher` for config changes
- [x] Implement `ReloadConfiguration()` endpoint
- [x] Add admin-only authorization
- [x] Document usage

**Estimated Effort:** 3-4 hours

### 12. Error Handling COMPLETE
- [x] Create global exception handler middleware
- [x] Add consistent error response format
- [x] Add error logging
- [x] Add user-friendly error messages

**Estimated Effort:** 3-4 hours

---

## Priority Order

**Week 1:**
1. Extract Domain Models (#1) - ⏳ Pending
2. Abstract Database Provider (#2) - Complete  
3. Standardize Plugin Interface (#3) - Complete

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

