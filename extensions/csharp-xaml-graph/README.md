# C#/XAML Dependency Graph

A self-contained VS Code extension that reads the C# and XAML in a workspace and draws an
interactive dependency graph: which view binds to which view model, which view model the container
resolves for a page, and what each type injects, inherits from or constructs.

Nothing outside this folder is needed, referenced or modified. There is no `.csproj`, no `dotnet`
runtime requirement and no reference to the host application's build.

## Run it

```
npm install
npm run build          # bundles out/ and copies the C# grammar into media/
npm test               # the unit and integration suite (Vitest)
npm run coverage       # the same suite with the coverage report
npm run selftest       # analyses this workspace and asserts the known-broken-out-loud facts
node out/selftest.js <path>   # the generic invariants against any other repository
npm run package:vsix   # produces csharp-xaml-graph.vsix
code --install-extension csharp-xaml-graph.vsix
```

Then open the Command Palette and run **C#/XAML Graph: Show Dependency Graph**.

**Progress.** Analysis of this repository takes about a second; a large one takes longer, so it is
never silent. Two things move at once. A notification counts up in the status bar and in the
notification list, so the bar is visible even with the panel behind another editor. The graph panel
draws the same figure over the canvas — a message such as `Parsing C# — 640 of 815` with the
percentage beside it — and a bare sweeping bar while it is still reading files and cannot yet say how
far along it is. Both disappear when the graph is drawn. The percentage is files parsed out of files
to parse, so it only ever moves forward.

The extension also adds a **C#/XAML Graph** tab to the panel beside Terminal. That tab holds a
shortcut to every command and a settings form, so the extension can be driven without the Command
Palette.

To debug it in place, open this folder as its own VS Code window and press F5. The launch
configuration lives in this folder's `.vscode`, and it analyses the repository two levels up.

## What it extracts

| Link | Evidence in source |
| --- | --- |
| view → view model | `pageService.Configure<TViewModel, TPage>()` |
| view → view model | `ViewModel = App.GetService<T>()` in a view constructor (any property name: `Panel`, `PasswordChange`, …) |
| view → view model | constructor parameter typed `*ViewModel`, including the `_viewModel = viewModel ?? throw …` pattern |
| view → view model | `services.GetRequiredService<T>()` on the navigation parameter |
| view → view model | `{x:Bind <root>.Member}` where `<root>` is a property on the view whose type is a `*ViewModel` |
| view → view model | a view property typed `*ViewModel` — the `DataContext` convention: `public ViewModel_X? ViewModel { get => DataContext as …; }`, whether or not the markup uses `x:Bind` |
| view → view model | a registration that pairs the two: `RegisterRoute(typeof(ViewModel_X), typeof(View_X))`, or the same `typeof` pair inside one navigation entry |
| view model → service | constructor parameter type |
| type → base / interface | the base list |
| type → type | `new X(...)` for view models and services, and assignment targets |
| view → row model | `DataTemplate x:DataType` — informational, hidden by default |

Two things are deliberately **not** treated as view-model links, because they are not:

- a binding inside a `DataTemplate` — its root is the row, not the page;
- a binding whose root matches an `x:Name` element — it addresses a control.

Classic `{Binding}` markup is read but never used to *prove* a link: it binds against a run-time
`DataContext` the analysis cannot follow, so those roots are left unresolved rather than guessed. The
two property and registration rows above are what connect a view to its view model in a repository
that binds that way.

## Reading the graph

The first version drew every node in one layout and collapsed into an unreadable line. The view now
opens on a scoped, legible subset and gives you explicit control over what is drawn.

**What you see first.** Views and view models only, with unconnected nodes hidden — 139 of 1,825
nodes in this repository. Everything else is one click away.

**Toolbar**

Three rows: the text and dropdown controls, then the checkboxes, then the buttons. The status line
and the focus bar sit over the canvas rather than in the toolbar, so the rows never overflow.

| Control | What it does |
| --- | --- |
| Search | Narrows to nodes whose name, namespace or module matches. `Esc` clears. |
| Module | Restricts the graph to one module **plus the partners it connects to**, so a view keeps the view model that lives in another project. |
| Layout | **Packed (auto)** — each connected piece arranged internally then tiled across the panel — plus Layered →, Layered ↓, Force, Radial, Grid, Circle. |
| Density | Compact, Normal, Roomy — node size and spacing. |
| Labels | When zoomed in (default), Always, On hover, Never. |
| Hops | How far a focus reaches: 1, 2 or 3. |
| Hide unconnected | Drops nodes with no edge in the current view — except flagged ones. |
| Only problems | Restricts the canvas to types with a cleanup finding or in a cycle. |
| Layers &amp; edges | Opens the visibility flyout: every node layer and edge kind with live counts, plus presets (MVVM flow, Structure, Cleanup, Everything). |
| Legend | Opens the shape/colour/line key. |
| Fit | Frames whatever is currently drawn. |
| Re-pack group | Returns to the whole graph after a shape was unpacked. Only shown while something is unpacked. |
| Hide details | Gives the graph the full width. |
| Nodes / Edges rows | Per-layer and per-edge-kind checkboxes. All ten edge kinds are individually switchable. |

**Unpacking a shape.** Right-click any node and choose **Unpack this group**. The panel then shows
**only that shape**: every other type is hidden, and the shape is drawn at full density in the layout
chosen for unpacked shapes, from its starting root to the end of the chain. Partners outside the
shape are deliberately left out — you asked to see this shape, not the workspace around it. The
toolbar shows a `1 unpacked · Layered Down` badge and a **Re-pack group** button, and the bar above
the canvas names the scope with **Show everything** to return.

Unpacking also changes the layout **inside** that shape. Packed tiles are always drawn compressed, so
an unpacked shape uses `csharpXamlGraph.unpackedLayout` instead — **Layered Down** by default, which
puts each type above the types it depends on and reads top to bottom. **Re-pack group** in the
toolbar, or **Re-pack this group** in the same menu, puts it back.

**Zoom stack.** The upper-left corner of the canvas carries three controls, top to bottom:

| Control | What it does |
| --- | --- |
| Magnifier | Arms region zoom. Drag a box over the shapes you want; the box becomes the panel. `Esc` cancels. |
| Vertical slider | Zooms in as you drag up and out as you drag down, about the middle of the view. It follows wheel zoom too. |
| Circular arrow | Resets to the normal view: zoom, pan, focus, scope and any unpacked shapes all return to the opening state. |

| Unpack this group | Right-click a node | Redraws its packed shape at full size, above the tiles, in the layout chosen for unpacked shapes. |

**Framing.** The default layout is **Packed (auto)**. A dependency graph is not one shape but many
small ones — a view with its view model, a view model with its services — and a plain layered layout
stacks those disconnected pieces into a single column 3,796 px tall. Packed arranges each piece
internally, then tiles the pieces across the panel, so the same 139 nodes occupy a 997 × 661 area
(aspect 1.51 instead of 0.05).

Fitting always runs, then the zoom is clamped at a readable floor: a graph that legitimately fits
fills the panel, and one that cannot is panned rather than shrunk to a smear.

**Focus.** Click a node to inspect it. The details panel offers **Focus here** (1 hop) and
**Expand 2 hops**, and every neighbour name in its lists is clickable. A focus bar shows what you are
focused on with a one-click way back; `Esc` also clears it. Double-click any node to open its source.

**Filters keep the picture in place.** Ticking a layer, changing an edge kind, typing in Search or
toggling **Hide unconnected** removes and re-adds elements, but every node keeps the position it
already had, so the graph does not reshuffle under you. Only a layout change or revealing nodes the
canvas has never placed triggers a fresh arrangement.

**Hover.** Resting on a node for a moment opens a tooltip: the type's name, layer, kind, module and
source line, how many dependencies it has in each direction, and every cleanup finding that points at
it — ordered by severity, with a plain statement when there are none.

Resting on a **line** opens a tooltip for that dependency: a swatch in the line's own colour, the edge
kind, the pair it joins (short and full names), the member it came from, the exact `file:line` of the
evidence, and what the confidence means — "read straight from the source", "inferred from a reference"
or "a weak hint — worth checking". The hovered line is redrawn white and thicker so you can see which
one is being described, and **double-clicking a line opens that evidence line**. Both tooltips follow
the pointer and never block a click. The status line and focus bar sit over the canvas, which keeps
the toolbar to three rows at any panel width.

**Colour.** Node colour is the layer; edge colour and dash is the kind. A white ring marks a type the
container registers, a red ring marks a cycle, and an amber dashed ring marks a cleanup finding.
The **Legend** button opens a panel that shows every shape, ring and line with the same swatches the
canvas uses.

## Show the graph for one thing

Right-click in a C# or XAML file and choose one of two commands.

| Command | What it does |
| --- | --- |
| **Show Dependency Graph for Selection** | Draws the graph for the type, method, property or field under the cursor. |
| **Show Dependency Graph for File** | Draws the types declared in that file and their partners. Also available on a file in Explorer. |

Resolution goes through the editor's own language services: the definition of what is under the
cursor, then the document symbols around it. That means a click on a field typed
`SetupWorkOrderViewModel` lands on the view model's declaration, not on the field's own class.

**When more than one graph could match**, a picker opens and asks which one you mean. That happens
when a simple name exists in several namespaces, and for a file that declares several types — the
first entry there is always **Whole file**.

The panel then opens restricted to what you chose: the type, everything within the hops shown in the
**Hops** box, and a bar above the canvas naming the scope. **Show everything** clears it. The model is
type-level, so a method or field arrives as its declaring type plus the member name, and the
dependency lines that carry that member are highlighted in white.

## Export

**Export JSON** in the toolbar — or the same command in the panel tab — writes **the graph you are
looking at**, not the whole workspace. The file carries:

| Field | Contents |
| --- | --- |
| `scope` | the filters that produced the picture: layout, density, visible layers and edge kinds, module, search, focus and hops, Hide unconnected, Only problems, and any unpacked or scoped shape |
| `nodes`, `edges` | only what is drawn |
| `findings` | only findings that point at a drawn node |
| `cleanup` | counts over those findings |
| `stats` | the visible counts alongside `totalNodes` and `totalEdges`, so the file still says what it was cut from |

Change a filter and export again and the file changes with it. The save dialog suggests
`dependency-graph-view.json` so a view export does not overwrite a whole-workspace one.

## Debug report

**C#/XAML Graph: Open Debug Report** — or **Debug report** in the panel tab — opens a diagnostics
page organised by subsystem rather than by data type, so each section doubles as a map of the code
you are about to change. Every section names the files that own it and has a **Copy** button.

| Section | Answers | Files |
| --- | --- | --- |
| **Host** | Which VS Code, host, platform and workspace is this? A second workspace root explains an empty graph. | `package.json`, `extension.ts` |
| **Settings** | What is each key *actually* set to, and which scope supplied it — user, workspace or default? | `extension.ts`, `menu/menuView.ts`, `webview/panel.ts` |
| **Analysis** | Model age, cache state, file and edge counts, elapsed time, truncation, warnings. | `graph/analyze.ts`, `graph/model.ts` |
| **Parsers** | What the parse pass saw before any graph existed: types declared, partials merged, ambiguous simple names, XAML files with and without `x:Class`, `x:Bind` counts, elements, handlers, and edges dropped as duplicates. | `graph/csharp/parser.ts`, `graph/csharp/symbols.ts`, `graph/xaml/parser.ts` |
| **Findings** | Counts by kind and severity, and every cycle with how many of its edges bind construction. | `graph/findings.ts` |
| **Canvas** | The exact filters `computeVisible` is running with, what is drawn, zoom, scope, unpacked set, and the last canvas events. | `webview/client/main.ts`, `view.ts`, `packing.ts` |
| **Commands** | Manifest versus live registration — the usual cause of a command that does nothing. | `package.json`, `extension.ts`, `menu/menuView.ts` |
| **Packaging** | Whether the grammar, bundles, stylesheet and icon the extension loads are present, with sizes. | `package.json`, `scripts/build.mjs`, `.vscodeignore` |

**Reading it while fixing a bug.** A missing edge is a **Parsers** question first: if the binding
count is healthy and the edge is absent, the resolver is at fault; if the count is low, the parser is.
A finding you disagree with is a **Findings** question, compared against the parser tallies. An empty
canvas is almost always **Canvas** — a layer switched off, a search still applied, or a scope left
behind. Nothing renders at all after an install is **Packaging**.

**Actions** in the report's toolbar: **Refresh report** re-collects both halves, **Re-analyse** clears
the cached model and re-reads the workspace, **Copy all** puts the plain-text report on the clipboard,
**Settings** opens the extension's settings, and **Panel tab** opens the commands tab.

The canvas half is fetched live from the open graph panel; when no graph is open, the report still
renders and says so. Copying a section produces plain text, so it can be pasted straight into a bug
note.

## The panel tab beside Terminal

The extension contributes a **C#/XAML Graph** tab to the panel, beside Terminal, Problems and
Output. It holds:

- **Shortcuts** — show the graph, re-analyse, export JSON, and open the extension's settings.
- **A command table** — what each command does and where to find it.
- **A settings form** — files to analyse, files to skip, the largest graph to draw, the layout used
  inside an unpacked shape, and whether framework types are shown. Saving writes to this workspace
  only; **Reset to defaults** removes the workspace overrides.

## Cleanup findings

The **Problems** tab lists what the graph implies about code health, each row carrying the file and
line so you can jump straight to it.

| Finding | How it is decided |
| --- | --- |
| **Cycle** | A strongly connected component across constructor injection, container registration, service location, bindings, composition **and every type reference** — property and field types, return and parameter types, locals, casts, `is`/`as`, `typeof`, attributes, generic constraints, static access and `catch` types. A self-dependency counts as its own finding. |

Cycle severity follows what the loop is made of, not how large it is:

- **High** — the construction edges inside the group form a loop of their own, so no member can be
  built first. This is the only case where the finding says construction is blocked.
- **Medium** — the loop closes through a plain reference (static access, `typeof`, a member typed as
  the other side), even if the group also contains construction edges that run one way. Nothing is
  blocked; it is a mutual dependency to unpick.
| **Dead binding** | An `x:Bind` path whose member does not exist on the type it is bound to — checked against the view for page-level bindings, and against the template's own `x:DataType` for bindings inside a `DataTemplate`. Members the MVVM generators add (`[RelayCommand] OnSave()` → `SaveCommand`, `[ObservableProperty] _x` → `X`) and positional record parameters count as present. |
| **Unbound view** | A view whose markup binds `ViewModel.*` but which declares no `ViewModel` member at all. |
| **Unused view** | A view that is neither registered with the container, nor navigated to, nor hosted as an element in another view's markup (`<views:SetUpPanel />`). |
| **Unused type** | A declared type with no incoming edge, no `typeof` reference, and no textual mention anywhere. Extension-method holders are credited to their methods. |
| **Unused member** | A private method, field or property whose name occurs only where it is declared. Generated members, overrides, partial methods, `[RelayCommand]` and `[ObservableProperty]` are excluded. |

**Reference edges matter.** Without them the graph is a declaration map, and this repository looks
acyclic because a class holding another as a *property* drew nothing. With them, seven cycles appear —
including a 14-type loop through the NewRequest view models and a 6-type loop through the startup
pages. They are numerous (2,600+ here), so they are off on the canvas by default and on for cycle
detection; switch them on from **Layers & edges → Type reference**.

Three honest limits, all deliberate:
- **Dead-binding checks are `x:Bind` only.** `{Binding}` and `{TemplateBinding}` are parsed and counted
  (see the debug report's Parsers section), but they resolve against a run-time `DataContext` this
  extension does not model, so they are never reported as dead. On a WPF or MAUI repository expect
  zero dead-binding findings — that is the documented limit, not a clean bill of health.
- **`x:Bind` is compile-checked.** In a project that builds, a dead binding should not exist, so a
  zero is the expected answer rather than a missing feature. The check exists for the moment a
  binding path is changed in markup alone.
- **Tests are excluded by default.** A member used only by tests is reported as unused, because the
  analysis never saw the test. Add the test project to `csharpXamlGraph.include` if you want test
  usage counted.

**Only problems** restricts the canvas to flagged types, and a flagged type is never hidden as
"unconnected" — the finding is the reason you opened the panel.

## Settings

| Setting | Default | Meaning |
| --- | --- | --- |
| `csharpXamlGraph.include` | `**/*.cs`, `**/*.xaml` | files to analyse |
| `csharpXamlGraph.exclude` | `**/bin/**`, `**/obj/**`, `**/node_modules/**`, `**/*.Tests/**`, `**/.github/**`, `**/.specify/**` | files to skip — the last two keep documentation and agent samples out of the analysis |
| `csharpXamlGraph.includeExternalTypes` | `true` | show framework and package types |
| `csharpXamlGraph.maxNodes` | `4000` | cap on nodes sent to the webview |
| `csharpXamlGraph.unpackedLayout` | `layered-tb` | layout used inside an unpacked shape: Layered Down, Layered Right, Force, Radial, Grid, Circle |

All five are editable from the panel tab's settings form as well as from the settings editor. A
change to the files read invalidates the analysed model, so the next graph is read from disk again;
a change to the unpacked layout only redraws, which is immediate.

## How it works

C# is parsed with a real grammar: `tree-sitter-c-sharp` compiled to WebAssembly and loaded by
`web-tree-sitter`. That gives exact declarations, base lists, constructor parameters and call sites
with line numbers, and needs no `dotnet` SDK. XAML is streamed with `htmlparser2` in XML mode, which
reports the offset of every element so each fact carries its real line. The graph is drawn with
Cytoscape.js using the dagre layout.

`npm run selftest` runs the whole pipeline over this repository and fails if anything breaks: the
known View→ViewModel links, the two cases that must stay out (template rows and named elements), the
default-scope size, module scoping across projects, that every control the script reaches for exists
in the panel markup, that unpacking a packed shape shows that shape alone in the configured layout, that
the toolbar keeps its three rows, that the hover tooltip states a type's findings, that the zoom
stack carries region zoom, a slider and a reset, that the panel tab carries every shortcut and
settings control, and that every contributed command is registered by the extension.

## Tests

`npm test` runs the suite: 26 files, 637 tests, Vitest on jsdom. `npm run coverage` adds the v8
report under `coverage/`.

Nothing in the suite is a stand-in for the extension's own code. The real tree-sitter WASM grammar
parses real C# and XAML, the real Cytoscape core draws and lays out the graph, and the panel under
test is the panel the webview serves. Three pieces of scaffolding make that possible:

| Scaffold | What it replaces | Why |
| --- | --- | --- |
| `test/support/vscode.ts` | the `vscode` module | the host API does not exist outside VS Code; the double is stateful, so commands, configuration changes and panel messages behave as they do in the editor |
| `test/setup/environment.ts` | jsdom gaps | a 2D canvas context, `ResizeObserver` and `matchMedia`; without them Cytoscape cannot measure or draw |
| `vitest.config.mts` | module resolution | aliases `vscode` to the double and supplies `__filename`, which the C# parser needs for `createRequire` |

Coverage today:

| Area | Statements | Branches | Functions |
| --- | --- | --- | --- |
| All source | 98.96 | 93.6 | 100 |
| `graph/` (analysis, findings, parsers) | 98–100 | 89–100 | 100 |
| `webview/` (panel, page script) | 98–100 | 91–100 | 100 |
| `menu/`, `symbols/`, `debug/` | 100 | 96–100 | 100 |

The thresholds in `vitest.config.mts` sit just under those figures, so a change that loses coverage
fails `npm run coverage` rather than quietly eroding the suite.

**What is still uncovered, and why.** What remains is defensive guards that cannot fire through any
path the panel offers: `if (!cy)` in helpers that are only reachable once a canvas exists, `if
(!model)` in functions only called when a model arrives, `?? []` fallbacks for grammar fields
tree-sitter always supplies, and `if (!decl)` after a lookup that only ever returns declarations. Two
guards were provably dead and were deleted rather than tested — a whole-row binding check that ran
after empty binding roots were already skipped, and a membership filter inside a cycle check whose
caller had already filtered. The rest stay: they are cheap, and they are the difference between a
crash and a blank panel if the markup or the grammar ever changes.

## What the tests changed

Writing the suite found real defects, each fixed in the source rather than asserted around:

- A `field_declaration` keeps its type inside a `variable_declaration`, so every field's type was the
  whole declaration text — silently dropping every field-typed reference.
- A `method_declaration` carries its return type in `returns`, not `type`, so every return type was
  empty.
- `using Alias = Something;` recorded the alias as a namespace.
- Array bounds were not stripped, so `new Foo[3]` never resolved to `Foo`.
- `x is T` patterns were never collected, so pattern-matching references were missing entirely.
- A type's own attributes and generic constraints sit outside its body and were never read.
- `System.Collections.IEnumerable` was classified from its qualified name, turning an implemented
  interface into a base class.
- The per-layer and per-edge counts beside the visibility checkboxes were never filled in.
- A dead binding on a later path segment named only the failing segment (`Row.Missing`) instead of
  the path that actually failed (`Row.Middle.Missing`).
