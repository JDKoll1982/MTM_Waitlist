# Implementation Plan: Generic Work Center — Temp-to-Live Conversion and Verification

**Branch**: `022-generic-wc-temp-to-live-verification` | **Spec**: [spec.md](./spec.md) | **Size**: normal

## Summary

This chunk proves an end state rather than building a screen: no `Temp` artefact survives anywhere the
solution can reach, a build-time check fails by name if one returns, and every generic request screen still
shows the three presentations the owner approved — populated, empty, and unavailable carrying the time of
the attempt and a retry — on its live source.

The approach is three small pieces. Widen the existing boundary audit from the application's reference
graph to every project in the solution, keeping its exemptions listed one at a time so no pattern can
quietly broaden them. Add a per-screen verification record in this feature directory that names, for each
screen, the presentations compared, the live read behind them and the test that proves it. Prove the whole
thing by building, running the suite, and running the deliberate-offence case that shows the guard fails
by name.

## Project Structure

```text
MTM_Waitlist.Tests/Module_GenericRequest/
  TempArtefactBoundaryAuditTests.cs      widened: every solution project, one-at-a-time exemptions

specs/022-generic-wc-temp-to-live-verification/
  verification.md                        the per-screen verification record (FR-014)
  contracts/verification-record.md       the fields a record entry must carry
  plan.md, research.md, data-model.md    this plan and its design artifacts
```

**Structure Decision**: the evidence a reviewer reads lives with the feature's documentation, not in the
application (spec assumption A4). The only code change is the guard, because the guard is what turns the
narrowing from a promise into a fact.

## Constitution Check

| Principle | Assessment |
|---|---|
| I. Spec-First, Verified Delivery | PASS. The spec precedes the work, and every task closes only on evidence recorded in this directory. |
| II. Live Data Integrity — Internal Stores Are Never Mocked | PASS. The chunk removes temporary material and adds no substitute source; the verification record cites the live read behind each screen. |
| III. Stored-Procedure-First Database Discipline | PASS. No stored-procedure or schema change. FR-007 only asserts that no `Temp` database artefact survives, and none exists. |
| IV. MCP-First, Grounded Decisions | PASS. The only library-shaped decision is how the audit enumerates projects, which the existing test already answers from real project files rather than from memory. |
| V. WinUI 3 Platform Conformance | PASS. No XAML or control change. The presentations the record compares are the ones the shipped pages already draw. |
| VI. Evidence-Based Verification Gates | PASS. This chunk *is* the gate: FR-014 requires a record per screen, and the completion write depends on a green build and suite. |
| VII. Readable, End-User-Facing Delivery | PASS. The record is written for a reviewer, and the chat reply reports what changed and what remains open. |

## Complexity Tracking

None. No principle is violated, so there is nothing to justify.
