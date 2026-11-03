# Data Model: Generic Work Center — Designation and Request Foundation

One stored table changes; the rest are in-memory shapes the flow and the board already carry, extended so a
generic request can be told apart, read back and drawn without a work order.

## Relationship overview

```text
waitlist_requests_queue (1 row = 1 request, generic or work-order)
  └─ read into ── SampleOrder (board row)
       └─ drawn by ── GenericRequestCardModel (card and detail page)

GenericRequestDraft (what the requester answered)
  └─ mapped onto ── WaitlistRequestDraft
       └─ written by ── sp_waitlist_request_insert

WorkCenterDetail ── selects into ── WorkCenterSelectionItem (IsGeneric carried on both)
GenericRequestItemOption ── supplies ── the item step's prompt and the card's StatedValue
```

## waitlist_requests_queue (reshaped)

The existing queue gains what a request with no work order needs, and loses the assumption that a job is always
present. Only the new and changed columns are listed; every other column is unchanged.

| Column | Shape | Change | Why |
| --- | --- | --- | --- |
| `active_setup_job_id` | `VARCHAR(64) NULL` | was `NOT NULL` | a generic request stores no work order at all (FR-008) |
| `is_generic` | `TINYINT(1) NOT NULL DEFAULT 0` | new | the kind of request is a stored fact, not an inference from a null (R2) |
| `description` | `VARCHAR(500) NULL` | new | the requester's optional description travels with the request (FR-016, R3) |
| `accepted_warnings_json` | `JSON NULL` | new | every warning the requester accepted is recorded (FR-019, R3) |

Validation rules:

| Rule | Source |
| --- | --- |
| `is_generic = 1` requires `active_setup_job_id IS NULL` | FR-008, R2 |
| `is_generic = 0` requires `active_setup_job_id` to be present and not blank | FR-005, R2 |
| `description` is optional and, when present, is not blank | FR-016 |
| `accepted_warnings_json` is optional and, when present, holds a JSON array | FR-019 |
| Existing rows stay valid: the default of `0` leaves every work-order request classified as it was | FR-004, SC-006 |

The default of `0` is a column default, not a built-in list of generic work centers: no work center is generic
until a permitted person marks it (FR-003).

## GenericRequestDraft

The answers given so far, held in `MTM_Waitlist.GenericRequest/Models/GenericRequestDraft.cs`.

Fields already carried: `WorkCenter`, `Category`, `ItemName`, `PartNumber`, `PartStage`, `Sequence`,
`DunnageType`, `DunnagePart`, `ScrapType`, `DieNumber`, `Quantity`, `NeededBy`, `Description`, `RequiresDunnage`,
`RequiresScrap`, `RequiresDie`, `RaisedRequestNumber`, `ResultPresentation`.

Fields added by this change:

| Field | Shape | Why |
| --- | --- | --- |
| `Building` | string | the work center's building, so the summary can identify where the request is (FR-017) |
| `ItemCode` | string | the item's code as the queue stores it, distinct from the display name (FR-019) |
| `Answer` | string | the configured text or choice the requester gave; the request's stated value (FR-019) |
| `EstimatedMinutes` | `int?` | the allotted time configured for the item; absent when none is configured (FR-013, FR-014) |
| `AcceptedWarnings` | list of string | warnings the requester saw and accepted at confirmation (FR-019) |
| `RaisedRequestNumber` | string | the public identifier the queue returned on success; it is also what the result step shows (FR-019) |

Validation rules:

| Rule | Source |
| --- | --- |
| A field is set by the step that owns it and is never cleared by navigation | US1 of spec 015 |
| `NeededBy` is required before the request can be raised | FR-015 |
| `Description` is optional | FR-016 |
| `EstimatedMinutes` stays absent rather than defaulting when the item configures no time | FR-014 |
| `Answer` is required when the chosen item declares a prompt | FR-007, FR-021 |
| The draft is not a stored record; abandoning it stores nothing | FR-011 |

## GenericRequestItemOption

One pickable item, in `MTM_Waitlist.GenericRequest/Models/GenericRequestOptions.cs`.

Fields already carried: `Code`, `Name`, `NeedsDunnage`, `NeedsScrap`, `NeedsDie`.

Fields added by this change:

| Field | Shape | Why |
| --- | --- | --- |
| `PromptText` | string | the question the item's configuration declares (FR-007) |
| `AnswerKind` | `GenericRequestAnswerKind` | what kind of answer that question takes (FR-007) |
| `AnswerOptions` | `IReadOnlyList<string>` | the choices offered when the answer is a choice |
| `AllottedMinutes` | `int?` | the configured time, carried to the draft and the card (FR-013) |

`GenericRequestAnswerKind` values:

| Value | Meaning |
| --- | --- |
| `None` | the item declares no question |
| `Text` | the answer is free text the requester types |
| `Choice` | the answer is one of `AnswerOptions` |

Validation rules:

| Rule | Source |
| --- | --- |
| `PromptText` is present when `AnswerKind` is not `None` | FR-007 |
| `AnswerOptions` is non-empty when `AnswerKind` is `Choice` and empty otherwise | FR-007 |
| `AllottedMinutes` is absent when the item configures no time | FR-014 |
| An option whose configuration cannot be read is not offered at all; the step reports unavailable | FR-021 |

## GenericRequestCardModel

What the card and its detail page read, in
`MTM_Waitlist.GenericRequest.Controls/Models/GenericRequestCardModel.cs`.

Fields already carried: `RequestNumber`, `Variant`, `WorkCenter`, `ItemName`, `PartNumber`, `DunnageType`,
`ScrapType`, `DieNumber`, `Quantity`, `NeededBy`, `RaisedBy`, `RaisedAt`, `History`.

Fields added by this change:

| Field | Shape | Why |
| --- | --- | --- |
| `CategoryText` | string | the category wording, drawn where the other cards draw it (FR-024) |
| `Description` | string | the requester's description, drawn when given (FR-024) |
| `StatedValue` | string | the request's identity: the answer the requester gave (FR-023) |

`GenericRequestCardVariant` gains one member:

| Value | Cell it draws |
| --- | --- |
| `Answer` | the configured answer the requester gave |

Validation rules:

| Rule | Source |
| --- | --- |
| `StatedValue` and `WorkCenter` are the card's identity | FR-023 |
| The model never carries a work-order, die, coil, flatstock or component value for a generic request | FR-022 |
| A cell whose value is absent is not returned, so nothing renders blank or placeholder | FR-025 |
| `RaisedBy`, `RaisedAt` and `History` appear on the detail page only | FR-026 |
| Every value comes from the stored row, the requester's answer or configuration — never a sample or substitute | FR-028 |

## SampleOrder

The board's row model, in `MTM_Waitlist.Waitlist.View/Models/SampleOrder.cs`.

Fields added by this change:

| Field | Shape | Why |
| --- | --- | --- |
| `IsGeneric` | bool | lets the board pick the generic card template without a second read (FR-022, R9) |
| `GenericAnswer` | string | the stated value, carried from the row to the card (FR-023, R10) |
| `Description` | string | the requester's description as the list read returns it (FR-024) |
| `AllottedMinutes` | `int?` | the estimate, so the board shows the same figure for both flows (FR-013) |

Validation rules:

| Rule | Source |
| --- | --- |
| `IsGeneric` is `false` for every work-order row | FR-022, SC-006 |
| A generic row carries no work-order value, so no work-order cell is drawn from it | FR-022 |
| `AllottedMinutes` stays absent when the item configures no time | FR-014 |

## WorkCenterDetail and WorkCenterSelectionItem

Both in `MTM_Waitlist.Shared/Models/`, and both gain:

| Field | Shape | Why |
| --- | --- | --- |
| `IsGeneric` | bool | the designation, read once with the catalog the work center step already loads (FR-001, R8) |

Validation rules:

| Rule | Source |
| --- | --- |
| `IsGeneric` is `false` unless a permitted person marked the work center | FR-003 |
| Clearing the designation does not alter a request already raised | FR-004 |

## State transitions

**The draft's outcome.** `GenericRequestDraft.ResultPresentation` moves out of `Empty` exactly once, when the
summary step asks the queue to write.

| From | To | Condition | What it carries |
| --- | --- | --- | --- |
| `Empty` | `Populated` | the write succeeded | `RaisedRequestNumber`, the queue's own identifier for the request |
| `Empty` | `Unavailable` | the write failed | the attempt time and a retry |
| — | `Submitting` | the write is in flight | Continue stays disabled until the write answers |

`Empty` means nothing has been raised yet. `Unavailable` is a per-step state with the time of the attempt and a
retry; it is never a persistent banner (FR-029).

**A request row's own lifecycle.** The stored request's status vocabulary is unchanged: this feature adds a kind
of request, not a new set of states. A generic request enters the queue in the same initial state as any other
request, is worked by the existing handler flow (FR-027), and records its history the same way.

## What this feature does not model

The four externally proved answers — part and stage with sequence, dunnage part, scrap type and lugger, and die —
have no entity here. Their answer steps exist and report their source as not yet available (FR-012); their
entities arrive with chunks 017 to 020.
