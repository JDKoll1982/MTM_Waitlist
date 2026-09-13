# Feature Specification: Shared Fuzzy Match Picker and Work Order Search

**Feature Branch**: `005-setup-work-order-search`
**Created**: 2026-09-13
**Status**: Draft
**Input**: The owner's request, verbatim: *"in the work order entry page of the setup workflow, when the user
enters a partial work order i want a fuzzy search modal to pop up with work orders that contain that work order.
This should be a shared feature that has the ability to read any given database table that the caller needs, and
return a fuzzy match. if the user enters an exatct match or a pre formatted exact match then just format the string
and use it. example: i type in 0451 in the work order text box, it will auto complete WO-0451%% and pull fuzzy
search showing all work orders with WO-0451%%."*

## User Scenarios & Testing

Typing a work order is currently all-or-nothing: the field accepts a five- or six-digit number, an optionally
prefixed form, and either resolves to exactly one work order or reports a failure. A person who half-remembers a
number gets a rejection, and the number they can see on the paper in front of them is rarely one they can recall in
full. This feature adds a **picker that completes what they typed**, and builds it as a **shared capability** so the
next screen that needs the same behaviour gets it by describing its own list rather than by growing another modal.

### User Story 1 - Type part of a work order and pick from the matches (Priority: P1)

A person arrives at the work order step knowing the number starts `0451` but not the rest. They type `0451`, leave
the field, and a modal lists the work orders that begin that way. They recognise theirs, choose it, and the flow
continues with that work order as though it had been typed in full.

**Why this priority**: this is the request. A partial number is the normal case on the floor, and today it is a dead
end.

**Independent Test**: on the work order step, type a partial number, leave the field, and confirm the modal lists
the matching work orders; choose one and confirm the flow continues with it.

**Acceptance Scenarios**:

1. **Given** the work order step, **When** a partial number is entered and the field is left, **Then** a modal is
   shown listing every work order matching it.
2. **Given** the modal is showing matches, **When** one is chosen, **Then** the entry becomes that work order and
   the flow continues exactly as if it had been typed.
3. **Given** the modal is showing matches, **When** it is dismissed, **Then** the entry and the flow are unchanged,
   and the person can search again.
4. **Given** a partial number that matches nothing, **When** the field is left, **Then** that is stated in plain
   language with an offer to try again — never an empty list and never a silent failure.
5. **Given** a partial number that matches so many work orders that they cannot all be shown, **When** the modal is
   shown, **Then** it stays usable and says how many matched, rather than becoming an unbounded list.

### User Story 2 - A complete work order is used straight away (Priority: P1)

A person who already knows the whole number types it and carries on. No modal interrupts them, because there is
nothing to choose between.

**Why this priority**: the picker must not tax the person who does not need it. Getting this wrong turns a
convenience into a toll on every entry.

**Independent Test**: enter a work order in each accepted form and confirm the next step is reached with no modal
appearing at any point.

**Acceptance Scenarios**:

1. **Given** the work order step, **When** a complete work order is entered in any accepted form, **Then** it is
   formatted and used directly and no modal is shown.
2. **Given** a complete work order that is already correctly formatted, **When** it is entered, **Then** it is used
   as entered — reformatting must not change it.
3. **Given** a complete work order that resolves to a real work order, **When** it is entered, **Then** the flow
   advances without the person having to acknowledge a modal.
4. **Given** an entry that cannot be a work order at all, **When** the field is left, **Then** the existing
   plain-language rejection is shown and no modal is raised.

### User Story 3 - The next screen gets the same picker without a new one (Priority: P2)

A later screen needs the same "type part of it, pick from the matches" behaviour against a different list. It
describes its own list and gets the same behaviour, and the picker itself is not edited to accommodate it.

**Why this priority**: the owner asked for this explicitly, and it is what stops the repository accumulating one
bespoke search modal per screen. It is P2 because the first caller is what proves it works.

**Independent Test**: add a second searchable source and confirm the picker's own code is unchanged, the second
caller behaves identically, and removing the second source leaves the first unaffected.

**Acceptance Scenarios**:

1. **Given** a second caller with its own list and its own value to match, **When** it uses the picker, **Then** the
   behaviour is the same and the picker's own code is not modified.
2. **Given** a caller names a list the picker does not know, **When** it searches, **Then** that is reported as
   unavailable rather than silently returning nothing.
3. **Given** the picker is asked for matches, **When** it searches, **Then** it does not need to know what a work
   order is in order to answer.

### Edge Cases

- **A four-digit partial (`0451`).** Too short to be a complete work order, so it is a *partial*: it becomes a
  search for `WO-0451` and is **not** zero-padded to six digits. This is the owner's own example.
- **A five-digit entry (`76951`).** Complete — it formats to `WO-076951` and is used directly.
- **A six-digit entry with a leading zero (`076951`).** Complete, and formats to `WO-076951`.
- **An already-prefixed entry (`WO-076951`).** Complete and already correctly formatted; used as entered.
- **A partial shorter than four digits (`045`).** Still a partial, so it searches; it will usually match nothing,
  which is reported plainly.
- **An entry that cannot be a work order at all (`ABCD`).** Not a partial — no search is run and the existing
  rejection is shown.
- **A complete form that does not exist.** It formats correctly but resolves to nothing; the person is told, and the
  search is available as the way forward.
- **A partial matching exactly one work order.** The modal is still shown, so behaviour is uniform and the person
  always sees what they are accepting.
- **The entry is edited while the modal is open.** The modal closes and the new text searches when the field is
  left again.
- **The searched list is unreachable.** Reported as the existing per-screen unavailable state with a manual retry —
  never as "no matches", which would be a false answer.
- **The searched list is reachable but has no match.** A real answer: the person is told there is no match. It must
  not be replaced by cached rows.

## Requirements

### Functional Requirements

- **FR-001**: The work order entry step MUST offer matching work orders in a modal when the entered text is a
  partial work order.
- **FR-002**: The modal MUST be shown only when the entry is not already a complete, resolvable work order.
- **FR-003**: A complete work order MUST be formatted and used directly, without the modal, and without requiring
  the person to acknowledge anything.
- **FR-004**: The accepted input forms MUST remain the ones the step already accepts, and the formatting rule MUST
  remain unchanged.
- **FR-005**: A partial entry MUST be turned into a search pattern by adding the work-order prefix and a trailing
  wildcard, so that entries beginning with the typed value are found.
- **FR-006**: A partial entry MUST NOT be padded. Padding applies only when the entry is complete.
- **FR-007**: The matches MUST be presented as the person would recognise them, with enough of each work order
  shown to choose between near-identical matches.
- **FR-008**: Choosing a match MUST set the entry to that work order and continue the flow exactly as though it had
  been typed.
- **FR-009**: Dismissing the modal MUST leave the entry and the flow unchanged and MUST NOT trap the person.
- **FR-010**: A search that finds nothing MUST be reported in plain, actionable language with a way to try again —
  never as an empty list, and never as a silent failure.
- **FR-011**: A search whose result cannot be shown in full MUST remain usable and MUST state how many matched.
- **FR-012**: The matching MUST be provided by one shared capability. The entry step MUST NOT contain its own
  work-order matching logic.
- **FR-013**: The capability MUST accept a **caller-described source** — the list to search and the value to match —
  and MUST return matches in a shape the caller can use, without knowing what the caller's values mean.
- **FR-014**: Adding a new searchable source MUST NOT require changing the capability's own code; it MUST be an
  additive act by the caller.
- **FR-015**: A source the capability does not know MUST be reported as unavailable, never silently searched and
  never silently empty.
- **FR-016**: When to search, what pattern to search with, and how many matches to ask for MUST be the caller's to
  state, not fixed inside the capability.
- **FR-017**: Matching MUST be case-insensitive, and MUST tolerate the separators a person actually types.
- **FR-018**: Every data operation MUST go through a stored procedure. No inline or hard-coded statement text may be
  introduced, and no statement may be assembled at run time from a supplied list name.
- **FR-019**: Searching a list that lives outside the application's own stores MUST keep the existing cached-fallback
  behaviour: a live read is attempted, and only genuine unreachability is served from the mirror with the same
  result shape. A reachable-but-empty result is a real answer and MUST NOT be replaced by cached rows.
- **FR-020**: Every schema artifact added, changed or removed MUST ship its matching rollback, and the schema master
  lists MUST be kept in sync.
- **FR-021**: Every user-visible string introduced or changed MUST come from the existing resource mechanism.
- **FR-022**: The step's existing behaviour for an entry that cannot be a work order at all MUST be preserved,
  including its plain-language message.

### Key Entities

- **Searchable source** — a list a caller wants matched against, described by the caller: what identifies it, which
  value is matched, how the value is displayed, and how many matches may be returned. The capability's unit of
  extension.
- **Match request** — what a caller asks for: the source, the value as typed, and the pattern form to use.
- **Match** — one row the caller can act on: the value that matched, and enough display context to choose between
  near-identical entries.
- **Work order** — as the step already understands it: a six-digit number, optionally prefixed, formatted one way
  and displayed another.
- **Partial entry** — text that could become a work order but is not yet one. Distinguished from an *invalid* entry,
  which cannot become a work order however it is completed.

## Success Criteria

### Measurable Outcomes

- **SC-001**: From the work order step, a partial number yields a modal listing matching work orders, and choosing
  one advances the flow with that work order stored — verified with a partial that matches several and a partial
  that matches one.
- **SC-002**: A complete work order in each accepted form reaches the next step with zero modals shown.
- **SC-003**: One implementation of matching exists in the codebase; zero other screens or services implement their
  own.
- **SC-004**: A second searchable source is added without editing the capability's own code, demonstrated by adding
  one.
- **SC-005**: Zero schema artifacts ship without a matching rollback, and the master lists agree with what is on
  disk.
- **SC-006**: Zero run-time-assembled statements and zero inline statements are introduced; the repository's inline
  audit passes.
- **SC-007**: An unreachable external source with mirror data still returns matches, and a reachable-but-empty
  source returns none rather than cached rows.
- **SC-008**: The full test suite reports no failures, and the solution builds with no warnings and no errors.
- **SC-009**: Zero user-visible strings introduced or changed by this feature are unlocalized.

## Assumptions

- **The picker is shared, but ships one caller.** The owner asked for a capability that can serve any list the
  caller needs. This specification therefore fixes the *contract* as generic and ships the work-order caller only.
  Adding a further caller is deliberately left to the work that needs it, so the contract is proven by a real second
  use rather than by a speculative one. **The heading of the feature directory still names the work-order case, even
  though the deliverable is now the shared picker.**
- **Prefix matching was chosen over contains-anywhere.** The owner's message says both "work orders that contain
  that work order" and "WO-0451%%". The worked example is read as authoritative: it is an *auto-completion*, so the
  pattern completes the prefix. The capability carries the pattern form as a caller-supplied input (FR-016), so
  switching a source to contains-anywhere is a caller change, not a capability change. **This is the decision most
  worth the owner confirming.**
- **The modal is raised on leaving the field.** The step already searches when the field loses focus; reusing that
  moment adds the picker without changing when the current search runs. Searching on every keystroke would put a
  modal in front of a moving caret.
- **One match still shows the modal.** Uniform behaviour is worth more than saving one click, and it means the person
  always sees the value they are accepting.
- **The searched list is the list the step already validates against.** The picker does not introduce a second
  source of truth for what a work order is.
- **The existing fuzzy matcher is a candidate for reuse, not a mandate.** The repository already has a
  Levenshtein-based best-match helper used for a different purpose. Whether the picker reuses it or the source's own
  query does the matching is a design decision for the plan; what the specification requires is the observable
  behaviour above.
- **The order of matches is not specified here**, beyond being stable between two identical searches. The plan fixes
  it.
- **The database is reinstalled from seed; no migration is written**, consistent with this repository's existing
  position. The reinstall is the owner's action, never the agent's.

## Verbatim Constraints

These strings are values the result must match exactly. None may be renamed, recased or reformatted.

**The accepted input forms**, as the step's own placeholder states them: `76951`, `076951`, `WO-076951`.

**The formatted work order**: `WO-` followed by the digits zero-padded to six — `76951` becomes `WO-076951`. This
rule is unchanged by this feature.

**The work-order prefix**: `WO-`.

**The owner's worked example, verbatim**: typing `0451` autocompletes to `WO-0451%%` and searches for all work
orders with `WO-0451%%`. Note that `%` is the search wildcard and a doubled `%%` denotes the same pattern as a
single one, so the derived pattern is `WO-0451` followed by one trailing wildcard. The digits are **not** padded to
six here, because `0451` is a partial and not a complete work order.
