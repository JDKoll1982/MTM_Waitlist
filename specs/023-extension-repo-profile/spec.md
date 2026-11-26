# Feature Specification: Repository Profiles for the C#/XAML Dependency Graph

**Feature Branch**: `023-extension-repo-profile`
**Created**: 2026-11-26
**Status**: Draft
**Input**: The C#/XAML dependency graph carries one set of naming and layering conventions that applies to every repository it reads. A repository whose conventions differ is read wrongly, and correcting that means editing the extension's source. Each repository should own a profile instead, created and refreshed through AI chat, with a control in the graph that asks the agent to look for patterns the profile does not yet know.

## User Scenarios & Testing

### User Story 5 - The approach is proven on real repositories, not only on this one (Priority: P1, ranked first)

The extension has so far only been shown to work on this repository, whose conventions are the ones baked into it. A pass here therefore proves little: the profile approach is only credible if a repository that has never seen the extension can be read correctly from a profile created for it. Four checkouts are named to prove it: this repository; a 538-file C# application with 341 XAML files and a solution file at its root; a 1,278-file C# application with 858 XAML files and a solution file at its root; and a 1,522-file C# application with 284 XAML files and no solution file at its root.

Each checkout gets its own profile, created by an assistant working in a fresh context with no knowledge of this repository, from the extension's own setup prompt plus that checkout's evidence. Each is measured against an expected-answer record written for it, and each must reach 100% accuracy.

**What "100% accuracy" means.** For the types and files inside that checkout's analysis scope:

- every type the record expects is placed in the layer that checkout's conventions give it, with zero mismatches;
- every view-to-view-model relationship the checkout actually states is found, with zero missing links and zero invented links;
- no finding is produced by a convention mismatch: nothing is reported as an unbound view that is bound, no binding is reported dead that resolves, no type is reported unused that is used;
- importing the profile reports zero problems;
- the extension's own invariants pass against that checkout, not only against this one.

A checkout meeting every line above is at 100%. One that is not is reported as failing, with the item named.

**Why this priority**: this is the credibility of the whole feature. If the approach only works where the conventions are already known, the profile is decoration. Every other story here is proven on a single repository; this is the one that makes the claim general.

**Independent Test**: take a checkout the extension has never seen, produce its expected-answer record from the extension's inspection surface, give only the setup prompt and that checkout to an assistant with no prior context, import the profile it produces, run the comparison, and confirm every item matches.

**Acceptance Scenarios**:

1. **Given** this repository, **When** the profile created for it is imported and its run is compared with its expected-answer record, **Then** every expected type sits in the layer its conventions give it and every relationship it states is found, with zero mismatches.
2. **Given** a repository that has never seen the extension, analysed from its location while it is not the open workspace, **When** a fresh assistant produces a profile from the setup prompt and that repository's evidence alone, **Then** the extension accepts the profile, importing it reports no problem, and a second run reproduces the same measurements.
3. **Given** a repository whose conventions cannot all be expressed in a profile, **When** its run is compared with its expected-answer record, **Then** every mismatch is reported one item at a time, naming the type and what was expected of it, and that repository is recorded as failing the bar rather than scored as a pass.
4. **Given** the four named checkouts, **When** all four have been validated, **Then** each has a recorded result and the four-of-four bar is either met or the failing checkouts are named with their mismatches.
5. **Given** the extension's own invariants, **When** they are run against any one of the checkouts, **Then** they pass there as they pass here.

*Numbering: this story carries the next number rather than the first because the stories, tasks and contracts already recorded refer to US1 to US4 by number. It ranks first in priority.*

---

### User Story 7 - The extension inspects the repository, so the author does not have to (Priority: P1)

Understanding a repository well enough to profile it means reading its folders, its project files, its namespaces and its naming habits, and reporting what they show. Left to themselves, the people and assistants who do that each write a script for the occasion, and a purpose-written script is a second, unpublished reading of the same repository. The extension supplies that inspection itself: given a workspace folder, it reports the structural and convention evidence a profile author needs, drawn from the same evidence the profile questions are built from, so the questions and the inspection cannot describe one repository two different ways. The inspection runs without an editor, leaves the folder it reads untouched, and serves a person and an unattended assistant alike. An author whose repository needs something the inspection does not report may still add tooling of their own, and says why the supplied inspection was not enough.

**Why this priority**: every profile in the validation set is produced by an assistant that must first understand a repository it has never seen, and every expected-answer record must be produced the same way. If that understanding is invented privately each time, the prompt and the record drift apart, and the drift is only discovered when a comparison fails. Taking the parsing burden off the author is what makes the four-checkout claim reproducible rather than lucky.

**Independent Test**: give an author a folder they have not seen, together with the inspection output and nothing else, and confirm the profile they produce needs no script of their own; then produce an expected-answer record from the same inspection and confirm it reproduces the analysis's own counts.

**Acceptance Scenarios**:

1. **Given** an author asked to profile a repository they have not seen, **When** they begin, **Then** the inspection reports the evidence the profile questions are built from, and the author writes no script of their own to understand the repository.
2. **Given** a repository that needs evidence the supplied inspection does not report, **When** the author adds tooling of their own, **Then** they can add it, and they state why the supplied inspection was not enough.
3. **Given** an expected-answer record produced from the supported inspection surface, **When** that checkout's analysis is run and compared with the record, **Then** the record reproduces the analysis's own counts, so any mismatch that remains is a disagreement about the checkout and not an artefact of how the record was made.
4. **Given** a workspace folder to inspect, **When** the inspection is run with no editor open and nobody attending it, **Then** it reports the same evidence, changes nothing in that folder, and needs no editor to run.

---

### User Story 1 - A repository is read by the conventions it declares (Priority: P1)

A developer works in a repository whose layering does not match the built-in conventions. Its services end in `Handler`, its view models sit in a `Screens` namespace, and its registrations run through a bespoke wiring call. They place a profile beside the code declaring those things. The graph then classifies and links the types by what the profile says, and nothing in the extension's source changes.

**Why this priority**: this is the change itself. Chat initialization and the refresh control only produce and update the file this story defines, so without it there is nothing to create.

**Independent Test**: hand-write a profile for a repository with one convention the extension has never seen, open the graph, and confirm the types are classified by the profile rather than by the built-in conventions.

**Acceptance Scenarios**:

1. **Given** a repository whose profile declares its own name suffixes, **When** the graph is shown, **Then** types are placed in the layers the profile declares.
2. **Given** a profile declaring the call that registers a service, **When** the graph is shown, **Then** registration links appear from that call, and the registered types are not reported as unused.
3. **Given** a profile declaring which types are entry points, which base types are transparent and which members are generated, **When** the findings are computed, **Then** those declarations are honoured in the report.
4. **Given** a profile declaring which files are read, **When** the graph is shown, **Then** only those files are analysed.
5. **Given** a profile declaring a precedence between two naming signals, **When** a type carries both, **Then** the layer the profile's precedence names is the one assigned.

---

### User Story 2 - The profile is created through AI chat, not by editing source (Priority: P1)

A developer adopts the tool in a repository that has never used it. They run one action. Before anything is written, the assistant asks the questions the profile needs answered — what marks a service, what marks a view model, how registration happens, which files matter — with each question carrying evidence drawn from that repository. The developer accepts or corrects the answers, the reply is imported, and a profile appears that they can read, adjust and commit with the rest of the code.

**Why this priority**: this is how the profile comes to exist without the developer first learning the profile's shape. Story 1 proves a profile works; this makes one obtainable in a repository nobody has prepared.

**Independent Test**: in a repository with no profile, run the initialization action, answer the chat, import the reply, and confirm the profile exists, is readable as text, and is what the graph then reads.

**Acceptance Scenarios**:

1. **Given** a repository with no profile, **When** the initialization action runs, **Then** the assistant opens with a setup prompt already prepared for that repository.
2. **Given** the setup prompt, **Then** it asks its questions before proposing any answer, and each question quotes evidence from the repository — a type name, a namespace, a call — so the developer can answer from what they can see.
3. **Given** the assistant's reply, **When** it is imported, **Then** a profile is written as plain readable text that a person can review in a diff and commit.
4. **Given** a reply that does not answer the questions in a usable form, **When** it is imported, **Then** no profile is written and the developer is told what was missing.
5. **Given** a profile written by the import, **When** the graph is shown, **Then** it reads the repository by that profile with no further step.
6. **Given** the developer declines, **Then** the repository is still analysed by the built-in conventions and nothing is written to it.

---

### User Story 6 - The assistant is never left to guess, and an incompatible repository is answered honestly (Priority: P1)

The person who creates or refreshes a profile is not shown the extension's source, so anything the prompt leaves implicit becomes an assumption. Two things follow.

The prompt must state the whole contract. Whenever the extension prepares a prompt for creating or refreshing a profile, that prompt describes in full what the extension expects the profile file to contain: every key, what it means, whether it is required or optional, the shape of each value, and what the extension does when the key is absent. A key the prompt does not describe is not written by the assistant. The format is stated before the questions, or alongside them, so the assistant knows what it is answering toward and does not infer a name, a case, or a nesting from habit.

Compatibility is settled before any profile work. Before the extension starts gathering conventions, it looks at the repository's actual structure and decides whether it can analyse it at all: which languages and project kinds are present, whether the files the extension reads are there, whether the repository is of a kind its analysis understands. If the repository is compatible, profile work proceeds as today. If it is not, the extension tells the person why, naming the specific things it cannot handle rather than a general failure, and then asks whether they want to start a new specification to make the extension compatible with that repository. Nothing is created or changed until that question is answered. If the answer is yes, a very detailed request file is written into `extensions/csharp-xaml-graph/update_prompts/{requestingRepo}/`, stating exactly what must change in the extension for that repository to be compatible, each change carrying the evidence from the repository that justifies it, and the file stating that it is to be run with `/speckit-companion-specify`. If the answer is no, nothing is written and the reason the repository is incompatible stays visible for the rest of the session rather than disappearing with a dialog.

**Why this priority**: a prompt that leaves the format implicit makes every profile a guess, and the guess is only discovered at import, after the person has invested the effort. A repository the extension cannot read, met with a general failure, leaves the person with no path forward and the extension with no record of what it lacks. Both are the same failure — the person is left to work out what the tool wanted — and both are cheap to prevent.

**Independent Test**: give a fresh assistant only the create prompt and an unfamiliar checkout's evidence, import its first reply, and confirm it reports zero problems; then point the extension at a repository it cannot analyse and confirm the specific structures are named, the offer is asked, and the request file appears only after the offer is accepted.

**Acceptance Scenarios**:

1. **Given** a prompt prepared for creating or refreshing a profile, **When** it is presented, **Then** it states the whole contract — every key, its meaning, whether it is required or optional, its value shape, and the absent-key behaviour — before the questions or alongside them.
2. **Given** a prompt that states the contract, **When** a fresh assistant answers from that prompt and the repository's evidence alone, **Then** its first reply imports with zero problems, and it has written no key the prompt did not describe.
3. **Given** a repository being profiled for the first time, **When** profile work begins, **Then** a compatibility verdict is issued for that repository before any convention question is asked.
4. **Given** a repository whose structure the extension cannot analyse, **When** the compatibility check runs, **Then** the person is told which specific structures cannot be handled, is not given a general failure, and is asked whether to start a new specification to make the extension compatible.
5. **Given** that question has been asked but not answered, **Then** no file has been created or changed in the repository.
6. **Given** the person answers yes, **Then** a request file appears under `extensions/csharp-xaml-graph/update_prompts/{requestingRepo}/` naming every change the extension needs, each with the evidence from the repository that justifies it, and stating that it is to be run with `/speckit-companion-specify`.
7. **Given** the person answers no, **Then** nothing is written, and the reason the repository is incompatible stays visible for the rest of the session.
8. **Given** a compatible repository, **When** profile work proceeds, **Then** it proceeds as it does today, with no compatibility question interrupting it.

---

### User Story 3 - New patterns are folded into the profile from the graph (Priority: P2)

Months later the repository grows a second way of registering services and a new suffix for its handlers. The graph misreads those types. From the graph itself, the developer presses a control that asks the assistant to find conventions the profile does not yet cover. The assistant proposes the additions, the developer imports them, and the graph re-reads and places those types correctly, in the same session.

**Why this priority**: a profile that can only be created once goes stale, and a stale profile is worse than none because it looks authoritative. Keeping it current is what makes the first story hold over time.

**Independent Test**: add a convention the profile does not know, press the refresh control, import the proposal, and confirm the affected types are classified correctly without reloading the editor.

**Acceptance Scenarios**:

1. **Given** an open graph and a profile that does not cover a convention present in the repository, **When** the refresh control is pressed, **Then** the assistant opens with the existing profile and a request to find what it does not cover.
2. **Given** the assistant proposes additions, **When** they are imported, **Then** the profile keeps everything it previously declared and gains the additions.
3. **Given** the updated profile, **Then** the repository is re-read and the new classification appears without the reader restarting anything.
4. **Given** the assistant proposes something the profile already declares, **When** it is imported, **Then** the declaration is not duplicated and the existing one is not silently contradicted.
5. **Given** no profile exists yet, **When** the refresh control is pressed, **Then** it initializes one instead of asking to update a file that is not there.

---

### User Story 4 - A missing or broken profile never blocks the graph (Priority: P2)

A colleague clones the repository without the profile, or edits it by hand and makes a mistake. The graph still opens. It applies what the profile says, falls back to the built-in conventions for the rest, and says plainly which part it could not read.

**Why this priority**: the tool answers questions on a working day, under time pressure. A profile problem must cost a warning, never a broken tool. It is second only because the value above can be demonstrated without it.

**Independent Test**: open the graph once with no profile and once with a deliberately broken one, and confirm both open and the second names what it could not read.

**Acceptance Scenarios**:

1. **Given** no profile, **When** the graph is shown, **Then** the result matches the built-in conventions exactly, and the chance to create a profile is offered once without blocking.
2. **Given** a profile with a malformed section, **When** the graph is shown, **Then** it opens, honours the sections it could read, uses the built-in conventions for the rest, and names what it could not read.
3. **Given** a profile carrying something the tool does not recognise, **Then** the graph opens and reports it rather than failing.
4. **Given** a profile whose file selection matches nothing, **Then** the graph says so instead of showing an unexplained empty canvas.
5. **Given** several workspace folders open, **Then** each folder's profile applies to that folder, and the graph states which folder it read.

---

### User Story 8 - Every package stays current, and the floor moves with it (Priority: P2)

The extension is built out of packages, and those packages are not still: each one has a latest release, and a tool that trails one is being exercised against an API nobody else runs. The dependencies and the development tooling are therefore kept on their newest releases, and the editor version the extension declares it supports moves whenever the API types it compiles against move ahead of it. Because an upgrade can change what the analysis does without anyone intending it, an upgrade is judged against the numbers already recorded rather than declared harmless: the same nodes, edges and findings as before it, and the whole gate set green. Where the coverage guard has to come down because new code is less covered than old, the new figure is the suite's real position and the reason travels beside it.

**Why this priority**: a toolchain a major behind fails slowly — what it misses is silent, and the first sign is a package that will no longer take a fix. The proof costs almost nothing here because the baseline already exists, so there is no excuse for accepting an upgrade on faith. It sits below the profile work because nothing above depends on it.

**Independent Test**: on the upgraded toolchain, run the analysis and the gate set and read the node, edge and finding counts against the baseline recorded before the upgrade; then read the coverage guard and confirm every threshold is the suite's current figure with its reason written beside it.

**Acceptance Scenarios**:

1. **Given** an upgrade whose run reproduces the recorded baseline, **When** the gate set is run, **Then** the upgrade is accepted and the recorded counts are exactly what they were before it.
2. **Given** an upgrade that changes any recorded count — nodes, edges, findings, or the test and coverage figures — **When** it is run, **Then** the change is reported rather than accepted, and someone states whether the code moved or the expectation did.
3. **Given** a package whose newer major would require an editor version above the declared floor, **When** that upgrade is taken, **Then** the floor is moved deliberately to what the package needs and the decision is recorded with its reason, rather than the package being quietly held back.
4. **Given** the coverage guard at its current figures, **When** it is read, **Then** each figure is the suite's actual position and carries the reason it sits where it does, so nobody has to guess whether a threshold was eroded or measured.

---

### Edge Cases

- A profile exists but is blank: treated as missing, with a message that says so rather than a parse failure.
- A profile answers only some of the questions: the answered parts apply, the rest fall back, and the graph names the gaps.
- The same declaration appears twice: applied once, with no error.
- A declared pattern currently matches nothing in the repository: kept, not pruned, so it survives until the code it describes arrives.
- The profile changes while the graph is open: the reader is told a re-read is needed, or the graph re-reads.
- Two declarations of the profile disagree about one type: the precedence the profile states decides.
- No assistant is available: initialization cannot run, so a hand-written profile from a documented example must remain possible without leaving the graph.
- The same reply is imported twice: the second import adds nothing new.
- A repository with no C# or XAML in scope: initialization says there is nothing to profile rather than writing an empty profile.
- A profile committed with conventions that disagree with the code: the graph follows the profile, since the profile is the repository's stated intent, and the disagreement is visible as a wrong classification.
- A validated checkout has moved or is no longer present when its result is checked: its recorded result stands, and a fresh run is reported as not run rather than as a pass.
- An expected-answer record omits a type the checkout contains: the comparison reports the unexpected type as a mismatch, so an omission in the record cannot be scored as accuracy.
- A checkout's conventions cannot all be expressed in a profile: the run reports the items it could not place or link, and that checkout fails the bar with those items named instead of being rounded up.
- A repository is compatible enough to open but has no C# or XAML in the files the extension reads: the compatibility verdict says so up front, before any convention question, rather than the prompt asking questions nothing can answer.
- The compatibility offer is left unanswered when the session ends: nothing is written, and the incompatibility reason stays visible with the rest of the session's record.
- An author adds tooling of their own because the supplied inspection does not report what their repository needs: the addition is recorded with the evidence the surface lacked, and the profile it helps produce still faces the same comparison as any other.
- A newer major of a package arrives in a different module format: it is bundled into the extension rather than resolved at run time, so nothing a user installs depends on how the package was published.
- An upgrade changes a recorded count: the difference is reported, and either the code is corrected or the baseline is re-justified in its own record; it is never absorbed as the new normal.
- A package's newest release demands an editor above the declared floor and the floor cannot be moved yet: the package waits with that stated as the reason, rather than the floor being left claiming support it does not have.

---

### Requirements › Functional Requirements

**FR-001**: The extension MUST read a repository's conventions from a profile stored in that repository, one profile per workspace folder.

**FR-002**: The profile MUST be able to declare layer classification: name suffixes, name prefixes, namespace segments, and the precedence between them.

**FR-003**: The profile MUST be able to declare link detection: the call or method names that create registration links, and the conventions by which markup binds to a view model.

**FR-004**: The profile MUST be able to declare findings rules: types that count as entry points, base types that are transparent to findings, and members that are generated rather than written.

**FR-005**: The profile MUST be able to declare which files are read and which are excluded.

**FR-006**: The extension MUST fall back to its built-in conventions for anything the profile does not declare, and MUST behave exactly as it does today when no profile exists.

**FR-007**: An initialization action MUST open AI chat with a setup prompt prepared for the current repository, and that prompt MUST ask its questions before proposing any answer.

**FR-008**: The setup prompt MUST carry repository evidence for each question it asks, taken from the repository being profiled.

**FR-009**: The extension MUST import the assistant's reply into the profile and MUST write it as plain, human-readable text a person can review, adjust and commit.

**FR-010**: The import MUST refuse a reply that would not produce a usable profile, write nothing in that case, and name what was missing.

**FR-011**: The graph MUST offer a control that asks the AI agent to find conventions the profile does not yet cover and propose an addition to it.

**FR-012**: Applying a profile MUST cause the repository to be re-read, so the visible graph reflects it without restarting the editor.

**FR-013**: An unreadable, malformed or partial profile MUST NOT prevent the graph from opening; the extension MUST apply what it can read, fall back for the rest, and report what it could not read.

**FR-014**: A repository with no profile SHOULD be offered the chance to create one, once per session, and that offer MUST be dismissible without blocking the analysis.

**FR-015**: A file pattern the person using the tool configured explicitly MUST keep taking effect, and MUST win over the profile's file selection.

**FR-016**: Refreshing a profile MUST preserve every declaration it already holds; it MUST add or correct, never silently drop.

**FR-017**: For each checkout used to validate this approach, the specification MUST carry an expected-answer record — the types in its analysis scope, the layer each belongs to, the view-to-view-model pairs it actually states, and the findings known to be true of it — and a run against that checkout MUST be compared with that record automatically, passing or failing as a whole.

**FR-018**: The extension MUST be able to analyse a checkout given its location, without any edit to the extension's source and without that checkout having to be the open workspace.

**FR-019**: Validation MUST be repeatable from nothing: an assistant with no prior context, given only the setup prompt and the checkout, MUST produce a profile the extension accepts, and a second run of that profile MUST reproduce the same measurements as the first.

**FR-020**: A comparison MUST report every mismatch one item at a time, naming the type and what was expected of it, and MUST NOT reduce the outcome to a count.

**FR-021**: The result of validating a checkout MUST be recorded — the profile used, the measurements taken, the mismatches found, and whether the 100% bar was met — so the four-checkout claim can be re-checked without repeating the run.

**FR-022**: The 100% bar MUST be assessed on every condition User Story 5 names — layer placement, view-to-view-model links, findings free of convention mismatches, a clean profile import, and the extension's own invariants passing against that checkout — and a checkout failing any one of them MUST be recorded as failing rather than as a partial success.

**FR-023**: Every prompt the extension prepares for creating or refreshing a profile MUST state the complete profile contract explicitly — every key the profile accepts, what the key means, whether it is required or optional, the shape of its value, and what the extension does when the key is absent — and MUST state it before the questions or alongside them, so the assistant knows what its answers are being shaped into.

**FR-024**: A prompt MUST leave no accepted key undescribed, and MUST NOT leave the assistant to infer a key's name, letter case or nesting from habit; a key the prompt does not describe MUST NOT be written by the assistant.

**FR-025**: Before gathering any convention, the extension MUST examine the repository's actual structure — which languages and project kinds are present, whether the files the extension reads are present, and whether the repository is of a kind the extension's analysis understands — and MUST issue a per-repository compatibility verdict from what it found.

**FR-026**: When the verdict is that a repository is incompatible, the extension MUST tell the person why by naming the specific structures it cannot handle rather than reporting a general failure, and MUST then ask whether they want to start a new specification to make the extension compatible with that repository; nothing MUST be created or changed before that question is answered.

**FR-027**: When the person answers yes to that offer, the extension MUST write a request file under `extensions/csharp-xaml-graph/update_prompts/{requestingRepo}/` that names every change required in the extension for that repository to become compatible, carries for each named change the evidence from the repository that justifies it, and states that the file is to be run with `/speckit-companion-specify`.

**FR-028**: When the person answers no to that offer, the extension MUST write nothing, and the reason the repository is incompatible MUST be retained and stay visible for the rest of the session rather than disappearing with the dialog that asked the question.

**FR-029**: The extension MUST provide, as a supported surface, an inspection that takes a workspace folder and reports the structural and convention evidence a profile author needs — the same evidence the profile questions are built from, so the prompt and the inspection agree about the repository; that inspection MUST run without VS Code, MUST NOT modify the folder it inspects, and MUST be usable both by a person and by an assistant working unattended.

**FR-030**: Every expected-answer record used to validate a checkout MUST be produced from that supported inspection surface rather than from a script written for the occasion, because a derivation written by hand cannot be trusted to agree with the analysis it judges: the attempt made so far, using a purpose-written script, reproduced the count of layer attributions exactly and yet disagreed on the findings, which means a record built that way cannot prove anything.

**FR-031**: The extension's dependencies and its development tooling MUST be kept on their newest releases, so no package trails the current major of the library it names; the editor version the extension declares it requires MUST NOT sit below the API types it compiles against; and an upgrade that can change behaviour MUST be proven against the recorded analysis baseline — accepted when the baseline is reproduced, reported when any recorded count moves — rather than assumed harmless.

**FR-032**: The coverage regression guard MUST record where the test suite actually stands after this change rather than a figure carried over from before it, and any lowering of a threshold MUST be a recorded decision carrying its reason, never a silent edit.

**State this change already meets FR-031 by.** An upgrade of the whole toolchain has been performed and verified. Every package moved to its latest release, and the declared editor floor moved with the API types it compiles against instead of being left behind them. In plain terms:

- the editor floor was raised from `1.90` to `1.146`, together with the editor API types it compiles against;
- the test and coverage tooling moved from major 3 to major 5;
- the XAML parser moved from major 10 to major 12 — a different module format — and is now bundled into the extension rather than loaded at run time;
- the compiler was replaced by its faster native release, TypeScript 5.9 giving way to 7.0;
- the two configuration options those majors removed were replaced with their current equivalents: coverage's `all` option is now expressed through the `include` list, and the compiler's `CommonJS` / `node` module pair became `preserve` / `bundler`.

Measured afterwards, against the baseline recorded before it: the same 1095 nodes, 4776 edges and 21 findings; 39 test files and 1028 tests passing; the self-test green.

**State this change already meets FR-032 by.** The coverage guard now states the suite's real position — branches 90 and functions 98, against 93 and 100 before — and says in the guard itself why they moved: the validation and compatibility code arrived with less coverage than the analysis modules, so the guard records where the suite is rather than hiding the gap behind a figure from before this change.

---

### Key Entities

- **Repository profile**: the conventions one workspace folder declares. Reviewable, committed with the code, and the only place a repository's conventions live. Holds layer rules, link rules, findings rules and file selection, and says what repository it describes.
- **Layer rule**: a naming or namespace signal and the layer it places a type in, together with its precedence against the other rules.
- **Link rule**: a code or markup pattern that creates a dependency the graph draws.
- **Findings rule**: a declaration that changes what the findings report counts or reports — entry points, transparent base types, generated members.
- **File selection**: the patterns that decide which files are read and which are excluded.
- **Expected-answer record**: what one validated checkout's run is measured against — the types in its scope, the layer each belongs to, the view-to-view-model pairs it states, and the findings true of it. Produced from the extension's supported inspection surface for that checkout, and reviewable and correctable like code; produced independently of the run it judges, so it cannot merely echo that run's output.
- **Inspection surface**: the extension's supported way of reading a workspace folder and reporting the structural and convention evidence a profile author and an expected-answer record both need — the same evidence the profile questions are drawn from. Runs without an editor, reads a folder without changing it, and serves a person or an assistant working unattended.
- **Validation result**: the record of one checkout's run — the profile used, the measurements taken, every mismatch found, and whether the 100% bar was met.
- **Compatibility verdict**: the extension's up-front decision on whether it can analyse a given repository at all, together with the specific structures that decided it — the languages and project kinds present, the files it reads, and whether the repository is of a kind its analysis understands.
- **Extension update request**: the file written under `extensions/csharp-xaml-graph/update_prompts/{requestingRepo}/` when a person accepts the offer to make the extension compatible with their repository — every required extension change, the repository evidence justifying each one, and the command that runs it.

---

### Success Criteria › Measurable Outcomes

**SC-001**: A developer adopting the tool in a repository whose conventions the extension has never seen reaches a committed profile the graph honours in 10 minutes or less, without editing extension source.

**SC-002**: Adopting a repository's conventions requires zero edits to extension source, measured as an empty diff outside the profile for that adoption.

**SC-003**: With no profile present, the graph's output for a given repository is identical to the build before this change: the same node count, edge count and finding count.

**SC-004**: Every malformed or partial profile tried still opens the graph, and each one names the part it could not read.

**SC-005**: After a profile is imported or refreshed, the visible graph reflects it in the same session, with no editor reload.

**SC-006**: A repository that gained one new convention after initialization is brought back into agreement by a single refresh: the assistant's proposal covers it in one pass.

**SC-007**: The profile is reviewable as text: a changed profile appears in a normal diff line by line, with no opaque content.

**SC-008**: Four of the four named checkouts reach 100% accuracy against their own expected-answer records, measured as zero mismatched items under each condition the bar names.

**SC-009**: Repeating a validated checkout's run — once from a fresh assistant and once from its recorded profile — reproduces identical measurements: the same node, edge, finding and mismatch counts, so no pass depends on the run.

**SC-010**: Validating this repository leaves its own numbers exactly as the baseline recorded before this change: the same node, edge and finding counts, so the profile added for it corrects nothing it already reported.

**SC-011**: For every validated checkout, an assistant with no prior context, given only the extension's create prompt and that checkout's evidence, produces a first reply that imports with zero problems — no follow-up correction and no unspecified key — which proves the contract was stated explicitly.

**SC-012**: For every validated checkout, the compatibility verdict is issued before any convention question is asked, measured as the verdict preceding the first question in every run.

**SC-013**: An incompatible repository yields a request file that names every required extension change with the repository evidence for each, and states the exact command `/speckit-companion-specify`; a declined offer yields no file at all while the incompatibility reason remains visible.

**SC-014**: For every validated checkout, the evidence a profile author needs comes from the supported inspection surface, and every expected-answer record in the validation set is produced by that surface, measured as zero records whose provenance is anything else.

**SC-015**: No validation task leans on an unrecorded reading of a repository: every author and every expected-answer record draws its evidence from the supported inspection surface, and where someone added tooling of their own because the surface did not report what that repository needed, the addition and the reason are recorded — so the only scripts outside the extension are stated exceptions rather than private ones.

**SC-016**: An author who needs evidence the supplied inspection does not report can add tooling of their own and says why, measured as: the addition is recorded with the evidence it was needed for, and the profile it helped produce still imports with zero problems and still faces the same comparison as any other.

**SC-017**: No package trails its latest release: every dependency and every development tool the extension declares is on its newest release, measured as zero packages one or more majors behind the version their ecosystem publishes.

**SC-018**: The editor version the extension declares it requires is not below the API types it compiles against, measured as the declared floor matching the API type version in use.

**SC-019**: The upgrade leaves the recorded analysis baseline untouched: the same 1095 nodes, 4776 edges and 21 findings as before it, and the same numbers again on a second run.

**SC-020**: The full gate set passes on the upgraded toolchain: 39 test files and 1028 tests green, the self-test green, and the coverage report meeting a guard that states the suite's actual position.

**SC-021**: Every threshold in the coverage regression guard is the suite's own current figure, and each one that sits below where it previously stood carries the reason it moved beside it, measured as zero thresholds that changed without a stated decision.

---

### Non-goals

- A separate profile per project inside one workspace folder; the profile's scope is the workspace folder.
- A shared or published registry of profiles, or profiles shipped with the extension.
- Generating conventions without an assistant, or letting the product guess a profile on its own; there is still no heuristic auto-detector. What the extension does supply is inspection: scripts that read a workspace folder and report the structural and convention evidence a profile author needs, so an author never writes its own throwaway parser to understand a repository. That inspection reports evidence and does not decide conventions. An author may still add tooling of its own where a repository needs something the supplied inspection does not cover, and says why.
- Changes to how the graph looks, how it is laid out, or what it draws beyond what the profile corrects.
- Languages other than C# and XAML.
- Per-person profiles; the profile is shared through the repository.
- Editing the profile through a settings form rather than as a file.
- Shipping profiles for arbitrary repositories: only the four named checkouts gain one, and the three outside this repository are inputs to validation whose working trees may gain nothing beyond a single profile file each.
- Correcting a validated checkout's own code to match what a run expects; a mismatch is reported and that checkout is left as it was.

---

### Change-level rules

- The profile is plain text, committed with the code, and reviewed like code.
- Reading and reviewing a profile must not require a new committed dependency.
- Behaviour with no profile is preserved exactly.
- No secret, credential or machine-specific path may be written into a profile.
- An upgrade is not finished until the recorded baseline and the full gate set pass unchanged; a behaviour change it carries is proven, not presumed.

---

### Verbatim Constraints

These strings are pinned: they must appear exactly as written wherever they are used, with the same spelling, case, punctuation and separator, and are not to be paraphrased, re-cased or reformatted.

- The folder `extensions/csharp-xaml-graph/update_prompts/`.
- The placeholder `{requestingRepo}`, standing for the name of the repository that requested the change.
- The literal `/speckit-companion-specify`.

---

### Verification plan

1. On the branch holding the previous build, analyse this repository and record the node, edge and finding counts. On the new build with no profile, repeat; the counts must match.
2. Hand-write a profile declaring a naming convention the built-in set does not contain; confirm the graph classifies by it.
3. From a repository with no profile, run initialization, answer the chat, import, commit the profile, and time the run end to end.
4. Break the profile three ways — a malformed section, something unrecognised, and a pattern matching nothing — and confirm the graph opens each time and names the problem.
5. Introduce a naming convention after initialization, press the refresh control, import, and confirm the graph changes without a reload.
6. Run the extension's existing test suite; nothing that passed before may fail.
7. For each of the four named checkouts, produce its expected-answer record from the extension's supported inspection surface, then hand only the setup prompt and that checkout to an assistant with no prior context, import the profile it produces, and run the comparison; all four must come out at 100%.
8. Repeat each checkout's run a second time from its recorded profile and confirm the measurements are identical.
9. On this repository, compare the counts taken under the profile created for it with the baseline recorded before this change; they must be identical.
10. Remove one type from an expected-answer record on purpose and confirm the comparison reports that item instead of passing.
11. Give a fresh assistant only the create prompt and a validated checkout's evidence, and confirm its first reply imports with zero problems and contains no key the prompt did not describe.
12. Point the extension at a repository it cannot analyse, confirm the verdict comes before any convention question and names the specific structures, confirm nothing is created before the offer is answered, then answer yes and inspect the request file's changes, evidence and stated command; repeat answering no and confirm nothing is written and the reason stays visible.
13. Take every package to its newest release, raise the declared editor floor to the API types in use, and run the whole gate set; the analysis must report the counts recorded before the upgrade unchanged, and each threshold in the coverage guard must be the suite's current figure with the reason it sits there written beside it.

---

### Assumptions

- The profile lives at one well-known place in the workspace folder, named so its purpose is obvious, and is committed with the repository. The exact name is a plan decision; nothing above depends on it.
- The assistant's reply reaches the extension by the developer confirming or pasting it. The mechanism is a plan decision, not a requirement here.
- An explicit file pattern configured by the person wins over the profile's file selection, because that setting is the existing escape hatch when a profile is wrong.
- The profile created by initialization starts from today's built-in conventions and records the repository's answers as corrections to them, so a half-answered profile is still a working one.
- The questions the setup prompt asks are grouped by what the profile declares: layers, links, findings and files.
- Entry-point, transparent-base and generated-member knowledge moves into the profile, and the built-in values remain the fallback so no repository is read worse than before.
- Each expected-answer record is produced from the extension's supported inspection surface for that checkout, and is committed and reviewable beside the evidence for the spec. A record derived from the run it judges would prove nothing, so it is produced independently of the extension's analysis output — and from the same supported surface the profile authors use, so a record and a profile cannot rest on two private readings of one repository.
- The four named checkouts are the validation set for this change. Adding a fifth is a later change and would extend the bar rather than meet it.
- The packages the extension depends on will keep releasing majors, and the baseline comparison rather than a version number is what decides whether a given upgrade is safe here.
- An inspection is different from analysis: it reports the evidence a profile is written from, and does not itself decide a layer, a link or a finding.
