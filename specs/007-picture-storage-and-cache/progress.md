# Progress: One Picture Policy, Configurable Picture Storage, and a Local Picture Cache

**Feature Branch**: `007-picture-storage-and-cache`
**Recorded**: 2026-09-22
**Status**: all requirements delivered except FR-021 (the storage-path screen). Verification runs are listed
below with the outcome each produced.

---

## 1. What shipped

### 1.1 One picture means "no picture", everywhere (FR-001 to FR-005)

| Delivered | Evidence |
|---|---|
| The single rule deciding whether a file may be drawn, and the one picture drawn when it may not | `MTM_Waitlist.Shared/Helpers/ImagePicturePolicy.cs` |
| The resolver every surface uses, which falls back to the no-image picture and to the application's packaged resources when the file is absent | `MTM_Waitlist.Shared/Helpers/PictureSource.cs` |
| All three picture converters routed through that one rule | `MTM_Waitlist.Settings/Converters/ResolvedImagePathToSourceConverter.cs`, `MTM_Waitlist.Setup/Converters/SetupImagePathToImageSourceConverter.cs`, `MTM_Waitlist.Waitlist.View/Converters/StringToImageSourceConverter.cs` |
| Built-in item artwork removed as a stand-in for an item's picture | `MTM_Waitlist.Waitlist.View/Models/SampleOrder.cs` (`ImagePath` retired; `EffectiveImagePath` is the picture or the no-image picture) |
| Every scope's placeholder unified on the one no-image picture | `MTM_Waitlist.Settings/Models/ImageLocationDefaults.cs`, `WorkCenterSelectionItem.cs`, `SetupModels.cs`, `SetupWorkCenterViewModel.cs`, `NewRequestWorkCenterViewModel.cs`, `ImageValidation.cs` |
| The first-visit bug fixed: a freshly started machine now resolves configured pictures on the first screen | `MTM_Waitlist.Settings/Services/ImageLocationService.cs` (`EnsureInitializedAsync`), same on `IWorkCenterImageService`, awaited at each point of use in the waitlist and detail view models |

The rule, exactly: a file is drawn only when it exists, can be read as a picture, measures at least 48 by 48 pixels,
and is square within a tolerance of 0.02.

### 1.2 Storage locations are settings (FR-006 to FR-011)

| Delivered | Evidence |
|---|---|
| The two default roots, the key-file naming, and path resolution against a root | `MTM_Waitlist.Shared/Helpers/AppStoragePaths.cs` |
| The two settings, both organisation-wide | `image_storage.shared_folder_path`, `keys.folder_path` in `MTM_Waitlist.Settings/Models/ConfigSettingValue.cs` and `ImageStorageConfigurationResolver.cs` |
| Pictures stored relative to the root, under a per-scope folder, with an archive inside that scope | `MTM_Waitlist.Settings/Services/ImageStorageService.cs` |
| The fifteenth permission, its area, its fallback and its descriptions | `permission.settings.storage_paths` in `MTM_Waitlist.Core/Permissions/PermissionKeys.cs`, `PermissionRegistry.cs`, `Strings/en-us/Resources.resw` |
| The permission granted to IT Department and Developer only | `Database/Seeds/seed_permission_role_baselines/create.sql` (nine role rows: the two entitled roles at 1, the other seven at 0) |
| The dead catalogue rows retired, and the seed no longer recreating them | `Database/Seeds/seed_waitlist_request_item_images/create.sql` and `rollback.sql` now write no rows and delete the legacy `Assets/RequestTypes/%` rows; the same delete is in `Database/Seeds/AllSeeds.sql` |
| The shipped configuration points at the agreed network locations | `appsettings.json` (`ImageStorage.SharedFolderPath`, `ImageStorage.KeysFolderPath`, `ImageStorage.CacheFolderPath`, `ImageStorage.CacheEnabled`, `DunnageImageOptions.RootFolder`) |

### 1.3 The local picture cache (FR-012 to FR-020)

| Delivered | Evidence |
|---|---|
| The cache's shape: two folders mirrored, the supported file types, the receiving application's dunnage folder adopted when present | `MTM_Waitlist.Shared/Helpers/ImageCachePaths.cs` |
| The refresh itself: copy when the copy is missing or the size or last-written time differs, stamp the copy with the source's own write time, remove copies whose source is gone, skip unsupported types, never touch anything else | `MTM_Waitlist.Shared/Services/ImageCacheSyncService.cs` (+ `IImageCacheSyncService.cs`) |
| The two sources and the enabled flag, resolved when a run starts rather than when the container is built | `Services/DependencyInjection/ServiceRegistrationExtensions.cs` |
| The waitlist screens preferring the local copy | `MTM_Waitlist.Settings/Services/ImageLocationService.cs` (`PreferCachedCopy`) |
| The dunnage screens preferring the local copy | `MTM_Waitlist.Setup/Services/DunnageImagePathResolver.cs` (`GetDisplayPath`) |
| The startup step, between the session check and the first screen, reporting its own splash line and never failing the start | `MTM_Waitlist.Startup/Services/StartupCoordinator.cs` |
| The Settings section: organisation-wide switch, copy folder with Save, refresh with a report, offered only to the entitled roles | `Module_Settings/Views/SettingsPage.xaml`, `SettingsPage.xaml.cs`, `MTM_Waitlist.Settings/ViewModels/SettingsViewModel.cs` |
| The two caching settings | `image_cache.enabled`, `image_cache.folder_path` |
| The changelog entry written for end users | `CHANGELOG.md` |

The Settings section reuses `permission.settings.part_pictures`, the entitlement the picture screen already uses, so
no sixteenth permission key was added and the two cannot drift apart.

## 2. Files removed

`Assets/pickup_fg.png`, `pickup_ncm.png`, `pickup_os.png`, `pickup_wip.png`, `scrap.png`, `coil.png`,
`Assets/Placeholders/default-workstation-image.png`, `default-request-item.png`, `default-request-catagory.png`, and
the six files under `Assets/RequestItems/`. Their `Content` entries were removed from `MTM_Waitlist.csproj`.

## 3. Verification

| Check | Command | Outcome |
|---|---|---|
| Whole solution builds | `dotnet build MTM_Waitlist.sln -p:Configuration=Debug -p:Platform=x64 /m:1 /nodeReuse:false` | succeeded, 0 warnings, 0 errors |
| Whole suite | `dotnet test "MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj" -c Debug -p:Platform=x64` | Failed 0, Passed 1372, Skipped 52, Total 1424 |
| The new tests alone | same, filtered to the four new and updated classes | Failed 0, Passed 27 (cache paths and startup), Passed 365 (settings module) |
| The running application | built executable, both database connection overrides set | reached the shell, signed in as the configured developer account, waitlist loaded 21 requests |
| The startup cache step in a real run | same run, with the picture share unreachable from the session | nothing was copied, the no-copy folder was never created, and startup completed: the best-effort path observed rather than argued |
| The Settings section renders | UI Automation read of the Settings screen | "Picture Cache" rendered directly under "Image Location Settings", visible to the developer account |
| The living-spec addition parses, and its shape holds | `python .specify/extensions/companion/scripts/living_validate.py --capability startup --json` | no shape findings for the added requirement (it has a heading, and both of its scenarios carry WHEN and THEN). One `spec-too-large` warning is reported against `capabilities/startup/spec.md`. That condition predates this change: the file was already over both thresholds (10 requirements, about 208 lines) before the requirement was added, against a threshold of 8 requirements or 160 lines. Adding one requirement made an existing oversize slightly worse; it did not create it. |

New and extended tests: `MTM_Waitlist.Tests/Module_Shared/Helpers/ImageCachePathsTests.cs`,
`.../Helpers/AppStoragePathsTests.cs`, `.../Services/ImageCacheSyncServiceTests.cs`,
`MTM_Waitlist.Tests/Services/StartupCoordinatorTests.cs`, `MTM_Waitlist.Tests/Module_Settings/SettingsViewModelTests.cs`,
`.../TestDoubles.cs`, plus the picture-rule cases in `Module_Waitlist` and `Module_Settings`.

## 4. Not verified

- **The picture cache was never seen filling.** The share `\\mtmanu-fs01\Expo Drive` is not reachable from the
  development session used (the drive letter is not mapped there), so the run above exercised the unreachable path
  only. The copy, replace and remove paths are covered by unit tests against real files on disk; they have not been
  observed against the real share.
- **The Settings section's three cards were not opened.** That control exposes no expand action to automation, so the
  switch, the folder box and the refresh button have been proven by test and by compilation, not by click.
- Hashing is deliberately not used to decide whether a picture changed (FR-013 uses size and last-written time). A
  source that changes a file without changing either will not be re-copied. This is the receiving application's own
  rule and is recorded as a deliberate ceiling, not an oversight.

## 5. Outstanding

- **FR-021**: no Settings screen writes the two storage folders. The entitlement, the stored values, the resolver and
  every reader are in place, and the folder paths resolve from the settings today; what is missing is the screen.
- **Database deployment**: `Database/Seeds/seed_permission_role_baselines/create.sql` must be applied, and
  `Database/Seeds/AllSeeds.sql` re-run, on each store that should carry the new permission baseline and the retired
  catalogue rows.
- **Untracked files to stage**: `MTM_Waitlist.Shared/Helpers/`, `MTM_Waitlist.Shared/Services/`,
  `MTM_Waitlist.Tests/Module_Shared/`, and `Assets/Placeholders/default-no-image.png`. The last one is the
  application's only fallback picture and is not tracked, so a fresh clone would have none.

## 6. Notes for a reviewer

- `.specify/feature.json` still points at `specs/006-user-management-and-permissions`. It was deliberately not
  repointed, because changing the active feature is a workflow decision rather than a documentation one.
- The areas this feature touched (Shared, Settings, Setup, Waitlist, Database, Module_Settings) are not registered in
  `living-specs.yml`, so drift across them stays invisible by design. Only the startup step has a capability home, and
  its requirement has been added to `capabilities/startup/spec.md`. Registering the other areas is a separate,
  opt-in decision recorded in that file's own commentary.
- The startup capability is over the size the validator recommends: 11 requirements over 233 lines, against a
  threshold of 8 requirements or 160 lines. That was already true before this change. Splitting the file into
  granular specs, one per concern, is the validator's suggested remedy and a decision for whoever owns that
  capability.
- The editor may report stale errors in the test files (for example the same signature listed as ambiguous with
  itself). The compiler does not: a fresh build of the test project reports 0 warnings and 0 errors.
