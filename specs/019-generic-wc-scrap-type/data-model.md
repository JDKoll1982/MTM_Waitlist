# Data Model — Generic Work Center: Stated Scrap Type and Lugger

Feature: `019-generic-wc-scrap-type`

This chunk adds one new answer record and reuses three existing shapes untouched: the scrap option the step already lists, and the arrangement snapshot's resolution result. The stated type is written onto the request's existing `answers_json` column, so no table, column or procedure changes. The lugger is never stored; it is read live each time the card is drawn.

## Entities

### 1. `GenericRequestScrapAnswer` (new)

The scrap type a generic requester stated, written to and read back from the request's own stored answers. It sits in Core beside `GenericRequestPartAnswer` and `GenericRequestDunnageAnswer` and is read the same way.

| Item | Kind | Meaning |
|---|---|---|
| `ScrapTypeKey` | constant, `"scrap.type"` | The answers key the stated type is stored under. |
| `ScrapType` | string, defaults empty | The type in the wording the requester chose. |

**`ToAnswers()`** returns the dictionary handed to `answers_json`, using a case-insensitive key comparer. It writes `scrap.type` only when the stated type is non-blank, so a request that never stated a type writes no scrap key at all.

**`TryRead(answers)`** returns `null` when the answers are absent or when `scrap.type` is missing or blank after trimming; otherwise it returns a record carrying the trimmed stored wording. The wording is preserved exactly (only surrounding spaces removed) so a type later renamed in the arrangement still reads as the requester stated it (FR-011).

**Storage.** The record rides the existing `answers_json` column through the same insert and list procedures every request already uses — no schema change (FR-005, plan "Phase 1 design").

### 2. `GenericRequestScrapOption` (already exists — not changed)

The single-select choice the scrap step lists, one per scrap type.

| Field | Meaning |
|---|---|
| `ScrapType` | The type's name. This is what is committed to the draft and stored in the answer. |
| `AvailableLuggerCount` | How many luggers for this type can receive material right now — the receivable rows in the arrangement for that type. Drawn beside the name via its display helper. |

The count is informational only and gates nothing: a type whose luggers are all blocked still appears, with a count of zero, and its card falls to the "Resolved, status beside the name" outcome (FR-008). The shape is not changed by this feature; only its count is filled from a real read.

### 3. Lugger resolution result (already exists — not changed)

`LuggerLocationResolution` and `LuggerLocationOutcome` in `MTM_Waitlist.Core\Models\Lugger\` are the one rule set both the work-order and generic cards use, so they cannot disagree for the same type (FR-007).

| Type | Members | Meaning |
|---|---|---|
| `LuggerLocationResolution` | `LuggerLocation` (string, null when nothing is named), `Outcome` (`LuggerLocationOutcome`), `IsConfigurationGap` | The lugger a handler walks to, or why there is none. |
| `LuggerLocationOutcome` | `Resolved` | A lugger was named (including the all-blocked case, where the name carries its status). |
| | `NoScrapDecision` | The value is blank, `Scrap Type Required` or `No Scrap` — not a scrap type, so no lugger and no gap. |
| | `NotConfigured` | The type is real but no lugger is assigned to it: a configuration gap. |

This entity is reused exactly as it exists; this chunk adds no field and changes no behaviour.

## Relationships

- One generic scrap request has at most one `GenericRequestScrapAnswer`, keyed `scrap.type` inside its `answers_json`.
- Each `GenericRequestScrapOption.ScrapType` is a candidate value for that answer.
- The card joins the stored answer to a live `LuggerArrangementSnapshot`, producing one `LuggerLocationResolution` per draw. Nothing links the two in storage.

## Validation rules

| Rule | Where it applies | Requirement |
|---|---|---|
| `No Scrap` is never offered as a choice. | Scrap step options | FR-003, Verbatim Constraints |
| `Scrap Type Required` is never offered as a choice. | Scrap step options | FR-003, Verbatim Constraints |
| Choices are the shared scrap-type list in its own order, then arrangement-only types appended. | Scrap step options | FR-002 |
| An outside type matches an arrangement entry ignoring surrounding spaces and letter case. | Option build, and `Resolve` | FR-002, Verbatim Constraints |
| `AvailableLuggerCount` is the number of receivable rows for that type. | Option build | plan / research D1 |
| A blank type is never recorded — `ToAnswers()` omits the key and `TryRead()` returns null. | Answer write and read | FR-004, FR-005 |
| The stored wording is not rewritten when the arrangement renames or drops the type. | Answer read, card | FR-011 |
| The lugger shown is the one assigned now, never a stored copy. | Card | FR-012 |

## State transitions — the card's lugger cell

The three outcomes are separated by the snapshot's row count, not by the resolver's outcome alone (plan "States", research D3). The type is named in every outcome.

| Outcome | Condition | What the card draws |
|---|---|---|
| **Resolved** | The arrangement holds rows and `Resolve` names a lugger — including the all-blocked case, where the name arrives with its status beside it. | The type, and the lugger name (with status if blocked). Shows `generic-scrap-card`. |
| **Gap** | The arrangement holds rows and `Resolve` reports a configuration gap (`NotConfigured`). | The type, the gap recorded once, and no lugger invented. Shows `generic-scrap-card`. (FR-009) |
| **Unavailable** | The arrangement was read as empty — it could not be read or holds nothing. | The type, and the lugger reported unavailable; never a stale or guessed name. Shows `generic-scrap-card`. (FR-010) |

A type reassigned to a different lugger simply changes which lugger the next draw resolves; the stored answer does not change (FR-012).

## Deliberately not stored

The lugger is **not** part of the request. Per FR-012 and assumption A2, the dump point is a live fact read from the arrangement every time the card is drawn; only the stated type is kept, in the requester's wording. Storing the lugger would freeze a value that must follow the arrangement and would be wrong the moment a lugger is blocked.
