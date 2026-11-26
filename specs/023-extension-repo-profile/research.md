# Phase 0 Research — Repository Profiles for the C#/XAML Dependency Graph

Three rounds of research are recorded here. R1 to R9 settled how a profile is read, created and
refreshed, and they describe code that is now built and green. R10 to R16 settle how the approach is
proven on four real checkouts, with no VS Code window open on any of them and no extension source edit
for each one. R17 to R21 settle what the second amendment adds: the profile format stated in the
prompt rather than implied by an example, and a structural compatibility verdict issued before any
convention question.

Every decision below rests on a source read during a run of this work. Where the authoritative source
was the `microsoft/vscode` repository rather than a documentation page, it is named as such.

## What the extension looks like today

| Area | Fact | Source |
|---|---|---|
| Layer classification | One function runs the compiled conventions and falls back to the built-in default. The decision order it replaced is now rule data. | `extensions/csharp-xaml-graph/src/graph/layers.ts:14` |
| Link detection | Call names, the dependency-property call, the `Add*` pattern, service-locator names and data-context casts all come from `CompiledConventions`. | `src/graph/conventions.ts:98`, `src/graph/analyze.ts` |
| Findings rules | Entry points, transparent bases and generated members come from the conventions; the built-in profile keeps the previous constants. | `src/graph/findings.ts`, `src/graph/profile.ts:194` |
| File selection | `vscode.workspace.findFiles` with the profile's include and exclude, where a configured setting wins one list at a time. Nothing outside the host matches globs. | `src/extension.ts:39`, `src/extension.ts:60`; searched this run for a matcher |
| Profile reading | `parseProfile` and `compileConventions` are pure. The file read and the watcher are host code. | `src/graph/profile.ts:613`, `src/extension.ts:128` |
| Setup prompt | `buildSetupPrompt(evidence, folderName)` renders the five questions and the reply shape. `ProfileEvidence` is derived inside the analysis and rides on the model. | `src/graph/profilePrompt.ts:87`, `src/graph/analyze.ts:165`, `src/graph/analyze.ts:1005` |
| Reply payload | The first fenced block is taken from a reply, else the whole text. That helper is private to the host. | `src/extension.ts:182` |
| Headless analysis | `node out/selftest.js <root>` analyses any checkout, with its own directory skip list, then runs the generic invariants. It never loads a profile. | `src/selftest.ts:41`, `src/selftest.ts:117` |
| Conventions seam | `analyzeWorkspace` takes `conventions` from its caller, so a headless run can supply a profile's conventions exactly as the host does. | `src/graph/analyze.ts:287`, `src/graph/analyze.ts:34` |
| Invariant exit | The self-test prints `ALL CHECKS PASSED` and exits 0, or prints a failing count and exits 1. | `src/selftest.ts:1750`, `src/selftest.ts:1751` |
| Selection reproduced | Segment-level exclusion by the default list reproduces the baseline's 815 C# files and 93 XAML files on this repository. | measured this run; `baseline.md` |

Round one's decisions (R1 to R9) rest on the first seven rows. Round two's rest on the last four: a
pure analysis seam, a pure profile parser, a prompt builder that needs only evidence, and one existing
path-taking entry point.

## Round one — reading, creating and refreshing a profile

### R1 — How the extension reaches AI chat

**Decision.** Open the Chat view with a prepared prompt by executing the workbench command
`workbench.action.chat.open` with `{ query, isPartialQuery: true }`, guarded first by
`vscode.commands.getCommands()`. If the command id is not in that list, copy the prompt to the
clipboard instead and tell the developer what to do with it. If the guard passes but the execute call
rejects, do the same thing: one `try`/`catch` around the call, one fallback.

**Sources.**
- `microsoft/vscode`, `src/vs/workbench/contrib/chat/browser/actions/chatActions.ts`:
  `export const CHAT_OPEN_ACTION_ID = 'workbench.action.chat.open';` and
  `export interface IChatViewOpenOptions`, whose first documented field is the query, with
  `isPartialQuery?: boolean` alongside it. The same option is described in
  `.../actions/chatQuickInputActions.ts` as "Whether the query is partial; it will wait for more user
  input".
- VS Code API reference, `commands.getCommands(filterInternal?: boolean)`: "Retrieve the list of all
  available commands. Commands starting with an underscore are treated as internal commands." Declared
  at `@types/vscode@1.90.0` `index.d.ts:10303`, which is the version this extension compiles against.

**Rationale.** The spec needs a conversation the developer steers: questions asked before any answer
(FR-007), each question carrying evidence (FR-008), the developer accepting or correcting answers
(US2), and a second round that only adds to what exists (FR-016). `isPartialQuery: true` puts the
prompt in the input box without sending it, so nothing leaves the machine until the developer presses
Enter. That also keeps the repo's privacy rule intact: no extension code calls an inference endpoint.

**Why not the Language Model API.** `vscode.lm.selectChatModels` is present in `@types/vscode@1.90.0`
(`index.d.ts:19228`), so availability is not the objection. The objection is the shape: it is a single
request and response with no conversation the developer can steer, it requires the extension to render
its own exchange, and the API documentation states plainly that the model list can be empty and
extensions must handle that. It would also make the extension the thing calling a model, which the
constitution's privacy constraint does not have to be tested against if the developer's own chat client
does it.

**Fallback.** Clipboard plus an information message, with `csharpXamlGraph.profile.import` still
available and the profile format documented in the README so a profile can be hand-written without any
assistant at all. This is the "no assistant is available" edge case, and the failure mode is a message
and a clipboard write, never an error.

**Known limitation, recorded rather than hidden.** `workbench.action.chat.open` is registered with
`f1: false` and is not listed on the public Built-in Commands page, so its presence depends on the chat
contribution being installed. The guard is what makes that safe. If the command is absent in a given
build, initialization degrades to the clipboard path and the graph still opens.

### R2 — How the assistant's reply reaches the extension

**Decision.** `csharpXamlGraph.profile.import` reads the clipboard with `vscode.env.clipboard.readText`,
takes the first fenced code block if the text contains one, otherwise the text itself, parses it as the
profile, merges it into the existing file, writes the file, opens it, and re-reads the repository.

**Rationale.** The clipboard is the transport every VS Code host has, needs no new dependency, and is
already proven in this extension (the panel's `copy` message writes it, `panel.ts:222`). Reading the
first fenced block matters because assistants answer in prose with the payload fenced; taking the whole
reply would fail on the prose every time.

**Alternatives considered.** A single-line input box cannot carry a profile. A save-dialog JSON file
adds a step and still needs a paste path for the common case. Letting the agent write the file directly
would bypass FR-010, which requires the extension to refuse an unusable reply and name what was
missing.

### R3 — Where the profile lives and what it is written in

**Decision.** `<workspace folder>/csharp-xaml-graph.profile.json`, strict JSON, a `version` field
carrying `1`.

**Rationale.** The name states its purpose, it sits at the workspace root where it is found and
committed, and `JSON.parse` reads it with no new dependency, which the spec's change-level rule
requires ("Reading and reviewing a profile must not require a new committed dependency"). A changed
profile is a line-by-line diff.

**Alternatives considered.** YAML is friendlier to comments but needs a parser dependency. JSONC needs
a comment stripper or a linter. A `.vscode/` location puts the repository's conventions inside the
editor's own settings folder, where VS Code tooling already writes and users expect machine-local
state.

### R4 — How a profile reaches the classifier without two behaviours

**Decision.** Express the layers rules as an ordered list of rules where the first match wins, and
make the built-in constants the default list. `classifyType` keeps its name and its signature and runs
the same evaluator either way.

**Rationale.** FR-002 asks for name suffixes, prefixes, namespace segments *and* the precedence
between them; "ordered, first match wins" is precedence in its plainest form. The current function's
decision order becomes seven rules in the same order, so the unprofiled path is the same decisions
expressed as data rather than a second implementation.

**Alternatives considered.** Keeping today's `classifyType` untouched and branching to a profiled
version only when a profile exists would leave two implementations of one decision, and their drift
would be invisible until a repository reported a wrong classification.

**Risk and its guard.** This is the highest-risk change in the feature, because SC-003 requires
identical output with no profile. The guard is the existing `test/graph/layers.test.ts` suite, the
finding and analysis suites that depend on layers, and `npm run selftest`, which analyses this
repository and asserts known node, edge and finding facts.

### R5 — Merge direction, in two directions

**Decision.** Profile over built-in defaults: an absent key keeps the built-in value, a present array
replaces the default array wholesale. Refresh over an existing profile: arrays are unioned and
de-duplicated, existing scalar values win, and an assistant proposal that differs from a declared
scalar is reported as a notice rather than silently applied.

**Rationale.** The spec states both rules and they point opposite ways, so they must be separated by
direction rather than by code path. Unioning on refresh is what makes FR-016 hold even when the
assistant returns a partial profile: the file cannot lose a declaration because the merge starts from
the file.

### R6 — Where the evidence for the prompt comes from

**Decision.** `analyzeWorkspace` already builds the merged declarations and the XAML files. A new pure
module derives a `ProfileEvidence` summary from them: name suffixes and prefixes seen on types, distinct
namespace segments, call method names with their type-argument counts, declared base types, attributes
that generate members, XAML root tags, and a couple of example type names per signal. It is attached to
the model as an optional `profileEvidence` field.

**Rationale.** Re-reading files to build the prompt would parse the workspace twice, and scraping the
public model would lose call names, which are exactly the evidence FR-008 wants for the registration
question. The field is optional and is not exported with the graph JSON.

### R7 — How a broken or partial profile is reported

**Decision.** Problems become entries in the model's existing `warnings` array, which the webview
already carries and the export already writes. They are rendered at the top of the Problems pane. The
tab badge keeps counting findings only.

**Rationale.** The pane is where a reader already looks for what went wrong, and leaving the badge on
findings keeps every existing assertion about it true. A malformed profile therefore costs a warning,
exactly as the spec's fourth story asks.

### R8 — Re-reading after a profile changes

**Decision.** A `FileSystemWatcher` on the profile path clears the cached model and tells the open
panel to re-read. The existing configuration-change invalidation stays as it is.

**Rationale.** The spec allows "is told a re-read is needed, or the graph re-reads"; re-reading is the
answer that keeps the visible graph honest, and it is what FR-012 and SC-005 ask for after an import.
A watcher also covers a profile edited by hand in the same window, which a configuration listener
cannot see.

**Test-double work.** `test/support/vscode.ts` has `workspace.fs`, `commands.getCommands`,
`env.clipboard`, `withProgress` and `configuration().inspect`, but no `createFileSystemWatcher`. The
double gains that seam, with a fire helper, in the same style as `fireConfigurationChange`.

### R9 — Generated members as data

**Decision.** Findings rules describe generated members as attribute, source kind and naming pattern.
The defaults reproduce today's two rules exactly: `[RelayCommand]` on a method yields `FooCommand` and
`FooCommand` from `FooAsync`, and `[ObservableProperty]` on `_foo` yields `Foo`.

**Rationale.** FR-004 requires generated members to be declarable. Anything less would leave the
largest source of false "dead binding" findings outside the profile.

## Round two — proving the approach on four checkouts

### R10 — Where the headless analysis lives

**Decision.** A new entry point, `src/validate.ts`, built to `out/validate.js`, with its pure half in
`src/graph/validation.ts`. It takes a checkout path and reads that checkout's profile from disk. The
build gains one esbuild entry and `package.json` gains one script.

**Sources.** `scripts/build.mjs` builds three entries today (`extension.ts`, `selftest.ts`, the
webview client), each as its own `entryPoints`/`outfile` pair, so a fourth follows the existing shape.
Nothing under `src/graph` imports `vscode`: the only Node builtin there is `node:module` in the C#
parser, read this run. `src/graph/analyze.ts:287` takes its conventions from the caller.

**Rationale.** FR-018 asks for analysis of a checkout by location, not as the open workspace. The three
existing entries cannot serve: `extension.ts` is the editor host, `selftest.ts` is the invariant runner
and never loads a profile, and the webview client is a browser bundle. A fourth entry keeps one job per
entry, gives the validator an exit code and stdout, and leaves the extension bundle untouched, which is
what keeps this repository's analysis output identical.

**Alternatives considered.** Extending the self-test with profile loading and record comparison was
rejected: it would put two jobs in a 79 KB script, its generic invariants would then depend on the mode,
and the bar's fifth condition needs the self-test to stay exactly what it is. A vitest test was
rejected because the check must run against a path outside this workspace, with no editor running, and
print a verdict a person can read in a terminal.

**What stays pure.** Record types, measurement and comparison live in `src/graph/validation.ts` with no
`node:fs` import, so the item-by-item verdict is unit-tested the way the rest of `src/graph` is.
`src/validate.ts` does the IO: walk, read, spawn, write, print.

### R11 — Which files the headless run reads

**Decision.** Apply the profile's `files.include` and `files.exclude` with a small matcher in
`src/graph/files.ts`, pruning a directory during the walk when its relative path matches an exclude
pattern. Supported glob syntax is `**`, `*` and `?`. A pattern outside that set is reported as a
problem rather than silently mismatched.

**Sources.** `baseline.md` records 815 C# files and 93 XAML files for this repository. Measured this
run: excluding directories named `bin`, `obj`, `node_modules`, `.vs`, `TestResults`, `out`, `.specify`,
`.github`, `.playwright-mcp`, or ending in `.Tests`, the same tree yields exactly 815 and 93. The
patterns in `profile.schema.json` and the shipped default exclusion use only `**` and `*`.

**Rationale.** The record's scope has to be the profile's selection, or a profile that drops half a
repository could pass by shrinking what is judged. Applying the profile's own section also proves the
section works, on four repositories, which no other check does.

**Alternatives considered.** Reusing the self-test's hardcoded skip list was rejected: the scope would
be defined by a list in a script rather than by the profile under test, and SC-010's comparison would
use a different selection than the profile declares. Skipping file selection entirely was rejected for
the same reason.

**Divergence recorded, not hidden.** The host keeps `vscode.workspace.findFiles`, which understands
more glob syntax and the editor's own rules. The validator's matcher is a second implementation of a
narrower grammar, bounded to the shapes the schema documents, and a pattern it cannot express is
reported instead of quietly matched the wrong way.

### R12 — The expected-answer record: shape and home

**Decision.** One JSON file per checkout at
`specs/023-extension-repo-profile/expected/<slug>.json`, committed with the spec. It names the
repository, states in prose how it was read, and then lists, exhaustively:

- every declared type in scope, by the identifier the graph uses (`Namespace.Type`), with its file and
  its expected layer;
- every view-to-view-model pair the checkout states, by those same identifiers;
- every finding true of the checkout, by kind and the types it names.

It is written by reading the checkout's code and committed **before** the first validator run, so the
commit order is the evidence that it was not derived from the output it judges.

**Rationale.** FR-017 puts the record in the specification, and the three external checkouts may gain
nothing but a profile file, so the record cannot live in them. Exhaustiveness is what the edge case
demands: a type in the run and absent from the record is an unexpected type and therefore a mismatch,
so an omission cannot be scored as accuracy. Listing a file beside each type makes the record
reviewable against the code it was read from. Using the model's full name means a mismatch can name
the type with no alias table.

**Alternatives considered.** A sample of representative types was rejected: it cannot detect a profile
that classifies the other nine tenths wrongly, and the edge case rules it out. Storing the record in
the checkout was rejected: one profile file is the only permitted addition. Deriving the record from an
export of the run was rejected outright by the spec's assumption that a record derived from the run it
judges proves nothing.

**Cost accepted.** The record is a large committed file for the bigger checkouts. It is data, not code:
produced by a mechanical pass over declarations, reviewed by sampling, and corrected when a mismatch
shows it was read wrongly. A corrected record is still evidence, as long as the correction comes from
reading the code and not from copying the run.

### R13 — How a fresh-context assistant produces a profile, headlessly

**Decision.** Three commands on the one entry point, and one rule for each.

1. `node out/validate.js <checkout> --prompt` analyses the checkout with no profile, derives
   `ProfileEvidence`, and prints the same text `buildSetupPrompt` gives the chat command.
2. The assistant is handed that text and the checkout path, and nothing else. It may write one file,
   `<checkout>/csharp-xaml-graph.profile.json`, and nothing else in the checkout.
3. `node out/validate.js <checkout> --check <reply>` extracts the fenced block with the helper the
   import command uses, runs `parseProfile`, prints one line per problem, and exits 0 only when the
   parsed profile records something. That is the "the extension accepts the profile" half of FR-019 and
   the headless form of FR-010.

**Sources.** `src/graph/profilePrompt.ts:87` builds the prompt from evidence alone, so it needs no
editor. `src/extension.ts:182` holds the fenced-block rule. `src/graph/profile.ts:613` returns the
profile and its problems from text with no IO.

**Rationale.** The spec requires a profile obtainable from the setup prompt plus the checkout by an
assistant with no prior context (FR-019), and the user requires that assistant to run in a fresh
context. A subagent in another session cannot see this repository or a chat panel, so the prompt has to
be text it can read and the check has to be a command it can run. Reusing the command's prompt builder
and parser keeps one definition of each; a second prompt for headless use would drift.

**Determinism, in two runs.** The recorded run writes a result file. The repeat run writes a second
result file from the same profile, and `--compare` compares the measurements and the mismatch list, not
the profile's hash, because SC-009 compares a run from a fresh assistant's profile with a run from the
recorded one, and two textually different profiles can express the same conventions.

### R14 — What a result records, and what it leaves out

**Decision.** `specs/023-extension-repo-profile/results/<slug>.json` holds the profile's SHA-256 and its
problem list, the measurements (node, edge and finding counts, per-layer, per-edge-kind and
per-finding-kind counts, the sorted type/layer pairs and the sorted link pairs), the five conditions
with their mismatch lists, and one verdict. No timestamp, no absolute path, no duration.

**Rationale.** FR-021 wants the profile, the measurements, the mismatches and the verdict, so the claim
can be re-checked without repeating the run. A timestamp would make two identical runs differ, the
opposite of what FR-019 asks to prove. An absolute checkout path is machine-specific and the
change-level rules bar one from committed artifacts; the slug identifies the checkout, and
`validation.md` names the machine path once as prose rather than as a field the comparison reads.

**Alternatives considered.** Recording only counts was rejected by FR-020, which forbids reducing the
outcome to a count; the mismatch list is the point.

### R15 — How the fifth condition is measured

**Decision.** The validator runs `node out/selftest.js <checkout>` as a child process and records the
exit code and summary line as the `invariants` condition, failing the run when it is nonzero.

**Sources.** `src/selftest.ts:41` takes the root from `process.argv[2]`; `src/selftest.ts:117` does the
analysis and the checks; `src/selftest.ts:1750` and `:1751` print the summary and set the exit code. Its
generic checks are structural, so they hold for any repository.

**Rationale.** FR-022 needs all five conditions in one verdict. Shelling out to the existing entry
reuses the real invariant suite, keeps `selftest.ts` unchanged, and cannot drift from what a person runs
by hand.

**Alternatives considered.** Extracting the generic checks into a shared module was rejected as a
refactor of a large script with no change to what is proven. Running the self-test by hand and joining
the result in prose was rejected: FR-022 asks for a recorded verdict, and a manual join is a partial
success waiting to happen.

**Cost accepted.** The checkout is parsed twice, once by the validator and once by the self-test. A
validation run happens a handful of times per release, so minutes are cheaper than a second
implementation of the invariants.

### R16 — What the validator analyses, and what it refuses to do

**Decision.** The validator calls `analyzeWorkspace` with `includeExternalTypes: false`, the profile's
compiled conventions and the files its selection yields. External nodes are excluded because the record
names declared types only. A run that cannot reach a verdict exits nonzero and writes no result.

**Rationale.** Excluding external types keeps the comparison in the same universe as the record: one
entry per declared type, with no framework nodes to enumerate. The spec's edge case about a moved
checkout says its recorded result stands and a fresh run is reported as not run, so a missing root must
not overwrite or invent a result.

**Facts verified this run.** The three checkouts exist at the paths the plan names; their `.cs` and
`.xaml` counts match the spec's 538/341, 1278/858 and 1522/284; two have a solution at the root and the
third does not, which no part of this mechanism reads.

## Round three — the contract stated, and compatibility settled first

### R17 — Where the profile contract is authored so it cannot drift from the parser

**Decision.** One table of fields, `PROFILE_CONTRACT`, in a new pure module
`src/graph/profileContract.ts`. Each entry names the key's path in the file, the key, its value shape,
whether it is required, what it means, and what the extension does when it is absent. The parser stops
carrying its own key lists: every `reportUnknownKeys` call takes `contractKeys(<section>)` from the
table instead of a literal array — `profile.ts:660` for the root, `:387` for `layers`, `:429` for
`links`, `:520` and `:561` for `findings`, `:592` for `files`. Both prompt builders render the same
table as a section placed before their first question
(`profilePrompt.ts:87` and `:147`), so create and refresh state one contract.

Three tests hold it, one per direction:

1. **The parser cannot accept what the contract does not describe.** Not a test — construction. The
   accepted set *is* the table, so there is no second list to fall behind.
2. **The contract describes nothing the parser refuses.** For every row, a minimal profile declaring
   that one key is parsed and must produce no problem at that path, so a row cannot describe a key the
   reader silently skips.
3. **The example cannot show a key the contract does not describe.** `REPLY_SHAPE`
   (`profilePrompt.ts:54`) is parsed and each key path it uses is looked up in the table. This is the
   failure the amendment names: today that example shows `links.addCallPattern`,
   `links.dependencyPropertyCall` and `findings.generatedMembers[].form` while the text says nothing
   about what they mean, that `form` admits exactly two values, or what happens when they are absent,
   so an assistant reading it can only guess.

A fourth test compares the table with `contracts/profile.schema.json` — every property the schema
names at any depth must be a row, and every row must be a schema property. It reads the schema from
`specs/023-extension-repo-profile/contracts/` and is skipped when that path is absent, so the
extension's own suite stays green in a checkout that does not carry the spec.

**Sources.** `src/graph/profile.ts:215` is the one function that decides a key is unrecognised, so it
is the one place the accepted set can be defined. `src/graph/profile.ts:613` is the only reader.
`src/graph/profilePrompt.ts:54` is the current example, and `:87`/`:147` are the two builders.

**Rationale.** FR-024 forbids leaving a key's name, case or nesting to be inferred from habit, and the
only way to guarantee that is for one description to be both what the parser reads and what the prompt
states. A rendered table also carries the two things an example cannot: value shapes (a closed set of
two is not visible in one example) and absent-key behaviour (an example shows only present keys).

**Alternatives considered.** Generating the prompt from the JSON Schema was rejected: the schema is a
spec artifact, not part of the extension package, so the extension would have to bundle a copy, and the
parser would still need its own reader — two descriptions again, with the schema a third. Writing the
contract as prose beside the prompt and asserting it by searching for key names was rejected: a search
proves a name is mentioned, not described, and it cannot notice a key the parser gains later unless
someone remembers to edit both.

### R18 — What "compatible" means, and where the verdict is computed

**Decision.** Four observations, taken from file names alone, and any one of them alone makes the
repository incompatible:

| Code | Observation | Evidence the reason carries |
|---|---|---|
| `no-csharp` | no file ends `.cs` | the XAML count, the project files found |
| `no-xaml` | no file ends `.xaml` | the C# count |
| `over-analysis-cap` | C# plus XAML files reach `ANALYSIS_FILE_CAP` | both counts and the cap |
| `unsupported-project` | a project file ends `.fsproj`, `.vbproj` or `.vcxproj` | up to three repository-relative paths |

Nothing else counts as a reason. A `.csproj` or `.sln` is recorded as a fact but never decides the
verdict: a folder of loose `.cs` files analyses correctly, so refusing it would refuse work the
extension can do. A project's `TargetFramework` is likewise not a reason: the analysis reads C# and
XAML as text and never consults the project model, so a console app and a WinUI app are read the same
way, and only the presence of XAML separates them.

`assessCompatibility(facts, limits)` lives in `src/graph/compatibility.ts` and is pure: no `node:fs`,
no `vscode`. `ANALYSIS_FILE_CAP` moves there from `extension.ts:24`, so the number that stops
`findFiles` and the number the verdict compares against are one constant. Facts are gathered by one
walk in `src/structure.ts` using `node:fs`, with the built-in include and exclude patterns from
`builtInProfile()` — the same selection the analysis would make with no profile — and that walk is
called by both `extension.ts` and `validate.ts`.

**Rationale.** FR-025 wants a verdict from structure before any convention is gathered, and all four
observations are answerable from paths, so the check costs one `readdir` walk and never parses a file.
One verdict function means the command and the headless validator cannot disagree; the only thing that
differs between them is who built the fact list.

**Alternatives considered.** Counting languages by parsing was rejected: it answers a question that
must be settled *before* an analysis with a full analysis. Asking the assistant was rejected: the
verdict has to exist before the prompt is built, so the prompt cannot be what produces it. Gathering
host facts with `vscode.workspace.findFiles` and validator facts with `node:fs` was rejected for the
same reason R11 rejected a second matcher: two glob grammars, two answers, one of them wrong and
neither obviously so.

**Limitation recorded rather than hidden.** In a remote window `fsPath` is not a local path, so the
walk yields nothing. The verdict is then `unknown` with one note saying the structure could not be
checked, and profile work proceeds exactly as it does today. Refusing to profile a repository because
the extension could not look at it would be worse than profiling it with no verdict.

### R19 — How the offer is asked, and how the reason survives the dialog

**Decision.** Two surfaces, one writer.

- **In the extension host**, the offer is one `showInformationMessage` with two buttons, `Start a new
  specification` and `Not now`, asked at most once per folder per session — a `Set` beside the
  existing `offeredProfile` flag at `extension.ts:487`. A dismissed dialog (`undefined`, which is what
  the test double's `messageResults` returns when nothing is queued, `support/vscode.ts:571-581`) is
  treated as `Not now`, because a person who closed the box did not say yes.
- **Unattended**, the offer is a flag rather than a dialog: `node out/validate.js <checkout>
  --compatibility` prints the verdict and its reasons and exits 3 when incompatible;
  `--request` is the yes answer and writes the file, and leaving it off is the no answer, which writes
  nothing. No path blocks waiting for input, which is what makes the same code usable from a script.

Both call the same `writeUpdateRequest`.

**Retention.** The verdict is recomputed on every analysis and pushed into `model.warnings`, one note
per reason, so it is on the model the panel already renders from (`client/main.ts:1960`) and therefore
survives the dialog, a dismissal and a Re-analyse for the rest of the session. The pane's block gains a
second recogniser beside `profileProblemNote` (`profile.ts:835`): a compatibility note is written as
`not analysed › <structure>: <sentence>`, which `profileProblems.ts:32` renders in the same list as a
profile problem, above the findings, and the block's heading becomes `This repository (N notes)`
because it now carries both.

**Rationale.** FR-028 says the reason must not disappear with the dialog. A warning on the model is
the one place in this extension that already outlives a dialog and is already shown to the reader; the
alternative is a new model field and a second renderer for the same sentence.

**Alternatives considered.** A new `compatibility` field on `GraphModel` plus a pane block was
rejected as a second channel for one message. A status-bar message was rejected: it expires, which is
the failure FR-028 names. Re-asking the offer on every analysis was rejected: it would interrupt a
person who already said no, and FR-014's once-per-session rule for the profile offer is the pattern
this extension already established.

### R20 — What the request file contains, and how it is named

**Decision.** `<requesting folder>/update_prompts/<slug>/request.md` under the extension's own root:
`context.extensionUri.fsPath` in the host, and `resolve(__dirname, '..')` from `out/validate.js` in the
validator. Both resolve to `extensions/csharp-xaml-graph` when the extension is developed in place, so
the path from this repository is exactly the pinned `extensions/csharp-xaml-graph/update_prompts/{requestingRepo}/`.
`<slug>` is the workspace folder name through the same slug rule the validation records use, so
`MTM_Waitlist` becomes `mtm-waitlist` and matches its record's name.

The body is generated from the verdict by a pure `buildUpdateRequest(verdict)` in
`src/graph/compatibility.ts`:

1. a heading naming the requesting repository;
2. the line stating the file is to be run with `/speckit-companion-specify`, verbatim, immediately
   under the heading so it cannot be missed;
3. one section per reason, in the order the verdict lists them, each beginning with the sentence the
   person was shown, then one subsection per required extension change, each carrying the evidence
   from the repository that justifies it — counts, and up to three repository-relative paths;
4. a closing line saying nothing in the requesting repository was created or changed.

The reasons-to-changes mapping is a table, `CHANGES_FOR_REASON`, so a test can assert that every
reason code produces at least one named change and that every change carries at least one evidence
line. A reason with no change would be a request that asks for nothing.

`writeUpdateRequest` in `src/structure.ts` creates the folder, writes the file and replaces an existing
one, so answering yes twice leaves one current request rather than a stack. When the write fails — an
installed copy whose folder is read-only, which is the normal case for a packaged extension — the text
goes to the clipboard and the message names the exact relative path, so the request is not lost and the
person is told where it belongs.

**Rationale.** FR-027 asks for every change with its evidence and for the stated command, and a fixed
template generated from the verdict is the only shape where "every change" can be checked by a test
rather than by reading four files. `request.md` inside the repository's own folder keeps the identity in
the folder name, which is what the pinned path encodes.

**Alternatives considered.** A single file per repository at the root of `update_prompts/` was rejected:
the pinned path ends in `{requestingRepo}/`, so the repository is a folder. Naming the file after the
slug as well (`mtm-waitlist-request.md`) was rejected as a second copy of the folder's own name. Writing
the request into the requesting repository was rejected: the request is to change the extension, so it
belongs beside the extension's source, and the requesting checkout is meant to gain nothing.

### R21 — How the run proves the first reply imported clean

**Decision.** Attempts are recorded beside the result. `node out/validate.js <checkout> --check <reply>
--record-attempt` appends `{ sha256, problems }` to `results/<slug>.attempts.json`. `--record` refuses
to write `import: pass` unless that file holds exactly one attempt and its problem list is empty, and
it copies the attempt count and the first attempt's problems into the `import` condition's detail.
Deleting the attempts file is how a fresh attempt begins, and that deletion is visible in git.

**Rationale.** SC-011 is about the *first* reply. The result's `profile.problems` cannot say that: a
profile corrected twice and imported on the third try is clean too. Recording each check as it happens
and gating the verdict on the count makes "first attempt" a measurement instead of a claim, and it costs
one small file per checkout.

**Alternatives considered.** A timestamp per attempt was rejected: results carry no timestamps so two
runs are byte-identical, and an attempts record that did would undo that. A count kept only inside the
final result was rejected: it would be written at the end, by the same run it grades, which is the
shape FR-017's own assumption rejects for records. Asking the assistant to state how many attempts it
took was rejected: that is a claim about the run, not a measurement of it.

## Facts verified, and what is still unknown

Verified in this run: the entry points and line numbers above; the absence of any glob matcher outside
the host; the baseline selection reproduced by segment exclusion; the conventions seam on
`analyzeWorkspace`; that `parseProfile`, `compileConventions` and `buildSetupPrompt` need no editor; the
self-test's root argument, summary line and exit code; the three checkouts' presence and file counts.

Verified this run for round three: the six `reportUnknownKeys` call sites are the only place an
accepted key set is written down (`profile.ts:215`, `:387`, `:429`, `:520`, `:561`, `:592`, `:660`); no
source file under `src/` mentions a compatibility check, `update_prompts` or `/speckit-companion-specify`,
so FR-023 to FR-028 are unmet rather than merely untested; `extensions/csharp-xaml-graph/update_prompts/`
exists and is empty; `REPLY_SHAPE` (`profilePrompt.ts:54`) shows six link keys and a
`generatedMembers[].form` value whose admitted set appears nowhere in the prompt text; the test double
returns a queued answer or `undefined` from `showInformationMessage` (`support/vscode.ts:575`), which is
what a dismissed dialog looks like; and `makeContext(extensionPath)` (`support/vscode.ts:653`) takes a
path, so a test can point the request writer at a temporary folder.

**Read inline, not by subagent.** The four reader briefs this step dispatched failed with a quota
refusal, so the areas were read directly instead of through workers. The findings above are the result
of that reading, and every line number is from the file as it stands.

Still unknown, and answered by a guard rather than by research: whether a given installation has the
chat command registered (R1's guard); whether a given checkout's conventions can all be expressed in a
profile (what the bar exists to find out); and whether the extension's own folder is writable at the
moment a request is written (R20's clipboard fallback).

Nothing is left as `NEEDS CLARIFICATION`.
