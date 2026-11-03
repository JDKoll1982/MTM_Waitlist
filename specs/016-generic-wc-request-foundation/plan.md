# Implementation Plan: Generic Work Center — Designation and Request Foundation

## Scale note

This is an oversized change. It spans the queue schema and four stored procedures, the work-centre catalog read, the
waitlist request seam, both generic request libraries, the new request wizard's entry point, the board and the detail
route, plus the test project. A reader should watch three boundaries: the queue write (one seam, one procedure), the
answer seam (`IGenericRequestStepData`, whose four externally proved reads must report unavailable), and the card
boundary (a generic row never draws a work-order cell).

## Summary

Make the generic work centre flow real. The designation already exists on the work centre catalog; this change gives a
designated work centre a request flow that never reads a job, asks the requester for the item's configured answer, and
writes one request to the same queue through the same stored procedure as every other request. The generic card and its
detail page become reachable from the board, drawing only the facts a generic request has. Every value comes from a
live read, the requester's answer, or configuration; the four answers whose sources ship in later chunks report their
source as unavailable rather than offering a substitute.

## Project Structure

```text
Database/Tables/18_waitlist_requests_queue/          create.sql, rollback.sql
Database/StoredProcedures/sp_waitlist_request_insert/ create.sql, rollback.sql
Database/StoredProcedures/sp_waitlist_request_list/   create.sql, rollback.sql
Database/StoredProcedures/sp_waitlist_request_get/    create.sql, rollback.sql
Database/StoredProcedures/sp_setup_work_centers_catalog_get/ create.sql, rollback.sql
Database/Bootstrap/update_table_descriptions.sql
Database/Tables/AllTables.sql, Database/StoredProcedures/AllSPs.sql
Database/Database-Ruleset.md

MTM_Waitlist.Waitlist.View/
  Models/WaitlistRequest.cs, WaitlistRequestDraft.cs, SampleOrder.cs
  Services/WaitlistRequestService.cs
MTM_Waitlist.Shared/
  Models/WorkCenterDetail.cs, WorkCenterSelectionItem.cs
  Services/LocalWorkCenterCatalogService.cs
MTM_Waitlist.Waitlist.NewRequest/
  ViewModels/NewRequestWorkCenterViewModel.cs

MTM_Waitlist.GenericRequest/
  Contracts/GenericRequestStep.cs, IGenericRequestStepData.cs, IGenericRequestNavigator.cs
  Models/GenericRequestDraft.cs, GenericRequestOptions.cs, GenericRequestSubmission.cs
  Services/GenericRequestLiveStepData.cs, GenericRequestFlowRules.cs
  ViewModels/GenericRequestStepViewModel.cs, GenericRequestItemViewModel.cs,
            GenericRequestSummaryViewModel.cs, GenericRequestResultViewModel.cs
  Strings/en-us/Resources.resw

MTM_Waitlist.GenericRequest.Controls/
  Models/GenericRequestCardModel.cs
  Helpers/GenericRequestCardRules.cs
  Views/GenericRequestDetailPage.xaml(.cs)
  Strings/en-us/Resources.resw

App composition root
  MTM_Waitlist.csproj
  Services/GenericRequestNavigator.cs
  Services/DependencyInjection/ServiceRegistrationExtensions.cs
  Module_Waitlist/Controls/GenericRequestCardTemplateSelector.cs
  Module_Waitlist/Views/WaitlistViewPage.xaml(.cs)

MTM_Waitlist.Tests/Module_GenericRequest/
  GenericRequestLiveStepDataTests.cs, GenericRequestFlowRulesTests.cs,
  GenericRequestCardRulesTests.cs
```

**Structure Decision**: the flow and its card stay in the two generic request libraries delivered by spec 015; only the
hosting seam (the navigator, the DI registrations, the entry point, the board template and the detail route) lands in
the application, because that is the layer that owns navigation and the shell.

## Interaction path

```text
calls submit-generic-request
The summary step asks IGenericRequestStepData to raise the draft. The live seam maps the draft onto WaitlistRequestDraft with IsGeneric set and hands it to IWaitlistRequestService.SubmitAsync, which is the one write door every request already uses.
SubmitAsync refuses a blank job id only when the draft is not generic, then calls sp_waitlist_request_insert with the generic flag, the description and the accepted warnings.
The result step reads back the draft's recorded outcome: the request's public id on success, the unavailable presentation with the attempt time on failure.
```

## States

```text
Unavailable: The read or the write failed. The step carries the attempt time and a retry control. shows stepframe
Empty: The read succeeded and found nothing, or nothing has been answered yet. There is nothing to retry. shows stepframe
Submitting: The summary step has asked the queue to write and is waiting. Continue is disabled until the write answers. shows summary
Raised: The queue accepted the request. The result step shows the request number and nothing invented. shows result
```

## Screens

```text
screen workcenter Pick the generic work centre the request belongs to, showing only designated work centres.
screen category Pick the request category from the work-order wizard's own list.
screen item Pick the item and answer the one question its configuration declares.
screen part State that the part source is not yet available.
screen details Give a required needed-by time and an optional description.
screen summary Review every answer, then raise the request.
screen result See the request number, or the failure with a retry.
screen board See generic and work-order requests in one list, each drawn by its own card.
screen detail Read one generic request: its facts, the requester, the time raised and its history.
```

## Constitution Check

| Principle | Assessment |
| --- | --- |
| I. Spec-First, Verified Delivery | PASS — spec, plan and tasks precede the work; no task is ticked until the build and the suite pass. |
| II. Live Data Integrity — Internal Stores Are Never Mocked | PASS — every read is live. No sample row, no cache substitute and no feature toggle is added; a failed read is reported as unavailable with a retry. |
| III. Stored-Procedure-First Database Discipline | PASS — the only write is `sp_waitlist_request_insert`, the read additions go through `sp_waitlist_request_list`, `sp_waitlist_request_get` and `sp_setup_work_centers_catalog_get`, and every changed artifact ships `create.sql` with `rollback.sql`. |
| IV. MCP-First, Grounded Decisions | PASS — WinUI 3 binding and template-selector behaviour, and the WinUI `DataTemplateSelector` contract, were checked against Microsoft Learn; repository facts were read from source. |
| V. WinUI 3 Platform Conformance | PASS — `Microsoft.UI.Xaml` only, CommunityToolkit.Mvvm generators, `ThemeResource` brushes, text styles not literal sizes, every string from `.resw`. |
| VI. Evidence-Based Verification Gates | PASS — a clean solution build and a green suite are the completion gate; `WMC9999` is treated as a real XAML error. |
| VII. Readable, End-User-Facing Delivery | PASS — the reply says what changed and what is open, in plain language. |

## Research Summary

Full entries are in [research.md](./research.md). The load-bearing choices are: reuse the one queue write seam rather
than build a second; make the job id nullable and add an explicit generic flag rather than infer the kind from a null;
add `description` and `accepted_warnings_json` rather than overload `note` or `answers_json`; ask the item's configured
answer on the item step, where the work-order flow asks it; and report the four externally proved answers as
unavailable rather than offer a substitute.

## Contracts

- [contracts/generic-request-flow.md](./contracts/generic-request-flow.md) — the step order, the entry point, the answer
  seam and the submission seam.
- [contracts/queue-payload.md](./contracts/queue-payload.md) — the queue columns and the procedure parameters a generic
  request writes and reads.
