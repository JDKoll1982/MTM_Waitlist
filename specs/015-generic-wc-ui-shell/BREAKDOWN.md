# Generic Work Center — Readable Breakdown and Chunk Index

A plain-language map of the whole feature and of the eight specifications it is split into. **This file is a
guide, not a specification.** The pre-split combined specification is preserved at
[PARENT-SPEC.md](./PARENT-SPEC.md) in this folder; the authoritative specifications are the eight chunk files
listed below, and every requirement lives in one of them.

## The idea in one paragraph

Some work centers do not run against a work order. Today the request wizard reads an active setup job to learn
what a request is for; at a generic work center there is nothing to read, so the requester states it
themselves. Everything they state must be provable — a part is proved against Infor Visual and the WIP floor,
which together supply the sequences the part runs at, and the chosen sequence decides where the quantity comes
from: the WIP app by part and sequence while the work is in process, Visual by part number once it is finished,
and nothing at all while it is at an outside vendor. Dunnage comes from the real dunnage catalogue shown as a
page, a die is found by searching Visual, and a scrap type comes from the plant's own scrap-type list. The
result is a normal waitlist request on the normal board, drawn with its own card because a work-order card
would be full of cells that do not apply.

## The eight chunks, in implementation order

Each chunk is independently reviewable and testable, and **no chunk needs a later one**. Read down the
"needs" column: every entry is a lower number.

| # | Spec | What it delivers | Needs | Why this position |
|---|---|---|---|---|
| 1 | [015-generic-wc-ui-shell](../015-generic-wc-ui-shell/spec.md) | **Every screen and both cards, laid out and navigable**, rendered from an in-memory preview host. No database, no queue write, no permission check. | nothing | The owner's instruction: see the layout before any data or logic is written. |
| 2 | [016-generic-wc-request-foundation](../016-generic-wc-request-foundation/spec.md) | The generic designation on the work center catalog, the flow that continues without a job, the same categories and items, requester-answered questions, allotted time, needed-by, confirmation, the real queue write, and the truthful generic card. | 015 | Nothing else can be attached until a generic request can really exist. |
| 3 | [017-generic-wc-stated-part](../017-generic-wc-stated-part/spec.md) | The stated part: completions from both systems, the sequence list, the chosen sequence, and the quantity read from the system that owns it — including outside service reading as not inventoried. | 015, 016 | The most-used answer, and the one every other item's shape follows. |
| 4 | [018-generic-wc-dunnage-page](../018-generic-wc-dunnage-page/spec.md) | The dunnage catalogue as a wizard page rather than a dialog, drawing the same source the substitute picker uses. One part per request. | 015, 016 | Independent of the part work; dunnage is a category the plant raises every shift. |
| 5 | [019-generic-wc-scrap-type](../019-generic-wc-scrap-type/spec.md) | The requester states the scrap type from the plant's one scrap-type list, and the card resolves the lugger from the arrangement that already governs every scrap card. | 015, 016 (+ 013, shipped) | Scrap is a physical pickup that cannot be deferred. |
| 6 | [020-generic-wc-die-search](../020-generic-wc-die-search/spec.md) | A die search page taking an FGT number or a part number, listing every die a part owns with its location, one request per chosen die. | 015, 016 | The most intricate lookup, and the only one that multiplies requests. |
| 7 | [021-generic-wc-optional-job-fill](../021-generic-wc-optional-job-fill/spec.md) | The optional offer to fill a generic request from a job that happens to be running. Declining changes nothing, and the job becomes provenance, never identity. | 016 | A convenience over a working flow; adds nothing the earlier chunks need. |
| 8 | [022-generic-wc-temp-to-live-verification](../022-generic-wc-temp-to-live-verification/spec.md) | **Proves the conversion happened**: no `Temp` artefact remains, and every screen behaves on live data the way the owner approved it on preview. | 015 to 021 | It asserts an end state, so it can only come after everything it asserts about. |

Chunks 3 to 6 are siblings: none needs another, so their order after 016 is a judgement about value, not a
dependency. If one is dropped, the other three still ship — and chunk 022 then verifies the dropped one's screen
reports its source as unavailable rather than that a temporary source survived it.

## What each chunk adds to the card

The card grows a cell at a time, and each chunk owns only its own cell. Nothing else touches the card.

| Chunk | Adds to the generic card | `Temp` artefacts it removes |
|---|---|---|
| 015 | The whole layout, previewable in part, dunnage, scrap and die variants — preview only | none; it creates them |
| 016 | Real identity (stated value + work center), category and item wording, needed-by, description | the work center, category, item and card scenarios |
| 017 | The part, the chosen sequence, the quantity and the system that supplied it; outside service reads *not inventoried* | the part and sequence scenarios |
| 018 | The chosen dunnage part | the dunnage scenario |
| 019 | The stated scrap type and the lugger the arrangement assigns | the scrap scenario |
| 020 | Each die and that die's location, one card per chosen die | the die scenario |
| 021 | Nothing — the job is provenance on the detail page, never a card cell | any remaining ones |
| 022 | Nothing — it proves absence and behaviour, it changes no cell | asserts that none remain, and that every cell matches what was approved |

## How anything temporary is named, and why one prefix is not enough

The owner asked for "mock data derived from a mock model" to preview layout, and then proposed that anything
temporary carry a `temp_` / `temp.` prefix so it is easy to find and convert later. **The convention is adopted
— but the prefix alone does not clear the constitutional objection, and it is important to say why.**

The prohibition in Principle II and `RetiredSymbolAuditTests` is not a naming rule. It exists because fake rows
have reached a shop-floor screen before. A service called `temp_PartSource` registered in the application's DI
would feed a handler invented stock just as effectively as one called `SampleDataService`; the audit would pass
simply because its regex no longer matches. A rename that silences a check is worse than the check, because the
guard was the only thing standing between preview data and a handler.

So the convention is adopted **with the two things that make it a control**:

1. **It is confined.** Everything temporary lives in one preview host project whose assembly, root namespace and
   folder all carry `Temp`, and the shipping application does not reference it — directly or transitively.
2. **It is enforced.** A build-time check fails, naming the offending artefact, if any `Temp` artefact appears
   where the application can reach it. Exemptions are listed one at a time, never by pattern, so an exemption
   cannot quietly widen.

The shapes, because this repository's naming guard allows `Temp` in a PascalCase type but not a `temp_` prefix
on one:

| Kind | Shape | Example |
|---|---|---|
| Type, interface, enumeration | PascalCase `Temp` prefix | `TempGenericRequestPartSource`, `ITempGenericRequestPartSource` |
| Namespace | trailing `.Temp` segment | `MTM_Waitlist.Waitlist.GenericRequest.Temp` |
| Project, assembly, folder, file | `Temp` as the first segment | `MTM_Waitlist.Waitlist.GenericRequest.Temp` |
| XAML `x:Name` | PascalCase `Temp` prefix | `TempPartList` |
| Resource key | `Temp` as the feature segment | `TempGenericRequest_Step.Title` |
| Database object | `temp_` prefix | `temp_generic_request_stage` |

Two further rules keep the convention honest rather than decorative:

- **The temporary material is never called sample data or mock data.** It is identified by `Temp` and nothing
  else, so the retired vocabulary stays retired and the audit keeps its force over everything the rebuild did
  not reuse.
- **Conversion is deletion, not renaming.** When a real implementation lands, its `Temp` counterpart is removed
  and no other edit is needed. Nothing may outlive it — no registration, key, reference or test — which is why
  spec 015 FR-026 forbids exactly that.

Each chunk from 016 onward deletes the `Temp` artefacts belonging to the screens it makes real, and states that
deletion as a requirement rather than an intention. Chunk 022 then owns the end state: it proves that no `Temp`
artefact remains anywhere, and that every screen behaves on live data the way the preview was approved — because
absence of fake data is worth nothing if the real data lands in the wrong cell.

## Where the facts behind the design came from

Not requirements — the existing code each claim leans on, so a chunk knows what it is changing away from.

| Claim in the specs | What backs it today |
|---|---|
| Work-order requests read an active setup job | `MTM_Waitlist.Waitlist.NewRequest` flow rules and the job availability provider behind the wizard. |
| Part stock can be read and does not invent a number | `IPartStockService` and the part stock window the Deliver Component card already opens. |
| Parts can be searched live in Visual | The existing part-match search, deliberately live-only with no cached mirror. |
| A part's sequences are readable from Visual | `REQUIREMENT.OPERATION_SEQ_NO` and `OPERATION.SEQUENCE_NO`. `GetSequences.sql` already lists them for a work order **and** part; the per-part list is that query without the order predicate. |
| A sequence's stage is readable from Visual | `OPERATION.STATUS` is a single character; the live-confirmed mapping — open `R`/`U`/`F`, closed `C`, cancelled `X` — is recorded in `RequestDispositionStatusCodes`. |
| The WIP floor holds stock against an operation | `inv_inventory` carries `Operation varchar(100)` beside `PartID`, `Location` and `Quantity`; live rows hold `29`, `20`, `10`, with `N/A` on dunnage rows. |
| WIP items are inventoried only in the WIP app | Owner, 2026-10-29: a person enters the part number and the sequence into a location; Visual is not used to inventory WIP. |
| Outside-service material is invented nowhere | Owner, 2026-10-29: outside service is not inventoried at all, which is why 017 reports *not inventoried* rather than `0`. |
| The WIP reads are part-only today, so a by-sequence read is new work | `GetWipInventoryLocations` and `GetWipFloorQuantities` both take only a part number; neither filters nor reports `Operation`. |
| A WIP part list can be completed from the floor | `GetWipInventoryPartNumbers` already returns the parts the floor holds. |
| A dunnage catalogue with pictures already exists | `IDunnageSubstitutePicker` and the substitute picker the dunnage step already calls. |
| One scrap-type list is already shared | `ScrapTypeCatalog` in `MTM_Waitlist.Core`, used by the Setup picker and the lugger arrangement editor. |
| The lugger arrangement already decides a dump point | Spec 013's arrangement tables and the transition rules on the scrap card. |
| A die is a Visual `FGT` part with a home location | The Visual subordinate-parts read, which classifies `FGT` as a die and resolves a die's location from its primary location. |
| Several dies already mean several requests | The existing wizard's one-request-per-die behaviour. |
| Time already comes from configured allotted minutes | The request item configuration's allotted minutes. |

## Decisions already taken by the owner

| Question, as a user would ask it | Answer | Landed in |
|---|---|---|
| May I say "read the job that is running here" and have it fill my request? | **Yes, optionally** — offered, never assumed; the job is provenance, never identity. | 016 (A5), 021 (whole chunk) |
| Visual knows the part but not the stage of that sequence — keep looking on the floor first, or refuse? | **Floor first**, then Visual when the floor holds none. | 017 FR-011, A2 |
| The WIP app holds a part at a sequence Visual has never heard of — can I ask for it? | **Yes** — the floor is a real sequence source, so the part is requestable and the floor supplies the sequence. | 017 FR-004, A1 |
| Is the requesting work center always where the part should go? | **Yes** — no delivery-point question is asked. | 016, 017 |

## Change map — tell me what changed, and this is what I edit

| If you change your mind about… | Edit |
|---|---|
| What a screen looks like, or which screens exist | [015 spec](../015-generic-wc-ui-shell/spec.md) |
| Whether the preview data is supplied or how the preview is run | [015 spec](../015-generic-wc-ui-shell/spec.md) FR-010 to FR-014 |
| How anything temporary is named, or where it may live | [015 spec](../015-generic-wc-ui-shell/spec.md) FR-021 to FR-026 |
| Whether a work center is generic, and who may say so | [016 spec](../016-generic-wc-request-foundation/spec.md) FR-001 to FR-004 |
| Whether the wizard reads an active job | [016 spec](../016-generic-wc-request-foundation/spec.md) A5 and [021 spec](../021-generic-wc-optional-job-fill/spec.md) |
| The time estimate | [016 spec](../016-generic-wc-request-foundation/spec.md) FR-013, FR-014 |
| What happens to an item whose answer chunk has not shipped | [016 spec](../016-generic-wc-request-foundation/spec.md) FR-012 |
| How a part is proved, or which system answers the quantity | [017 spec](../017-generic-wc-stated-part/spec.md) FR-001 to FR-020 |
| What "all sequences" means, or which are offered | [017 spec](../017-generic-wc-stated-part/spec.md) FR-004 to FR-006, A1 |
| What is shown for outside-service work | [017 spec](../017-generic-wc-stated-part/spec.md) FR-010, FR-014 |
| How dunnage is chosen, or how many parts a request carries | [018 spec](../018-generic-wc-dunnage-page/spec.md) |
| Where scrap types come from, or how the lugger is decided | [019 spec](../019-generic-wc-scrap-type/spec.md) |
| How a die is searched, or whether several dies mean several requests | [020 spec](../020-generic-wc-die-search/spec.md) |
| Whether the job fill is offered, and what it may pre-fill | [021 spec](../021-generic-wc-optional-job-fill/spec.md) |
| When temporary material must be gone, and how that is proved | [022 spec](../022-generic-wc-temp-to-live-verification/spec.md) |
| Anything about what a card may show, or what must never be faked | The chunk that owns that cell, per the card table above |

## Suggested next step

Run `/speckit.plan` for **015** only. Chunks 016 to 022 get planned as they come; planning all eight now would
re-introduce the thing this split exists to avoid, by committing to data details before the layout has been
reviewed.
