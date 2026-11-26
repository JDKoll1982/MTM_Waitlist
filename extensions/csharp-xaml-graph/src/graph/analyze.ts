import type { EdgeConfidence, Finding, GraphEdge, GraphModel, GraphNode } from './model';
import type { CSharpFile, TypeDecl } from './csharp/parser';
import { initCSharpGrammar, parseCSharpFile } from './csharp/parser';
import { TypeIndex, moduleOf } from './csharp/symbols';
import { compileConventions, isViewModelType, type CompiledConventions } from './conventions';
import { classifyType, isViewModelName } from './layers';
import { computeFindings } from './findings';
import type { XamlFile } from './xaml/parser';
import { parseXamlFile } from './xaml/parser';
import { deriveProfileEvidence, mergeDeclarations } from './inspect';

export interface AnalyzeInput {
  /** Workspace-relative path with forward slashes. */
  path: string;
  text: string;
}

export interface AnalyzeOptions {
  workspaceRoot: string;
  csharp: AnalyzeInput[];
  xaml: AnalyzeInput[];
  grammarWasmPath: string;
  includeExternalTypes?: boolean;
  maxNodes?: number;
  /**
   * Called as the analysis advances. `completed` and `total` count files, so a caller can show a real
   * bar rather than an indeterminate spinner: parsing dominates the elapsed time, and it is the part
   * a reader waits on.
   */
  progress?: (phase: string, completed: number, total: number) => void;
  /**
   * The conventions the repository declares, compiled once by the host. Absent means the built-in
   * conventions, which is what a repository with no profile gets.
   */
  conventions?: CompiledConventions;
}

const VIEW_ROOT_KIND: Record<string, string> = {
  Page: 'page',
  UserControl: 'usercontrol',
  Window: 'window',
  WindowEx: 'window',
  windowex: 'window',
};

/** Splits a declared type into the identifiers it names: `IReadOnlyList<PermissionCell>` → both. */
function typeNamesIn(rawType: string): string[] {
  if (!rawType) {
    return [];
  }
  const names: string[] = [];
  const pattern = /[A-Za-z_][A-Za-z0-9_.]*/g;
  let match = pattern.exec(rawType);
  while (match !== null) {
    const name = match[0];
    // Skip the keywords and modifiers a type expression can carry.
    if (!['global', 'this', 'params', 'ref', 'out', 'in', 'readonly', 'static', 'new', 'void'].includes(name)) {
      names.push(name);
    }
    match = pattern.exec(rawType);
  }
  return names;
}

/** Case-insensitive membership, because a route call name was always matched without regard to case. */
function namedCall(names: Set<string>, method: string): boolean {
  const lower = method.toLowerCase();
  for (const name of names) {
    if (name.toLowerCase() === lower) {
      return true;
    }
  }
  return false;
}

/** Builds the whole dependency model from parsed sources. */
export async function analyzeWorkspace(options: AnalyzeOptions): Promise<GraphModel> {
  const started = Date.now();
  const includeExternalTypes = options.includeExternalTypes !== false;
  const maxNodes = options.maxNodes && options.maxNodes > 0 ? options.maxNodes : 4000;
  const conventions = options.conventions ?? compileConventions();

  /**
   * Parsing is synchronous, so the extension host cannot repaint while a loop of 900 files runs.
   * Yielding every so often lets the progress bar actually move — and costs nothing measurable.
   */
  const YIELD_EVERY = 40;
  const yieldToHost = (): Promise<void> => new Promise((resolve) => setTimeout(resolve, 0));

  const total = options.csharp.length + options.xaml.length;
  let completed = 0;
  const report = (phase: string): void => {
    if (!options.progress) {
      return;
    }
    try {
      options.progress(phase, completed, total);
    } catch {
      // A failing progress listener must never abort an analysis.
    }
  };

  report('Reading the C# grammar');
  await initCSharpGrammar(options.grammarWasmPath);

  const warnings: string[] = [];
  const csharpFiles: CSharpFile[] = [];
  let parseErrors = 0;

  for (let index = 0; index < options.csharp.length; index += 1) {
    const file = options.csharp[index];
    try {
      const parsed = parseCSharpFile(file.path, file.text, conventions.serviceLocator);
      if (parsed.hasSyntaxError) {
        parseErrors += 1;
      }
      csharpFiles.push(parsed);
    } catch (error) {
      warnings.push(`${file.path}: ${(error as Error).message}`);
    }

    completed += 1;
    if (index % YIELD_EVERY === 0) {
      report(`Parsing C# — ${index + 1} of ${options.csharp.length}`);
      await yieldToHost();
    }
  }

  const xamlFiles: XamlFile[] = [];
  for (let index = 0; index < options.xaml.length; index += 1) {
    const file = options.xaml[index];
    try {
      xamlFiles.push(parseXamlFile(file.path, file.text));
    } catch (error) {
      warnings.push(`${file.path}: ${(error as Error).message}`);
    }

    completed += 1;
    if (index % YIELD_EVERY === 0) {
      report(`Parsing XAML — ${index + 1} of ${options.xaml.length}`);
      await yieldToHost();
    }
  }

  // Parsing is the bulk of the work; the graph build and the findings rules are comparatively quick,
  // but they still get a message so a very large workspace does not appear to have stalled.
  report('Building the graph');
  await yieldToHost();

  const entries = mergeDeclarations(csharpFiles);
  const index = new TypeIndex();
  const usingsByFullName = new Map<string, string[]>();
  const declarationCounts = new Map<string, number>();

  for (const file of csharpFiles) {
    for (const decl of file.types) {
      declarationCounts.set(decl.fullName, (declarationCounts.get(decl.fullName) ?? 0) + 1);
    }
  }

  for (const entry of entries.values()) {
    index.add(entry.decl);
  }
  for (const file of csharpFiles) {
    for (const decl of file.types) {
      if (!usingsByFullName.has(decl.fullName)) {
        usingsByFullName.set(decl.fullName, file.usings);
      }
    }
  }

  const usingsFor = (decl: TypeDecl): string[] => usingsByFullName.get(decl.fullName) ?? [];

  const nodes = new Map<string, GraphNode>();
  const edges: GraphEdge[] = [];
  const edgeKeys = new Set<string>();
  /** Edges that could not be drawn at all: a missing endpoint, or a type pointing at itself. */
  let edgesDropped = 0;
  /**
   * Edges collapsed into one already recorded: the same pair related the same way, met again.
   *
   * This is the common case by far — a type named in twenty methods yields one injection edge — and
   * it is not a fault, so it is counted apart from `edgesDropped`. Reporting the two together made
   * the debug report warn on every run of this repository about something no one could act on.
   */
  let edgesMerged = 0;

  const ensureTypeNode = (decl: TypeDecl, file: string): GraphNode => {
    const existing = nodes.get(decl.fullName);
    if (existing) {
      return existing;
    }
    const node: GraphNode = {
      id: decl.fullName,
      label: decl.name,
      fullName: decl.fullName,
      layer: classifyType(decl, conventions),
      module: moduleOf(file),
      kind: decl.kind,
      location: { file, line: decl.line },
      interfaces: decl.bases.filter((base) => /^I[A-Z]/.test(base)),
      baseType: decl.bases[0],
    };
    nodes.set(node.id, node);
    return node;
  };

  const ensureExternalNode = (name: string): GraphNode | undefined => {
    if (!includeExternalTypes || !name) {
      return undefined;
    }
    const existing = nodes.get(name);
    if (existing) {
      return existing;
    }
    const node: GraphNode = {
      id: name,
      label: name.slice(name.lastIndexOf('.') + 1),
      fullName: name,
      layer: 'external',
      module: '(external)',
      kind: 'external',
    };
    nodes.set(node.id, node);
    return node;
  };

  const addEdge = (
    source: string,
    target: string,
    kind: GraphEdge['kind'],
    confidence: EdgeConfidence,
    evidence?: { file: string; line: number },
    label?: string,
  ): void => {
    if (!source || !target || source === target) {
      edgesDropped += 1;
      return;
    }
    const key = `${source}|${target}|${kind}`;
    if (edgeKeys.has(key)) {
      edgesMerged += 1;
      return;
    }
    edgeKeys.add(key);
    edges.push({ id: `e${edges.length}_${kind}`, source, target, kind, confidence, evidence, label });
  };

  // Every declared type becomes a node first, so edge resolution always has a target to point at.
  for (const entry of entries.values()) {
    ensureTypeNode(entry.decl, entry.file);
  }

  // Views: the XAML file carries the identity; the code-behind partial supplies the members.
  for (const xaml of xamlFiles) {
    if (!xaml.className) {
      continue;
    }
    const decl = index.get(xaml.className);
    const kind = VIEW_ROOT_KIND[xaml.rootTag] ?? xaml.rootTag.toLowerCase();

    if (decl) {
      const node = ensureTypeNode(decl, xaml.path);
      node.layer = 'view';
      node.kind = kind;
      node.xamlFile = xaml.path;
    } else {
      nodes.set(xaml.className, {
        id: xaml.className,
        label: xaml.className.slice(xaml.className.lastIndexOf('.') + 1),
        fullName: xaml.className,
        layer: 'view',
        module: moduleOf(xaml.path),
        kind,
        xamlFile: xaml.path,
        location: { file: xaml.path, line: 1 },
      });
    }
  }

  const diRegistered = new Set<string>();

  for (const entry of entries.values()) {
    const { decl, file } = entry;
    const usings = usingsFor(decl);
    const self = decl.fullName;

    // A route registration pairs a page with its view model through `typeof`:
    // `navigation.RegisterRoute(typeof(ViewModel_X), typeof(View_X))`, or the same pair inside a tuple
    // of navigation entries. This repository states the relationship nowhere else — the page's
    // constructor takes nothing and the markup binds with `{Binding}` — so without reading the pair
    // the view and its view model reach the canvas unconnected. One pair per line is the convention,
    // which is why the mentions are grouped by line rather than by declaration.
    const mentionsByLine = new Map<number, { id: string; layer: string; via: string; file: string }[]>();
    for (const mention of decl.mentions) {
      const target = index.resolve(mention.name, usings, decl.namespace);
      if (!target) {
        continue;
      }
      const layer = nodes.get(target.fullName)?.layer;
      if (layer !== 'view' && layer !== 'viewmodel') {
        continue;
      }
      const list = mentionsByLine.get(mention.line) ?? [];
      // A partial type's mentions can come from any of its files, so the file travels with the
      // mention rather than being taken from the entry; otherwise the evidence names one file and a
      // line from another.
      list.push({ id: target.fullName, layer, via: mention.via, file: mention.file ?? file });
      mentionsByLine.set(mention.line, list);
    }

    for (const [line, group] of mentionsByLine) {
      const views = group.filter((mention) => mention.layer === 'view');
      const viewModels = group.filter((mention) => mention.layer === 'viewmodel');
      // Only a `typeof` on one side makes this a registration rather than two names that happen to
      // share a line: `typeof(ViewModel_X), typeof(View_X)` is a table, `new View_X()` is not.
      if (views.length === 0 || viewModels.length === 0
        || !group.some((mention) => mention.via === 'typeof')) {
        continue;
      }
      for (const view of views) {
        for (const viewModel of viewModels) {
          addEdge(view.id, viewModel.id, 'resolves', 'medium',
            { file: view.file, line }, 'route registration');
        }
      }
    }

    for (const call of decl.calls) {
      // pageService.Configure<TFirst, TSecond>() registers the page against what it resolves.
      // The first argument is usually a view model, but this codebase also registers a page against a
      // route type, so both sides count as container-registered — otherwise every route-registered
      // page looks dead to the unused-type rule.
      if (conventions.genericPairCalls.has(call.method) && call.typeArgs.length >= 2) {
        const viewModel = index.resolve(call.typeArgs[0], usings, decl.namespace);
        const page = index.resolve(call.typeArgs[1], usings, decl.namespace);
        if (viewModel) {
          diRegistered.add(viewModel.fullName);
        }
        if (page) {
          diRegistered.add(page.fullName);
        }
        if (viewModel && page) {
          addEdge(page.fullName, viewModel.fullName, 'resolves', 'high', { file: call.file ?? file, line: call.line });
        }
        continue;
      }

      // navigation.RegisterRoute(typeof(TViewModel), typeof(TPage)) and the tuple/array route tables
      // that feed it. The types live in `typeof(...)` arguments, not in generic parameters, so a
      // generic-argument scan alone never sees them.
      if (namedCall(conventions.typeofPairCalls, call.method) && call.typeOfArgs.length >= 2) {
        const viewModel = index.resolve(call.typeOfArgs[0], usings, decl.namespace);
        const page = index.resolve(call.typeOfArgs[1], usings, decl.namespace);
        if (viewModel) {
          diRegistered.add(viewModel.fullName);
        }
        if (viewModel && page) {
          addEdge(page.fullName, viewModel.fullName, 'resolves', 'high',
            { file: call.file ?? file, line: call.line }, 'route');
        }
        continue;
      }

      /* A view model exposed as a dependency property:
         DependencyProperty.Register(nameof(ViewModel), typeof(ViewModel_X), typeof(View_Y))
         The third typeof is the owning view; the second is the view model it carries. */
      if (call.method === conventions.dependencyPropertyCall && call.typeOfArgs.length >= 3) {
        const valueType = index.resolve(call.typeOfArgs[1], usings, decl.namespace);
        const ownerType = index.resolve(call.typeOfArgs[2], usings, decl.namespace);
        if (valueType && ownerType && isViewModelType(conventions, valueType)) {
          addEdge(ownerType.fullName, valueType.fullName, 'getservice', 'high',
            { file: call.file ?? file, line: call.line }, 'DependencyProperty');
          diRegistered.add(valueType.fullName);
        }
        continue;
      }

      // services.AddTransient<TViewModel>() and friends
      if (conventions.addCallPattern.test(call.method) && call.typeArgs.length === 1) {
        const registered = index.resolve(call.typeArgs[0], usings, decl.namespace);
        if (registered) {
          diRegistered.add(registered.fullName);
        }
        continue;
      }

      // services.AddSingleton<TInterface, TImplementation>() — the implementation serves the interface,
      // which is a real relationship the graph should carry, and it keeps the interface from looking dead.
      if (conventions.addCallPattern.test(call.method) && call.typeArgs.length >= 2) {
        const service = index.resolve(call.typeArgs[0], usings, decl.namespace);
        const implementation = index.resolve(call.typeArgs[1], usings, decl.namespace);
        if (service) {
          diRegistered.add(service.fullName);
        }
        if (implementation) {
          diRegistered.add(implementation.fullName);
        }
        if (service && implementation && service.fullName !== implementation.fullName) {
          addEdge(implementation.fullName, service.fullName,
            service.kind === 'interface' ? 'implements' : 'inherits',
            'medium', { file: call.file ?? file, line: call.line }, 'container registration');
        }
      }
    }

    // Constructor parameters: the view-model and service edges that carry the most weight.
    for (const constructor of decl.constructors) {
      for (const parameter of constructor.params) {
        const target = index.resolve(parameter.type, usings, decl.namespace);
        if (target) {
          addEdge(self, target.fullName, 'injects', 'high', { file: constructor.file ?? file, line: constructor.line });
        } else {
          const external = ensureExternalNode(parameter.type);
          if (external) {
            addEdge(self, external.id, 'injects', 'low', { file: constructor.file ?? file, line: constructor.line });
          }
        }
      }
    }

    // Assignment targets: service locator, direct construction, and the field-backed dialog pattern.
    for (const assignment of decl.assignments) {
      const property = decl.properties.find((candidate) => candidate.name === assignment.target);
      const field = decl.fields.find((candidate) => candidate.name === assignment.target);
      const value = assignment.value;

      if (value.kind === 'service-locator') {
        const target = index.resolve(value.typeName, usings, decl.namespace)
          ?? (property ? index.resolve(property.type, usings, decl.namespace) : undefined)
          ?? (field ? index.resolve(field.type, usings, decl.namespace) : undefined);
        if (target) {
          addEdge(self, target.fullName, 'getservice', 'high',
            { file: assignment.file ?? file, line: assignment.line }, assignment.target);
          if (isViewModelName(target.name, conventions)) {
            diRegistered.add(target.fullName);
          }
        }
        continue;
      }

      if (value.kind === 'new') {
        const target = index.resolve(value.typeName, usings, decl.namespace);
        if (target) {
          addEdge(self, target.fullName, 'constructs', 'high',
            { file: assignment.file ?? file, line: assignment.line }, assignment.target);
        }
        continue;
      }

      if (value.kind === 'variable' && field) {
        const variableName = value.name;
        const parameter = decl.constructors
          .flatMap((constructor) => constructor.params)
          .find((candidate) => candidate.name === variableName);
        const target = index.resolve(field.type, usings, decl.namespace)
          ?? (parameter ? index.resolve(parameter.type, usings, decl.namespace) : undefined);
        if (target) {
          addEdge(self, target.fullName, 'field', 'high',
            { file: assignment.file ?? file, line: assignment.line }, assignment.target);
        }
      }
    }

    for (const creation of decl.creations) {
      const target = index.resolve(creation.typeName, usings, decl.namespace);
      const layer = target ? nodes.get(target.fullName)?.layer : undefined;
      // Only composed view models and services are interesting; `new List<T>()` and row models are noise.
      if (target && (layer === 'viewmodel' || layer === 'service')) {
        addEdge(self, target.fullName, 'composes', 'medium', { file: creation.file ?? file, line: creation.line });
      }
    }

    // A control that casts its data context to an INTERFACE binds that shape of view model without
    // naming a class (`DataContext is IGridCommands`). Class-typed casts are already handled by the
    // typed-property rule above, which carries the markup as evidence, so only interfaces are added
    // here — otherwise the same relationship would produce two edges with different evidence.
    for (const mention of decl.mentions) {
      if (!conventions.dataContextCasts.has(mention.via)) {
        continue;
      }
      const target = index.resolve(mention.name, usings, decl.namespace);
      if (target && target.kind === 'interface') {
        addEdge(self, target.fullName, 'binds', 'medium',
          { file: mention.file ?? file, line: mention.line }, 'DataContext');
      }
    }

    // Inheritance and interface implementation.
    for (const base of decl.bases) {
      const target = index.resolve(base, usings, decl.namespace);
      if (target) {
        addEdge(self, target.fullName, target.kind === 'interface' ? 'implements' : 'inherits',
          'high', { file, line: decl.line }, base);
        continue;
      }
      if (!base) {
        continue;
      }
      const external = ensureExternalNode(base);
      if (external) {
        // Decide from the simple name: `System.Collections.IEnumerable` is an interface even though
        // the qualified name it was written as begins with `S`.
        const simpleBase = base.slice(base.lastIndexOf('.') + 1);
        addEdge(self, external.id, /^I[A-Z]/.test(simpleBase) ? 'implements' : 'inherits',
          'low', { file, line: decl.line }, base);
      }
    }

    /* Plain type references. A property, field, return type or parameter typed as another workspace
       type is a dependency even though it carries no DI meaning — and it is what makes structural
       cycles visible. Self-references are skipped: a tree node holding its parent is not a cycle. */
    const emitReference = (rawType: string, memberName: string, line: number, origin: string): void => {
      for (const name of typeNamesIn(rawType)) {
        if (name === decl.name) {
          continue;
        }
        const target = index.resolve(name, usings, decl.namespace);
        if (!target || target.fullName === self) {
          continue;
        }
        addEdge(self, target.fullName, 'reference', 'medium', { file: origin, line }, memberName);
      }
    };

    for (const property of decl.properties) {
      emitReference(property.rawType, property.name, property.line, property.file ?? file);
    }
    for (const field of decl.fields) {
      emitReference(field.rawType, field.name, field.line, field.file ?? file);
    }
    for (const method of decl.methods) {
      emitReference(method.rawReturnType, method.name, method.line, method.file ?? file);
      for (const parameterType of method.rawParamTypes) {
        emitReference(parameterType, method.name, method.line, method.file ?? file);
      }
    }

    // Everything else the body mentions: locals, casts, `is`/`as`, events, attributes, constraints,
    // static access and catches. This is what turns the graph from a declaration map into a real
    // dependency map, and what makes structural cycles visible.
    for (const mention of decl.mentions) {
      if (mention.name === decl.name) {
        continue;
      }
      const target = index.resolve(mention.name, usings, decl.namespace);
      if (!target || target.fullName === self) {
        continue;
      }
      addEdge(self, target.fullName, 'reference', 'low',
        { file: mention.file ?? file, line: mention.line }, `${mention.via} ${mention.name}`);
    }
  }

  // XAML bindings confirm which view-model properties the markup actually uses.
  for (const xaml of xamlFiles) {
    if (!xaml.className) {
      continue;
    }
    const viewNode = nodes.get(xaml.className);
    const decl = index.get(xaml.className);
    if (!viewNode || !decl) {
      continue;
    }

    const usings = usingsFor(decl);
    const boundProperties = new Set<string>();

    for (const binding of xaml.bindings) {
      if (binding.inTemplate || !binding.root) {
        continue;
      }
      // A binding whose root is a named element addresses that element, not the view model.
      if (xaml.elementNames.includes(binding.root)) {
        continue;
      }

      const property = decl.properties.find((candidate) => candidate.name === binding.root);
      if (!property) {
        continue;
      }

      const target = index.resolve(property.type, usings, decl.namespace);
      if (!target) {
        continue;
      }

      boundProperties.add(binding.root);
      addEdge(xaml.className, target.fullName, 'binds', 'high',
        { file: xaml.path, line: binding.line }, binding.root);
    }

    // DataTemplate row models are recorded, but only as informational edges.
    for (const dataType of xaml.dataTypes) {
      const separator = dataType.value.indexOf(':');
      const prefix = separator > 0 ? dataType.value.slice(0, separator) : '';
      const local = separator > 0 ? dataType.value.slice(separator + 1) : dataType.value;
      const namespace = xaml.xmlnsUsing[prefix];
      const target = (namespace ? index.resolve(`${namespace}.${local}`, usings, decl.namespace) : undefined)
        ?? index.resolve(local, usings, decl.namespace);
      if (target) {
        addEdge(xaml.className, target.fullName, 'template', 'low',
          { file: xaml.path, line: dataType.line }, local);
      }
    }

    viewNode.viewModelProperties = decl.properties
      .filter((property) => isViewModelName(property.type, conventions))
      .map((property) => property.name);

    // A view that exposes its view model as a property is bound to it even when the markup carries no
    // `x:Bind`: `public ViewModel_X? ViewModel { get => DataContext as ...; }` *is* the binding, and in
    // a repository that binds with `{Binding}` it is the only place the relationship is written down.
    // Without this the pair existed only as a low-confidence type reference, and the canvas hides
    // those by default — so the view and its view model arrived unconnected.
    for (const property of decl.properties) {
      if (!isViewModelName(property.type, conventions)) {
        continue;
      }
      const target = index.resolve(property.type, usings, decl.namespace);
      if (!target) {
        continue;
      }
      addEdge(xaml.className, target.fullName, 'binds', 'high',
        { file: property.file ?? xaml.path, line: property.line }, property.name);
    }

    if (boundProperties.size > 0) {
      viewNode.boundViewModelProperties = [...boundProperties];
    }
  }

  for (const fullName of diRegistered) {
    const node = nodes.get(fullName);
    if (node) {
      node.diRegistered = true;
    }
  }

  report('Running the cleanup rules');
  const cleanup = computeFindings({
    nodes: [...nodes.values()],
    edges,
    decls: new Map([...entries].map(([fullName, entry]) => [fullName, entry.decl])),
    files: new Map([...entries].map(([fullName, entry]) => [fullName, entry.file])),
    declarationCounts,
    index,
    usingsFor: (fullName: string) => {
      const entry = entries.get(fullName);
      return entry ? usingsFor(entry.decl) : [];
    },
    xamlFiles,
    sources: [
      ...options.csharp.map((file) => file.text),
      ...options.xaml.map((file) => file.text),
    ],
    conventions,
  });

  for (const id of cleanup.inCycle) {
    const node = nodes.get(id);
    if (node) {
      node.inCycle = true;
    }
  }

  for (const finding of cleanup.findings) {
    for (const id of finding.nodeIds) {
      const node = nodes.get(id);
      if (node) {
        node.problemCount = (node.problemCount ?? 0) + 1;
      }
    }
  }

  const degree = new Map<string, number>();
  for (const edge of edges) {
    degree.set(edge.source, (degree.get(edge.source) ?? 0) + 1);
    degree.set(edge.target, (degree.get(edge.target) ?? 0) + 1);
  }

  const priority = (node: GraphNode): number => {
    switch (node.layer) {
      case 'view': return 0;
      case 'viewmodel': return 1;
      case 'service': return 2;
      case 'model': return 3;
      default: return 4;
    }
  };

  const allNodes = [...nodes.values()];
  const ranked = [...allNodes].sort((left, right) =>
    priority(left) - priority(right) || (degree.get(right.id) ?? 0) - (degree.get(left.id) ?? 0));

  const kept = ranked.length > maxNodes ? new Set(ranked.slice(0, maxNodes).map((node) => node.id)) : null;
  const nodesInModel = kept ? ranked.filter((node) => kept.has(node.id)) : ranked;

  /* The canvas shows one name per type, so two types sharing one are indistinguishable there: `App`
     exists in the app, in the mock service and in the deployment tool. `label` itself must stay the
     simple name — the symbol search matches against it and finding titles read from it — so the
     disambiguation lives in `displayLabel`, which only the canvas uses. Two types in the *same*
     module would still collide on the module name, so that case falls back to the full name. */
  const labelCounts = new Map<string, number>();
  const labelAndModule = new Map<string, number>();
  for (const node of nodesInModel) {
    labelCounts.set(node.label, (labelCounts.get(node.label) ?? 0) + 1);
    const key = `${node.label}|${node.module}`;
    labelAndModule.set(key, (labelAndModule.get(key) ?? 0) + 1);
  }
  const finalNodes = nodesInModel.map((node) => {
    if ((labelCounts.get(node.label) ?? 0) < 2) {
      return node;
    }
    // A placeholder module ("(root)" for a file at the repository root, "(external)" for a framework
    // type) says nothing about where the type lives, so a repeated label falls back to the full name.
    const placeholder = !node.module || node.module.startsWith('(');
    const shared = (labelAndModule.get(`${node.label}|${node.module}`) ?? 0) > 1;
    const suffix = shared || placeholder ? node.fullName : node.module;
    return { ...node, displayLabel: `${node.label} · ${suffix}` };
  });
  const finalEdges = kept ? edges.filter((edge) => kept.has(edge.source) && kept.has(edge.target)) : edges;

  const keptIds = new Set(finalNodes.map((node) => node.id));
  const finalFindings: Finding[] = cleanup.findings
    .map((finding) => ({
      ...finding,
      nodeIds: finding.nodeIds.filter((id) => keptIds.has(id)),
      edgeIds: finding.edgeIds?.filter((id) => finalEdges.some((edge) => edge.id === id)),
    }))
    // A finding whose nodes were all cut by the node cap would point at nothing.
    .filter((finding) => finding.nodeIds.length > 0);

  const count = (kind: Finding['kind']): number =>
    cleanup.findings.filter((finding) => finding.kind === kind).length;

  const simpleNames = new Map<string, Set<string>>();
  const namespaces = new Set<string>();
  const modules = new Set<string>();
  let typesDeclared = 0;
  for (const entry of entries.values()) {
    typesDeclared += declarationCounts.get(entry.decl.fullName) ?? 1;
    namespaces.add(entry.decl.namespace);
    modules.add(moduleOf(entry.file));
    const seen = simpleNames.get(entry.decl.name) ?? new Set<string>();
    seen.add(entry.decl.fullName);
    simpleNames.set(entry.decl.name, seen);
  }

  const parser = {
    typesDeclared,
    partialTypes: [...declarationCounts.values()].filter((declarations) => declarations > 1).length,
    ambiguousSimpleNames: [...simpleNames.values()].filter((names) => names.size > 1).length,
    namespaces: namespaces.size,
    modules: modules.size,
    xamlWithClass: xamlFiles.filter((file) => file.className !== null).length,
    xamlWithoutClass: xamlFiles.filter((file) => file.className === null).length,
    bindings: xamlFiles.reduce((sum, file) => sum + file.bindings.length, 0),
    templateBindings: xamlFiles.reduce(
      (sum, file) => sum + file.bindings.filter((binding) => binding.inTemplate).length,
      0,
    ),
    classicBindings: xamlFiles.reduce(
      (sum, file) => sum + file.bindings.filter((binding) => binding.dialect !== 'x:Bind').length,
      0,
    ),
    elementTags: xamlFiles.reduce((sum, file) => sum + file.elementTags.length, 0),
    handlerNames: xamlFiles.reduce((sum, file) => sum + file.handlerNames.length, 0),
    dataTypes: xamlFiles.reduce((sum, file) => sum + file.dataTypes.length, 0),
    edgesDropped,
    edgesMerged,
  };

  return {
    workspace: options.workspaceRoot,
    generatedAt: new Date().toISOString(),
    nodes: finalNodes,
    edges: finalEdges,
    findings: finalFindings,
    cleanup: {
      cycles: count('cycle'),
      deadBindings: count('dead-binding'),
      unboundViews: count('unbound-view'),
      unusedTypes: count('unused-type'),
      unusedMembers: count('unused-member'),
    },
    stats: {
      filesAnalysed: options.csharp.length + options.xaml.length,
      csharpFiles: options.csharp.length,
      xamlFiles: options.xaml.length,
      parseErrors,
      parseWarnings: warnings.length,
      elapsedMs: Date.now() - started,
      truncated: Boolean(kept),
      parser,
    },
    warnings,
    profileEvidence: deriveProfileEvidence(
      entries,
      xamlFiles,
      options.csharp.length,
      options.xaml.length,
    ),
  };
}
