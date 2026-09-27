# Analysis Status of Record — Startup Rebuild

Feature: `010-startup-rebuild` · Loop run: 2026-09-26 · Constitution: v1.2.0 (7 principles + 3 additional constraints)

This file is the **latest analysis state**. The per-iteration reports are kept beside it:

| File | What it is |
|---|---|
| `analysis-pre-fix.md` | An earlier run's pre-fix record, kept because it carries that run's per-finding dispositions |
| `analysis-after-fixes.md` | An earlier run's post-fix verification pass |
| `analysis-iteration1.md` | This run, pass 1 — 15 findings |
| `analysis-iteration2.md` | This run, pass 2 — 13 findings |
| `analysis-iteration3.md` | This run, pass 3 — 12 findings |
| `analysis-iteration4.md` | This run, pass 4 — 13 findings |
| `analysis-iteration5.md` | This run, pass 5 — 9 findings |

## Where the loop stopped

The command's safety limit is five iterations, and the fifth pass was the last one taken, so the status is
**MAX_ITERATIONS_REACHED** — not CLEAN. Every finding any pass raised has been fixed except the three that were
independently verified as false and rejected below. The final round of edits — the fixes for the fifth pass's nine
findings — was applied after the cap, at the owner's standing instruction to fix all findings in the spec
documentation, and **has not been verified by a further independent pass**, because the loop had reached its
limit. That is the one open verification debt this file reports rather than hides.

## The findings the loop raised, by pass

| Pass | Raised | Fixed | Rejected as false |
|---|---|---|---|
| 1 | 15 | 15 | 0 |
| 2 | 13 | 12 | 1 |
| 3 | 12 | 11 | 1 |
| 4 | 13 | 12 | 1 |
| 5 | 9 | 9 (unverified) | 0 |
| **Total** | **62** | **59** | **3** |

## The three rejections, with the evidence that refuted them

1. **"T137 names a test path that does not exist."** Raised in pass 2. T137 already carried
   `MTM_Waitlist.Tests/Module_Settings/SettingsViewModelTests.cs`, which exists. The same pass's report said so
   itself in the pass-1 verification section. The *real* defect it half-pointed at — the same wrong path sitting in
   Phase 8's file inventory — was found in pass 3 and fixed there.
2. **"`STARTUP-FLOW.html` and `STARTUP-FLOW.pdf` are named for deletion but do not exist."** Raised in pass 3.
   `git ls-files` lists both: they are tracked and deleted only in the working tree, so T027's instruction is
   correct and now says so.
3. **"The logging contract's '31 files import `Microsoft.Extensions.Logging`' is wrong; the number is 56."**
   Raised in pass 4. A tree-wide search for a line that is exactly `using Microsoft.Extensions.Logging;` returns
   **31**; 56 is the count of files merely containing the substring. The contract's figure is right.

## The fifth pass's findings, and their disposition

| ID | Sev | Finding | Disposition |
|---|---|---|---|
| CG1 | HIGH | `Module_Settings/Services/UserManagementLiveIntegrationTests.cs` imports `MTM_Waitlist.Module_Startup.Services` and constructs `StartupSessionRepository`, which T015 clears and T020's procedures back — no task owned it, so T015's green-build checkpoint was unreachable | **T188** added to the Foundational addendum, in US3 Wave 3; the file added to the addendum's Files list; T185's uniqueness claim corrected to "the production file outside the startup module" |
| CA1 | MEDIUM | The destructive-operation approval (T056, Wave 17) came after T020's nine procedure deletions (Wave 5), so nine deletions had no recorded confirmation ahead of them | The approval is now recorded at each deletion point: T020 carries it, T056's wording names its own set as the second point, and `plan.md`'s Complexity Tracking paragraph says so |
| UN1 | MEDIUM | T005 keyed its guard on the `…Module_Startup.Services` and `…Module_Startup.ViewModels` *namespaces*, which the rebuilt services and view models occupy under the delivered paths — the I40 defect relocated | T005 now keys on the **removed type and member names** and the old view file names, and states why a namespace or an empty-folder assertion cannot hold (S2 decisions 1 and 2) |
| AM1 | LOW | The I41 fix's cross-reference said "section 5's table", which in that file is the never-delete list | Now names `contracts/machine-configuration-contract.md` §5 |
| IN1 | LOW | `data-model.md` E10 still said "the five seeds and aggregates" | Now "the three seeds and the five aggregate and descriptions files" |
| IN2 | LOW | The addendum placed T185/T186 "beside T111" (US3 Wave 1) while the wave line said Wave 3 | The addendum now says US3 Wave 3 |
| IN3 | LOW | `plan.md` said the reference "produced" D25 to D29, which is five identifiers for four decisions | Now D25 to D28, with D29 named as the owner's separate panel-copy decision |
| IN4 | LOW | The checklist's Phase 0 evidence recorded "20 decisions" where brief S2 holds 24 | Now 24 |
| US1 | LOW | The panel copy's ceiling is "stated" but no artifact states it, so T183/T184 could not be verified against a number | The logging contract records that the value is not yet fixed and that T183 records it there; T183 now carries that obligation |

## Prior passes, in one line each

- **Pass 1** raised the DPAPI deviation, the missing `update_table_descriptions.sql` coverage, the unretired
  `startup-diagnostics` capability, a non-existent procedure name, three wrong aggregate paths, disagreeing
  local-state and database counts, the terminal-outcome set, the `ILogger` figure's scope, SC-005's wording,
  T160's criterion, the deleted registry file, FR-025's scope and SC-008's preference list — 15 findings, all fixed.
- **Pass 2** challenged the fixes and found more: a permission-key miscount, a tooltip-key miscount, the brief's
  protected list contradicting its own deletion list, an unreproducible call-site figure, the plan and research
  quoting the 226 unscoped, T170's stale criteria range, two stale migration-map items, a misplaced capability
  entry, an underivable protected count, the superseded three-destination rule, a missing per-direction count and
  a T159/T171 overlap — 13 raised, 12 fixed, 1 rejected.
- **Pass 3** attacked the second round and found the biggest defect of the run: deleting `ISignOutService`,
  `SignOutResult` and `IAppProcessRestarter` stranded the shell with no owner, and `ShellPage.xaml.cs` still spelled
  the namespace T005 forbade. Also `IComputerRegistryService`'s stranded Settings consumers, two `StartupDebugLog`
  comment-only references, a phase gate saying three keys, "three new fields" listing four, a wrong entity
  cross-reference, a mis-stated "already dead" double, an absolute checklist-reference claim, and an underived
  protected count — 12 raised, 11 fixed, 1 rejected.
- **Pass 4** found that the C1 fix had addressed the DPAPI clause but not the constraint's destructive-operation
  clause, that T005's assertion still contradicted the delivery for services and view models, that the brief's
  S3.2 list and §6/checklist disagreed on how many keys a fresh store grants each role, plus counts, placements and
  wording — 13 raised, 12 fixed, 1 rejected.

## Metrics at the end of the run

- Requirements: **55** (38 FR + 17 SC); coverage **100%** (55/55 have ≥1 task; FR-022 is cited by T035 and T036)
- Tasks: **188** (T001–T188, contiguous, no duplicates)
- Critical issues open: **0** (the recorded DPAPI deviation is dispositioned in `plan.md` → Complexity Tracking,
  and the destructive-operation clause is addressed there and at T020/T056)
- Findings open: **0** as far as four independent passes could determine; the fifth pass's nine are fixed but
  unverified
- Rejections recorded: **3**

## Next Actions

- The artifacts are consistent enough to begin `/speckit.implement`; nothing the passes found blocks it.
- One verification debt is stated above: re-run `/speckit.analyze` once to close the unverified final round.
- The `startup-diagnostics` capability retirement belongs to T026/T163 when the feature is implemented; the
  registry and `capabilities/DRIFT.md` are deliberately edited once, at re-adoption.

## Owner-directed close-out, 2026-09-26

Two questions were put to the owner at the end of the loop and are now settled.

1. **The tracked-but-deleted files were restored.** `living-specs.yml`, `STARTUP-FLOW.html` and
   `STARTUP-FLOW.pdf` had been deleted in the working tree without being staged. All three are restored from
   `HEAD`, so the tree matches what the artifacts assume: T027 deletes the flow documents as a deliberate change,
   and T163 edits a registry that exists. The two task notes that described the deleted-in-working-tree state
   were removed with it, because they no longer describe the tree.
2. **`IComputerRegistryService` is decided.** The owner directed that the capability be recreated inside the
   Settings screen, over the rebuilt machine contracts. Recorded as **D30** in `research.md` and rewritten into
   **T187**: the Settings "Computers" panel keeps listing, adding, editing and deactivating registry rows, its
   service is rebuilt at `MTM_Waitlist.Settings/Services/ComputerRegistryService.cs`, both view models stop
   injecting the deleted interface, and T010 keeps the interface standing until that rebuild lands in Foundational
   W4 beside T015. The write path stays behind a Settings-owned seam because FR-022 makes the identity and
   machine-fact contracts read-only.
