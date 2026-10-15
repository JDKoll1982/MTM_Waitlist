# Contract - The status vocabulary, its precedence and its themes

What the status zone reads and how it is coloured. Every word below is copied verbatim from `spec.md` (FR-011 to
FR-023, FR-070 to FR-076, FR-102 and the Verbatim Constraints).

## 1. The vocabulary: eight states in three roles

**The four lifecycle words, in friendly display wording** (FR-012):

| Lifecycle word |
|---|
| `Waiting` |
| `In Progress` |
| `Done` |
| `Cancelled` |

These are display text. They must never be written to the store and must never be compared as though they were a
stored status.

**The overdue marking:**

| Marking |
|---|
| `Overdue` |

**The four handler-set conditions** (FR-071):

| Condition |
|---|
| `Product Buried` |
| `Cant Find Product` |
| `Changing Forklift` |
| `Changing Propane` |

The four lifecycle words plus the four conditions open the vocabulary from four states to eight, and every reader of
the status palette must now handle all eight (FR-102).

**These eight words are the store's vocabulary and are deliberately not localized.** They are literals in code
(`SampleOrder.LifecycleWordFor` holds the four lifecycle words, `HandlerStatusCatalog.Conditions` the four
conditions, and `SampleOrder` the `Overdue` marking), they are compared **ordinally and exactly** against a stored
value, and they are written verbatim. A differently cased, padded or otherwise altered value is **not** the same
state: `HandlerStatusCatalog.IsCondition` and the palette's own role lookup both reject it, and the tests pin every
one of the eight verbatim and assert that the near-misses fall through. Turning them into resource keys would make a
display string decide a comparison, which is what FR-012 and FR-102 exist to prevent. **FR-104 is therefore met in
part**: the ring's accessible name, `Open details`, the Print message, the new cell labels and the die-shop phrases
are localized resources, and the eight words above are not, by this decision.

## 2. Precedence

Row 1 reads, in precedence order (FR-012, FR-023):

1. the handler-set condition, when one is set;
2. otherwise `Overdue`, when the request is overdue;
3. otherwise the lifecycle word.

A handler-set condition and an overdue marking are **never both shown**, and the condition takes precedence for as
long as it is set. A request that is both overdue and carrying a condition reads only the condition and is not themed
as overdue (FR-023, FR-014 counterpart). This is a deliberate, visible trade-off: a condition hides the lifecycle
state on the card for as long as it is set.

## 3. The three column themes

The whole status column carries one of three distinct themes (FR-019):

| Theme | Applies when |
|---|---|
| normal | the word is a lifecycle word and the request is not overdue |
| handler-condition, a **lighter red** than the overdue theme | a handler-set condition is shown in row 1 (FR-072) |
| overdue | the request is overdue and no handler condition is set (FR-014) |

An overdue request remains distinguishable as its own state under every sort and never depends on the row's position
(FR-020).

## 4. The ring

- The ring fills as the allotted time is used and carries the time left inside it (FR-013).
- An overdue request shows the time overrun in its ring (FR-014).
- A closed request freezes the ring at the moment it closed and carries the time taken instead of the time left
  (FR-015).
- The ring states which of the two things it is measuring, and its accessible name says the same (FR-016).
- The ring re-evaluates on the same one-minute tick that refreshes the row's other time text, so the ring and that
  text never disagree (FR-017).
- An Item with no configured allotment uses the labelled default allotment of **15 minutes**.
- The status zone shows no percentage and no elapsed-time line (FR-021).

## 5. The person rows

- Row 3 reads the requester while the request is unclaimed and the material handler once it is claimed (FR-022).
- A closed request shows the material handler, or the person who cancelled, in row 3, with the requester in row 4
  (FR-022).
- A status row with no value is removed rather than left blank (FR-018).

## 6. Who may set a condition

- When the signed-in person is the material handler working on that request, the status word becomes clickable
  (FR-070).
- Clicking the status word opens a flyout offering the four conditions in the order listed in section 1 (FR-071).
- Choosing a condition re-themes the whole status column to the handler-condition theme and is stored against the
  request, so it is restored onto that row when the list is next read (FR-072, FR-073).
- The status word is inert for everyone else, both for another person and on a request the signed-in person is not
  working (FR-074).
- Only the row's own material handler can set that row's condition (FR-076).
- The flyout is reachable and operable by keyboard alone, and whether the word is operable is announced rather than
  left to be discovered (FR-075).

## 7. Where the values are stored

- The four conditions are stored in the nullable `handler_status` column on `waitlist_requests_queue`, written by
  `sp_waitlist_request_handler_status_update` and returned by `sp_waitlist_request_list`. See
  `contracts/persistence.md`.
- The lifecycle status is kept intact underneath a condition, because the precedence rule in section 2 needs the
  lifecycle word. A condition is never written over the lifecycle status.
