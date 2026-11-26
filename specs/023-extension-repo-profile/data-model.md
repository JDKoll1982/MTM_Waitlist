# Phase 1 Data Model — Repository Profiles

Five things are stored. The profile holds a repository's conventions, and it is the only one written
into a checkout. The expected-answer record holds what one checkout's run is judged against, the
validation result holds what the run measured and how it was judged, and the attempts record holds what
each check of the assistant's reply found; all three live with the spec. The update request is written
only when a repository the extension cannot analyse asks for the extension to be changed, and it lives
beside the extension's source. Everything else here is derived at analysis time and never written down.
`contracts/profile.schema.json`, `contracts/expected-record.schema.json`,
`contracts/validation-result.schema.json` and `contracts/compatibility.schema.json` carry these shapes
in the form a consumer reads; this document carries the meaning and the rules.

## RepositoryProfile

One per workspace folder, stored at `<workspace folder>/csharp-xaml-graph.profile.json`, committed with
the code, reviewed like code. Scope is the folder, never the project inside it (FR-001, non-goals).

| Field | Type | Required | Meaning |
|---|---|---|---|
| `version` | integer | yes | Format version. Absent or not `1` makes the reply unusable. |
| `repository` | string | no | Which repository this describes. Informational; never used to match or gate. |
| `layers` | Layers | no | Layer classification (FR-002). Absent falls back to the built-in rules. |
| `links` | Links | no | Link detection (FR-003). |
| `findings` | Findings | no | Findings rules (FR-004). |
| `files` | Files | no | File selection (FR-005). |

Partial-friendly: every section and every key inside it is optional, an absent key keeps the built-in
value, and a present array replaces the built-in array rather than extending it. An empty file is
treated as absent, not as a parse failure.

## Layers

Classification is an ordered list where the first rule that matches decides, and a default for the
type no rule matches.

| Field | Type | Meaning |
|---|---|---|
| `rules` | LayerRule[] | In precedence order. Order is the precedence FR-002 asks for. |
| `default` | `view` \| `viewmodel` \| `service` \| `model` | Layer for a type no rule matches. Defaults to `model`. |

### LayerRule

| Field | Type | Meaning |
|---|---|---|
| `layer` | `view` \| `viewmodel` \| `service` \| `model` | Layer assigned when the rule matches. |
| `kinds` | string[] | Declaration kinds the rule applies to: `class`, `interface`, `enum`, `record`, `struct`. Absent means any. |
| `nameSuffixes` | string[] | The type name ends with one of these. |
| `namePrefixes` | string[] | The type name starts with one of these. |
| `namespaceSegments` | string[] | The namespace has a segment exactly equal to one of these. Whole-segment equality, because a substring test for `Models` also matches `ViewModels`. |
| `unlessNameSuffixes` | string[] | The rule does not apply when the name ends with one of these. This is how a row model beside the view models stays a model. |

A rule matches when every condition it states holds. The built-in default is the current decision order
written as rules: interface service names, then interfaces as models, then view-model names, then the
`ViewModels` namespace for classes that do not read as data, then enums as models, then service names
or the `Services` namespace.

## Links

Flat name lists, each mapped to one existing branch of the analysis, so a declaration names a call
rather than describing a shape.

| Field | Type | Built-in value | Effect |
|---|---|---|---|
| `genericPairCalls` | string[] | `Configure` | `x.Call<TA, TB>()` draws a `resolves` edge from the second type to the first and registers both. |
| `typeofPairCalls` | string[] | `RegisterRoute` | `x.Call(typeof(TA), typeof(TB))` draws a `resolves` edge and registers the first. |
| `dependencyPropertyCall` | string | `Register` | `x.Call(nameof(P), typeof(TValue), typeof(TOwner))` draws a `getservice` edge when the value type is a view model, and registers it. |
| `addCallPattern` | string | `^Add[A-Z]` | A method matching this registers its single type argument, or pairs an implementation with the interface it serves when given two. |
| `serviceLocator` | string[] | `GetService`, `GetRequiredService` | Recognised as a container lookup rather than a plain call. |
| `dataContextCasts` | string[] | `is`, `as` | A cast of the data context to an interface is a binding edge. Class-typed casts stay handled by the typed-property rule. |

The view-model test that gates the dependency-property edge is the `viewmodel` layer rule, not a
suffix check of its own, so a repository that calls its view models something else keeps that edge.

## Findings

| Field | Type | Built-in value | Effect |
|---|---|---|---|
| `entryPoints` | string[] | `App`, `Program`, `Startup`, `Main` | A type with one of these names is never reported unused. |
| `transparentBases` | string[] | the 21 names currently in `TRANSPARENT_BASES` | A base type here adds no bindable members, so a missing member is genuinely missing. |
| `generatedMembers` | GeneratedMembersRule[] | `[RelayCommand]` on a method, `[ObservableProperty]` on a field | Members the compiler generates, so a binding to one is not a dead binding. |

### GeneratedMembersRule

| Field | Type | Meaning |
|---|---|---|
| `attribute` | string | Attribute name without brackets. |
| `on` | `method` \| `field` | Which declaration carries the attribute. |
| `form` | `commandSuffix` \| `pascalCaseWithoutPrefix` | How the generated member is named. `commandSuffix` yields `NameCommand` and, for a name ending in `Async`, `StemCommand`. `pascalCaseWithoutPrefix` strips leading underscores and upper-cases the first letter. |

`form` is a closed set of two, matching the two generators the analysis already understands. A profile
cannot invent a third naming shape; it can move the attributes it applies to, which is what a different
toolkit needs.

## Files

| Field | Type | Built-in value | Effect |
|---|---|---|---|
| `include` | string[] | `**/*.cs`, `**/*.xaml` | Glob patterns, relative to the workspace folder, describing the files to read. |
| `exclude` | string[] | the current `DEFAULT_EXCLUDE` list | Patterns excluded from the read. |

An explicitly configured `csharpXamlGraph.include` or `csharpXamlGraph.exclude` wins over this section,
pattern by pattern, because that setting is the existing escape hatch when a profile is wrong (FR-015).
A selection that matches nothing is reported rather than drawn as an empty canvas.

## Profile contract

Derived from the profile's format and never stored. It is two things at once: the set of keys the
parser accepts, and the description every create and refresh prompt carries. One row per key, grouped by
the part of the file that holds it.

| Field | Type | Meaning |
|---|---|---|
| `path` | string | Where the key sits, in the dotted form the problems already use: `layers.rules[].layer`. |
| `key` | string | The key's exact spelling, case included. It is the string a reply must contain. |
| `section` | string | The part of the file that holds it, so the prompt can render one group at a time. |
| `shape` | string | The value in words: `text`, `a list of text`, `one of view, viewmodel, service, model`, `a list of layer rules`, `a regular expression`, `the number 1`. |
| `required` | boolean | True for `version` alone. |
| `meaning` | string | One sentence: what the key declares. |
| `absent` | string | One sentence: what the extension does when the key is absent. |
| `values` | string[] | Optional. The closed set, when the shape is a choice. |
| `fields` | ProfileField[] | Optional. The keys of a nested object, or of the items of a list, so one table reaches every depth. |

Three rules make it a contract rather than a description:

- **Every key the parser accepts is a row.** `reportUnknownKeys` takes its list from the rows of one
  section, so the accepted set and the described set are the same list.
- **Every row is rendered in every prompt, before the questions.** A key the prompt omits is a key the
  assistant would have to invent, which FR-024 forbids. Create and refresh render the same table.
- **No row describes a key the parser refuses.** A row that names a key the reader skips is a promise
  the extension does not keep, so each row is exercised against the parser.

## StructureFacts

Derived per repository before any analysis, by one path walk that never opens a file. These are the
observations FR-025 names: which languages are present, whether the files the extension reads are there,
and whether the repository is of a kind its analysis understands.

| Field | Type | Meaning |
|---|---|---|
| `csharpFiles` | number | Files ending `.cs`, under the built-in selection. |
| `xamlFiles` | number | Files ending `.xaml`, under the built-in selection. |
| `projectFiles` | string[] | Repository-relative paths ending `.csproj`, `.fsproj`, `.vbproj` or `.vcxproj`, sorted. |
| `solutionFiles` | string[] | Repository-relative paths ending `.sln`, sorted. |
| `truncated` | boolean | True when the walk stopped at the cap, so the counts are a floor rather than a total. |

A project file is a fact, never a verdict on its own: a folder of loose `.cs` files analyses correctly,
so its absence cannot be a refusal. Only an extension the analysis does not read at all — `.fsproj`,
`.vbproj`, `.vcxproj` — is evidence against compatibility.

## IncompatibilityReason

| Field | Type | Meaning |
|---|---|---|
| `code` | `no-csharp` \| `no-xaml` \| `over-analysis-cap` \| `unsupported-project` | Which observation failed. |
| `structure` | string | The thing that cannot be handled, in the reader's words: `no C# files`, or `src/Other.fsproj`. |
| `message` | string | One plain sentence naming the counts where the counts are the evidence. |
| `evidence` | string[] | Counts and up to three repository-relative paths that justify the reason. Never empty. |

## CompatibilityVerdict

| Field | Type | Meaning |
|---|---|---|
| `schemaVersion` | integer | `1`, for the recorded form. |
| `repository` | string | The slug, matching that checkout's record. |
| `verdict` | `compatible` \| `incompatible` \| `unknown` | The per-repository answer FR-025 asks for. |
| `reasons` | IncompatibilityReason[] | Empty for `compatible`; every failed observation for `incompatible`; one note for `unknown`. |
| `facts` | StructureFacts | What the walk saw, so a recorded verdict can be re-read without the repository. |
| `limits` | `{ analysisFileCap: number }` | The cap the counts were compared against. |

`unknown` is a third answer, not a soft `compatible`: it means the structure could not be read at all,
which is a remote window or a folder the process cannot list. Profile work then proceeds exactly as it
does today, and one note says the structure was not checked. `incompatible` requires at least one
reason, because a refusal that names nothing is the general failure FR-026 forbids. A verdict reads and
writes nothing in the repository it describes.

## ProfileProblem

Derived, never stored. One per thing the reader could not use.

| Field | Type | Meaning |
|---|---|---|
| `path` | string | Where in the profile, in dotted form with array indices, e.g. `layers.rules[2].kinds`. |
| `message` | string | One plain sentence: what was expected and what was found. |
| `severity` | `warning` \| `error` | `error` means the whole file was unusable and the built-in rules applied; `warning` means one declaration was skipped. |

Problems reach the reader through the model's existing `warnings` array.

## CompiledConventions

Derived per analysis run from the profile and the built-in defaults; the only thing the analysis
modules see. Holds the resolved layer rules and default, the link name sets, the entry-point and
transparent-base sets, the generated-member rules, and the file selection. Built once per run so a
1000-file analysis does not re-resolve a profile 1000 times. `src/graph/conventions.ts` is its home and
it imports nothing from `vscode`.

## ProfileEvidence

Derived per analysis run, used only to build the two prompts. Counts and examples, never file contents:

- Name suffixes and prefixes on declared types, with two example type names each.
- Namespace segments, with a count of types carrying each.
- Call method names with their type-argument counts, with one example call site.
- Base types declared in the repository.
- Attributes seen on methods and fields.
- XAML root tags, and how many files carry each.
- File counts: C# files and XAML files read.

A question in the setup prompt that has no evidence behind it is not asked, so a repository with
nothing to profile says so instead of writing an empty profile.

## The profile's lifecycle

```text
Absent ──import a partial reply──▶ Partial ──import the rest──▶ Valid
   │                                                              │
   └──hand-write with a mistake──▶ Malformed ──correct or import──┘
                                                                  │
                                                    code gains a convention
                                                                  ▼
                                                                Stale
                                                                  │
                                                     refresh then import
                                                                  ▼
                                                                Valid
```

Every state keeps the graph open. `Absent` behaves exactly as the previous build and offers creation
once per session, dismissibly. `Malformed` applies what it can and names what it could not read.
`Stale` is the state the toolbar control exists to leave.

One state precedes all of them, and it is per repository rather than per profile file:

```text
Unchecked ──structure read──▶ Compatible ──▶ the lifecycle above
    │
    ├──structure could not be read──▶ Unknown ──▶ the lifecycle above, with one note
    │
    └──a named structure cannot be handled──▶ Incompatible
                                                    │
                                    ┌─ yes ──▶ Request written, reason still shown
                                    └─ no or dismissed ──▶ nothing written, reason still shown
```

`Incompatible` never opens a prompt and never asks a convention question, because there is no profile
worth creating for a repository the analysis cannot read. The reason shown in that state is the same
sentence the request file carries, and it stays on the model for the rest of the session.

## ExpectedAnswerRecord

One per validated checkout, stored at `specs/023-extension-repo-profile/expected/<slug>.json`, committed
before the first run against that checkout. It is written by reading the checkout's code, never from the
run it judges (FR-017).

| Field | Type | Required | Meaning |
|---|---|---|---|
| `schemaVersion` | integer | yes | Format version, `1`. |
| `repository` | string | yes | The checkout's name. It identifies the checkout without a path, so the file stays portable. |
| `source` | string | yes | One sentence saying how and when the record was read out of the code. |
| `types` | ExpectedType[] | yes | Every declared type inside the checkout's analysis scope. |
| `links` | ExpectedLink[] | yes | Every view-to-view-model pair the checkout states. |
| `findings` | ExpectedFinding[] | yes | Every finding known to be true of the checkout. |

Rules the comparison enforces:

- **Complete over the scope.** A declared type the run produces and the record lacks is a mismatch, and
  so is a recorded type the run does not produce. An omission therefore cannot be scored as accuracy.
- **One entry per type**, keyed by the identifier the model uses, `Namespace.Type`. That is the string a
  mismatch prints, so no alias table sits between a failure and the type it names.
- **An empty array is a claim**, meaning "none", and is checked like any other.
- **No absolute path, machine name, timestamp or run output.** Files are checkout-relative. The record is
  reviewable against the code, and cannot quietly embed what the run said.

### ExpectedType

| Field | Type | Meaning |
|---|---|---|
| `name` | string | The type's full name, matching the node id the graph emits. |
| `file` | string | Checkout-relative path the declaration was read from, so a reviewer can open it. |
| `layer` | `view` \| `viewmodel` \| `service` \| `model` | The layer the checkout's own conventions give this type. |

### ExpectedLink

| Field | Type | Meaning |
|---|---|---|
| `view` | string | Full name of the view. |
| `viewModel` | string | Full name of the view model that view binds. |

### ExpectedFinding

| Field | Type | Meaning |
|---|---|---|
| `kind` | `cycle` \| `dead-binding` \| `unbound-view` \| `unused-type` \| `unused-member` | The finding kind. |
| `types` | string[] | The full names the finding is about, sorted, so a match does not depend on a title's wording. |

## Measurement

Derived from one run and never stored on its own. It is the part two runs have to agree on (SC-009).

| Field | Meaning |
|---|---|
| `nodes`, `edges`, `findings` | The three counts the baseline records. |
| `byLayer`, `byEdgeKind`, `byFindingKind` | Counts per label, so a shift in one category is visible. |
| `types` | `[name, layer]` pairs, sorted. |
| `links` | `[view, viewModel]` pairs, sorted. |

No timestamp, no duration, and nothing that depends on directory-walk order, so two runs over the same
code with equivalent profiles produce the same measurement byte for byte.

## Mismatch

One row per failing item. This is the unit FR-020 insists on: the outcome is a list, never a count.

| Field | Type | Meaning |
|---|---|---|
| `item` | `type` \| `link` \| `finding` | What kind of expectation was not met. |
| `name` | string | The type the row is about: the type id for a type, the view for a link, the first named type for a finding. |
| `expected` | string | What the record says: a layer, a view model, "no finding", or a finding kind. |
| `actual` | string or null | What the run produced, or null when it produced nothing. |

An unexpected type reads `expected: "not in the record"` and `actual: "<layer>"`. A recorded type the run
does not produce reads `actual: null`. A missing link reads `actual: null`; an invented one reads
`expected: "no link"`.

## ValidationResult

Stored at `specs/023-extension-repo-profile/results/<slug>.json` (FR-021).

| Field | Type | Meaning |
|---|---|---|
| `schemaVersion` | integer | `1`. |
| `repository` | string | The slug, matching the record's. |
| `profile` | `{ sha256: string, problems: ProfileProblem[] }` | Which profile was used, and what it could not read. |
| `measurements` | Measurement | What the run counted. |
| `conditions` | object | Five entries, `layers`, `links`, `findings`, `import`, `invariants`, each `{ pass, detail, mismatches? }`. |
| `verdict` | `pass` \| `fail` | `pass` only when all five conditions pass, so a partial success cannot be recorded (FR-022). |

Two rules about what a result does not contain. It carries no timestamp, so two identical runs produce
identical files. It carries no absolute checkout path, so it stays portable and satisfies the
change-level rule against machine-specific paths; `validation.md` names the machine path once as prose.

`profile.sha256` is recorded but is not part of the determinism comparison, because SC-009 compares a run
from a fresh assistant's profile with a run from the recorded profile, and two textually different
profiles can express the same conventions. The measurement and the mismatch list are what must match.

## UpdateRequest

Written only when the person answers yes to the offer, and never in the requesting repository. It is
the record of what the extension lacks, in the form a specification can be run from.

| Field | Type | Meaning |
|---|---|---|
| `repository` | string | The slug of the requesting repository. It is also the folder name. |
| `path` | string | Where the file goes, relative to the extension root: `update_prompts/<slug>/request.md`. |
| `reasons` | IncompatibilityReason[] | The verdict's reasons, verbatim and in its order. |
| `changes` | RequiredChange[] | One or more per reason, so every refusal names what would fix it. |
| `command` | string | The literal `/speckit-companion-specify`. |

### RequiredChange

| Field | Type | Meaning |
|---|---|---|
| `title` | string | What must change in the extension, in one line. |
| `because` | string | The reason code this change answers. |
| `change` | string | One sentence saying what the extension would do differently. |
| `evidence` | string[] | The repository's own facts: counts and repository-relative paths. Never empty. |

Rules the writer enforces: one section per reason, one subsection per change, every change carrying at
least one evidence line, the command stated verbatim directly under the heading, and a closing line
saying nothing in the requesting repository was created or changed. The file's format is
`contracts/update-request.md`; the folder is `{requestingRepo}` and the file inside it is `request.md`,
so answering yes twice leaves one current request rather than a stack.

## Import attempts

`specs/023-extension-repo-profile/results/<slug>.attempts.json` records every check of the assistant's
reply, so the first one can be told apart from a correction (SC-011).

| Field | Type | Meaning |
|---|---|---|
| `schemaVersion` | integer | `1`. |
| `repository` | string | The slug. |
| `attempts` | ImportAttempt[] | One entry per recorded check, in the order they happened. |

### ImportAttempt

| Field | Type | Meaning |
|---|---|---|
| `sha256` | string | A hash of the reply's text, so two different replies are visibly different. |
| `problems` | ProfileProblem[] | What the import refused or warned about. Empty is the claim "clean". |

The `import` condition of a result passes only when the attempts record holds exactly one entry and that
entry is clean, and the result's `import` detail states the attempt count. Deleting the file starts a
fresh first attempt, and that deletion is visible in git.

## The validation lifecycle

```text
Bare ──--prompt, then a fresh assistant──▶ Drafted ──--check accepts──▶ Accepted ──--record──▶ Measured
                                             │                                                  │
                                             └──--check names problems──▶ Refused              ├──▶ Passed
                                                                             │                  └──▶ Failed
                                                                             └──correct and re-check
```

`Bare` is a checkout with its record committed and no profile. `Refused` writes nothing and measures
nothing. `Passed` requires all five conditions, and `Failed` names each item that broke one. A repeat run
from `Accepted` produces a second result file rather than a new lifecycle, and the two are compared.
