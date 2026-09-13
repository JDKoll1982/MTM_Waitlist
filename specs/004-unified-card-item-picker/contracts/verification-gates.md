# Contract — Verification gates

The evidence each requirement is allowed to be closed with. A task is ticked only against one of these gates and
only with the result recorded — never speculatively (constitution I and VI).

## 1. Build

```powershell
dotnet build MTM_Waitlist.sln -c Debug -p:Platform=x64 /m:1 /nodeReuse:false
```

**Pass:** 0 warnings, 0 errors. Run from a shell that matches the running app's privilege level, with `MTM_Waitlist.exe`
not running (it locks the output and produces `PRI175` / `PRI224`, which are artifacts, not code errors).

**`WMC9999` is a real, masked XAML error**, never an environment problem: this machine's WindowsAppSDK
`XamlCompiler` is missing the satellite resource that carries its error text, so any genuine XAML compile error —
bad type, bad binding, wrong member name — surfaces only as `WMC9999`. Surface the real cause with the documented
technique (a deliberate C# error to force the file/type report, or bisecting the recently changed XAML) and fix it.
Never record it as an environment issue.

**The binding trap that produces it here:** CommunityToolkit.Mvvm's `[RelayCommand]` strips a trailing `Async`, so
`private async Task ContinueToReviewAsync()` binds as `ContinueToReviewCommand`, **not** `ContinueToReviewAsyncCommand`.
Every new command binding in this feature follows that rule.

## 2. Tests

```powershell
dotnet test MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj -c Debug -p:Platform=x64
```

**Pass:** no failures. SC-011 requires the full suite green, and requires it green *with* the new tests below, not
instead of them.

### The tests this feature must add

| Area | What it asserts | Requirement |
|---|---|---|
| Visibility matrix | for each of the **eight** job configurations, the exact set of visible Items — driven through the picker rules, not a snapshot of expected strings | FR-002, SC-004 |
| Never-empty | the Item step offers at least one Item for a job with no parts and for a work centre with no active job — and a Category that would offer none is not offered | FR-002, SC-005 |
| Merged pickup Item | a job with **only** flatstock still offers the merged pickup Item | FR-002, SC-004, D21 |
| Scrap gate | offered for a real scrap type only; **not** for `No Scrap`, **not** for `Scrap Type Required`, not when unset | FR-031 |
| Configuration mechanism | fields are read from configuration **in the declared order**, with the right value types and labels | FR-013, FR-015 |
| No frozen field list | **no test asserts today's field contents** — a change to an Item's fields must not fail a build | FR-015 |
| Missing configuration row | an Item with no row is reported unavailable, never half-configured, never a crash | FR-014 |
| Stray configuration row | a row naming an Item that does not exist is ignored and never offered | Edge Cases |
| The die's Line 2 | the die's location when the captured destination is `Home Location`; the die's number otherwise | FR-005, US2 |
| The two wrong-material Items | each reads as one Deliver request whose Line 2 names the **correct** material | FR-029 |
| Item and Category display text | resolves through the resource mechanism | FR-022 |
| Observed average | completed requests only; acceptance to completion; a released-then-completed request contributes once; no value when nothing has completed | FR-019 |
| Default allotment | an Item with no configured minutes uses **15 minutes**, is labelled as a default, and keeps its place in the urgency order | FR-017, SC-007 |
| Sort preference | each of the five options orders the list; the default is most urgent; the choice survives a restart | FR-010, FR-011, SC-009 |
| Sort versus overdue | overdue rows stay visibly marked under every sort | FR-012 |
| Retirements | `RetiredSymbolAuditTests` extended to fail the build if any retired symbol returns | FR-023, SC-012 |
| No inline SQL | `InlineSqlAuditTests` stays green with the three new procedures in place | FR-024 |
| The spreadsheet is not shipped | a new audit fails the build if any source file, project file or build target references `Request-Config-Template.csv` or `unified-item-picker-workflows.md` | FR-027 |
| Seed round-trip | the seeded `subordinate_parts_json` deserializes through `ActiveJobItemResolverService`'s own deserializer into `SetupSubordinatePart` with the normalised Category | SC-004 |

## 3. Live database validation

The new and changed schema artifacts are validated against a live local `mtm_waitlist` (constitution III): every
paired `create.sql` / `rollback.sql` applies and reverses cleanly, the three new procedures return the documented
shapes, and the seeds load.

**The reinstall is the owner's action, never the agent's.** The owner runs `Database/install_local_database.vbs`.
No migration code is written (the database is reinstalled from seed), and the absence of a migration is a recorded
assumption rather than an omission.

**Then the master lists and descriptions must agree with the files on disk:** `Database/Tables/AllTables.sql`,
`Database/StoredProcedures/AllSPs.sql`, `Database/Seeds/AllSeeds.sql` and
`Database/Bootstrap/update_table_descriptions.sql`.

## 4. Scripted UI verification

The running app is verified with scripted UI automation, as the previous specification did. This replaces the
requirement to verify with the in-app inspector, which is **not wired in this repository** — and it is stronger
evidence, because it drives the same artifact a user runs.

The `specs/003` traps carry over and are not optional:

- both connection overrides are needed (`MTM_WAITLIST_DB_CONNECTION_STRING` **and**
  `MTM_WAITLIST_STARTUP_DB_CONNECTION_STRING`), or the app stops at the connection dialog and nothing is tested;
- **never locate the window by title** — the splash window and the sign-in window report different titles, so a
  title lookup is wrong for some states and not others, which is worse than always wrong;
- **re-read a control's bounding rectangle in the same command that clicks it** — a rectangle read before the window
  was raised or maximized is stale, and a real click at stale coordinates lands on whatever is there now;
- close the app when finished, so no orphan process holds database connections.

**What the UI pass must prove:** a request can be raised by choosing a Category and an Item in one pass without ever
being asked for a type or a subtype (SC-001); requests for several different Items each draw the **same** card shape
(SC-003, US2); the sort control changes the order and its choice returns after a restart (SC-009); and the two
Settings screens show and save per-Item values (US5).

**The real path is proven once.** Walk one work centre through the actual Setup flow — against the mock mirror,
since `172.16.1.104` is unreachable from this workstation — and confirm the picker filters from what Setup wrote.
This is what makes the eight-configuration matrix a proof about the real path rather than about a seed shape.

## 5. Requirement-to-gate map

| Requirement | Gate |
|---|---|
| FR-001, FR-002, FR-003 | Unit: visibility matrix, never-empty, merged pickup Item · UI: raise a request in the new vocabulary |
| FR-004, FR-023, FR-025 | Live DB: the request's two columns and the retired artifacts · `RetiredSymbolAuditTests` · master lists agree |
| FR-005, FR-006, FR-007, FR-008, FR-009 | Unit: Line 1, Line 2, the die rule, the four metadata rows, picture resolution · UI: one card shape across Items |
| FR-010, FR-011, FR-012 | Unit: the five orders and the overdue invariant · UI: order changes and the choice survives a restart |
| FR-013, FR-015, FR-027 | Unit: the configuration mechanism, the no-frozen-field-list rule, the spreadsheet audit |
| FR-014, FR-026 | Unit: missing row, stray row, unparsable payload, unresolvable token — each reported, none silent |
| FR-016, FR-017, FR-018, FR-019 | Unit: the deadline derives from the Item's minutes; the 15-minute labelled default; configured and observed are distinct; the average's population and endpoints |
| FR-020 | Unit: both screens still gate on **their own** existing role check — `CanManageUrgencySettings` for the minutes editor, `CanManageImageLocationSettings` for the picture screen — and neither is collapsed into one nor replaced by a new gate |
| FR-021 | Unit: the picture resolution order and the "nothing configured never overwrites" rule |
| FR-022 | Unit: display text resolves from the resource mechanism · and no unlocalized string is introduced (SC-010) |
| FR-024 | `InlineSqlAuditTests` green with the three new procedures |
| FR-028 | Unit: the four out-of-scope Items are catalogued and never offered |
| FR-029 | Unit: each wrong-material Item is one Deliver request naming the correct material |
| FR-030, FR-031 | Unit: the scrap value comes from the job and is never asked for; the three-way decision gate |
| FR-032 | Live DB: `sp_waitlist_request_status_update` untouched · Unit: the existing lifecycle tests stay green |

## 6. Documentation in the same change

Changes that retire a data store or an integration pattern update the affected documentation in the same change
(constitution, *Documentation & Extensibility*), including removing stale references:

- `specs/003-waitlist-handler-fulfilment` — the card-anatomy constraint and its matching test rewritten to the new
  anatomy as a **recorded supersession** (not deleted, so an unintended change still fails), and its FR-015 amended
  from "MUST be ordered most-urgent-first" to "defaults to most-urgent-first";
- `WeekendProject/*`, `README.md`, `CHANGELOG.md` — the retired type/subtype vocabulary removed;
- SC-012: **zero** references to the retired vocabulary remain in application code, database artifacts or project
  documentation.
