# UI Contract: the job fill offer

This is the interface feature 021 exposes on the request flow. Anything named below is something a consumer or a
test may rely on; everything else is an internal detail.

## Verbatim copy

These strings are exact. They must not be recased, reworded, pluralised, or given different punctuation.

| Purpose | Copy |
|---|---|
| Offer title | `A job is running at this work center` |
| Offer question | `Fill this request from job {0} at {1}?` |
| Accept | `Fill the request` |
| Decline | `Start blank` |
| Pre-filled qualifier | `From job {0}` |
| Provenance line on the detail page and in any export | `Filled from job {0}` |
| Unavailable reading | `The running job could not be read` |

Format placeholders, exactly as written:

- Offer question `{0}` is the running job's work order, `{1}` is the work center.
- Pre-filled qualifier `{0}` is the running job's work order.
- Provenance line `{0}` is the running job's work order.

## Resource keys

Every string above resolves through `MTM_Waitlist.GenericRequest/Strings/en-us/Resources.resw` except the provenance
line, which is drawn on the waitlist detail page and resolves through the application's `Strings/en-us/Resources.resw`.

| Resource key | Carries | Lands on |
|---|---|---|
| `GenericRequest_Category_JobFill_Title.Text` | `A job is running at this work center` | Category step |
| `GenericRequest_Category_JobFill_Question.Text` | `Fill this request from job {0} at {1}?` | Category step |
| `GenericRequest_Category_JobFill_Accept.Content` | `Fill the request` | Category step |
| `GenericRequest_Category_JobFill_Decline.Content` | `Start blank` | Category step |
| `GenericRequest_Category_JobFill_Filled.Text` | `Filled from job {0}` | Category step, after accepting |
| `GenericRequest_Category_JobFill_Unavailable.Text` | `The running job could not be read` | Category step |
| `GenericRequest_Category_JobFill_Attempted.Text` | attempted-at line for the failed read | Category step |
| `GenericRequest_JobFill_FromJob.Text` | `From job {0}` | Part, Dunnage and Die steps |
| `Waitlist_Detail_JobFill_Provenance.Text` | `Filled from job {0}` | Waitlist request detail page |

The retry on the unavailable panel reuses the existing `GenericRequest_StepFrame_Retry.Content`. No new copy is
written beyond the rows above.

## Where the offer is asked

The offer is a panel on the **Category** step, above the category cards. It is asked there on both entry paths —
`GenericRequestNavigator.Launch` from a work center tile and `StartAnother` through the WorkCenter step — because
Category is the first step after the work center step either way.

- No wizard step is added, moved or removed. `GenericRequestStep.WorkCenter`, `.Category`, `.Item`, `.Part`,
  `.Dunnage`, `.Scrap`, `.Die`, `.Details`, `.Summary` and `.Result` keep their order.
- No route, page registration or service registration is added for the offer. It rides the already-registered
  `GenericRequestCategoryViewModel` -> `GenericRequestCategoryPage`.
- No dialog seam is introduced; the panel is answered in the page.

## Offer state

The offer is one request's decision. It moves forward only and never returns to `Offered`.

```text
NotChecked   the Category step has not loaded yet.
Offered      a job is running and the offer is on screen            shows category-offer
Answered     accepted with values seeded, or declined with nothing  shows category-filled / category-plain
Unavailable  the accepted read failed; attempt time and a retry     shows category-offer-unavailable
```

The four states are the members of `GenericRequestJobFillOfferState`, and the Category view model exposes them as
`OfferState`.

| State | Reached when | Never reached when |
|---|---|---|
| `NotChecked` | the view model is constructed | never |
| `Offered` | the probe answered `Populated` and no answer has been given for this request | the probe answered `Empty` or `Unavailable`, or the requester has already answered |
| `Answered` | `AcceptFillCommand` ran and the read answered `Populated`, or `DeclineFillCommand` ran | the read answered `Empty` or `Unavailable` after accepting |
| `Unavailable` | `AcceptFillCommand` ran and the read answered `Empty` or `Unavailable` | the requester has not accepted |

## View model surface

On `MTM_Waitlist.GenericRequest.ViewModels.GenericRequestCategoryViewModel`:

| Member | Meaning |
|---|---|
| `GenericRequestJobFillOfferState OfferState` | the state above |
| `string JobWorkOrder` | the running job's work order; empty unless a job was named |
| `bool IsOfferVisible` | `OfferState == Offered` |
| `bool IsFilledVisible` | `OfferState == Answered` and the fill seeded at least one answer |
| `bool IsFillUnavailable` | `OfferState == Unavailable` |
| `string OfferTitle` | `GenericRequest_Category_JobFill_Title.Text` |
| `string OfferQuestion` | `GenericRequest_Category_JobFill_Question.Text` formatted with the job and the work center |
| `string FilledLine` | `GenericRequest_Category_JobFill_Filled.Text` formatted with the job |
| `string FillUnavailableText` | `GenericRequest_Category_JobFill_Unavailable.Text` |
| `string FillAttemptedAtDisplay` | the attempt time of the failed read, formatted as the step frame already formats attempts |
| `AcceptFillCommand` | accept; the read it makes is specified in `job-fill-data-and-provenance.contract.md` |
| `DeclineFillCommand` | decline |
| `RetryFillCommand` | retry the failed read; only meaningful while `IsFillUnavailable` |

On `GenericRequestPartViewModel`, `GenericRequestDunnageViewModel` and `GenericRequestDieViewModel`:

| Member | Meaning |
|---|---|
| `string FromJobText` | `GenericRequest_JobFill_FromJob.Text` formatted with the job work order while that answer is still the job's; `string.Empty` otherwise |
| `bool HasFromJob` | `FromJobText.Length > 0` |

`FromJobText` is empty before any job fill, empty for a declined offer, and empty again the moment the requester
changes that answer (FR-007, A6).

## Screens

```text
category-offer
Title:    A job is running at this work center
Question: Fill this request from job {0} at {1}?
Buttons:  Fill the request / Start blank
Drawn above the category cards. No job card, no job cell, no category preselected.
```

```text
category-filled
Line above the cards: Filled from job {0}
The category cards are unchanged; nothing is preselected by the fill.
```

```text
category-plain
Nothing is added. The step is exactly what it is today, whether the work center has no job or the offer was
declined (FR-003, FR-004).
```

```text
category-offer-unavailable
The running job could not be read
attempted at {0}
Retry
The category cards are still usable; no value is seeded; the request is not presented as filled (FR-016).
```

```text
part-die-dunnage
The chosen candidate carries From job {0} until the requester changes it.
```

## Per-request, never remembered

- The offer is per request (A3). Starting another request at the same work center in the same shift offers again.
- A decline is not stored anywhere and is not shown again inside the same request (FR-003, A7).
- A decline is not a failure: nothing is logged as a problem, and the step reads `category-plain`, not an error state.

## What this contract does NOT change

- No card cell is added for the job. A filled request draws the generic card (FR-012).
- No export surface is added (research R9).
- No step gains a new question beyond this one panel on Category.
