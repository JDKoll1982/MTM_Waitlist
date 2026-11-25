import type { EdgeKind, Finding, GraphEdge, GraphNode, SourceLocation } from './model';
import type { TypeDecl } from './csharp/parser';
import type { TypeIndex } from './csharp/symbols';
import type { XamlFile } from './xaml/parser';

/** Edge kinds that represent a real dependency; inheritance and template rows are excluded. */
const CYCLE_EDGE_KINDS = new Set<EdgeKind>([
  'injects', 'getservice', 'resolves', 'binds', 'field', 'constructs', 'composes', 'reference',
]);

/** Base types known to add no bindable members, so a missing member is genuinely missing. */
const TRANSPARENT_BASES = new Set([
  'ObservableObject', 'ObservableRecipient', 'ObservableValidator', 'ObservableMessage',
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

  /* ------------------------------------------------------------ cycles */

  const cycleEdges = edges.filter((edge) => CYCLE_EDGE_KINDS.has(edge.kind));
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

    findings.push({
      id: nextId('cycle'),
      kind: 'cycle',
      severity: component.length > 3 ? 'high' : 'medium',
      title: `${component.length === 1 ? 'Self-dependency' : `Cycle of ${component.length}`}: ${names.join(' ↔ ')}`,
      detail: component.length === 1
        ? 'The type depends on itself.'
        : `${loopEdges.length} dependency edge${loopEdges.length === 1 ? '' : 's'} form a loop, so none of these can be`
          + ' constructed or resolved without the others. Break it by extracting the shared part into a type the loop can point at.',
      nodeIds: [...component],
      edgeIds: loopEdges.map((edge) => edge.id),
      location: locationOf(files, component[0], decls),
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
        // A row member may also be a whole-row binding like `{x:Bind}` with no path.
        if (binding.path.length === 0 && binding.root === '') {
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

      const rootProperty = viewDecl.properties.find((property) => property.name === binding.root);
      if (!rootProperty) {
        // The root is neither a view member nor a named element: the binding can only fail at runtime.
        const isTypeRoot = /^[A-Z]/.test(binding.root) && xaml.xmlnsUsing[binding.root.split(':')[0]] === undefined;
        findings.push({
          id: nextId('deadbinding'),
          kind: 'dead-binding',
          severity: 'high',
          title: `${xaml.className.split('.').pop()}: x:Bind ${binding.root} has no such member`,
          detail: `The markup binds \`${binding.expression}\` but ${viewDecl.name} declares no member named `
            + `\`${binding.root}\`${isTypeRoot ? ' and no element of that name exists' : ''}. `
            + 'This binding cannot resolve, so it fails silently at run time.',
          nodeIds: [xaml.className],
          location: { file: xaml.path, line: binding.line },
        });
        continue;
      }

      if (binding.path.length === 0) {
        continue;
      }
      const targetType = index.resolve(rootProperty.type, usings, viewDecl.namespace);
      if (!targetType) {
        continue;
      }

      const targetNode = nodeById.get(targetType.fullName);
      if (!targetNode || targetNode.layer === 'external') {
        continue;
      }

      const members = memberNamesOf(targetType.fullName, decls, index, usingsFor);
      if (!members) {
        // An unresolvable base type could supply the member, so no claim is made.
        continue;
      }

      const first = binding.path[0];
      if (members.has(first)) {
        continue;
      }

      findings.push({
        id: nextId('deadbinding'),
        kind: 'dead-binding',
        severity: 'medium',
        title: `${xaml.className.split('.').pop()}: x:Bind ${binding.root}.${first} is not on ${targetType.name}`,
        detail: `The markup binds \`${binding.expression}\`, but ${targetType.name} declares no member named `
          + `\`${first}\` and inherits none that could supply it. The binding is dead.`,
        nodeIds: [xaml.className, targetType.fullName],
        location: { file: xaml.path, line: binding.line },
      });
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

    const usings = usingsFor(view.id);
    const hasViewModelProperty = decl.properties.some((property) => property.name === 'ViewModel')
      || decl.fields.some((field) => field.name === 'ViewModel');

    if (hasViewModelProperty) {
      continue;
    }

    void usings;
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
      // Markup is not usage: a view with an x:Class but no registration and no navigation is dead.
      if (node.diRegistered) {
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
      : 'Nothing in the analysed files refers to it, by type name or by edge.';

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
    what: string;
  }

  const candidates: Candidate[] = [];
  const candidateNames = new Set<string>();

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
      candidates.push({ typeName: fullName, member: method.name, line: method.line, what: 'method' });
      candidateNames.add(method.name);
    }

    for (const field of decl.fields) {
      if (field.visibility !== 'private' || field.attributes.includes('ObservableProperty')) {
        continue;
      }
      if (field.isReadonly) {
        continue;
      }
      candidates.push({ typeName: fullName, member: field.name, line: field.line, what: 'field' });
      candidateNames.add(field.name);
    }

    for (const property of decl.properties) {
      if (property.visibility !== 'private' || property.isObservable) {
        continue;
      }
      candidates.push({ typeName: fullName, member: property.name, line: property.line, what: 'property' });
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
      location: files.get(candidate.typeName)
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
