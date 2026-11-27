# Phase 0 Research: Documentation Accuracy Corrections

Four genuine choices were open when the spec was drafted. Each was settled by reading the repository rather than by preference, and each is recorded here because the task list depends on it.

## D1. How far the moved design brief's references are followed

**Decision**: update the two README links and the three path literals in `AcceptanceSurfaceTests`, and leave every citation inside `specs/` planning records as written. The moved brief carries one line naming its former location.

**Rationale**: the README is a live index a reader browses today, so a link there has to resolve. The guard tests read the brief from disk and assert on what it records, so a stale path there does not degrade gracefully, it fails three tests. A planning record under `specs/010-startup-rebuild/`, `specs/012-` or `specs/024-` is a record of how the work was planned at the time; rewriting its citations would edit history to hide a move that the one line in the brief already explains.

**Alternatives considered**: updating all twenty-odd citations (edits history and touches folders this feature has no other business in); leaving a stub file at the old path (a second copy to keep in step, and the spec says the old path stops holding the brief); not moving at all (rejected by the approval decision).

## D2. Which references are live and which are history

**Decision**: only references in always-applied guidance, current indexes and living records are corrected. Historical records keep their wording: the twelve defect records under `docs/defects/`, the pre-spec research note, the analysis and progress files inside older spec folders, and the audit's own assessment folder.

**Rationale**: a defect record states what was true when the defect was found, and a path that no longer resolves is part of that record. Correcting it would make the record describe a repository state that never existed on the day it was written.

**Alternatives considered**: correcting everything a search finds (would rewrite the same tokens in five spec folders and a dozen records, for no reader benefit); correcting nothing outside `docs/` (leaves the alive guidance wrong, which is the highest-cost error here).

## D3. The drift ledger's retired helpers

**Decision**: fix the ledger's dead file references, including the specification path on line 103, and leave the method prose that names three helpers absent from the tree. Record the gap in the spec's Non-goals so the next reader knows it was seen and deliberately not taken on.

**Rationale**: the approved remediation for that ledger was to repoint one path and remove references to files that are gone. Rewriting its method section is a different change with a different risk: it would describe a drift-recording mechanism that no longer exists, and guessing what replaced it would be invention rather than correction.

**Alternatives considered**: rewriting the method section now (scope creep into prose nobody approved); deleting the ledger (it holds the one record of several retired files, and its surviving references are useful).

## D4. How many image links actually need repairing

**Decision**: eight of the twelve. The before and after images live under each proposal's `pngs/` directory, so those eight links take that directory in their path. The four comparison images sit beside their proposal and already resolve, so their links are left exactly as they are.

**Rationale**: measured by listing each proposal folder and its `pngs/` directory. A blanket prefix would have broken the four links that work, turning eleven working links into twelve broken ones. The audit counted twelve broken links; the tree says eight.

**Alternatives considered**: prefixing all twelve uniformly (breaks four); moving the comparison images into `pngs/` (moves files nobody asked to move, and other documents link them where they are).

## D5. How a removal is stated

**Decision**: one plain sentence per document, naming what was removed and that it went with its workflow. The five-row and ten-document structures collapse to what exists, and the remaining rows keep their columns.

**Rationale**: a reader who remembers the removed workflows needs to learn what happened to them; a reader who never saw them needs the document to stop linking nowhere. One sentence does both without turning the index into a changelog.

**Alternatives considered**: deleting the rows silently (a reader who remembers them cannot tell removal from an oversight); keeping the rows with the links removed (five empty rows still imply five workflows exist).

## Measured facts this plan relies on

- Guardrail test files: 18, counted `MTM_Waitlist.Tests/**/*AuditTests.cs` on 2026-11-27.
- Interface files: 92, counted `**/*.xaml` outside `bin` and `obj` on 2026-11-27.
- Diagram PDFs: two, both under `docs/diagrams/pdf/startup-launch/`.
- Diagram spec files: only the startup-launch pair remains under `docs/diagrams/tools/`.
- Startup-launch proposals: four, holding twelve image links between them, eight of which are broken.
- The startup brief is read from disk at three points in `AcceptanceSurfaceTests` (lines 249, 263 and 313).
