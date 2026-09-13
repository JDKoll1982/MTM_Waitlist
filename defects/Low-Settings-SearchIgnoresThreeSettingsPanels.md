# Low — Settings — The settings search does not refresh three of its panels

| Field | Value |
| --- | --- |
| **Criticality** | Low |
| **Feature area** | Settings — search / panel filtering (Image Location Settings, New Request Alerts, Max Allotted Time) |
| **Type** | Missing change notification on computed properties — panels ignore the search |
| **Found** | 2026-09-12, working tree at `7bf6857` |
| **Status** | **FIXED — closed by `specs/002-truthful-data-and-controls`**. **Closure:** the three missing change notifications were added, `RefreshSearchVisibility()` now iterates the declared `s_searchAwareProperties` list that holds all ten search-consuming members, and the coverage check that fails when a future panel is added unregistered is in place — with the existing value-only assertion corrected to assert the change notification. **Remainder owned by: none.** **Proof:** `MTM_Waitlist.Tests/Module_Settings/SettingsViewModelTests`. |
| **Planned fix** | **Spec `01-truthful-data-and-controls`** — adds the three missing change notifications and the coverage test that fails when a future search-aware panel is not registered. Fixing it earlier is a three-line change and would also protect template 06, which adds new panels to the same view model. Seed: `WeekendProject/SpecTemplates/01-truthful-data-and-controls.md` |
| **Files** | `MTM_Waitlist.Settings/ViewModels/SettingsViewModel.cs` (lines 195–209, 238–250, 868–879) |
| **Also recorded as** | `specs/001-module-mock-visual-fallback/tasks.md` (Phase 27, "A pre-existing gap found while editing: `IsUrgencyAllotmentsPanelVisible` is missing from `SettingsViewModel.RefreshSearchVisibility()`") — this file also covers the two sibling properties that were missed |

---

## 1. Summary

The Settings page filters its panels with a search box: each panel exposes a computed visibility
property that consults `MatchesSearch(...)`, and every change to `SearchQuery` calls
`RefreshSearchVisibility()` to raise `PropertyChanged` for them. Three of those properties are missing
from that method:

```csharp
// SettingsViewModel.cs:195 — declared …
public bool IsNewRequestAlertsPanelVisible => MatchesSearch(...);
// SettingsViewModel.cs:202 — declared …
public bool IsUrgencyAllotmentsPanelVisible => MatchesSearch(...);
// SettingsViewModel.cs:238 — declared …
public bool IsImageLocationSettingsPanelVisible => CanManageImageLocationSettings && MatchesSearch(...);

// … but never notified here:
private void RefreshSearchVisibility()                                    // line 868
{
    OnPropertyChanged(nameof(IsAppearancePanelVisible));
    OnPropertyChanged(nameof(IsHotWorkCentersPanelVisible));
    OnPropertyChanged(nameof(IsDunnageTypeVisibilityPanelVisible));
    OnPropertyChanged(nameof(IsIgnoredLocationsPanelVisible));
    OnPropertyChanged(nameof(IsCacheRefreshPanelVisible));
    OnPropertyChanged(nameof(IsAboutPanelVisible));
    OnPropertyChanged(nameof(IsComputersPanelVisible));
    OnPropertyChanged(nameof(IsAppearanceCategoryVisible));
    OnPropertyChanged(nameof(IsOperationsCategoryVisible));
    OnPropertyChanged(nameof(IsAboutCategoryVisible));
}                                                                          // line 879
```

The three expanders load their visibility through a one-way `x:Load` binding
(`Module_Settings/Views/SettingsPage.xaml`, for example line 1018 for the Max Allotted Time expander
and line 1114 for the cached-data panel), so they only re-evaluate when `PropertyChanged` is raised for
that exact property name. They therefore keep their **initial** state no matter what is typed.

## 2. Impact

- Typing a search term filters most panels but leaves these three in place — the visible result is a
  settings list that partially ignores the search.
- Typing a term that should *reveal* one of them does nothing, so a user who searches "alert" or
  "urgency" may conclude the setting does not exist.
- Low severity (no data or state is affected, and the panels are reachable by clearing the search) but
  it makes the settings surface feel unreliable for exactly the three operations panels.

## 3. Evidence

| Item | Evidence |
| --- | --- |
| Declared, search-aware properties | `SettingsViewModel.cs:195` `IsNewRequestAlertsPanelVisible`; `:202` `IsUrgencyAllotmentsPanelVisible`; `:238` `IsImageLocationSettingsPanelVisible` (also role-gated on `CanManageImageLocationSettings`). |
| Missing notifications | `SettingsViewModel.cs:868–879` — the three names above do not appear. The other seven panels plus the three category aggregates do. |
| One-way binding surface | `Module_Settings/Views/SettingsPage.xaml` — `x:Load="{x:Bind ViewModel.IsUrgencyAllotmentsPanelVisible, Mode=OneWay}"` (Urgency expander) and the same pattern for the other two. |
| Documented but unfixed | `specs/001-module-mock-visual-fallback/tasks.md`, Phase 27: the gap was found while editing the file and deliberately left as out of scope; it names only `IsUrgencyAllotmentsPanelVisible`. |
| Tested behaviour | `MTM_Waitlist.Tests/Module_Settings/SettingsViewModelTests.cs:118` sets `SearchQuery = "notification"` and asserts visibility — the test reads the property directly, so it passes regardless of whether the notification was raised. That is why the gap survives. |

## 4. Root cause

`RefreshSearchVisibility()` was written as a hand-maintained list of property names and drifted as
panels were added (the New Request Alerts, Max Allotted Time and Image Location panels were added
after the method). Nothing enumerates the search-aware properties automatically, and the existing test
asserts property values rather than notification delivery.

## 5. Fix

1. **Add the three missing notifications** to `RefreshSearchVisibility()`:

   ```csharp
   OnPropertyChanged(nameof(IsNewRequestAlertsPanelVisible));
   OnPropertyChanged(nameof(IsUrgencyAllotmentsPanelVisible));
   OnPropertyChanged(nameof(IsImageLocationSettingsPanelVisible));
   ```

2. **Remove the drift risk** — the durable fix is to stop hand-listing names. Either
   - iterate a declared array of property names (a `static readonly string[] s_searchAwareProperties`
     placed beside the definitions, with a unit test asserting it covers every property whose getter
     calls `MatchesSearch`), or
   - call `OnPropertyChanged(string.Empty)`, which raises the "all properties changed" notification —
     acceptable here because the settings view model is small and only the visible page subscribes, but
     note it is a broader invalidation than the rest of the file uses.
   Prefer the array + coverage test: it fails when the next panel is added without registering.
3. **Correct the test** that gives false confidence: assert that a `PropertyChanged` was raised for the
   property under test (subscribe in the test), not just that its value is right.

## 6. Verification

1. New test: set `SearchQuery` to a term matching one of the three panels and assert a
   `PropertyChanged` for `IsUrgencyAllotmentsPanelVisible` / `IsNewRequestAlertsPanelVisible` /
   `IsImageLocationSettingsPanelVisible` — fails today.
2. New coverage test: reflection over `SettingsViewModel` properties whose getter references
   `MatchesSearch` must all be present in the notification list (or the array).
3. Manual: open Settings, type "alert", and confirm the New Request Alerts panel is the only operations
   panel left; clear the search and confirm all panels return; repeat with "urgency" and "image".
4. Build `0 Warning(s) 0 Error(s)`; suite `Failed: 0`.

## 7. Related

- `WeekendProject/ChangeLog.Simple.md` feature 16/17 — the ignored-location and urgency panels are
  themselves only partly applied elsewhere; this defect is about their visibility only.
- `MTM_Waitlist.Tests/Module_Settings/SettingsViewModelTests.cs` — the existing search test.
- `FEATURES.md` §8.
