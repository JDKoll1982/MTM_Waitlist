# Data Model: Generic Work Center Stated Scrap Type and Lugger

This document names the entities the plan introduces and the fields each one carries. It states shapes, keys, states and transitions. It does not repeat why the plan chose them.

The feature turns on two facts. The requester states a scrap type, which a new answer record writes into the request's existing `answers_json` column. The card then resolves that stated type to a lugger through a new outcome record, using the same arrangement routine the work-order scrap card already calls. One existing type gains a new behaviour, one existing option loses two leftover fields, and one snapshot gains a field that tells an outage from an empty arrangement.

## 1. `GenericRequestScrapAnswer`

`GenericRequestScrapAnswer` is the value a raised request records in its existing `answers_json` column. It is new and lives in `MTM_Waitlist.Core/Models/`, beside `GenericRequestDunnageAnswer`.

It mirrors the dunnage answer shape. It is a sealed record with a const key string, a `ToAnswers()` method that returns a case-insensitive dictionary, and a static `TryRead` that answers null rather than throwing.

| Field | Type | Meaning |
| --- | --- | --- |
| `ScrapType` | `string` | The scrap type as the requester chose it, in their wording. |
| `ScrapTypeKey` | `const string` | The recorded key, fixed as `scrap.type`. |

`ToAnswers()` returns a dictionary holding `scrap.type` with the stated type as its value, keyed with `StringComparer.OrdinalIgnoreCase` as `GenericRequestDunnageAnswer` does.

`TryRead(IReadOnlyDictionary<string, string>? answers)` reads `scrap.type`. It returns null when the key is missing, and returns null when the stored value is empty or whitespace. A blank type records nothing.

## 2. `GenericRequestScrapOption`

`GenericRequestScrapOption` is the choice the scrap step offers. It is an existing record in `MTM_Waitlist.GenericRequest/Models/GenericRequestOptions.cs`. This feature reshapes it.

It loses `AvailableLuggerCount` and the computed `AvailableLuggerDisplay`, leaving the scrap type alone.

The two fields are a leftover of the deleted `Temp` preview and nothing populates them. The page loses its two count columns and the label that promised a lugger count.

| Field | Type | Meaning |
| --- | --- | --- |
| `ScrapType` | `string` | The scrap type the row offers. |

## 3. `ScrapTypeCatalog.Compose`

`ScrapTypeCatalog` is the existing compiled scrap-type list in `MTM_Waitlist.Core/Services/`. It gains one behaviour, `Compose(IEnumerable<string>? assignedTypes)`.

`Compose` takes the arrangement's own type names and returns the ordered choices. This is the FR-002 rule, and it now has exactly one home. The arrangement editor calls it instead of its own private copy.

The ordering rule is the catalogue first, in `All`'s own order, then every assigned name the catalogue does not already hold.

The de-duplication rule compares names trimmed and case-insensitively, so two spellings that differ only in letter case or surrounding spaces are one choice.

`Compose` drops `ScrapDecisionRules.RequiredPlaceholder` and `ScrapDecisionRules.NoScrap`. They answer whether there is scrap, not where it goes.

## 4. `LuggerArrangementSnapshot.WasRead`

`LuggerArrangementSnapshot` is the existing arrangement snapshot in `MTM_Waitlist.Core/Models/Lugger/`. It gains one field, `WasRead`.

`WasRead` is the only thing that separates an outage from an arrangement with no rows. Both leave `Rows` empty, but only a read that answered sets it true.

| Path | `WasRead` | Meaning |
| --- | --- | --- |
| `Create(rows)` | `true` | The arrangement read answered. |
| `Empty`, also the type's initialiser | `false` | The read failed, or never happened. |

The generic scrap step reports `Unavailable` when `WasRead` is false, and offers no list at all.

## 5. `StatedScrapLugger`

`StatedScrapLugger` is the record a stated scrap type resolves to. It is new and lives in `MTM_Waitlist.Settings/Models/`.

| Field | Type | Meaning |
| --- | --- | --- |
| `LuggerLocation` | `string?` | The lugger name, or null when none is assigned and when the arrangement was not read. |
| `LuggerImagePath` | `string` | The picture that belongs to the lugger, resolved where the arrangement was in hand. |
| `ArrangementRead` | `bool` | Whether the arrangement read answered. |

It encodes three outcomes.

| Outcome | `ArrangementRead` | `LuggerLocation` | Requirement |
| --- | --- | --- | --- |
| A named lugger | `true` | the lugger name | FR-008, and generally FR-006 and FR-007 |
| A configuration gap | `true` | `null` | FR-009 |
| An unreadable arrangement | `false` | `null` | FR-010 |

An all-blocked type arrives already composed as `Name (Status)`, such as `Door 9 Lugger (Full)`, because `LuggerArrangementSnapshot.Resolve` builds that string. The card shows it as the lugger name and does not draw a blank.

## 6. `GenericRequestDraft.ScrapType`

`GenericRequestDraft.ScrapType` is the existing in-flight value on the draft, in `MTM_Waitlist.GenericRequest/Models/GenericRequestDraft.cs`. It is unchanged.

It holds the stated type while the requester works through the step. It is what the confirmation reads and what back navigation re-selects.

At raise, when `RequiresScrap` is set and `ScrapType` is not empty, the draft value becomes `GenericRequestScrapAnswer.ScrapType`. The draft is the source and the recorded answer is the copy.

## 7. `WaitlistRequest.Answers`

`WaitlistRequest.Answers` is the existing answers dictionary on the raised request. `WaitlistRequest` gains no field.

The recorded answer lands here under `scrap.type`. `GenericRequestScrapAnswer.TryRead(request.Answers)` supplies the wording for the card, so a type removed or renamed in the arrangement later leaves the request saying what the requester said.

## State transitions

### Step lifecycle

```text
Unread -> Choices | NoList
NoList -> Choices
Choices -> Chosen
Chosen -> Stated
```

| State | Meaning |
| --- | --- |
| `Unread` | The arrangement read is in flight and nothing is offered. |
| `Choices` | The read answered and the composed list is offered. |
| `Chosen` | One type is selected and Continue is enabled. |
| `NoList` | The read failed, so no list at all is offered. |
| `Stated` | The type is on the request and the details step is skipped. |

The step reports `NoList` when `WasRead` is false. A retry re-runs the same read and can move it to `Choices`.

### Card facts

| State | Card shows | Meaning |
| --- | --- | --- |
| `Resolved` | the type and the assigned lugger | The arrangement assigns a lugger now. |
| `Gap` | the type and "Not assigned" | No lugger is assigned to the type. |
| `LuggerUnavailable` | the type and "Temporarily unavailable" | The arrangement could not be read. |

The lugger is a live read at draw time. A reassignment shows on the next load and nothing is copied onto the request.

## Validation rules

| Rule | Statement |
| --- | --- |
| FR-001 | The step asks for the scrap type and reads no job. |
| FR-002 | The choices come from the shared list in its order, plus arrangement types outside it, through `ScrapTypeCatalog.Compose`. No second list is introduced. |
| FR-003 | The choices never include the required placeholder or the no-scrap value. |
| FR-004 | The step does not advance without a chosen type. |
| FR-005 | The chosen type is carried on the confirmation, the card and the stored request in the requester's wording. |
| FR-006 | The card names the lugger the arrangement assigns now and reads no job. |
| FR-007 | The work-order and generic cards resolve through one routine, so they cannot disagree. |
| FR-008 | An all-blocked type draws the first lugger in order with its status beside the name. |
| FR-009 | A type with no lugger records a configuration gap, never an invented lugger. |
| FR-010 | An unreadable arrangement reports the lugger as temporarily unavailable, never invented. |
| FR-011 | A type removed or renamed later leaves the stated wording intact. |
| FR-012 | A type reassigned later shows the lugger assigned now. |
| FR-013 | A stored type outside the shared list still draws with its type named. |

## Schema and storage

There is no schema change, no new stored procedure and no migration. The arrangement is read through the existing `sp_waitlist_lugger_arrangement_list` and `ILuggerArrangementService`. The stated type is written through the queue's existing submit path into `answers_json`. No `create.sql` or `rollback.sql` is needed.
