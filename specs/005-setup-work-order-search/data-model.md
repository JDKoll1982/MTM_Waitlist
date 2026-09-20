# Phase 1 Data Model: Shared Fuzzy Match Picker and Work Order Search

**Feature**: `005-setup-work-order-search` | **Date**: 2026-09-13 | **Plan**: [plan.md](plan.md)

Nothing here is persisted by the application. The capability's types are in-memory shapes on the path from a typed
value to a chosen one; the only stored entity is the new **read mirror** in `mtm_mock`, which is a cache of an
external system and never a system of record.

---

## 1. The capability's entities

### 1.1 `SearchSource` — a caller-described source *(Key Entity: "Searchable source")*

The capability's **unit of extension**. Declared once, in code, by whoever owns the list.

| Field | Type | Required | Meaning | Constraint |
|---|---|---|---|---|
| `Key` | string | yes | The identifier a request names the source by. | Stable, lowercase snake_case. Unique in the catalog; a duplicate is a registration error surfaced at startup, not a silent winner. |
| `ReadKind` | `SearchReadKind` | yes | Which registered reader kind serves this source. | Must be a registered kind. An unregistered kind is a registration error, not an "no results" answer. |
| `ReadTarget` | string | yes | What that kind resolves to. For `VisualReadShape`, the **catalog shape key**. | Declared here, never taken from a request (FR-018). Must resolve, or the read fails loudly rather than returning nothing. |
| `ValueColumn` | string | yes | The projection column whose value is the match. | Must be one of the read's projected columns. |
| `DisplayColumns` | ordered list of string | yes | The columns shown so near-identical matches can be told apart. | Non-empty (FR-007); every name must be a projected column. |
| `DefaultMaxMatches` | int | yes | The cap used when a request does not state one. | `≥ 1`; `≤ MaxMatchesCeiling`. |
| `MaxMatchesCeiling` | int | yes | The largest cap a request may ask for. | `≥ DefaultMaxMatches`. A request above the ceiling is clamped, not rejected — the person asked for matches, not for a specific page size. |

**Relationship**: one source → one reader kind → one `ReadTarget`. Many sources may share one `ReadTarget` only if
the projection is identical; the shipped set has exactly one source.

### 1.2 `SearchReadKind` — the reader registry key

| Value | Shipped | Resolves to |
|---|---|---|
| `VisualReadShape` | **yes** | A `VisualReadShapeCatalog` entry by `ReadTarget`, read through that shape's `IVisualReadFallback<TRequest,TRow>` — live first, mirror only on unreachability (FR-019). |

A second value (an internal store's own procedure, read through `IMySqlHelperServer`) is the documented extension
point and is **deliberately not shipped** — no caller needs it yet, and shipping it would be a speculative second
use.

### 1.3 `SearchRequest` — what a caller asks for *(Key Entity: "Match request")*

| Field | Type | Required | Meaning |
|---|---|---|---|
| `SourceKey` | string | yes | The source to search. An unknown key is reported, never searched (FR-015). |
| `Pattern` | string | yes | The **ready-to-use** pattern, wildcards included. Caller-owned (FR-016); the capability neither appends nor escapes. |
| `MaxMatches` | int? | no | How many matches to ask for. Absent ⇒ the source's `DefaultMaxMatches`. Above the source's ceiling it is clamped. |

*Trigger* — what makes a search happen — is deliberately **not** on the request. The caller decides when; the
capability has no opinion (FR-016).

### 1.4 `SearchMatch` — one row the caller can act on *(Key Entity: "Match")*

| Field | Type | Required | Meaning |
|---|---|---|---|
| `SourceKey` | string | yes | Which source answered. |
| `Value` | string | yes | The value that matched — the thing the caller will act on. |
| `Display` | ordered list of string | yes | The display columns in the source's declared order, positional against `DisplayColumns`. |

Rows whose `Value` is blank are dropped before the result is built, so a match the caller cannot act on never
reaches the modal.

### 1.5 `SearchResult` — what comes back

| Field | Type | Meaning |
|---|---|---|
| `Outcome` | `SearchOutcome` | Which of the four situations occurred (below). |
| `Matches` | ordered list of `SearchMatch` | Populated only for `Matched`; empty otherwise. |
| `TotalMatched` | int | How many matched in the source, **before** the cap (FR-011). `0` unless `Matched`. |
| `IsCapped` | bool | True when `TotalMatched > Matches.Count` — the modal says how many matched rather than implying it showed them all. |
| `Message` | string | The localized text for the non-`Matched` outcomes (FR-010). Localized through the resource mechanism with a fallback (FR-021). |

### 1.6 `SearchOutcome` — the closed set of answers

| Value | When | What the caller shows | Requirement |
|---|---|---|---|
| `Matched` | The read returned one or more rows. | The modal, listing the matches. | FR-001, FR-007, FR-008 |
| `NoMatches` | The read **succeeded** and returned nothing. | Plain language with a way to try again — never an empty list, never silence. | FR-010 |
| `UnknownSource` | `SourceKey` is not in the catalog. **No data path is touched.** | Unavailable. | FR-015 |
| `Unavailable` | The source could not be read (unreachable with no mirror configured, or a read fault). | The existing per-screen unavailable state with a manual retry — **never** "no matches". | FR-019, and spec 001 FR-021's per-screen unavailable state |

**State transitions.** `Unavailable` and `NoMatches` are distinct by construction: the first means "we could not
ask", the second means "we asked and the answer is none". Collapsing either into the other is the false answer
FR-019 forbids, and the existing `VisualReadFallback` is what keeps them apart — a successful live read returns
its rows **including an empty set**, and only `Unreachable` reaches the mirror.

---

## 2. The work-order caller's entities

### 2.1 `WorkOrderEntryClassification` — the load-bearing distinction *(Key Entity: "Partial entry")*

| Value | Rule (after trimming, then removing a leading `WO`/`wo` and an optional `-`) | Consequence |
|---|---|---|
| `Complete` | 5 or 6 digits | Normalise by the **existing unchanged rule** to `WO-` + six padded digits. **No search, no modal.** |
| `Partial` | 1–4 digits | Derive the pattern `WO-` + the digits **as typed** + `%`, then search. **Not padded** (FR-006). |
| `Invalid` | anything else (7+ digits, or any non-digit) | The existing plain-language rejection (FR-022). **No search.** |

The boundary is a property of the spec's Edge Cases, each of which lands in exactly one row:

| Input | Classification | Pattern / result |
|---|---|---|
| `0451` | Partial | `WO-0451%` — **not** `WO-000451%` |
| `045` | Partial | `WO-045%` (usually matches nothing, reported plainly) |
| `76951` | Complete | `WO-076951`, used directly |
| `076951` | Complete | `WO-076951`, used directly |
| `WO-076951` | Complete | used **as entered** — reformatting must not change it |
| `ABCD` | Invalid | the existing rejection, no modal |

### 2.2 The work-order search source

| Field | Value |
|---|---|
| `Key` | `work_orders` |
| `ReadKind` | `VisualReadShape` |
| `ReadTarget` | `work_order_search` (the new catalog shape key) |
| `ValueColumn` | `WorkOrder` |
| `DisplayColumns` | `PartNumber`, `Description`, `WorkCenter` |
| `DefaultMaxMatches` | `25` |
| `MaxMatchesCeiling` | `200` |

**Why those display columns.** FR-007 asks for "enough of each work order shown to choose between
near-identical matches", and `WO-045112` and `WO-045113` are indistinguishable as numbers alone. They are the exact
three columns the step's existing work-order lookup already shows a person for a work order, so choosing from the
modal presents the same information as arriving there.

---

## 3. The stored entity: the read mirror

### 3.1 `visual_work_order_search_result` *(new, `mtm_mock`)*

One row per (addressable work order, part) pair — the same granularity and the same addressable-set rule as the
existing `visual_work_order_lookup_result`, so a search and a lookup cannot disagree about which orders exist.

| Column | Type | Notes |
|---|---|---|
| `normalized_work_order` | `varchar(64)` | The matched value and the mirror's key. Copied from the live read's key domain: literally `WO-` + six digits. |
| `part_number` | `varchar(60)` | Display column. |
| `description` | `varchar(255)` | Display column. |
| `work_center` | `varchar(32)` | Display column. |
| `refreshed_utc` | `datetime` | Snapshot time. **Deliberately not projected by the read** — adding it would break the structural identity with the live read, which is the same rule the existing shapes follow. |
| `is_seed_content` | `tinyint(1)` | `1` for baseline rows so a fresh install returns a usable result rather than an error. |

**Index**: `normalized_work_order`. This is what makes a prefix search a range scan rather than a table scan, and
it is why the pattern must remain a **parameter** of a `LIKE` and never a function wrapped around the column
(`UPPER(col) LIKE …` would defeat it).

### 3.2 `visual_work_order_search_result_stage` *(new, `mtm_mock`)*

The stage twin the atomic swap needs — identical columns, no readers. Present for the same reason as the other
five: MySQL `RENAME TABLE` is all-or-nothing across the three-name swap, so a reader observes either the complete
previous snapshot or the complete new one and never a partial set.

### 3.3 The read projection — the live/mirror contract

Both sides project **exactly** this, in this order. The identity is the whole point of FR-019: a caller must not
be able to tell which one answered.

| # | Column | Source |
|---|---|---|
| 1 | `WorkOrder` | the mirror's `normalized_work_order` / the live `BASE_ID` |
| 2 | `PartNumber` | `part_number` |
| 3 | `Description` | `description` |
| 4 | `WorkCenter` | `work_center` |
| 5 | `TotalMatched` | the count of **all** matches for the pattern, before the cap |

**Live (SQL Server)**: `SELECT TOP (@MaxMatches) …, COUNT(*) OVER () AS TotalMatched`. The window is evaluated
before `TOP` is applied, which is what makes the count the true total.
**Mirror (MySQL 5.7)**: the same projection with a scalar subquery for `TotalMatched` and `LIMIT p_max_matches`,
because 5.7 has no window functions. Both take the pattern and the cap as **parameters**.

### 3.4 Validation rules

| Rule | Where enforced | Requirement |
|---|---|---|
| A source's `ReadTarget` must resolve in the shape catalog | At construction of the reader — fails loudly | Prevents a registered source that silently serves nothing |
| `ValueColumn` and every `DisplayColumns` name must be projected by the read | Registration-time assertion, mirrored by the projection-parity validation artifact | Prevents a column rename from turning the modal blank |
| The pattern and the cap are never concatenated into a statement or a procedure name | `InlineSqlAuditTests` (statement-marker scan) + the `RawSqlSeamIsConfinedToItsReviewedAllowlist` scan | FR-018, SC-006 |
| A mirrored row set is replaced as a complete snapshot, atomically | `sp_visual_work_order_search_refresh`'s truncate → load → validate → single-`RENAME` sequence | Spec 001 FR-006 |
| A reachable-but-empty live result is returned as-is | `VisualReadFallback`'s `VisualQueryStatus.Ok` branch — no mirror read on that path | FR-019, SC-007 |
