# Deliverables

The specification is always written; the rest depend on the flags used. If a target file already exists,
write a versioned alternative (`…_Report_v2.pdf`) rather than overwriting.

| File | When |
| --- | --- |
| `Dependency_Update_and_Modernization_Report.pdf` | Always, unless `--markdown-only` |
| `DEPENDENCY_AND_MODERNIZATION_SPECIFICATION.md` | Always |
| `REPORT_SUMMARY.md` | `--markdown-only` only — the report's executive summary, approval summary and final recommendations |
| `register.json` | `--json` only — see [proposal-register.md](./proposal-register.md#machine-readable-register---json) |

## PDF report

`Dependency_Update_and_Modernization_Report.pdf` — for the application owner. The reader understands
their applications and general computing, not package management or implementation detail. Explain
technical consequences in terms of security, speed, stability, usability and maintenance cost.

### Section order

1. **Cover** — workspace name, report title, research date, report version, repositories, projects and
   dependencies reviewed, available updates, modernization opportunities.
2. **Executive summary** — overall health, update status, security concerns, performance, modernization,
   UX, AI development, highest-value recommendations.
3. **Update overview** — table: package, current, latest stable, recommended, projects, priority,
   decision.
4. **Per-package assessment** — versions, projects and files, why it matters, performance and security
   effect, compatibility, required work, risks, effort, decision.
5. **Custom functionality and modernization** — existing behaviour, recommended alternative, affected
   files, why it should change, benefit by dimension, risks, effort, decision.
6. **Development tools and AI optimization** — Copilot, agents, MCP servers, extensions, instructions,
   navigation, documentation retrieval, testing, productivity, with approval status.
7. **Workspace-wide changes** — shared files, libraries, framework-level moves, multi-project impact.
8. **Implementation roadmap** — approved work in phases with priority, benefit, effort and dependencies;
   deferred and pending items shown separately.
9. **Testing and risk** — validation approach and the risks that matter.
10. **Approval summary** — approved, rejected, deferred, pending, more-research.
11. **Final recommendations** — the highest-value approved items.
12. **Sources** — clickable official references.

### Print contract

US Letter 8.5 × 11 in, portrait. Consistent heading hierarchy, readable margins, subtle professional
colour, clear priority indicators, page numbers, header and footer, table of contents, clickable links,
tables that paginate without splitting a row, no clipped or overflowing text. It must read as a report
for management, not as raw technical documentation.

After rendering, open it and check: every page renders, nothing is clipped, tables are intact, links
work, findings are complete, approval statuses match the register.

## Specification

`DEPENDENCY_AND_MODERNIZATION_SPECIFICATION.md` — input to `/speckit.specify`. Detailed enough that the
next implementation agent can plan without re-doing the research.

### Section order

1. Overview — objectives and scope.
2. Workspace context — repositories, projects, frameworks, architecture, runtimes, SDKs, package
   management.
3. Dependency inventory — every dependency with declared and resolved versions, support status, update
   availability.
4. Approved dependency updates — package, current and target version, projects, required work,
   compatibility, benefit, breaking changes, security impact, migration, sources, approval ID.
5. Existing custom functionality — path, class or method, current behaviour, limitation, reason to
   modernize, current decision.
6. Approved replacements — ID, existing solution, replacement, package or API, version, alternatives
   considered, comparison, benefit, risk, compatibility, dependencies, effort, approval.
7. File-level change requirements — path, existing implementation, required modification, owning
   proposal, reason, mandatory or optional, expected outcome, approval ID.
8. Cross-project changes — shared libraries, shared components, consolidation, central configuration.
9. AI development environment — instructions, agents, prompts, skills, MCP servers, extensions,
   retrieval, navigation, context improvements, permission requirements. Separate what is installed
   from what is proposed.
10. Security requirements — CVEs, package trust, licensing, approved improvements.
11. Performance requirements — evidence-backed opportunities and how each is validated.
12. UX and maintainability requirements.
13. Architecture and compatibility requirements.
14. Implementation plan — ordered phases, each with objective, projects, packages, files, changes,
    prerequisites, phase dependencies, approval IDs, completion criteria.
15. Testing and verification strategy.
16. Acceptance criteria — checkable items only. Cover: approved packages at their target versions,
    restore succeeds, affected projects compile, existing behaviour preserved, approved replacements
    implemented, no unnecessary dependency added, identified security issues addressed, performance
    claims validated where applicable, relevant tests pass, AI workflow changes validated, and **no
    rejected or unapproved change implemented**.
17. Risks and constraints.
18. Out of scope — unrelated work and rejected proposals.
19. User decisions and approval register — the authoritative table from
    [proposal-register.md](./proposal-register.md#register-table).
20. Decisions required — pending decisions and open research questions.
21. Research references — packages, release notes, migration guides, advisories, tool docs.

### Spec Kit readiness

1. Describe outcomes, not prescribed internals.
2. Record existing behaviour that must survive.
3. Name every affected project and shared component.
4. Keep file-level evidence accurate; no invented paths.
5. Make acceptance criteria specific and testable.
6. Separate mandatory work from optional improvement.
7. State phase dependencies.
8. Carry risks and constraints.
9. Protect existing business logic.
10. Preserve each decision verbatim.

Scope follows decisions: `APPROVED` may appear as a requirement, `REJECTED` is excluded, `DEFERRED` is
future-only, `PENDING_USER_DECISION` stays open, `MORE_RESEARCH_REQUIRED` is marked incomplete, and
`KEEP_CURRENT` explicitly protects what exists.

## Cross-document consistency

Counts, recommended versions, affected files, priorities, security findings, proposal IDs and decisions
must agree between the two documents. Run `scripts/check-deliverables.ps1` and fix every failure before
reporting completion.
