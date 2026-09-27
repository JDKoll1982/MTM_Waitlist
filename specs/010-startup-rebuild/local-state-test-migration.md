# Local-State Test Migration Map

Feature: `010-startup-rebuild` · Created: 2026-09-26 · Authoritative for finding **I5**

This file is the single authoritative list of what happens to the test surface when the local settings
mechanism is removed. It replaces the three conflicting figures that were in circulation (plan.md's
"18 test files with 115 local-settings references", tasks.md T157's "twelve behaviour-test files listed in
S11.5.5", and a tree-wide string search that counted differently).

Sources: `STARTUP-REBUILD-BRIEF.md` §S11.5.3, §S11.5.5; `tasks.md` T025, T134, T135, T137, T138, T139, T142,
T157; `plan.md` Scope.

Line numbers were taken from the working tree on 2026-09-26 and are 1-based and inclusive. A test method's
attribute line sits one line above its start line; a nested test double's closing brace is the last line of
its range.

---

## 1. Test files slated for deletion

### 1A. Deleted by the local-state migration (S11.5.5, six files)

| Test file | Lines | Refs (brief) | Deleted by | Replacement coverage |
|---|---|---|---|---|
| `MTM_Waitlist.Tests/Services/LocalSettingsServiceTests.cs` | 200 | 13 | T157 | T138 `Services/ScopedPreferenceTests.cs`; the store-side settings seam is covered by T137 |
| `MTM_Waitlist.Tests/Services/StartupRecoveryServiceTests.cs` | 70 | 7 | T025 | T120 recreates the same path against the rebuilt recovery service |
| `MTM_Waitlist.Tests/ViewModels/SplashViewModelTests.cs` | 142 | 6 | T025 | T085 `Module_Startup/ViewModels/SplashViewModelTests.cs` |
| `MTM_Waitlist.Tests/Services/StartupCoordinatorTests.cs` | 1095 | 6 | T025 | T077/T082 (pipeline and retry), T080 (runner), T081 (feed) |
| `MTM_Waitlist.Tests/Services/StartupLogServiceTests.cs` | 163 | 5 | T134 | T129 (log panel), T130 (store-log coverage) |
| `MTM_Waitlist.Tests/Module_Startup/Services/SignOutServiceTests.cs` | 198 | 5 | T025 | T111 (session cleared on sign-out, judged on the store clock) |

### 1B. Deleted by the initial startup purge (T025, nine files)

Four of these are also in §1A, which is expected: the brief and the purge list overlap.

| Test file | Lines | In §1A? | Deleted by |
|---|---|---|---|
| `MTM_Waitlist.Tests/Services/ComputerGateServiceTests.cs` | 148 | no | T025 |
| `MTM_Waitlist.Tests/Services/ComputerRegistryServiceTests.cs` | 286 | no | T025 |
| `MTM_Waitlist.Tests/Services/StartupCoordinatorTests.cs` | 1095 | yes | T025 |
| `MTM_Waitlist.Tests/Services/StartupRecoveryServiceTests.cs` | 70 | yes | T025 |
| `MTM_Waitlist.Tests/ViewModels/LoginViewModelTests.cs` | 773 | **corrected — see below** | T025 |
| `MTM_Waitlist.Tests/ViewModels/SplashViewModelTests.cs` | 142 | yes | T025 |
| `MTM_Waitlist.Tests/Module_Startup/Services/SignOutServiceTests.cs` | 198 | yes | T025 |
| `MTM_Waitlist.Tests/Module_Startup/Services/StartupArchiveCleanupTests.cs` | 153 | no | T025 |
| `MTM_Waitlist.Tests/Module_Startup/Services/TemporaryCredentialLimitTests.cs` | 406 | no | T025 (replaced by T106's `TemporaryCredentialAttemptLimitTests.cs`) |

### 1C. Correction to S11.5.5's classification

`MTM_Waitlist.Tests/ViewModels/LoginViewModelTests.cs` (773 lines) is listed by S11.5.5 among the files
"kept and re-pointed at the store seam" with 13 references, but T025 deletes it with the rest of the old
sign-in surface. The deletion is right and the S11.5.5 classification is wrong: the file tests
`LoginViewModel`, which is retired, and it declares its own `NoOpLocalSettingsService` at lines
**642–685**. Its remember-me and session-token tests (lines **43–64**, **143–190**) disappear with it. The
replacement coverage is T109 (`Module_Startup/ViewModels/SignInViewModelTests.cs`) for the sign-in surface
and T108 (`RememberedSignInServiceTests.cs`) for the remembered sign-in.

**The kept list is therefore eleven files, not twelve.**

---

## 2. Files kept and re-pointed at the store seam

One section per file. "Remove" covers both test methods and nested test doubles, because in most of these
files the only thing that changes is the double.

### 2.1 `MTM_Waitlist.Tests/Core/Services/IgnoredLocationsServiceTests.cs` — 90 lines

| Unit | Kind | Start | Stop | Disposition | Why |
|---|---|---|---|---|---|
| `InMemorySettings` | test double | 56 | 89 | Replace with the store-seam double | Implements `ILocalSettingsService`; used at lines 14, 24, 36 and 46 |

No test method is removed.

| Test to add | Task | Notes |
|---|---|---|
| none | — | The four tests keep their assertions; only the double changes (T157). Plant-scope read behaviour is re-asserted by T142's change and covered by T138's scope tests. |

### 2.2 `MTM_Waitlist.Tests/Core/Services/NewRequestAlertServiceTests.cs` — 101 lines

| Unit | Kind | Start | Stop | Disposition | Why |
|---|---|---|---|---|---|
| `StubLocalSettings` | test double | 64 | 100 | Replace with the store-seam double | Implements `ILocalSettingsService`; constructed by five tests (lines 14, 24, 34, 45, 58) |

No test method is removed.

| Test to add | Task | Notes |
|---|---|---|
| none | — | T138 owns the person-scoped alert preference proof. |

### 2.3 `MTM_Waitlist.Tests/Core/Services/NewRequestAlertNotifierTests.cs` — 124 lines

| Unit | Kind | Start | Stop | Disposition | Why |
|---|---|---|---|---|---|
| `StubLocalSettings` | test double | 75 | 106 | Replace with the store-seam double | Implements `ILocalSettingsService`; constructed by four tests (lines 15, 37, 52, and the file's fourth) |

No test method is removed. `RecordingNotificationService` (108–123) stays unchanged.

| Test to add | Task | Notes |
|---|---|---|
| none | — | T138 owns the person-scoped alert preference proof. |

### 2.4 `MTM_Waitlist.Tests/Core/Services/UrgencySettingsServiceTests.cs` — 179 lines

| Unit | Kind | Start | Stop | Disposition | Why |
|---|---|---|---|---|---|
| none | — | — | — | — | This file declares no local-settings double |

`TheService_NoLongerTakesThePerWindowsUserLocalSettingsStore` (declared at line 38) asserts by reflection
that the service does **not** take `ILocalSettingsService`. That assertion stays true after T142 and the
method is not removed; it is re-verified rather than edited.

| Test to add | Task | Notes |
|---|---|---|
| none | — | — |

### 2.5 `MTM_Waitlist.Tests/Module_Core/Services/WaitlistSortPreferenceServiceTests.cs` — 109 lines

| Unit | Kind | Start | Stop | Disposition | Why |
|---|---|---|---|---|---|
| `InMemoryLocalSettings` | test double | 84 | 108 | Replace with the store-seam double | Implements `ILocalSettingsService`; constructed by five tests (lines 19, 35, 47, 60, 72) |

No test method is removed.

| Test to add | Task | Notes |
|---|---|---|
| none | — | T138 owns the person-scoped sort-order proof. |

### 2.6 `MTM_Waitlist.Tests/Module_Settings/SettingsViewModelTests.cs` — 890 lines

| Unit | Kind | Start | Stop | Disposition | Why |
|---|---|---|---|---|---|
| `AddAndRemove_PersistToLocalSettings` | test method | 36 | 55 | **Remove** | Asserts the local key `Feature.IgnoredLocations` through the local double. T146 deletes the duplicate local read and calls the service instead; the store-backed behaviour is asserted by T137 |
| `DefaultSeed_PopulatesTheDefaultIgnoredLocations` | test method | 24 | 33 | Re-point | Only the constructor plumbing changes (line 27); the assertion stays |
| `PersistenceRoundTrip_SeedsFromStoredInsteadOfDefaults` | test method | 58 | 66 | Re-point | Seeds the local key at line 60; must seed the store seam instead |
| `AddRejectsInvalidCode_WhenThePermissionIsNotHeld` | test method | 69 | 79 | Re-point | Line 73's comment and gate name the old `permission.settings.ignored_locations`; T146 moves the write gate to the new edit key |
| `CanManageIgnoredLocations_FollowsThePermissionAndNothingElse` | test method | 82 | 97 | Re-point | Lines 89 and 94 use `PermissionKeys.SettingsIgnoredLocations`; the manage gate becomes `SettingsIgnoredLocationsEdit` under T146 |
| `RecordingLocalSettingsService` | test double | 856 | 889 | Remove | Implements `ILocalSettingsService`; passed by the two `BuildViewModel` helpers |
| `BuildViewModel` / `BuildViewModelWithoutLoading` | test helpers | 654 | 712 | Re-point | Their `ILocalSettingsService` parameter becomes the store seam |

No other method in this file asserts local-file persistence: the remaining `RecordingLocalSettingsService`
mentions (lines 103, 140, 147, 169, 195–205, 213, 237, 265, 282, 295, 316, 334, 344, 404–409, 430, 494,
520, 525, 533, 545, 575, 589, 609, 640) are constructor plumbing inside tests whose assertions do not depend on it.

| Test to add | Task | Notes |
|---|---|---|
| The ignored-locations editor is read-only for every role but `IT Department` and `Developer`, and the write is refused where the action happens | T137 | Backs FR-024 |
| The session-length setting refuses an unauthorised change with a stated reason | T137 | Backs FR-029, SC-011 |

### 2.7 `MTM_Waitlist.Tests/Module_Settings/TestDoubles.cs` — 556 lines

| Unit | Kind | Start | Stop | Disposition | Why |
|---|---|---|---|---|---|
| `FakeLocalSettingsService` | test double | 189 | 200 | **Remove** | Implements `ILocalSettingsService`. Already dead: no test in the tree constructs it, so this is a pure deletion with no caller to update |

| Test to add | Task | Notes |
|---|---|---|
| none | — | — |

### 2.8 `MTM_Waitlist.Tests/Module_Setup/Services/SetupPersistenceServiceTests.cs` — 172 lines

| Unit | Kind | Start | Stop | Disposition | Why |
|---|---|---|---|---|---|
| `InMemoryLocalSettingsService` | test double | 133 | 171 | **Remove** | Implements `ILocalSettingsService`. No instantiation found anywhere in the tree, so this is a pure deletion |

No test method is removed.

| Test to add | Task | Notes |
|---|---|---|
| none | — | — |

### 2.9 `MTM_Waitlist.Tests/Module_Setup/Services/SetupWorkflowServiceTests.cs` — 280 lines

| Unit | Kind | Start | Stop | Disposition | Why |
|---|---|---|---|---|---|
| `InMemoryLocalSettingsService` | test double | 216 | 254 | **Remove** | Implements `ILocalSettingsService`. No instantiation found outside its own constructor |

No test method is removed.

| Test to add | Task | Notes |
|---|---|---|
| none | — | — |

### 2.10 `MTM_Waitlist.Tests/Module_Waitlist/Services/WaitlistInventoryServiceTests.cs` — 146 lines

| Unit | Kind | Start | Stop | Disposition | Why |
|---|---|---|---|---|---|
| `InMemorySettings` | test double | 112 | 145 | Replace with the store-seam double | Implements `ILocalSettingsService`; used by tests at lines 22, 38 and 52 and by the `CreateService` helper at line 85 |

No test method is removed.

| Test to add | Task | Notes |
|---|---|---|
| none | — | The ignored-locations filter assertions already sit in this file and are kept. |

### 2.11 `MTM_Waitlist.Tests/Module_Waitlist/Services/WaitlistRequestServiceTests.cs` — 1607 lines

| Unit | Kind | Start | Stop | Disposition | Why |
|---|---|---|---|---|---|
| `InMemoryLocalSettingsService` | test double | 1551 | 1589 | **Remove** | Implements `ILocalSettingsService`. No instantiation found anywhere in the tree, so this is a pure deletion |

No test method is removed. (`RefusingPermissionService` at 1592–1606 stays.)

| Test to add | Task | Notes |
|---|---|---|
| none | — | T138 owns the person-scoped seen-requests proof. |

---

## 3. Where the replacement coverage is added instead

The eleven kept files gain almost nothing of their own; the coverage that the removed tests used to give
lands in new files. These are the new test files the feature adds, with the task that writes each.

| New test file | Task | Covers |
|---|---|---|
| `MTM_Waitlist.Tests/Services/ScopedPreferenceTests.cs` | T138 | Theme, waitlist order, alerts and seen-requests follow the person (FR-023, SC-008) |
| `MTM_Waitlist.Tests/Module_Core/Permissions/RoleRestrictionInventoryTests.cs` | T139, T149 | Every role-restricted setting keeps its restriction (FR-030, SC-012) |
| `MTM_Waitlist.Tests/Module_Startup/Services/LaunchStepRunnerTests.cs` | T080 | Steps named before they run; stated maximum enforced |
| `MTM_Waitlist.Tests/Module_Startup/Services/LaunchActivityFeedTests.cs` | T081 | Append-only, timestamped feed |
| `MTM_Waitlist.Tests/Module_Startup/Services/LaunchPipelineRetryTests.cs` | T082 | Retry repeats only the failed step and what follows |
| `MTM_Waitlist.Tests/Module_Startup/Services/PictureCacheStepTests.cs` | T083 | Picture refresh cannot stop the launch |
| `MTM_Waitlist.Tests/Module_Startup/Services/VisualVerdictPrimingStepTests.cs` | T084 | Verdict settled before a screen opens, bounded |
| `MTM_Waitlist.Tests/Module_Startup/ViewModels/SplashViewModelTests.cs` | T085 | Feed lines precede the work they describe |
| `MTM_Waitlist.Tests/Module_Startup/Services/MachineSetupGateTests.cs` | T095 | Setup authorised by two roles only |
| `MTM_Waitlist.Tests/Module_Startup/Services/MachineSetupNoBypassTests.cs` | T096 | No route reaches the shell from an unconfigured machine |
| `MTM_Waitlist.Tests/Module_Startup/ViewModels/MachineSetupViewModelTests.cs` | T097 | Duplicate display name refused |
| `MTM_Waitlist.Tests/Module_Startup/Services/TemporaryCredentialAttemptLimitTests.cs` | T106 | Five-attempt limit survives a restart |
| `MTM_Waitlist.Tests/Module_Startup/Services/LaunchSessionServiceTests.cs` | T107 | Store clock decides; sign-out clears first |
| `MTM_Waitlist.Tests/Module_Startup/Services/RememberedSignInServiceTests.cs` | T108 | Unreadable key falls back to the ordinary form |
| `MTM_Waitlist.Tests/Module_Startup/ViewModels/SignInViewModelTests.cs` | T109 | The hint states what is missing |
| `MTM_Waitlist.Tests/Module_Startup/Services/BlockedStateRemediesTests.cs` | T119 | Only remedies that could work are offered |
| `MTM_Waitlist.Tests/Module_Startup/Services/StartupRecoveryServiceTests.cs` | T120 | Reset previews and confines itself (recreated after T025 deletes the old path) |
| `MTM_Waitlist.Tests/Module_Startup/ViewModels/BlockedStateViewModelTests.cs` | T121 | Retry goes through retry-from |
| `MTM_Waitlist.Tests/Module_Settings/ViewModels/DeveloperLogPanelViewModelTests.cs` | T129 | Bounded, filtered panel queries |
| `MTM_Waitlist.Tests/Module_Startup/Services/StoreLogCoverageTests.cs` | T130 | A release build records at least one entry |
| `MTM_Waitlist.Tests/Module_Startup/EdgeCases/LaunchEdgeCaseTests.cs` | T159, T171–T174 | The reconstructed edge-case catalogue, including the four spec cases that had no task of their own |

---

## 4. Validation

Checked on 2026-09-26 against the working tree.

**Reconciles.**

- Every file named in §1A, §1B, §1C and §2 exists, and every line range is inside its file's length.
- The six §1A deletions, the eleven §2 files, `RetiredSymbolAuditTests.cs` (which needs no change) and the
  two `LocalSettingsOptions` model-default tests account for all twenty test files that name a local-settings
  type.
- T025 covers four of the six §1A deletions (`StartupCoordinatorTests`, `StartupRecoveryServiceTests`,
  `SplashViewModelTests`, `SignOutServiceTests`); T157 covers `LocalSettingsServiceTests`; T134 covers
  `StartupLogServiceTests` and the log-path file's production counterpart.
- No removal in §2 breaks a caller: four of the removed doubles (`FakeLocalSettingsService`,
  `InMemoryLocalSettingsService` in `SetupPersistenceServiceTests`, `InMemoryLocalSettingsService` in
  `SetupWorkflowServiceTests`, `InMemoryLocalSettingsService` in `WaitlistRequestServiceTests`) are never
  constructed anywhere in the tree, so they are dead code today.
- The five test methods named in §2.6 are the only test methods in the kept set that assert local-file
  persistence; every other local-settings mention in a kept file is constructor plumbing.

**Does not reconcile — recorded, not smoothed over.**

1. **The brief's per-file reference counts are hand counts.** A mechanical search of the tree for
   `ILocalSettingsService`, `LocalSettingsService`, `LocalSettingsOptions`, `SettingsStorageExtensions` and
   the store doubles finds **233 matches across 20 test files**, not 115 across 18. The extra two files are
   `Core/Models/StartupOptionsModelsTests.cs` and `Models/StartupModelsTests.cs`, which assert
   `LocalSettingsOptions` defaults and are not part of the migration. The brief's number is therefore a
   count of *store usages* per file, not of identifier occurrences; it is not reproducible by search and
   should not be quoted as if it were new. Use the §1 and §2 line ranges instead.
2. **`LoginViewModelTests.cs` is misclassified in S11.5.5** (§1C). This is the substantive correction in
   this file.
3. **T137's file path was wrong — resolved 2026-09-26.** It named
   `MTM_Waitlist.Tests/Module_Settings/ViewModels/SettingsViewModelTests.cs`; the file is at
   `MTM_Waitlist.Tests/Module_Settings/SettingsViewModelTests.cs` (there is no `ViewModels/` copy), and that is
   the path T137 now carries, so the ignored-locations coverage stays in one file rather than splitting across
   two.
4. **`TestDoubles.cs` is counted as one of the eighteen, but it is a shared double file, not a test file.**
   It holds no test methods, so it belongs in §2 as a deletion of one type, which is how it is recorded
   here.
5. **Two test files compile against `LocalSettingsOptions` — resolved 2026-09-26.**
   `Core/Models/StartupOptionsModelsTests.cs` (`LocalSettingsOptions_Defaults`, declared at line 98) and
   `Models/StartupModelsTests.cs` (`LocalSettingsOptions_AcceptsConfiguredValues`, declared at line 71) both
   construct the type that T152 deletes, and T152 now names both files and both method names, so neither is
   left uncovered. The same shape applies
   to the test files that construct `StartupState` (48 files mention it), which T040's migration and T043's
   build confirmation cover only implicitly.

---

## 5. Totals

| Measure | Count |
|---|---|
| Test files deleted (local-state migration, §1A) | 6 |
| Test files deleted (startup purge, §1B, four of them overlapping §1A) | 9 |
| Distinct test files deleted (§1A ∪ §1B) | 11 |
| Test files kept and re-pointed (§2) | 11 |
| Test methods removed from kept files | 1 (`AddAndRemove_PersistToLocalSettings`, T157, §2.6) |
| Test methods re-pointed in kept files | 4 (§2.6) |
| Test doubles removed from kept files | 10 (5 replaced, 5 deleted outright) |
| Test doubles replaced with the store seam | 5 (§2.1, §2.2, §2.3, §2.5, §2.10) |
| Test doubles deleted outright | 5 (§2.6, §2.7, §2.8, §2.9, §2.11) |
| Existing test files that gain new tests | 1 (`SettingsViewModelTests.cs`, T137) |
| New test files the feature adds | 21 (§3) |
