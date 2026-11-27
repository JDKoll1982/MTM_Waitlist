---
name: qol-run-whole-workflow
description: 'Run the QOL and diagram workflows end to end on one screen change: map the flow, design before and after, verify, present for approval, then implement and compare.'
disable-model-invocation: true
argument-hint: 'workflow id (or flow name), screen, refactor slug, what improves'
---
Read `docs/ui-designs/ui-designs-09-run-the-whole-workflow.md` and execute it exactly as written. That
file is the single source of this procedure; do not restate or improvise around it.

Fill its inputs from the values below. An input left empty is asked for at the point the run needs it,
using the ask list in the procedure - not guessed.

If anything else the run needs is missing - a file, a folder, access to the running application, a sign-in
session, an approval or a decision - ask for it and wait. Ask in plain language for the person who wanted
the change: one short sentence for what is missing and one for what it is needed for, at most three
choices when a choice is real, no file paths, no tool output, no JSON, no jargon. Never start a phase that
depends on an answer you do not have, and never invent a value, a screenshot or an approval to keep going.

| Input | Value |
| --- | --- |
| `<<WORKFLOW>>` | ${input:workflow:Workflow id from manifest.json, or the name of a flow that is not mapped yet} |
| `<<SCREEN>>` | ${input:screen:Screen or step, for example sign-in} |
| `<<REFACTOR>>` | ${input:refactor:Short slug for this run; it becomes the run folder name} |
| `<<IMPROVEMENT>>` | ${input:improvement:What gets better for the person using the screen} |
| `<<CAPTURE>>` | ${input:capture:yes when the application may be started and signed in for the BEFORE capture} |
| `<<AUDIENCE>>` | ${input:audience:developer or stakeholder} |

Stop at the end of Phase 6 with the proposal presented. Do not implement, and do not touch an
application `.cs` or `.xaml` file, until an approval has been recorded by name.
