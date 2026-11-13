# Research: Generic Work Center — Stated Scrap Type and Lugger

The spec at [spec.md](./spec.md) is settled on *what*. This records the four choices the codebase did not settle for us, and what was rejected.

## D1 — Where the choices come from, and in what order

**Decision.** Build the list from `ScrapTypeCatalog.All` in its own order, then append every type in `LuggerArrangementSnapshot.ScrapTypes` that the catalog does not already hold, matching by trimmed, case-insensitive name.

**Rationale.** FR-002 names the shared list as the source and forbids a second one. `ScrapTypes` is exposed alphabetised (`OrderByOrdinalIgnoreCase`), so using it as the list would silently reorder the plant's list and break FR-002. Reading the catalog first and appending only what it lacks preserves both halves of the requirement.

**Alternatives rejected.** Using `ScrapTypes` alone (wrong order, and empty when the arrangement cannot be read, which would hide the plant's own list). Building a new scrap-type table (a second list, forbidden).

## D2 — How the two no-scrap answers are kept out

**Decision.** Filter every candidate through `ScrapDecisionRules.IsNoScrap` and `ScrapDecisionRules.IsRequiredPlaceholder` before it reaches the options, whether it came from the catalog or from the arrangement.

**Rationale.** The catalog already omits them, but the arrangement holds whatever an operator typed, so an arrangement entry spelled `No Scrap` would otherwise reach the list. Filtering both sources in one place is the only way FR-003 holds for outside types too.

**Alternatives rejected.** Trusting the catalog and skipping the arrangement side (leaves FR-003 open for recorded spellings).

## D3 — Who resolves the lugger on a generic card, and how the three card outcomes are told apart

**Decision.** The card path resolves the lugger itself, through `LuggerArrangementSnapshot.Resolve`, on a snapshot read once per list load. The three outcomes are separated by the snapshot, not by the resolver's outcome alone:

- snapshot has rows and `Resolve` returns a lugger → name it (this covers the all-blocked case, where the name arrives with its status beside it);
- snapshot has rows and `Resolve` reports a configuration gap → record the gap and draw no lugger;
- snapshot has no rows → the arrangement could not be read or holds nothing, so report the lugger as unavailable.

**Rationale.** FR-007 requires the same resolution the work-order card uses, and `Resolve` *is* that resolution, so calling it is the only way the two cannot disagree. `Resolve` alone cannot separate a gap from an outage — an empty snapshot and an unassigned type both report `NotConfigured` — so the row count does that, which is exactly how `RequestJobAvailabilityProvider` already separates them.

**Alternatives rejected.** Copying the rule set into the board (two resolvers, the divergence FR-007 exists to prevent). Reusing the work-order availability provider (it answers per work centre from the *active job*, and FR-006 forbids reading a job for this). Dropping the lugger cell when the arrangement cannot be read, as the work-order card does (FR-010 requires the type to still be named and the lugger reported unavailable).

## D4 — Where the stated type is kept, and what survives a change

**Decision.** Record it on the request's existing `answers_json` column through a new `GenericRequestScrapAnswer` record with the key `scrap.type`, and read it back the way `GenericRequestPartAnswer` and `GenericRequestDunnageAnswer` are read. The lugger is never stored; it is resolved live on every read.

**Rationale.** A2 says the stated type is what the request keeps and the dump point is a live fact, which is exactly the split `answers_json` gives without a schema change. Storing the lugger would freeze a value that FR-012 says must follow the arrangement, and would be wrong the moment a lugger is blocked.

**Alternatives rejected.** A new column and procedure pair (a schema change for one string, and every insert and list procedure would have to carry it). Storing the resolved lugger beside the type (contradicts A2 and FR-012).

## Open question the spec left, resolved here

The spec's design note asked what the step shows and in which wording, because the review host that rendered it is gone. The answer needs no new design: the step keeps the page it already has — a single-select list of types with the count of luggers free to receive material beside each — and the shared step frame already supplies the loading, empty, unavailable and retry presentations the other steps use (A4).

## What the code does that the spec does not say

Three behaviours were found while reading the flow. Each is small, and each is decided here rather than left to the implementation:

1. **The option carries a lugger count.** `GenericRequestScrapOption` has `AvailableLuggerCount` and the page draws it. The count is the number of rows for that type that can receive material. Left uncomputed, every row would read zero.
2. **Changing the work center leaves the old type behind.** `GenericRequestWorkCenterViewModel` clears the answer and the four item requirements but not `Draft.ScrapType`, so a type stated at one work center would ride into a request for another. It is cleared with the rest.
3. **The card's second line reads `{scrap_type}` from the job.** A generic request has no job, so the token stays unresolved and the line falls back to the Item's name. It is filled from the stored answer instead, the way `{dunnage_part}` already is, so the card names the type in both places it is asked to.
