# Contract: the shared search capability

**Feature**: `005-setup-work-order-search` | **Date**: 2026-09-13 | **Plan**: [../plan.md](../plan.md)

This is the interface a **consumer** codes against and a **test** asserts on. Identifiers are exact; nothing here
may be renamed, recased or pluralised.

---

## 1. Surfaces

| Surface | Namespace / project | Purpose |
|---|---|---|
| `ISearchPickerService` | `MTM_Waitlist.Module_Core.Contracts.Services` (`MTM_Waitlist.Core`) | The capability's only public entry point. |
| `ISearchableSourceCatalog` | `MTM_Waitlist.Module_Core.Contracts.Services` | Resolves a source key to its declaration. Unknown key ⇒ not found. |
| `ISearchSourceReader` | `MTM_Waitlist.Module_Core.Contracts.Services` | The fetch seam behind one reader kind. Fetches rows for one declared source. |
| `ISearchSourceReaderRegistry` | `MTM_Waitlist.Module_Core.Contracts.Services` | Resolves a `SearchReadKind` to its reader. |
| `SearchSource`, `SearchReadKind`, `SearchRequest`, `SearchMatch`, `SearchResult`, `SearchOutcome` | `MTM_Waitlist.Module_Core.Models` | The data shapes. |
| `SearchPickerService`, `SearchableSourceCatalog` | `MTM_Waitlist.Module_Core.Services` | The one implementation. |

### 1.1 `ISearchPickerService`

```csharp
Task<SearchResult> SearchAsync(SearchRequest request, CancellationToken cancellationToken = default);
```

The **only** search implementation in the codebase. A screen that needs "type part of it, pick from the matches"
calls this; a screen that writes its own matching is the thing SC-003 forbids.

### 1.2 `ISearchSourceReader`

```csharp
SearchReadKind Kind { get; }
Task<SearchSourceRead> ReadAsync(SearchSource source, string pattern, int maxMatches, CancellationToken cancellationToken = default);
```

A reader **fetches rows**. It does not rank, trim, count, classify, or decide what a no-match means — that is the
picker's, so that two sources cannot behave differently. `SearchSourceRead` carries the rows plus `TotalMatched`.

---

## 2. Identifier contract

These strings are values the implementation must match exactly. They are the identifiers a test or a caller types.

| Identifier | Exact value | Where |
|---|---|---|
| The shipped source key | `work_orders` | `SearchSource.Key` |
| The shipped reader kind | `VisualReadShape` | `SearchReadKind`, `ISearchSourceReader.Kind` |
| The shipped read target | `work_order_search` | `SearchSource.ReadTarget` = a `VisualReadShapeCatalog` key |
| The shipped procedure names | `sp_visual_work_order_search_get`, `sp_visual_work_order_search_refresh` | derived from the shape key by `VisualReadShape` |
| The mirror table | `visual_work_order_search_result` | derived: `visual_<key>_result` |
| The stage twin | `visual_work_order_search_result_stage` | derived: `visual_<key>_result_stage` |
| The live query script | `Database/InforVisual/Queues/Module_Setup/Queries/SearchWorkOrders.sql` | the shape's `SourceScriptRelativePath` |
| The population script | `Database/InforVisual/Queues/Module_Mock/Populations/work_order_search_population.sql` | the shape's `PopulationScriptRelativePath` |
| The read projection | `WorkOrder`, `PartNumber`, `Description`, `WorkCenter`, `TotalMatched` | in this order, on both sides |

---

## 3. Behaviour contract — one row per requirement

| Requirement | The observable guarantee | How it is asserted |
|---|---|---|
| FR-012 | The entry step contains no work-order matching logic; every match comes from this capability. | A source scan of `SetupWorkOrderViewModel` for matching terms, plus the picker unit tests |
| FR-013 | The capability takes a caller-described source and a caller-supplied value, and holds no knowledge of what the values mean. | `SearchPickerService` names no work-order term; a test source that is not a work order returns matches unchanged |
| FR-014 | Adding a source is additive — a declaration plus its own artifacts. **No file under `MTM_Waitlist.Core` is edited.** | SC-004: the second source is added in a test and the picker's own files are asserted unchanged |
| FR-015 | An unknown `SourceKey` yields `UnknownSource` with **no data call made**. | A unit test with a recording reader asserts zero reads |
| FR-016 | Trigger, pattern and cap are the caller's; the capability fixes none of them. | `SearchRequest` carries `Pattern` and `MaxMatches`; `DefaultMaxMatches`/`MaxMatchesCeiling` are the source's, and an over-ceiling request is **clamped** |
| FR-017 | Matching is case-insensitive and tolerates the separators a person types. | Case-insensitivity from the stores' collations (no `UPPER()` on the column); the separator tolerance is the caller-side derivation, asserted in the work-order contract |
| FR-018 | No inline statement text; no statement assembled at run time from a supplied name. | `InlineSqlAuditTests.NoMySqlStatementTextRemainsInApplicationCode` and `RawSqlSeamIsConfinedToItsReviewedAllowlist` stay green; `ReadTarget` comes from the declaration, never from the request |
| FR-019 | A source that lives outside our own stores is read live first, and only **genuine unreachability** is served from the mirror with an identical shape. A reachable-but-empty result is a real answer. | `IVisualReadFallback` is the only path; the roundtrip validation and the shape's fallback tests assert the three outcomes separately (SC-007) |
| FR-020 | Every artifact ships paired `create.sql` / `rollback.sql`; the master lists agree with disk. | `build_mtm_mock_masters.ps1 -Check` is the staleness gate (SC-005) |
| SC-003 | One implementation of matching exists; zero screens implement their own. | The picker is the only type that maps rows to matches; a source scan for a second |

---

## 4. Extension playbook — adding a searchable source

The ordered steps a contributor follows. This is the contract for "any caller-described source", and it is
deliberately mechanical.

1. **Decide the source's identity.** A stable lowercase snake_case `Key`, e.g. `dunnage_parts`. It is the only
   thing a request will name.
2. **Decide the value and the display.** The `ValueColumn` (what the caller acts on) and the `DisplayColumns`
   (enough to choose between near-identical values).
3. **Provide the read.**
   - *An external (Infor Visual) list* — add a **new shape** to `VisualReadShapeCatalog` and its five artifacts:
     the live script, the population script, the mirror table and stage twin, and the `_get` / `_refresh`
     procedure pair. Then implement one `IVisualReadFallback<TRequest,TRow>` for it, exactly as the five existing
     shapes do. **No existing shape's artifact, signature, contract or result type changes.**
   - *An internal store* — add one stored procedure and register a reader that calls it through
     `IMySqlHelperServer.ExecuteStoredProcedureQueryAsync`. (Not shipped by this feature; recorded so the path is
     not invented later.)
4. **Declare the source** — one `SearchSource` record, with `ReadTarget` naming the artifact from step 3.
5. **Register it** — one line in the owning module's DI extension, alongside the reader it needs. Which module
   registers it is the caller's business; `MTM_Waitlist.Core` is not edited.
6. **Prove it** — a test that the picker's own files are unchanged and that a search through the new key returns
   the new source's rows.

**What the capability never accepts.** A table name, a column name, a procedure name, a statement, or a
"where clause". Step 4's `ReadTarget` is written in *our* code as part of a declaration that ships through review;
it is not request data. That is the whole of the FR-018 answer, and it is why this contract can honestly claim
"read any table the caller needs" while carrying no run-time-assembled SQL.
