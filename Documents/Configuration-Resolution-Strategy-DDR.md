---
Title: Image Storage Configuration Resolution Strategy
Author: Tech Lead
Date: 2026-08-18
Status: Design Decision Record (DDR)
Relates-To: Image Location Settings Feature (Phase 1.2)
---

# Configuration Resolution Strategy: Image Storage Path Override

## Executive Summary

This design decision document establishes the configuration cascade strategy for resolving the shared network folder path used by the image location feature. The cascade prioritizes **admin runtime flexibility** while maintaining **deployment-time defaults** and **hard-coded fallbacks** for operational resilience.

**Resolution Order (Top → Bottom, First Match Wins):**
1. **Database Override** — `config_settings_values` (admin-configurable at runtime)
2. **appsettings.json Default** — Deployment-time configuration (deployment-specific)
3. **Hard-Coded Fallback** — In-code constant (emergency fallback)

---

## Decision Analysis

### Chosen Strategy: Cascade Override Pattern

**Decision:** Implement a three-tier configuration cascade with database override as the primary source, appsettings.json as the secondary source, and hard-coded defaults as the tertiary fallback.

**Rationale:**

| Tier | Source | Use Case | Owner | Runtime Mutable |
|------|--------|----------|-------|-----------------|
| Primary | Database (`config_settings_values`) | Admin changes without redeployment | Admin/Platform Team | ✅ Yes |
| Secondary | `appsettings.json` | Environment-specific defaults | DevOps/Deployment | ❌ No (requires redeploy) |
| Tertiary | Hard-coded in code | Emergency fallback if config missing | Engineering | ❌ No |

### Why Not Other Options?

#### ❌ Option 1: Environment Variables Only
- **Rejected:** Cannot change the path at runtime without restarting the app
- **Impact:** Admin workflow would require app restart, violating UX requirements
- **Tradeoff Loss:** Eliminates "change path in-app" capability

#### ❌ Option 2: Database Only (No appsettings)
- **Rejected:** Requires database connection on cold start; no fallback if DB unavailable
- **Impact:** Application startup fails if database is unreachable
- **Tradeoff Loss:** Violates operational resilience principle

#### ✅ Option 3: Cascade (Chosen)
- **Accepted:** All paths now supported with graceful degradation
- **Impact:** Admin has runtime control; ops has deployment-time defaults; code has emergency fallback
- **Tradeoff Cost:** Adds complexity in configuration resolution logic (mitigated by caching)

---

## Implementation Details

### Configuration Cascade Diagram

```
┌─────────────────────────────────────────────────────────────────────────┐
│ CONFIGURATION RESOLUTION CASCADE                                         │
│ Image Storage Shared Folder Path                                         │
└─────────────────────────────────────────────────────────────────────────┘

  ┌──────────────────────────────────────────────────────────────┐
  │  REQUEST: Get Shared Folder Path                             │
  │  (At startup or when config is accessed)                     │
  └───────────────────────┬──────────────────────────────────────┘
                          │
                          ▼
  ┌─────────────────────────────────────────────────────────────┐
  │  CACHE CHECK                                                │
  │  Is value cached and valid (TTL 5 min)?                    │
  └───────────┬─────────────────────────────────────┬───────────┘
              │ YES                                 │ NO
              ▼                                     ▼
        ┌──────────────┐          ┌──────────────────────────────┐
        │ Return cached│          │ Query Database               │
        │   value      │          │ config_settings_values       │
        │ Source: cache│          │ WHERE setting_key = '...'    │
        └──────────────┘          └────┬──────────────────┬──────┘
                                       │ Found            │ Not Found
                                       ▼                  ▼
                        ┌──────────────────────────┐   ┌─────────────────┐
                        │ Validate & Cache value   │   │ Read appsettings│
                        │ Return DB value          │   │ ImageStorage    │
                        │ Source: database         │   │ .SharedFolderPath
                        └──────────────────────────┘   └────┬───────┬────┘
                                                             │ Valid │ Null
                                                             ▼       ▼
                                                   ┌─────────────┐  ┌─────────────┐
                                                   │ Cache value │  │ Use fallback│
                                                   │ Return      │  │ const DEFAULT
                                                   │ Source:     │  │ Source:     │
                                                   │ appsettings │  │ hard-coded  │
                                                   └─────────────┘  └─────────────┘
                                                             │            │
                                                             └──────┬─────┘
                                                                    ▼
                                                        ┌──────────────────────┐
                                                        │ Validate Path        │
                                                        │ Log Source & Value   │
                                                        │ Return Final Value   │
                                                        └──────────────────────┘
```

### Resolution Order: Detailed Explanation

#### Tier 1: Database Override (Primary)

**Table:** `config_settings_values`

**Key Row:**
```sql
setting_key = 'image_storage.shared_folder_path'
scope_type = 'all_users'
scope_key = 'all_users'
value_type = 'text'
setting_value = <admin-configured UNC path or NULL>
```

**When Used:**
- ✅ If row exists AND `setting_value` is not NULL and not empty
- ❌ If row doesn't exist OR `setting_value` IS NULL or empty string

**Admin Experience:**
- Change via settings UI or direct database update
- Takes effect immediately (after cache invalidation)
- No app restart required
- Logs: "Using database override for shared folder path: {Path}"

**Fallback Trigger:**
If `setting_value` is NULL/empty, cascade continues to Tier 2

---

#### Tier 2: appsettings.json Default (Secondary)

**File:** `appsettings.json`

**Configuration Section:**
```json
{
  "ImageStorage": {
    "SharedFolderPath": "X:\\Software Development\\Live Applications\\MTM_Waitlist\\Images"
  }
}
```

**When Used:**
- ✅ If database value is NULL or not found AND appsettings value is valid
- ❌ If appsettings value is missing or malformed

**Deployment Experience:**
- Configured by DevOps before deployment
- Allows different paths per environment (dev, test, prod)
- Requires app restart to take effect
- Logs: "Using appsettings.json default for shared folder path: {Path}"

**Fallback Trigger:**
If appsettings value is invalid, cascade continues to Tier 3

---

#### Tier 3: Hard-Coded Fallback (Tertiary)

**Code Location:** `ImageStorageDefaults` or `ImageStorageOptions`

**Hard-Coded Default:**
```csharp
public const string DefaultSharedFolderPath = 
    "X:\\Software Development\\Live Applications\\MTM_Waitlist\\Images";
```

**When Used:**
- ✅ Only when database and appsettings both fail to provide valid value
- This should be treated as an emergency fallback

**Operational Status:**
- Logs: "Using hard-coded default for shared folder path: {Path}"
- Alerts: Log WARN level if fallback is used (indicates config problem)

---

## Cache Strategy

**Cache Mechanism:**
- In-memory `ConcurrentDictionary<string, CachedValue<T>>`
- Time-to-Live (TTL): 5 minutes
- Thread-safe for concurrent access

**Cache Invalidation:**
- Called explicitly when admin updates a setting
- Invalidates all cached configuration values
- Logged as "Invalidating image storage configuration cache"

**Performance Impact:**
- First request per configuration item: ~1-5ms (database query)
- Cached requests: <1ms
- Cache hit rate: High (config rarely changes at runtime)

---

## Error Handling & Logging

### Logging Levels

| Level | Condition | Example |
|-------|-----------|---------|
| INFO | Normal cascade resolution | "Using database override for shared folder path: X:\..." |
| INFO | Falling back to next tier | "Using appsettings.json default for shared folder path: ..." |
| WARN | Emergency fallback used | "Using hard-coded default for shared folder path: ..." |
| ERROR | Configuration invalid | "Failed to resolve shared folder path: path is null or empty" |
| DEBUG | Cache hit | "Using cached shared folder path: database" |

### Exception Handling

**Caught & Logged:**
- Database query failures (connection lost)
- Malformed JSON in appsettings
- Invalid path format (e.g., UNC syntax error)

**Propagated:**
- `InvalidOperationException` when all tiers fail
- Logged with full context including which tiers were attempted

---

## Configuration Override Scope

### Global (Recommended)
- Applied to all users and workstations
- `scope_type = 'all_users'`, `scope_key = 'all_users'`
- Single entry in database

### Future: Per-Workstation or Per-User Overrides
- Not currently implemented
- Schema supports: `scope_type = 'workstation'` or `'user'`
- Enables advanced use cases (e.g., "Press 1 uses Network Share A, Press 2 uses Network Share B")

---

## Compliance Mapping

### Standards Adherence

| Standard | Document | Requirement | Status | Evidence |
|----------|----------|-------------|--------|----------|
| Naming Conventions | `.github/instructions/csharp-xaml-naming-rules.instructions.md` | PascalCase for class/method names | ✅ COMPLIANT | `ImageStorageOptions`, `GetSharedFolderPathAsync()` |
| Database Rules | `.github/instructions/database-schema-rules.instructions.md` | snake_case for table/column names | ✅ COMPLIANT | `config_settings_values`, `setting_key`, `setting_value` |
| DI Registration | `App.xaml.cs` pattern | Services registered in DI container | ⏳ PENDING (Phase 1.2) | `IImageStorageConfigurationResolver` → `ImageStorageConfigurationResolver` |
| Configuration Loading | `StartupOptions` pattern | Options bound from config section | ✅ COMPLIANT | `ImageStorageOptions.SectionName = "ImageStorage"` |
| Logging | Existing `ILogger<T>` usage | Use Microsoft.Extensions.Logging | ✅ COMPLIANT | Constructor injection + `_logger.LogInformation()` |

### Out-of-Scope Boundaries

| Boundary | In Scope? | Reason |
|----------|-----------|--------|
| Encrypting database settings | ❌ NO | Covered by database encryption at rest (ops responsibility) |
| Replicating config across multiple servers | ❌ NO | Single database is source of truth |
| Real-time cache invalidation via SignalR | ❌ NO | 5-minute TTL acceptable for this use case |
| UI for editing settings | ❌ NO | Direct database updates or future admin panel (Phase 2) |

---

## Maintenance Guidelines

### For Developers

**When adding a new image storage configuration option:**

1. Add constant to `ConfigSettingKeys` class
2. Add property to `ImageStorageOptions` model
3. Add resolution method to `IImageStorageConfigurationResolver`
4. Add database seed entry to `06_config_image_storage_settings.sql`
5. Document the cascade in this file
6. Update compliance checklist if needed

**When debugging configuration:**

1. Enable DEBUG logging to see cache hits/misses
2. Check database: `SELECT * FROM config_settings_values WHERE setting_key LIKE 'image_storage%'`
3. Check appsettings.json: Verify `ImageStorage` section is valid JSON
4. Call `resolver.InvalidateCache()` to force re-resolution
5. Review logs for tier resolution (database/appsettings/fallback)

### For Operations

**When overriding configuration at runtime:**

1. Connect to MySQL: `mtm_waitlist` database
2. Check current value: `SELECT setting_value FROM config_settings_values WHERE setting_key = 'image_storage.shared_folder_path'`
3. Update value: `UPDATE config_settings_values SET setting_value = 'X:\...' WHERE setting_key = 'image_storage.shared_folder_path'`
4. Notify application to invalidate cache (API call or manual cache clear)
5. Verify: Query logs for "Using database override" message
6. Test: Verify image operations use new path

**Rollback Procedure:**

1. Set database value to NULL: `UPDATE config_settings_values SET setting_value = NULL WHERE setting_key = 'image_storage.shared_folder_path'`
2. Invalidate cache
3. Application falls back to appsettings.json value automatically

### For Future Enhancements

**Possible Extensions:**

- Per-workstation overrides (different paths for different facilities)
- Per-user overrides (users with elevated permissions get different folder)
- Dynamic validation (check path accessibility before accepting override)
- Audit trail (log all configuration changes to separate audit table)
- UI admin panel for managing settings (Phase 2)

---

## Testing Strategy

### Unit Tests (to validate cascade logic)

```csharp
[Test]
public async Task GetSharedFolderPathAsync_WithDatabaseOverride_ReturnsDbValue()
{
    // Arrange: Mock database service to return override
    // Act: Call resolver
    // Assert: Returns database value with source="database"
}

[Test]
public async Task GetSharedFolderPathAsync_WithoutDbOverride_ReturnsAppsettingsValue()
{
    // Arrange: Mock database service to return null
    // Act: Call resolver
    // Assert: Returns appsettings value with source="appsettings"
}

[Test]
public async Task GetSharedFolderPathAsync_WithInvalidAppsettings_ReturnsFallback()
{
    // Arrange: Mock appsettings and database to fail
    // Act: Call resolver
    // Assert: Returns hard-coded default with source="default"
}

[Test]
public void InvalidateCache_ClearsAllCachedValues()
{
    // Arrange: Populate cache
    // Act: Call InvalidateCache()
    // Assert: Cache is empty
}
```

### Integration Tests (to validate end-to-end resolution)

```csharp
[Test]
public async Task EffectiveConfiguration_WithAllTiersAvailable_UsesDatabase()
{
    // Arrange: Database has override, appsettings has default
    // Act: Get effective configuration
    // Assert: Database value is used, logged as "database" source
}

[Test]
public async Task EffectiveConfiguration_CachesAndReturnsQuickly()
{
    // Arrange: First call triggers database query
    // Act: Second call within 5 minutes
    // Assert: Second call uses cache (much faster)
}
```

---

## Decision Tradeoffs Summary

| Aspect | Benefit | Cost | Mitigation |
|--------|---------|------|-----------|
| **Database Dependency** | Runtime flexibility | Database query on each resolution | 5-minute cache, TTL-based invalidation |
| **Three Tiers** | Resilience & flexibility | Increased complexity | Clear cascade order, comprehensive logging |
| **Cache Layer** | Performance (avoid repeated queries) | Stale data for 5 minutes | Admin can invalidate cache explicitly |
| **Hard-Coded Fallback** | Emergency resilience | Bypass of intentional overrides | Only used if config fails; logs WARN |

---

## Conclusion

This cascade strategy balances **operational flexibility** (admins can change paths at runtime without app restart), **deployment simplicity** (different paths for different environments), and **resilience** (falls back gracefully if config unavailable).

The decision prioritizes **admin control** as the primary use case while maintaining **ops control** (deployment-time) and **engineering safety** (hard-coded defaults) as fallbacks.

---

**Document Version:** 1.0  
**Last Updated:** 2026-08-18  
**Next Review:** After Phase 1.2 completion  
**Owner:** Tech Lead (Image Location Settings Feature)
