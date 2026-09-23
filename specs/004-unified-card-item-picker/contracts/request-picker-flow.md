# Contract — Request picker flow

The UI contract: the steps a person moves through, what each step offers, and — for every Item — the rule that
gates it and the answer it captures. A consumer or a test codes against the identifiers on this page.

## 1. The steps

```text
Work Centre ──▶ Category ──▶ [availability resolved] ──▶ Item ──▶ [Dunnage | Die | Component] ──▶ Details ──▶ Preview ──▶ Summary ──▶ Result
```

The **Dunnage**, **Die** and **Component** steps are entered only when the chosen Item's configuration names the
matching job list as the source of its answer (§4); every other Item goes from Item straight to Details or Preview.
Nothing else about the step order changes.

| Step | View model | Offers |
|---|---|---|
| Work Centre | `NewRequestWorkCenterViewModel` | unchanged |
| Category | `NewRequestJobTypeViewModel` (re-laid) | the four Categories, each filtered by the availability pass (§3) |
| Item | `NewRequestItemViewModel` (new) | the Items of the chosen Category that the requesting job supports, in the Item's `Order` |
| Dunnage | `NewRequestDunnageViewModel` (new) | the dunnage parts the requesting job carries, as picture cards, plus the substitute picker (§9) |
| Die | `NewRequestDieViewModel` | the dies the requesting job carries, as cards naming each die's number and where it lives, with a **select all** action (FR-054) |
| Component | `NewRequestComponentViewModel` | the components the requesting job carries, as one **clickable box** each — the part number, and a picture box that draws the shared no-image placeholder until part numbers can be pictured — so the operator no longer picks a part number out of a drop-down list. The question is the **row's own prompt**, because the two component rows ask it differently (one collects, one brings) |
| Details | `NewRequestDetailsViewModel` (re-laid) | the Item's configured prompt, limits and fields — nothing when the Item requires no answer |
| Preview / Summary / Result | unchanged | unchanged |

The Details step keeps its own control for an enumerated answer that names no job list of its own, so a row added
later still has a step to land on without a build (FR-015).

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
| `pickup-die` | `HasDie` | pick **which die** — the job's dies are offered as cards, more than one may be chosen, and a **select all** action takes every die in one action; one request per die (D22) | the die's own number — `FGT0002000` (§10) |
| `pickup-component` | `HasComponent` | pick which component, from the job's list — on the **component step**, as one clickable box per component the job carries (2026-09-22) | the component's part number |
| `pickup-fg` | **out of scope** | — | — |
| `pickup-ncm` | **out of scope** | — | — |
| `pickup-wip` | **out of scope** | — | — |
| `pickup-outside-service` | **out of scope** | — | — |
| `pickup-riser-table` | always offered | — | `Riser Table` (fixed) |
| `pickup-dunnage` | `HasDunnage` | pick which dunnage they need, from the parts the job carries — on its own step, with the substitute picker beside it | the part they picked |
| `pickup-scrap` | a **real scrap decision** on the job (§5) | — | the job's scrap type |
| `pickup-hopper` | always offered | — | `Hopper` (fixed) |
| `deliver-coil` | `HasCoil` | — | the coil number |
| `deliver-riser-table` | always offered | — | `Riser Table` (fixed) |
| `deliver-hopper` | always offered | — | `Hopper` (fixed) |
| `deliver-flatstock` | `HasFlatstock` | — | the flatstock part number |
| `deliver-component` | `HasComponent` | pick which component, from the job's list — the Deliver counterpart of `pickup-component`, added 2026-09-22, asked on the **component step** as one clickable box per component | the component's part number |
| `deliver-die` | `HasDie` | pick **which die**, exactly as `pickup-die` does — FR-054 applies to a die whichever Item raises it, so this Item asks the same question and offers the same select-all action | the die's own number — `FGT0002000` (§10) |
| `deliver-dunnage` | `HasDunnage` | pick which dunnage they need, from the parts the job carries — on its own step, with the substitute picker beside it | the part they picked |
| `deliver-wrong-coil` | `HasCoil` | one short explanation of why the coil is wrong | the **correct** coil being brought |
| `deliver-wrong-flatstock` | `HasFlatstock` | one short explanation of why the flatstock is wrong | the **correct** flatstock being brought |
| `assist-coil-turn` | `HasCoil` | — | the coil number |
| `assist-table-place` | `HasAnySubordinate` | — | the part number |
| `assist-table-remove` | `HasAnySubordinate` | — | the part number |
| `other` | always offered | one free-text message | the message |

**The job-independent Items are exactly five:** `pickup-riser-table`, `deliver-riser-table`, `pickup-hopper`,
`deliver-hopper`, `other`. `pickup-scrap` is **not** one of them — it is gated on a real scrap decision.

**Where a thing goes.** Deliver Items are delivered to the requesting work centre, and a die — pickup or deliver —
goes to its own **home location** as the job records it. The person is never asked where anything goes, and **no Item
captures a destination**: the question `pickup-die` used to ask retired 2026-09-20 (D22), which is what freed the
request's one value column to carry the chosen die.

**What the request page shows for a die (FR-057, added 2026-09-20).** A die request's page lists **every** die
location the requesting job carries, not only the location of the die that request names — a handler reading one
request should be able to see where the job's dies live. The job's dies are read **once per work centre**, and a
location the job cannot supply is not invented. A job with several dies therefore produces several requests that all
show the same complete location list.

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
  (the coil, die, component and part Items);
- **one option pick**, where the configuration declares an enumerated answer;
- **one text input**, where the configuration declares a text answer, honouring the configured prompt and the
  configured minimum and maximum length, with a plain-language message when the value is out of range;
- **a message**, where the Item carries its own text and nothing is asked for.

No branch of this step keys on the Item's identity; every branch keys on the configuration row (FR-013).

The dunnage Items are **not** asked here. Their enumerated answer names the job's dunnage list, so it is asked on the
Dunnage step (§1, §9) — the step whose cards can show the parts. By the time the flow reaches this step a dunnage
answer is already captured, so it goes straight on to Preview.

## 9. The Dunnage step

The step exists for one shape of answer: a configuration row that declares an enumerated answer drawn from a named
job list, where that list is the **dunnage** parts assigned to the requesting job.

| What | Rule |
|---|---|
| Which Items use it | those whose answer field names the `dunnage` list (FR-050) — never a list of Item codes in code |
| What it shows | one card per dunnage part the job carries: the part's picture, its name, its part number |
| What it asks | which part the operator needs; the flow does not continue until one is chosen (FR-048) |
| The substitute | a control on the step that opens the receiving dunnage catalogue — parts the job does **not** carry — so a substitute can be used; the part chosen there becomes the request's value exactly as an assigned one does (FR-049) |
| Dismissing the substitute | nothing changes: the step stays as it was, with whatever was chosen before |
| A part with no number | refused, and reported — a part that cannot be the card's identifier is never captured as an empty answer (FR-026) |
| What it stores | the chosen part as the request's one answer; the request does not distinguish an assigned part from a substitute once it is stored |

## 10. The job values a card's lines read (FR-053)

Two of an Item's tokens name values the request never stored — they are the **job's**, not an answer — so they come
from the requesting job's snapshot. `{job_part_number}` is the job's own `part_number`; `{die_number}` and
`{die_location}` name the job's dies — every die the job carries, in the job's own order, each written as its number
and its location (FR-054, FR-057). A job carrying no die row of its own falls back to the snapshot's primary die.

| Rule | Why |
|---|---|
| The job is read **once per work centre per load**, not per row | every row of a work centre needs the same job; a list of N rows costs one read per distinct work centre |
| A value is handed over **only where a template names its token** | an Item that names none of them keeps the card it had, so this cannot change another Item's line |
| A job's dies are **listed in full**, in the job's own order | a job carrying several dies shows **every** one of them and where each lives, never only the first — the request page must not hide the rest of the job's dies (FR-054, FR-057) |
| A job with no die row of its own falls back to the snapshot's primary die | the snapshot's primary die is the only die the job can supply, so nothing is invented for it (FR-057) |
| A job that cannot be read is **logged, not fatal** | the list keeps rendering from what the requests themselves carry |

## 7. Failure behaviour

| Failure | What the person sees |
|---|---|
| The store is unreachable | the existing per-screen unavailable state — `Store`, `LastAttemptUtc`, `RetryCount`, `NextRetryUtc` and a manual retry — never an empty list and never a fabricated row |
| The Item has no configuration row | the Item is reported **unavailable** in plain language |
| A configuration row names an Item that does not exist | nothing breaks; the row is never offered |
| A configuration payload cannot be parsed | a reportable configuration problem in plain language |
| A Line 2 token cannot be resolved | the Item's own display name, with the configuration problem reported |
| A Line 1 token cannot be resolved | the Item's umbrella phrase — no problem is reported, because the phrase alone still says what kind of request this is (FR-005) |
| The requesting job cannot be read while a card is built | the card falls back to what the request itself carries, logged rather than failing the list (FR-026) |
| The substitute dunnage picker cannot open | the step reports it in plain language and the assigned cards stay usable |

Nothing fails silently and nothing appears to succeed when it did not (FR-026).

## 8. Invariants

- **The lifecycle does not change with the Item** (FR-032): the stored statuses `Pending`, `Accepted`, `Completed`,
  `Canceled`, the legal transitions and who may perform them are exactly as they are, whichever Item was chosen.
  `sp_waitlist_request_status_update` is not touched.
- **Every user-visible string** introduced or changed comes from the existing resource mechanism (FR-022).
- **No screen offers a request type or a subtype** (FR-003).
