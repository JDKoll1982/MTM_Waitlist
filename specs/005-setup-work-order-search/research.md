# Phase 0 Research: Shared Fuzzy Match Picker and Work Order Search

**Feature**: `005-setup-work-order-search` | **Date**: 2026-09-13 | **Spec**: [spec.md](spec.md)

This is where the design choices the spec deliberately left open are settled, with the alternative that was
rejected and why. Everything below is either grounded in a source read for this plan or in the repository's
existing pattern.

---

## R1 — Where the shared capability lives

**Decision.** The capability's contracts, its models and its **one implementation** go in `MTM_Waitlist.Core`,
the existing home for cross-module service contracts (`IMySqlHelperServer`, `ISearchPickerService` and friends
sit beside them). Nothing in `Core` references the picker's sources. The **one shipped reader kind** —
`VisualShapeSearchSourceReader` — goes in `MTM_Waitlist.Mock`, beside `VisualReadShapeCatalog`, which is what it
resolves against. The **caller's** source declaration, its dialogue seam and its view-model wiring go in
`MTM_Waitlist.Setup`. Only the modal view goes in the composition root (`Module_Shared/Views`), because a WinUI 3
`ContentDialog` needs the live `XamlRoot`.

**Rationale.** The dependency direction is forced, not chosen: `MTM_Waitlist.Mock` already depends on `Core` (for
`IMySqlHelperServer`), so a contract in `Core` can be implemented by a reader in `Mock` — but a contract in `Mock`
could not be implemented by a caller in `Setup` without an inverted reference. Putting the contracts in `Core` is
also what makes the capability usable by a *second* module without that module taking a dependency on `Mock`.

**Alternatives considered.**

- **A new `MTM_Waitlist.Search` project.** Rejected: one new project for three contracts and one service. It would
  need a `Complexity Tracking` entry and adds a build edge for no isolation the existing assembly boundaries do
  not already give. `MTM_Waitlist.Core` is already the cross-module contract home.
- **All of it in `MTM_Waitlist.Mock`.** Rejected: the picker would then only ever be usable by code that also
  takes the Mock dependency, and an internal-store source could never be registered without pulling in the
  Visual fallback library.
- **All of it in the app (composition root).** Rejected: the app cannot be unit-tested by `MTM_Waitlist.Tests`,
  which references the `MTM_Waitlist.*` libraries directly. The picker's behaviour is exactly what SC-003 and
  SC-004 need proven, so it has to live where the test project can reach it.

---

## R2 — The mechanism for "any caller-described source", and why it does not violate FR-018

**Decision.** A **closed catalog of registered sources**, plus a **registry of reader kinds**. A source is
declared once, as data, by the code that owns it:

| Field | Meaning | Shipped value (work orders) |
|---|---|---|
| `Key` | Stable identifier the request names the source by | `work_orders` |
| `ReadKind` | Which registered reader kind serves this source | `VisualReadShape` |
| `ReadTarget` | What that kind resolves — for the shipped kind, the **catalog shape key** | `work_order_search` |
| `ValueColumn` | The column whose value is the match | `WorkOrder` |
| `DisplayColumns` | Ordered columns shown so near-identical matches can be told apart | `PartNumber`, `Description`, `WorkCenter` |
| `DefaultMaxMatches` / `MaxMatchesCeiling` | The source's own default and hard ceiling | `25` / `200` |

`SearchPickerService` receives a request carrying only `SourceKey`, `Pattern` and `MaxMatches`. It resolves
`SourceKey` through `ISearchableSourceCatalog`; if it is not there it returns **`UnknownSource` without touching
a data path** (FR-015). It then resolves `ReadKind` through the reader registry and asks the reader for rows.
Neither `ReadKind` nor `ReadTarget` is ever accepted from a request.

**Rationale — why this is FR-018-safe, precisely.** FR-018 forbids a run-time-assembled statement *and* a
statement selected by a supplied list name. Here the caller supplies a **key**, which is looked up in a closed
registry, and the thing that reaches the data layer is a value already written into our own source code: for the
shipped kind it is a shape key that `VisualReadShapeCatalog` must contain, and `VisualReadShape` derives the
procedure name from it (`sp_visual_<key>_get`) exactly as the existing five shapes do. The `Pattern` and
`MaxMatches` travel as **command parameters** — `AddWithValue` on the live side, ordinary procedure arguments on
the mirror side — never as name fragments. There is no `EXECUTE`, no `PREPARE`, no `CONCAT` into a statement, and
no string interpolation of anything a caller controls. A caller cannot name a table, cannot name a procedure, and
cannot reach a procedure that is not already an approved artifact of a registered source.

**Alternatives considered.**

- **A procedure that takes a table or column name and builds the query.** This is precisely the reading FR-018
  bans, and `InlineSqlAuditTests` scans for the statement markers such a procedure would be assembled from.
  Rejected.
- **Putting the procedure name in the request** (`SearchRequest.ProcedureName`). Simpler by one type, and
  rejected: it moves the choice of executed statement into caller-supplied data, which is the property FR-018
  exists to prevent. The registration is the right home for it.
- **One reader interface implemented per source.** Rejected: the orchestration (map, trim, count, classify) would
  then be re-implemented per source or passed around as a default-method, and SC-003's "one implementation of
  matching exists" would be satisfied only in letter. A reader kind that only *fetches rows for a declared
  target* keeps the orchestration in one place.
- **A single hardcoded reader with no registry.** Rejected: adding a source of a different kind (an internal
  store's own procedure) would then require editing the picker, which is what FR-014 forbids. The registry is
  three lines and is the difference between additive and invasive.

---

## R3 — The existing Levenshtein helper: not reused, not extracted

**Decision.** Matching is the **source query's `LIKE`**, applied in the store. `FindBestScrapTypeMatch` /
`NormalizeForFuzzy` / `ComputeLevenshteinDistance` in `MTM_Waitlist.Setup/Services/SetupWorkflowService.cs` stay
exactly where they are, untouched, and the picker does not reference them.

**Rationale.** They answer a different question. `FindBestScrapTypeMatch` takes one source string and a small
in-memory candidate list and returns **the single closest** candidate plus its edit distance; it ranks by
similarity, so `WO-1451` would outrank the `WO-0451xx` orders the person is actually typing towards. The picker
needs **every** value beginning with what was typed, with a stable order — an auto-completion, which is what the
owner's worked example (`WO-0451%%`) is. Reusing it would also mean pulling the *whole* work-order list into
memory to rank it, which is the unbounded list the store-side cap exists to avoid. And leaving it in place
protects SC-003: extracting it would put a second, differently-purposed matcher in the codebase under a name
that reads like the shared one.

**Alternatives considered.** Extract a shared fuzzy matcher used by both the scrap picker and the search picker —
rejected as a rename that makes two different algorithms look like one, and as a refactor of a working,
unrelated workflow that this feature has no requirement to touch.

---

## R4 — Where the work-order text rules live

**Decision.** The classification ("this is a partial", "this cannot be a work order") and the derivation of the
search pattern go into **`WorkOrderValidationService`**, as new members beside the existing `TryNormalize`.
`TryNormalize` itself is **not changed**: complete forms still normalise to `WO-` + six padded digits, and the
search pattern is produced by a *separate* method so the padding rule cannot leak into it (FR-006).

**Rationale.** Three requirements point the same way. FR-012 forbids the entry step holding its own matching
logic. FR-013 says the capability must not know what a work order is, so the pattern cannot be derived there.
And the only place that must agree about "what a work order looks like" is the service that already owns that
rule — splitting the partial rule into a second file would give the repository two definitions of the same
grammar. Two methods on one service also means a unit test can assert the trap directly: `0451` ⇒ `WO-0451%`, and
**not** `WO-000451%`.

**Alternatives considered.** Deriving the pattern in `SetupWorkOrderViewModel` (rejected: FR-012, and it would
put the padding rule in a second place); deriving it in `SearchPickerService` (rejected: FR-013 — the picker would
then have to know the `WO-` prefix and the digit rule); a new `WorkOrderSearchService` owning it (rejected: a
second home for work-order text rules, with no boundary it protects that the validation service does not).

---

## R5 — A new read shape, rather than a second read procedure on the existing one

**Decision.** Add **shape 6**, `work_order_search`, with its own live script, population script, mirror table,
stage twin, `_get` and `_refresh` procedures, and one catalog entry. Do not add a second read procedure to the
existing `work_order_lookup` shape.

**Rationale.** The shape's derived-artifact-name contract is load-bearing: `VisualReadShape` derives
`MirrorTableName`, `StageTableName`, `GetProcedureName` and `RefreshProcedureName` from `Key`, and the service's
startup validation plus the refresh engine rely on those names existing. A second `_get` on one shape would break
that derivation for one shape only, and would give `work_order_lookup` two different contracts (a full-key read
and a pattern read) under one key. The repo's own playbook states the alternative plainly — "adding a sixth shape
adds one entry [to the catalog]; no existing shape's artifact, procedure signature, contract, or result type
changes". Following it means this feature **adds and changes no existing database artifact**: SC-005 and FR-020
are satisfied by addition alone.

**Cost, stated honestly.** The mirror now holds the addressable work orders twice, once keyed by exact
`normalized_work_order` for the lookup and once for the pattern search. That is real duplication, and it is the
same duplication the existing five shapes already accept (the lookup mirror and the subordinate-parts mirror both
carry work-order rows). It is the price of the derived-name invariant.

**Alternatives considered.**

- **A second `_get` on the existing shape** (e.g. `sp_visual_work_order_lookup_search`). Rejected above; it would
  also need a `Complexity Tracking` entry justifying the invariant break, for a saving of one table.
- **Reusing `visual_work_order_lookup_result` with a new procedure.** Rejected for the same reason — it needs a
  procedure name the catalog does not derive.

---

## R6 — Capping and the match count are computed in the stores

**Decision.** The read returns at most the caller's cap **and** the total number of matches, as one projection.
Live (SQL Server): `SELECT TOP (@MaxMatches) …, COUNT(*) OVER () AS TotalMatched`. Mirror (MySQL 5.7):
`… (SELECT COUNT(*) FROM visual_work_order_search_result WHERE normalized_work_order LIKE p_pattern) AS TotalMatched
… LIMIT p_max_matches`. Both project the identical column set, in the identical order, which is what keeps the
cached answer structurally indistinguishable from the live one (FR-019).

**Rationale.** FR-011 requires the modal to "state how many matched" while staying usable, and FR-016 makes the
cap the caller's to state. Computing both in one read keeps that to a single round trip and keeps the *count*
honest — a count taken from the capped rows would understate the matches and turn "showing 25 of 400" into
"25 matched", which is a false answer of the same family FR-019 forbids.

`LIMIT` with an integer routine parameter is documented MySQL behaviour ("Within stored programs, `LIMIT`
parameters can be specified using integer-valued routine parameters or local variables" — *MySQL Reference
Manual*, SELECT Statement; the fetch resolved to the current revision, and the rule is unchanged since 5.7).
Window functions are the reason the two sides differ: MySQL 5.7 has no `WINDOW` clause, so the mirror's count is
a scalar subquery. The 5.7 constraint is corroborated in-repo — `sp_visual_work_order_lookup_refresh` already
builds a numeric tally for its JSON expansion because 5.7 lacks the richer set operators.

**Alternatives considered.** A second count round trip (rejected: doubles the cost of every search to answer a
label, and can disagree with the page it labels); `SQL_CALC_FOUND_ROWS` + `FOUND_ROWS()` (rejected: the manual
marks both deprecated and "expected to be removed"); a window function on the mirror side (rejected: not
available in 5.7); fetching `cap + 1` rows and saying "more than N" (rejected: FR-011 asks for how many matched,
and "more than 25" is a dodge).

---

## R7 — The pattern is the caller's, verbatim

**Decision.** `SearchRequest.Pattern` is the **ready-to-use** pattern string — for the work-order caller,
`WO-0451%`. The capability does not append a wildcard and does not escape anything; the source's declaration
documents which wildcards its read understands.

**Rationale.** FR-016 makes "what pattern to search with" the caller's to state, and the owner's worked example is
a pattern the caller forms. It also keeps the capability free of any query-language assumption, which is what lets
a future source state a different pattern form without the picker changing. The owner's `%%` is noted and
harmless: the spec records that a doubled `%` denotes the same pattern as a single one, so the derived pattern is
`WO-0451` followed by one trailing wildcard.

**Consequence, recorded rather than hidden.** `%` and `_` are wildcards in both engines' `LIKE`, so a pattern
carrying a literal `_` would over-match. It cannot arise for the shipped source — a partial is digits-only by
definition — but the responsibility for escaping belongs to whoever writes a pattern, and the source contract
says so rather than leaving it to be discovered.

**Alternatives considered.** The capability appends the trailing wildcard (rejected: it would have to know the
query language, and FR-016 hands the pattern form to the caller); the capability escapes `%`/`_` unconditionally
(rejected: it would silently break a caller who *wants* a wildcard, and it would still need the query language).

---

## R8 — The modal is a `ContentDialog` behind an injectable seam

**Decision.** `SearchPickerDialog`, a `Microsoft.UI.Xaml.Controls.ContentDialog` in `Module_Shared/Views`, raised
through a new `ISearchPickerDialogService` implemented app-side by `SearchPickerDialogService` — the same
module-contract/app-implementation split the Setup module already uses for `ISetupDialogService`. The view model
depends only on the interface, so the gates run headless.

**Rationale, grounded.** The WinUI 3 rules that matter here were checked rather than recalled (Microsoft Learn,
*Dialog controls*, and the `ContentDialog` API reference):

- `XamlRoot` **must** be set on the dialog before `ShowAsync`; in WinUI 3 an unset `XamlRoot` throws at runtime.
  The app-side service therefore reads it from the live window's content element, exactly as the existing
  `SetupDialogService` does, and returns "nothing picked" when there is no root (a headless host).
- Only **one `ContentDialog` may be open per thread at a time**, and opening a second throws. The field-exit path
  is user-driven and the page already re-focuses the text box on status changes, so the show is guarded rather
  than assumed to be alone — which is also how FR-009's "must not trap the person" is satisfied in practice.
- `PrimaryButtonText`/`CloseButtonText` and `ContentDialogResult` are the supported way to express "chosen" and
  "dismissed"; the picker sets primary/close text from the resource mechanism (FR-021).

**Alternatives considered.** A `Flyout` or `Popup` anchored to the text box (rejected: the spec asks for a modal
that the person dismisses, and a flyout's light-dismiss interaction is not the same contract as FR-009's
"dismissing leaves the entry unchanged"); an in-page overlay panel on the entry page (rejected: it would put
picker presentation in the entry step that FR-012 says must hold no matching logic, and it would not be reusable
by the second caller US3 describes).

---

## R9 — FR-017 tolerance, and where case-insensitivity comes from

**Decision.** A **partial** is what remains after trimming whitespace, removing a leading `WO`/`wo`, and removing
an optional `-`: one to four digits. The derived pattern is `WO-` + those digits + `%`. A five- or six-digit entry
is a complete form and is normalised by the existing rule. Seven or more digits, or any non-digit, is not a
partial and runs no search (FR-022). Case-insensitivity is left to the stores' existing case-insensitive
collations on both sides; `UPPER()` is not applied.

**Rationale.** FR-017 asks that matching "tolerate the separators a person actually types", and the separator
people actually type is the `WO-` prefix, in whatever case. Widening *what starts a search* does not touch FR-004:
the accepted **complete** input forms and the formatting rule are unchanged, and a complete form is still
classified before any search happens. Not wrapping the column in `UPPER()` is both correct (the mirror is
`utf8mb4_unicode_ci` and the Infor Visual side is case-insensitive for these values) and necessary — a function
around the column would defeat the index the mirror's key column carries.

**Alternatives considered.** Deriving the pattern with the existing `^(?:WO-)?(\d{5,6})$` regex and treating
anything it rejects as "not a partial" — rejected outright, because it would reject the owner's own `0451` and
`045`, which the spec's Edge Cases require to search. (This is the trap the spec's checklist flags: a design that
pads the partial derives `WO-000451` and fails the worked example.)

---

## R10 — Match ordering

**Decision.** The store orders by the value column, then by the declared display columns, so two identical
searches against an unchanged snapshot return matches in the same order. The spec leaves ordering to the plan and
requires only stability.

**Rationale.** Deterministic ordering makes the unit and roundtrip assertions meaningful, and the value column
first puts the person's own number at the top of a prefix match. No relevance ranking is introduced — ranking by
similarity is the Levenshtein behaviour R3 rejects.

---

## R11 — No migration; the database is reinstalled from seed

**Decision.** This feature ships schema artifacts and no migration code. The new mirror table's baseline rows
join the existing `seed_visual_mirror_baseline` artifact, and the owner reinstalls; the agent does not.

**Rationale.** The spec records this as the repository's existing position, and it is what the ruleset's artifact
convention already assumes. It is also why the artifact set below is exactly: one table and its stage twin, one
pair of procedures, one live script and one population script — plus the generated `mtm_mock` master lists.

---

## R12 — The `mtm_mock` master lists are regenerated, not hand-edited

**Decision.** `Database/Mock/AllTables.sql`, `AllSPs.sql` and `AllSeeds.sql` are produced by
`Database/CopilotScripts/build_mtm_mock_masters.ps1`, which globs the file-per-artifact directories in ordinal
name order. The change runs the generator and keeps `-Check` green; it does not hand-edit the aggregates.

**Rationale.** This is the `mtm_mock`-only deviation the ruleset records and approves. Because the generator is
directory-driven, adding the two new table directories and the two new procedure directories is all that is
needed — and `-Check` is a real staleness gate rather than a convention someone must remember.

---

## Open points carried to `plan.md` / `contracts/`

1. **The modal for a complete form that does not resolve.** The spec's Edge Case says a complete form that
   formats correctly but resolves to nothing tells the person, "and the search is available as the way forward",
   while FR-002 permits the modal only when the entry is *not a complete, resolvable* work order. The two are
   consistent if "the search is available" means the modal is permitted here too. This plan takes that reading —
   the modal is raised, using the exact `WO-######%` pattern — and records it here rather than in the spec, so the
   owner can flip it to "the entry stays editable and the person shortens it themselves" without a spec change.
2. **The searched list's identity.** The spec's Assumptions say "the searched list is the list the step already
   validates against". Read literally, that is `work_order_lookup` — but that shape is keyed by an exact
   normalised work order and cannot answer a prefix. The list is therefore the **same Infor Visual addressable
   work orders**, read through a *new* shape whose population predicate reuses the existing one's addressable-key
   rule verbatim (`STATUS IN ('R','U','F')` and `BASE_ID` literally `WO-` + six digits). No second source of truth
   is introduced; the source of truth is Infor Visual, and the addressable-set rule is copied, not reinvented.
