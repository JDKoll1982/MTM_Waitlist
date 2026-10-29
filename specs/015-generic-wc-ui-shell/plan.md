# Implementation Plan: Generic Work Center — Screen Layer and Preview Host

## Scale note

This change spans roughly forty-five files across three new projects, one new test area, and one shared
document. A reader should watch two boundaries: the seam line (interfaces the app and the preview host can
both reference) and the `Temp` line (everything standing in for data the real feature will supply). Anything
crossing the `Temp` line into the shipped application is a defect, and the guard test is what proves it.

## Summary

Build the generic request screens as two new library projects that the shipped application does not
reference, and render them from a third, separate preview application that supplies every screen from an
in-memory scenario. The screens talk to data only through interfaces, so the same views will work unchanged
when a later chunk supplies a real implementation. A build-time audit fails if any `Temp`-named artefact ever
appears inside the shipped application or its reference graph, which keeps the preview out of the product
while making every temporary thing findable by one search.

## Project Structure

```text
MTM_Waitlist.GenericRequest/
  Contracts/            IGenericRequestStepData, IGenericRequestNavigator, GenericRequestStep
  Models/               result, options, draft
  Services/             GenericRequestFlowRules
  ViewModels/           step view-model base + ten step view models
  Views/                ten step pages
  Controls/             GenericRequestStepFrame (the three-state presenter)
  Strings/en-us/        Resources.resw  (real screen wording)
MTM_Waitlist.GenericRequest.Controls/
  Models/               card model, card cell
  Helpers/              GenericRequestCardRules
  Controls/             GenericRequestCard
  Views/                GenericRequestDetailPage
  Strings/en-us/        Resources.resw
TempGenericRequest.Preview/
  Temp/                 TempGenericRequestStepData, TempGenericRequestNavigator, TempPreviewScenarios
  Views/                TempPreviewShell, TempPreviewCardPage
  Strings/en-us/        Resources.resw  (Temp-prefixed keys only)
MTM_Waitlist.Tests/Module_GenericRequest/
  GenericRequestCardRulesTests.cs
  GenericRequestFlowRulesTests.cs
  TempArtefactBoundaryAuditTests.cs
```

**Structure Decision**: three projects, mirroring the arrangement the repository already uses for the
work-order wizard (a library holding views and view models beside the shipping application). The card and its
detail page get their own library because they are read by handlers independently of the flow, and the preview
application is separate because FR-003 forbids the shipped application reaching it.

## Interaction path

```text
calls build-card
A step view model builds the screen's options from IGenericRequestStepData and puts them in an observable list.
The step frame reads the resulting presentation (populated, empty, unavailable) and shows exactly one of the three.
Choosing Continue asks GenericRequestFlowRules for the next step and hands it to IGenericRequestNavigator.
The preview application implements that navigator by swapping the page it hosts, so no database, queue or permission code runs.
```

## States

```text
Unavailable: The read failed, or the scenario says so. Carry the attempt time and a retry control. shows stepframe
Empty: The read succeeded and returned nothing. The retry control is absent, because there is nothing to retry. shows stepframe
Populated: The read returned rows. shows stepframe
```

## Screens

```text
screen workcenter Pick the work center the request belongs to.
screen category Pick the request category.
screen item Pick the item the request is about.
screen part State the part, its stage and its sequence.
screen dunnage Choose dunnage type and parts from the catalogue page.
screen scrap State the scrap type the request needs.
screen die Search for and pick a die.
screen details Give needed-by time, quantity and a description.
screen summary Review every answer before raising.
screen result See the outcome.
screen cardpreview Compare the generic card and the work-order card in one list, in all four variants.
screen detail Read one generic request: its facts, the requester, the time raised and its history.
```

## Constitution Check

| Principle | Assessment |
| --- | --- |
| I. Spec-First, Verified Delivery | PASS — spec, plan and tasks precede the work; no task is ticked until the build and the suite pass. |
| II. Live Data Integrity — Internal Stores Are Never Mocked | PASS — no store is read or written by this change. The preview application holds in-memory values and opens no connection (FR-013). It is not a mode of the application and shares no seam with a store. |
| III. Stored-Procedure-First Database Discipline | PASS — this change adds no database artefact and issues no SQL. |
| IV. MCP-First, Grounded Decisions | PASS — WinUI 3 control behaviour and static-resource usage were checked against Microsoft Learn; repository facts were read from source. |
| V. WinUI 3 Platform Conformance | PASS — `Microsoft.UI.Xaml` only, CommunityToolkit.Mvvm generators, `ThemeResource` brushes, text styles rather than literal sizes, localized strings from `.resw`, no literal colours. The new pages live outside the shell, so no navigation service is claimed (the `IGenericRequestNavigator` seam is what a later chunk wires to `INavigationService`). |
| VI. Evidence-Based Verification Gates | PASS — a clean solution build and a green suite are the completion gate; `WMC9999` is treated as a real XAML error. |
| VII. Readable, End-User-Facing Delivery | PASS — the reply says what changed and what is open, in plain language. |

## Research Summary

Full entries are in [research.md](./research.md). The four load-bearing choices are: a separate preview
application rather than an application mode; interfaces declared in the flow library and implemented twice;
three presentations modelled as one result type rather than three booleans; and a source-scanning audit rather
than a compiler attribute for the `Temp` boundary.

## Contracts

- [contracts/step-data.md](./contracts/step-data.md) — the data seam and its result type.
- [contracts/navigation.md](./contracts/navigation.md) — the navigation seam, step order and routing rule.
- [contracts/ui-identifiers.md](./contracts/ui-identifiers.md) — the pinned names a consumer or test codes
  against: project names, namespaces, resource-key shapes, `Temp` naming shapes.
