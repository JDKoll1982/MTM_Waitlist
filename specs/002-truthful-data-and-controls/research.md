# Phase 0 — Research: Truthful Data and Truthful Controls

**Date**: 2026-09-12 | **Plan**: [plan.md](./plan.md) | **Spec**: [spec.md](./spec.md)

Every Technical Context unknown is resolved below. Sources: the feature's defect documents (`defects/*.md`), the repository instruction files, and Microsoft Learn (constitution IV).

---

## R1 — How an unsubstantiated value is presented

**Decision**: Omit the row. Where the empty result *is* the answer, hide the block. Where the read
failed, show the error in place with a retry. Absence and failure are never the same state.

**Evidence**: `defects/Critical-Waitlist-CardAndDetailShowFabricatedMaterialData.md` §6.5 — "omit the row rather than print a placeholder value — a missing row is truthful, a fake value is not." · Same defect §3 lists the hard-coded `"Not available"` defaults among the fabricated literals, so they go with the rest. · Absence/failure split: recorded clarification, 2026-09-12.

---

## R2 — Block presence mechanism in WinUI 3

**Decision**: Two mechanisms, chosen per surface:

- **Detail-page sections and the confirm-step coil card** (not virtualized): `x:Load="{x:Bind …, Mode=OneWay}"`
  on the section container, with `x:Name` on every element carrying `x:Load`. The failure view is a
  sibling inside the same container, `x:Load`-gated on the failed state, with the retry bound to a
  `[RelayCommand]`.
- **Rows inside the waitlist card list** (items in an `ItemsControl`/list template): `Visibility` bound
  through the already-registered global `BoolToVisibilityConverter` (`App.xaml` line 21, key
  `BoolToVisibilityConverter`) rather than deferring elements inside a list.

**Evidence**: Microsoft Learn *x:Load attribute* — "Using **x:Load** has a similar visual effect to **Visibility**, except that when the element is not loaded, its memory is released"; `x:Bind`-driven; a container's children load and unload as a group; ~600 bytes per attributed element, so use it "only on elements that need to be hidden". · Microsoft Learn *Optimize XAML loading* — "Be wary of deferring elements in a `ListView` … it could also decrease your panning performance". · Precedents: `Module_Mock/Views/ReadStatusIndicator.xaml` (`InfoBar` via `x:Bind`), the Settings expanders. · Traps: `x:Name` is required on anything using `x:Load`; a one-way `x:Load` re-evaluates only on `PropertyChanged`.

---

## R3 — Notification capability gate

**Decision**: The settings view model exposes a read-only capability, `IsNewRequestAlertsAvailable` (the name
`contracts/surface-contracts.md` C3 and the tasks use), answering "can *this installation* deliver a new-request notification?" —
today, in the unpackaged build, it is false. The toggle binds `IsEnabled` to it, and the static
explanation is replaced by a localized message about this installation. No delivery work is done here.

**Evidence**: Microsoft Learn *Use app notifications with a .NET app* — "For unpackaged apps, `Register()` automatically sets up the COM server registration that allows Windows to launch your app when a notification is clicked", and "`NotificationInvoked` handles clicks that occur while the app is already running." · Today's gates: `MTM_Waitlist.Core/Services/AppNotificationService.cs` lines 28/47/73 and `WaitlistRequestService.cs:809` all test `RuntimeHelper.IsMSIX`; the shipped copy claims packaged-only, which the documentation contradicts.

---

## R4 — Where the new and corrected strings live

**Decision**: Use the file each surface already reads — `Strings/en-us/Resources.resw` for the waitlist,
Settings/About and notification strings — and follow the existing key style
(`Settings_*`, `Waitlist*`, `Store_Unavailable.*`). Delete the retired keys rather than leaving them, and
never move a key another surface still reads. If any surface is already on the per-file layout
(`Strings/en-us/<Module>/<File>.resw`, currently `Module_Setup`), add its keys there.

**Evidence**: Consumers read this file — `Module_Settings/Views/SettingsPage.xaml` (`SettingsPage_PrivacyTermsLink.*`), the orphan `MockSaved` (~line 211) and `AppNotificationSamplePayload` (~495) all sit in `Strings/en-us/Resources.resw`, and the `Module_Waitlist` views read it too. · Constitution V requires resource-backed localized text; it does not require one layout.

---

## R5 — Verification seams

**Decision**: MSTest tests in the existing per-module folders under `MTM_Waitlist.Tests`. The
settings-search coverage check uses reflection over `SettingsViewModel` properties whose getter calls
`MatchesSearch(...)`, compared against a declared `static readonly string[]` registration list, and the
existing value-only test is corrected to assert that `PropertyChanged` was raised. The retired-wording and
placeholder-value scans extend the existing `MTM_Waitlist.Tests/Module_Mock/RetiredSymbolAuditTests.cs`,
which already scans `.cs/.xaml/.resw/.sql/.csproj/.json`.

**Evidence**: `defects/Low-Settings-SearchIgnoresThreeSettingsPanels.md` §5.2 — "Prefer the array + coverage test: it fails when the next panel is added without registering"; §5.3 — the existing test "reads the property directly, so it passes regardless of whether the notification was raised". · `defects/Low-Localization-RetiredMockWordingStillShips.md` §4.4 and `defects/Low-Settings-PrivacyPolicyLinkPointsAtPlaceholderUrl.md` §6.2 both name the audit file as the extension point.

---

## R6 — UI state, binding and localization conventions

**Decision**: Block presence and failure are view-model properties (`Has…`, `Is…Failed`) with a retry
`[RelayCommand]`; strings are new `.resw` keys resolved through `GetLocalized()` **with a fallback** for
dialog and notification text; `BoolToVisibilityConverter` is already registered globally, so no new
converter is required; `[RelayCommand]` on `XxxAsync` methods generates `XxxCommand` (the trailing `Async`
is stripped), which is the binding name the XAML must use.

**Evidence**: Constitution V (MVVM Toolkit generators, resource-backed text, no hardcoded pixel layout). · `App.xaml` lines 18–21 confirm the registered converters, including `BoolToVisibilityConverter`. · `.github/copilot-instructions.md` records both traps — the `Async` suffix stripping on `[RelayCommand]` and the `WMC9999` mask over real XAML errors. · `GetLocalized()` returns the raw key when the PRI is stale, so the fallback form is mandatory where no label can fall back.

---

## R7 — Document set updated in the same change

**Decision**: Update, in one pass: `FEATURES.md` (the ten defect rows and their status),
`defects/*.md` (each closed defect's status line, quoted evidence left untouched),
`WeekendProject/ChangeLog.Simple.md` (feature 18 alert caveat; feature 19 retired toggle),
`WeekendProject/ChangeLog.md` (same behaviour, prose form), `OPEN-TASKS.md` §3.1 (retired-sources note for
the six sources), `WeekendProject/OPEN-WORK-NEXT-SPEC.md` §10.2 (pointer at that note),
`WeekendProject/Module_Mock/Tasks.md` (reconcile the 72 stale boxes).

**Evidence**: FR-026/FR-027/FR-028. · `WeekendProject/OPEN-WORK-NEXT-SPEC.md` §10.2 names the changelog file, the stale backlog sources and the module task file. · The alerts defect §5 and the retired-wording defect §1 name changelog features 18 and 19. · Closing convention: `WeekendProject/SpecTemplates/02-handler-fulfilment-and-urgency.md` §5 — "update that defect file's status line to `FIXED` … with the commit and the tests that prove it". · The note goes to `OPEN-TASKS.md` §3.1, which already holds the table.

---

## R8 — Database artifacts

**Decision**: None. This feature adds no table, seed or stored procedure and removes no stored-procedure call. If a change would need one it is out of scope — the real figures belong to the follow-on specifications (`03`, `04`) that own them. Should any `Database/**` file be touched anyway, it ships `create.sql` + `rollback.sql` and updates the master lists in the same change.

**Evidence**: FR-011/FR-012 hand the real queue figure and the per-coil weight to the follow-on specs (`WeekendProject/SpecTemplates/00-INDEX.md` defect→spec map: "01 — remove the fabricated queue/wait card … 04 (optional) — a real queue count and wait estimate"; "01 — remove the wrong-part lookup and its fixed fallback … 03 — per-coil lookup"). · Constitution III and `InlineSqlAuditTests` (T098) apply only if an artifact is introduced.

---

## R9 — Verification evidence and the environment gate

**Decision**: Gates are the solution build (`/m:1 /nodeReuse:false`, 0 warnings / 0 errors), the full suite (`Failed: 0`), the scan gates (empty outside `specs/` and `defects/`), the per-defect automated checks (≥ 10 new, each failing against the pre-change behaviour), and a manual pass over the four affected surfaces. UI automation follows `.github/instructions/winui3-ui-automation.instructions.md`; steps that need a signed-in session are recorded as environment-gated when no credential is available, exactly as `specs/001` recorded its live-environment checks.

**Evidence**: Constitution VI. · `.github/instructions/winui3-ui-automation.instructions.md` — "Reaching the **shell** needs a sign-in as well". · `defects/Critical-Waitlist-CardAndDetailShowFabricatedMaterialData.md` §5 — `RetiredSymbolAuditTests` matched only *symbol names*, so `"Northstar Manufacturing"` shipped through a green build.

<!-- token-budget: compacted (level=aggressive) on 2026-09-12T23:54:00Z; original at research.full.md -->
