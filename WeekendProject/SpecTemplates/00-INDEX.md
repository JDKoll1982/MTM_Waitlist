# Spec Templates — seeds for the next `/speckit.specify` runs

**Created:** 2026-09-12 · **Source of truth for scope:** `OPEN-TASKS.md` (§2 carry-forward) and
`WeekendProject/OPEN-WORK-NEXT-SPEC.md` (§4–§10 itemised detail).

## How to use a file in this folder

Each template is a **seed brief** for one feature, and each one contains a ready-to-paste
**Input block** (§2 of the template). To start that feature:

1. Open the template and read §3 (carried-forward requirements) and §4 (out of scope) first — the
   requirements section is quoted from the source checklist and must survive into the spec.
2. Run `/speckit.specify`, pasting the template's **Input block** as the argument, and nothing else.
   The command assigns the next sequential feature number itself (`specs/002-…` and onward); do not
   type a number into the input.
3. After the spec is generated, check §6 of the template against the generated `spec.md`, then run
   `/speckit.clarify` with the template's §7 open decisions as the agenda.
4. Before `/speckit.plan`, confirm every defect file listed in the template's §5 has been updated to
   name the generated spec (that mapping is already written into each defect file — see the table
   below).

**One workstream per `/speckit.specify` run.** Do not merge two templates into one spec; the
carry-forward counts below are the box counts those workstreams were sized against.

## Run order and defect ownership

| # | Template | Source | Carry-forward | Defects it closes |
| --- | --- | --- | ---: | --- |
| 01 | `01-truthful-data-and-controls.md` | Defect-driven (no backlog workstream) — completes `specs/001` FR-001/FR-003/FR-014 and SC-013 | 10 defects | **All ten** — see below |
| 02 | `02-handler-fulfilment-and-urgency.md` | `OPEN-WORK-NEXT-SPEC.md` §4 (`PromptFiles/07`, `08`) | 11 | Completes the Accept/Complete/Release half of `High-Waitlist-CardCancelAndAcceptButtonsAreInert` |
| 03 | `03-unified-card-and-taxonomy.md` | `OPEN-WORK-NEXT-SPEC.md` §5 (`PromptFiles/14-57%`) + `App-Validation-Checklist.md` §4–§8 | 20 + 27 checks | Completes the real-values half of `Critical-Waitlist-CardAndDetailShowFabricatedMaterialData` and `High-Coil-AverageWeightIsResolvedForOneHardCodedPart` |
| 04 | `04-waitlist-analytics.md` | `OPEN-WORK-NEXT-SPEC.md` §6 (`PromptFiles/10`) | 20 | Optional follow-on for `High-NewRequest-QueueAndWaitTimeIsHardcoded` (a real queue/wait figure) |
| 05 | `05-request-administration-and-retention.md` | `OPEN-WORK-NEXT-SPEC.md` §7 (`PromptFiles/11`) | 8 | — |
| 06 | `06-user-management.md` | `OPEN-WORK-NEXT-SPEC.md` §8 (`PromptFiles/15`) | 39 | — |
| 07 | `07-startup-gate-polish.md` | `OPEN-WORK-NEXT-SPEC.md` §9 (`PromptFiles/13`, startup-gate items only) | 2 | — |
| 08 | `08-notification-delivery-and-packaging.md` | Defect-driven (no backlog workstream) | — | Completes `Medium-Settings-NewRequestAlertsCannotFireUnpackaged` and verifies `Low-Notifications-ToastActivationShowsTodoDialog` |
| — | Docs hygiene (§10.2) | `OPEN-WORK-NEXT-SPEC.md` §10 | — | Folded into template 01 — see its §5 |

## Template number → shipped spec number (added 2026-09-20)

A template's number is **not** its spec number. `01-truthful-data-and-controls` shipped as
`specs/002-truthful-data-and-controls`, not `specs/001-…` (`specs/001` is the Module_Mock workstream,
which pre-dates the templates). Every `defects/*.md` **Planned fix** row names specs by *template*
number and now carries a banner pointing back here.

| Template | Shipped as | Status |
| --- | --- | --- |
| `01-truthful-data-and-controls.md` | **`specs/002-truthful-data-and-controls`** | **shipped** |
| `02-handler-fulfilment-and-urgency.md` | **`specs/003-waitlist-handler-fulfilment`** | **shipped** (49/49) |
| `03-unified-card-and-taxonomy.md` | **`specs/004-unified-card-item-picker`** | **in flight** |
| `04-waitlist-analytics.md` | — | not specified |
| `05-request-administration-and-retention.md` | — | not specified |
| `06-user-management.md` | — | not specified |
| `07-startup-gate-polish.md` | — | not specified (its 2 boxes are live carry-forward in `PromptFiles/13`) |
| `08-notification-delivery-and-packaging.md` | — | not specified |

**Consequence for the defect map above:** `01` rows are closed, `02` rows are closed, and `03` rows are
partly closed — `specs/004` **removes** the fabricated values (already done by `specs/002`) and builds the
unified card and item picker, but the NCM defect panel and the FG/WIP/Outside items are **explicitly out of
its scope** (`specs/004-unified-card-item-picker/spec.md` line 509). Those need template `05`-or-later
ownership, or the surviving `sp_waitlist_defect_types_*` artifacts need retiring.

### Why template 01 exists (and why it runs first)

**Eight of the ten defects have no owning workstream at all** in `OPEN-WORK-NEXT-SPEC.md` — they were
found while inventorying the application and belong to no planned feature. The other two are only
*partly* owned: §4.3 describes the handler actions behind the inert buttons, and §5.6/§5.7 describe
replacing the hard-coded item fields — but in both cases the workstream would fix the defect only as a
side effect of building the feature, and only when that feature is reached.

Meanwhile the defects are live now. Four of them are also *aggravated* by the planned work: template 03
rewrites the field-population code that currently shows fabricated values, so leaving the literals in
place until then keeps them on screen for the whole of that workstream.

Template 01 is therefore a short, dependency-free **compliance closure** spec, run before the feature
work, whose rule is:

> The application must never display a value it cannot substantiate and never offer a control it
> cannot honour. Where the real value or behaviour does not exist yet, the field or control is removed
> or marked unavailable — never filled with a plausible substitute. Where wording (on screen, in a
> resource file, or in a comment) describes behaviour that was deleted, it is corrected or deleted.

That rule is not new policy: it is `specs/001` FR-001, FR-003, FR-014 and SC-013 applied to the screens
and files those requirements were never checked against.

### Defect → spec map (the single copy; every defect file repeats its own row)

| Defect file | Fixed in | Feature completion |
| --- | --- | --- |
| `defects/Closed-Partial-Critical-Waitlist-CardAndDetailShowFabricatedMaterialData.md` | **01** — remove every fabricated value so nothing false is shown | 03 — restore the real values through the resolvers |
| `defects/Closed-High-Waitlist-CardCancelAndAcceptButtonsAreInert.md` | **01** — stop offering the buttons until they work | 02 — wire Accept/Complete/Release and cancel-own |
| `defects/Closed-Partial-High-NewRequest-QueueAndWaitTimeIsHardcoded.md` | **01** — remove the fabricated queue/wait card | 04 (optional) — a real queue count and wait estimate |
| `defects/Closed-Partial-High-Coil-AverageWeightIsResolvedForOneHardCodedPart.md` | **01** — remove the wrong-part lookup and its fixed fallback | 03 — per-coil lookup for the request's own part |
| `defects/Closed-Partial-Medium-Settings-NewRequestAlertsCannotFireUnpackaged.md` | **01** — gate the toggle on the capability so the surface stops promising it | 08 — decide packaging and make alerts actually deliver |
| `defects/Closed-Medium-Settings-AboutLabelsCollideOnOneResourceKey.md` | **01** | — |
| `defects/Closed-Low-Settings-PrivacyPolicyLinkPointsAtPlaceholderUrl.md` | **01** | — |
| `defects/Closed-Low-Settings-SearchIgnoresThreeSettingsPanels.md` | **01** | — |
| `defects/Closed-Low-Localization-RetiredMockWordingStillShips.md` | **01** — with §10.2 documentation hygiene | — |
| `defects/Closed-Partial-Low-Notifications-ToastActivationShowsTodoDialog.md` | **01** — delete the placeholder dialogs | 08 — proves the real activation path |

**Every `specs/001` box is closed** (`173/173`), so no template below re-opens that feature. Its one
dated follow-up is the **SC-007 / SC-008 30-day re-check due 2026-10-12** — a measurement, not a task.
