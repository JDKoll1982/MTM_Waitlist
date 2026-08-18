---
Author: Tech Lead
Date: 2026-08-18
Status: Accepted
Relates-To: Image Location Settings Feature (Phase 1.1)
---

# Design Decision: Image Path Resolution Cascade Architecture

## Executive Summary

This document establishes the formal architecture for resolving image paths across request types, subtypes, and work centers in MTM Waitlist. The cascade resolution pattern enables role-based customization while maintaining safe fallback behavior and backward compatibility.

---

## 1. Resolution Architecture

### 1.1 Cascade Pattern by Scope

#### Request Type Images
```
┌─────────────────────────────────────────────────────────────┐
│ User Customization Request: Resolve Image Path              │
└────────────────────────┬────────────────────────────────────┘
                         │
                    ┌────▼─────────────────────────────┐
                    │ Level 1: Database Override       │
                    │ (config_images_locations)        │
                    │ scope='request_type',            │
                    │ is_active=1                      │
                    └────┬──────────────────────────────┘
                         │ Found? → Return Path
                         │
                    ┌────▼──────────────────────────┐
                    │ Level 2: JSON Config          │
                    │ request_type.imagePath        │
                    │ (from waitlist-request-types) │
                    └────┬───────────────────────────┘
                         │ Exists? → Return Path
                         │
                    ┌────▼──────────────────────────┐
                    │ Level 3: Scope Default        │
                    │ Assets\Images\                │
                    │ default-request-type.png      │
                    └────────────────────────────────┘
                         │ Always Available
                         └──► Return Default
```

#### Subtype Images (With Parent Fallback)
```
┌─────────────────────────────────────────────────────────────┐
│ User Customization Request: Resolve Subtype Image Path      │
└────────────────────────┬────────────────────────────────────┘
                         │
                    ┌────▼─────────────────────────────┐
                    │ Level 1: Database Override       │
                    │ (config_images_locations)        │
                    │ scope='request_subtype',         │
                    │ is_active=1                      │
                    └────┬──────────────────────────────┘
                         │ Found? → Return Path
                         │
                    ┌────▼──────────────────────────┐
                    │ Level 2: Subtype JSON Config  │
                    │ subtype.imagePath             │
                    │ (from waitlist-request-types) │
                    └────┬───────────────────────────┘
                         │ Exists? → Return Path
                         │
                    ┌────▼──────────────────────────┐
                    │ Level 3: Parent Request Type  │
                    │ Resolve parent.imagePath      │
                    │ using Request Type cascade    │
                    └────┬───────────────────────────┘
                         │ Parent resolved? → Return Path
                         │
                    ┌────▼──────────────────────────┐
                    │ Level 4: Scope Default        │
                    │ Assets\Images\                │
                    │ default-request-type.png      │
                    └────────────────────────────────┘
                         │ Always Available
                         └──► Return Default
```

#### Work Center Images (No JSON Config)
```
┌─────────────────────────────────────────────────────────────┐
│ User Customization Request: Resolve Work Center Image Path  │
└────────────────────────┬────────────────────────────────────┘
                         │
                    ┌────▼─────────────────────────────┐
                    │ Level 1: Database Override       │
                    │ (config_images_locations)        │
                    │ scope='work_center',             │
                    │ is_active=1                      │
                    └────┬──────────────────────────────┘
                         │ Found? → Return Path
                         │
                    ┌────▼──────────────────────────┐
                    │ Level 2: Scope Default        │
                    │ Assets\Images\                │
                    │ default-workstation-image.png │
                    └────────────────────────────────┘
                         │ Always Available
                         └──► Return Default
```

### 1.2 Resolution Contract

**Inputs:**
- `scope`: One of `request_type`, `request_subtype`, `work_center`
- `scope_item_id`: Stable GUID (for types/subtypes) or numeric ID (for work centers)
- `fallback_scope_id` (subtypes only): GUID of parent request type

**Process:**
1. Query `config_images_locations` with `(scope, scope_item_id, is_active=1)`
2. If found and file exists on disk, return `image_path`
3. If found but file missing, log warning and continue to next level
4. For subtypes: recursively resolve parent, then continue to default
5. Return scope default asset (always available, pre-packaged)

**Guarantees:**
- Always returns a valid path (never null)
- Files missing on disk trigger fallback, not error
- All customizations are optional (JSON `imagePath` and DB overrides)
- Backward compatible: JSON entries without `imagePath` work correctly

---

## 1.3 Fallback Behavior: Missing File Handling

### What Happens When a Stored Override File No Longer Exists?

**Scenario:** Admin customized a request type with a specific image file. Later, the shared network folder was cleaned up and that file was deleted. User opens the app.

**Resolution Behavior:**
1. DB query finds override row with path `X:\MTM\Images\custom-pickup.png`
2. File system check: file not found at that path
3. **Log warning:** "Image override exists but file not found. Scope=request_type, Item=7bb056da..., Path=X:\MTM\Images\custom-pickup.png"
4. **Fallback:** Proceed to next level (JSON `imagePath` or parent or default)
5. **UI warning (in edit dialogs):** Show default asset with inline warning: "Stored image file not found. Using default. [Fix]"
6. **User action:** Either re-upload the image or click Reset to clear the override

### Fallback Logic by Scope

| Scope | Level 1 (DB) | Level 2 (JSON) | Level 3 (Parent) | Level 4 (Default) | Behavior |
|-------|--------------|----------------|------------------|-------------------|----------|
| request_type | Override file missing | JSON imagePath missing | — | Always available | Use default, log warning |
| request_type | Override file missing | JSON imagePath exists | — | Always available | Use JSON path, log warning |
| request_subtype | Override file missing | Subtype JSON missing | Parent resolved | Always available | Resolve parent, log warning |
| request_subtype | Override file missing | JSON exists but missing | — | Default | Use JSON if found, else parent, else default |
| work_center | Override file missing | — | — | Always available | Use default, log warning |

### When Fallback Fails (Error Case)

**Scenario:** Scope default asset is missing or corrupted.  
**Response:**
1. Log **error** (not warning): "Scope default asset unavailable. Scope=request_type, Expected=Assets\Images\default-request-type.png"
2. Return **empty string or placeholder path** to caller
3. UI renders a **broken image icon** (WinUI standard behavior for invalid source)
4. Administrator must restore the missing default asset or redeploy

**Prevention:** 
- Default assets are part of application deployment package
- Pre-deployment validation (Phase 4.1) confirms all assets are present
- Should never occur in production unless deployment was corrupted

### Detecting Missing Files: The File System Check

**Implementation:**
```csharp
private async Task<bool> FileExistsAsync(string imagePath)
{
    try
    {
        // Normalize UNC paths and check existence
        var info = new FileInfo(imagePath);
        return info.Exists;
    }
    catch (UnauthorizedAccessException ex)
    {
        // Network path may be unreachable (permissions or network issue)
        _logger.LogWarning("File access check failed: {Path}. Error: {Error}", imagePath, ex.Message);
        return false; // Treat as missing, will fallback
    }
    catch (PathTooLongException ex)
    {
        // Path exceeds 260 chars (MAX_PATH on Windows)
        _logger.LogError("Invalid path length: {Path}. Error: {Error}", imagePath, ex.Message);
        return false;
    }
}
```

**Trade-off:** File system check adds latency (~10-50ms per check)
- **Mitigation:** Cached results (5-min TTL) eliminate repeat checks
- Batch all file checks at once (resolve 8 types in parallel)

### Admin Notifications for Missing Files

**In Edit Dialogs (Module_Settings):**
- Display default image with **yellow warning icon** and text:
  > "Stored image file not found at: X:\MTM\Images\custom-pickup.png
  > Using default image. Choose 'Reset' or re-upload a new image."

**In Application Logs (Module_Core):**
- Severity: WARNING
- Details: Scope, item ID, stored path, resolution fallback level
- Example log entry:
  > `2026-08-18 14:32:15.456 [WARNING] Image override missing. Scope=request_type, ItemID=7bb056da-2dfd-4da5-824c-cff0973544fb, Path=X:\MTM\Images\custom-pickup.png, FallbackTo=JSON imagePath`

**No UI Toast/Alert:** 
- Missing overrides are not critical errors
- Users see correct image rendering (via fallback)
- Admin aware via inline dialog warning (not intrusive)

### Reconciliation: Periodic Cleanup of Orphaned Overrides

**Optional Future Enhancement:**
```csharp
public async Task<int> CleanupOrphanedOverridesAsync()
{
    // Query all active overrides
    var overrides = await _db.ConfigImagesLocations
        .Where(x => x.IsActive)
        .ToListAsync();
    
    int orphanedCount = 0;
    foreach (var ovr in overrides)
    {
        if (!await FileExistsAsync(ovr.ImagePath))
        {
            ovr.IsActive = false;
            orphanedCount++;
        }
    }
    
    if (orphanedCount > 0)
    {
        await _db.SaveChangesAsync();
        _logger.LogInformation("Cleaned up {Count} orphaned image overrides", orphanedCount);
    }
    
    return orphanedCount;
}
```

**Trigger:** 
- Manually via diagnostic/admin tool
- Automatically on app startup (with TTL to limit frequency)
- Phase 4.2 smoke test: Verify cleanup works correctly



---

## 2. Design Tradeoffs & Rationale

### Decision: Cascade Over Hardcoding

**Tradeoff:** Cascade resolution adds 1-2 DB queries vs. hardcoded image references  
**Rationale:**
- Enables admin-time customization without code changes or redeployment
- Supports role-based image theming for different sites/shifts
- Maintains flexibility for future image behavior changes
- Single point of customization (Settings page) instead of multiple UI locations

**Mitigations:**
- Cache resolved paths in `IImageLocationService` with TTL
- Pre-compute all 8 request type + 24 subtypes on startup (32 resolves)
- Query batching: load all overrides for a scope in single DB call

---

### Decision: JSON Config for Request Types/Subtypes, Not Work Centers

**Tradeoff:** Work centers store only DB overrides; JSON config exists only for types/subtypes  
**Rationale:**
- Request types/subtypes are **static** (in repo, updated via deployment)
- Work centers are **dynamic** (catalog in production database, live updates)
- JSON provides single-deployment defaults without DB dependency during startup
- Prevents cold-start issues: new app instances resolve defaults immediately

**Implications:**
- Request types can have both JSON defaults and DB overrides
- Work centers have only DB overrides; no JSON configuration
- Subtype inheritance resolves to parent request type, which may have JSON default

---

### Decision: Stable IDs (GUIDs) Over Display Names

**Tradeoff:** Store GUID as `scope_item_id` instead of `requestType` string  
**Rationale:**
- Display names change; stable IDs do not
- A rename ("Pickup" → "Parts Pickup") must never orphan stored image paths
- Allows backward compatibility: existing entries without `imagePath` still work
- Separates identity (ID) from presentation (name), enabling refactoring

**Implementation:**
- Each request type has unique GUID in JSON
- Each subtype has globally unique GUID (may share names across parents)
- Work centers use numeric ID from `setup_workstations_catalog.id`

---

### Decision: Soft-Delete Over Hard-Delete in Override Table

**Tradeoff:** Add `is_active` flag rather than immediately deleting rows  
**Rationale:**
- Audit trail: preserves record of all customizations (who, when, what changed)
- Recovery: admins can reactivate overrides without re-uploading images
- Compliance: satisfies audit requirements for role-based changes
- Performance: SET instead of DELETE (indexed query remains valid)

**Query Optimization:**
- Index on `(scope, is_active)` for fast active-only lookups
- Resolution logic filters `is_active=1` at query time

---

### Decision: Inline Warnings (Not Errors) for Missing Files

**Tradeoff:** Fallback gracefully vs. fail hard on missing image files  
**Rationale:**
- Share path may be temporarily unavailable (network maintenance, permissions)
- Users should see the default image, not a broken state
- Warning appears in the editing dialog so admins can notice and fix
- Prevents cascade of errors affecting entire application

**Behavior:**
- Missing override file → log warning, use Level 2 (JSON) or Level 3 (parent)
- Missing JSON file (unlikely) → log warning, skip to Level 3
- Missing default asset → log error, return empty/placeholder (should never happen)

---

## 3. Architecture & Component Interactions

### 3.1 Service Layer Contract

```csharp
/// <summary>
/// Resolves the effective image path for a given scope and item.
/// Implements the cascade resolution pattern with fallback behavior.
/// </summary>
public interface IImageLocationService
{
    /// <summary>
    /// Resolve the effective image path for a request type.
    /// Resolution: DB Override → JSON imagePath → Scope Default
    /// </summary>
    Task<string> ResolveRequestTypeImagePathAsync(
        string requestTypeId);

    /// <summary>
    /// Resolve the effective image path for a subtype.
    /// Resolution: DB Override → Subtype JSON imagePath → 
    ///             Parent Request Type → Scope Default
    /// </summary>
    Task<string> ResolveRequestSubtypeImagePathAsync(
        string subtypeId, 
        string parentRequestTypeId);

    /// <summary>
    /// Resolve the effective image path for a work center.
    /// Resolution: DB Override → Scope Default (no JSON config)
    /// </summary>
    Task<string> ResolveWorkCenterImagePathAsync(
        long workCenterId);

    /// <summary>
    /// Save an image path override to the database.
    /// Raises change notifications to refresh open views.
    /// </summary>
    Task SaveOverrideAsync(
        string scope, 
        string scopeItemId, 
        string imagePath,
        long? updatedByUserId);
}
```

### 3.2 Data Flow

```
┌──────────────────────────────────────────────────────────┐
│ Module_Settings / SettingsPage (UI)                      │
│  - Displays expander with three cards                    │
│  - Binds to SettingsViewModel.ImageLocationsViewModel   │
└────────────┬─────────────────────────────────────────────┘
             │
        ┌────▼──────────────────────────────────────────┐
        │ SettingsViewModel.ImageLocationsViewModel      │
        │  - Loads overrides from DB                    │
        │  - Tracks edits (pending uncommitted changes) │
        │  - Raises INotifyPropertyChanged events       │
        └────┬───────────────────────────────────────────┘
             │
        ┌────▼────────────────────────────────────────────┐
        │ IImageLocationService (DI-registered)           │
        │  - Implements resolution cascade               │
        │  - Queries DB (config_images_locations)        │
        │  - Loads JSON (waitlist-request-types.json)    │
        │  - Raises change notifications (MessengerBus)  │
        └────┬───────────────────────────────────────────┘
             │
        ┌────▼───────────────────────────────────────────┐
        │ MySQL Database (config_images_locations)       │
        │  - Stores overrides by scope+item+active      │
        │  - Audit columns (created/updated by/when)     │
        └────────────────────────────────────────────────┘
```

### 3.3 Notification Flow (After Edit)

```
UI Dialog (Save)
    ↓
IImageLocationService.SaveOverrideAsync()
    ↓
INSERT/UPDATE config_images_locations
    ↓
MessengerBus.Send<ImagePathChangedMessage>()
    ↓
[Subscribed View Models Refresh]
    ├─ Module_Waitlist.WaitlistViewModel (recalculate thumbnails)
    ├─ Module_Waitlist.DetailViewModel (recalculate hero)
    └─ Module_Settings.SettingsViewModel (refresh dialog)
```

---

## 4. Compliance & Standards Mapping

### 4.1 Database Naming Conformance

| Element | Standard | Implementation | Notes |
|---------|----------|-----------------|-------|
| Table name | `config_*` | `config_images_locations` | ✓ Conforms |
| PK column | `id` | BIGINT AUTO_INCREMENT | ✓ Conforms |
| UUID column | `public_id` | CHAR(36) | ✓ Conforms |
| FK naming | `fk_<from>_<to>_<col>` | `fk_config_images_locations_*_user_id` | ✓ Conforms |
| Unique naming | `uq_<table>_<purpose>` | `uq_config_images_locations_scope_item` | ✓ Conforms |
| Index naming | `idx_<table>_<purpose>` | `idx_config_images_locations_scope_active` | ✓ Conforms |
| Audit columns | `*_utc`, `*_by_user_id` | `created_utc`, `updated_by_user_id` | ✓ Conforms |

**Verification Task:** [Phase 3.2 Compliance Task] Verify table and field naming against `.github/instructions/database-schema-rules.instructions.md`

### 4.2 Architectural Pattern Alignment

| Pattern | Repository Usage | Image Feature Usage | Alignment |
|---------|------------------|---------------------|-----------|
| Cascade resolution | Config settings override → DB → default | Override → JSON → Parent → Default | ✓ Consistent |
| Soft-delete with `is_active` | `config_settings_*` tables | `config_images_locations.is_active` | ✓ Consistent |
| Role-based authorization | Auth middleware + role claims | Expander x:Load binding to role check | ✓ Consistent |
| Notification pattern | MessengerBus for cross-module comms | Change notifications after Save | ✓ Consistent |
| JSON config defaults | `waitlist-request-types.json` | Extended with `id`, `imagePath` | ✓ Backward compatible |

### 4.3 Out-of-Scope Boundaries (Per Section 7 of Spec)

**Deliberately Excluded (Do Not Implement):**
- ✗ Image generation or editing tools
- ✗ Server-side image upload/hosting
- ✗ UI styling changes beyond the Settings page
- ✗ Automatic image resizing or format conversion
- ✗ Bulk image upload workflows

**Reason:** Keep feature scope focused on **override management**, not asset creation. Image sourcing remains a manual admin responsibility.

---

## 5. Maintenance & Future Guidelines

### 5.1 Adding a New Customizable Image Scope

If future work requires a fourth scope (e.g., "work_order_templates"), follow this checklist:

1. **Database:**
   - Add rows to `config_images_locations` with new `scope` value
   - Update unique constraint documentation

2. **JSON Config (if applicable):**
   - Add `id` and `imagePath` fields to the new JSON structure
   - Ensure IDs are globally unique across all entities

3. **Service Layer:**
   - Add new `ResolveXxxImagePathAsync()` method to `IImageLocationService`
   - Implement cascade logic (skip JSON step if not applicable)
   - Cache resolved paths with TTL

4. **UI:**
   - Add new `SettingsCard` to the expander
   - Create new `ContentDialog` for the new scope
   - Bind edit/save/reset logic to the service

5. **Testing:**
   - Unit tests for new resolution cascade
   - Integration tests for DB override behavior
   - Edge case: missing file warnings for new scope

### 5.2 Caching Strategy

**Recommended Implementation:**
```csharp
private readonly Dictionary<string, (string path, DateTime expiry)> _pathCache;
private readonly TimeSpan _cacheTtl = TimeSpan.FromMinutes(5);

public async Task<string> ResolveRequestTypeImagePathAsync(string requestTypeId)
{
    var cacheKey = $"request_type:{requestTypeId}";
    if (_pathCache.TryGetValue(cacheKey, out var cached) && cached.expiry > DateTime.UtcNow)
    {
        return cached.path;
    }
    
    var resolvedPath = await _resolveAsync(requestTypeId);
    _pathCache[cacheKey] = (resolvedPath, DateTime.UtcNow.Add(_cacheTtl));
    return resolvedPath;
}

// On change notification: _pathCache.Clear();
```

**Trade-off:** 5-minute TTL vs. immediate consistency
- Mitigates DB load from rapid queries
- Acceptable for admin workflows (manual changes are infrequent)
- MessengerBus notifies immediately, so UI sees change instantly

### 5.3 Migration from Hardcoded Paths

**If moving existing hardcoded image references to this system:**

1. Map existing hardcoded paths to override entries
2. Insert rows into `config_images_locations` with hardcoded paths
3. Remove hardcoded image bindings from XAML/ViewModels
4. Update UI to use resolved paths from `IImageLocationService`
5. No data loss: existing images continue to render via cascade

---

## 6. Risk Mitigation

### 6.1 Single Point of Failure: Shared Network Folder

**Risk:** Share path unreachable or permissions changed  
**Mitigation:**
- Resolution falls back gracefully to default asset
- Inline warning alerts admins in the Settings dialog
- Pre-deployment validation gate (Phase 4.1)
- Permissions logging on write failure

### 6.2 Performance: Resolution Query Count

**Risk:** One query per image on each render could cause N+1 problem  
**Mitigation:**
- Batch resolution at view-model level (resolve all items at once)
- 5-minute cache with immediate MessengerBus invalidation
- Index on `(scope, is_active)` for fast queries
- Startup pre-warm: resolve all 32 items on app initialization

### 6.3 Backward Compatibility: Existing JSON Without imagePath

**Risk:** Entries without `imagePath` fail to resolve  
**Mitigation:**
- Cascade treats missing `imagePath` as NULL (skips to next level)
- No schema migration required; optional fields work immediately
- Default asset always available as final fallback
- Tested during Phase 3.1 validation

---

## 7. Rollback Strategy

If the feature must be rolled back (Phase 4 gate failure):

1. **Database:** Rollback migration via `Database/Tables/17_config_images_locations/rollback.sql`
2. **JSON:** Revert `Assets/Config/waitlist-request-types.json` to version without `id`/`imagePath`
3. **Code:** Remove `IImageLocationService` DI registration and all usages
4. **UI:** Remove the new expander from `SettingsPage.xaml`
5. **Result:** App reverts to hardcoded image bindings (all images available, no customization)

**Testing:** Rollback migration runs as part of Phase 4 deployment validation.

---

## 8. Approval & Next Steps

**Status:** ✓ APPROVED for implementation  
**Phase 1.1 Gate:** Architecture contract complete and documented  
**Next:** Full Stack Engineers proceed to implement service layer and data model (Phase 1.1/1.2)

**Compliance Enforcement Gate (Phase 3.2):**
- Tech Lead verifies naming standards (Task: line 151)
- Tech Lead confirms no scope expansion (Task: line 153)
- Tech Lead validates migration artifacts (Task: line 152)

---

**Document Version:** 1.0  
**Last Updated:** 2026-08-18  
**Next Review:** Post-Phase 2 (before Phase 3 testing)
