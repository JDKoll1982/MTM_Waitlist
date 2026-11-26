# Implementation Plan — Repository Profiles for the C#/XAML Dependency Graph

**Feature**: `specs/023-extension-repo-profile` | **Branch**: `extension-profile-update` | **Size**: oversized

## Scale note

This change spans the extension's analysis core, its host wiring, its webview toolbar, its test double
and a new headless entry point, and it adds a validation set of four checkouts with a record and a
result each. There are three tracks a reader can follow: what a profile changes about classification,
what the two prompts must state for a profile to be written without guessing, and how a profile gets
created and then proven. Watch four places for risk: the layer classifier, where identical output
without a profile is a hard requirement; the file selection the headless run applies, which must
reproduce this repository's baseline counts; the records, which must be written from each checkout's
code and never from the run they judge; and the profile contract, which is now one table that both the
parser's accepted-key sets and the prompt's text come from, so an omission there is an omission
everywhere at once.

## Where this re-plan starts

FR-001 to FR-016 and User Stories 1 to 4 are built and green: 34 test files, 945 tests, `npm run
typecheck` and `npm run selftest` pass, and with no profile this repository still analyses to 1095
nodes, 4776 edges and 21 findings, unchanged from `baseline.md`. Nothing in this plan reopens that
work.

A second amendment adds User Story 6 with FR-023 to FR-028 and SC-011 to SC-013, and this re-plan
absorbs it without discarding the first one. Two things are genuinely missing from the built code, and
both are additions rather than corrections:

- The setup prompt ends with a JSON example whose keys are never described. `REPLY_SHAPE`
  (`src/graph/profilePrompt.ts`) shows six link keys and a `generatedMembers[].form` value while the
  text states no meaning, no requiredness, no admitted values and no absent-key behaviour, so the
  assistant is left to infer exactly what FR-023 and FR-024 forbid. The refresh prompt carries no format
  description at all.
- Nothing in the extension examines a repository's structure before profiling it. There is no
  compatibility check, no offer, and no request writer, so a repository the analysis cannot read meets
  whatever failure it meets and the person is given no path forward.

The first re-plan's four-checkout validation still stands and is extended here, not replaced:
`src/validate.ts` with its pure half in `src/graph/validation.ts`, the four modes, the expected records
under `expected/`, the results under `results/`, and one profile file per external checkout. What is
added to it is a verdict mode that runs before any prompt, a request mode that answers the offer without
a person present, and an attempts record that makes "the first reply imported clean" a measurement
instead of a claim. `tasks.md` is converge's to extend; this plan names the mechanism.

## Summary

Each workspace folder may hold a `csharp-xaml-graph.profile.json` file declaring its own C#/XAML
conventions: layer rules with their precedence, the calls that create registration links, the rules
findings apply, and which files are read. The analysis modules that hardcoded those conventions today
read them from a compiled profile instead, with the current constants becoming the default profile, so
a repository with no profile produces exactly today's graph. Two host commands open VS Code's Chat view
with a prepared prompt built from the repository's own evidence, and a third imports the assistant's
reply through the clipboard, validates it, merges it into the existing file and re-reads. One button in
the graph toolbar asks the assistant for conventions the profile does not cover.

Proving that on four checkouts adds one headless entry point and no dependency. `out/validate.js`
analyses a checkout given its path, loading that checkout's profile from disk and applying its file
selection, then compares the result with an expected-answer record that was written from the
checkout's code and committed with the spec. Every mismatch is listed one item at a time, naming the
type and what was expected of it. The run also records the extension's own invariants for that
checkout, and writes a result naming the profile, the measurements, the mismatches and the verdict, so
the four-of-four claim can be re-checked later without repeating the work. A fresh-context assistant
produces each profile from the setup prompt and the checkout alone, and a repeat run from the recorded
profile must produce identical measurements.

The chat mechanism is the workbench command `workbench.action.chat.open` with a partial query, guarded
by `vscode.commands.getCommands`, falling back to the clipboard when the command is absent. Its id is
documented in the `microsoft/vscode` source rather than the public API reference, and `research.md`
records that reasoning and the alternative considered. The validator imports nothing from `vscode`, so
no editor needs to be running for any part of the proof.

Two things make the prompt safe to answer from. Every key the profile accepts is described once, in a
table in `src/graph/profileContract.ts`: what it means, whether it is required, the shape of its value,
and what happens when it is absent. The parser takes its accepted-key lists from that table, and both
prompt builders render it before their first question, so the prompt cannot describe a key the parser
refuses and the parser cannot accept a key the prompt leaves out. The example that used to be the only
statement of the format stays, but it is checked against the same table.

Before any of that, a repository is measured against what the analysis can read. One path walk counts
C# files, XAML files, project files and solution files, and a pure function turns those facts into a
verdict. A repository with no C#, no XAML, more files than the analysis will read, or a project kind the
analysis does not read at all is told so by name, then offered a new specification. Answering yes writes
a request file under the extension's own `update_prompts/<repository>/`; answering no, or closing the
dialog, writes nothing and leaves the reason on the graph's Problems pane for the rest of the session.

## Project Structure

```text
extensions/csharp-xaml-graph/
  package.json                       EDIT  three commands, a `validate` script; no new dependency
  scripts/build.mjs                  EDIT  a fourth entry: src/validate.ts to out/validate.js
  README.md                          EDIT  profile format, starter example, the commands, validation
  update_prompts/                    EXISTS, empty until a request is written
  src/
    extension.ts                EDIT  read/watch the profile, the verdict gate, three profile commands
    selftest.ts                 EDIT  assert the control and a profile round trip; keeps its root arg
    structure.ts                NEW   one path walk: structure facts, and the request writer
    validate.ts                 NEW   headless run: --prompt, --check, --record/--result, --compare,
                                      --compatibility, --request
    graph/
      profile.ts                EDIT  accepted-key lists come from the contract table
      profileContract.ts        NEW   every key the format accepts: meaning, required, shape, absent
      compatibility.ts          NEW   structure types, the verdict, reason-to-change table, request text
      conventions.ts            NEW   the predicates layers/analyze/findings consume
      profilePrompt.ts          EDIT  render the contract before the questions, in both prompts
      files.ts                  NEW   the profile's include and exclude as a pure matcher
      validation.ts             NEW   record types, measurement, item-by-item comparison, attempt gate
      layers.ts                 EDIT  classifyType runs the ordered rules
      analyze.ts                EDIT  link rules from conventions; evidence on the model
      findings.ts               EDIT  entry points, transparent bases, generated members
      model.ts                  EDIT  optional profileEvidence field
    webview/
      html.ts                   EDIT  one toolbar button
      panel.ts                  EDIT  forward the profile message to the command
      client/main.ts            EDIT  wire the button; render profile and compatibility notes
      client/profileProblems.ts EDIT  render both note kinds in one block
  test/
    graph/profileContract.test.ts NEW  every key described, every row accepted, the example covered
    graph/compatibility.test.ts NEW   one verdict per fact combination; reasons name structures
    graph/updateRequest.test.ts NEW   a change and its evidence for every reason; the pinned strings
    graph/profile.test.ts       NEW   parse, merge, refusal, diagnostics
    graph/conventions.test.ts   NEW   ordered rules, precedence, built-in equivalence
    graph/files.test.ts         NEW   include and exclude against this repository's baseline files
    graph/validation.test.ts    NEW   every mismatch kind, an omission, an invented link, a first-attempt gate
    extension.test.ts           EDIT  contributed commands, profile read and fallback, the verdict gate,
                                      the offer's two answers, nothing written before the answer
    support/vscode.ts           EDIT  file-system watcher seam

specs/023-extension-repo-profile/
  expected/mtm-waitlist.json            NEW  the record for this repository
  expected/mtm-wip.json                 NEW
  expected/winui3-learning-lab.json     NEW
  expected/mtm-receiving-application.json NEW
  results/mtm-waitlist.json             NEW  profile, measurements, mismatches, verdict
  results/mtm-wip.json                  NEW
  results/winui3-learning-lab.json      NEW
  results/mtm-receiving-application.json NEW
  results/<slug>.attempts.json          NEW  one entry per check of the assistant's reply
  validation.md                         NEW  the four-of-four rollup and the determinism note
  contracts/profile.schema.json         the profile format a consumer reads
  contracts/compatibility.schema.json   NEW  the verdict the command and the validator both produce
  contracts/update-request.md           NEW  the request file's path, name and sections
  contracts/expected-record.schema.json NEW  the record format the comparison reads
  contracts/validation-result.schema.json NEW  the result format a reviewer re-checks
  contracts/validate.cli.md             NEW  the six modes, their arguments and exit codes
```

**Structure Decision.** The profile work splits along the boundary the extension already keeps: pure,
`vscode`-free logic under `src/graph`, and every read, write, watch and command in `src/extension.ts`
with the panel forwarding what the webview asks for. The validation work follows the same boundary: the
comparison and measurement are pure modules under `src/graph`, the walk, the disk reads, the child
process and the printing are in `src/validate.ts`, and nothing in either imports `vscode`.

The second amendment keeps that split and adds one rule to it. Anything two callers must agree on lives
where neither caller owns it: the profile contract is a table in `src/graph`, because the parser and the
prompt have to agree; the verdict is a function in `src/graph`, because the command and the validator
have to agree; and the one piece of IO both need — the path walk and the request write — is
`src/structure.ts`, outside `src/graph`, so `src/graph` stays pure. `extension.ts` and `validate.ts` each
call the same two functions and differ only in what they do with the answer. The records, results and
attempts live with the spec rather than in the checkouts, because the three external checkouts may gain
one profile file each and nothing else.

## Call paths

```calls Analysing a workspace with a profile
  activate() @ extensions/csharp-xaml-graph/src/extension.ts:276
    buildModel() @ extensions/csharp-xaml-graph/src/extension.ts:201
+     loadProfile() @ extensions/csharp-xaml-graph/src/extension.ts:128
~     collectSources() @ extensions/csharp-xaml-graph/src/extension.ts:39
      analyzeWorkspace() @ extensions/csharp-xaml-graph/src/graph/analyze.ts:287
~       classifyType() @ extensions/csharp-xaml-graph/src/graph/layers.ts:14
        evaluateLayerRules() @ extensions/csharp-xaml-graph/src/graph/conventions.ts:84
        computeFindings() @ extensions/csharp-xaml-graph/src/graph/findings.ts:290
~         generatedMembers() @ extensions/csharp-xaml-graph/src/graph/findings.ts:122
```
note: `collectSources` keeps the last word on file selection, so an explicitly configured pattern wins over the profile.

```calls Validating a checkout headlessly
+ validateCheckout() **new** @ extensions/csharp-xaml-graph/src/validate.ts
+   collectForProfile() **new** @ extensions/csharp-xaml-graph/src/graph/files.ts
    parseProfile() @ extensions/csharp-xaml-graph/src/graph/profile.ts:613
+   compileConventions() @ extensions/csharp-xaml-graph/src/graph/conventions.ts:98
    analyzeWorkspace() @ extensions/csharp-xaml-graph/src/graph/analyze.ts:287
+   compareWithRecord() **new** @ extensions/csharp-xaml-graph/src/graph/validation.ts
+   recordAttempt() **new** @ extensions/csharp-xaml-graph/src/graph/validation.ts
+   firstAttemptClean() **new** @ extensions/csharp-xaml-graph/src/graph/validation.ts
+   runInvariants() **new** @ extensions/csharp-xaml-graph/src/validate.ts
      main() @ extensions/csharp-xaml-graph/src/selftest.ts:117
+   writeResult() **new** @ extensions/csharp-xaml-graph/src/validate.ts
```
note: the invariants are the self-test's own generic checks, run as a child process, and `firstAttemptClean` judges the import condition from the attempts file rather than from the profile that survived the checks.

```calls Deciding whether a repository can be profiled at all
  openSetupChat() @ extensions/csharp-xaml-graph/src/extension.ts:447
+   assessFolder() @ extensions/csharp-xaml-graph/src/extension.ts:128
+     gatherStructure() **new** @ extensions/csharp-xaml-graph/src/structure.ts
+     assessCompatibility() **new** @ extensions/csharp-xaml-graph/src/graph/compatibility.ts
      buildSetupPrompt() @ extensions/csharp-xaml-graph/src/graph/profilePrompt.ts:87
+   offerNewSpecification() @ extensions/csharp-xaml-graph/src/extension.ts:475
+     buildUpdateRequest() **new** @ extensions/csharp-xaml-graph/src/graph/compatibility.ts
+     writeUpdateRequest() **new** @ extensions/csharp-xaml-graph/src/structure.ts
```
note: on `incompatible` the prompt is never built, so no convention question can be asked before the verdict; the offer is the only thing on that path that can write, and it writes only after yes.

The toolbar control is three small edits rather than a path of its own: a button added after `export`
in the actions row posts a `profile` message, and the panel forwards it to
`csharpXamlGraph.profile.refresh`, exactly as the re-analyse button reaches its command.

## Code pins

```ts extensions/csharp-xaml-graph/src/graph/conventions.ts:84-90
export function evaluateLayerRules(
  rules: LayerRule[],
  fallback: ProfileLayer,
  subject: LayerSubject,
): ProfileLayer {
  return rules.find((rule) => ruleMatches(rule, subject))?.layer ?? fallback;
}
```
pin 89: the first rule that matches decides, which is how a profile's precedence is written, and the fallback is what a type no rule matches gets.

```ts extensions/csharp-xaml-graph/src/graph/analyze.ts:575-578
      if (call.method === conventions.dependencyPropertyCall && call.typeOfArgs.length >= 3) {
        const valueType = index.resolve(call.typeOfArgs[1], usings, decl.namespace);
        const ownerType = index.resolve(call.typeOfArgs[2], usings, decl.namespace);
        if (valueType && ownerType && isViewModelType(conventions, valueType)) {
```
pin 578: the dependency-property edge is gated on the profile's view-model test rather than a `ViewModel` suffix, so a repository that names its view models differently keeps that edge.

```ts sketch src/extension.ts
const available = await vscode.commands.getCommands(true);
if (!available.includes('workbench.action.chat.open')) {
  await vscode.env.clipboard.writeText(prompt);
  void vscode.window.showInformationMessage(
    'Copilot Chat is not available here. The setup prompt is on your clipboard; paste it into any assistant, then run Import Profile Reply.');
  return;
}
await vscode.commands.executeCommand('workbench.action.chat.open', { query: prompt, isPartialQuery: true });
```
pin 2: the command id is verified before use because it is an internal workbench command, not a published API.
pin 8: a partial query fills the chat input without sending it, so nothing leaves the machine until the developer presses Enter.

```ts sketch extensions/csharp-xaml-graph/src/graph/validation.ts hl=2,10
export interface Mismatch {
  item: 'type' | 'link' | 'finding';
  name: string;
  expected: string;
  actual: string | null;
}

export function compareWithRecord(model: GraphModel, record: ExpectedRecord): ValidationReport {
  const mismatches = [...byType(model, record), ...byLink(model, record), ...byFinding(model, record)];
  return { mismatches, pass: mismatches.length === 0, measurements: measure(model) };
}
```
pin 2: every mismatch names the type and what was expected of it, and the list is never collapsed into a count (FR-020).
pin 10: one item turns the verdict to failing, so a checkout is never recorded as a partial success (FR-022).

```ts sketch src/graph/profileContract.ts hl=3,5
export interface ProfileField {
  path: string; key: string; section: SectionName;
  shape: string; required: boolean; meaning: string; absent: string;
  values?: string[]; fields?: ProfileField[];
}
```
pin 3: `section` is what makes one table serve six call sites instead of six lists that can drift apart.
pin 5: `fields` reaches every depth, so `layers.rules[].namespaceSegments` is described by the same row mechanism as `version`.

```ts sketch src/graph/profileContract.ts hl=2,5
export function contractKeys(section: SectionName): string[] {
  return PROFILE_CONTRACT.filter((field) => field.section === section).map((field) => field.key);
}

export function renderContract(): string { ... }
```
pin 2: the parser's accepted set and the prompt's described set are one list, which is the whole of FR-024.
pin 5: rendered before the questions, so the assistant knows what its answers are being shaped into (FR-023).

```ts pin extensions/csharp-xaml-graph/src/graph/profile.ts:660
  reportUnknownKeys(raw, contractKeys('root'), '', problems);
```
pin: the five section calls and this root call take their lists from the contract, so a key the prompt never describes cannot be accepted by accident.

```ts sketch src/graph/compatibility.ts hl=3,8
export function assessCompatibility(facts: StructureFacts, cap: number): CompatibilityVerdict {
  const reasons: IncompatibilityReason[] = [];
  if (facts.csharpFiles === 0) { reasons.push(noCsharp(facts)); }
  if (facts.xamlFiles === 0) { reasons.push(noXaml(facts)); }
  if (facts.csharpFiles + facts.xamlFiles >= cap) { reasons.push(overCap(facts, cap)); }
  for (const path of unsupportedProjects(facts)) { reasons.push(unsupportedProject(path)); }
  return { ...verdictFor(reasons, facts), limits: { analysisFileCap: cap } };
}
```
pin 3: every failed observation becomes its own reason, so the person is told all of what is wrong rather than the first thing found.
pin 8: the cap is a parameter and the caller passes the constant the analysis stops at, so the two cannot disagree; a verdict reads and writes nothing.

```ts sketch src/graph/compatibility.ts hl=1,8
const CHANGES_FOR_REASON: Record<ReasonCode, RequiredChange[]> = {
  'no-csharp': [{ title: ..., change: ..., }],
  'no-xaml': [...],
  'over-analysis-cap': [...],
  'unsupported-project': [...],
};

export function buildUpdateRequest(verdict: CompatibilityVerdict): string { ... }
```
pin 1: one table maps every refusal to at least one change, so a test can assert that no reason is a dead end and no change lacks evidence.
pin 8: the text is built from the verdict alone, so the file the person reads and the sentence the dialog showed cannot say different things.

## States

```states A workspace folder's profile
Absent: No profile file, so the built-in rules apply and the offer appears once. (start)
Partial: Some sections are declared and the rest fall back to the built-in rules. shows graph
Malformed: Part of the file cannot be read; the rest applies and the problems are named. shows graph
Valid: Every section the reader declared applies. shows graph
Stale: The code gained a convention the profile does not cover. shows graph
Absent -> Partial: import a reply that answers some questions
Absent -> Malformed: hand-write a profile with a mistake (proposed)
Partial -> Valid: import the remaining answers
Malformed -> Valid: correct the file, or import a corrected reply
Valid -> Stale: the repository gains a convention
Stale -> Valid: refresh then import
grid:
Absent | Partial | Valid
.      | Stale   | .
Malformed | . | .
```
note: no state stops the graph from opening; each one only changes which conventions apply and what is reported.

```states A repository's compatibility
Unchecked: No structure has been read yet. (start)
Compatible: The analysis can read this repository, so the compatibility question is answered and profile work continues above. (final)
Unknown: The structure could not be read, so profile work continues above with one note saying so. (final)
Incompatible: A named structure cannot be handled, and the reason is shown.
Requested: The person answered yes and the request file is on disk. (final)
Declined: The person answered no or closed the dialog, and nothing was written. (final)
Unchecked -> Compatible: no reason found
Unchecked -> Unknown: the walk could not list the folder, as in a remote window
Unchecked -> Incompatible: no C#, no XAML, above the cap, or a .fsproj, .vbproj or .vcxproj
Incompatible -> Requested: Start a new specification
Incompatible -> Declined: Not now, or the dialog dismissed
grid:
Unchecked | Compatible | Unknown | Incompatible
Requested | Declined  | .       | .
```
note: the offer is asked once per folder, so `Requested` and `Declined` are both final for the session, and the reason stays on the model in either case.

## Screens

```screen graph Graph toolbar and Problems pane
title: C#/XAML Dependency Graph
row:
  field: Module
  field: Layout
  button: Re-analyse
  button: Export JSON
  button: Update profile (1)
text: Profile problems and compatibility notes listed above the findings (2)
list: Details | Problems
```
1: **Where it sits.** Last in the actions row, after Export JSON, so both existing buttons keep their place and the new control reads as an addition.
2: **What it says.** One line per note, naming the part of the profile that could not be read or the structure the analysis cannot handle, and why. The heading becomes `This repository (N notes)` because the block now carries both kinds, and the tab badge keeps counting findings only, so nothing already asserted changes.

## Constitution Check

| Principle | Verdict | Basis |
|---|---|---|
| I. Spec-First, Verified Delivery | PASS | The spec, this plan and the task list precede the code, and tasks close only on a green typecheck, test run and selftest. The validation half closes on a recorded verdict per checkout, not on a claim. |
| II. Live Data Integrity | PASS | Nothing here reads or writes the application's stores. The validator opens a checkout read-only and writes only under the spec, and the one file this change writes into a repository is a request file inside the extension's own folder, after an explicit yes. Nothing in the analysed repository is ever created or changed. |
| III. Stored-Procedure-First | PASS | No database work at all. The extension is a local analysis tool and gains no data access. |
| IV. MCP-First, Grounded Decisions | PASS | Every claim about the VS Code surface and about the headless seam is grounded in a file and line cited in `research.md`, including the deviation from the Context7 and Microsoft Learn default (the chat command id exists only in `microsoft/vscode`). |
| V. WinUI 3 Platform Conformance | PASS | No XAML, no WinUI namespace and no navigation is touched; the graph is a webview, and the checkouts are read as text. |
| VI. Evidence-Based Verification Gates | PASS | The gates are `npm run typecheck`, `npm test`, `npm run selftest`, and a validator exit code per checkout with its mismatch list. A checkout at less than every condition is recorded as failing. |
| VII. Readable, End-User-Facing Delivery | PASS | Every new message is a plain sentence, and the verdict states what cannot be handled in the reader's words rather than a code. The profile contract ships as prose in the prompt and as the worked example in the README, the request file reads as a document a person can act on, and `validation.md` reads as prose a person can follow. |
| Security & Secrets | PASS | No credential, token or machine-specific path is written into a profile, a record, a result, a verdict or a request. The setup prompt carries type and namespace names only; the verdict and the request carry counts and repository-relative paths only. |
| External Integration & Cache Boundaries | PASS | The extension calls no inference endpoint. The verdict is a local path walk, the validator spawns the local self-test, and nothing leaves the machine. |
| Change-level rules | PASS | The profile is plain text reviewed like code, no dependency is added to read or check it, behaviour with no profile is unchanged, and every artifact is portable: the four records, the results, the verdicts and the requests carry no machine path. |
| Documentation & Extensibility | PASS | README, CHANGELOG and the panel tab's command table are updated in the same change, the README example is the documented way to extend a profile by hand, `validation.md` is the documented way to re-check a claim, and `contracts/update-request.md` is the documented way a request is written and read. |

No violations, so there is no Complexity Tracking table.

Re-checked against the final design once `data-model.md`, the profile schema and the record schema were
on disk: the conventions module and the validation module import nothing from `vscode`, the profile
schema admits no credential or machine-specific path, the result schema names no absolute path, and
the two new analysis modules stay pure.

Re-checked again once the second amendment's artifacts were on disk. `profileContract.ts` and
`compatibility.ts` sit under `src/graph` and import nothing from `vscode` and nothing from `node:fs`;
the walk and the request write are in `src/structure.ts`, outside that boundary, and both callers use
them rather than growing their own. `contracts/compatibility.schema.json` admits no absolute path and
requires evidence on every reason. The one write this amendment adds — the request file — happens in
the extension's own repository and only after the person says yes, which keeps principle II intact: the
repository being analysed is read and never changed. No verdict changed.

## How one checkout is proven

0. **Issue its verdict.** `node out/validate.js <checkout> --compatibility` walks the checkout and prints
   `compatibility: compatible` with the facts first, then exits 0. A nonzero exit here stops the run and
   is itself the evidence for the incompatible case; a compatible checkout writes nothing at this step.
1. **Write its record.** Read the checkout's declarations and markup and list every type in scope with
   the layer its own conventions give it, every view-to-view-model pair it states, and the findings
   known true of it. Commit it under `expected/` before any run against that checkout.
2. **Print its setup prompt.** `node out/validate.js <checkout> --prompt` analyses the checkout with no
   profile and prints the same prompt the chat command would, contract first. The printed text is kept
   with the run, because SC-012 is measured on it: the verdict and the contract both precede the first
   question.
3. **Have a fresh-context assistant write the profile.** It receives that prompt and the checkout path,
   and may write one file: `<checkout>/csharp-xaml-graph.profile.json`. Nothing else about the format is
   given to it, which is the point of FR-023.
4. **Check the reply before it counts.** `--check <reply> --record-attempt` parses it with the import
   command's parser, prints one line per problem, and appends the attempt to
   `results/<slug>.attempts.json`. Exit 0 means the extension would accept it. A first reply with a
   problem is recorded rather than quietly retried, because SC-011 is about the first reply.
5. **Measure it.** `--record expected/<slug>.json --result results/<slug>.json` analyses with the
   profile's conventions and file selection, compares item by item, runs the self-test's invariants
   against the checkout, passes the `import` condition only when the attempts file holds one clean
   attempt, and writes one verdict.
6. **Prove it repeats.** Run step 5 again from the same profile into a second result file, then
   `--compare` the two. The comparison covers measurements and mismatches, not the profile's hash.

A checkout passes only when all five conditions hold: every expected type sits in the layer its
conventions give it, with no unexpected type; every view-to-view-model link is found with none missing
and none invented; no finding comes from a convention mismatch and none known true is missed; the
profile imports with zero problems on the first attempt; and the invariants pass. Any failing condition
names each item.

The incompatible case is proven separately, with a repository the analysis cannot read: `--compatibility`
exits 3 and names the structure, and the folder is untouched afterwards; `--request` then writes
`update_prompts/<slug>/request.md` whose every change carries evidence and whose second line states
`/speckit-companion-specify`; and running `--compatibility` again without `--request` leaves the file
absent when it was absent before, which is the no answer.

## Phase 0 — Research

`research.md` holds twenty-one decisions with their rationale and the alternatives rejected, in three
rounds. Round one's load-bearing ones for the profile itself: the chat mechanism and its fallback, the
profile's location and format, the ordered rule list that keeps one classifier, the two directions of
merge, and where the prompt's evidence comes from. Round two's for the proof: where the headless run
lives (R10), the selection it applies and the baseline that pins it (R11), the record's shape and home
(R12), the fresh-context procedure and the determinism comparison (R13), what a result records and omits
(R14), and how the fifth condition is measured (R15). Round three's for the amendment: where the profile
contract is authored so it cannot drift from the parser (R17), what "compatible" means and where the
verdict is computed (R18), how the offer is asked and how the reason survives the dialog (R19), what the
request file contains and how it is named (R20), and how a run proves the first reply imported clean
(R21).

## Phase 1 — Design and contracts

`data-model.md` describes the profile, its sections and the compiled conventions they produce, with the
validation rules drawn from the requirements and the lifecycle above. It now also describes the profile
contract as a table, the structure facts and the compatibility verdict, the update request, the expected
answer record, the measurement, the mismatch, the recorded result and the attempts record.

`contracts/profile.schema.json` is the profile format as a consumer sees it: the chat assistant is told
to answer in it, the import validates against it, and a hand-written profile is checked by it. The spec
names no API, but the proof and the amendment each have a format a consumer codes against, so they are
written down the same way. `contracts/compatibility.schema.json` is the verdict both the command and the
headless validator produce, with evidence required on every reason and no absolute path admitted.
`contracts/update-request.md` is the request file: where it goes, how the repository name becomes a
folder, the order of its sections, and the two pinned strings it must contain. Both live beside
`contracts/expected-record.schema.json`, which is what a record author writes and the comparison reads,
and `contracts/validation-result.schema.json`, which is what a reviewer re-checks a claim against.
`contracts/validate.cli.md` is the command surface, its six invocations and their exit codes, including
the two that write nothing.

The profile contract itself is not a new file: it is the table in `profileContract.ts` rendered into the
prompts, and `profile.schema.json` is the same format written for a reader. A test keeps the two in
agreement rather than a third document restating both.

Identifiers in all of these files are the ones used throughout this plan, unchanged, and the two pinned
strings — `extensions/csharp-xaml-graph/update_prompts/{requestingRepo}/` and
`/speckit-companion-specify` — appear exactly as the spec writes them.

## Verification the plan commits to

The previous build's node, edge and finding counts on this repository are recorded before the change,
and the same three counts are reproduced with no profile present. The suite continues to gate command
registration, toolbar markup and settings behaviour, and `selftest` gains the profile control and a
round trip in which a hand-written profile changes one classification and nothing else.

For FR-017 to FR-022 the commitments are these five.

- Each of the four checkouts has a record committed before its first run, and a result whose verdict is
  either 100% or failing with every mismatched item named.
- The validator run on this repository, with its profile, reports 1095 nodes, 4776 edges and 21
  findings, the same three numbers in `baseline.md`, so the profile added here corrects nothing the
  extension already reported (SC-010).
- A repeat run from the recorded profile produces the same measurements and the same mismatch list
  (SC-009), checked by `--compare` rather than by eye.
- Removing one type from a record on purpose makes the comparison fail and name that type, which is how
  the record proves it is load-bearing rather than decorative (verification plan step 10).
- A checkout that is no longer present produces a nonzero exit and no result file, so a moved checkout
  is reported as not run rather than as a pass.

For FR-023 to FR-028 the commitments are these six.

- Every key the parser accepts appears in `PROFILE_CONTRACT`, and `contractKeys` is the only list the
  parser reads, so a key the prompt does not describe cannot be accepted (FR-024). The test proves it
  three ways: every row parses with no problem at its path, every key of the reply example is a row, and
  every property of `profile.schema.json` at any depth is a row.
- Both prompts render the whole contract before their first question, and the test asserts the position
  rather than the presence, so "before the questions" is measured and not asserted in prose (FR-023).
- A fresh assistant is given the prompt and the checkout and nothing else, and its *first* reply's check
  is recorded in `results/<slug>.attempts.json`; `--record` passes the `import` condition only when that
  file holds one clean attempt, which is the measurement SC-011 asks for (FR-023, SC-011).
- The verdict runs before any prompt is built, in the command and in `--prompt`, so no convention
  question exists on an incompatible path; the printed run is kept with the checkout's evidence, which is
  how SC-012 is measured rather than claimed (FR-025, SC-012).
- An incompatible checkout is named by structure, exits 3, and changes nothing; the offer is asked once
  per folder, a dismissed dialog counts as no, and the reason is on `model.warnings` afterwards, which is
  what makes it outlive the dialog (FR-026, FR-028).
- Answering yes writes `update_prompts/<slug>/request.md` under the extension's own root, with one
  section per reason and a change and its evidence under each, and the second line stating
  `/speckit-companion-specify`; a test asserts both pinned strings and the evidence on every change, and
  a folder snapshot asserts that nothing else was created (FR-027, SC-013).
