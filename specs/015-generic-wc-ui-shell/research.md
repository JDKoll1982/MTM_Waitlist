# Research: Generic Work Center — Screen Layer and Preview Host

## Decision: a separate preview application, not a mode of the shipping application

**Rationale**: constitution Principle II and `RetiredSymbolAuditTests` forbid a demo or mock mode inside the
application, and the spec keeps that prohibition (FR-019, FR-025). A second executable that references the
flow library satisfies the owner's actual request, seeing the layout before the data work exists, without
putting a switch in the product.

**Alternatives considered**: a feature key that swaps the seam implementations inside the application. Rejected
because that is precisely the retired mock-routing arrangement, and the audit would fail the build.

## Decision: the seam interfaces live in the flow library and are implemented twice

**Rationale**: A4 of the spec pins this. The interfaces themselves are real, so they carry no `Temp` prefix;
only the preview implementations do. A later chunk implements the same interface against the live store and
deletes the `Temp` class, which is FR-026's deletion-not-renaming rule.

**Alternatives considered**: one interface per step, fifteen files deep. Rejected: one seam with a method per
data need expresses each need (FR-010) without a file per method.

## Decision: one result type with a presentation, not three booleans

**Rationale**: FR-006 requires three presentations and requires the retry control to disappear with the
unavailable state. A single `Presentation` value makes "exactly one of three" impossible to violate, where
three booleans allow contradictory combinations.

**Alternatives considered**: `IsEmpty`, `IsUnavailable`, `IsPopulated` on the view model. Rejected because a
read failure and an empty read would both be expressible, and the retry control would need a fourth rule.

## Decision: the three presentations are drawn once, by one step frame control

**Rationale**: ten pages need the same empty block, the same unavailable block with an attempt time and a
retry control, and the same header. One control means one place to get it right. The spec forbids a blank or
placeholder cell, so the frame is also the one place that decides which presentation shows.

**Alternatives considered**: a merged resource dictionary of data templates. Rejected because a data template
cannot own the retry command, which belongs to each step's view model.

## Decision: the `Temp` boundary is enforced by a source-scanning test

**Rationale**: FR-023 asks for a build-time check that names the offending artefact. The repository already
enforces its boundaries this way (`RetiredSymbolAuditTests`, `InlineSqlAuditTests`, `SurfaceInventoryAuditTests`),
and a scanning test can name the file and the token, which an analyzer cannot do as plainly. It also runs in the
existing suite, so nothing new has to be wired into the build.

**Alternatives considered**: a Roslyn analyzer. Rejected as new tooling for one rule, and it could not see
resource keys or file names.

## Decision: the work-order card is compared through a `Temp` stand-in inside the preview application

**Rationale**: FR-017 wants the generic card and the work-order card in one list. The real work-order card is a
control inside the shipping application project, which a preview executable cannot reference without dragging
the application's startup, packaging and service locator into it. A `Temp`-named comparison card inside the
preview application gives the owner the side-by-side judgement and is deleted with the rest of `Temp`.

**Alternatives considered**: moving the work-order card into a library so both can share it. Rejected for this
chunk because it edits a shipped surface the spec says must not be modified, and it would put spec 012's card
tests at risk. Recorded as a concern: the comparison card can drift from the real one, and the owner should
re-check the pair when chunk 022 verifies against live data.

## Decision: screen wording lives in each library's own resw

**Rationale**: FR-009 requires resource keys and SC-006 requires zero hard-coded user-visible strings. A class
library's resw is merged into the consuming application's resource map at build time, so the same keys resolve
in the preview application and, later, in the shipping application.

**Alternatives considered**: adding the generic screens' keys to the shipping application's `Resources.resw`.
Rejected because it edits the shipped application for a surface it does not host, and it would mix a later
chunk's wording into the app's resource file early.

## Open item carried into the tasks

`plan-blocks.py` ships without its `presets/_parts/*.md` files in this installation and raises
`FileNotFoundError: call-paths.md`. The required blocks (calls, states, screens) were therefore written by hand
into `plan.md` in the shape that script prints.
