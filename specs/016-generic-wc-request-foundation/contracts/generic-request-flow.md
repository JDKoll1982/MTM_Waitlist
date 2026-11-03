# Contract: Generic Request Flow

The interface this feature exposes: the order a generic request visits its steps, how the flow is entered, the
two seams a step talks to (`IGenericRequestNavigator` and `IGenericRequestStepData`), and the resource keys every
screen draws from.

Sources: [spec.md](../spec.md) FR-005 to FR-021, [plan.md](../plan.md) "Interaction path" and "States",
[research.md](../research.md) R1, R4, R5 and R7.

## Step order

`GenericRequestStep` declares the steps in the order the flow normally visits them. `GenericRequestFlowRules.Order`
decides the order for one draft, and no view model hard-codes a successor.

| `GenericRequestStep` | Visited | Condition |
| --- | --- | --- |
| `WorkCenter` | always | the first step; the work center the request belongs to |
| `Category` | always | the same categories a work-order request offers (FR-006, A4) |
| `Item` | always | the item, and the one question its configuration declares (FR-006, R4) |
| `Part` | conditional | the chosen item needs a proved part |
| `Dunnage` | conditional | the chosen item's `NeedsDunnage` flag |
| `Scrap` | conditional | the chosen item's `NeedsScrap` flag |
| `Die` | conditional | the chosen item's `NeedsDie` flag |
| `Details` | always | the required needed-by answer and the optional description (FR-015, FR-016) |
| `Summary` | always | every answer reviewed before the request is raised (FR-017) |
| `Result` | always | the request number, or the failure with a retry |

Conditional means the chosen item says so, so a request that needs a die never shows the dunnage step and a request
whose item needs none of the four visits four steps rather than eight. `WorkCenter`, `Category`, `Item`, `Details`,
`Summary` and `Result` are always visited: they are what every request has.

The four conditions read the item's own flags, which arrive with `GenericRequestItemOption`. `NeedsPart`,
`NeedsDunnage`, `NeedsScrap` and `NeedsDie` are derived from the item's own card line 2 template, which is the
catalogue's existing statement of what the request has to state: a `{part_number}`, `{die}`, `{dunnage_part}` or
`{scrap_type}` token names the answer kind that item needs, and a literal or `{answer}` names none of the four. So an
item that states a configured answer never sees the part step, and an item that needs a proved part sees it and is told
that source is not yet available (FR-012, R5).

## Entry point

`NewRequestWorkCenterViewModel.SelectWorkCenter` decides which flow runs, on the work centre the requester picked:

| `WorkCenterSelectionItem.IsGeneric` | Navigation target |
| --- | --- |
| `true` | `MTM_Waitlist.GenericRequest.ViewModels.GenericRequestWorkCenterViewModel` |
| `false` | `MTM_Waitlist.Module_Waitlist.ViewModels.NewRequestJobTypeViewModel` |

`IsGeneric` is read once with the catalogue the step already loads, through `WorkCenterDetail` and
`WorkCenterSelectionItem` (R8). A generic selection continues without an active job and never fails for a missing
one (FR-005); a non-generic selection runs the existing work-order flow unchanged (FR-010, SC-006).

The target names above are the page keys `PageService` resolves. The step view models ship in
`MTM_Waitlist.GenericRequest.ViewModels`, and the generic pages carry a parameterless constructor that asks
`GenericRequestPageServices.Get<T>()` for its view model, so the host registers the view models and calls
`GenericRequestPageServices.Configure(IServiceProvider)` once at start-up (R7).

## Navigator seam

`MTM_Waitlist.GenericRequest.Contracts.IGenericRequestNavigator` moves the flow and carries the answers given so
far. A step asks this to advance and never touches a frame or a page.

| Member | Meaning |
| --- | --- |
| `Draft` | the answers given so far; a step writes its own answer and no step clears one |
| `CurrentStep` | the step on screen |
| `GoTo(GenericRequestStep)` | show the step given; which step comes next is `GenericRequestFlowRules`'s question |

The application implements it as `MTM_Waitlist.Services.GenericRequestNavigator` over
`MTM_Waitlist.Module_Core.Contracts.Services.INavigationService`, mapping each step to its view model's full name.
Hosting the steps inside one page instead was rejected: it would duplicate the shell's navigation, back stack and
transitions (R7).

## Data seam

`MTM_Waitlist.GenericRequest.Contracts.IGenericRequestStepData` is the only door between a step and the data. A
view model asks one of these questions and never a store, a service or a file directly.

| Method | Read |
| --- | --- |
| `ReadWorkCentersAsync` | the designated generic work centres |
| `ReadCategoriesAsync(workCenter)` | the categories a generic request offers at that work centre |
| `ReadItemsAsync(category)` | the items, and the question each declares |
| `ReadPartsAsync(itemCode)` | a proved part and its sequence |
| `ReadSequencesAsync(partNumber)` | the sequences for a proved part |
| `ReadDunnageAsync(itemCode)` | the dunnage part |
| `ReadScrapTypesAsync` | the scrap types and their luggers |
| `ReadDiesAsync(searchText)` | a die |
| `ReadStepAvailabilityAsync(step)` | whether a step that reads no rows can be shown |
| `SubmitAsync(GenericRequestDraft, CancellationToken)` | raise the request; returns a `GenericRequestSubmission` |

Every read returns `GenericRequestStepResult<T>`, which is exactly one of three presentations so an empty read and a
failed read can never both be true:

| Presentation | Factory | What the screen draws |
| --- | --- | --- |
| `Populated` | `Populated(items, attemptedAtUtc)` | the rows |
| `Empty` | `Empty(attemptedAtUtc)` | the step's empty text; there is nothing to retry |
| `Unavailable` | `Unavailable(attemptedAtUtc)` | the attempt time and a retry control (FR-029) |

`AttemptedAtUtc` is carried by all three, so a failure always names when it happened. `Unavailable` is a per-step
state with a retry, never a persistent banner and never a silent blank.

`SubmitAsync` is the one write. It maps the draft onto `WaitlistRequestDraft` with `IsGeneric` set and hands it to
`IWaitlistRequestService.SubmitAsync`, which is the write door every request already uses (R1, FR-009); the summary
step records the returned `GenericRequestSubmission` — the public identifier and number on success, the attempt time
and a retry on failure — so the result step shows what actually happened (FR-018).

The live implementation is `MTM_Waitlist.GenericRequest.Services.GenericRequestLiveStepData`. There is no preview,
sample or cached-substitute implementation: every read is live (FR-028, constitution II).

## Unavailable reads

Part, dunnage, scrap and die reads return `GenericRequestStepResult<T>.Unavailable` in this chunk (FR-012). The step
reports that its answer source is not available, with the attempt time and a retry, and offers no text box, no
offline list and no other substitute. Hiding the four steps instead was rejected: a request whose item needs a part
would reach confirmation without the part, which understates the submission (FR-017) and would change the flow's
shape when chunk 017 lands (R5).

## Resource keys

Every string a step draws comes from `.resw`, never from literal XAML text (constitution V). The shapes:

| Shape | Used for |
| --- | --- |
| `GenericRequest_{Step}_{Title\|Intro\|Empty}.Text` | the step's title, its one-line introduction, and its populated-nothing text |
| `GenericRequest_StepFrame_{Continue\|Back\|Retry}.Content` | the frame's advance, back and retry controls |
| `GenericRequest_Card_*` | every label and value the card and the detail page draw |

`{Step}` values are `WorkCenter`, `Category`, `Item`, `Part`, `Dunnage`, `Scrap`, `Die`, `Details`, `Summary` and
`Result`, so a step's three keys differ from its neighbour's by one word. `GenericRequest_Card_*` covers the card's
labels, values, kinds and detail-page sections; a new card fact adds a key in that shape rather than literal text.
