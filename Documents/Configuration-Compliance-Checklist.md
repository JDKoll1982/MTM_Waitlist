---
Title: Image Storage Configuration Compliance Checklist
Author: Tech Lead
Date: 2026-08-18
Relates-To: Configuration Resolution Strategy
---

# Compliance Checklist: Image Storage Configuration

## Standards Mapped

This checklist ensures the image storage configuration implementation adheres to all repository standards and design patterns.

---

## ✅ Naming Conventions (csharp-xaml-naming-rules.instructions.md)

### C# Naming Rules

- [x] **Classes:** PascalCase
  - ✅ `ImageStorageOptions` — configuration model
  - ✅ `ImageStorageConfigurationResolver` — service implementation
  - ✅ `ConfigSettingValue` — domain model
  - ✅ `ConfigSettingKeys` — static constant class

- [x] **Methods:** PascalCase, Async methods end in `Async`
  - ✅ `GetSharedFolderPathAsync()` — async method with Async suffix
  - ✅ `GetEffectiveConfigurationAsync()` — async method
  - ✅ `InvalidateCache()` — sync method (not async)
  - ✅ `IsExtensionAllowed()` — sync method

- [x] **Properties:** PascalCase, Init-only where appropriate
  - ✅ `SharedFolderPath` — public property
  - ✅ `MaxFileSizeBytes` — public property
  - ✅ `SectionName` — public constant (PascalCase for const)

- [x] **Fields:** camelCase with `_` prefix for private fields
  - ✅ `_logger` — private logger field
  - ✅ `_appsettingsOptions` — private dependency field
  - ✅ `_configService` — private dependency field
  - ✅ `_cache` — private cache field

- [x] **Constants:** UPPER_SNAKE_CASE or PascalCase for well-known constants
  - ✅ `ConfigSettingKeys.ImageStorageSharedFolderPath` — well-known key (constant string)
  - ✅ `CacheTtl` — well-known constant

### XML Documentation

- [x] All public types have `/// <summary>` documentation
  - ✅ `ImageStorageOptions` class
  - ✅ `IImageStorageConfigurationResolver` interface
  - ✅ `ImageStorageConfigurationResolver` class
  - ✅ `ConfigSettingValue` class

- [x] All public members have parameter and return value documentation
  - ✅ Constructor parameters documented
  - ✅ Method return values documented
  - ✅ Method exceptions documented

---

## ✅ Database Schema Rules (database-schema-rules.instructions.md)

### Table & Column Naming

- [x] **Table Names:** snake_case
  - ✅ `config_settings_values` — standard repository table
  - ✅ `config_images_locations` — new table (already created in Phase 1.1)

- [x] **Column Names:** snake_case
  - ✅ `setting_key` — lookup key
  - ✅ `scope_type` — scope type indicator
  - ✅ `scope_key` — scope key value
  - ✅ `setting_value` — configuration value
  - ✅ `value_type` — type indicator
  - ✅ `updated_utc` — timestamp
  - ✅ `updated_by_user_id` — audit reference

- [x] **Primary Keys:** Use `id` BIGINT AUTO_INCREMENT
  - ✅ `id` — primary key (existing table)

- [x] **Foreign Keys:** Follow naming convention `fk_<child_table>_<parent_table>_<column>`
  - ✅ `fk_values_users_updated_by_user_id` — (existing, follows pattern)

### Constraints & Indexes

- [x] **Unique Constraint:** Composite unique on (setting_key, scope_key)
  - ✅ `uq_config_settings_values_setting_scope` — prevents duplicate settings

- [x] **Indexes:** On foreign key columns
  - ✅ `idx_config_settings_values_updated_by_user_id` — (existing)
  - ✅ `idx_config_settings_values_workstation_id` — (existing)
  - ✅ `idx_config_settings_values_user_id` — (existing)

### Data Integrity

- [x] **NULL Safety:** Appropriate nullable columns
  - ✅ `setting_value` — nullable (for non-text types)
  - ✅ `setting_value_int` — nullable (for non-int types)
  - ✅ `updated_by_user_id` — nullable (for seeded values)

- [x] **Data Types:** Appropriate sizes
  - ✅ `setting_key` VARCHAR(190) — sufficient for keys like "image_storage.shared_folder_path"
  - ✅ `scope_key` VARCHAR(255) — sufficient for scope identifiers
  - ✅ `setting_value` TEXT — sufficient for UNC paths
  - ✅ `setting_value_int` BIGINT — sufficient for file sizes (bytes)

---

## ✅ Configuration Loading Pattern (appsettings.json & Options)

### Configuration Section

- [x] **Section Name:** Defined as constant
  - ✅ `ImageStorageOptions.SectionName = "ImageStorage"`

- [x] **JSON Structure:** Matches C# model properties
  - ✅ JSON section name matches bound class name
  - ✅ Property names match JSON keys (PascalCase)
  - ✅ All properties optional (no required fields)

- [x] **Binding:** Uses `IOptions<T>` pattern
  - ✅ `IOptions<ImageStorageOptions>` injected in resolver
  - ✅ Bound via `configuration.GetSection(ImageStorageOptions.SectionName)`
  - ✅ Follows existing repo pattern (e.g., `LocalSettingsOptions`)

### Validation

- [x] **Validation Method:** Exists and called before use
  - ✅ `ImageStorageOptions.Validate()` method
  - ✅ Checks: paths not empty, file sizes > 0, extensions valid
  - ✅ Throws `InvalidOperationException` with detailed errors
  - ✅ Called in `ImageStorageConfigurationResolver.GetEffectiveConfigurationAsync()`

---

## ✅ Dependency Injection Pattern (App.xaml.cs)

### Service Registration

- [ ] **Pending (Phase 1.2):** Services registered in DI container
  - ⏳ `IImageStorageConfigurationResolver` → `ImageStorageConfigurationResolver`
  - ⏳ `IConfigSettingsValueService` → (implementation TBD)
  - ⏳ `ImageStorageOptions` bound from config

- [ ] **Pending:** Constructor injection verified
  - ⏳ All dependencies injected (no `new` in service)
  - ⏳ Logging injected via `ILogger<T>`

---

## ✅ Logging Strategy (Microsoft.Extensions.Logging)

### Logger Usage

- [x] **Logger Injected:** Via constructor in all services
  - ✅ `ILogger<ImageStorageConfigurationResolver>` parameter
  - ✅ `ILogger<RequestTypeDisplayLabelService>` parameter
  - ✅ `ILogger<RequestSubtypeDisplayLabelService>` parameter

- [x] **Log Levels:** Appropriate levels used
  - ✅ `LogDebug()` — cache hits, detailed flow
  - ✅ `LogInformation()` — normal cascade resolution
  - ✅ `LogWarning()` — fallback used, display name changed
  - ✅ `LogError()` — configuration errors

- [x] **Structured Logging:** Parameters use `{Placeholder}` syntax
  - ✅ Examples: `"Using database override for shared folder path: {Path}"`
  - ✅ Allows Serilog/other providers to extract structured data

---

## ✅ Error Handling & Exceptions

### Exception Strategy

- [x] **Null Reference:** ArgumentNullException for constructor parameters
  - ✅ `throw new ArgumentNullException(nameof(logger))`
  - ✅ `throw new ArgumentNullException(nameof(appsettingsOptions))`

- [x] **Invalid State:** InvalidOperationException for configuration errors
  - ✅ "Configuration validation failed: ..."
  - ✅ "Failed to resolve shared folder path configuration"

- [x] **Not Found:** ArgumentException for missing IDs
  - ✅ "Request type ID not found in registry"
  - ✅ Includes helpful message about initialization

- [x] **Logging on Exception:** Error logged with context
  - ✅ All exceptions logged at ERROR level
  - ✅ Original exception included for stack trace

---

## ✅ Cache & Performance Pattern

### Caching Strategy

- [x] **Thread-Safe Collection:** ConcurrentDictionary used
  - ✅ `ConcurrentDictionary<string, CachedValue<object>>`
  - ✅ No locks needed; safe for concurrent access

- [x] **Cache Invalidation:** Method provided for explicit clearing
  - ✅ `InvalidateCache()` called when settings updated
  - ✅ TTL-based expiry (5 minutes)
  - ✅ Logged when invalidated

- [x] **Performance Impact:** Minimal
  - ✅ First request: database query (~1-5ms)
  - ✅ Cached requests: <1ms
  - ✅ Cache hit rate: high (config rarely changes)

---

## ✅ Test Naming & Coverage (Repository Convention)

### Test Class Naming

- [x] **Convention:** `ClassName + Tests` (not `Test` prefix)
  - ✅ `ImageStorageConfigurationResolverTests`
  - ✅ `RequestTypeDisplayLabelServiceTests`
  - ✅ `ImageStorageOptionsTests`

- [x] **Test Method Naming:** `When<Condition>_Then<Expected>` pattern
  - ✅ `WhenImageOver10MB_ThenRejectAndLogWarning`
  - ✅ `WhenDatabaseOverrideExists_ThenUseDbValue`
  - ✅ `WhenAllTiersFail_ThenThrowInvalidOperation`

- [x] **Attributes:** [Theory]/[Fact] or [TestMethod] per repo style
  - ✅ Uses xUnit `[Fact]` and `[Theory]` attributes (consistent with repo)

---

## ✅ Scope Compliance (Section 7: Out-of-Scope)

### Feature Boundaries

- [x] **In Scope:** Configuration override pattern
  - ✅ Database-backed setting storage
  - ✅ Cascade resolution (database → appsettings → default)
  - ✅ Caching for performance

- [x] **Out of Scope:** Encryption & security
  - ✅ Not implementing: Encrypting database settings
  - ✅ Reason: Covered by database encryption at rest
  - ✅ Responsibility: DevOps/infrastructure team

- [x] **Out of Scope:** Real-time updates
  - ✅ Not implementing: SignalR cache invalidation
  - ✅ Reason: 5-minute TTL acceptable
  - ✅ Alternative: Manual `InvalidateCache()` call or wait for TTL

- [x] **Out of Scope:** UI administration
  - ✅ Not implementing: Settings management UI
  - ✅ Reason: Deferred to Phase 2 admin panel
  - ✅ Current: Direct database updates supported

- [x] **Out of Scope:** Image generation/editing
  - ✅ Not implementing: Image manipulation
  - ✅ Scope: File copy to network share only
  - ✅ Validation: Type, size, aspect ratio checks only

---

## ✅ Integration with Existing Patterns

### Consistency with Repo

- [x] **MVVM Patterns:** Services integrate with existing ViewModels
  - ✅ `IImageStorageConfigurationResolver` will be injected into settings ViewModels
  - ✅ Same pattern as existing services (ILocalSettingsService, etc.)

- [x] **Repository Layer:** Data access through interfaces
  - ✅ `IConfigSettingsValueService` interface defined
  - ✅ Implementation to be provided by data access layer
  - ✅ Consistent with DDD/repository pattern

- [x] **Module Structure:** Services in `Module_Settings`
  - ✅ `Module_Settings/Services/` location
  - ✅ `Module_Settings/Models/` location
  - ✅ Respects module boundaries

---

## ✅ Documentation Completeness

### Design Decision Record

- [x] **Decision & Rationale:** Documented
  - ✅ Three-tier cascade chosen with rationale
  - ✅ Alternatives considered with tradeoffs

- [x] **Architecture Diagram:** Provided
  - ✅ ASCII flowchart of resolution order
  - ✅ Cache flow documented

- [x] **Compliance Mapping:** This checklist
  - ✅ Each standard mapped to implementation

- [x] **Maintenance Guidelines:** For devs, ops, future enhancements
  - ✅ How to add new settings
  - ✅ How to debug configuration
  - ✅ How to override at runtime
  - ✅ How to rollback changes

- [x] **Testing Strategy:** Unit and integration test outline
  - ✅ Test cases for each tier
  - ✅ Cache behavior tests
  - ✅ Error path tests

---

## Summary

| Category | Status | Notes |
|----------|--------|-------|
| **Naming Conventions** | ✅ COMPLIANT | All classes, methods, properties follow rules |
| **Database Schema** | ✅ COMPLIANT | Uses existing table; naming matches patterns |
| **Configuration Pattern** | ✅ COMPLIANT | Follows IOptions<T> pattern from repo |
| **DI Registration** | ⏳ PENDING | Will be done in Phase 1.2 |
| **Logging** | ✅ COMPLIANT | Microsoft.Extensions.Logging throughout |
| **Error Handling** | ✅ COMPLIANT | Appropriate exception types and logging |
| **Caching** | ✅ COMPLIANT | Thread-safe with TTL invalidation |
| **Test Naming** | ✅ COMPLIANT | Follows repo conventions |
| **Scope** | ✅ COMPLIANT | Respects out-of-scope boundaries |
| **Integration** | ✅ COMPLIANT | Consistent with existing patterns |

---

**Checklist Version:** 1.0  
**Last Updated:** 2026-08-18  
**Next Review:** After DI registration in Phase 1.2  
**Owner:** Tech Lead (Image Location Settings Feature)
