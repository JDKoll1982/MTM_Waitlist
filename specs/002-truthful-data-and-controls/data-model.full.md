# Phase 1 — Data Model: Truthful Data and Truthful Controls

**Date**: 2026-09-12 | **Plan**: [plan.md](./plan.md) | **Research**: [research.md](./research.md)

This feature persists nothing, so there is no schema. What follows is the model of *what the surfaces must
be able to express* and the *inventory the change operates on*; it is what the contracts, the tasks and the
verification are checked against.

---

## 1. Block state (the only new state)

A **block** is a card, a section, or a labelled attribute group on one of the affected surfaces.

| State | Cause | Rendering rule | Requirement |
| --- | --- | --- | --- |
| `Present` | every attribute it shows is traced to the request on screen, to an authoritative read for it, or to the cached external read | rows render normally | FR-001, FR-006 |
| `Row absent` | an individual attribute has no source | that row is not rendered; its label goes with it; no substitute value | FR-002 |
| `Hidden` | the read succeeded and found nothing (a work order / part / operation pair that genuinely has no coil, an empty section) | the block is not in the visual tree (`x:Load` false) or is collapsed; no message, no empty shell | FR-002, US1 §7 |
| `Failed` | the read threw or reported an error result | the block renders the error text plus a retry action in place; the rest of the page is unaffected | FR-002, SC-014 |

**Transitions**: `Present → Failed` (read throws) · `Failed → Present` (retry succeeds with rows) ·
`Failed → Hidden` (retry succeeds with no rows) · `Hidden → Present` (a later load finds rows).

**Invariants** — violations are defects, not styling choices:

1. `Failed` is never rendered as `Hidden` or as `Row absent` (no failure may be disguised as absence).
2. `Hidden` is never accompanied by a placeholder value, a stand-in figure, or an empty labelled shell.
3. A page-level **store-outage** state keeps its existing meaning (`specs/001` FR-021: message, retry,
   no sample data). A block failure never renders as that state, and that state is not reused for one.

---

## 2. Fabricated attribute inventory (what comes out)

Source of truth: `defects/Closed-Partial-Critical-Waitlist-CardAndDetailShowFabricatedMaterialData.md` §3 (roughly thirty
literals) plus the two companion defects. Disposition is identical for every row.

| Group | Attributes | Typical literal today | Disposition here | Restored by |
| --- | --- | --- | --- | --- |
| Coil | coil number, quantity in house, description | `COIL-204`, `46,000 lb`, `0.060 x 48 in galvanized coil` | removed | template 03 |
| Coil | average coil weight | `5,000 lb`, part fixed at `MMC0001000` | lookup + fallback removed; no value shown | template 03 (per coil) |
| Finished goods | part, customer, packlist, traceability id | `FG-10042`, `Northstar Manufacturing`, `PL-88213`, `TR-5501` | removed | template 03 |
| NCM | part, quantity, disposition text | `NCM-2211` | removed | template 03 |
| WIP | destination, quantity | `Sub-assembly`, `24 pieces` | removed | template 03 |
| Outside service | destination, vendor, quantity | `Heat Treat Section`, `Midwest Heat Treat`, `6 pieces` | removed | template 03 |
| Flatstock | part, quantity, destination | `RM-8201`, `12 sheets`, `Flatstock staging` | removed | template 03 |
| Table handling | part, quantity, destination, operation sequence | `Part A-12`, `1`, `Table staging` | removed | template 03 |
| Die handling | die number, quantity, destination | `Die 4402`, `Die shop` | removed | template 03 |
| Scrap | part number | `"Not provided"` (rendered as a value, not as absence) | removed — a value that says nothing is still a value | template 03 |
| Detail inventions | "Tipping strategy", "Allowed categories", `Pending`, `Pending assignment`, `Pending Quality review`, `Pending pickup` | static literals with no source | removed | template 03 (only where a real source exists) |
| Detail defaults | `"Not available"` fallbacks on the label/value helper | a default parameter value | removed: the helper no longer takes a fallback | — |

---

## 3. Control inventory

| Surface | Control | Behaviour today | This feature |
| --- | --- | --- | --- |
| Waitlist card (all 6 type templates) | Cancel and Accept buttons | drawn everywhere, no `Command`, no handler | removed; the action area shows the request's real status or nothing |
| New Request confirm | queue count + estimated wait card | fixed `0 active request(s)…` and `…approximately 15 minutes` | removed (real figures belong to template 04) |
| New Request confirm | coil details card | real values from the work centre's current job | unchanged — already substantiated |
| New Request confirm | average coil weight field | constant-keyed lookup + fixed fallback | lookup and fallback removed; no value shown |
| Settings → Operations | New Request Alerts toggle | always enabled; silently stores a preference | `IsEnabled` bound to the installation's capability; localized reason shown when unavailable; no preference stored while unavailable |
| Settings → About | Privacy Policy hyperlink | `https://YourPrivacyUrlGoesHere/` | entry and its resource keys removed (no published statement exists) |

---

## 4. Settings search registration

Model: `SettingsViewModel` holds a declared list of the property names that consume `MatchesSearch(...)`;
`RefreshSearchVisibility()` raises `PropertyChanged` for every name in it.

- **Members today**: the seven panels already notified **plus** `IsNewRequestAlertsPanelVisible`,
  `IsUrgencyAllotmentsPanelVisible`, `IsImageLocationSettingsPanelVisible`, plus the three category
  aggregates (`IsAppearanceCategoryVisible`, `IsOperationsCategoryVisible`, `IsAboutCategoryVisible`).
- **Invariant**: every property whose getter calls `MatchesSearch` is a member of the list, and every member
  raises `PropertyChanged` when the term changes.
- **Enforcement**: a reflection coverage test (property set whose getter references `MatchesSearch` ⊆ the
  declared list) plus a notification test (a `PropertyChanged` is observed for each of the three previously
  missing panels when `SearchQuery` changes) — value-only assertions are explicitly not sufficient.

---

## 5. Notification activation

| Aspect | Model |
| --- | --- |
| Argument shape | `action=openrequest&request=<guid>` — parsed by the existing `WaitlistRequestLink.TryParse` |
| Mapped, app running | navigate to the waitlist detail page for that request id **and** bring the window forward; no dialog |
| Mapped, cold start | unchanged (already correct) |
| Unmapped, app running | log for diagnosis, bring the window forward, show nothing |
| Unmapped, cold start | log, show nothing (no new placeholder path) |
| Placeholder payload | the shipped sample-argument resource entry is removed when it has no consumer |

---

## 6. Documentation record

| Document | Role | Required change |
| --- | --- | --- |
| `FEATURES.md` | the running-application inventory that recorded the ten defects | mark the defects this feature closes; keep the ⚠ markers only where a follow-on spec owns the remainder |
| `defects/*.md` (10 files) | evidence record | add a closure status line naming this spec, the follow-on owner and the tests; leave every quoted literal as evidence |
| `WeekendProject/ChangeLog.Simple.md` | plain-language changelog | feature 19 no longer presents the removed manual toggle as current; feature 18 records the alerts caveat |
| `WeekendProject/ChangeLog.md` | detailed changelog | same corrections in prose form |
| `OPEN-TASKS.md` §3.1 | backlog index | "retired sources" note on the six stale sources with the reason each must not be reopened |
| `WeekendProject/OPEN-WORK-NEXT-SPEC.md` §10.2 | forward-work document | point at that note; do not reopen §10.1 |
| `WeekendProject/Module_Mock/Tasks.md` | the seed task file, 72 boxes behind reality | reconcile completed items so the count matches what is finished |

---

## 7. Entities (one line each)

- **Value Source** — for a value a surface displays, its identified origin (the request on screen, an
  authoritative read, or the cached external read); a value with no identified source is not displayed.
- **Block State** — `Present` / `Row absent` / `Hidden` / `Failed`, per §1.
- **Unavailable Capability** — behaviour the application cannot perform in this installation (notification
  delivery today), represented by absence or by an explicit, disabled state carrying its reason.
- **Settings Search Registration** — the declared list of search-consuming properties, plus the coverage
  invariant that keeps it complete.
- **Notification Activation** — parsed arguments that either map to a request or do not, with the two
  outcomes of §5.
- **Documentation Record** — a repository document describing behaviour, with the change §6 requires.
