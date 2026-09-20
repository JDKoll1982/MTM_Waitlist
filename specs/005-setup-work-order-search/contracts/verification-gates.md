# Contract: verification gates

**Feature**: `005-setup-work-order-search` | **Date**: 2026-09-13 | **Plan**: [../plan.md](../plan.md)

The commands and assertions that decide whether this feature is done. A task is ticked only against one of these,
never against the belief that the code looks right.

---

## 1. Build and test

| Gate | Command | Pass condition |
|---|---|---|
| Clean solution build | `dotnet build MTM_Waitlist.sln -c Debug -p:Platform=x64 /m:1 /nodeReuse:false` | **0 warnings, 0 errors** |
| Unit suite | `dotnet test MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj -c Debug -p:Platform=x64` | No failures |
| The two standing audits | within the suite | `InlineSqlAuditTests` green (both of its tests) and `RetiredSymbolAuditTests` green |

`WMC9999` is a **real masked XAML error**, never an environment problem: when it appears, the underlying error is
surfaced by the documented technique (`.github/copilot-instructions.md` → Known Build Quirks) rather than
suppressed. `PRI175` / `PRI224` are stale-artifact or file-lock symptoms, resolved by the documented technique —
not by disabling the check.

**Command naming.** `[RelayCommand]` **strips a trailing `Async`** from a generated command: a
`private async Task FooAsync()` binds in XAML as `FooCommand`, never `FooAsyncCommand`. Binding the wrong name is
a silent XAML failure that surfaces only as `WMC9999`.

---

## 2. Database gates

| Gate | Command | Pass condition |
|---|---|---|
| Master-list staleness | `pwsh -File Database/CopilotScripts/build_mtm_mock_masters.ps1 -Check` | Exit 0 — the committed `Database/Mock/{AllTables,AllSPs,AllSeeds}.sql` match the file-per-artifact sources |
| Artifact pairing | source scan | Every added, changed or removed artifact under `Database/` has `create.sql` **and** `rollback.sql` in the same change (SC-005) |
| Description registration | review | `Database/Mock/Bootstrap/update_table_descriptions.sql` names the new table and its columns |
| Projection parity | `Database/Mock/Validation/all_shapes_roundtrip/validate.sql` extended | Shape 6 round-trips through `sp_visual_work_order_search_refresh` → `sp_visual_work_order_search_get`, projecting the live read's exact column set |
| Procedure shape is 5.7-safe | live MySQL 5.7 | The refresh loads a complete snapshot and swaps it atomically; the read caps with `LIMIT p_max_matches` (a routine parameter) and counts with a scalar subquery, **not** a window function |
| No run-time-assembled statement | `InlineSqlAuditTests` | No statement marker in application source; the raw-SQL seam allowlist is unextended |

---

## 3. Behaviour gates — must be *driven*, not sampled

| # | Scenario | Requirement | Pass condition |
|---|---|---|---|
| G-1 | A partial matching **several** orders | SC-001 | The modal is shown and lists them |
| G-2 | A partial matching **exactly one** order | SC-001, spec Edge Case | The modal is **still** shown |
| G-3 | A partial matching **nothing** | FR-010 | Plain language, **no empty list**, no silence |
| G-4 | A partial matching **more than the cap** | FR-011 | The modal is usable **and** states how many matched |
| G-5 | `76951` / `076951` / `WO-076951` | SC-002 | The next step is reached with **zero modals** |
| G-6 | `WO-076951` | FR-004 | Used **as entered** — reformatting does not change it |
| G-7 | `ABCD` | FR-022 | The existing rejection, unchanged, **no modal** |
| G-8 | Dismiss the modal | FR-009 | The entry and the flow are unchanged; a second search is possible |
| G-9 | Choose a match | FR-008 | The entry becomes that order and the flow continues as if typed |
| G-10 | `0451` | FR-005, **FR-006** | The derived pattern is `WO-0451%` — asserted exactly, and **not** `WO-000451%` |
| G-11 | The source is **unreachable**, mirror populated | SC-007 | Matches are returned from the mirror with the identical shape |
| G-12 | The source is **reachable but empty** | SC-007, FR-019 | Zero matches — **cached rows must not appear** |
| G-13 | An **unknown** source key | FR-015 | Reported unavailable, with **no data call made** |
| G-14 | A **second** source is added | SC-004 | The picker's own files are unchanged and the second caller behaves identically |
| G-15 | An edited entry while the modal is open | spec Edge Case | The modal closes and the new text searches on the next field exit |

G-10, G-11, G-12, G-13 and G-14 are the five that a "looks right" review is least likely to catch, and each maps
to a requirement the design was shaped around. They are gates, not samples.

---

## 4. Scope gates

| Gate | Pass condition |
|---|---|
| Terminology | No user-visible string introduces a new name for an existing concept, and no verbatim identifier from the work-order contract is renamed, recased or reformatted |
| Localization | Zero introduced or changed user-visible strings are unlocalized (SC-009) — every one resolves through the resource mechanism with a fallback |
| One implementation | Exactly one type maps rows to matches; no screen or service implements its own (SC-003) |
| Data integrity | No internal store is mocked; the only fallback path is the Infor Visual read, and it is automatic — there is no manual or demo mode (constitution II) |
| No new project | The solution gains no project; the capability is library code in the existing assemblies |
