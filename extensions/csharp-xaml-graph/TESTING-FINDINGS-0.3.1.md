# C#/XAML Graph — Independent Test Pass

Date: 2026-11-25
Extension version tested: **0.3.1** (`package.json`), working tree at MTM_Waitlist commit `36b19525`
Environment: Windows, Node 24.18.0, npm 11.16.0, TypeScript 5.9.3, `web-tree-sitter` 0.27.2
Scope: the whole extension in this folder, driven from a plain shell (`npm run build`, `npm test`,
`node out/selftest.js <repo>`), plus synthetic fixtures analysed through the real pipeline.

This pass starts from the previous register in [`TESTING-FINDINGS.md`](TESTING-FINDINGS.md). Findings
**F1–F15 were re-checked and remain fixed** — by running the behaviour where it is reachable from a
plain shell (F1–F7, F9–F11, F15) and by reading the fix plus its covering test for the host-only
paths (F8, F12–F14). Nothing below repeats them. The new findings are numbered from **F16** so the
register stays append-only.

---

## 1. What was run

| Check | Result |
| --- | --- |
| `npm run typecheck` (`tsc --noEmit`) | Pass |
| `npm run build` (extension, webview, selftest bundles, grammar copy) | Pass, no warnings |
| `npm test` (Vitest) | **642 passed**, 26 files, 43.6 s |
| `node out/selftest.js` (MTM_Waitlist, repository-specific assertions included) | `ALL CHECKS PASSED` |
| `node out/selftest.js <repo>` on 5 other repositories | `ALL CHECKS PASSED` on every one |
| `npm run package:vsix` | Pass, 16 files, 934.55 KB, grammar + `web-tree-sitter` runtime present |
| 45 synthetic C#/XAML fixtures through `analyzeWorkspace` (rounds 1–5) | No crashes; 14 defects below |
| Robustness: unclosed XAML tag, 600-deep XML nesting, binary garbage `.cs`, empty workspace, 3 000 types in one file, 4 000-term expression | No throw, no hang; worst case 124 ms |

### Repository matrix (re-measured in this pass, default settings, `maxNodes` 4 000)

| Repository | Files (cs/xaml) | Nodes | Edges | Findings | Opening canvas | Elapsed |
| --- | --- | --- | --- | --- | --- | --- |
| MTM_WIP | 199 / 28 | 299 | 857 | 11 | 39 nodes, 22 edges (10 hidden as unconnected) | 404 ms |
| MTM_Receiving_Application | 1 015 / 145 | 1 159 | 6 774 | 23 | 243 nodes, 284 edges | 2.5 s |
| MTM_Loader_WinUI3 | 217 / 26 | 336 | 1 318 | 4 | 39 nodes, 49 edges | 362 ms |
| MTM_WIP_Application_WinForms | 342 / 0 | 334 | 1 553 | 36 | 0 (no XAML) | 1.6 s |
| MTM_Application_Loader_WinForms | 26 / 0 | 28 | 92 | 1 | 0 (no XAML) | 48 ms |
| MTM_Waitlist | 815 / 93 (2 222 bindings parsed) | 1 095 | 4 784 | 23 | 162 nodes, 218 edges | 1.6 s |

No repository hit the node cap, so F13/F14 remain untriggered rather than re-proven.

Spot-checked findings for truthfulness on MTM_WIP: the 5 `unused-type` and 5 `unused-member`
findings are accurate. `Helper_Text_Ellipsis`, `Helper_Text_ShrinkToFit` and
`Validator_Data_DatabaseOptions` are used only from `MTM_WIP.Tests`, which the default `exclude`
drops on purpose and which the settings panel explains. The single cycle
(`GuardedNetworkFileSink ↔ Options_Logging_Destinations ↔ SerilogLoggingConfiguration`) is a real
mutual reference: the sink injects the options type, the options type reads constants off the
Serilog configuration, and the configuration constructs the sink.

### How the new findings were produced

Synthetic repositories were analysed through the shipping pipeline (`analyzeWorkspace` bundled from
`src/` with esbuild, same arguments `extension.ts` passes), then the emitted nodes, edges, findings
and counts were inspected. Each finding below quotes the fixture and the observed output, so it can
be reproduced without the harness.

---

## 2. Findings

| Id | Severity | Area | Finding |
| --- | --- | --- | --- |
| F16 | High | Parser | A primary-constructor parameter produces no `injects` edge — the modern DI idiom is invisible |
| F17 | Medium | Parser | Every member of a nested type is also attributed to its enclosing type, adding duplicate edges |
| F18 | Medium | Findings | `unbound-view` is a false positive when the view model property is inherited from a base page |
| F19 | Medium | Parser | WPF `Application.StartupUri` is not read, so the application's main window is reported an unused view |
| F20 | Medium | Analysis | The `binds` edge is only drawn from a view's *own* property typed exactly `*ViewModel`, so fields, generic collections and inherited properties leave the view unconnected |
| F21 | Medium | Analysis | A partial type resolves names using the `using` directives of the first file that declares it, losing edges for members declared in later partials |
| F22 | Medium | Host | UTF-16 / non-UTF-8 source files are decoded as UTF-8 and silently produce nothing |
| F23 | Low | Parser | `this._field = value` produces no `field` edge |
| F24 | Low | Analysis | `cleanup` counts (shown in the Problems summary) are computed before the node cap and can disagree with the list |
| F25 | Low | Webview | The Problems tab renders every finding with no cap; 3 000 types produce 2 997 DOM rows in one `innerHTML` |
| F26 | Low | Menu | "Settings saved" confirmation can be lost because every settings write re-renders the menu webview |
| F27 | Low | Host | Warning notifications that offer a button never wire it (`'Show graph'`) |
| F28 | Low | Packaging | `TESTING-FINDINGS.md` ships inside the VSIX |
| F29 | Low | Analysis | A namespaced root element (`<muxc:Page>`) yields a node `kind` of `muxc:page` |

---

### F16 — Primary-constructor parameters produce no `injects` edge (high)

**Fixture**

```csharp
namespace N {
  public interface IBar { }
  public class Base { }
  public class Foo(IBar bar) : Base { public IBar Bar => bar; }
}
```

**Observed**

```
edge N.Foo -inherits(high)-> N.Base        @src/P.cs:4 Base
edge N.Foo -reference(medium)-> N.IBar     @src/P.cs:5 Bar
```

No `injects` edge. Compare the classic constructor, which does produce `injects(high)`.

**Why it matters.** `class MainPage(INavigationService nav) : Page` is the current MAUI and ASP.NET
Core template shape, and C# 12 primary constructors on records carry the same meaning. On such a
repository the constructor-injection layer silently disappears: view models look unconnected, the
MVVM preset draws a view with nothing attached, and the injection edges that the README advertises
are missing. Four real primary-constructor declarations already exist across the sibling
repositories (two of them in production code, e.g. `MTM_Waitlist.GenericRequest/Models/GenericRequestOptions.cs:21`).

**Cause.** [`src/graph/csharp/parser.ts:562`](src/graph/csharp/parser.ts) reads
`constructor_declaration` members only. A primary constructor lives in a `parameter_list` on the
`class_declaration` / `record_declaration` itself, and `visitChildren` never reads it — even though
`readRecordParameters` ([`parser.ts:744`](src/graph/csharp/parser.ts)) already proved that parameter
list is reachable from there.

**Suggested fix.** In `visitChildren`, when `isTypeDeclaration(child.type)`, read
`child.childForFieldName?.('parameters') ?? named(child).find(c => c.type === 'parameter_list')`
and, when present, push a `ConstructorDecl { params: readParameters(list), line: lineOf(child) }`.
`readRecordParameters` already handles the property side of the same node; the two should share the
parameter list rather than each finding it.

---

### F17 — Nested-type members are attributed to the enclosing type (medium)

**Fixture**

```csharp
namespace N {
  public class Outer {
    public class Inner {
      public Service S { get; set; } = new Service();
    }
  }
  public class Service { }
}
```

**Observed** (four edges where two are correct)

```
edge N.Outer -composes(medium)-> N.Service   @src/N.cs:4
edge N.Outer -reference(low)-> N.Service     @src/N.cs:4 property Service
edge N.Outer.Inner -composes(medium)-> N.Service   @src/N.cs:4
edge N.Outer.Inner -reference(medium)-> N.Service  @src/N.cs:4 S
```

A static call or `new` inside a nested type behaves the same way (`Outer -reference(low)-> Helpers`
from a call that only `Inner` makes).

**Why it matters.** Every nested type lends its whole dependency set to its container. That changes
the meaning of the graph (an outer type appears to depend on things it never names), feeds the
cycle pass with edges that do not exist, and can keep a type out of the `unused-type` list because
a name only used inside the nested type counts as an occurrence.

Nested types are not rare in the target languages: WinForms designer partials, `Builder` types,
`State` enums inside a class, and generated `Settings` classes all use them.

**Cause.** [`src/graph/csharp/parser.ts:652-656`](src/graph/csharp/parser.ts) collects
`assignments`, `calls`, `creations`, `typeofRefs` and `mentions` by walking the *whole* body, and a
declaration body contains the nested type's body. The member lists themselves are read only from
direct children, so only these five record sets leak.

**Suggested fix.** Skip nested type declarations during the body walk, e.g. give `walk` a predicate
that does not descend into `class_declaration` / `record_declaration` / `struct_declaration` /
`interface_declaration` children, or subtract the nested type's own records after the walk. A test
asserting that `Outer` has no edge to a type only `Inner` mentions would pin it.

---

### F18 — `unbound-view` is a false positive for an inherited view model property (medium)

**Fixture**

```csharp
public class PageBase : Microsoft.UI.Xaml.Controls.Page { public ItemViewModel? ViewModel { get; set; } }
public partial class V : PageBase { }
```

```xml
<Page x:Class="App.V"><TextBlock Text="{x:Bind ViewModel.Status}" /></Page>
```

**Observed**

```
finding unbound-view/high: V binds ViewModel but declares none
```

The same fixture shows the *other* checks are already inheritance-aware: `dead-binding` correctly
resolves `ViewModel.Status` through `PageBase` and reports nothing. Only this rule disagrees.

**Why it matters.** A shared base page (or base `ContentDialog`) that owns the `ViewModel` property
is a common WinUI layout. The extension then emits a **high**-severity finding against code that
compiles and runs, and the canvas paints the view red for it.

**Cause.** [`src/graph/findings.ts:600`](src/graph/findings.ts) tests
`decl.properties.some(...) || decl.fields.some(...)` on the view's own declaration only, instead of
walking the base chain.

**Suggested fix.** Replace the two `some` calls with the existing
`memberNamesOf(view.id, decls, index, usingsFor)` result: `!members || members.has('ViewModel')`
means "cannot prove it missing", matching how `dead-binding` already decides. Note that
`memberNamesOf` also covers the generated members, which is the right semantics here.

---

### F19 — `StartupUri` is not read, so a WPF main window reads as unused (medium)

**Fixture** (a minimal WPF shape)

```
src/App.xaml        <Application x:Class="WpfApp.App" ... StartupUri="MainWindow.xaml" />
src/App.xaml.cs     public partial class App : System.Windows.Application { }
src/MainWindow.xaml + .xaml.cs
```

**Observed**

```
finding unused-type: Unused view: MainWindow
```

**Why it matters.** A generated WPF template for .NET Framework, the kind of repository this
extension says it reads, reaches its main window only through `StartupUri`. The extension then
reports the *entry view* of the application as dead — the single most visible false positive a user
of a new repository could hit, and it appears before anything else in the Problems tab.

**Cause.** Nothing reads the `StartupUri` attribute. [`src/graph/xaml/parser.ts`](src/graph/xaml/parser.ts)
handles only `x:Class`, `x:Name`, `x:DataType` and a fixed list of event attributes;
[`src/graph/analyze.ts:315`](src/graph/analyze.ts) builds view nodes from `x:Class` and never looks
at `Application`-level startup.

**Suggested fix.** Add `startupUri: string | null` to `XamlFile` (read on the root element when the
root is `Application`), resolve `MainWindow.xaml` relative to that file's folder in `analyzeWorkspace`,
and emit the same `resolves`/`getservice`-style ownership edge the DI paths already use — or simply
mark the resolved view `diRegistered`. A cheap alternative that fixes the false positive without a
new edge kind: treat a view whose `xamlFile` equals any `StartupUri` as registered.

---

### F20 — The `binds` edge ignores fields, generic element types and inherited properties (medium)

Two fixtures, both valid WinUI:

```csharp
// (a) field root
public partial class V : Page { public ItemViewModel? ViewModel; }
```
```xml
<TextBlock Text="{x:Bind ViewModel.Status}" />
```

```csharp
// (b) generic collection root
public partial class V : Page { public ObservableCollection<ItemViewModel> Items { get; } = new(); }
```
```xml
<ListView ItemsSource="{x:Bind Items}" />
```

**Observed**

```
(a) edge App.V -reference(medium)-> App.ViewModels.ItemViewModel   (no binds edge)
(b) edge App.V -reference(medium)-> App.ViewModels.ItemViewModel   (no binds edge)
```

`dead-binding` stays silent in both cases, correctly: `memberNamesOf` sees the field, and the path
resolves. So the model *knows* the binding is good and still does not draw it.

**Why it matters.** `reference` is one of the three edge kinds that are off by default, so with the
opening filters the view and its view model are unconnected — and "Hide unconnected" removes the
view entirely. Any repository that keeps its `ViewModel` in a field (common in older hand-written
code-behind) or binds to a collection therefore opens on a graph that says the page has no view
model. The inherited-property case (`ViewModel` declared on a base page) has the same symptom.

**Cause.** [`src/graph/analyze.ts:585`](src/graph/analyze.ts) searches `decl.properties` only for the
markup root; [`analyze.ts:615-624`](src/graph/analyze.ts) builds the `binds` edges from the view's
own properties whose *normalised* type is a view-model name. Normalisation strips the generic
arguments (`normalizeTypeText` keeps `ObservableCollection`), and a field is never considered,
and a base type is never consulted.

**Suggested fix.** For each non-template `x:Bind` root that resolves to a member, emit
`binds` with that member's declared type as target when the type resolves (use the same
`memberType(root, view)` helper `findings.ts` already has, which walks bases). For a generic
`IEnumerable<T>`/`ObservableCollection<T>` root, additionally resolve the element type and emit the
edge to it, labelled with the property name. That keeps one `binds` edge per real binding and does
not invent a dependency: the markup does read that type.

---

### F21 — A partial type uses only the first file's `using` directives (medium)

**Fixture**

```csharp
// src/A.cs
namespace N { public partial class VM { } }
// src/B.cs
using X;
namespace N { public partial class VM { public Other P { get; set; } } }
namespace X { public class Other { } }
namespace Y { public class Other { } }
```

**Observed** — no edge from `N.VM` to `X.Other`, although `using X;` is in scope where `P` is
declared. Remove the `Y.Other` duplicate and the edge appears, but only because resolution falls
back to "the single workspace type with that name" — not because the using was read.

**Why it matters.** `MainViewModel.cs` + `MainViewModel.Commands.cs`, or
`MainPage.xaml.cs` + `MainPage.Handlers.cs`, is the normal partial layout. When the second file
adds a `using` (or only that file's members need it), references declared there resolve against the
wrong directive set: edges go missing, and a `dead-binding` path through such a member can be judged
against the wrong type. Silent, and hard to attribute to a using.

**Cause.** [`src/graph/analyze.ts:230`](src/graph/analyze.ts): `usingsByFullName` is written only
when the key is absent (`if (!usingsByFullName.has(decl.fullName))`), so the first parsed file wins.

**Suggested fix.** Record usings **per declaration**, not per merged name: keep
`using` lookup keyed by `(fullName, file)` and let `usingsFor` return the union of the usings of
every file that declares the type (a union is safe for name-based resolution and never loses a
candidate). `stampOrigin` already gives every member its origin file, so a member-exact variant is
also possible if the union turns out to over-resolve.

---

### F22 — Non-UTF-8 source files are decoded as UTF-8 and vanish (medium)

**Fixture** — a `.cs` file whose bytes are UTF-16LE (what Visual Studio's "Unicode (UTF-16)" save
produces; still common in older .NET Framework and WinForms repositories), passed through the same
decoding the host performs:

```ts
text: Buffer.from('namespace N { public class Legacy { } }', 'utf16le').toString('utf8')
```

**Observed**

```
parse errors: 1
nodes: 0
```

No warning, no finding — the file simply contributes nothing. The only trace is the parse-error
count inside the debug report.

**Why it matters.** The extension is advertised as working on "any repository with C# and XAML". A
repository with one UTF-16 project silently loses that project's types, and the graph shows the
rest as if nothing were missing. `bin/obj` exclusion does not help: source files are affected.

**Cause.** [`src/extension.ts:28`](src/extension.ts) decodes with
`Buffer.from(bytes).toString('utf8')` and
[`src/selftest.ts:89`](src/selftest.ts) mirrors it with `readFileSync(full, 'utf8')`.

**Suggested fix.** Decode with BOM detection: check for `FF FE` / `FE FF` / `EF BB BF` and use
`utf16le` (swap for BE using `Buffer.swap16`) or `utf8` accordingly; fall back to UTF-8. Push a
`warnings` entry naming any file that produced a syntax error, and show the count in the canvas
status line, so a lost file is visible rather than absent.

---

### F23 — `this._field = value` produces no `field` edge (low)

**Fixture**

```csharp
public class Holder {
  private Svc _svc;
  public Holder(Svc svc) { this._svc = svc; }
}
```

**Observed** — `injects` and `reference` edges only; the `field` kind exists for exactly this pattern
but never fires.

**Cause.** [`src/graph/csharp/parser.ts:339`](src/graph/csharp/parser.ts) drops an
`assignment_expression` unless `left.type === 'identifier'`; `this._svc` is a
`member_access_expression`.

**Suggested fix.** Accept `member_access_expression` whose `expression` is `this`, and take the
`name` field as the target. Everything downstream is already name-based.

---

### F24 — `cleanup` counts are computed before the node cap (low)

**Fixture** — five classes, `maxNodes: 3`.

**Observed**

```
cleanup counts: {"cycles":1,"deadBindings":0,"unboundViews":0,"unusedTypes":2,"unusedMembers":0}
findings listed: 1
```

The Problems summary in the webview prints `model.cleanup` (["unused types 2"]), directly above a
list containing one unused type.

**Cause.** [`src/graph/analyze.ts:716`](src/graph/analyze.ts) counts `cleanup.findings`, while the
returned list is `finalFindings` (already filtered for the cap).

**Suggested fix.** Count `finalFindings`, and add `stats.truncated` to the summary line so the
reader knows both numbers are partial.

---

### F25 — The Problems tab has no cap (low)

A single file declaring 3 000 classes produces **2 997 findings**, all rendered by
[`renderProblems`](src/webview/client/main.ts) into one `innerHTML`
([`main.ts:1521`](src/webview/client/main.ts)) with a listener per row. On generated DTO or designer
corpora this is thousands of DOM nodes and event handlers built synchronously on every model load.

**Suggested fix.** Render the first ~200 after sorting by severity with a "showing 200 of 2 997"
line and a filter box, or virtualise. The findings are already sorted, so a `slice` before mapping
is a two-line change.

---

### F26 — "Settings saved" can be swallowed by the menu re-render (low)

[`src/menu/menuView.ts:39`](src/menu/menuView.ts) subscribes to `onDidChangeConfiguration` and calls
`refresh()`, which replaces `view.webview.html`. `save()` issues five `configuration.update(...)`
calls, each firing that event, and only afterwards posts `{ type: 'saved' }`. The post races the
fresh document's script, so the status line can stay on "Saving…" and the form re-renders under the
reader's cursor while they are still editing.

**Suggested fix.** Re-render only when the change did not originate from this form (guard with a
`private saving = false` flag), or drop the re-render and write the returned settings into the
existing form fields — the `saved` handler already does exactly that.

---

### F27 — The multi-root warning's button does nothing (low)

[`src/extension.ts`](src/extension.ts) calls

```ts
void vscode.window.showWarningMessage(`${...}`, 'Show graph');
```

and discards the result, so clicking **Show graph** closes the notification and starts nothing.
Either await the result and run `csharpXamlGraph.show`, or drop the button.

---

### F28 — A development document ships in the VSIX (low)

`npm run package:vsix` includes `TESTING-FINDINGS.md` (28.88 KB). `.vscodeignore` excludes the
sources, tests and coverage but not this file. Add `TESTING-FINDINGS*.md` (and keep `readme.md`,
which is wanted).

---

### F29 — A namespaced root element leaks the prefix into `kind` (low)

`<muxc:Page x:Class="App.P">` produces `node.kind = "muxc:page"` because
[`analyze.ts:315`](src/graph/analyze.ts) looks up `VIEW_ROOT_KIND[xaml.rootTag]` with the prefixed
name and then lower-cases the whole tag. The tooltip and the debug report show the colon. Strip the
prefix before the lookup (`rootTag.slice(rootTag.indexOf(':') + 1)`) so prefixed markup maps to
`page` like the default-prefix form does.

---

## 3. Smaller observations (no fix required, worth knowing)

- **Panel listeners accumulate.** `GraphPanel` pushes both `webview.onDidReceiveMessage` and
  `panel.onDidDispose` into `context.subscriptions`. Each close/reopen adds two disposables that are
  only released at deactivation; the old panel's listeners can never fire, so this is growth, not a
  bug. Keeping the disposables on the instance and disposing them in `onDidDispose` would be tidier.
- **`isViewModelLike` is dead production code** (`src/graph/layers.ts:81`): referenced only by its
  own test. `classifyType` and `isViewModelName` cover the call sites.
- **`escapeHtml` does not escape `'`**, in both the webview and the menu. Correct today because
  every interpolated attribute uses double quotes; worth a comment so a future single-quoted
  attribute does not open an injection.
- **A binding-shaped string inside a literal attribute** (`Text="a &gt; b {x:Bind Real}"`) is read as
  a binding and can produce a dead-binding finding. XAML would reject the value anyway, so this is
  noise rather than a wrong verdict.
- **`DataTemplate`s outside a view** (a `ResourceDictionary` of templates, the common WPF shape) are
  parsed and their `template` edge is drawn, but their `x:Bind` paths are never judged, because the
  check requires an `x:Class` to borrow a namespace from. Conservative and documented, but it means
  "dead template binding" coverage differs between repositories.
- **`show` never re-analyses after an edit.** That is the intended F12 behaviour, but nothing tells
  the reader the graph is stale; the debug report is the only place `modelLoadedAt` is visible. A
  "model is N minutes old" line in the canvas status would close the loop.

## 4. What works well (verified in this pass, not taken on faith)

- **Robustness.** Malformed XAML, 600-level nesting, binary garbage, empty workspaces, 3 000 types
  in one file and a 4 000-term expression all complete without throwing or hanging.
- **Determinism and invariants.** All five generic selftest invariants pass on every repository
  tried, including "every location lies inside the file it names" and "no visible edge points at a
  node that is not drawn" across 8 filter combinations.
- **Encoding of *_line numbers*.** BOM + CRLF sources produce correct 1-based lines and correct
  attribute lines; the earlier F11/F2 regressions stay fixed.
- **Partial-type merging.** `[ObservableProperty]` and `[RelayCommand]` members declared in a second
  partial resolve, so no dead binding is reported for `x:Bind ViewModel.Title` or
  `x:Bind ViewModel.SaveCommand` (F1 re-verified).
- **Inheritance-aware dead-binding.** `x:Bind ViewModel.Status` through a `PageBase`-declared
  property resolves correctly; only the sibling `unbound-view` rule (F18) misses it.
- **The MVVM and cleanup rules on real code.** 11 findings on MTM_WIP and 23 on
  MTM_Receiving_Application, spot-checked and accurate; `{Binding}`/`{TemplateBinding}` are read but
  never judged, as documented.
- **Packaging.** The VSIX contains the grammar, the `web-tree-sitter` runtime it resolves at
  `require.resolve('web-tree-sitter/web-tree-sitter.wasm')`, both bundles and the stylesheet — and
  nothing from `src/`, `test/` or `coverage/`.

## 5. Not covered by this pass

- **Live VS Code interaction.** No GUI session was available, so the webview was exercised through
  the jsdom suite (642 tests) and the pure `computeVisible`/`packedLayout` functions, not by
  clicking in a real panel. Command registration, activation and the panel protocol are covered by
  the mocked-host tests only.
- **The node cap and file cap paths** (F13/F14 fixes): no repository reached 4 000 nodes or 20 000
  files, so those messages were read, not observed.
- **Multi-root workspaces**: the F8 warning path was read, not run; F27 is a finding about its
  button, from source.

## 6. Suggested fix order

1. **F16** (primary constructors) and **F18** (false high finding) — both change what a reader
   concludes about correct code.
2. **F17** (nested attribution) and **F21** (partial usings) — they corrupt the graph silently.
3. **F19** (StartupUri) and **F20** (binds from field/generic/inherited roots) — they decide whether
   a non-WinUI-convention repository shows anything at all on opening.
4. **F22** (encoding) — one small helper, high payoff on legacy repositories.
5. **F23–F29** — cheap, isolated, and each has a one- or two-line fix.
