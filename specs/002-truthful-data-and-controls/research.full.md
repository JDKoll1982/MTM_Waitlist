# Phase 0 — Research: Truthful Data and Truthful Controls

**Date**: 2026-09-12 | **Plan**: [plan.md](./plan.md) | **Spec**: [spec.md](./spec.md)

Every unknown in the Technical Context is resolved below. Sources are the feature's own defect documents
(`defects/*.md`), the repository instruction files, and Microsoft Learn (constitution IV).

---

## R1 — How an unsubstantiated value is presented

**Decision**: Omit the row. Where the empty result *is* the answer, hide the block. Where the read
failed, show the error in place with a retry. Absence and failure are never the same state.

**Rationale**: `defects/Critical-Waitlist-CardAndDetailShowFabricatedMaterialData.md` §6.5 — "If any of
these fields genuinely cannot be sourced yet, **omit the row** rather than print a placeholder value — a
missing row is truthful, a fake value is not." The same defect lists the hard-coded `"Not available"`
defaults among the fabricated literals (§3), so they are removed with the rest rather than used as the
absence marker. The absence/failure split is the recorded clarification of 2026-09-12.

**Alternatives considered**: (a) a localized "Not available" label — rejected: it is the existing
placeholder, and it hides a real read failure behind the same string as a legitimate absence;
(b) the page-level `Unavailable` state for a block failure — rejected: `specs/001` FR-021 already gives
that state the meaning "an internal store is unreachable", one failed section must not cost the page, and
reusing it would overload an operator-facing message.

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

**Rationale**: Microsoft Learn — *x:Load attribute*: "Using **x:Load** has a similar visual effect to
**Visibility**, except that when the element is not loaded, its memory is released and internally a small
placeholder is used to mark its place in the visual tree"; it is driven by `x:Bind`; on a container,
children load and unload as a group; the placeholder costs ~600 bytes per attributed element, so it should
be used "only on elements that need to be hidden". *Optimize XAML loading* adds the counterweight used
here for the list case: "Be wary of deferring elements in a `ListView`, as it will decrease your startup
time, but could also decrease your panning performance". Repo precedents: `Module_Mock/Views/ReadStatusIndicator.xaml`
(`InfoBar` bound via `x:Bind`) and the Settings expanders (`x:Load` + one-way binding). Repo traps carried
into the tasks: the XAML compiler requires `x:Name` on anything using `x:Load`, and a one-way `x:Load`
re-evaluates only when `PropertyChanged` is raised for that property — the same mechanism whose absence
causes the settings-search defect.

**Alternatives considered**: `Visibility` everywhere (rejected for the detail sections: collapsed elements
are still created, which the platform guidance explicitly calls out); `x:Load` on card rows (rejected on
the platform's own panning guidance); code-behind `FindName` load/unload (rejected: `x:Bind` already
expresses it and name-based lookups are the fragile path).

---

## R3 — Notification capability gate

**Decision**: The settings view model exposes a read-only capability (working name
`IsNewRequestAlertsAvailable`) answering "can *this installation* deliver a new-request notification?" —
today, in the unpackaged build, it is false. The toggle binds `IsEnabled` to it, and the static
explanation is replaced by a localized message about this installation. No delivery work is done here.

**Rationale**: Microsoft Learn, *Use app notifications with a .NET app* / *console app*: "For unpackaged
apps, `Register()` automatically sets up the COM server registration that allows Windows to launch your
app when a notification is clicked. You don't need to configure COM activation or an AUMID manually", and
"`NotificationInvoked` handles clicks that occur while the app is already running." So the platform does
support notifications for unpackaged WinUI 3 apps; this app has simply never registered, and its whole
notification path is gated on package identity (`MTM_Waitlist.Core/Services/AppNotificationService.cs`
lines 28/47/73 and `WaitlistRequestService.cs:809` both test `RuntimeHelper.IsMSIX`). Consequence for this
feature: the surface must describe **this installation's** capability, not assert a packaging-only
platform rule — the shipped sentence "Notifications are only shown when the app is installed as a packaged
app" is a claim Microsoft Learn contradicts and is corrected with the same change.

**Alternatives considered**: (a) keep gating the toggle on `RuntimeHelper.IsMSIX` with no named capability
— rejected: it hard-codes a packaging assumption the platform does not require, and leaves the surface to
be rewritten when delivery lands; (b) call `AppNotificationManager.Register()` and let the toggle work
unpackaged — rejected: notification *delivery* is a later specification's scope, and the feature's rule
forbids offering a capability that is not implemented.

---

## R4 — Where the new and corrected strings live

**Decision**: Use the file each surface already reads — `Strings/en-us/Resources.resw` for the waitlist,
Settings/About and notification strings — and follow the existing key style
(`Settings_*`, `Waitlist*`, `Store_Unavailable.*`). Delete the retired keys rather than leaving them, and
never move a key another surface still reads. If any surface is already on the per-file layout
(`Strings/en-us/<Module>/<File>.resw`, currently `Module_Setup`), add its keys there.

**Rationale**: Grep of the consumers — `Module_Settings/Views/SettingsPage.xaml` reads
`SettingsPage_PrivacyTermsLink.*`, and the orphan `MockSaved` (~line 211 in `Resources.resw`) plus
`AppNotificationSamplePayload` (~495) sit in the same file; `Module_Waitlist` views read the same file.
Constitution V requires resource-backed localized text; it does not require a single file layout.

**Alternatives considered**: migrating every surface to per-file `.resw` in this change — rejected: a
localization reorganisation is not this feature's job and would enlarge the diff for no user-visible gain.

---

## R5 — Verification seams

**Decision**: MSTest tests in the existing per-module folders under `MTM_Waitlist.Tests`. The
settings-search coverage check uses reflection over `SettingsViewModel` properties whose getter calls
`MatchesSearch(...)`, compared against a declared `static readonly string[]` registration list, and the
existing value-only test is corrected to assert that `PropertyChanged` was raised. The retired-wording and
placeholder-value scans extend the existing `MTM_Waitlist.Tests/Module_Mock/RetiredSymbolAuditTests.cs`,
which already scans `.cs/.xaml/.resw/.sql/.csproj/.json`.

**Rationale**: `defects/Low-Settings-SearchIgnoresThreeSettingsPanels.md` §5.2 — "Prefer the array +
coverage test: it fails when the next panel is added without registering"; §5.3 — "assert that a
`PropertyChanged` was raised … the existing test reads the property directly, so it passes regardless of
whether the notification was raised". `defects/Low-Localization-RetiredMockWordingStillShips.md` §4.4 and
`defects/Low-Settings-PrivacyPolicyLinkPointsAtPlaceholderUrl.md` §6.2 both name the audit file as the
extension point rather than a new mechanism.

**Alternatives considered**: `OnPropertyChanged(string.Empty)` (rejected in the defect as a broader
invalidation than the rest of the file uses); a new audit test class (rejected: the extension point
exists).

---

## R6 — UI state, binding and localization conventions

**Decision**: Block presence and failure are view-model properties (`Has…`, `Is…Failed`) with a retry
`[RelayCommand]`; strings are new `.resw` keys resolved through `GetLocalized()` **with a fallback** for
dialog and notification text; `BoolToVisibilityConverter` is already registered globally, so no new
converter is required; `[RelayCommand]` on `XxxAsync` methods generates `XxxCommand` (the trailing `Async`
is stripped), which is the binding name the XAML must use.

**Rationale**: Constitution V (MVVM Toolkit generators, resource-backed text, converters registered in
`App.xaml`, no hardcoded pixel layout); `App.xaml` lines 18–21 confirm the registered converters;
`.github/copilot-instructions.md` records both traps — the `Async` suffix stripping and the `WMC9999` mask
that hides real XAML errors; repo memory records that `GetLocalized()` returns the raw key when the PRI is
stale, so the fallback form is mandatory for text that has no visible label to fall back on.

**Alternatives considered**: a new bool-to-visibility converter in `MTM_Waitlist.Waitlist.View` — not
needed, the global one is registered and reached through `StaticResource`.

---

## R7 — Document set updated in the same change

**Decision**: Update, in one pass: `FEATURES.md` (the ten defect rows and their status),
`defects/*.md` (each closed defect's status line, quoted evidence left untouched),
`WeekendProject/ChangeLog.Simple.md` (feature 18 alert caveat; feature 19 retired toggle),
`WeekendProject/ChangeLog.md` (same behaviour, prose form), `OPEN-TASKS.md` §3.1 (retired-sources note for
the six sources), `WeekendProject/OPEN-WORK-NEXT-SPEC.md` §10.2 (pointer at that note),
`WeekendProject/Module_Mock/Tasks.md` (reconcile the 72 stale boxes).

**Rationale**: FR-026/FR-027/FR-028; `WeekendProject/OPEN-WORK-NEXT-SPEC.md` §10.2 names the changelog
file, the stale backlog sources and the module task file; the alerts defect §5 ("Also correct") and the
retired-wording defect §1 name the changelog features 18 and 19; the repo's closing convention is recorded
in `WeekendProject/SpecTemplates/02-handler-fulfilment-and-urgency.md` §5 — "update that defect file's
status line to `FIXED` … with the commit and the tests that prove it".

**Alternatives considered**: a new standalone "retired sources" document — rejected: `OPEN-TASKS.md` §3.1
already holds the table, so the note goes where a reader already looks.

---

## R8 — Database artifacts

**Decision**: None. This feature adds no table, seed or stored procedure, and removes no stored-procedure
call. If a change would need one, it is out of scope — the real figures belong to the follow-on
specifications (`03`, `04`) that own them. Should any `Database/**` file be touched anyway, it ships
`create.sql` + `rollback.sql` and updates the master lists in the same change.

**Rationale**: FR-011/FR-012 hand the real queue figure and the per-coil weight to the follow-on specs
(`WeekendProject/SpecTemplates/00-INDEX.md` defect→spec map: "01 — remove the fabricated queue/wait card …
04 (optional) — a real queue count and wait estimate"; "01 — remove the wrong-part lookup and its fixed
fallback … 03 — per-coil lookup"). Constitution III and `InlineSqlAuditTests` (T098) apply only if an
artifact is introduced.

**Alternatives considered**: adding the queue-count stored procedure now — rejected: it is template 04's
deliverable and would put an analytics figure behind an unreviewed stop-gap.

---

## R9 — Verification evidence and the environment gate

**Decision**: Gates are the solution build (`/m:1 /nodeReuse:false`, 0 warnings / 0 errors), the full test
suite (`Failed: 0`), the five scan gates (empty outside `specs/` and `defects/`), the per-defect automated
checks (≥ 10 new, each failing against the pre-change behaviour), and a manual pass over the four affected
surfaces. UI automation follows `.github/instructions/winui3-ui-automation.instructions.md`; steps that
need a signed-in session are recorded as environment-gated when no credential is available, exactly as
`specs/001` recorded its live-environment checks.

**Rationale**: Constitution VI; the automation instructions state plainly that "Reaching the **shell**
needs a sign-in as well", so the sign-in-dependent half of the manual pass cannot be claimed from a bare
launch. The existing gates missed these defects because they are runtime-rendered values — the audit
patterns, not the compiler, are what make them visible.

**Alternatives considered**: treating the green build as sufficient — rejected: `defects/Critical-…` §5
records that `RetiredSymbolAuditTests` matched only *symbol names*, so `"Northstar Manufacturing"` shipped
through a green build.
