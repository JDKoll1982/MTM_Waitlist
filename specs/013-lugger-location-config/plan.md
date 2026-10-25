# Implementation Plan: Scrap Lugger Location

**Feature Branch**: `013-lugger-location-config`
**Spec**: `specs/013-lugger-location-config/spec.md`
**Size**: normal (no size recorded on the spec context)

## Summary

`Lugger Location` on the `pickup-scrap` card becomes a live read of a small configuration
capability instead of the "In development" placeholder. A new permissioned Settings page lets a
Material Handler Lead maintain the lugger set, each lugger's status, and the ordered scrap-type
assignments, and a transition engine keeps exactly one dump point per scrap type without the lead
reconciling anything by hand.

The card resolves the cell while the list is being built, from an arrangement snapshot read once
per list load, so a status change shows on the next load and the request stores nothing about the
lugger. Every arrangement change is written by the stored procedure that makes it, in the same
transaction, and the waitlist list shows each change once as an in-application notice.

No new stack choice. The work uses WinUI 3 and `Microsoft.UI.Xaml.Controls`, MySQL stored
procedures through `IMySqlHelperServer`, and the existing service registration, view model and
localization conventions.

## Project Structure

```text
Database/
  Tables/
    AllTables.sql                                             (+ the three new tables)
    36_waitlist_lugger_locations/
    37_waitlist_lugger_scrap_assignments/
    38_waitlist_lugger_arrangement_changes/
  StoredProcedures/
    AllSPs.sql
    sp_waitlist_lugger_locations_list/                        create.sql + rollback.sql
    sp_waitlist_lugger_locations_insert/
    sp_waitlist_lugger_locations_rename/
    sp_waitlist_lugger_locations_delete/
    sp_waitlist_lugger_locations_status_set/
    sp_waitlist_lugger_locations_promote/
    sp_waitlist_lugger_scrap_assignments_list/
    sp_waitlist_lugger_scrap_assignments_set/
    sp_waitlist_lugger_scrap_assignments_delete/
    sp_waitlist_lugger_arrangement_list/
    sp_waitlist_lugger_arrangement_changes_list/
  Seeds/
    AllSeeds.sql
    seed_waitlist_lugger_arrangement_defaults/                create.sql + rollback.sql
    seed_permission_role_baselines/                           (+ nine rows, amend only)
  Bootstrap/
    update_table_descriptions.sql

MTM_Waitlist.Core/
  Models/Lugger/                                              Lugger, LuggerStatus, ScrapAssignment,
                                                              ArrangementChange
  Contracts/Services/ILuggerArrangementService.cs
  Services/LuggerArrangementService.cs                        procedure calls and mapping
  Services/ScrapTypeCatalog.cs                                the shipped scrap-type spellings
  Permissions/PermissionKeys.cs                               + SettingsLuggerArrangement
  Permissions/PermissionRegistry.cs                           + one Entry

MTM_Waitlist.Setup/
  Services/SetupWorkflowService.cs                            reads the catalogue instead of its own array

MTM_Waitlist.Settings/
  Services/RequestJobFieldValues.cs                           + `lugger location` arm
  ViewModels/LuggerArrangementViewModel.cs
  ViewModels/SettingsViewModel.cs                             + entry visibility and open command

Module_Settings/Views/
  LuggerArrangementPage.xaml, LuggerArrangementPage.xaml.cs
  SettingsPage.xaml                                           + the entry card

Services/
  DependencyInjection/ServiceRegistrationExtensions.cs        + one contribution file
  DependencyInjection/ServiceRegistrationExtensions.LuggerArrangement.cs
  RequestJobAvailabilityProvider.cs                           recomputes LuggerLocation per load

MTM_Waitlist.Waitlist.View/
  Helpers/WaitlistCardCellRules.cs                            placeholder list deleted
  Services/ILuggerChangeNoticeService.cs, LuggerChangeNoticeService.cs
  ViewModels/WaitlistViewViewModel.cs                         notices + one poll per tick

Module_Waitlist/Views/WaitlistViewPage.xaml                   notice strip above the list

Strings/en-us/Resources.resw                                  new settings, status and notice keys

MTM_Waitlist.Tests/
  Module_Core/Services/LuggerArrangementServiceTests.cs
  Module_Core/Permissions/...
  Module_Settings/ViewModels/LuggerArrangementViewModelTests.cs
  Module_Settings/Services/RequestJobFieldValuesTests.cs      (amended)
  Module_Waitlist/Services/LuggerChangeNoticeServiceTests.cs
  Module_Waitlist/Views/WaitlistLineCardDetailTemplateTests.cs (amended)
```

**Structure Decision**: the arrangement service, its models and the scrap-type catalogue go in
`MTM_Waitlist.Core`, because Settings and the waitlist card path both need them and Core is the only
project both reference. The notice surface and its watermark live in `MTM_Waitlist.Waitlist.View`,
because only the waitlist list shows a notice.

## Call Paths

```text
pickup-scrap card drawn
  WaitlistViewViewModel.LoadOrdersCoreAsync
    -> RequestJobAvailabilityProvider builds the per-work-centre availability
         -> ILuggerArrangementService.SnapshotAsync            one read per list load
         -> availability with { LuggerLocation = <dump point name, or first name (status)> }
    -> ResolveDeclaredCells -> ResolveDeclaredCell (source 'job')
         -> RequestJobFieldValues.Resolve("Lugger Location", availability)
              -> the snapshot's LuggerLocation, or null so the cell is not drawn

scrap type has no assignment
  LuggerArrangementService.SnapshotAsync -> Resolve returns Gap
    -> RequestJobAvailabilityProvider logs the gap through AppLog.Warn and leaves LuggerLocation empty
    -> the cell is absent; no substitute value is invented

lead sets a status
  LuggerArrangementViewModel -> ILuggerArrangementService.SetStatusAsync
    -> sp_waitlist_lugger_locations_status_set
         one transaction: lock the lugger's assignments, apply the status, re-derive each affected
         scrap type's dump point, reorder so no blocked lugger precedes a dump point, write one
         arrangement-change row per affected scrap type

handler sees the change
  WaitlistViewViewModel minute tick -> ILuggerChangeNoticeService.PollAsync
    -> sp_waitlist_lugger_arrangement_changes_list (id greater than the session watermark)
    -> undismissed, and only while the person holds permission.requests.handle
    -> ObservableCollection<LuggerChangeNotice> -> one InfoBar per notice
    -> Closing with Reason = CloseButton records the dismissal and removes the bar
```

## States

Lugger status, as the lead's set and the card's role read it:

```text
Available: usable, and holding the dump point for at least one scrap type it takes. shows lugger-arrangement
Backup: usable, but not the dump point for the scrap type being read. shows lugger-arrangement
Full: filled, cannot receive. The next usable lugger in that scrap type's order holds the dump point. shows lugger-arrangement
Do Not Use: blocked with a required reason, promoted past exactly as Full is, and the reason is readable. shows lugger-arrangement
```

The four transitions the feature has to perform, none of which the lead reconciles by hand:

```text
Available  --lead sets Full-------------> Full      the next usable lugger becomes the dump point;
                                                    the blocked lugger sorts after every usable one
Full       --lead empties it------------> Backup    the current dump point keeps the role
Do Not Use --lead sets Available--------> Backup    the current dump point keeps the role
Backup     --lead promotes it explicitly-> Available the previous dump point returns to Backup
```

A lugger status is a property of the lugger, so setting it to `Full` or `Do Not Use` affects every
scrap type it takes, and each of those scrap types promotes its own next lugger.

Notice lifetime on the list:

```text
signed in, list opened -> session watermark set to the current highest change id; nothing before it is ever shown
change recorded         -> appears in the notice strip on the next minute tick, once
dismissed               -> gone for that person, and never offered again in this session
```

## Screens

```text
screen lugger-arrangement  The Settings page: every lugger with its status and the scrap types it
                           takes, and the editor behind an explicit action (add, rename, remove,
                           set status with a reason, assign, reorder, promote).
screen waitlist-change-notice  The InfoBar strip above the waitlist list, one closable bar per
                               undismissed arrangement change.
```

## Constitution Check

| Principle | Assessment |
|---|---|
| I. Spec-First, Verified Delivery | PASS. `spec.md` is written and approved; this plan and `tasks.md` precede code; nothing is marked complete before the build and test gates pass. |
| II. Live Data Integrity | PASS. Every read and write reaches `mtm_waitlist` through a procedure. The arrangement snapshot is re-read on each list load and is never a substitute for the store, and there is no mock or demo path. |
| III. Stored-Procedure-First Database Discipline | PASS. Three tables, one procedure per operation, paired `create.sql` and `rollback.sql`, and the four master lists kept in sync. No inline SQL. |
| IV. MCP-First, Grounded Decisions | PASS. The InfoBar close semantics this plan relies on (`Closing` with `Reason`, `IsClosable`) were read from Microsoft Learn, and every repository fact came from the source in this session. |
| V. WinUI 3 Platform Conformance | PASS. `Microsoft.UI.Xaml.Controls`, CommunityToolkit.Mvvm, `INavigationAware`, `.resw` with `GetLocalized`, fluid sizing. InfoBar is not an MSIX-only API, which is why the notice does not depend on packaging. |
| VI. Evidence-Based Verification Gates | PASS. The gates are named in `tasks.md`: clean x64 solution build, green unit suite, schema validation, and the opt-in live DB tests for the new procedures. |
| VII. Readable, End-User-Facing Delivery | PASS. No violation. |

No principle is violated, so there is no Complexity Tracking table.
