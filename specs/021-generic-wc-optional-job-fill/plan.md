# Implementation Plan — Optional Job Fill

**Scale note**: this change spans two modules, the composition root, the request queue's schema and three stored
procedures, and the waitlist detail page — about fourteen files across five areas. Watch the ordering: the offer is
asked on the Category step, the seeded answers land on the Part, Dunnage and Die steps, and provenance reaches the
queue through the one write path every request already uses.

## Summary

A generic work center that happens to have a job running now offers to fill the request from it: the panel appears on
the Category step, accepting seeds the job's part, sequence, die and dunnage as changeable candidates, and declining
changes nothing. The job is stored and shown as provenance, never as the request's identity.

The job is read through a new interface owned by the generic request module and implemented at the composition root
over the Setup module's existing active-job read, so no library gains a reference it does not have. Provenance rides
the same stored-procedure write path every request already uses, through one new nullable column.

## Project Structure

```text
MTM_Waitlist.GenericRequest/
  Contracts/IGenericRequestJobFillData.cs          new: probe + read seam
  Models/GenericRequestJobFill.cs                  new: the seeded candidates and their flags
  Models/GenericRequestDraft.cs                    add JobFill
  Models/GenericRequestCategoryOption.cs           unchanged
  ViewModels/GenericRequestCategoryViewModel.cs    the offer panel
  ViewModels/GenericRequestPartViewModel.cs        seed part + sequence
  ViewModels/GenericRequestDunnageViewModel.cs     seed dunnage
  ViewModels/GenericRequestDieViewModel.cs         seed die
  ViewModels/GenericRequestItemViewModel.cs        clear the seeds with the item
  Services/GenericRequestLiveStepData.cs           write provenance on the raise
  Views/GenericRequestCategoryPage.xaml            the panel
  Views/GenericRequestPartPage.xaml                "From job" qualifier
  Views/GenericRequestDunnagePage.xaml             "From job" qualifier
  Views/GenericRequestDiePage.xaml                 "From job" qualifier
  Strings/en-us/Resources.resw                     the pinned copy

Services/
  GenericRequestJobFillSource.cs                   new: app-root adapter over the Setup read
  DependencyInjection/ServiceRegistrationExtensions.cs   register the adapter

MTM_Waitlist.Waitlist.View/
  Models/WaitlistRequestDraft.cs                   add ProvenanceWorkOrder
  Models/WaitlistRequest.cs                        add ProvenanceWorkOrder
  Services/WaitlistRequestService.cs               pass it, read it back
  ViewModels/WaitlistViewDetailViewModel.cs        the provenance row

Database/
  Tables/18_waitlist_requests_queue/{create,rollback}.sql
  Tables/AllTables.sql
  StoredProcedures/sp_waitlist_request_insert/{create,rollback}.sql
  StoredProcedures/sp_waitlist_request_get/{create,rollback}.sql
  StoredProcedures/sp_waitlist_request_list/{create,rollback}.sql
  StoredProcedures/AllSPs.sql
  Bootstrap/update_table_descriptions.sql

MTM_Waitlist.Tests/Module_GenericRequest/            offer, seeding, clearing, provenance
MTM_Waitlist.Tests/Module_Waitlist/                  provenance reaches the draft and reads back
```

**Structure Decision**: the seam lives in the consuming module and its implementation at the composition root, which is
the arrangement `RequestJobAvailabilityProvider` already uses for the same reason — no library may see
`MTM_Waitlist.Setup`.

## Calls

```text
Category step load
  GenericRequestCategoryViewModel.LoadCoreAsync
    -> IGenericRequestJobFillData.ProbeAsync(workCenter)          names the job, reads no values
    -> IGenericRequestStepData.ReadCategoriesAsync(workCenter)    unchanged
  AcceptFillCommand
    -> IGenericRequestJobFillData.ReadAsync(workCenter)           values, only after the choice
    -> Draft.JobFill                                              seeds the candidates

Raise
  GenericRequestLiveStepData.SubmitAsync
    -> WaitlistRequestDraft.ProvenanceWorkOrder <- Draft.JobFill.WorkOrder
    -> IWaitlistRequestService.SubmitAsync
      -> sp_waitlist_request_insert(..., p_provenance_work_order)
```

## Code

`GenericRequestJobFill` is the only place that knows an answer came from the job, and it answers one question per
answer: whether that answer is still the job's.

```csharp
public bool IsPartFromJob { get; private set; }
public void ClearPart() => IsPartFromJob = false;
```

## States

The offer is one request's decision, so it moves through four states and never returns to `Offered`:

```text
NotChecked: the step has not loaded yet.
Offered: a job is running and the offer is on screen. shows category-offer
Answered: accepted, values seeded, or declined, nothing seeded. shows category-filled / category-plain
Unavailable: the accepted read failed; attempt time and a retry. shows category-offer-unavailable
```

## Screens

```text screen category-offer The Category step with the job-fill offer above the category cards
Title: A job is running at this work center
Question: Fill this request from job {0} at {1}?
Buttons: Fill the request / Start blank
```screen category-filled The Category step after accepting
Line above the cards: Filled from job {0}
```screen category-plain The Category step with no job, or after declining
Nothing added; the step is exactly what it is today.
```screen category-offer-unavailable The Category step when the accepted read failed
The running job could not be read / attempted at {0} / Retry
```screen part-die-dunnage The seeded steps
The chosen candidate carries the qualifier From job {0} until the requester changes it.
```

## Constitution Check

| Principle | Assessment |
|---|---|
| I. Spec-First, Verified Delivery | PASS — spec 021 precedes this plan; tasks are ticked only when built and verified. |
| II. Live Data Integrity | PASS — the job read is an internal store read, live, with no cached or mirrored substitute; no demo mode is introduced. |
| III. Stored-Procedure-First Database Discipline | PASS — one nullable column and three procedures gain a parameter or a column, each with `create.sql` / `rollback.sql`, and `AllTables.sql`, `AllSPs.sql` and `update_table_descriptions.sql` move in the same change. No inline statement text. |
| IV. MCP-First, Grounded Decisions | PASS — the design follows code already in the repository (`RequestJobAvailabilityProvider`, the step/view-model seam, the append-only procedure idiom) plus Context7 and Microsoft Learn for the WinUI and MVVM surfaces touched. |
| V. WinUI 3 Platform Conformance | PASS — `Microsoft.UI.Xaml` only, MVVM Toolkit generators, every string through `.resw` and `GetLocalized`, `ThemeResource` brushes and text styles, no hardcoded sizes. |
| VI. Evidence-Based Verification Gates | PASS — solution build and the full test suite are the gate, with targeted generic-request and waitlist tests run first. |
| VII. Readable, End-User-Facing Delivery | PASS — the report is plain English and anything outstanding is last. |

No violations, so there is no Complexity Tracking table.
