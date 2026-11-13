# UI and Data Contract: Generic Work Center — Stated Scrap Type and Lugger

This is the interface feature 019 exposes. Anything below is something a consumer or a test may name directly. Everything else is an internal detail and may change freely.

## Verbatim constraints

These strings and rules are exact and must not be recased, pluralised or reworded:

- `No Scrap` — the answer meaning there is no scrap. Never offered as a destination.
- `Scrap Type Required` — the placeholder meaning no decision was made. Never offered as a destination.
- An outside type is matched to an arrangement entry ignoring surrounding spaces and letter case, as the arrangement editor already matches it (trim, then compare case-insensitively).

## Stored answer contract

- New record `GenericRequestScrapAnswer`, in namespace `MTM_Waitlist.Module_Core.Models`.
- Key constant `scrap.type` — the answers key the stated type is stored under.
- `ToAnswers()` writes only non-empty values into a case-insensitive dictionary, so a request with no stated type writes nothing for scrap.
- `TryRead()` returns null when no type is stated, so callers can tell "no answer" from "empty answer".

No column, table or stored procedure changes. The answer rides the request's existing answers storage (`answers_json`) through the existing insert and list paths.

## Step contract

- The scrap step is `GenericRequestStep.Scrap`.
- Continue is enabled only when the seam reported `Populated` and a type is selected. Any other seam state, or no selection, leaves Continue disabled.
- The chosen type is written to `GenericRequestDraft.ScrapType`.
- The confirmation step already renders the stated type under the resource key `GenericRequest_Summary_Scrap.Label`; no new confirmation resource or step is added.

## Card contract

For a generic scrap request the card declares two cell labels, in this order and in these exact words:

- `Scrap Type`
- `Lugger Location`

Those are the already-declared field labels in the item configuration; they are not new.

The lugger cell has three outcomes, and the rule for telling them apart is:

- Resolved — the arrangement holds rows and resolution names a lugger (including the all-blocked case, where the name arrives with its status beside it).
- Gap — the arrangement holds rows and resolution reports a configuration gap. The gap is recorded once, no lugger is drawn, and the type is still named.
- Unavailable — the arrangement returned no rows (could not be read, or holds nothing). The type is still named and the lugger is reported unavailable, never a stale or guessed one.

## Routes and registrations that do NOT change

Do not add a route, page registration or service registration for this feature. `GenericRequestScrapTypeViewModel` -> `GenericRequestScrapTypePage` is already registered in `Services\DependencyInjection\ServiceRegistrationExtensions.cs`, and the page and view model are already registered there as well. The scrap step is already wired into the flow.

## What a test may rely on

- The order of the choices: the plant's shared scrap-type list order, with arrangement-only types appended.
- The two excluded names: `No Scrap` and `Scrap Type Required` never appear as choices.
- The gate: Continue stays disabled unless the seam is `Populated` and a type is selected.
- The stored key: the stated type is read and written under `scrap.type`.
- The three card outcomes: Resolved, Gap and Unavailable, distinguished by the arrangement rows plus whether a lugger is named.
