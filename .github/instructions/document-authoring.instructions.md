---
name: 'Document authoring'
description: 'Audience-first authoring standard for every document the agent produces: end-user guides, quick-review summaries, technical references, SOPs and proposals. Use when creating, restructuring or reviewing documentation, when choosing an output format (Markdown, PDF, Word, spreadsheet), or when deciding how much detail a document needs.'
applyTo: '**/*'
---

# Document authoring

A document is finished when its intended reader can act on it. Optimize for that reader, not for
volume of output.

## 1. Identify the audience before writing

Pick the document type from the reader, not from the subject.

| Type | Reader | Shape |
| --- | --- | --- |
| End-user | Application users, operators, supervisors, admin staff | Task steps, plain language, expected results |
| Quick-review | Managers, supervisors, stakeholders, reviewers, decision-makers | Purpose first, findings, impact, decisions |
| Technical | Developers and maintainers | Architecture, files, data flow, setup, verification |
| SOP | Anyone repeating an operational task | Purpose, scope, steps, exceptions |
| Proposal | Anyone approving a change | Current state, proposal, benefit, risk, decision needed |

Choose the output format by how the document will be used, and produce more than one format only
when asked or clearly useful:

- Markdown for instructions, technical references and repository documentation.
- PDF for printable guides, formal reports and procedures — render and inspect it, never assume.
- Word for documents the reader will edit — use the `md-to-docx` skill rather than hand-writing XML.
- Spreadsheet for structured data, tracking and calculations.

When the audience is not stated, infer it from the request, default to plain language with accurate
technical content, and ask one clarification question only when the audience would change the document
materially.

## 2. Write for the reader you identified

**End-user.** Everyday words, terms explained where they are unavoidable, numbered steps, the expected
result after each step, and a warning wherever a wrong action causes harm. Explain what the user does,
not how the code works. Screenshots only when they materially help. The reader must not need a
developer to finish the task.

**Quick-review.** Purpose in the first lines, then problem, proposed change and expected outcome.
Short sections with descriptive headings. Tables for options and comparisons. Risks, dependencies and
decisions called out. Technical depth only where the decision needs it. Label each claim as confirmed
finding, recommendation or assumption. The reader must reach a decision without reading a full report.

**Technical.** Correct terminology, not simplified. Component relationships, the files and types that
change, data flow, configuration, verification.

**SOP.** Purpose, scope, responsibilities, prerequisites, numbered procedure, safety notes, expected
result, exception handling.

**Proposal.** Present the improvement as proposed. Never write planned work as though it already exists.

## 3. Structure

Title, then purpose, then information in the order the reader needs it. Group related material, keep
heading levels consistent, and do not restate the same point in two sections. Close with next steps or
references where that helps.

Longer documents add a table of contents, a glossary for specialized terms, references for outside
sources, and an index when the length justifies it. Print-ready documents start major chapters on a new
page. Short documents get none of these unless they earn their space.

## 4. Prose

Short direct sentences, active voice, acronyms expanded on first use, one term per concept, paragraphs
broken up, lists for parallel items, tables for comparisons. Remove filler and inflated wording.
Explain why a technical finding matters in the reader's terms. Never trade accuracy for readability.

## 5. Presentation

Clear hierarchy, consistent typography and spacing, readable type, tables where they help, callouts for
warnings and decisions, diagrams where they beat prose, page numbers on long documents.

Avoid decorative color, icons and bold as decoration, crowded pages, tiny text, and large gaps left by
bad pagination.

For PDF: use the requested paper size and orientation, respect margins, keep tables and figures legible,
avoid splitting related content across pages, and open the rendered pages to confirm pagination,
margins, clipping and readability. A successful generator run is not evidence of correct formatting.
Workflow diagrams in `docs/diagrams/` follow
`.github/instructions/diagram-authoring.instructions.md` instead — edit the JSON spec, never the output.

A QOL or UI proposal is not complete without its design: it carries the section
`# QOL Refactor — Before and After Design` with A–E, and embeds the rendered BEFORE, AFTER and
comparison images. ASCII sketches, Markdown boxes and plain-text layouts are not substitutes. Follow
`docs/ui-designs/ui-designs-guide.md` and the `qol-ui-design` skill; a diagram records current behaviour
and never authorises a change.

## 6. Accuracy and sources

Repository files and supplied material are the only sources. Do not invent features, behavior,
requirements or implementation detail. Preserve terminology and technical relationships. Keep existing
and proposed functionality visibly separate. Note assumptions and open questions instead of resolving
conflicts silently. Cite source files, diagrams or requirements when it helps the reader check the work.
Never claim code, tests or behavior were verified unless they were. State what is unknown.

## 7. Length

As long as the purpose requires, and no longer. End-user documents carry every step needed and no
implementation detail. Quick-review documents carry findings, decisions, impact and recommendations,
with supporting detail moved to an appendix. Technical documents organize complexity rather than hide
it. A summary is not a report, and a report is not a manual.

## 8. Review before delivering

Check that the document matches its audience, states its purpose immediately, reads in a workable order,
uses language the reader knows, and makes no unsupported technical claim. Confirm nothing important is
missing and nothing unnecessary remains, headings and formatting are consistent, tables and figures are
readable, and spelling and layout are clean. For PDF, inspect the rendered pages.

## Boundaries

- Chat replies follow `.github/instructions/response-format.instructions.md`; this file governs
  documents, not the chat channel.
- Do not restate, duplicate or contradict the host document's own conventions: this repository's
  Markdown files keep the frontmatter, heading style and file naming already in use under `docs/`,
  `specs/` and `.github/`.
- Documentation-only changes need no build or test run unless the repository tests that documentation.
