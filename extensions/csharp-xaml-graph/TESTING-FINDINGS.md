# C#/XAML Graph — Test Findings

Last Updated: 2026-11-25
Extension version tested: 0.1.6
**Status: all findings below are fixed in 0.2.0** — see *Resolution* under each entry.
Tested on: Windows, Node 24.18.0, TypeScript 5.9.3, `web-tree-sitter` 0.27.2

## What was tested

| Test | Result |
| --- | --- |
| `npx tsc --noEmit` | Pass |
| `npm run build` (extension, webview, selftest bundles, grammar copy) | Pass |
| `npm run selftest` against MTM_Waitlist | Pass, 60+ checks, `ALL CHECKS PASSED` |
| Analysis run against 5 other repositories (see matrix) | No crashes, but 6 defects reproduce on real code |
| 17 synthetic C# constructs (primary constructors, nested types, records, raw strings, preprocessor, tuple/generic/nullable parameters, default interface members, extension methods, malformed input) | Parser handled every case; malformed input flagged, never threw |
| 8 synthetic XAML cases (x:Bind, DataTemplate with x:DataType, nested templates, comments, CDATA-style literals, entity encoding, no `x:Class`, unclosed tag) | All parsed; unclosed tag did not throw |
| Empty workspace, XAML-only workspace, top-level-statements file, BOM + CRLF file, deeply nested generics | No crash; line numbers correct with BOM and CRLF |
| Packaged `.vsix` contents | Correct: grammar, `web-tree-sitter` runtime, bundles, stylesheet, icon all present; sources excluded |

### Repository matrix

| Repository | Files | Nodes | Edges | Findings | Default canvas (views + view models, unconnected hidden) |
| --- | --- | --- | --- | --- | --- |
| MTM_Waitlist (self test) | — | 1,825 | — | — | 139 nodes |
| MTM_Receiving_Application | 1,217 cs, 142 xaml | 1,382 | 7,310 | 208 | 86 nodes, 25 edges of 1,148 modelled |
| MTM_Loader_WinUI3 | 342 cs, 26 xaml | 516 | 1,994 | 123 | 38 nodes, 44 edges |
| MTM_WIP | 262 cs, 28 xaml | 431 | 1,170 | 62 | **2 nodes, 0 edges** |
| MTM_WIP_Application_WinForms | 342 cs, 0 xaml | 334 | 1,444 | 33 | 0 nodes (no XAML) |
| MTM_Application_Loader_WinForms | 26 cs, 0 xaml | 28 | 92 | 2 | 0 nodes (no XAML) |

Node and edge counts in the matrix include test projects; the default-canvas column uses the
extension's own defaults, which exclude `*.Tests` folders (F4's figures for MTM_Receiving_Application
are from that same exclusion, hence 1,148 nodes rather than 1,382).

---

## Summary

| Id | Severity | Area | Finding |
| --- | --- | --- | --- |
| F1 | Critical | Analysis | Methods, type mentions and `typeof` references declared in a partial file after the first are discarded |
| F2 | High | Analysis | Evidence lines for merged partial members point at the wrong file |
| F3 | High | Findings | `dead-binding` only inspects the view's properties, so fields, function bindings and generated members are false positives |
| F4 | High | Findings | Layer classification only understands the `*ViewModel` suffix, so the default canvas is empty on other conventions |
| F5 | Medium | Findings | Cycles are dominated by low-confidence reference edges and collapse into one unusable 99-type finding |
| F6 | Medium | Findings | A `Main` entry point is reported as an unused private member |
| F7 | Medium | Parser | WPF/MAUI `{Binding}` markup is not read at all, so non-WinUI repositories get no bindings |
| F8 | Medium | Host | Multi-root workspaces analyse one root but read every root |
| F9 | Low | Host | Export defaults to `~/.vscode/extensions/dependency-graph.json` |
| F10 | Low | Analysis | Module bucket is the first path segment, so `src/`-layout repositories have one module |
| F11 | Low | Parser | Binding line numbers point at the element start, not at the attribute |
| F12 | Low | Host | The main `show` command always forces a full re-analysis and silently drops concurrent refreshes |
| F13 | Low | Webview | Node-cap truncation is never surfaced on the canvas |
| F14 | Low | Host | The 20,000-file `findFiles` cap truncates a large workspace silently |
| F15 | Low | Test harness | `selftest` is hard-wired to the MTM_Waitlist layout, so it cannot validate another repository |

## Resolution summary (0.2.0)

| Id | Fix |
| --- | --- |
| F1 | `mergeDeclarations` merges `methods`, `mentions` and `typeofRefs`; every member list is now copied and pushed. |
| F2 | Every parsed record carries its origin `file`; edge evidence and `unused-member` locations use it. Proven by the `every location lies inside the file it names` invariant. |
| F3 | The binding root is judged against `memberNamesOf` (properties, fields, methods, generated members, inherited), and the rest of the path is walked segment by segment. |
| F4 | `classifyType` recognises suffix and prefix naming, `ViewModels`/`Services`/`Contracts` segments, service prefixes, and exact segment matching instead of substring. |
| F5 | Low-confidence references are excluded from the structural strongly-connected pass; a second pass reports the remaining reference knots as one low-severity finding with a member sample. |
| F6 | `Main` is skipped when static or declared on `Program`; markup handlers and bound names also keep members alive. |
| F7 | `{Binding Path=…}`, `{Binding …}` and `{TemplateBinding}` are parsed and counted; they are documented as never judged dead. |
| F8 | File discovery is scoped to the analysed folder, and a multi-root workspace warns once, naming the folder used. |
| F9 | Export defaults beside the workspace; the extensions directory is only a fallback when no folder is open. |
| F10 | `moduleOf` skips container segments (`src`, `lib`, `app`, `tests`, …). |
| F11 | Binding lines come from the attribute's own offset inside the open tag. |
| F12 | `show` reuses the cached model; `Refresh` re-reads; a dropped refresh reports through the status bar. |
| F13 | The canvas status line says `TRUNCATED to N … raise maxNodes`. |
| F14 | Hitting the file cap warns and points at `include`/`exclude`. |
| F15 | `node out/selftest.js <path>` validates any repository: four generic invariants plus five synthetic fixtures run against any root, and repository-specific assertions are skipped. |

Verified after the fixes: `npm run selftest` passes on MTM_Waitlist, and the generic harness passes on
MTM_Receiving_Application, MTM_Loader_WinUI3, MTM_WIP, MTM_WIP_Application_WinForms and
MTM_Application_Loader_WinForms. The `every location lies inside the file it names` invariant found one
further real defect during that run (`unused-member` evidence using the type's first partial file),
which is fixed with F2.

### Independent re-verification (0.3.0, 2026-11-25)

Re-checked from scratch against the source and re-measured with the original harness. All 15
findings are genuinely fixed; the table above is not taking the code at its word.

| Check | Before | After |
| --- | --- | --- |
| `npm run build` (tsc + esbuild) | Pass | Pass |
| `npm test` (Vitest) | no suite existed | **637 passed**, 26 files |
| `node out/selftest.js "<other repo>"` | impossible (F15) | `ALL CHECKS PASSED` |
| Out-of-range evidence lines (3 repos) | 9 / 10 / 11 | **0 / 0 / 0** |
| `dead-binding` on MTM_Receiving_Application | 9 (8 false) | **0** |
| `ViewModel_*` classified `model` | 182 | **0** (all 118 → `viewmodel`) |
| Opening canvas, MTM_Receiving | 86 nodes / 25 edges | **241 nodes / 269 edges** |
| Opening canvas, MTM_WIP | 2 nodes / 0 edges | **6 nodes / 3 edges** |
| Largest cycle | 99 nodes, medium | **30 nodes, low** |
| `Program.Main` flagged unused | 2 repos | **0** |
| `{Binding}` / `{TemplateBinding}` parsed | 0 of 3 dialects | **3 of 3** |
| Module buckets, `src/`-layout repo | 2 | **10–12** |
| Synthetic partial: `SaveCommand` | reported dead | resolves |
| Synthetic partial: `Holder` evidence | `src/A.cs:15` (wrong file) | `src/B.cs:7` (correct) |

Two behaviours improved but stay data-limited, and neither is an open finding:

- **A 30-type cycle remains** on MTM_Receiving and MTM_WIP_Application_WinForms. It shrank from 99
  and 115, is now `severity=low`, and is built from medium-confidence references plus construction
  edges, so it reads as a mutual dependency rather than a build blocker. A genuinely large
  strongly-connected group still exists in those code bases.
- **MTM_WIP's opening canvas is 6 of 291 nodes.** All 20 `ViewModel_*` types now classify correctly,
  but only 2 of its 28 views carry any switchable edge — the repository has one `x:Bind` and no
  `pageService.Configure` call, so the default filter has nothing to draw.

One count rose as a direct consequence of F1 and was checked rather than assumed: unused members on
MTM_WIP_Application_WinForms went 18 → 20. Both new findings are methods in
`TransactionDetailPanel.Designer.cs` that the merge bug previously dropped, and each occurs exactly
once in the corpus, so both are true positives.

---

## F1 — Members declared after the first partial are discarded (critical)

**Symptoms**

- `x:Bind` to a `[RelayCommand]`-generated command is reported as a dead binding when the command's
  method lives in a partial file other than the one parsed first.
- No dependency edges, cycle involvement or unused-member candidates are produced for methods,
  type mentions or `typeof` references declared in a non-first partial.

**Reproduction**

Two files declaring one partial type; the second declares a `[RelayCommand]` method.

```csharp
// src/A.cs
namespace N;
public partial class Thing
{
    public string Alpha { get; set; } = string.Empty;
}
```

```csharp
// src/B.cs
namespace N;
public partial class Thing
{
    [RelayCommand]
    private void Save() { }

    private void Second() => MarkerOnly.Reset();
}
```

Observed: `dead-binding` finding `x:Bind ViewModel.SaveCommand is not on Thing`, and no edge from
`Thing` to `MarkerOnly`. Both are wrong.

Real-world reproduction: MTM_Receiving_Application reports 9 dead bindings; 8 of them are commands
declared in a partial file, for example
`View_ReconciliationMain.xaml:544` binding `ViewModel.AcceptCommand`, whose `[RelayCommand]` method
lives in `ViewModel_ReconciliationMain.Review.cs`.

**Root cause**

`src/graph/analyze.ts:39` `mergeDeclarations`. The merge pushes `constructors`, `properties`,
`fields`, `assignments`, `calls`, `creations` and `bases` from each further declaration, but never
`methods`, `mentions` or `typeofRefs`. The `{ ...decl }` in the first branch keeps those three
arrays from the first file only, so every later file's copy is silently dropped.

**Suggested fix**

Copy the missing arrays in the first branch and push them in the merge branch:

```ts
decl: {
  ...decl,
  constructors: [...decl.constructors],
  properties: [...decl.properties],
  fields: [...decl.fields],
  methods: [...decl.methods],
  assignments: [...decl.assignments],
  calls: [...decl.calls],
  creations: [...decl.creations],
  mentions: [...decl.mentions],
  typeofRefs: [...decl.typeofRefs],
  bases: [...decl.bases],
}
```

and in the merge branch:

```ts
target.methods.push(...decl.methods);
target.mentions.push(...decl.mentions);
target.typeofRefs.push(...decl.typeofRefs);
```

**Evidence (measured on real repositories)**

| Repository | Partial types | Methods dropped | Mentions / `typeof` dropped | Evidence lines attributed to the wrong file |
| --- | --- | --- | --- | --- |
| MTM_Receiving_Application | 5 | 163 | 985 | 1,393 |
| MTM_Loader_WinUI3 | 8 | 89 | 563 | 809 |
| MTM_WIP_Application_WinForms | 71 | 156 | 29,108 | 40,397 |

Worst cases: `MainWindow` (97 methods dropped), `App` in MTM_Loader_WinUI3 (64 methods dropped
across six files), `ViewModel_Dunnage_WorkFlowViewModel` (32 methods).

---

## F2 — Evidence lines for merged members name the wrong file (high)

**Symptoms**

- Clicking a tooltip's `file:line` or double-clicking an edge opens a line in a file that does not
  contain the code, sometimes past the end of the file.
- 9 to 11 line numbers per repository fall outside the file they claim; hundreds more are inside
  the file but wrong.

**Reproduction**

Synthetic two-partial case from F1, where the field and the `new Helper()` live in `B.cs`:

```text
reference: N.Thing -> N.Helper (src/A.cs:15) Holder
```

`src/A.cs` has 11 lines and does not declare `Holder`; the declaration is `src/B.cs:11`.

Real-world examples (line number is past the end of the named file):

```text
edge constructs  App -> IdleTimerStage   line 147 of 139 in src/MTM.Loader.App/App.Bundle.cs
edge composes    App -> LoaderUpgradeStateStore  line 199 of 139 in src/MTM.Loader.App/App.Bundle.cs
edge reference   ViewModel_ReconciliationMain -> IService_ReconciliationWriteService
                 line 52 of 49 in Module_Reconciliation/ViewModels/ViewModel_ReconciliationMain.Attention.cs
```

**Root cause**

`src/graph/analyze.ts` merges each partial into one `Entry` that stores a single `file`
(`mergeDeclarations`, line 39), then records every edge as `{ file, line: <member line> }` where
`<member line>` came from whichever partial declared that member. The member records in
`src/graph/csharp/parser.ts` carry only a line, never the file they came from.

**Suggested fix**

Carry the origin file with each parsed record (`PropertyDecl`, `FieldDecl`, `MethodDecl`,
`AssignmentSite`, `CallSite`, creations, `TypeMention`, `typeofRefs`) or keep a
`Map<memberKey, file>` per merged entry, and use it when building evidence. F1 and F2 are the same
root defect and should be fixed together.

---

## F3 — `dead-binding` inspects only the view's properties (high)

**Symptoms**

False "dead binding" findings for any `x:Bind` whose root is not a property of the page class.
Everything below was reported dead by the extension although it is valid:

| Markup | Reality |
| --- | --- |
| `{x:Bind _status}` | `private string _status` field on the page |
| `{x:Bind _viewModel.Status}` | readonly view-model field on the page |
| `{x:Bind IsBusy}` | property generated by `[ObservableProperty] private bool _isBusy` |
| `{x:Bind Normalize(ViewModel.Status)}` | function binding calling a page method |
| `{x:Bind ToInfoBarSeverity(ViewModel.StatusSeverity)}` | real code, `View_Receiving_LabelOverlayDialog.xaml:29` |

Only `{x:Bind ViewModel.DoesNotExist}` was a true positive.

**Root cause**

`src/graph/findings.ts:423`:

```ts
const rootProperty = viewDecl.properties.find((property) => property.name === binding.root);
if (!rootProperty) { /* emits a high-severity dead-binding finding */ }
```

Only declared properties are considered. The member set that the rest of the extension uses
(`memberNamesOf`, `src/graph/findings.ts:144`) already covers fields, methods, `[RelayCommand]` /
`[ObservableProperty]` generated members and inherited members, but is not consulted here.

A secondary gap: after the root is found, only `binding.path[0]` is validated
(`src/graph/findings.ts:460`), so a genuinely broken deeper segment such as
`{x:Bind ViewModel.Items.Missing}` is not reported.

**Suggested fix**

Judge the root against `memberNamesOf(xaml.className, decls, index, usingsFor)` instead of
`properties.find`, and treat an expression containing `(` as a function binding that must resolve to
a method rather than a property. Optionally walk the remaining path segments when the intermediate
types resolve.

---

## F4 — Layer classification only understands the `*ViewModel` suffix (high)

**Symptoms**

On repositories that name view models `ViewModel_Foo` (prefix convention) or rely on a
`*.ViewModels` namespace, view models are classified as `model`, so the opening canvas, the MVVM
preset, the colours and the layer filters are all wrong.

- MTM_Receiving_Application: 185 types named `ViewModel_*`; 182 classified `model`, 2 `service`,
  1 `viewmodel`.
  Opening the graph shows 86 of 1,148 nodes (85 views, 1 view model) and 25 of 6,199 edges.
- MTM_WIP: 24 view-model types, 20 of them named `ViewModel_*` and every one of those 20 classified
  `model`; only the 4 suffix-named ones land in `viewmodel`. Opening the graph on a repository with
  28 views shows **2 nodes and 0 edges**.
- MTM_Loader_WinUI3 (suffix convention): 38 nodes, 44 edges — the feature works as designed.

**Root cause**

`src/graph/layers.ts:13` `classifyType`:

```ts
if (name.endsWith('ViewModel')) return 'viewmodel';
...
if (MODEL_NAMESPACE_HINTS.some((hint) => namespace.includes(hint))) return 'model';
```

Three problems in one function:

1. Only the suffix is recognised, never a `ViewModel_`/`VM_` prefix.
2. Only `.Contracts` / `.Services` / `SERVICE_NAME_SUFFIXES` produce `service`; `Dao_*`,
   `Service_*` prefixes and `Repository` conventions are not covered (`Dao_*` → `model`).
3. `MODEL_NAMESPACE_HINTS` contains `'.Models'` (`src/graph/layers.ts:10`), and the check at
   `src/graph/layers.ts:40` uses `namespace.includes(hint)`, so `'.ViewModels'.includes('.Models')`
   is `true` and a namespace-ending check intended for models captures view-model namespaces.
   Measured: the 20 nodes in MTM_WIP's `*.ViewModels` namespaces all classify as `model`.

**Suggested fix**

Classify from several conventions and check the most specific first:

- name ends with `ViewModel`, or starts with `ViewModel_` / `VM_`;
- namespace segment equals `ViewModels` or ends with `.ViewModels`;
- classify `Dao_`, `Repository`, `Store`, `Provider`, `Service_` prefixes as `service`;
- replace `namespace.includes(hint)` with segment equality (`namespace.split('.').includes('Models')`).

Consider exposing the convention as a setting, since every repository has its own.

---

## F5 — Cycles are dominated by low-confidence reference edges (medium)

**Symptoms**

A single "cycle" finding swallows a large fraction of the application, and it is not actionable.

- MTM_Receiving_Application: `Cycle of 99: App ↔ Control_Shared_TypedLookupTextBox ↔ … ↔ VolvoShipmentEditDialog`,
  built from 224 `reference` edges of which 185 are `low` confidence (`static access`, `typeof`),
  plus 31 `injects`, 11 `binds`, 3 `constructs`, 2 `getservice`, 1 `field`.
- MTM_WIP_Application_WinForms: `Cycle of 115` covering 115 of 334 nodes (34% of the graph).

**Root cause**

`src/graph/findings.ts:7` `CYCLE_EDGE_KINDS` includes `reference`, and every `reference` edge is fed
into the strongly-connected-component pass regardless of confidence. `collectTypeMentions` in
`src/graph/csharp/parser.ts` emits `static access` mentions for any upper-case identifier used as a
receiver, which in MVVM code is extremely common (`App.Current`, `Settings.Default`, `Math.Max`).

**Suggested fix**

Either exclude `reference` edges with `confidence === 'low'` from the SCC pass (keeping `medium`
and `high` references, which come from declared member types), or split the report: keep
construction-level cycles at high/medium severity and report reference-only SCCs as a single
low-severity "mutual references" note with the group size and the shortest cycle path rather than
every member. A "largest strongly connected group" statistic on the debug page would make the
difference visible.

---

## F6 — `Main` is reported as an unused private member (medium)

**Symptoms**

`Unused method: Program.Main` on both console/WinForms repositories tested
(MTM_Application_Loader_WinForms, MTM_WIP).

**Root cause**

`src/graph/findings.ts:16` defines `ENTRY_POINT_NAMES = ['App', 'Program', 'Startup', 'Main']`, but
the only use is the *type* loop at `src/graph/findings.ts:584`. The unused-member pass
(`src/graph/findings.ts:646` onwards) does not consult it, so a `private static void Main` method is
treated as dead.

**Suggested fix**

In the candidate collection for methods, skip `Main` when the method is static, or when it carries
`STAThread`, or when the declaring type is named `Program`. Top-level statements already produce no
members, so nothing else needs to change.

---

## F7 — WPF/MAUI `{Binding}` markup is invisible (medium)

**Symptoms**

On any XAML flavour that is not WinUI/UWP, there are no `binds` edges and no dead-binding findings,
and every view looks unbound. A WPF `{Binding Status}` probe produced `bindings = 0`.

**Root cause**

`src/graph/xaml/parser.ts:119` matches only `{x:Bind`. There is no equivalent for
`{Binding Path=…}`, `{TemplateBinding …}`, or `DataContext` assignment in code-behind.

**Suggested fix**

Either add `{Binding}` extraction with a `DataContext = …` rule for the root type (WPF/MAUI
resolution), or state the WinUI-only limit in the popup, the panel tab and the README so a user on a
WPF repository does not read "0 bindings" as a clean bill of health.

---

## F8 — Multi-root workspaces analyse one root but read every root (medium)

**Root cause**

`src/extension.ts:28` and `:54` take `vscode.workspace.workspaceFolders?.[0]` as the analysis root,
while `src/extension.ts:42` calls `findFiles` without a folder scope, so files from every folder are
read. `vscode.workspace.asRelativePath(uri, false)` then returns a path relative to each file's own
folder, so two folders can produce the same relative path, and `GraphPanel.reveal`
(`src/webview/panel.ts:266`) joins the path against folder 0 only.

**Suggested fix**

Iterate `workspaceFolders`, run one analysis per folder (or prefix every path with the folder name
and analyse the workspace as a whole), and pass the owning folder through to `reveal`. At minimum,
warn when more than one folder is open.

---

## F9 — Export default path is inside the extensions directory (low)

`src/webview/panel.ts:245` builds the suggested save location as:

```ts
vscode.Uri.joinPath(this.context.extensionUri, '..', visible ? 'dependency-graph-view.json' : 'dependency-graph.json')
```

which resolves to `~/.vscode/extensions/dependency-graph.json`. That directory is not writable in
some remote and managed setups, and it is never where a user wants the export.

**Suggested fix**

Default to the first workspace folder (`vscode.Uri.joinPath(folder.uri, 'dependency-graph.json')`)
and fall back to the document directory.

---

## F10 — Module bucket is the first path segment (low)

`src/graph/symbols.ts:84` `moduleOf` returns `segments[0]`. On repositories that put the code under
`src/`, every node lands in the bucket `src` (307 of 516 nodes in MTM_Loader_WinUI3, plus a `tests`
bucket), so the Module dropdown offers a single usable value and the compound grouping degenerates.
Repositories that put modules directly at the root (MTM_Receiving_Application: 20 buckets) work as
intended.

**Suggested fix**

Skip conventional container segments (`src`, `source`, `lib`, `app`, `tests`, `test`) and use the
next segment, or use the nearest `.csproj` directory as the bucket.

---

## F11 — Binding lines point at the element start (low)

`src/graph/xaml/parser.ts:213` computes `const line = lineOf(parser.startIndex)` once per element and
reuses it for every attribute, so every binding on a multi-line element reports the element's first
line. Real example: the `dead-binding` finding for `View_Receiving_LabelOverlayDialog.xaml` reports
line 24 (`<InfoBar`) while the binding is on line 29.

**Suggested fix**

Use `onattribute` from `htmlparser2`, which reports the attribute's own start index, or offset from
the open tag start by locating the attribute value in the source slice.

---

## F12 — `show` always re-analyses and drops concurrent refreshes (low)

`src/extension.ts:166` and `:171` call `panel.refresh(true)`, so **Show Dependency Graph** and
**Refresh Dependency Graph** both re-parse the entire workspace on every invocation (2.7 s for 1,217
files) while `showForSelection` / `showForFile` use the cache. Separately,
`GraphPanel.refresh` (`src/webview/panel.ts:160`) returns immediately when `busy`, so a second
refresh during a run is silently ignored with no feedback.

**Suggested fix**

Make `show` reuse the cached model unless the caller asked to refresh, and show progress or a
"already analysing" state when a refresh is dropped.

---

## F13 — Node-cap truncation is not surfaced on the canvas (low)

`analyzeWorkspace` sets `stats.truncated` when `maxNodes` cuts the model, but
`updateCounters` (`src/webview/client/main.ts:669`) reports nodes, edges, elapsed time and parse
errors only. A truncated graph therefore looks complete. The debug report is the only place that
mentions it.

**Suggested fix**

Append `· truncated to N nodes` to the status line when `stats.truncated` is true.

---

## F14 — `findFiles` cap truncates a large workspace silently (low)

`src/extension.ts:42` passes a 20,000 result cap. A repository with more matching files loses the
remainder with no warning. The same call reads every file into memory before analysis
(`toInputs`), so memory grows with the corpus.

**Suggested fix**

Detect a full result set (`uris.length === 20000`), warn, and point at
`csharpXamlGraph.exclude`.

---

## F15 — `selftest` cannot validate another repository (low)

`src/selftest.ts:27` hard-codes `workspaceRoot = resolve(extensionRoot, '..', '..')` and then
asserts facts that are specific to MTM_Waitlist (`AdministrationSettingsPage`, `NewRequestJobTypeViewModel`,
`139 of 1,825 nodes`, 7 cycles, and so on). Everything in the harness is therefore unavailable as a
smoke test for "any repo with C# and XAML" — which is exactly the scenario that exposed F1 to F4.

**Suggested fix**

Accept a root path argument, split the repository-specific assertions into a fixture module, and add
generic invariants that hold for every repository:

- every edge endpoint and finding node id exists in `nodes`;
- every node, edge-evidence and finding line lies inside the file it names;
- no node id is emitted twice;
- a partial type spread over N files contributes the union of its members.

Those four checks are cheap, and the second and fourth fail immediately on the repositories above
(the other two hold today, which is what makes them worth keeping as regression tests).

---

## Smaller observations

All of these are also fixed in 0.2.0:

- `src/graph/analyze.ts` had a mangled line; `emitReference` is now a normal function signature.
- `handlerNames` from the XAML parser now keeps markup-invoked members alive, alongside bound names.
- The unreachable `binding.path.length === 0 && binding.root === ''` branch is gone — the loop skips a
  null root before it.
- The `void body;` and `void usings;` suppressions are removed.
- Interfaces are classified before the view-model check, so `IResettableViewModel` stays a model.
- `GraphNode.kind`'s documentation now lists the XAML root-tag values it can actually hold.
- README's `exclude` default matches `package.json`, including `**/.github/**` and `**/.specify/**`.

## What works well

- The C# grammar path is solid: 17 constructs including primary constructors, nested types, raw
  string literals, preprocessor branches, default interface members, tuple/generic/nullable
  parameters, BOM + CRLF and genuinely malformed files were all handled without a throw and with
  correct line numbers.
- The XAML parser correctly scopes nested DataTemplates to their own `x:DataType`, ignores commented
  markup and literal braces, resolves `using:` prefixes and skips named elements.
- `unused-type` and `unused-member` findings are trustworthy on real code: on
  MTM_Receiving_Application 13 of 13 unused types and 7 of 7 unused members, and on
  MTM_WIP_Application_WinForms 14 of 14 and 18 of 18, had a single occurrence in the corpus. Views
  constructed inline (`await new Dialog().ShowAsync()`) are correctly kept alive by the `reference`
  edge that `object_creation_expression` produces.
- Empty workspaces, XAML-only workspaces, top-level-statements files and unresolvable bases are all
  handled without crashing, and an unresolvable base correctly suppresses uncertain dead-binding
  claims.
- The webview HTML is consistently escaped and the debug page ships a nonce-based CSP; the packaged
  `.vsix` contains exactly the runtime assets the extension loads.

## How these findings were produced

No changes were made to the extension source. Two throw-away harnesses were built outside the
extension's own toolchain and deleted afterwards:

1. A repository runner that bundles `src/graph/analyze.ts` with esbuild, walks a repository for `.cs`
   and `.xaml`, runs `analyzeWorkspace`, then asserts graph invariants (dangling edges, duplicate
   ids, out-of-range lines) and dumps the model as JSON for inspection.
2. A set of focused probes: partial-class merging, `x:Bind` root variants, binding inside nested
   templates, layer classification by naming convention, cycle composition by edge kind and
   confidence, default-canvas size via `computeVisible`, and identifier-occurrence cross-checks for
   every `unused-type` and `unused-member` finding.

The first repository run used a harness with a path bug of its own (a shadowed root in the recursive
walk), which produced an apparent "all paths are bare file names" result; that observation was
discarded and the harness fixed before any finding was recorded. All numbers in this report come
from the corrected harness.
