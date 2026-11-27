# Baseline before the corrections

Recorded 2026-11-27, before any edit in this feature, so the change can be judged against a number rather than an impression. Counts are files, searched with `git grep`, excluding this feature's folder and the audit's assessment folder.

| Stale token | Files holding it | Of those, live guidance, an index or a living record |
| --- | --- | --- |
| `WeekendProject/PromptFiles` | 9 | 2 (`.github/instructions/spec-kit.instructions.md`, `docs/living-specs/living-specs-drift-ledger.md`) |
| `WeekendProject/Module_Mock` | 16 | 1 (`.github/instructions/spec-kit.instructions.md`) |
| `WAITLIST_REQUEST_WORKFLOW_TASKS.md` | 1 | 1 (`.github/copilot-instructions.md`) |
| `specs/001/tasks.md` | 2 | 2 (`docs/living-specs/living-specs-drift-ledger.md`, `.github/memories/repo/tasks-md-id-allocation.md`) |
| `file:///C:/Users/jkoll/` | 2 | 1 (`docs/scripts/scripts-store-publish-app-payload.md`) |
| `drift.py` | 5 | 2 (`.github/instructions/spec-kit.instructions.md`, `docs/living-specs/living-specs-drift-ledger.md`) |

The remaining files are historical records this feature deliberately leaves as written: defect records, earlier spec folders, and the audit's own assessment.

Counts as they stand before the change:

- Guardrail tests: `AGENTS.md` states 17; the tree holds 18 files matching `MTM_Waitlist.Tests/**/*AuditTests.cs`.
- Interface files: `.github/instructions/winui3-adoption.instructions.md` states 65; the tree holds 92 files matching `**/*.xaml` outside `bin` and `obj`.
- Diagram PDFs: `.github/memories/repo/workflow-diagrams.md` names ten; two exist, both under `docs/diagrams/pdf/startup-launch/`.
- Startup-launch proposal images: twelve links across four documents, eight of which resolve nowhere.

Build and test result before the change, for the comparison task: clean build with zero warnings, 2915 tests passed, 66 skipped, 0 failed.
