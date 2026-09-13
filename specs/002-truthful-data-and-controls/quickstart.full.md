# Quickstart — validating Truthful Data and Truthful Controls

**Feature**: `002-truthful-data-and-controls` | **Spec**: [spec.md](./spec.md) | **Contracts**: [contracts/](./contracts/)

Runnable validation for the whole feature. Every scenario states what to run, what to look at, and what a
pass looks like. Detail lives in the contracts and the data model; this file is the walkthrough.

---

## Prerequisites

- Windows x64, .NET 10 SDK, the repo at `C:\Users\johnk\source\repos\MTM_Waitlist`.
- The app runs **unpackaged** and self-contained: launch the built executable, no install step.
- Reaching the shell needs a **signed-in account**, and the startup gate needs a reachable
  `mtm_waitlist`. Set both connection overrides for a local database before launching:

  ```powershell
  $cs = 'Server=localhost;Port=3306;Database=mtm_waitlist;User Id=root;Password=root;AllowPublicKeyRetrieval=True;'
  $env:MTM_WAITLIST_DB_CONNECTION_STRING = $cs
  $env:MTM_WAITLIST_STARTUP_DB_CONNECTION_STRING = $cs
  ```

  Without credentials, §3–§6 cannot be walked; record those steps as environment-gated rather than passed.

---

## 1. Build and suite gates

```powershell
dotnet build MTM_Waitlist.sln -c Debug -p:Platform=x64 /m:1 /nodeReuse:false
dotnet test MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj -c Debug -p:Platform=x64
```

Expected: `0 Warning(s) 0 Error(s)`, then `Failed: 0`. `WMC9999` is a masked XAML error — find the real cause,
never ignore it. If the build complains about `PRI175`/`PRI224`, a running app or stale `*.pri` is the cause.

## 2. Scan gates

Each command must return nothing. `defects/` and `specs/` are excluded by design — those files quote the
removed values as evidence.

```powershell
Select-String -Path (Get-ChildItem -Recurse -Include *.cs,*.xaml,*.resw,*.sql -File) -Pattern 'YourPrivacyUrlGoesHere|MockSaved|TODO: Handle notification' | Select-Object Path,LineNumber,Line
Select-String -Path (Get-ChildItem -Recurse -Include *.cs,*.xaml,*.resw -File | Where-Object { $_.FullName -notmatch '\\(specs|defects)\\' }) -Pattern 'RecvMockData|Northstar Manufacturing|COIL-204|MMC0001000|YourPrivacyUrlGoesHere'
```

## 3. Waitlist card and request detail (US1)

1. Launch the app, sign in, open **Waitlist**.
2. Look at any card: the action area offers **no** Cancel/Accept control, and no coil, part, quantity,
   customer, packlist, traceability, destination, vendor or die value is displayed.
3. Open a request's detail page: sections show only sourced values. A section with nothing to show is
   **absent**, not an empty titled box; a section whose read failed shows its error with a **Retry** while
   the rest of the page still renders.
4. With Infor Visual unreachable, confirm the cached values still render and the read-only indicator still
   reports the cached state and its age.

Pass: nothing on either surface states a fact the application cannot support.

## 4. New Request confirm step (US2)

1. Start a New Request and walk to the confirm step for two different work centers.
2. Confirm no active-request count and no estimated wait time appear anywhere, and no stand-in card fills
   the gap.
3. Confirm no average coil weight is shown; in no state does `5,000 lb` appear.
4. Open **Settings → Operations → New Request Alerts** on the unpackaged build: the toggle is disabled and a
   localized message explains that this installation cannot deliver notifications. Toggling is impossible
   and nothing is stored.

## 5. Settings (US3)

1. Open **Settings → About**: the section header, the version card and the about card each read their own
   text (no three-way "About this application").
2. Confirm there is **no** Privacy Policy hyperlink, and that no placeholder address ships.
3. Type `alert`, then `urgency`, then `image` in the settings search: the New Request Alerts, Max Allotted
   Time and Image Location Settings panels each respond like every other panel. Clear the search and confirm
   every panel returns.

## 6. Notification activation (US4)

1. While the app is running, deliver an activation carrying `action=openrequest&request=<guid>` of a real
   request: the request's detail page opens and no dialog appears.
2. Deliver an activation with an unrecognised argument (for example `action=Settings`): nothing visible
   happens; the event is recorded in the startup log.
3. Repeat both: identical outcomes, no dialog accumulation.

The toast itself cannot fire on the unpackaged build (the capability is reported as unavailable); exercise
these paths through the activation entry points, not by waiting for a toast.

## 7. Documentation check (US5)

```powershell
Select-String -Path FEATURES.md,OPEN-TASKS.md,WeekendProject\ChangeLog.Simple.md,WeekendProject\ChangeLog.md,WeekendProject\OPEN-WORK-NEXT-SPEC.md,WeekendProject\Module_Mock\Tasks.md -Pattern 'RecvMockData|manual.*toggle|Mock ON|mock mode'
```

Expected: no line presents the retired manual toggle, sample rows or mock mode as current behaviour. The
backlog index carries the retired-sources note; the module task file's completed count matches reality; each
closed defect carries its closure status line naming this spec, its follow-on owner and its test.

## 8. Definition of done

| Gate | Expected |
| --- | --- |
| Build | `0 Warning(s) 0 Error(s)` |
| Suite | `Failed: 0`, with ≥ 10 new checks, each failing against the pre-change behaviour |
| Scan gates | empty, `specs/` and `defects/` excluded |
| Manual pass | four surfaces walked on a signed-in build (or recorded as environment-gated) |
| Preserved behaviour | cached fallback, read-only indicator, retry, stored-procedure-first rule unchanged |
| Documents | ten defects carry closure lines; the changelogs and the backlog index are corrected |
