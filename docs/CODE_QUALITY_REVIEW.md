
---

### 4. Violation: Dependency Inversion Principle (SOLID) & Runtime Safety
**Severity:** Critical
**Location:** `TicketMasala.Web.Engine.Ingestion.EmailIngestionService` & `SimpleSentimentAnalyzer`

**Issue:**
1.  **Hardcoded Dependency:** `SimpleSentimentAnalyzer` was a `static` class, making it impossible to mock in unit tests and tightly coupling consumers to its implementation.
2.  **Missing Registration (Broken Dependency):** `EmailIngestionService` depended on `IEmailTicketProcessor`, but `IEmailTicketProcessor` was only registered in an unused extension method (`AddCoreServices`), causing a potential runtime crash when the background service started.

**Refactoring:**
1.  Refactored `SimpleSentimentAnalyzer` to implement a new `ISentimentAnalyzer` interface.
2.  Injected `ISentimentAnalyzer` into `EmailTicketProcessor` and `EnrichmentBackgroundService`.
3.  Correctly registered `IEmailTicketProcessor` and `ISentimentAnalyzer` in `WebApplicationBuilderExtensions.cs` (the active composition root).

**Before (Static Call):**
```csharp
// Tightly coupled static call
var (urgencyScore, sentimentLabel) = SimpleSentimentAnalyzer.Analyze(email.Subject, email.Body);
```

**After (Dependency Injection):**
```csharp
// Injected interface
var (urgencyScore, sentimentLabel) = _sentimentAnalyzer.Analyze(email.Subject, email.Body);
```

**Impact:** Enabled unit testing for `EmailTicketProcessor` (mocking sentiment analysis) and prevented a critical runtime failure in the background ingestion service.

---

### 5. Violation: Dead Code (YAGNI)
**Severity:** Low (Maintenance Debt)
**Location:** `TicketMasala.Web.Extensions.CoreServiceCollectionExtensions.cs`

**Issue:**
The file contained a deprecated service registration method `AddCoreServices` that was no longer used by the application (superseded by `WebApplicationBuilderExtensions.cs`). However, it still contained unique service registrations (like `EnrichmentBackgroundService`) that were missing from the active composition root, leading to potential confusion and runtime errors if developers assumed they were registered.

**Refactoring:**
1.  Identified services present in `CoreServiceCollectionExtensions.cs` but missing from `WebApplicationBuilderExtensions.cs` (e.g., `EnrichmentBackgroundService`, `IEnrichmentQueue`).
2.  Migrated missing registrations to `WebApplicationBuilderExtensions.cs`.
3.  Deleted `CoreServiceCollectionExtensions.cs`.

**Impact:** Removed 100+ lines of dead code, eliminated a source of confusion, and ensured all active services are registered in a single, authoritative location.

---

### 6. Violation: Redundant Code (DRY/KISS) & Security Risk
**Severity:** High (Security & Maintenance)
**Location:** `LocalFileStorageService.cs` (renamed), `FileService.cs`, `TicketAttachmentsController.cs`

**Issue:**
1.  **Duplicate Implementation:** Two services (`FileService` and `LocalFileStorageService`) implemented nearly identical file storage logic, violating DRY.
2.  **Security Risk:** `LocalFileStorageService` concatenated paths without sanitizing the file ID (`Path.Combine(_storagePath, fileId)`), creating a Directory Traversal vulnerability.
3.  **Hardcoded Path:** `LocalFileStorageService` used a hardcoded path (`App_Data/Uploads`) without configuration support.

**Refactoring:**
1.  **Consolidation:** Renamed `LocalFileStorageService` to `DiskFileStorageService` and made it the single source of truth for file storage, implementing `IFileStorageService`.
2.  **Security Hardening:** Added `Path.GetFileName(fileId)` to sanitize inputs, preventing directory traversal attacks.
3.  **Configurability:** Added `IConfiguration` support to allow overriding the storage path via `Storage:Path` setting.
4.  **Cleanup:** Migrated `TicketAttachmentsController` to use `DiskFileStorageService` and deleted the redundant `FileService.cs` and `IFileService.cs`.

**Key Code (Secure Path Handling):**
```csharp
public Task<Stream> RetrieveFileAsync(string fileId)
{
    // Fix: Prevent directory traversal by sanitizing the filename
    var fileName = Path.GetFileName(fileId);
    var filePath = Path.Combine(_storagePath, fileName);
    // ...
}
```

**Impact:** Eliminated code duplication, patched a critical security vulnerability, and improved system configurability.
