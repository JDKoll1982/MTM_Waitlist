# Tasks — Repository Profiles for the C#/XAML Dependency Graph

**Feature**: `specs/023-extension-repo-profile` | **Branch**: `extension-profile-update` | **Size**: oversized
**Plan**: `plan.md` | **Spec**: `spec.md` | **Contract**: `contracts/profile.schema.json` | **Data model**: `data-model.md`

## Scale note

This feature edits sixteen files across the extension's analysis core, its host wiring, its page script and
its test double. A reader should follow one of two tracks: what a profile changes about classification
(Phase 3), and how a profile gets created (Phases 2, 4 and 6). Watch two places. The layer classifier must
produce the same output when no profile exists, so every Phase 3 task runs the unprofiled case beside the
profiled one. The chat command id is internal to the workbench, so every path that opens chat is guarded and
has a clipboard fallback.

Two structural facts about this list:

1. **Phase 2 is large on purpose.** `src/graph/profile.ts`, `src/graph/conventions.ts`,
   `src/graph/profilePrompt.ts`, `src/graph/model.ts` and `src/extension.ts` are each used by more than one
   user story, so the plan's single-host-file design puts them in one owner phase, built once, before any
   story begins. Each story phase then owns the files that only it needs.
2. **US4 is ordered before US3.** Both are P2, so priority order is unchanged. US4 formats the profile rows
   that US3's page script renders, so the formatter has to exist first. Ordering them the other way would
   leave a phase of untested markup waiting for its caller.

### Which phase owns which file

Each file below has exactly one owner, so implement can hand a phase to its own worker. A path that appears
in two phases is a defect in this list, not a discovery for implement.

| Phase | Files it owns |
|---|---|
| Setup | `extensions/csharp-xaml-graph/package.json`, `specs/023-extension-repo-profile/baseline.md` |
| Foundational | `extensions/csharp-xaml-graph/src/graph/profile.ts`, `src/graph/conventions.ts`, `src/graph/profilePrompt.ts`, `src/graph/model.ts`, `src/extension.ts`, `test/support/vscode.ts`, `test/graph/profile.test.ts`, `test/graph/conventions.test.ts`, `test/extension.test.ts` |
| US1 | `extensions/csharp-xaml-graph/src/graph/layers.ts`, `src/graph/analyze.ts`, `src/graph/findings.ts`, `test/graph/layers.test.ts`, `test/graph/analyze.test.ts`, `test/graph/findings.unused.test.ts`, `test/graph/findings.bindings.test.ts`, `test/graph/findings.cycles.test.ts` |
| US2 | `extensions/csharp-xaml-graph/src/menu/menuHtml.ts`, `src/menu/menuView.ts`, `test/graph/profilePrompt.test.ts`, `test/menu/menuHtml.test.ts`, `test/menu/menuView.test.ts` |
| US4 | `extensions/csharp-xaml-graph/src/webview/client/profileProblems.ts`, `src/webview/client/style.css`, `test/webview/profileProblems.test.ts`, `test/graph/profileFallback.test.ts` |
| US3 | `extensions/csharp-xaml-graph/src/webview/html.ts`, `src/webview/panel.ts`, `src/webview/client/main.ts`, `src/selftest.ts`, `test/webview/html.test.ts`, `test/webview/panel.test.ts`, `test/webview/main.test.ts` |
| Polish | `extensions/csharp-xaml-graph/README.md` |

## Phase 1: Setup

**Wave 1: independent (different files):**

- [x] **T001** [P] Record the pre-change baseline in a new file: run `npm test` and `npm run selftest` in `extensions/csharp-xaml-graph`, and write down the pass and fail totals, the test-file count, the node, edge and finding counts `selftest` reports for this repository, and the extension's version (SC-003 depends on these numbers) · `specs/023-extension-repo-profile/baseline.md`
- [x] **T002** [P] Declare `csharpXamlGraph.profile.initialize`, `csharpXamlGraph.profile.import` and `csharpXamlGraph.profile.refresh` in `contributes.commands` with titles and the `C#/XAML Graph` category, add them to `activationEvents`, bump `version` to `0.6.0`, and add no setting and no dependency · `extensions/csharp-xaml-graph/package.json`

**⟶ Wait for Wave 1 to finish, then:** Phase 2 begins. T002's declared list is what T013 asserts.

## Phase 2: Foundational

Every file here is used by more than one story, so no story work starts until this phase is done.
`src/graph/profilePrompt.ts` holds both prompts: US2 opens the setup prompt and US3 opens the refresh
prompt, which is why it is shared rather than owned by either.

**Wave 1: independent (different files):**

- [x] **T003** [P] Add the optional `profileEvidence` field to `GraphModel` and the `ProfileEvidence` type it carries (name suffixes and prefixes with examples, namespace segments with counts, call names with type-argument counts, declared base types, attributes on methods and fields, XAML root tags, file counts), documented as never written by the graph JSON export · `extensions/csharp-xaml-graph/src/graph/model.ts`
- [x] **T004** [P] Add the `workspace.createFileSystemWatcher` seam to the test double with a `fireWatcherChange` helper in the style of `fireConfigurationChange`, and make `commands.getCommands` return a list a test can set, so the chat guard's absent branch is reachable · `extensions/csharp-xaml-graph/test/support/vscode.ts`
- [x] **T005** [P] Create the profile format: `RepositoryProfile` and its section types matching `contracts/profile.schema.json`, `PROFILE_FILE_NAME` (`csharp-xaml-graph.profile.json`), the built-in defaults taken from today's constants (the layer rules in `classifyType`'s decision order, the link call names, the 21 transparent bases, the four entry-point names, both generated-member rules, include and exclude), `parseProfile(text)` returning the profile with a `ProfileProblem[]`, `mergeProfile(existing, incoming)` where arrays union and existing scalars win, and `formatProfile(profile)` writing stable two-space JSON; it imports nothing from `vscode` (FR-001, FR-005, FR-009, FR-010, FR-013, FR-016) · `extensions/csharp-xaml-graph/src/graph/profile.ts`

**⟶ Wait for Wave 1 to finish, then:**

- [x] **T006** [P] Create the compiled conventions: `CompiledConventions` holding the resolved layer rules and default, the link name sets, the entry-point and transparent-base sets, the generated-member rules and the file selection; `compileConventions(profile?)` resolving a profile over the built-in defaults; `evaluateLayerRules(rules, default, decl)` returning the first rule that matches; and the predicates `isViewModelType`, `isServiceName` and `hasNamespaceSegment`; it imports nothing from `vscode` (FR-002, FR-003, FR-004) · `extensions/csharp-xaml-graph/src/graph/conventions.ts`
- [x] **T007** [P] Test the format through its public functions: a built-in profile has no problems; a profile section overrides defaults and an absent key keeps them; a present array replaces the default array; `mergeProfile` keeps every existing declaration and adds the new one once; a reply without `version: 1` is refused with the missing part named; a blank file reads as absent with a message rather than a parse error; an unknown key is reported and ignored; a malformed section keeps the rest (FR-010, FR-016) · `extensions/csharp-xaml-graph/test/graph/profile.test.ts`

**⟶ Wait for Wave 2 to finish, then:**

- [x] **T008** [P] Create both prompt builders over `ProfileEvidence`: `buildSetupPrompt(evidence, folderName)` asking one question per area (what marks a service, what marks a view model, how a type is registered, what counts as an entry point, which files matter), each question quoting its evidence, every question placed before any proposal; `buildRefreshPrompt(evidence, profile, folderName)` carrying the existing profile and asking only for what it does not cover; `nothingToProfile(folderName)` for a folder with no C# or XAML in scope (FR-007, FR-008) · `extensions/csharp-xaml-graph/src/graph/profilePrompt.ts`
- [x] **T009** [P] Test the ordered rules against today's behaviour: the built-in rule list classifies every case the current `classifyType` decides (interface service, interface as model, view-model name, `ViewModels` namespace for a class that does not read as data, enum as model, service name or `Services` namespace, default) to the same layer; a profile rule placed first wins over a namespace rule; `unlessNameSuffixes` keeps a row model a model; a rule with `kinds` applies to that kind only (FR-002) · `extensions/csharp-xaml-graph/test/graph/conventions.test.ts`

**⟶ Wait for Wave 3 to finish, then:**

- [x] **T010** Read the profile into the analysis: load `<workspace folder>/csharp-xaml-graph.profile.json` through `workspace.fs`, treat a missing or blank file as absent, compile the conventions, pass them to `analyzeWorkspace`, and use the profile's `files` section for the read; an explicitly configured `csharpXamlGraph.include` or `csharpXamlGraph.exclude` wins pattern by pattern; carry each `ProfileProblem` into the model's `warnings` with its dotted path; and report a selection that matched no file instead of drawing an empty canvas (FR-001, FR-005, FR-006, FR-013, FR-015) · `extensions/csharp-xaml-graph/src/extension.ts`

**⟶ Wait for Wave 4 to finish, then:**

- [x] **T011** Add the three profile commands: `initialize` builds the setup prompt and opens chat through `workbench.action.chat.open` with `{ query, isPartialQuery: true }` after `commands.getCommands` confirms the id, and on a missing id or a rejected execute copies the prompt to the clipboard and says what to do with it; `import` reads the clipboard, takes the first fenced block or the whole reply, refuses a reply `parseProfile` will not accept with nothing written and the missing part named, and otherwise merges into the existing file, writes it, opens it and re-reads; `refresh` opens chat with the refresh prompt, or initializes when no profile exists (FR-007, FR-009, FR-010, FR-011, FR-012, FR-016) · `extensions/csharp-xaml-graph/src/extension.ts`

**⟶ Wait for Wave 5 to finish, then:**

- [x] **T012** [P] Watch the profile and offer creation once: a `FileSystemWatcher` on the profile path clears the cached model and tells the open panel to re-read; a dismissed offer is not repeated in the session; a repository with no profile gets one non-blocking dismissible offer naming the create action; and the model states which workspace folder it read (FR-012, FR-014, US4 scenario 5) · `extensions/csharp-xaml-graph/src/extension.ts`
- [x] **T013** [P] Update the host suite for the new behaviour: the contributed command list in `CONTRIBUTED` gains the three profile ids, a deliberate assertion change that `package.json` must match; a profile in the folder changes the conventions handed to the analysis, while no profile leaves them at the built-in defaults; a malformed profile still produces a model and names the problem in `warnings`; an explicit `include` wins over the profile's file selection (FR-006, FR-013, FR-015) · `extensions/csharp-xaml-graph/test/extension.test.ts`

**⟶ Wait for Wave 6 to finish, then:**

- [x] **T014** Finish the host suite: initialize falls back to the clipboard when the chat command is absent; import refuses an unusable reply and writes nothing; importing a good reply writes the merged file and re-reads; a watcher change clears the cache; and the no-profile offer appears once and is dismissible (FR-007, FR-010, FR-012, FR-014) · `extensions/csharp-xaml-graph/test/extension.test.ts`

## Phase 3: User Story 1 (P1): a repository is read by the conventions it declares

**Files:** `extensions/csharp-xaml-graph/src/graph/layers.ts`, `extensions/csharp-xaml-graph/src/graph/analyze.ts`, `extensions/csharp-xaml-graph/src/graph/findings.ts`

### Tests

Every case below runs twice: once with a hand-written profile and once with none. The unprofiled run must
reproduce today's answer, which is what guards SC-003.

- [x] **T015** [P] Test classification by a profile: a type whose name suffix appears only in the profile's rules lands in the layer the profile names; a profile rule ordered before the `ViewModels` namespace rule wins when a type carries both; an `unlessNameSuffixes` entry keeps a row model a model; and with no profile the existing cases do not change (FR-002) · `extensions/csharp-xaml-graph/test/graph/layers.test.ts`
- [x] **T016** [P] Test link detection by a profile: a call name the profile declares as a generic pair call draws the `resolves` edge and registers both types, so neither is reported unused; a `typeof` pair call name declared by the profile draws its edge; the dependency-property edge is gated by the profile's view-model test rather than a `ViewModel` suffix, so a repository that calls its view models `Screen` keeps it; a data-context cast keyword the profile declares draws the binding edge; and with no profile every existing edge case is unchanged (FR-003, FR-006) · `extensions/csharp-xaml-graph/test/graph/analyze.test.ts`
- [x] **T017** [P] Test findings rules by a profile: a type named by the profile's `entryPoints` is not reported unused; a base type the profile lists as transparent adds no members, so a genuinely missing member is still reported; and with no profile the existing unused-type and unused-member cases are unchanged (FR-004, FR-006) · `extensions/csharp-xaml-graph/test/graph/findings.unused.test.ts`
- [x] **T018** [P] Test generated members by a profile: an attribute the profile declares on a method makes `FooCommand` and, for `FooAsync`, `FooCommand` count as members, so a binding to one is not dead; an attribute declared on a field makes the stripped, upper-cased name a member; and the built-in two rules still cover `RelayCommand` and `ObservableProperty` with no profile (FR-004) · `extensions/csharp-xaml-graph/test/graph/findings.bindings.test.ts`
- [x] **T019** [P] Guard the cycle report against the layer change: with no profile, the cycle findings over a fixed fixture are the same set as before the change, and a profile that reclassifies two types leaves cycle detection untouched because edges, not layers, drive it (FR-006) · `extensions/csharp-xaml-graph/test/graph/findings.cycles.test.ts`

### Implementation

**Wave 1: independent (different files):**

- [x] **T020** [P] Replace the hardcoded decision order in `classifyType` with `evaluateLayerRules` over the compiled layer rules, keeping its name and its documented reasons, and make `isViewModelLike` ask the conventions' view-model predicate instead of a suffix; the suffix, prefix and data-name lists move into the built-in profile (FR-002, FR-006) · `extensions/csharp-xaml-graph/src/graph/layers.ts`
- [x] **T021** [P] Read `entryPoints`, `transparentBases` and `generatedMembers` from the conventions instead of `ENTRY_POINT_NAMES`, `TRANSPARENT_BASES` and the two hardcoded attribute checks, keeping member names identical for the built-in rules and deriving generated names from the rule's `form` (FR-004, FR-006) · `extensions/csharp-xaml-graph/src/graph/findings.ts`

**⟶ Wait for Wave 1 to finish, then:**

- [x] **T022** Accept `conventions` in `AnalyzeOptions`, use the compiled link names for the four registration branches, the service locator and the data-context casts, gate the dependency-property edge on the conventions' view-model predicate, pass the conventions into `classifyType` and `computeFindings`, and derive `profileEvidence` from the merged declarations and XAML files onto the model (FR-002, FR-003, FR-004, FR-006) · `extensions/csharp-xaml-graph/src/graph/analyze.ts`

**Checkpoint:** with a profile declaring a convention the extension has never seen, classification,
registration links and findings follow the profile, and with no profile every existing test and the
selftest's recorded counts are unchanged. Story 1 is functional and testable on its own.

## Phase 4: User Story 2 (P1): the profile is created through AI chat, not by editing source

**Files:** `extensions/csharp-xaml-graph/src/menu/menuHtml.ts`, `extensions/csharp-xaml-graph/src/menu/menuView.ts`, `extensions/csharp-xaml-graph/test/graph/profilePrompt.test.ts`, `extensions/csharp-xaml-graph/test/menu/menuHtml.test.ts`, `extensions/csharp-xaml-graph/test/menu/menuView.test.ts`

### Tests

- [x] **T023** [P] Test the setup prompt: every question the prompt asks appears before any proposed answer; each question quotes evidence taken from the repository (a type name, a namespace segment, a call with its type-argument count); a folder with no C# or XAML produces the nothing-to-profile sentence instead of an empty prompt; the refresh prompt carries the existing profile and asks only for what it does not cover (FR-007, FR-008) · `extensions/csharp-xaml-graph/test/graph/profilePrompt.test.ts`
- [x] **T024** [P] Test the panel tab's new actions: two buttons, `Set up repository profile` and `Import profile reply`, render in the actions row, and the Commands table gains a row for each with what it does in plain words; the existing tab assertions are updated where the table grew (FR-007) · `extensions/csharp-xaml-graph/test/menu/menuHtml.test.ts`
- [x] **T025** [P] Test the forwarding: a `command` message naming `profileInitialize` runs `csharpXamlGraph.profile.initialize`, and one naming `profileImport` runs `csharpXamlGraph.profile.import` (FR-007, FR-009) · `extensions/csharp-xaml-graph/test/menu/menuView.test.ts`

### Implementation

**Wave 1: independent (different files):**

- [x] **T026** [P] Add the two buttons and their two Commands table rows to the panel tab, posting `command` messages named `profileInitialize` and `profileImport` through the existing `post` helper, and state in each row that the setup prompt asks before proposing (FR-007) · `extensions/csharp-xaml-graph/src/menu/menuHtml.ts`
- [x] **T027** [P] Extend `runCommand` to forward `profileInitialize` and `profileImport` to the two profile commands, in the same shape as the four command names it already maps (FR-007, FR-009) · `extensions/csharp-xaml-graph/src/menu/menuView.ts`

**Checkpoint:** a developer with no profile runs one action from the panel tab, answers a chat that asked
before proposing and quoted their own code, imports the reply, and the written profile is plain reviewable
text the graph then reads. Story 2 is functional and testable on its own.

## Phase 5: User Story 4 (P2): a missing or broken profile never blocks the graph

Ordered before Phase 6 because Phase 6's page script renders the rows this phase formats.

**Files:** `extensions/csharp-xaml-graph/src/webview/client/profileProblems.ts`, `extensions/csharp-xaml-graph/src/webview/client/style.css`, `extensions/csharp-xaml-graph/test/webview/profileProblems.test.ts`, `extensions/csharp-xaml-graph/test/graph/profileFallback.test.ts`

### Tests

- [x] **T028** [P] Test every profile failure the spec names, through the parse and compile surface: a blank file is treated as absent with a message and not a parse failure; a partial profile applies the answered parts and falls back for the rest; an unrecognised key is reported and the run continues; a malformed section leaves the other sections applied and names the section and the reason; a selection matching nothing is reported; the same declaration twice applies once with no error; a declaration matching nothing in the repository is kept (FR-013, SC-004) · `extensions/csharp-xaml-graph/test/graph/profileFallback.test.ts`
- [x] **T029** [P] Test the profile row renderer: one row per problem carrying the dotted path and the sentence; text is escaped; no problems renders nothing rather than an empty box; the rows come back as a block that sits above the findings list (FR-013) · `extensions/csharp-xaml-graph/test/webview/profileProblems.test.ts`

### Implementation

**Wave 1:**

- [x] **T030** Create the renderer that turns the model's profile problems into the Problems pane section, one row per problem with its path and sentence, escaped, returning an empty string when there is nothing to say (FR-013) · `extensions/csharp-xaml-graph/src/webview/client/profileProblems.ts`

**⟶ Wait for Wave 1 to finish, then:**

- [x] **T031** Style the profile section so it reads as a notice above the findings and not as a finding: one rule set for the block, a muted tone for the path, and the existing severity colours untouched (FR-013) · `extensions/csharp-xaml-graph/src/webview/client/style.css`

**Checkpoint:** every malformed or partial profile tried still opens the graph, each one names the part it
could not read in a form the page can display, and a repository with no profile is analysed exactly as
before. Story 4 is functional and testable on its own.

## Phase 6: User Story 3 (P2): new patterns are folded into the profile from the graph

**Files:** `extensions/csharp-xaml-graph/src/webview/html.ts`, `extensions/csharp-xaml-graph/src/webview/panel.ts`, `extensions/csharp-xaml-graph/src/webview/client/main.ts`, `extensions/csharp-xaml-graph/src/selftest.ts`

### Tests

- [x] **T032** [P] Test the toolbar: an `update-profile` button sits last in the actions row after `Export JSON`, and the Problems pane carries the container the profile rows fill; the three-row toolbar assertion is updated where the row grew, and the existing control assertions stand (FR-011) · `extensions/csharp-xaml-graph/test/webview/html.test.ts`
- [x] **T033** [P] Test the forwarding: a `profile` message from the page runs `csharpXamlGraph.profile.refresh`, in the same shape as `reanalyze` runs the refresh command (FR-011) · `extensions/csharp-xaml-graph/test/webview/panel.test.ts`
- [x] **T034** [P] Test the page script: clicking the control posts the `profile` message; a model whose warnings carry profile problems shows them above the findings; the tab badge still counts findings only, so its existing assertions do not change (FR-011, FR-013) · `extensions/csharp-xaml-graph/test/webview/main.test.ts`

### Implementation

**Wave 1: independent (different files):**

- [x] **T035** [P] Add the `Update profile` button after `Export JSON` in the actions row and the profile section container at the head of the Problems pane (FR-011) · `extensions/csharp-xaml-graph/src/webview/html.ts`
- [x] **T036** [P] Handle a `profile` message by executing `csharpXamlGraph.profile.refresh`, beside the existing `reanalyze` case, so the page asks for the command rather than duplicating it (FR-011) · `extensions/csharp-xaml-graph/src/webview/panel.ts`

**⟶ Wait for Wave 1 to finish, then:**

- [x] **T037** Wire the new button to post the `profile` message, and render the profile section through the renderer from Phase 5 ahead of the findings list, leaving the badge counting findings only (FR-011, FR-012, FR-013) · `extensions/csharp-xaml-graph/src/webview/client/main.ts`

**⟶ Wait for Wave 2 to finish, then:**

- [x] **T038** Add two selftest checks: the graph page carries the profile control, and a hand-written profile that changes one classification changes that classification and nothing else in the node, edge and finding counts (FR-011, FR-012) · `extensions/csharp-xaml-graph/src/selftest.ts`

**Checkpoint:** from an open graph whose profile does not cover a convention, one press opens the assistant
with the existing profile and the request to find what it misses; importing the proposal keeps every prior
declaration, adds the new one once, and the graph re-reads in the same session with no editor reload. Story 3
is functional and testable on its own.

## Phase 7: Polish

**Wave 1:**

- [x] **T039** Document the profile in `extensions/csharp-xaml-graph/README.md`: where the file lives, the sections with a worked example a person can adapt by hand, the three commands and where to find them, what happens with no profile, and that an explicit `include` or `exclude` wins; rewrite the "How a type is placed in a layer" paragraph now that the rules are data, and correct the Tests section's file and test counts to the numbers the suite reports · `extensions/csharp-xaml-graph/README.md`

**⟶ Wait for Wave 1 to finish, then:**

- [x] **T040** Prove backward compatibility: with no profile present, analyse this repository on this build and compare the node, edge and finding counts against the pre-change baseline recorded in Phase 1; they must be equal (SC-003). Confirm too that adopting a repository's conventions needs no change outside the profile file (SC-002). Record the comparison with `write-context.py --verified`
- [x] **T041** Run the gates in `extensions/csharp-xaml-graph` and record what each reports: `npm run typecheck`, `npm test` (no test that passed before may fail, and the count grows by the new suites), `npm run selftest` (every check green, including the two added in T038) and `npm run coverage` (the thresholds in `vitest.config.mts` still hold). Record each command's result with `write-context.py --verified`
- [x] **T042** Validate against Success Criteria: walk SC-001 to SC-007, including the end-to-end adoption timing, the three broken-profile trials, the same-session refresh, and the profile's readability in a diff; record each result with `write-context.py --verified`

## Dependencies & Execution Order

**Phase order:** Setup (T001, T002) → Foundational (T003 to T014) → US1 (T015 to T022) → US2 (T023 to T027)
→ US4 (T028 to T031) → US3 (T032 to T038) → Polish (T039 to T042). No story starts before the Foundational
phase is done, because every one of them consumes the profile format, the compiled conventions or the host
wiring built there.

**Waves, and what each blocks:**

- Setup, wave 1: T001 and T002 run together; T002 gates T013's contributed-command assertion.
- Foundational, wave 1: T003, T004 and T005 together; all three block wave 2.
- Foundational, wave 2: T006 and T007 together; block wave 3.
- Foundational, wave 3: T008 and T009 together; block the host work.
- Foundational, wave 4: T010 alone; blocks T011.
- Foundational, wave 5: T011 alone; blocks wave 6.
- Foundational, wave 6: T012 and T013 together; block T014.
- Foundational, wave 7: T014 closes the phase.
- US1 tests (T015 to T019) are written first and fail until wave 1 and wave 2 land: T020 and T021 together,
  then T022, which both of them gate.
- US2: T026 and T027 run together after Phase 2; the three test tasks (T023 to T025) assert behaviour built
  in Phase 2 and in this phase's two files, so they may be written before or beside them.
- US4: T030 before T031; the two test tasks are written first and must fail until the renderer exists.
- US3: T035 and T036 together, then T037, then T038.
- Polish: T039 runs beside the verification tasks; T040, T041 and T042 come after every code task.

**Parallel opportunity:** Setup wave 1, Foundational waves 1 to 3, US1's five test files, US2's two source
files, and US3's two wave-1 source files are each internally independent and can be handed to separate
workers. Everything that touches `src/extension.ts` is sequential by construction.

## Phase 8: Convergence

Appended by `/speckit.converge` after T001–T042 closed. Two amendments landed after those tasks were
written — FR-017 to FR-022 (User Story 5, cross-repository validation) and FR-023 to FR-028 (User Story 6,
the stated prompt contract and the compatibility gate) — and none of their mechanism exists in the code
yet. The assessment found the first build intact: `npm test` reports 34 files and 945 tests passing, and
`npm run selftest` reports 1095 nodes, 4776 edges and 21 findings over 815 C# and 93 XAML files, the
numbers in `baseline.md`. Nothing above this line is reopened, renumbered or reworded.

Two orderings are load-bearing and the waves follow them. The contract table is built, given to the
parser and rendered in the prompts (Waves 1 to 3) before anything asserts a prompt against it, in that
order. The verdict is computed (Wave 1) before the offer asks anything (Wave 4) and before the request
writer is proven (Waves 2 and 3), because a refusal that names nothing has nothing to offer. The validator
is built (Waves 6 to 9) and proven on this repository (Wave 10) before any external checkout is measured
(Wave 11), and inside each checkout the expected-answer record is committed before that checkout's first
measured run.

The three external checkouts gain exactly one file each — `csharp-xaml-graph.profile.json` at that
checkout's root — and the checker confirms it is the only change. Every record, result, attempts file and
request file lives in this repository.

**Wave 1: independent (new files, nothing imports them yet):**

- [x] **T043** [P] Create the one table of profile fields: `PROFILE_CONTRACT`, one row per key at every depth naming the dotted `path`, the exact `key`, its `section`, the value `shape` in words, whether it is `required` (`version` alone), what it `meaning`s, and what the extension does when the key is `absent`; give the closed value sets for `layers.default` and the layer names, `layers.rules[].kinds`, `findings.generatedMembers[].on` and `.form`, and reach the nested keys of `layers.rules[]` and `findings.generatedMembers[]` through `fields`; add `contractKeys(...)` returning the accepted keys of one section or nested path and `renderContract()` rendering every row as prose grouped by section; it imports nothing from `vscode` and nothing from `node:fs` (FR-023, FR-024) · `extensions/csharp-xaml-graph/src/graph/profileContract.ts`
- [x] **T044** [P] Create the compatibility verdict: `StructureFacts`, `IncompatibilityReason` and `CompatibilityVerdict` matching `contracts/compatibility.schema.json`; `ANALYSIS_FILE_CAP` moved here from `extension.ts`'s `FILE_CAP` so the walk and the analysis compare one number; `assessCompatibility(facts, cap)` emitting one reason per failed observation from `no-csharp`, `no-xaml`, `over-analysis-cap` and `unsupported-project`, each carrying its counts and up to three repository-relative paths as evidence; `unknown` with one note when no fact could be read; and a `.csproj` or `.sln` recorded as a fact that never decides the verdict; pure, importing nothing from `vscode` or `node:fs` (FR-025, FR-026) · `extensions/csharp-xaml-graph/src/graph/compatibility.ts`
- [x] **T045** [P] Create the pure half of the proof: `ExpectedAnswerRecord` with its `ExpectedType`, `ExpectedLink` and `ExpectedFinding` entries per `contracts/expected-record.schema.json`; `Measurement` (the three counts, per-layer, per-edge-kind and per-finding-kind counts, sorted type/layer and link pairs); `Mismatch` (`item`, `name`, `expected`, `actual`); `ValidationReport` and `ValidationResult` per `contracts/validation-result.schema.json`; `measure(model)` and `compareWithRecord(model, record)` returning one mismatch per failing item — an unexpected type, an absent one, a missing link, an invented link, a finding expected and absent, a finding present and unexpected; plus `recordAttempt` and `firstAttemptClean`, so `import` passes only on exactly one clean attempt; pure, importing nothing from `vscode` or `node:fs`, never collapsing the outcome to a count (FR-017, FR-020, FR-021, FR-022) · `extensions/csharp-xaml-graph/src/graph/validation.ts`
- [x] **T046** [P] Create the profile's file selection as a pure matcher: `**`, `*` and `?` over workspace-relative paths, an exclude pattern pruning a directory during the walk when its relative path matches, a pattern outside that grammar reported as a problem rather than silently mismatched, and the built-in selection expressed through the same function so the headless run and the host agree on which files are read (FR-005, FR-018) · `extensions/csharp-xaml-graph/src/graph/files.ts`

**⟶ Wait for Wave 1 to finish, then:**

**Wave 2: independent (different files):**

- [x] **T047** [P] Make the parser's accepted keys come from the table: replace the literal arrays at all six `reportUnknownKeys` call sites — the root, `layers`, `links`, `findings`, `files` and the `findings.generatedMembers[]` item keys — with the rows the contract declares for that section, so a key the prompt never describes cannot be accepted and no second list can fall behind (FR-024) · `extensions/csharp-xaml-graph/src/graph/profile.ts`
- [x] **T048** [P] State the whole contract in both prompts: render `renderContract()` before the first question in `buildSetupPrompt` and in `buildRefreshPrompt` by the same call, keep the reply example but make every key in it a row the table describes, and leave every existing question, its quoted evidence and the `nothingToProfile` sentence unchanged (FR-023, FR-024) · `extensions/csharp-xaml-graph/src/graph/profilePrompt.ts`
- [x] **T049** [P] Create the one walk and the one writer: `gatherStructure(root)` listing the tree once with `node:fs` and never opening a file, counting `.cs` and `.xaml` under the built-in selection, collecting `.csproj`/`.fsproj`/`.vbproj`/`.vcxproj` and `.sln` as sorted repository-relative paths, and setting `truncated` when the cap stops it; and `writeUpdateRequest(extensionRoot, slug, text)` creating `update_prompts/<slug>/`, writing `request.md`, replacing an existing one, and returning the path on success or a failure the caller can fall back on rather than throwing (FR-025, FR-027) · `extensions/csharp-xaml-graph/src/structure.ts`
- [x] **T050** [P] Generate the request text from the verdict alone: `CHANGES_FOR_REASON` mapping every reason code to at least one required change, and `buildUpdateRequest(verdict)` producing the heading, the `/speckit-companion-specify` line immediately under it, one section per reason in the verdict's order, one subsection per change carrying the repository's evidence, and the closing line saying nothing in the requesting repository was created or changed (FR-027) · `extensions/csharp-xaml-graph/src/graph/compatibility.ts`

**⟶ Wait for Wave 2 to finish, then:**

**Wave 3: independent (test files, one per module):**

- [x] **T051** [P] Test the contract in three directions: every row parses a minimal profile declaring just that key with no problem at its path; every key path the reply example in `profilePrompt.ts` uses is a row; every property `contracts/profile.schema.json` names at any depth is a row and every row is one of its properties, read from `specs/023-extension-repo-profile/contracts/` and skipped when that path is absent so the extension's suite stays green without the spec (FR-023, FR-024) · `extensions/csharp-xaml-graph/test/graph/profileContract.test.ts`
- [x] **T052** [P] Test both prompts against the table: every row appears with its meaning, requiredness, value shape and absent-key behaviour, and appears before the first question in the create prompt and before the request to propose in the refresh prompt; no key in the reply example lacks a row; a refresh prompt on a profile that answers only some sections still states the whole contract; the existing evidence and nothing-to-profile assertions stand (FR-023, FR-024) · `extensions/csharp-xaml-graph/test/graph/profilePrompt.test.ts`
- [x] **T053** [P] Test one verdict per fact combination: no `.cs` gives one `no-csharp` reason; no `.xaml` one `no-xaml`; counts at the cap one `over-analysis-cap`; a `.fsproj`, `.vbproj` or `.vcxproj` one `unsupported-project` naming up to three paths; a mixed folder gives every failed reason rather than the first; a compatible folder with a `.csproj` and a `.sln` and no reason gives `compatible`; unreadable facts give `unknown` with one note; every reason names its structure and carries non-empty evidence, and every code in `CHANGES_FOR_REASON` maps to at least one change (FR-025, FR-026) · `extensions/csharp-xaml-graph/test/graph/compatibility.test.ts`
- [x] **T054** [P] Test the request file: the heading names the repository; the second line states `/speckit-companion-specify` verbatim; every reason is one section in the verdict's order opening with the sentence the person was shown; every change is a subsection with at least one evidence line and no absolute path, machine name or timestamp; the closing line says nothing in the requesting repository changed; `writeUpdateRequest` creates `update_prompts/<slug>/request.md` under the extension root it is given, replaces an existing file rather than stacking a second one, and reports a failure instead of throwing when the folder is not writable (FR-027) · `extensions/csharp-xaml-graph/test/graph/updateRequest.test.ts`
- [x] **T055** [P] Test the matcher: the built-in include and exclude yield exactly the 815 C# and 93 XAML files `baseline.md` records for this repository, excluding `bin`, `obj`, `node_modules`, `.vs`, `TestResults`, `out`, `.specify`, `.github` and `*.Tests` directories; `**`, `*` and `?` each match what they should and nothing else; an excluded directory is pruned so a file inside it is never offered; a pattern outside that grammar is reported as a problem (FR-005, FR-018) · `extensions/csharp-xaml-graph/test/graph/files.test.ts`
- [x] **T056** [P] Test the comparison: each mismatch kind is produced by its own case — an unexpected type, a recorded type the run does not produce, a missing link, an invented link, a finding expected and absent, a finding present and unexpected; one mismatch turns the verdict to failing; an empty array in the record is checked as a claim of "none"; a record omitting a type the run produces fails and names that type; the measurement carries no timestamp and no absolute path, and two equivalent runs measure identically; `firstAttemptClean` is false with no attempts file, false with one attempt carrying a problem, and true with exactly one clean attempt (FR-019, FR-020, FR-022) · `extensions/csharp-xaml-graph/test/graph/validation.test.ts`

**⟶ Wait for Wave 3 to finish, then:**

**Wave 4: independent (different files):**

- [x] **T057** [P] Put the verdict before the prompt and the offer after it: call `gatherStructure` and `assessCompatibility` at the head of `openSetupChat` and of `profile.refresh`; on `incompatible` never build the prompt, show each reason as its own plain sentence naming the structure, ask one offer — `Start a new specification` or `Not now` — at most once per folder per session, treat a dismissed dialog as `Not now`, write the request only on yes through `writeUpdateRequest` with the clipboard fallback naming `update_prompts/<slug>/request.md` when the folder is not writable, and push one `not analysed › <structure>: <sentence>` warning per reason onto the model so it survives the dialog, the dismissal and a re-analyse; on `unknown` proceed as today with one note; on `compatible` proceed exactly as today; replace `FILE_CAP` with the shared `ANALYSIS_FILE_CAP` (FR-025, FR-026, FR-027, FR-028) · `extensions/csharp-xaml-graph/src/extension.ts`
- [x] **T058** [P] Recognise the second note kind: beside the profile-problem recogniser, read `not analysed › <structure>: <sentence>` and render it in the same list above the findings with the structure in the muted path tone, escaped, and nothing rendered when there are no notes of either kind (FR-028) · `extensions/csharp-xaml-graph/src/webview/client/profileProblems.ts`
- [x] **T059** [P] Carry both note kinds in one block: the heading becomes `This repository (N notes)` counting profile problems and compatibility notes together, and the tab badge keeps counting findings only so its existing assertions do not change (FR-028) · `extensions/csharp-xaml-graph/src/webview/html.ts`, `extensions/csharp-xaml-graph/src/webview/client/main.ts`

**⟶ Wait for Wave 4 to finish, then:**

**Wave 5: independent (different test files):**

- [x] **T060** [P] Test the host's gate and offer: an incompatible folder names each structure and builds no prompt; nothing is created or changed before the answer; yes writes `update_prompts/<slug>/request.md` holding `/speckit-companion-specify`; no writes nothing and the reason is still on `model.warnings` after the dialog is gone; a dismissed dialog is a no; a compatible folder asks nothing and proceeds; an unknown verdict proceeds with one note; the offer is asked once per folder per session; and a read-only extension root falls back to the clipboard with the relative path named (FR-025, FR-026, FR-027, FR-028) · `extensions/csharp-xaml-graph/test/extension.test.ts`
- [x] **T061** [P] Test the pane's two note kinds: profile problems and compatibility notes render in one list above the findings, each escaped, with the heading counting both and the badge counting findings only; an empty list renders no block (FR-028) · `extensions/csharp-xaml-graph/test/webview/profileProblems.test.ts`, `extensions/csharp-xaml-graph/test/webview/html.test.ts`, `extensions/csharp-xaml-graph/test/webview/main.test.ts`

**⟶ Wait for Wave 5 to finish, then:**

**Wave 6: the validator entry point:**

- [x] **T062** Add the fourth entry: `src/validate.ts` built to `out/validate.js` by a fourth `entryPoints`/`outfile` pair in `scripts/build.mjs`, reached by a `validate` script in `package.json`, with argument parsing, the exit-code table and the fixed print order from `contracts/validate.cli.md`; implement `--prompt` (analyse the checkout with no profile, derive the evidence the analysis already gathers, print exactly what `buildSetupPrompt` would put in chat) and `--check <reply> [--record-attempt]` (take the first fenced block or the whole text with the import command's rule, run `parseProfile`, print one line per problem, append `{ sha256, problems }` to `specs/023-extension-repo-profile/results/<slug>.attempts.json` only when asked); nothing in either mode touches the checkout, and no mode waits for input (FR-018, FR-019) · `extensions/csharp-xaml-graph/src/validate.ts`, `extensions/csharp-xaml-graph/scripts/build.mjs`, `extensions/csharp-xaml-graph/package.json`

**⟶ Wait for Wave 6 to finish, then:**

**Wave 7: the validator's verdict and request modes:**

- [x] **T063** Add `--compatibility` and `--request` to the same entry: `--compatibility` walks the checkout through `gatherStructure`, prints the verdict, then the facts, then one `cannot analyse <structure>: <sentence>` line per reason with its evidence indented under it, and exits 0, 3 or 2 as the contract states, writing nothing; `--request` runs the same verdict and, only when it is incompatible, writes `update_prompts/<slug>/request.md` from the extension's own root resolved from `out/validate.js` and prints its path, writing nothing on a compatible checkout and exiting 2 when no verdict could be reached (FR-026, FR-027, FR-028) · `extensions/csharp-xaml-graph/src/validate.ts`

**⟶ Wait for Wave 7 to finish, then:**

**Wave 8: the measuring mode:**

- [x] **T064** Add `--record <expected> --result <results>`: read `<checkout>/csharp-xaml-graph.profile.json`, parse it and compile its conventions, select files through the matcher in `src/graph/files.ts`, analyse with `includeExternalTypes: false`, compare with the record item by item, run `node out/selftest.js <checkout>` as a child process for the `invariants` condition, pass `import` only through `firstAttemptClean`, and write the result with no timestamp, no duration and no absolute path; print the counts, the five conditions as PASS or FAIL, every mismatch on its own line as `<item> <name>: expected <expected>, got <actual>`, and the final `VERDICT:`; exit 0, 1 or 2, and on 2 write no result so a moved checkout is recorded as not run rather than as a pass (FR-019, FR-021, FR-022) · `extensions/csharp-xaml-graph/src/validate.ts`

**⟶ Wait for Wave 8 to finish, then:**

**Wave 9: the determinism mode:**

- [x] **T065** Add `--compare <first> <second>`: compare two result files on their measurements, their mismatch lists and their conditions, ignoring `profile.sha256` because two textually different profiles can express the same conventions; print `IDENTICAL` and exit 0, or `DIFFERENT` naming the first field that differs and exit 1 (FR-019, SC-009) · `extensions/csharp-xaml-graph/src/validate.ts`

**⟶ Wait for Wave 9 to finish, then:**

**Wave 10: this repository, proven before any other:**

- [x] **T066** Write this repository's expected-answer record from its own code and commit it before its first measured run: every declared type in scope by the `Namespace.Type` id the graph emits, with its checkout-relative file and the layer its own conventions give it; every view-to-view-model pair it states; every finding known true of it, by kind and the types it names, matching the 21 findings `baseline.md` records; `source` saying how and when it was read; no run output, no absolute path, no timestamp (FR-017) · `specs/023-extension-repo-profile/expected/mtm-waitlist.json`
- [x] **T067** Print this repository's setup prompt with `node out/validate.js . --prompt` and keep the printed text, then give a fresh-context subagent that prompt and this checkout's path alone — no other description of the format — and let it write exactly one file, `csharp-xaml-graph.profile.json` at this repository's root; then run `--check <reply> --record-attempt` once and record the outcome, because a first reply carrying a problem is recorded rather than quietly retried (FR-019, FR-023, SC-011) · `csharp-xaml-graph.profile.json`, `specs/023-extension-repo-profile/results/mtm-waitlist.attempts.json`
- [x] **T068** Measure this repository against its record: `--record expected/mtm-waitlist.json --result results/mtm-waitlist.json`, which must report 1095 nodes, 4776 edges and 21 findings — identical to `baseline.md`, so the profile added here corrects nothing the extension already reported; then repeat into `results/mtm-waitlist.repeat.json` and `--compare` the two; a mismatch is fixed by re-reading the code, never by copying the run into the record (FR-022, SC-009, SC-010) · `specs/023-extension-repo-profile/results/mtm-waitlist.json`, `specs/023-extension-repo-profile/results/mtm-waitlist.repeat.json`

**⟶ Wait for Wave 10 to finish, then:**

**Wave 11: the three external checkouts, each measured only after its record is committed.**

**Checkout `mtm-wip` at `C:\Users\johnk\Source\repos\MTM_WIP`:**

- [x] **T069** Write and commit the expected-answer record for `MTM_WIP` from its own code, before its first measured run: every declared type in its scope with its file and the layer its conventions give it, every view-to-view-model pair it states, every finding known true of it, read from the checkout and never from a run (FR-017) · `specs/023-extension-repo-profile/expected/mtm-wip.json`
- [x] **T070** Print that checkout's setup prompt, hand the prompt and the checkout path alone to a fresh-context subagent, and let it write nothing but `<checkout>/csharp-xaml-graph.profile.json`; check its first reply once with `--check --record-attempt`, and confirm from the checkout's own `git status --short` that the profile file is the only change (FR-019, SC-002, SC-011) · `C:\Users\johnk\Source\repos\MTM_WIP\csharp-xaml-graph.profile.json`, `specs/023-extension-repo-profile/results/mtm-wip.attempts.json`
- [x] **T071** Measure `MTM_WIP` with `--record expected/mtm-wip.json --result results/mtm-wip.json`, require all five conditions, then repeat into `results/mtm-wip.repeat.json` and `--compare`; a checkout below the bar is recorded as failing with every mismatched item named, and its code is never corrected to make the run pass (FR-020, FR-021, FR-022, SC-008, SC-009) · `specs/023-extension-repo-profile/results/mtm-wip.json`, `specs/023-extension-repo-profile/results/mtm-wip.repeat.json`

**Checkout `winui3-learning-lab` at `C:\Users\johnk\Source\repos\WinUI3LearningLab`:**

- [x] **T072** Write and commit the expected-answer record for `WinUI3LearningLab` from its own code, before its first measured run: every declared type in its scope with its file and layer, every view-to-view-model pair it states, every finding known true of it, read from the checkout and never from a run (FR-017) · `specs/023-extension-repo-profile/expected/winui3-learning-lab.json`
- [x] **T073** Print that checkout's setup prompt, hand the prompt and the checkout path alone to a fresh-context subagent, and let it write nothing but `<checkout>/csharp-xaml-graph.profile.json`; check its first reply once with `--check --record-attempt`, and confirm from the checkout's own `git status --short` that the profile file is the only change (FR-019, SC-002, SC-011) · `C:\Users\johnk\Source\repos\WinUI3LearningLab\csharp-xaml-graph.profile.json`, `specs/023-extension-repo-profile/results/winui3-learning-lab.attempts.json`
- [x] **T074** Measure `WinUI3LearningLab` with `--record expected/winui3-learning-lab.json --result results/winui3-learning-lab.json`, require all five conditions, then repeat into `results/winui3-learning-lab.repeat.json` and `--compare`; a checkout below the bar is recorded as failing with every mismatched item named, and its code is never corrected to make the run pass (FR-020, FR-021, FR-022, SC-008, SC-009) · `specs/023-extension-repo-profile/results/winui3-learning-lab.json`, `specs/023-extension-repo-profile/results/winui3-learning-lab.repeat.json`

**Checkout `mtm-receiving-application` at `C:\Users\johnk\Source\repos\MTM_Receiving_Application`:**

- [x] **T075** Write and commit the expected-answer record for `MTM_Receiving_Application` from its own code, before its first measured run: every declared type in its scope with its file and layer, every view-to-view-model pair it states, every finding known true of it, read from the checkout and never from a run (FR-017) · `specs/023-extension-repo-profile/expected/mtm-receiving-application.json`
- [x] **T076** Print that checkout's setup prompt, hand the prompt and the checkout path alone to a fresh-context subagent, and let it write nothing but `<checkout>/csharp-xaml-graph.profile.json`; check its first reply once with `--check --record-attempt`, and confirm from the checkout's own `git status --short` that the profile file is the only change (FR-019, SC-002, SC-011) · `C:\Users\johnk\Source\repos\MTM_Receiving_Application\csharp-xaml-graph.profile.json`, `specs/023-extension-repo-profile/results/mtm-receiving-application.attempts.json`
- [x] **T077** Measure `MTM_Receiving_Application` with `--record expected/mtm-receiving-application.json --result results/mtm-receiving-application.json`, require all five conditions, then repeat into `results/mtm-receiving-application.repeat.json` and `--compare`; a checkout below the bar is recorded as failing with every mismatched item named, and its code is never corrected to make the run pass (FR-020, FR-021, FR-022, SC-008, SC-009) · `specs/023-extension-repo-profile/results/mtm-receiving-application.json`, `specs/023-extension-repo-profile/results/mtm-receiving-application.repeat.json`

**⟶ Wait for Wave 11 to finish, then:**

**Wave 12: the incompatible repository, both answers:**

- [x] **T078** Point the validator at a folder the analysis cannot read — a `.fsproj`-only folder with no `.cs` and no `.xaml`, created outside all four checkouts — and confirm `--compatibility` exits 3, names each structure it cannot handle in a plain sentence with its evidence, and that running it again without `--request` leaves no request file anywhere: that second run is the no answer, and the folder's own listing and status are unchanged; record both runs and the fact that no file was written under the incompatible-repository heading of the rollup (FR-026, FR-028, SC-013) · `specs/023-extension-repo-profile/validation.md`
- [x] **T079** Then answer yes: `--request` writes `extensions/csharp-xaml-graph/update_prompts/{requestingRepo}/request.md`, whose heading names the requesting repository, whose next line states `/speckit-companion-specify` verbatim, whose every reason is a section and every change a subsection carrying at least one evidence line naming a count or a repository-relative structure; run it twice and confirm one current request rather than a stack, and confirm the requesting folder still changed nothing (FR-027, SC-013) · `extensions/csharp-xaml-graph/update_prompts/{requestingRepo}/request.md`

**⟶ Wait for Wave 12 to finish, then:**

**Wave 13: the rollups and the gates:**

- [x] **T080** Add the four-of-four rollup beside the incompatible-repository heading T078 wrote: one section per checkout with its compatibility verdict, the profile's digest, the measurement, the mismatches (none, or each named), the attempts count and whether the first reply was clean, the determinism comparison, and the pass or fail verdict; state either that four of four reached the bar or name the failing checkouts with their items; name each machine path once as prose, because no result file carries one; and say how a recorded result is re-checked from disk (FR-021, FR-022, SC-008) · `specs/023-extension-repo-profile/validation.md`
- [x] **T081** Document the new surfaces in the extension's README: the profile contract as prose with the worked example kept in agreement with it, the compatibility verdict and what each of the two answers does, where the request file lands and that it is run with `/speckit-companion-specify`, and the validator's invocations with what each writes and what each exit code means, plus how to re-check a recorded result (FR-023, FR-026, FR-027) · `extensions/csharp-xaml-graph/README.md`
- [x] **T082** Run every gate from `extensions/csharp-xaml-graph` and record what each reports: `npm run typecheck`; `npm test`, where no test that passed before may fail and the count grows beyond 34 files and 945 tests by the new suites; `npm run selftest`, still 1095 nodes, 4776 edges and 21 findings with every check green; and each validator mode once — `--compatibility`, `--prompt`, `--check`, `--record --result`, `--compare`, `--request` — with its printed verdict and exit code (Constitution VI, SC-003, SC-012) · verified record with `write-context.py --verified`

**Checkpoint:** the four checkouts each carry a committed record, a profile produced from the prompt alone,
a recorded result and a repeat comparison; the three external working trees show one added file each and
nothing else; the prompt states every key before it asks anything; an incompatible repository is refused by
name, offered, and answered both ways; and the gates above are green with this repository's three counts
unchanged.

## Phase 9: Convergence

Appended by `/speckit.converge` after T001–T065 closed and T066–T082 were found open. Four requirements
landed on the spec after Phase 8 was written — FR-029 and FR-030 (the inspection surface, and the
provenance it forces on every expected-answer record) and FR-031 and FR-032 (the toolchain and the
coverage guard) — and they change the method Phase 8 assumes in four places: a record is no longer
written by reading a checkout by hand, which `research.md` R12, `plan.md`'s "How one checkout is proven"
step 1 and the wording of T066, T069, T072 and T075 all say it is; the record and the prompt must come
from one inspection; each fresh-context author is handed that inspection and not only the prompt; and the
toolchain's proof is recorded rather than asserted.

Nothing above this line is reopened, renumbered or reworded. Where this phase supersedes the method of an
earlier open task it says so by ID, so implement performs each step once: T083 corrects the figures T039
left behind and T081 does not cover; T090 closes the gap T066 was left open for; T091 to T093 replace the
hand-read derivation T069, T072 and T075 name; T094 retires the script those tasks were written around;
T097 replaces T082's mode list and its 34-file premise. The steps Phase 8 already describes correctly are
not repeated: T067, T068, T070, T071, T073, T074, T076, T077, T078, T079, T080 and T081 stand as written,
and T084 records how the runs among them are amended.

Waves. The inspection is built and proven (Waves 2 and 3) before any record is regenerated (Wave 4),
because a record is only evidence of the surface if the surface existed when it was produced. Every record
is regenerated before any measured run, so commit order still proves that no record echoes the run it
judges. The toolchain proof (Wave 5) rereads the gates after every code change in this phase, and the
final gate run (Wave 6) comes last.

Two constraints hold throughout. The three external checkouts gain one profile file each and nothing else;
every record, result, attempts file and request file lives in this repository. No record, result, request
or printed output carries a machine path, a duration or a timestamp.

**Wave 1: independent (different files):**

- [x] **T083** [P] Correct the two stale figures in `README.md` from the suite rather than from memory: the Tests paragraph reading `34 files, 945 tests` becomes the 39 files and 1028 tests `npm test` reports, and both rows of its coverage table are re-read from `coverage/coverage-summary.json` — `All source` is statements 98.2, branches 90.32 and functions 98.5, not the 98.43 / 93.31 / 100 still printed — with the guard's floors (lines 98, statements 98, branches 90, functions 98) stated beside them so the page and the guard cannot disagree again; leave every other figure and section of the page alone, because T081 owns the new surfaces (Constitution — Documentation & Extensibility, FR-032, SC-020, SC-021) (contradicts) · `extensions/csharp-xaml-graph/README.md`
- [x] **T084** [P] Write the provenance decision and the amended validation procedure in one new file: state that `research.md` R12 and `plan.md`'s "How one checkout is proven" step 1 are superseded by FR-030, giving the reason the spec gives — the hand-derived attempt reproduced the layer counts exactly and still disagreed on the findings, so a record built that way proves nothing — and name every artifact that rests on the old method, including the `source` string in `expected/mtm-waitlist.json` and `tools/derive_expected.py` with its `tools/__pycache__/` directory; state that each fresh-context profile run (T067, T070, T073, T076) is handed its checkout's `--inspect` output beside the setup prompt and no other description of the format; state that an author who adds tooling of its own records it with the evidence the surface lacked, as SC-016 requires; and state that the rollup at T080 carries each record's provenance (FR-030, SC-014, SC-015, SC-016) (contradicts) · `specs/023-extension-repo-profile/results/provenance.md`

**⟶ Wait for Wave 1 to finish, then:**

**Wave 2: the inspection surface, built once and used by the prompt, the record and the author.**

- [x] **T085** Lift the evidence derivation out of the analysis and into an inspection that can run on its own: move `deriveProfileEvidence` (`src/graph/analyze.ts:165`, today reachable only through `analyzeWorkspace` at line 1005) into a new `src/graph/inspect.ts` and export it, so the model, the prompts and the inspection report one derivation of one repository; add `inspectFolder(root, selection?)` composing `gatherStructure` from `src/structure.ts` with the parse-and-evidence pass, returning the structure facts, the selected file counts and the `ProfileEvidence` a profile author needs, without building a graph, deciding a layer, a link or a finding, or importing `vscode`; keep the model's evidence byte-identical by calling the moved function from `analyzeWorkspace` (FR-029, FR-006) (missing) · `extensions/csharp-xaml-graph/src/graph/inspect.ts`, `extensions/csharp-xaml-graph/src/graph/analyze.ts`
- [x] **T086** [P] Test the inspection on its own terms: over this repository it reports 815 C# files, 93 XAML files, the 16 projects and the 1 solution `--compatibility` prints, and the same `ProfileEvidence` the model carries for the same folder, so the prompt and the inspection cannot describe one repository two ways; it decides no layer, link or finding; it writes nothing and touches no editor API; a folder with no C# and no XAML reports its facts and an empty evidence set rather than failing; and a folder it cannot read is reported rather than thrown (FR-029, US7/AC4) (missing) · `extensions/csharp-xaml-graph/test/graph/inspect.test.ts`
- [x] **T087** [P] Expose the inspection as a supported surface beside the existing modes: add `--inspect` and `--json` to `src/validate.ts`, reusing `parseInvocation`, `usage` and the fixed print order, printing the compatibility facts first and then the evidence section by section, exiting 0 on a folder it can read and 2 when no evidence could be gathered, writing nothing inside the checkout, waiting for no input, and add the mode to the command surface described in `contracts/validate.cli.md` (FR-029, US7/AC4) (missing) · `extensions/csharp-xaml-graph/src/validate.ts`, `specs/023-extension-repo-profile/contracts/validate.cli.md`

**⟶ Wait for Wave 2 to finish, then:**

**Wave 3: the record producer, so no record is written by hand again.**

- [x] **T088** Add `--expected <path>` to `src/validate.ts` as the one way an expected-answer record is produced: take the checkout's inspection from `inspectFolder`, compile its conventions from its profile through `compileConventions`, and write the record `contracts/expected-record.schema.json` describes — every declared type by its `Namespace.Type` id with its checkout-relative file and the layer the compiled rules give it, every view-to-view-model pair the markup states, and every finding the declarations and rules imply — with `source` naming this surface and the extension version, entries in a stable order, and no absolute path, duration or timestamp; it must never read `analyzeWorkspace`'s model, so the record cannot echo the run it judges (FR-030, FR-017, US7/AC3) (missing) · `extensions/csharp-xaml-graph/src/validate.ts`
- [x] **T089** Prove the producer by its own output on this repository: `--expected` run twice into two files must be byte-identical, and the record it writes must reproduce the analysis's own counts when read back — 1051 declared types split 82 view, 76 viewmodel, 475 service and 418 model, the view-to-view-model pairs the markup states, and the 21 findings split 8 cycle, 9 unused-type and 4 unused-member that `baseline.md` records — with no machine path or timestamp in the file and a `source` naming nothing but the surface; keep this output as the working file `results/mtm-waitlist.candidate.json` for T090 to adopt (FR-030, US7/AC3, SC-010) (missing) · `specs/023-extension-repo-profile/results/mtm-waitlist.candidate.json`

**⟶ Wait for Wave 3 to finish, then:**

**Wave 4: one record per checkout, produced by the surface before any measured run.**

- [x] **T090** [P] Regenerate this repository's record from the surface and reconcile it by reading the code, closing the gap T066 was left open for: take the candidate T089 produced, check every finding the analysis reports against the declarations that justify it — the 4 unused members, the 9 unused types and the 8 cycles `baseline.md` counts — and correct the record from the code, never by copying the run's mismatch list; replace `expected/mtm-waitlist.json` with the result, its `source` naming the surface and carrying no machine path, delete the candidate file, and close T066 against the regenerated record (FR-030, FR-017, SC-010, SC-014) (partial) · `specs/023-extension-repo-profile/expected/mtm-waitlist.json`
- [x] **T091** [P] Produce and commit the record for `MTM_WIP` from the surface, before its first measured run, replacing the hand read T069 names: run `--expected` over `C:\Users\johnk\Source\repos\MTM_WIP`, review every entry against that checkout's own declarations and markup, correct from the code where an entry is wrong, and commit it with a `source` naming the inspection and no machine path; the checkout itself gains nothing yet (FR-030, FR-017, SC-014) (missing) · `specs/023-extension-repo-profile/expected/mtm-wip.json`
- [x] **T092** [P] Produce and commit the record for `WinUI3LearningLab` from the surface the same way, replacing the hand read T072 names, reviewed against that checkout so its scope and its file counts are what the inspection reports rather than what a walk assumed (FR-030, FR-017, SC-014) (missing) · `specs/023-extension-repo-profile/expected/winui3-learning-lab.json`
- [x] **T093** [P] Produce and commit the record for `MTM_Receiving_Application` from the surface the same way, replacing the hand read T075 names, reviewed against that checkout's own declarations and markup (FR-030, FR-017, SC-014) (missing) · `specs/023-extension-repo-profile/expected/mtm-receiving-application.json`

**⟶ Wait for Wave 4 to finish, then:**

- [x] **T094** Retire the hand-written derivation now that every record comes from the surface: delete `tools/derive_expected.py` and `tools/__pycache__/` from the spec folder, or keep the script only if a checkout still needs evidence the surface does not report, in which case record that exception in `results/provenance.md` together with the evidence it supplied; confirm that every file under `expected/` names the inspection in its `source` and that no other script outside the extension remains unrecorded (FR-030, SC-015, SC-016) (unrequested) · `specs/023-extension-repo-profile/tools/`, `specs/023-extension-repo-profile/results/provenance.md`

**⟶ Wait for the retirement to finish, then:**

**Wave 5: the toolchain's proof, recorded rather than asserted.**

- [x] **T095** Record the upgraded toolchain and re-verify every gate against it, because the upgrade was performed outside this task list and nothing in the feature folder states what it produced: write each declared package's version beside the latest release it resolves to (`typescript` 7.0.2, `vitest` and `@vitest/coverage-v8` 5.1.2, `jsdom` 31.0.1, `htmlparser2` 12.0.0, `@types/vscode` 1.146.0, `@types/node` 26.6.11, and the rest of `devDependencies`), the `engines.vscode` floor `^1.146.0` against the `@types/vscode` version it compiles with, and the two replaced options (`coverage.all` expressed through `include`, and `module`/`moduleResolution` now `preserve`/`bundler`); then run `npm run typecheck`, `npm test`, `npm run selftest` and `npm run coverage` from `extensions/csharp-xaml-graph` and record each exit code with 39 test files, 1028 tests, `ALL CHECKS PASSED`, and 1095 nodes, 4776 edges and 21 findings reproduced on a second run — reporting any count that moved rather than absorbing it; record each command with `write-context.py --verified` (FR-031, SC-017, SC-018, SC-019, SC-020) (partial) · `specs/023-extension-repo-profile/toolchain.md`
- [x] **T096** Record the coverage guard's real position and the reason each moved threshold sits where it does, so no threshold can be read as eroded or as set by guesswork: read `vitest.config.mts`'s four thresholds against the report `coverage/coverage-summary.json` holds — lines 98 against 98.19, statements 98 against 98.2, branches 90 against 90.32 and functions 98 against 98.5 — state beside them that branches and functions were lowered from 93 and 100 with the reason the guard already gives in its own comment, which is that the validation and compatibility code arrived less covered than the analysis modules, and record the comparison in `toolchain.md` beside the gate results; raise no threshold and change no reason silently (FR-032, SC-021) (partial) · `specs/023-extension-repo-profile/toolchain.md`, `extensions/csharp-xaml-graph/vitest.config.mts`

**⟶ Wait for Wave 5 to finish, then:**

**Wave 6: the gate that closes the phase.**

- [x] **T097** Run every gate and every validator mode from `extensions/csharp-xaml-graph` and record what each reports, replacing T082 whose premise (34 files, 945 tests) and mode list predate this phase: `npm run typecheck`; `npm test`, where no test that passed before may fail and the suite reports 39 files and 1028 tests; `npm run selftest`, still 1095 nodes, 4776 edges and 21 findings with every check green; `npm run coverage` against the guard T096 recorded; and one run of each validator mode — `--inspect`, `--prompt`, `--compatibility`, `--check`, `--record --result`, `--compare`, `--request` and `--expected` — with its printed verdict and exit code, confirming that no mode wrote inside a checkout and none waited for input; record every command with `write-context.py --verified` (Constitution VI, FR-029, FR-030, SC-012, SC-020) (partial) · verified record with `write-context.py --verified`

**Checkpoint:** every expected-answer record in `expected/` names the inspection surface in its `source`
and no hand-written derivation remains; the inspection reports a folder's facts and evidence without an
editor, without changing the folder and without deciding a layer, a link or a finding; the toolchain's
versions, the editor floor and the coverage guard are recorded with their reasons, and the analysis
reproduces 1095 nodes, 4776 edges and 21 findings; and the whole gate set, including the new modes, is
green. T066's gap is closed by T090, and the runs T067 to T080 describe then measure against records the
surface produced.