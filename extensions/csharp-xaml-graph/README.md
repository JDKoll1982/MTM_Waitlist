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
npm run selftest       # analyses this workspace and asserts the known-broken-out-loud facts
npm run package:vsix   # produces csharp-xaml-graph.vsix
code --install-extension csharp-xaml-graph.vsix
```

Then open the Command Palette and run **C#/XAML Graph: Show Dependency Graph**.

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
| view model → service | constructor parameter type |
| type → base / interface | the base list |
| type → type | `new X(...)` for view models and services, and assignment targets |
| view → row model | `DataTemplate x:DataType` — informational, hidden by default |

Two things are deliberately **not** treated as view-model links, because they are not:

- a binding inside a `DataTemplate` — its root is the row, not the page;
- a binding whose root matches an `x:Name` element — it addresses a control.

## Reading the graph

The first version drew every node in one layout and collapsed into an unreadable line. The view now
opens on a scoped, legible subset and gives you explicit control over what is drawn.

**What you see first.** Views and view models only, with unconnected nodes hidden — 139 of 1,825
nodes in this repository. Everything else is one click away.

**Toolbar**

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
| Hide details | Gives the graph the full width. |
| Nodes / Edges rows | Per-layer and per-edge-kind checkboxes. All ten edge kinds are individually switchable. |

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

**Colour.** Node colour is the layer; edge colour and dash is the kind. A white ring marks a type the
container registers, a red ring marks a cycle, and an amber dashed ring marks a cleanup finding.
The **Legend** button opens a panel that shows every shape, ring and line with the same swatches the
canvas uses.

## Cleanup findings

The **Problems** tab lists what the graph implies about code health, each row carrying the file and
line so you can jump straight to it.

| Finding | How it is decided |
| --- | --- |
| **Cycle** | A strongly connected component across constructor injection, container registration, service location, bindings, composition **and every type reference** — property and field types, return and parameter types, locals, casts, `is`/`as`, `typeof`, attributes, generic constraints, static access and `catch` types. A self-dependency counts as its own finding. |
| **Dead binding** | An `x:Bind` path whose member does not exist on the type it is bound to — checked against the view for page-level bindings, and against the template's own `x:DataType` for bindings inside a `DataTemplate`. Members the MVVM generators add (`[RelayCommand] OnSave()` → `SaveCommand`, `[ObservableProperty] _x` → `X`) and positional record parameters count as present. |
| **Unbound view** | A view whose markup binds `ViewModel.*` but which declares no `ViewModel` member at all. |
| **Unused type** | A declared type with no incoming edge, no `typeof` reference, and no textual mention anywhere. Extension-method holders are credited to their methods. A view must be registered or navigated to. |
| **Unused member** | A private method, field or property whose name occurs only where it is declared. Generated members, overrides, partial methods, `[RelayCommand]` and `[ObservableProperty]` are excluded. |

**Reference edges matter.** Without them the graph is a declaration map, and this repository looks
acyclic because a class holding another as a *property* drew nothing. With them, seven cycles appear —
including a 14-type loop through the NewRequest view models and a 6-type loop through the startup
pages. They are numerous (2,600+ here), so they are off on the canvas by default and on for cycle
detection; switch them on from **Layers & edges → Type reference**.

Two honest limits, both deliberate:

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
| `csharpXamlGraph.exclude` | `**/bin/**`, `**/obj/**`, `**/node_modules/**`, `**/*.Tests/**` | files to skip |
| `csharpXamlGraph.includeExternalTypes` | `true` | show framework and package types |
| `csharpXamlGraph.maxNodes` | `4000` | cap on nodes sent to the webview |

## How it works

C# is parsed with a real grammar: `tree-sitter-c-sharp` compiled to WebAssembly and loaded by
`web-tree-sitter`. That gives exact declarations, base lists, constructor parameters and call sites
with line numbers, and needs no `dotnet` SDK. XAML is streamed with `htmlparser2` in XML mode, which
reports the offset of every element so each fact carries its real line. The graph is drawn with
Cytoscape.js using the dagre layout.

`npm run selftest` runs the whole pipeline over this repository and fails if anything breaks: the
known View→ViewModel links, the two cases that must stay out (template rows and named elements), the
default-scope size, module scoping across projects, and that every control the script reaches for
exists in the panel markup.
