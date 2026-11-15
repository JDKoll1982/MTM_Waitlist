# Data Model: Generic Work Center — Die Search

Three values are new or reshaped: what one search row is, what one chosen die is, and what the request stores about it. Everything else the chunk touches already exists and keeps its shape.

## Die search row

The line a search answers, before a person sees it.

| Field | Type | Rule |
|---|---|---|
| `DieNumber` | text | The die's Infor Visual `PART.ID`. Never empty — a row without a number is not a die and is not drawn (FR-002). |
| `Description` | text | The die's own `PART.DESCRIPTION`, trimmed. Shown as recorded, including the plant's `Die still needs location` placeholder, which is never read as a location (FR-009). |
| `Location` | text | `PART_SITE.PRIMARY_LOC_ID`, else the lowest `PART_LOCATION.LOCATION_ID`, else empty. Empty is drawn as "No location recorded", never as a blank cell (FR-006, SC-005). |
| `Association` | text | `Die`, `Explicit` or `History`. Context for ordering and for the read's own tests; it is not drawn. |

Ordering: the exact die number first, then explicit associations, then history, each alphabetically by die number. The same die number appears once however many associations named it (FR-003, FR-007).

## Chosen die

`GenericRequestDieOption`, already the step's row, gains one field and the rule that it is a choice rather than a single selection.

| Field | Type | Rule |
|---|---|---|
| `DieNumber` | text | As the search answered it. |
| `Description` | text | As the search answered it. |
| `Location` | text | As the search answered it, possibly empty. |
| `LocationText` | text | What the row draws for the location: the location, or the "No location recorded" wording when there is none. Never empty (SC-005). |
| `IsSelected` | flag | Whether this die is one of the requester's choices. Set and cleared by a tap; survives a new search for the same die number (FR-010). |

`GenericRequestDraft` keeps `DieNumber` as the first chosen die and gains `SelectedDies`, the dies in the order they were chosen. `SelectedDies` is what raising the request walks (FR-011).

## Stored die answer

`GenericRequestDieAnswer`, in `MTM_Waitlist.Core.Models`, beside the scrap and part answers. It is written into the request's existing answers column; no store change is made.

| Field | Stored under | Rule |
|---|---|---|
| `DieNumber` | `die.number` | Written only when a die is stated. Trimmed. |
| `DieLocation` | `die.location` | Written only when a die is stated and a location is known; an empty location writes no key rather than an empty value (FR-006). |

`ToAnswers()` reads neither the board nor the store. `TryRead()` answers null for a request that states no die, and reads both keys case-insensitively, because a request raised under a different spelling of the same key is still the same request.

## Raising several requests

`GenericRequestSubmission` keeps its three outcomes and gains the count:

| Field | Rule |
|---|---|
| `IsRaised` | True only when every chosen die's request was accepted. |
| `RaisedCount` | How many requests were raised. Zero for a failure, one for the ordinary path. |
| `RequestId` | The first raised request's identifier, which is the one a person is shown when the count is one. |
| `IsDuplicate` | True only when the queue refused every attempt as a duplicate. |
| `FailureMessage` | The queue's own words for the first refusal, or the first failure. |

## Step states

The die step reuses the four presentations every step already has and adds no state of its own.

```text
Empty term         → a search on entry, answered as the source answers; nothing is assumed.
Rows               → Populated. The count is drawn with the rows; Continue is enabled once one row is chosen.
No rows            → Empty. The line names Infor Visual; Continue stays disabled.
Source unreachable → Unavailable. Attempt time, a retry, the typed term kept; Continue stays disabled.
No die chosen      → the step does not advance; the refusal line is drawn (FR-013).
```
