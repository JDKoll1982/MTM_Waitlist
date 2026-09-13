# Contract: the work-order source and the entry step

**Feature**: `005-setup-work-order-search` | **Date**: 2026-09-13 | **Plan**: [../plan.md](../plan.md)

The first caller of [the shared capability](search-capability.md), and the only one shipped. Identifiers are
exact; the values in §2 are copied from the spec's Verbatim Constraints and may not be renamed, recased or
reformatted.

---

## 1. Surfaces

| Surface | Project | Purpose |
|---|---|---|
| `IWorkOrderValidationService` | `MTM_Waitlist.Setup` → `MTM_Waitlist.Module_Setup.Contracts.Services` | **Changed**: gains the classification and the pattern derivation. Owns every work-order text rule. |
| `IWorkOrderSearchService` | same | **New**: the caller-side seam. Derives the pattern, calls `ISearchPickerService`, returns the outcome the view model renders. |
| `ISearchPickerDialogService` | same | **New**: raises the modal and returns the picked value, or `null` when dismissed. |
| `SearchPickerDialog` | composition root, `Module_Shared/Views` | **New**: the modal view. |
| `SetupWorkOrderViewModel` | `MTM_Waitlist.Setup/ViewModels` | **Changed**: the handoff on field exit. **No matching logic** (FR-012). |

---

## 2. Identifier contract — the work-order strings

| Identifier | Exact value |
|---|---|
| The accepted input forms, as the step's placeholder states them | `76951`, `076951`, `WO-076951` |
| The formatted work order | `WO-` followed by the digits zero-padded to **six** — `76951` becomes `WO-076951`. **Unchanged by this feature.** |
| The work-order prefix | `WO-` |
| The owner's worked example, verbatim | typing `0451` autocompletes to `WO-0451%%` and searches for all work orders with `WO-0451%%` |

**The one thing that must not be got wrong.** `0451` is a **partial**, not a malformed work order, and partials
are searched **unpadded**: the derived pattern is `WO-0451` + one trailing wildcard — `WO-0451%`. It is **never**
`WO-000451%`. The owner's `%%` denotes the same pattern as a single `%` (the spec records this), so the
implementation emits one trailing wildcard. Complete forms are padded exactly as they are today.

---

## 3. Classification contract

After trimming, removing a leading `WO`/`wo`, and removing an optional `-`:

| Remaining | Classification | Pattern derived | Modal |
|---|---|---|---|
| 5 or 6 digits | **Complete** | none — normalised to `WO-` + six padded digits by the **existing unchanged** rule | not for a resolvable order (FR-002/FR-003) |
| 1–4 digits | **Partial** | `WO-` + the digits **as typed** + `%` | **yes** (FR-001) |
| anything else | **Invalid** | none | **no** — the existing plain-language rejection (FR-022) |

Worked examples, each landing in exactly one row:

| Input | Classification | Result |
|---|---|---|
| `0451` | Partial | searches `WO-0451%` |
| `045` | Partial | searches `WO-045%` — usually no match, reported plainly |
| `WO-0451` | Partial | searches `WO-0451%` (prefix and separator tolerated, FR-017) |
| `76951` | Complete | `WO-076951`, used directly, **no modal** |
| `076951` | Complete | `WO-076951`, used directly |
| `WO-076951` | Complete | used **as entered** — reformatting must not change it |
| `ABCD` | Invalid | the existing rejection, **no search** |

---

## 4. Entry-step behaviour — the full matrix

The search is raised on **leaving the field**, reusing the existing `LostFocus` →
`AutoSearchOnWorkOrderBlurAsync` path. Nothing searches on a keystroke.

| Situation | Behaviour | Requirement |
|---|---|---|
| Empty or whitespace entry | Nothing happens; no search, no modal. | existing behaviour |
| **Partial**, matches ≥ 1 | The modal lists them. **One match still shows the modal** — uniform behaviour, and the person always sees what they are accepting. | FR-001, US1 |
| **Partial**, matches 0 | Plain language with a way to try again. Never an empty list, never silence. | FR-010 |
| **Partial**, more matches than the cap | The modal stays usable and **says how many matched** (`TotalMatched`, with `IsCapped` set) — never an unbounded list. | FR-011 |
| **Complete**, resolves | Formatted and used directly. **No modal at all**, nothing to acknowledge. | FR-002, FR-003 |
| **Complete**, resolves to nothing | The existing "no parts" message, **and the modal is raised** with the exact `WO-######%` pattern, so the search is available as the way forward. *(Reading recorded in [research.md](../research.md) §Open points 1 — the owner may flip this without a spec change.)* | spec Edge Case |
| **Invalid** | The existing rejection, unchanged, including its wording. **No modal.** | FR-022 |
| The list is **unreachable** | The existing per-screen unavailable state with a manual retry. **Never** "no matches" — that would be a false answer. | FR-019, SC-007 |
| The list is **reachable but empty** | A real answer: the person is told there is no match. **Never** replaced by cached rows. | FR-019, SC-007 |
| Choosing a match | The entry becomes that work order and the flow continues **exactly as if it had been typed in full**. | FR-008 |
| Dismissing | The entry and the flow are **unchanged**, and the person can search again — no trap. | FR-009 |
| The entry is edited while the modal is open | The modal closes; the new text searches when the field is left again. | spec Edge Case |

---

## 5. Modal contract — the identifiers a test or an automation codes against

| Identifier | Value | Purpose |
|---|---|---|
| Automation id, the dialog root | `SearchPickerDialog` | Locating the modal |
| Automation id, the results list | `SearchPickerResults` | Locating the matches |
| Automation id, the count line | `SearchPickerCount` | Asserting "how many matched" (FR-011) |
| Resources, title | `Setup_WorkOrderSearch.DialogTitle` | FR-021 |
| Resources, no matches | `Setup_WorkOrderSearch.NoMatches` | FR-010 |
| Resources, count | `Setup_WorkOrderSearch.Count` | FR-011 |
| Resources, confirm | `Setup_WorkOrderSearch.Confirm` | FR-008 |
| Resources, dismiss | `Setup_WorkOrderSearch.Dismiss` | FR-009 |
| Resources, unavailable | `Setup_WorkOrderSearch.Unavailable` | FR-019 |

**States the modal must render distinctly** — four, not three: matches; **no match**; **more than the cap**
(the count line, the list still usable); and **unavailable** (the existing per-screen unavailable state, never
styled as an empty result). Every string resolves through `"Key".GetLocalized()` with a fallback literal, so a
missing resource degrades to readable English rather than to a resource key.

**Row anatomy** — `Value` first, then the display columns (`PartNumber`, `Description`, `WorkCenter`) in the
source's declared order, so a person can tell `WO-045112` from `WO-045113`.

---

## 6. Verification this contract implies

| Claim | Evidence required |
|---|---|
| `0451` derives `WO-0451%` and **not** `WO-000451%` | A unit test on the classification, asserting the exact string |
| A complete form in each accepted form shows **zero** modals | SC-002 — a view-model test with a dialog seam that records being raised |
| Dismissing leaves the entry and the flow unchanged | A view-model test asserting the entry and the step after `null` comes back |
| `ABCD` still produces the existing rejection | The existing message resource key, asserted unchanged |
| The flow advances with the chosen work order | A view-model test asserting the normalised value and the resulting step |
