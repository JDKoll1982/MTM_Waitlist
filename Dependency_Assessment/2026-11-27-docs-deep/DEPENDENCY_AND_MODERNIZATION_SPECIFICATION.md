# Dependency and Modernization Specification — Documentation Accuracy (docs area)

**Workspace:** MTM_Waitlist (`JDKoll1982/MTM_Waitlist`)
**Audit area:** `docs` only — drift between `docs/` and code, stale instructions, duplicated guides, orphaned references
**Research date:** 2026-11-27 · **Depth:** deep · **Audit output:** `Dependency_Assessment/2026-11-27-docs-deep/`
**Companion report:** `Dependency_Update_and_Modernization_Report.pdf`

This document is the input to `/speckit.specify`. It records what was measured, what was approved, and
what an implementation agent must change. Nothing has been implemented: no source, instruction, resource,
database artefact or package manifest was edited by this audit.

---

## 1. Overview

### Objectives

Bring this repository's documentation back in line with the code it describes, so that an agent or a
new maintainer following it lands on a file that exists and a statement that is still true.

### Scope

Only the `docs` area was researched. Areas A (dependencies), B (code modernization), C (AI tooling beyond
documentation accuracy), D (database), E (security), F (performance), G (UX) and H (tests) were **not**
researched and appear nowhere except in §18.

### Result

16 proposals: 14 approved, 2 keep-current, 0 rejected, 0 deferred, 0 pending. Every claim was verified
against the working tree on 2026-11-27; no external source was needed and none was used.

### How the work was measured

Every `.md` under `docs/` (74 prose documents; the 5,866 CSVs under `docs/database/` are read-only
reference data and were excluded), plus `AGENTS.md`, `.github/copilot-instructions.md`, the 14 files under
`.github/instructions/` and the 13 notes under `.github/memories/repo/`, was scanned for:

- relative Markdown links whose target does not exist — 58 candidates, 29 of them `data:` URIs and so not
  references at all;
- inline-code paths whose file does not exist — 558 candidates: 52 repo-relative misses, 3 relative to a
  retired module folder, 376 bare or external names that are not repo paths;
- inline-code `Type.Member` references whose leaf identifier appears nowhere in C#, XAML or RESW — 29
  candidates, of which 20 are external framework APIs, configuration keys, database columns or a package
  name;
- claims the code contradicts (project count, XAML count, audit-test count).

Every surviving candidate was then read in context, so a document that already says "retired" or
"deleted" is not reported as drift.

## 2. Workspace Context

| Fact | Value |
| --- | --- |
| Solution | `MTM_Waitlist.sln`, 17 projects, one target framework each |
| Framework | .NET 10, `net10.0-windows10.0.19041.0`, WinUI 3, unpackaged |
| Repository | single root, 1 workspace folder, branch `master` |
| Documentation surface | 74 prose docs under `docs/`; `AGENTS.md`; `.github/copilot-instructions.md`; 14 instruction files; 13 memory notes; 82 skills; 92 agents |
| Reference data | 5,866 tab-delimited CSVs under `docs/database/` (read-only, excluded) |
| Tooling used | `dotnet` SDK, PowerShell 7, Python 3, Microsoft Edge (headless PDF render) |

## 3. Dependency Inventory

**Not researched in this run.** `docs` was the only area requested; no package version, license or
advisory was inspected and this document makes no claim about any dependency. Nothing approved here
changes the solution's inventory.

## 4. Approved Dependency Updates

**None.** No dependency change is proposed or approved by this run.

## 5. Existing Custom Functionality

Nothing here is software being replaced; each row is a *document* whose statements no longer match the
repository. Paths are repository-relative; lines are as at 2026-11-27.

| Ref | Document | Current behaviour (what it says) | Limitation (what is true) |
| --- | --- | --- | --- |
| DEV-001 | `.github/instructions/spec-kit.instructions.md` lines 61, 64 | Calls `WeekendProject/PromptFiles/*` authoritative for checklists and names `WeekendProject/Module_Mock/Spec.md` as the first feature seed | The `WeekendProject/` tree does not exist |
| DEV-002 | `.github/instructions/spec-kit.instructions.md` line 45 | Says the Companion's `write-context.py` / `doctor.py` / `drift.py` calls should use `python` | `.specify/extensions/companion/scripts/` holds 17 scripts and no `drift.py` |
| DEV-003 | `docs/living-specs/living-specs-drift-ledger.md` | A living spec citing six files | `FEATURES.md`, `OPEN-TASKS.md`, `RELEASE-NOTES.md`, `DeveloperUI-VisualPrompts.md`, `VALIDATION-PROMPT-SERVER.md` and `drift.py` do not exist; `specs/001/tasks.md` is now `specs/001-module-mock-visual-fallback/tasks.md` |
| DEV-004 | `docs/diagrams/README.md`, `docs/diagrams/diagram-workflow-refactor-notes.md` | "Five workflows… the ten documents", with eight PDF links under `pdf/<workflow>/` | Only `pdf/startup-launch/` exists (2 PDFs); `dot/` holds 5 startup-launch sources and nothing for the other four workflows |
| DEV-005 | `docs/startup/startup-rebuild-design-brief.md` | Says "Nothing here is implemented yet" and points at `STARTUP-REBUILD-CHECKLIST.md` | Feature 010 shipped; the checklist file does not exist; 27 of its file references no longer resolve |
| DEV-006 | five QOL proposal docs under `docs/ui-designs/startup-launch/**` | Link `name-the-authority-before.png` etc. as siblings | The PNGs exist in `pngs/` beside each proposal, so every image is broken |
| DEV-007 | `.github/copilot-instructions.md` line 95 | Offers `WAITLIST_REQUEST_WORKFLOW_TASKS.md` as a checklist example | No such file exists |
| DEV-008 | `.github/instructions/test-conventions.instructions.md` | Cites `Module_Mock/RetiredSymbolAuditTests.cs` | The file is `MTM_Waitlist.Tests/Module_Mock/RetiredSymbolAuditTests.cs` |
| DEV-009 | `.github/instructions/winui3-adoption.instructions.md` line 11 | Says this repository has 65 XAML files | It has 92 |
| DEV-010 | `.github/memories/repo/tasks-md-id-allocation.md` | Cites `specs/001/tasks.md` | The spec folder is `specs/001-module-mock-visual-fallback/` |
| DEV-011 | `.github/memories/repo/workflow-diagrams.md` | Cites four diagram PDFs at flat `docs/diagrams/pdf/<name>.pdf` | Only `docs/diagrams/pdf/startup-launch/diagram-startup-launch{,-friendly}.pdf` exist |
| DEV-012 | `docs/scripts/scripts-automation-index.md` | Lists `scripts/assets/tooltip-inventory/{summary,tooltip-inventory,tooltip-missing}.json|txt` as repo artefacts | `scripts/assets/tooltip-inventory/` does not exist; the file is produced by `scripts/assets/Generate-TooltipInventory.ps1` |
| DEV-013 | `docs/scripts/scripts-store-publish-app-payload.md` | Links two `file:///C:/Users/jkoll/source/repos/MTM_Loader_WinUI3/...` documents | Absolute paths on another workstation and another repository; both dead, and both expose a second profile name |
| DEV-014 | `AGENTS.md` line 29 | "The test project carries 17 `*AuditTests` guardrails" | There are 18 source `*AuditTests.cs` files |
| DEV-015 | 12 records under `docs/defects/` | Cite paths and symbols as they were when written | Several have since moved or been retired. Decision: **keep current** — a defect record is a snapshot |
| DEV-016 | `docs/research/research-waitlist-card-layout-pre-spec.md` | Cites pre-spec paths | Decision: **keep current** — a research record is a snapshot |

## 6. Approved Replacements

Each row replaces a stale statement with a verified one. No code, package or API is introduced.

| ID | Existing solution | Replacement | Alternatives considered | Benefit | Risk | Effort | Approval |
| --- | --- | --- | --- | --- | --- | --- | --- |
| DEV-001 | Two sentences pointing at `WeekendProject/` | Point at `specs/001-module-mock-visual-fallback/` for the seed and at `.github/skills/checklist-creation` / `checklist-execution` for the authoritative checklists | Delete both sentences (loses the seed pointer); leave as is (agent follows a dead path) | An agent following the instruction lands on a real folder | Marginal behaviour change in always-applied guidance; the replacement names the same artefacts | Small | APPROVED |
| DEV-002 | `drift.py` named beside two real scripts | Name `doctor.py` only | Drop the script names; keep only the `python` vs `python3` warning | The warning stays while naming only what exists | None identified | Small | APPROVED |
| DEV-003 | Six dead references plus one wrong spec path | Repoint the spec path; remove the five references to files that are gone | Leave it (a drift ledger with dead links misleads); rewrite it (loses history) | A living document that can be followed | Removing a reference loses the record that the artefact existed — say so in the line | Small | APPROVED |
| DEV-004 | README and refactor notes describe five workflows | README describes the one workflow that exists and says the other four were removed with their flows; the notes' dead links are removed | Regenerate the four workflows (not approved — their source material is gone); delete the README | No dead links; the surviving diagrams stay documented | If the four flows are paused rather than removed the wording is wrong — the implementer must confirm | Small | APPROVED |
| DEV-005 | A shipped feature's brief lives in `docs/startup/` and reads unbuilt | Move to `specs/010-startup-rebuild/` | Mark it shipped in place and fix 27 paths; delete it (loses rationale) | `docs/` stops claiming an unbuilt feature; rationale survives beside its spec | The move changes a path other documents may cite | Small | APPROVED |
| DEV-006 | Sibling image links | Prefix the three links in each proposal with `pngs/` | Copy the PNGs beside the proposals (duplicates 15 files) | Rendered proposals show their BEFORE/AFTER images | None | Small | APPROVED |
| DEV-007 | A checklist filename that never existed here | Use `.github/skills/checklist-execution/` and `checklist.md` as the example | Drop the example | Instructions point at the real mechanism | None | Small | APPROVED |
| DEV-008 | `Module_Mock/RetiredSymbolAuditTests.cs` | `MTM_Waitlist.Tests/Module_Mock/RetiredSymbolAuditTests.cs` | Leave the short form | A path the reader can open | None | Small | APPROVED |
| DEV-009 | "65 XAML files" | State 92, dated | Remove the counts | The measurement matches the tree | Will age again | Small | APPROVED |
| DEV-010 | `specs/001/tasks.md` | `specs/001-module-mock-visual-fallback/tasks.md` | Leave it | The note can be followed | None | Small | APPROVED |
| DEV-011 | Four flat PDF paths | Cite `docs/diagrams/pdf/startup-launch/…`; state the other four workflows are gone | Remove the diagram section | The memory note matches the tree | None | Small | APPROVED |
| DEV-012 | Three artefacts presented as committed files | Say they are outputs of `Generate-TooltipInventory.ps1` | Remove the three references | A reader stops hunting generated files | None | Small | APPROVED |
| DEV-013 | Two absolute links to another machine | Replace with the in-repo script the document describes, or remove | Leave them | No private workstation path; no dead link | The quoted design documents are not in this repo, so the sentence must say what it quotes | Small | APPROVED |
| DEV-014 | "17 `*AuditTests`" | 18 | Remove the number | Accurate guardrail count | Ages; pair with the date already in the file | Small | APPROVED |

## 7. File-Level Change Requirements

| Path | Existing implementation | Required modification | Owning proposal | Mandatory | Expected outcome |
| --- | --- | --- | --- | --- | --- |
| `.github/instructions/spec-kit.instructions.md` | L61 cites `WeekendProject/PromptFiles/*`; L64 names the seed as `WeekendProject/Module_Mock/Spec.md`; L45 names `drift.py` | Repoint L61/L64 to `specs/001-module-mock-visual-fallback/` and the two checklist skills; replace `drift.py` with `doctor.py` in L45 | DEV-001, DEV-002 | Mandatory | Every path in the file resolves |
| `docs/living-specs/living-specs-drift-ledger.md` | Six unresolvable references | Repoint `specs/001/tasks.md`; remove the five references to files that are gone | DEV-003 | Mandatory | Every reference resolves |
| `docs/diagrams/README.md` | Table of five workflows, eight dead PDF links | Keep the startup-launch row; state that the other four were removed with their flows | DEV-004 | Mandatory | No dead links |
| `docs/diagrams/diagram-workflow-refactor-notes.md` | Eight flat `./pdf/*.pdf` links | Remove or repoint; keep the refactor narrative | DEV-004 | Mandatory | No dead links |
| `docs/startup/startup-rebuild-design-brief.md` | Says nothing is implemented | Move to `specs/010-startup-rebuild/startup-rebuild-design-brief.md` | DEV-005 | Mandatory | The brief is a record beside its feature |
| five `docs/ui-designs/startup-launch/**/…-proposal.md` | Sibling image links | Prefix `pngs/` | DEV-006 | Mandatory | Images render |
| `.github/copilot-instructions.md` | L95 example filename | Use `.github/skills/checklist-execution/` | DEV-007 | Mandatory | The example exists |
| `.github/instructions/test-conventions.instructions.md` | Short audit-test path | Full path | DEV-008 | Mandatory | The path resolves |
| `.github/instructions/winui3-adoption.instructions.md` | L11 says 65 XAML files | Say 92, dated | DEV-009 | Mandatory | The count matches the tree |
| `.github/memories/repo/tasks-md-id-allocation.md` | `specs/001/tasks.md` | Full spec folder path | DEV-010 | Mandatory | The path resolves |
| `.github/memories/repo/workflow-diagrams.md` | Four flat PDF paths | Point at `pdf/startup-launch/`; state the other four are gone | DEV-011 | Mandatory | The paths resolve |
| `docs/scripts/scripts-automation-index.md` | Three artefact paths | Describe them as generated outputs | DEV-012 | Mandatory | No missing files are implied |
| `docs/scripts/scripts-store-publish-app-payload.md` | Two `file:///C:/Users/jkoll/...` links | Replace with the in-repo script reference or remove | DEV-013 | Mandatory | No dead, machine-specific link |
| `AGENTS.md` | "17 `*AuditTests` guardrails" | 18 | DEV-014 | Mandatory | The count is accurate |

## 8. Cross-Project Changes

None. Every approved change is confined to a documentation or instruction file; no project, package or
shared component is touched and no `.csproj` change is implied.

## 9. AI Development Environment

Only the **accuracy of existing guidance** was assessed, and only because a stale instruction is
documentation drift. Nothing was proposed about which model, agent, skill or MCP server to use; Area C
was not researched.

Installed and in force (unchanged): 82 skills, 92 agents, 14 instruction files, 13 memory notes,
`.mcp.json` with the `microsoft-learn` and `context7` servers, and `.specify/` with the Companion
extension.

Approved accuracy changes: DEV-001, DEV-002, DEV-007, DEV-008, DEV-009, DEV-010, DEV-011, DEV-014.

Note for the implementer: these files are always applied or auto-loaded, so each sentence must keep its
meaning. Do not restructure them and do not add guidance.

## 10. Security Requirements

No security work was researched. One approved change removes a security-adjacent hygiene problem:
DEV-013 removes two absolute `file:///C:/Users/jkoll/...` links, which disclosed another workstation's
user-profile path inside this repository. No credential, connection string or token was read, quoted or
committed by this audit; `appsettings.json` was not inspected beyond confirming it is tracked.

## 11. Performance Requirements

Not in scope. No approved change affects runtime behaviour: all 16 proposals are documentation edits.

## 12. UX and Maintainability Requirements

The benefit is that a person or an agent following the documentation stops hitting dead ends:

- every path cited by an in-force instruction resolves;
- the living drift ledger resolves;
- a shipped feature no longer reads as unbuilt;
- QOL proposals render their own images.

## 13. Architecture and Compatibility Requirements

- **No code change.** This specification is not permission to alter C#, XAML, resources, database
  artefacts or package manifests.
- The repository's guards stay in force: no `<Page>` items, no inline SQL, no demo or mock mode; nothing
  here reopens them.
- Instruction files keep their frontmatter (`description`, `applyTo`) and their voice.

## 14. Implementation Plan

### Phase 1 — Restore trust in live guidance (no dependencies)

Objective: following always-applied guidance never lands on a missing file.
Files: `.github/instructions/spec-kit.instructions.md`, `.github/copilot-instructions.md`,
`.github/instructions/test-conventions.instructions.md`,
`.github/instructions/winui3-adoption.instructions.md`, `AGENTS.md`.
Proposals: DEV-001, DEV-002, DEV-007, DEV-008, DEV-009, DEV-014.
Completion: `WeekendProject`, `drift.py`, `WAITLIST_REQUEST_WORKFLOW_TASKS.md`, the short audit-test path
and the "65 XAML" claim each return zero hits; `AGENTS.md` says 18.

### Phase 2 — Repair living documents (after Phase 1)

Files: `docs/living-specs/living-specs-drift-ledger.md`. Proposals: DEV-003.
Completion: the reference scan finds no dead path in the ledger.

### Phase 3 — Retire and reduce stale record-shaped material (after Phase 2)

Files: `docs/startup/startup-rebuild-design-brief.md` (moved), `docs/diagrams/README.md`,
`docs/diagrams/diagram-workflow-refactor-notes.md`. Proposals: DEV-004, DEV-005.
Completion: `docs/startup/` no longer holds the brief; every diagram README link resolves and its prose
names what was removed.

### Phase 4 — Path hygiene in notes, scripts and proposals (independent of Phases 1–3)

Files: five `docs/ui-designs/**/…-proposal.md`; `docs/scripts/scripts-automation-index.md`;
`docs/scripts/scripts-store-publish-app-payload.md`; `.github/memories/repo/tasks-md-id-allocation.md`;
`.github/memories/repo/workflow-diagrams.md`. Proposals: DEV-006, DEV-010, DEV-011, DEV-012, DEV-013.
Completion: the five proposals' images render; each cited path resolves; no `file:///C:/Users/` string
remains under `docs/`.

### Protected by decision

DEV-015 and DEV-016 are `KEEP_CURRENT`: the 12 defect records and the pre-spec research record keep the
paths they were written with, and an implementation must not "tidy" them.

## 15. Testing and Verification Strategy

| Change | Verification | Acceptance signal |
| --- | --- | --- |
| Any path or link repointed | Re-run the documentation reference scan (Markdown links + inline-code paths over `docs/**/*.md`, `AGENTS.md`, `.github/instructions/**`, `.github/memories/repo/**`) | 0 dead references in every file the phase touched |
| Instruction wording | Read the diff for meaning preservation; frontmatter intact | Only the reference changed; no guidance added or removed |
| Proposal image links | Open the five proposals in a Markdown renderer | Three images render per proposal |
| Diagram README | Follow every link | No 404 inside the tree |
| Moved brief | The file exists at the new path and nowhere else | One copy, under `specs/010-startup-rebuild/` |
| Guardrail counts | `Get-ChildItem -Recurse -Filter *AuditTests.cs` excluding `bin`/`obj` | 18 files, matching `AGENTS.md` |

No build, test or restore is required by any approved change; documentation-only changes need no build
validation in this repository.

## 16. Acceptance Criteria

1. Every path cited in the files listed in §7 resolves from the repository root, or from the citing
   document for its relative links.
2. `WeekendProject` appears nowhere in `.github/instructions/spec-kit.instructions.md`.
3. `drift.py` appears nowhere in `.github/instructions/spec-kit.instructions.md`.
4. `docs/living-specs/living-specs-drift-ledger.md` cites only files that exist.
5. `docs/startup/startup-rebuild-design-brief.md` no longer exists; `specs/010-startup-rebuild/` holds it.
6. `docs/diagrams/README.md` links only to PDFs that exist and states that four workflows were removed.
7. Each of the five QOL proposals renders its BEFORE, AFTER and comparison images.
8. `AGENTS.md` states 18 `*AuditTests` guardrails.
9. `.github/instructions/winui3-adoption.instructions.md` states the current XAML count with its date.
10. No `file:///C:/Users/` string remains anywhere under `docs/`.
11. No rejected, deferred or unapproved change is implemented; DEV-015 and DEV-016 records are untouched.
12. Existing behaviour is preserved: no source, resource, database or manifest file changes.

## 17. Risks and Constraints

| Risk | Likelihood | Mitigation |
| --- | --- | --- |
| Rewording always-applied guidance changes agent behaviour | Low | Change only the reference inside each sentence; no guidance added or removed; review the diff line by line |
| Deleting a ledger reference loses the record that an artefact existed | Medium | State in the ledger that the artefacts were removed rather than silently dropping the line |
| "Removed" is the wrong word for the four diagram workflows if they are only paused | Medium | The implementer confirms with the owner; the README wording must match the truth |
| Moving the design brief breaks another document's link | Low | Grep for the old path before and after the move |
| Counts age again | Certain | Pair each count with its date, as `AGENTS.md` already does for versions |

## 18. Out of Scope

- Dependencies and package versions (Area `dependencies`) — not researched.
- Custom code versus framework capability (Area `modernization`) — not researched.
- AI tooling beyond documentation accuracy (Area `ai-tooling`) — not researched; 82 skills and 92 agents
  were inventoried, not assessed.
- Security, performance, UX, architecture, tests, database and localization areas — not researched.
- Code changes of any kind.
- DEV-015 and DEV-016: `KEEP_CURRENT`, so the 12 defect records and the research record are excluded from
  scope by decision.

## 19. User Decisions and Approval Register

| Proposal ID | Proposed Change | Category | Priority | Decision | Notes |
| --- | --- | --- | --- | --- | --- |
| DEV-001 | Repoint the `WeekendProject/` references in the spec-kit instruction | docs | medium | APPROVED | Seed spec and the two checklist skills named instead |
| DEV-002 | Replace `drift.py` with `doctor.py` in the spec-kit instruction | docs | medium | APPROVED | The Companion has no `drift.py` |
| DEV-003 | Repair the living drift ledger's references | docs | medium | APPROVED | One wrong spec path, five references to files that are gone |
| DEV-004 | Reduce the diagrams README to what exists | docs | medium | APPROVED | Chosen over regenerating: the source material for four workflows is gone |
| DEV-005 | Move the shipped startup design brief to `specs/010-startup-rebuild/` | docs | medium | APPROVED | Chosen over marking it shipped in place |
| DEV-006 | Fix the QOL proposals' image links | docs | low | APPROVED | Images live in `pngs/` |
| DEV-007 | Replace the checklist example filename | docs | low | APPROVED | Points at the checklist-execution skill |
| DEV-008 | Correct the audit-test path in test conventions | docs | low | APPROVED |  |
| DEV-009 | Correct the XAML count in the design-system instruction | docs | low | APPROVED | 92, dated |
| DEV-010 | Correct the spec path in the memory note | docs | low | APPROVED |  |
| DEV-011 | Correct the diagram PDF paths in the memory note | docs | low | APPROVED |  |
| DEV-012 | Mark the tooltip-inventory artefacts as generated | docs | low | APPROVED |  |
| DEV-013 | Remove the two `file:///C:/Users/jkoll/...` links | docs | low | APPROVED | Also removes a second profile path from the repository |
| DEV-014 | Correct the audit-test count in `AGENTS.md` | docs | low | APPROVED | 17 → 18 |
| DEV-015 | 12 defect records cite moved or retired paths | docs | low | KEEP_CURRENT | A defect record is a snapshot of the evidence at the time |
| DEV-016 | Pre-spec research record cites pre-spec paths | docs | low | KEEP_CURRENT | A research record is a snapshot |

## 20. Decisions Required

None. All 16 proposals carry a decision; none is `PENDING_USER_DECISION` and none needs
`MORE_RESEARCH_REQUIRED`.

## 21. Research References

No external source was used. Every claim was verified against this repository on 2026-11-27:

| Evidence | How it was established |
| --- | --- |
| `WeekendProject/` absent | `Test-Path WeekendProject` → False |
| Companion script list | `Get-ChildItem .specify/extensions/companion/scripts -File` → 17 scripts, no `drift.py` |
| Missing ledger targets | `Test-Path` on `FEATURES.md`, `OPEN-TASKS.md`, `RELEASE-NOTES.md`, `DeveloperUI-VisualPrompts.md`, `VALIDATION-PROMPT-SERVER.md` → all False |
| Diagrams | `Get-ChildItem docs/diagrams/pdf -Recurse` → only `startup-launch/diagram-startup-launch{,-friendly}.pdf`; `dot/` → 5 startup-launch sources |
| Startup brief | `docs/startup/startup-rebuild-design-brief.md` lines 1-4; `Test-Path STARTUP-REBUILD-CHECKLIST.md` → False |
| QOL images | `Get-ChildItem …\name-the-authority\pngs` lists the PNGs the proposal links as siblings |
| `WAITLIST_REQUEST_WORKFLOW_TASKS.md` | `Test-Path` → False |
| Audit-test path | `Get-ChildItem MTM_Waitlist.Tests -Recurse -Filter *AuditTests.cs` → includes `Module_Mock\RetiredSymbolAuditTests.cs` |
| XAML count | `Get-ChildItem -Recurse -Filter *.xaml` excluding `bin`/`obj` → 92 |
| Spec folder | `specs/001-module-mock-visual-fallback/tasks.md` exists; `specs/001/tasks.md` does not |
| Diagram memory note | the four flat paths tested false; `pdf/startup-launch/` tested true |
| Tooltip inventory | `Test-Path scripts/assets/tooltip-inventory` → False; `scripts/assets/Generate-TooltipInventory.ps1` exists |
| Workstation links | literal `file:///C:/Users/jkoll/...` in `docs/scripts/scripts-store-publish-app-payload.md` |
| Audit-test count | 18 files matching `*AuditTests.cs` excluding `bin`/`obj` |
