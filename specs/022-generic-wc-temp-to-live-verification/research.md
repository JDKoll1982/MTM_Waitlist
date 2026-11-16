# Phase 0 Research: Generic Work Center — Temp-to-Live Conversion and Verification

**Feature**: [spec.md](./spec.md) | **Plan**: [plan.md](./plan.md)

## Decision 1 — The boundary audit scans every project in the solution

**Decision**: `TempArtefactBoundaryAuditTests` walks every project the solution file names, instead of only
the projects reachable from `MTM_Waitlist.csproj`.

**Rationale**: FR-001 speaks about the solution, and the preview project that used to be the risk has been
deleted. What still bounds the scan today is the application's reference graph, which leaves the test
project and the two deployment projects unchecked: a `Temp` type parked in a tool would satisfy FR-002
vacuously and break FR-001 in silence.

**Alternatives considered**: keeping the reference-graph scan only (fails FR-001 outside the graph); a
repository grep in a script (not a build gate, and FR-003 asks for a build-time failure).

## Decision 2 — The audit keeps its own name and exempts itself by exact path

**Decision**: the guard file stays `TempArtefactBoundaryAuditTests.cs` and appears as one named exemption,
with the reason written beside it.

**Rationale**: FR-004 says exemptions are listed one at a time so none can widen without being read. A guard
that renames itself to slip past its own rule hides the rule; an exemption a reviewer can read states it.

**Alternatives considered**: renaming the file and class to avoid the prefix (loses the shared vocabulary and
the references in earlier specs); a pattern exemption for `*AuditTests.cs` (exactly the broadening FR-004
forbids).

## Decision 3 — A name-level exemption exists for fixtures that mean "temporary directory"

**Decision**: exemptions are keyed either by exact path, or by exact path plus exact name.
`MTM_Waitlist.Tests/Module_Mock_Service/ServiceCapabilityEnforcementTests.cs::TempRootFixture` uses the
name-level form.

**Rationale**: that fixture is a temporary folder, not the preview convention. A path-only exemption would
stop checking the rest of a file that has nothing to do with this feature.

**Alternatives considered**: renaming the fixture (churn in an unrelated suite); exempting the whole file
(weakens the check for no reason).

## Decision 4 — `TemporaryCredentialAttemptLimitTests.cs` is exempted, not the rule narrowed

**Decision**: the file that begins with `Temp` gets a path exemption with its reason. The file-name rule
stays "begins with `Temp`".

**Rationale**: narrowing the rule to `Temp[A-Z]` would stop catching a `TempSomething.cs` artefact, which is
the shape this feature's temporary material actually took.

**Alternatives considered**: narrowing the rule (loses the offence it exists to catch).

## Decision 5 — The verification record lives in the feature directory

**Decision**: `specs/022-generic-wc-temp-to-live-verification/verification.md`, one entry per screen.

**Rationale**: spec assumption A4 puts the evidence with the feature's documentation. Sibling chunks keep
their evidence the same way (018 keeps `wording.md` there).

**Alternatives considered**: a page under `docs/` (separates the record from the spec it proves); a JSON
manifest a test reads (machinery for a single reader).

## Decision 6 — Each screen's evidence is the test that already proves it

**Decision**: the record cites the view-model tests the owning chunk shipped. No new per-screen test is
written for this chunk.

**Rationale**: chunks 016 to 021 already prove the three presentations, the attempt time and the retry for
their own screens. A second copy of those assertions would be evidence that can drift away from the first.

**Alternatives considered**: one end-to-end UI test per screen (needs a running application and the review
host that produced the approval has been deleted); a single aggregate test (reports one verdict for ten
screens, which FR-014 rules out because each screen needs its own outcome).
