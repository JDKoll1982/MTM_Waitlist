import type { EdgeKind, Finding, GraphEdge, GraphNode, SourceLocation } from './model';
import type { TypeDecl } from './csharp/parser';
import type { TypeIndex } from './csharp/symbols';
import type { XamlFile } from './xaml/parser';

/** Edge kinds that represent a real dependency; inheritance and template rows are excluded. */
const CYCLE_EDGE_KINDS = new Set<EdgeKind>([
  'injects', 'getservice', 'resolves', 'binds', 'field', 'constructs', 'composes', 'reference',
]);

/**
 * Base types known to add no bindable members, so a missing member is genuinely missing.
 *
 * Framework bases matter here: a WinUI page derives from `Page`, a WPF page from `Page` or
 * `UserControl`, a MAUI page from `ContentPage`. None of them declares application members, and none
 * of them is declared in the workspace, so without this list every view would look like it had an
 * unresolvable base and no dead binding could ever be reported.
 */
const TRANSPARENT_BASES = new Set([
  'ObservableObject', 'ObservableRecipient', 'ObservableValidator', 'ObservableMessage',
  'BindableBase', 'ViewModelBase',
  'Page', 'UserControl', 'Window', 'WindowEx', 'Application', 'ContentPage', 'ContentView',
  'ContentDialog', 'Form', 'Control', 'FrameworkElement', 'Component', 'UserControlBase',
]);

const ENTRY_POINT_NAMES = new Set(['App', 'Program', 'Startup', 'Main']);

const MAX_BASE_DEPTH = 8;

export interface FindingInput {
  nodes: GraphNode[];
  edges: GraphEdge[];
  /** Merged declarations, keyed by full name. */
  decls: Map<string, TypeDecl>;
  /** File each type was declared in, keyed by full name. */
  files: Map<string, string>;
  /** How many files declare each type; a partial type appears in more than one. */
  declarationCounts: Map<string, number>;
  index: TypeIndex;
  usingsFor: (fullName: string) => string[];
  xamlFiles: XamlFile[];
  /** Every analysed source (C# and XAML), for member-usage counting. */
  sources: string[];
}

export interface FindingResult {
  findings: Finding[];
  /** Node ids that are part of a dependency cycle. */
  inCycle: Set<string>;
}

/**
 * Tarjan's strongly connected components.
 *
 * Recursion is avoided deliberately: a deep dependency chain in a large workspace would otherwise
 * overflow the stack inside the extension host.
 */
function stronglyConnected(nodeIds: string[], adjacency: Map<string, string[]>): string[][] {
  const index = new Map<string, number>();
  const low = new Map<string, number>();
  const onStack = new Set<string>();
  const stack: string[] = [];
  const components: string[][] = [];
  let counter = 0;

  for (const start of nodeIds) {
    if (index.has(start)) {
      continue;
    }

    const frame: { id: string; edge: number }[] = [{ id: start, edge: 0 }];
    index.set(start, counter);
    low.set(start, counter);
    counter += 1;
    stack.push(start);
    onStack.add(start);

    while (frame.length > 0) {
      const top = frame[frame.length - 1];
      const neighbours = adjacency.get(top.id) ?? [];

      if (top.edge < neighbours.length) {
        const next = neighbours[top.edge];
        top.edge += 1;

        if (!index.has(next)) {
          index.set(next, counter);
          low.set(next, counter);
          counter += 1;
          stack.push(next);
          onStack.add(next);
          frame.push({ id: next, edge: 0 });
        } else if (onStack.has(next)) {
          low.set(top.id, Math.min(low.get(top.id)!, index.get(next)!));
        }
        continue;
      }

      frame.pop();
      if (frame.length > 0) {
        const parent = frame[frame.length - 1];
        low.set(parent.id, Math.min(low.get(parent.id)!, low.get(top.id)!));
      }

      if (low.get(top.id) === index.get(top.id)) {
        const component: string[] = [];
        while (stack.length > 0) {
          const id = stack.pop() as string;
          onStack.delete(id);
          component.push(id);
          if (id === top.id) {
            break;
          }
        }
        components.push(component);
      }
    }
  }

  return components;
}

/**
 * Members the MVVM source generators add to a type.
 *
 * `[RelayCommand]` on `Foo()` declares `FooCommand`; on `FooAsync()` it declares `FooCommand` (the
 * toolkit strips the trailing `Async`). `[ObservableProperty]` on `_foo` declares `Foo`. Without
 * these, every `x:Bind ViewModel.SomeCommand` in the workspace reads as a dead binding.
 */
function generatedMembers(decl: TypeDecl): string[] {
  const names: string[] = [];

  for (const method of decl.methods) {
    if (!method.attributes.includes('RelayCommand')) {
      continue;
    }
    const stem = method.name.endsWith('Async') ? method.name.slice(0, -'Async'.length) : method.name;
    names.push(`${method.name}Command`, `${stem}Command`);
  }

  for (const field of decl.fields) {
    if (!field.attributes.includes('ObservableProperty')) {
      continue;
    }
    const trimmed = field.name.replace(/^_+/, '');
    if (trimmed.length > 0) {
      names.push(trimmed.charAt(0).toUpperCase() + trimmed.slice(1));
    }
  }

  return names;
}

function memberNamesOf(
  fullName: string,
  decls: Map<string, TypeDecl>,
  index: TypeIndex,
  usingsFor: (name: string) => string[],
  depth = 0,
): Set<string> | null {
  const decl = decls.get(fullName);
  if (!decl || depth > MAX_BASE_DEPTH) {
    return null;
  }

  const names = new Set<string>();
  for (const property of decl.properties) {
    names.add(property.name);
  }
  for (const field of decl.fields) {
    names.add(field.name);
  }
  for (const method of decl.methods) {
    names.add(method.name);
  }
  for (const generated of generatedMembers(decl)) {
    names.add(generated);
  }

  for (const base of decl.bases) {
    const simple = base.slice(base.lastIndexOf('.') + 1);
    if (TRANSPARENT_BASES.has(simple)) {
      continue;
    }
    const resolved = index.resolve(base, usingsFor(fullName), decl.namespace);
    if (!resolved) {
      // An unknown base could supply the member, so nothing below it can be judged missing.
      return null;
    }
    const inherited = memberNamesOf(resolved.fullName, decls, index, usingsFor, depth + 1);
    if (!inherited) {
      return null;
    }
    for (const name of inherited) {
      names.add(name);
    }
  }

  return names;
}

/** Counts occurrences of the given identifiers across every source, in one pass. */
function countIdentifiers(sources: string[], candidates: Set<string>): Map<string, number> {
  const counts = new Map<string, number>();
  if (candidates.size === 0) {
    return counts;
  }

  const pattern = /[A-Za-z_][A-Za-z0-9_]*/g;
  for (const source of sources) {
    pattern.lastIndex = 0;
    let match = pattern.exec(source);
    while (match !== null) {
      const word = match[0];
      if (candidates.has(word)) {
        counts.set(word, (counts.get(word) ?? 0) + 1);
      }
      match = pattern.exec(source);
    }
  }

  return counts;
}

function locationOf(files: Map<string, string>, fullName: string, decls: Map<string, TypeDecl>): SourceLocation | undefined {
  const file = files.get(fullName);
  const decl = decls.get(fullName);
  if (!file || !decl) {
    return undefined;
  }
  return { file, line: decl.line };
}

/**
 * True when the given edges form a cycle inside this component. Used to tell a real construction
 * loop (nothing in it can be built first) from a group that merely contains a few construction
 * edges alongside references.
 */
function hasCycleWithin(component: string[], edges: GraphEdge[]): boolean {
  if (edges.length === 0) {
    return false;
  }
  const adjacency = new Map<string, string[]>();
  for (const edge of edges) {
    const list = adjacency.get(edge.source);
    if (list) {
      list.push(edge.target);
    } else {
      adjacency.set(edge.source, [edge.target]);
    }
  }

  // 0 = unseen, 1 = on the current path, 2 = finished.
  const state = new Map<string, number>();
  const visit = (id: string): boolean => {
    state.set(id, 1);
    for (const next of adjacency.get(id) ?? []) {
      const seen = state.get(next) ?? 0;
      if (seen === 1) {
        return true;
      }
      if (seen === 0 && visit(next)) {
        return true;
      }
    }
    state.set(id, 2);
    return false;
  };

  for (const id of component) {
    if ((state.get(id) ?? 0) === 0 && visit(id)) {
      return true;
    }
  }
  return false;
}

/** Builds every cleanup finding the graph can support. */
export function computeFindings(input: FindingInput): FindingResult {
  const { nodes, edges, decls, files, declarationCounts, index, usingsFor, xamlFiles, sources } = input;
  const nodeById = new Map(nodes.map((node) => [node.id, node]));
  const findings: Finding[] = [];
  let sequence = 0;
  const nextId = (kind: string): string => {
    sequence += 1;
    return `f${sequence}_${kind}`;
  };

  /**
   * The declared type of a member, walking the base chain so an inherited property still resolves.
   * Used to follow a binding path one segment at a time.
   */
  const memberType = (member: string, typeFullName: string, depth = 0): TypeDecl | undefined => {
    const decl = decls.get(typeFullName);
    if (!decl || depth > MAX_BASE_DEPTH) {
      return undefined;
    }
    const property = decl.properties.find((candidate) => candidate.name === member);
    const field = decl.fields.find((candidate) => candidate.name === member);
    const rawType = property?.type ?? field?.type;
    if (rawType) {
      return index.resolve(rawType, usingsFor(typeFullName), decl.namespace);
    }
    for (const base of decl.bases) {
      const simple = base.slice(base.lastIndexOf('.') + 1);
      if (TRANSPARENT_BASES.has(simple)) {
        continue;
      }
      const resolved = index.resolve(base, usingsFor(typeFullName), decl.namespace);
      if (resolved) {
        const inherited = memberType(member, resolved.fullName, depth + 1);
        if (inherited) {
          return inherited;
        }
      }
    }
    return undefined;
  };

  /* ------------------------------------------------------------ cycles */

  // Low-confidence references — `static access`, `typeof`, an attribute — are emitted for the
  // commonest idioms in MVVM code (`App.Current`, `Settings.Default`). Feeding them into the
  // strongly-connected-component pass merges half an application into one unusable cycle, so they
  // stay out of it. Declared member types (medium confidence) still count: those are real structure.
  const cycleEdges = edges.filter((edge) =>
    CYCLE_EDGE_KINDS.has(edge.kind)
    && !(edge.kind === 'reference' && edge.confidence === 'low'));
  const adjacency = new Map<string, string[]>();
  for (const edge of cycleEdges) {
    if (!nodeById.has(edge.source) || !nodeById.has(edge.target)) {
      continue;
    }
    const list = adjacency.get(edge.source);
    if (list) {
      list.push(edge.target);
    } else {
      adjacency.set(edge.source, [edge.target]);
    }
  }

  const inCycle = new Set<string>();
  const components = stronglyConnected([...adjacency.keys()], adjacency);

  for (const component of components) {
    const members = new Set(component);
    const selfLoop = component.length === 1
      && (adjacency.get(component[0]) ?? []).includes(component[0]);
    if (component.length < 2 && !selfLoop) {
      continue;
    }

    const loopEdges = cycleEdges.filter((edge) => members.has(edge.source) && members.has(edge.target));
    for (const id of component) {
      inCycle.add(id);
    }

    const names = component
      .map((id) => nodeById.get(id)?.label ?? id)
      .sort((left, right) => left.localeCompare(right));

    // A cycle of `injects`/`constructs` edges really does prevent independent construction. A cycle
    // made only of `reference` edges — static access, `typeof`, a member's type — is a mutual
    // dependency but nothing stops either side being built, so it must not claim otherwise.
    const constructionKinds = new Set(['injects', 'constructs', 'composes', 'resolves', 'getservice', 'field', 'binds']);
    const constructionEdges = loopEdges.filter((edge) => constructionKinds.has(edge.kind));
    const isConstructionLoop = hasCycleWithin(component, constructionEdges);

    const edgeWord = (count: number): string => `${count} ${count === 1 ? 'edge' : 'edges'}`;
    const loopText = isConstructionLoop
      ? `${edgeWord(loopEdges.length)} form a loop, ${constructionEdges.length} of them binding construction or`
        + ' resolution, and those construction edges form a loop of their own — so none of these types can be'
        + ' built first. Break it by extracting the shared part into a type the loop can point at.'
      : constructionEdges.length > 0
        ? `${edgeWord(loopEdges.length)} form a loop, ${constructionEdges.length} of them binding construction or`
          + ' resolution. Those construction edges do not themselves form a loop, so no type here is blocked from'
          + ' being built; the loop closes through a reference back, and that is what to unpick.'
        : `${edgeWord(loopEdges.length)} — all type references — form a loop: static access, \`typeof\`, or a member`
          + ' typed as the other side. Nothing prevents these types being built, so this is a mutual dependency'
          + ' rather than a build failure: move the shared constant, helper or factory out of the loop.';

    findings.push({
      id: nextId('cycle'),
      kind: 'cycle',
      severity: isConstructionLoop ? 'high' : 'medium',
      title: `${component.length === 1 ? 'Self-dependency' : `Cycle of ${component.length}`}: ${names.join(' ↔ ')}`,
      detail: component.length === 1 ? 'The type depends on itself.' : loopText,
      nodeIds: [...component],
      edgeIds: loopEdges.map((edge) => edge.id),
      location: locationOf(files, component[0], decls),
    });
  }

  /* ------------------------------------- reference-closure groups */

  /*
   * Some genuine mutual references are closed entirely by low-confidence edges — `A.X` calling
   * `B.Y` that calls back into `A`, or two `typeof` constants. Those were excluded above, so run a
   * second pass over the full graph and report what it adds:
   *
   * - a small group (a real, actionable knot) is reported as a low-severity cycle;
   * - a large group is reported once, with a sample of its members, because a 99-type finding with
   *   every name in the title is unreadable and paints the whole canvas red.
   */
  const REFERENCE_GROUP_SAMPLE = 30;
  const fullAdjacency = new Map<string, string[]>();
  for (const edge of edges) {
    if (!CYCLE_EDGE_KINDS.has(edge.kind)) {
      continue;
    }
    if (!nodeById.has(edge.source) || !nodeById.has(edge.target)) {
      continue;
    }
    const list = fullAdjacency.get(edge.source);
    if (list) {
      list.push(edge.target);
    } else {
      fullAdjacency.set(edge.source, [edge.target]);
    }
  }

  const alreadyReported = new Set(inCycle);
  for (const component of stronglyConnected([...fullAdjacency.keys()], fullAdjacency)) {
    if (component.length < 2 || component.every((id) => alreadyReported.has(id))) {
      continue;
    }

    const members = new Set(component);
    // Only the reference edges belong to this finding: the group is defined by them, and mixing in
    // construction edges would make a low-severity reference note look like a construction loop.
    const loopEdges = edges.filter((edge) =>
      edge.kind === 'reference' && members.has(edge.source) && members.has(edge.target));
    const sampled = [...component].sort((left, right) => left.localeCompare(right)).slice(0, REFERENCE_GROUP_SAMPLE);
    for (const id of sampled) {
      inCycle.add(id);
    }

    const names = sampled.map((id) => nodeById.get(id)?.label ?? id);
    const truncated = component.length > sampled.length;
    findings.push({
      id: nextId('cycle'),
      kind: 'cycle',
      severity: 'low',
      title: component.length <= 3
        ? `Cycle of ${component.length}: ${names.join(' ↔ ')}`
        : `Mutual references across ${component.length} types: ${names.slice(0, 4).join(' ↔ ')}${truncated ? ' …' : ''}`,
      detail: `${component.length} types reach each other only through low-confidence references — static`
        + ' access, `typeof`, or an attribute. Nothing here blocks construction, so this is a tangle of'
        + ' shared constants and helpers rather than a dependency loop to break.'
        + (truncated ? ` Only ${sampled.length} members are listed and highlighted so the canvas stays readable.` : ''),
      nodeIds: sampled,
      edgeIds: loopEdges.filter((edge) => sampled.includes(edge.source) && sampled.includes(edge.target))
        .map((edge) => edge.id),
      location: locationOf(files, sampled[0], decls),
    });
  }

  /* ------------------------------------------------- dead bindings */

  const viewByClass = new Map<string, XamlFile>();
  for (const xaml of xamlFiles) {
    if (xaml.className) {
      viewByClass.set(xaml.className, xaml);
    }
  }

  const bindingChecked = new Set<string>();

  for (const xaml of xamlFiles) {
    if (!xaml.className) {
      continue;
    }
    const viewDecl = decls.get(xaml.className);
    if (!viewDecl) {
      continue;
    }

    const usings = usingsFor(xaml.className);

    for (const binding of xaml.bindings) {
      if (!binding.root) {
        continue;
      }
      if (xaml.elementNames.includes(binding.root)) {
        continue;
      }

      const key = `${xaml.className}|${binding.root}|${binding.path.join('.')}|${binding.templateDataType ?? ''}`;
      if (bindingChecked.has(key)) {
        continue;
      }
      bindingChecked.add(key);

      // Inside a DataTemplate the binding targets the row, not the page, so it is judged against
      // the template's own x:DataType rather than against the view.
      if (binding.inTemplate) {
        if (!binding.templateDataType) {
          continue;
        }
        const templateType = binding.templateTypeNamespace
          ? index.resolve(binding.templateTypeNamespace, usings, viewDecl.namespace)
            ?? index.resolve(binding.templateDataType, usings, viewDecl.namespace)
          : index.resolve(binding.templateDataType, usings, viewDecl.namespace);
        if (!templateType) {
          continue;
        }
        const templateMembers = memberNamesOf(templateType.fullName, decls, index, usingsFor);
        if (!templateMembers || templateMembers.has(binding.root)) {
          // Either an unresolvable base could supply it, or the member is genuinely there.
          continue;
        }
        findings.push({
          id: nextId('deadbinding'),
          kind: 'dead-binding',
          severity: 'medium',
          title: `${templateType.name}: x:Bind ${binding.root} is not on the template's row type`,
          detail: `${xaml.path} binds \`${binding.expression}\` inside a DataTemplate whose x:DataType is `
            + `${binding.templateDataType}, but ${templateType.name} declares no member named `
            + `\`${binding.root}\` and inherits none that could supply it. The binding is dead.`,
          nodeIds: [templateType.fullName],
          location: { file: xaml.path, line: binding.line },
        });
        continue;
      }

      // The root is judged against everything the view can offer, not only its properties: x:Bind
      // resolves fields, methods and the members the MVVM generators add (`SaveCommand` from
      // `[RelayCommand] Save`), and a function binding root must be a method.
      const viewMembers = memberNamesOf(xaml.className, decls, index, usingsFor);
      if (!viewMembers) {
        // An unresolvable base could supply the member, so no claim is made.
        continue;
      }

      if (!viewMembers.has(binding.root)) {
        const isTypeRoot = /^[A-Z]/.test(binding.root) && xaml.xmlnsUsing[binding.root.split(':')[0]] === undefined;
        findings.push({
          id: nextId('deadbinding'),
          kind: 'dead-binding',
          severity: 'high',
          title: `${xaml.className.split('.').pop()}: x:Bind ${binding.root} has no such member`,
          detail: `The markup binds \`${binding.expression}\` but ${viewDecl.name} declares no property, field`
            + `, method or generated member named \`${binding.root}\`${isTypeRoot ? ' and no element of that name exists' : ''}. `
            + 'This binding cannot resolve, so it fails silently at run time.',
          nodeIds: [xaml.className],
          location: { file: xaml.path, line: binding.line },
        });
        continue;
      }

      // Walk the rest of the path while each step resolves, so a broken later segment is reported
      // against the type that should have supplied it rather than going unnoticed.
      let current = memberType(binding.root, xaml.className);
      for (let depth = 0; depth < binding.path.length; depth += 1) {
        if (!current) {
          break;
        }
        const segment = binding.path[depth].replace(/\(.*$/, '').trim();
        if (!segment) {
          break;
        }
        const members = memberNamesOf(current.fullName, decls, index, usingsFor);
        if (!members) {
          break;
        }
        if (!members.has(segment)) {
          const failingPath = [binding.root, ...binding.path.slice(0, depth + 1)].join('.');
          findings.push({
            id: nextId('deadbinding'),
            kind: 'dead-binding',
            severity: 'medium',
            title: `${xaml.className.split('.').pop()}: x:Bind ${failingPath} is not on ${current.name}`,
            detail: `The markup binds \`${binding.expression}\`, but ${current.name} declares no member named `
              + `\`${segment}\` and inherits none that could supply it. The binding is dead.`,
            nodeIds: [xaml.className, current.fullName],
            location: { file: xaml.path, line: binding.line },
          });
          break;
        }
        current = memberType(segment, current.fullName);
      }
    }
  }

  /* ------------------------------------------------- unbound views */

  const viewNodes = nodes.filter((node) => node.layer === 'view' && node.xamlFile);
  for (const view of viewNodes) {
    const xaml = viewByClass.get(view.id);
    const decl = decls.get(view.id);
    if (!xaml || !decl) {
      continue;
    }

    const bindsViewModel = xaml.bindings.some((binding) => binding.root === 'ViewModel');
    if (!bindsViewModel) {
      continue;
    }

    const hasViewModelProperty = decl.properties.some((property) => property.name === 'ViewModel')
      || decl.fields.some((field) => field.name === 'ViewModel');

    if (hasViewModelProperty) {
      continue;
    }

    findings.push({
      id: nextId('unboundview'),
      kind: 'unbound-view',
      severity: 'high',
      title: `${view.label} binds ViewModel but declares none`,
      detail: `${xaml.path} binds \`ViewModel.*\` throughout, but ${decl.name} has no property of a view-model type. `
        + 'Every one of those bindings fails.',
      nodeIds: [view.id],
      location: { file: xaml.path, line: 1 },
    });
  }

  /* ------------------------------------------------- unused types */

  const incoming = new Map<string, number>();
  for (const edge of edges) {
    if (edge.kind === 'template') {
      continue;
    }
    incoming.set(edge.target, (incoming.get(edge.target) ?? 0) + 1);
  }

  const typeofUsed = new Set<string>();
  for (const [fullName, decl] of decls) {
    const usings = usingsFor(fullName);
    for (const reference of decl.typeofRefs) {
      const resolved = index.resolve(reference, usings, decl.namespace);
      if (resolved) {
        typeofUsed.add(resolved.fullName);
      }
    }
  }

  const unusedTypes = new Set<string>();

  /**
   * Element names hosted by some view's markup. A UserControl is used this way — `<views:SetUpPanel/>`
   * inside `SignInPage.xaml` — and that is real usage, not a dormant declaration.
   */
  const hostedElements = new Set<string>();
  for (const xaml of xamlFiles) {
    const ownName = xaml.className ? xaml.className.split('.').pop() : undefined;
    for (const tag of xaml.elementTags) {
      if (tag !== ownName) {
        hostedElements.add(tag);
      }
    }
  }

  // A graph edge is not the only way one type mentions another: a return type, a generic argument,
  // a `new` expression and a XAML `x:Class` all count. Counting identifier occurrences once across the
  // corpus catches every one of them, which keeps the unused list to genuinely dead types.
  const typeCandidates = new Set<string>();
  for (const node of nodes) {
    if (node.layer !== 'external') {
      typeCandidates.add(node.label);
    }
  }
  const typeOccurrences = countIdentifiers(sources, typeCandidates);

  // An extension-method holder is used through its methods, never through its own name, so a type
  // whose extension methods are called elsewhere must not be reported as dead.
  const extensionHolders = new Map<string, string[]>();
  const extensionNames = new Set<string>();
  for (const [fullName, decl] of decls) {
    const names = decl.methods.filter((method) => method.isExtension).map((method) => method.name);
    if (names.length > 0) {
      extensionHolders.set(fullName, names);
      for (const name of names) {
        extensionNames.add(name);
      }
    }
  }
  const extensionOccurrences = countIdentifiers(sources, extensionNames);

  const calledExtensionHolder = (fullName: string): boolean =>
    (extensionHolders.get(fullName) ?? []).some((name) => (extensionOccurrences.get(name) ?? 0) > 1);

  for (const node of nodes) {
    if (node.layer === 'external') {
      continue;
    }
    if (ENTRY_POINT_NAMES.has(node.label)) {
      continue;
    }
    if ((incoming.get(node.id) ?? 0) > 0) {
      continue;
    }
    if (typeofUsed.has(node.id)) {
      continue;
    }

    if (node.layer === 'view') {
      // Registration is usage, and so is being hosted: a UserControl appears in another view's
      // markup as an element (`<views:SetUpPanel />`), which no C# graph edge records. Only a view
      // that is neither registered nor hosted anywhere is genuinely dead.
      if (node.diRegistered) {
        continue;
      }
      if (hostedElements.has(node.label)) {
        continue;
      }
    } else {
      const declarations = declarationCounts.get(node.id) ?? 1;
      const occurrences = typeOccurrences.get(node.label) ?? 0;
      if (occurrences > declarations) {
        // Mentioned somewhere the graph does not draw: a return type, a generic argument, a `new`.
        continue;
      }
      if (calledExtensionHolder(node.id)) {
        continue;
      }
      if (node.diRegistered) {
        continue;
      }
    }

    unusedTypes.add(node.id);

    const reason = node.layer === 'view'
      ? 'It is not registered with the container and nothing navigates to it.'
      : 'Nothing in the analysed files refers to it, by type name or by edge. Test projects are '
        + 'excluded from the analysis, so a type used only by its tests is reported here — check that '
        + 'before treating it as dead.';

    findings.push({
      id: nextId('unusedtype'),
      kind: 'unused-type',
      severity: 'medium',
      title: `Unused ${node.layer === 'view' ? 'view' : node.kind}: ${node.label}`,
      detail: `${node.fullName} is never referenced. ${reason}`
        + (node.layer === 'view' ? ' A view is normally reached by navigation, so check its registration.' : ''),
      nodeIds: [node.id],
      location: node.location,
    });
  }

  /* ------------------------------------------------- unused members */

  interface Candidate {
    typeName: string;
    member: string;
    line: number;
    /** The file that declared the member; a partial type spreads its members over several. */
    file?: string;
    what: string;
  }

  const candidates: Candidate[] = [];
  const candidateNames = new Set<string>();

  /** Members markup binds to or handles, so the identifier count is not the only evidence. */
  const handlerNames = new Set<string>(
    xamlFiles.flatMap((xaml) => xaml.handlerNames),
  );
  for (const xaml of xamlFiles) {
    for (const binding of xaml.bindings) {
      if (binding.root) {
        handlerNames.add(binding.root);
      }
      for (const segment of binding.path) {
        handlerNames.add(segment.replace(/\(.*$/, '').trim());
      }
    }
  }

  for (const [fullName, decl] of decls) {
    // A member of an unused type is reported once, as the type, not again per member.
    if (unusedTypes.has(fullName)) {
      continue;
    }

    for (const method of decl.methods) {
      if (method.visibility !== 'private' || method.isOverride || method.isPartial) {
        continue;
      }
      if (method.attributes.some((name) => name === 'RelayCommand' || name === 'Conditional')) {
        continue;
      }
      if (method.name.startsWith('get_') || method.name.startsWith('set_')) {
        continue;
      }
      // An application entry point is invoked by the runtime, never by code in the corpus, so it can
      // never look used. `Main` is only an entry point when it is static or on a type named Program.
      if (method.name === 'Main' && (method.isStatic || decl.name === 'Program')) {
        continue;
      }
      // A handler named by markup is invoked by XAML, not by a call the identifier count can see.
      if (handlerNames.has(method.name)) {
        continue;
      }
      candidates.push({ typeName: fullName, member: method.name, line: method.line, file: method.file, what: 'method' });
      candidateNames.add(method.name);
    }

    for (const field of decl.fields) {
      if (field.visibility !== 'private' || field.attributes.includes('ObservableProperty')) {
        continue;
      }
      if (handlerNames.has(field.name)) {
        continue;
      }
      if (field.isReadonly) {
        continue;
      }
      candidates.push({ typeName: fullName, member: field.name, line: field.line, file: field.file, what: 'field' });
      candidateNames.add(field.name);
    }

    for (const property of decl.properties) {
      if (property.visibility !== 'private' || property.isObservable) {
        continue;
      }
      if (handlerNames.has(property.name)) {
        continue;
      }
      candidates.push({ typeName: fullName, member: property.name, line: property.line, file: property.file, what: 'property' });
      candidateNames.add(property.name);
    }
  }

  const counts = countIdentifiers(sources, candidateNames);

  for (const candidate of candidates) {
    const occurrences = counts.get(candidate.member) ?? 0;
    // One occurrence is the declaration itself, so anything at one or below is unreferenced.
    if (occurrences > 1) {
      continue;
    }

    const decl = decls.get(candidate.typeName);
    findings.push({
      id: nextId('unusedmember'),
      kind: 'unused-member',
      severity: 'low',
      title: `Unused ${candidate.what}: ${decl?.name ?? candidate.typeName}.${candidate.member}`,
      detail: `The private ${candidate.what} \`${candidate.member}\` is declared but never read or called anywhere in`
        + ' the analysed files.',
      nodeIds: [candidate.typeName],
      // The member's own file, not the type's: a partial type declares members in several files, and
      // naming the first one sends the reader to a line that does not exist there.
      location: candidate.file
        ? { file: candidate.file, line: candidate.line }
        : files.get(candidate.typeName)
          ? { file: files.get(candidate.typeName) as string, line: candidate.line }
          : undefined,
    });
  }

  /* ------------------------------------------------------------ rank */

  const severityRank: Record<Finding['severity'], number> = { high: 0, medium: 1, low: 2 };
  findings.sort((left, right) =>
    severityRank[left.severity] - severityRank[right.severity]
    || left.kind.localeCompare(right.kind)
    || left.title.localeCompare(right.title));

  return { findings, inCycle };
}
