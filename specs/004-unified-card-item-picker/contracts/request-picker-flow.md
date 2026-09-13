# Contract — Request picker flow

The UI contract: the steps a person moves through, what each step offers, and — for every Item — the rule that
gates it and the answer it captures. A consumer or a test codes against the identifiers on this page.

## 1. The steps

```text
Work Centre ──▶ Category ──▶ [availability resolved] ──▶ Item ──▶ Details ──▶ Preview ──▶ Summary ──▶ Result
```

| Step | View model | Offers |
|---|---|---|
| Work Centre | `NewRequestWorkCenterViewModel` | unchanged |
| Category | `NewRequestJobTypeViewModel` (re-laid) | the four Categories, each filtered by the availability pass (§3) |
| Item | `NewRequestItemViewModel` (new) | the Items of the chosen Category that the requesting job supports, in the Item's `Order` |
| Details | `NewRequestDetailsViewModel` (re-laid) | the Item's configured prompt, limits and fields — nothing when the Item requires no answer |
| Preview / Summary / Result | unchanged | unchanged |

`NewRequestSubtypePage` and `NewRequestSubtypeViewModel` are **gone**; no screen offers a request type or a subtype
(FR-003).

## 2. The four Categories

`Pickup`, `Deliver`, `Assist`, `Other` — the Category step's order is exactly this.

## 3. The availability rule, and where it runs

**It runs before the Item list is built, never after** (FR-002). The snapshot is filled once, at the composition
root, by mapping `IActiveJobItemResolverService` onto `RequestJobPartAvailability`; the Item list is then assembled
through `INewRequestPickerService.GetVisibleItems(category, availability)`, so an unsupported Item is never
constructed, never bound and never offered, and is therefore never offered and then refused.

**The Category list is filtered by the same pass** (D20): a Category that would offer no Item is not offered, which
is what makes FR-002's "MUST never be empty" and SC-005's "at least one Item in every case" true of every Item step
a person can reach.

The snapshot is compared against these flags, which the resolver derives from the job's subordinate parts
(`MMC→Coil`, `MMF→Flatstock`, `FGT→Die` from the part-number prefix; otherwise the stored category, else
`Component`):

| Flag | Meaning |
|---|---|
| `HasActiveJob` | the work centre has an active setup job |
| `HasCoil` | the job has an `MMC` subordinate |
| `HasFlatstock` | the job has an `MMF` subordinate |
| `HasDie` | the job has an `FGT` subordinate |
| `HasComponent` | the job has some other subordinate part |
| `HasDunnage` | the job has an assigned dunnage part |
| `HasAnySubordinate` | any of the four above |

## 4. Per-Item contract

**Availability** is the rule that gates the Item. **Captures** is the one thing the Details step asks for, if
anything. **Line 2** is the card's identifier (see `card-and-identifier.md`). The four Items marked *out of scope*
are catalogued but never offered (FR-028).

| Item | Availability | Captures | Line 2 |
|---|---|---|---|
| `pickup-coil` | `HasCoil` **or** `HasFlatstock` (D21) | — | the coil number |
| `pickup-die` | `HasDie` | pick the die, then the destination from `Die Shop` / `Home Location` / `Other` | the die's **location** when the destination is `Home Location`, otherwise the die's **number** |
| `pickup-component` | `HasComponent` | pick which component, from the job's list | the component's part number |
| `pickup-fg` | **out of scope** | — | — |
| `pickup-ncm` | **out of scope** | — | — |
| `pickup-wip` | **out of scope** | — | — |
| `pickup-outside-service` | **out of scope** | — | — |
| `pickup-riser-table` | always offered | — | `Riser Table` (fixed) |
| `pickup-dunnage` | `HasDunnage` | — | the assigned dunnage part |
| `pickup-scrap` | a **real scrap decision** on the job (§5) | — | the job's scrap type |
| `pickup-hopper` | always offered | — | `Hopper` (fixed) |
| `deliver-coil` | `HasCoil` | — | the coil number |
| `deliver-riser-table` | always offered | — | `Riser Table` (fixed) |
| `deliver-hopper` | always offered | — | `Hopper` (fixed) |
| `deliver-flatstock` | `HasFlatstock` | — | the flatstock part number |
| `deliver-die` | `HasDie` | — | the die's number and its location |
| `deliver-dunnage` | `HasDunnage` | — | the assigned dunnage part |
| `deliver-wrong-coil` | `HasCoil` | one short explanation of why the coil is wrong | the **correct** coil being brought |
| `deliver-wrong-flatstock` | `HasFlatstock` | one short explanation of why the flatstock is wrong | the **correct** flatstock being brought |
| `assist-coil-turn` | `HasCoil` | — | the coil number |
| `assist-table-place` | `HasAnySubordinate` | — | the part number |
| `assist-table-remove` | `HasAnySubordinate` | — | the part number |
| `other` | always offered | one free-text message | the message |

**The job-independent Items are exactly five:** `pickup-riser-table`, `deliver-riser-table`, `pickup-hopper`,
`deliver-hopper`, `other`. `pickup-scrap` is **not** one of them — it is gated on a real scrap decision.

**Deliver destination.** Every Deliver Item is delivered to the requesting work centre; the person is never asked
where. No Deliver Item captures a destination, which is why only `pickup-die` has a destination-dependent Line 2.

**The wrong-material Items are single Deliver requests**, not compound ones: one request, Line 1 is the Item's own
bring phrase, Line 2 names the material being brought — the **correct** one, never the wrong one being collected.

## 5. The scrap decision — the exact rule

`pickup-scrap` is offered **only** when the job has a *real* scrap decision: a value that is **set**, is **not**
`No Scrap`, and is **not** `Scrap Type Required`.

| Job's scrap value | Is the Scrap Item offered? | Why |
|---|---|---|
| a real scrap type | **yes** | a decision was made and something has to be collected |
| `No Scrap` | **no** | a real answer, and a real answer meaning there is no scrap to collect |
| `Scrap Type Required` | **no** | the workflow falls back to this placeholder when nothing has been saved, so a stored placeholder means **no decision was made** — it must never be presented as though it were a scrap type |
| unset | **no** | no decision was made |

The rule is evaluated through the codebase's canonical predicate `HasScrapDecision`
(`MTM_Waitlist.Setup/ViewModels/SetupDunnageTypeViewModel.cs`) and the constants in
`MTM_Waitlist.Setup/Services/SetupWorkflowService.cs`, so Setup and New Request cannot diverge on what a real
decision is.

**The Scrap Item's value is the scrap type already on the job.** The person is never asked to enter it (FR-030).

## 6. The Details step

It renders from the Item's configuration and nothing else:

- **nothing**, when the Item is a pure flag (the fixed-label Items), or when the Item's value is derived from the job
  (the coil, die, dunnage, component and part Items);
- **one option pick**, where the configuration declares an enumerated answer;
- **one text input**, where the configuration declares a text answer, honouring the configured prompt and the
  configured minimum and maximum length, with a plain-language message when the value is out of range;
- **a message**, where the Item carries its own text and nothing is asked for.

No branch of this step keys on the Item's identity; every branch keys on the configuration row (FR-013).

## 7. Failure behaviour

| Failure | What the person sees |
|---|---|
| The store is unreachable | the existing per-screen unavailable state — `Store`, `LastAttemptUtc`, `RetryCount`, `NextRetryUtc` and a manual retry — never an empty list and never a fabricated row |
| The Item has no configuration row | the Item is reported **unavailable** in plain language |
| A configuration row names an Item that does not exist | nothing breaks; the row is never offered |
| A configuration payload cannot be parsed | a reportable configuration problem in plain language |
| A Line 2 token cannot be resolved | the Item's own display name, with the configuration problem reported |

Nothing fails silently and nothing appears to succeed when it did not (FR-026).

## 8. Invariants

- **The lifecycle does not change with the Item** (FR-032): the stored statuses `Pending`, `Accepted`, `Completed`,
  `Canceled`, the legal transitions and who may perform them are exactly as they are, whichever Item was chosen.
  `sp_waitlist_request_status_update` is not touched.
- **Every user-visible string** introduced or changed comes from the existing resource mechanism (FR-022).
- **No screen offers a request type or a subtype** (FR-003).
