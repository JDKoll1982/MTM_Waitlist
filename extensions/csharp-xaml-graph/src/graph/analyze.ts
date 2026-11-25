import type { EdgeConfidence, Finding, GraphEdge, GraphModel, GraphNode, NodeLayer } from './model';
import type { CSharpFile, TypeDecl } from './csharp/parser';
import { initCSharpGrammar, parseCSharpFile } from './csharp/parser';
import { TypeIndex, moduleOf } from './csharp/symbols';
import { classifyType } from './layers';
import { computeFindings } from './findings';
import type { XamlFile } from './xaml/parser';
import { parseXamlFile } from './xaml/parser';

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
}

interface Entry {
  decl: TypeDecl;
  file: string;
}

const VIEW_ROOT_KIND: Record<string, string> = {
  Page: 'page',
  UserControl: 'usercontrol',
  Window: 'window',
  WindowEx: 'window',
  windowex: 'window',
};

/** Merges every declaration of a partial type into one record. */
function mergeDeclarations(files: CSharpFile[]): Map<string, Entry> {
  const merged = new Map<string, Entry>();

  for (const file of files) {
    for (const decl of file.types) {
      const existing = merged.get(decl.fullName);
      if (!existing) {
        merged.set(decl.fullName, {
          decl: {
            ...decl,
            constructors: [...decl.constructors],
            properties: [...decl.properties],
            fields: [...decl.fields],
            assignments: [...decl.assignments],
            calls: [...decl.calls],
            creations: [...decl.creations],
            bases: [...decl.bases],
          },
          file: file.path,
        });
        continue;
      }

      const target = existing.decl;
      target.constructors.push(...decl.constructors);
      target.properties.push(...decl.properties);
      target.fields.push(...decl.fields);
      target.assignments.push(...decl.assignments);
      target.calls.push(...decl.calls);
      target.creations.push(...decl.creations);
      for (const base of decl.bases) {
        if (!target.bases.includes(base)) {
          target.bases.push(base);
        }
      }
      target.isPartial = target.isPartial || decl.isPartial;
      target.isAbstract = target.isAbstract || decl.isAbstract;
    }
  }

  return merged;
}

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

/** Builds the whole dependency model from parsed sources. */
export async function analyzeWorkspace(options: AnalyzeOptions): Promise<GraphModel> {
  const started = Date.now();
  const includeExternalTypes = options.includeExternalTypes !== false;
  const maxNodes = options.maxNodes && options.maxNodes > 0 ? options.maxNodes : 4000;

  await initCSharpGrammar(options.grammarWasmPath);

  const warnings: string[] = [];
  const csharpFiles: CSharpFile[] = [];
  let parseErrors = 0;

  for (const file of options.csharp) {
    try {
      const parsed = parseCSharpFile(file.path, file.text);
      if (parsed.hasSyntaxError) {
        parseErrors += 1;
      }
      csharpFiles.push(parsed);
    } catch (error) {
      warnings.push(`${file.path}: ${(error as Error).message}`);
    }
  }

  const xamlFiles: XamlFile[] = [];
  for (const file of options.xaml) {
    try {
      xamlFiles.push(parseXamlFile(file.path, file.text));
    } catch (error) {
      warnings.push(`${file.path}: ${(error as Error).message}`);
    }
  }

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

  const ensureTypeNode = (decl: TypeDecl, file: string, layer?: NodeLayer): GraphNode => {
    const existing = nodes.get(decl.fullName);
    if (existing) {
      if (layer) {
        existing.layer = layer;
      }
      return existing;
    }
    const node: GraphNode = {
      id: decl.fullName,
      label: decl.name,
      fullName: decl.fullName,
      layer: layer ?? classifyType(decl),
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
      return;
    }
    const key = `${source}|${target}|${kind}`;
    if (edgeKeys.has(key)) {
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

    for (const call of decl.calls) {
      // pageService.Configure<TViewModel, TPage>()
      if (call.method === 'Configure' && call.typeArgs.length >= 2) {
        const viewModel = index.resolve(call.typeArgs[0], usings, decl.namespace);
        const page = index.resolve(call.typeArgs[1], usings, decl.namespace);
        if (viewModel && page) {
          diRegistered.add(viewModel.fullName);
          addEdge(page.fullName, viewModel.fullName, 'resolves', 'high', { file, line: call.line });
        }
        continue;
      }

      // services.AddTransient<TViewModel>() and friends
      if (/^Add[A-Z]/.test(call.method) && call.typeArgs.length === 1) {
        const registered = index.resolve(call.typeArgs[0], usings, decl.namespace);
        if (registered) {
          diRegistered.add(registered.fullName);
        }
        continue;
      }

      // services.AddSingleton<TInterface, TImplementation>() — the implementation serves the interface,
      // which is a real relationship the graph should carry, and it keeps the interface from looking dead.
      if (/^Add[A-Z]/.test(call.method) && call.typeArgs.length >= 2) {
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
            'medium', { file, line: call.line }, 'container registration');
        }
      }
    }

    // Constructor parameters: the view-model and service edges that carry the most weight.
    for (const constructor of decl.constructors) {
      for (const parameter of constructor.params) {
        const target = index.resolve(parameter.type, usings, decl.namespace);
        if (target) {
          addEdge(self, target.fullName, 'injects', 'high', { file, line: constructor.line });
        } else {
          const external = ensureExternalNode(parameter.type);
          if (external) {
            addEdge(self, external.id, 'injects', 'low', { file, line: constructor.line });
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
            { file, line: assignment.line }, assignment.target);
          if (target.name.endsWith('ViewModel')) {
            diRegistered.add(target.fullName);
          }
        }
        continue;
      }

      if (value.kind === 'new') {
        const target = index.resolve(value.typeName, usings, decl.namespace);
        if (target) {
          addEdge(self, target.fullName, 'constructs', 'high',
            { file, line: assignment.line }, assignment.target);
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
            { file, line: assignment.line }, assignment.target);
        }
      }
    }

    for (const creation of decl.creations) {
      const target = index.resolve(creation.typeName, usings, decl.namespace);
      const layer = target ? nodes.get(target.fullName)?.layer : undefined;
      // Only composed view models and services are interesting; `new List<T>()` and row models are noise.
      if (target && (layer === 'viewmodel' || layer === 'service')) {
        addEdge(self, target.fullName, 'composes', 'medium', { file, line: creation.line });
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
        addEdge(self, external.id, /^I[A-Z]/.test(base) ? 'implements' : 'inherits',
          'low', { file, line: decl.line }, base);
      }
    }

    /* Plain type references. A property, field, return type or parameter typed as another workspace
       type is a dependency even though it carries no DI meaning — and it is what makes structural
       cycles visible. Self-references are skipped: a tree node holding its parent is not a cycle. */
    const emitReference = (rawType: string, memberName: string, line: number): void => {      for (const name of typeNamesIn(rawType)) {
        if (name === decl.name) {
          continue;
        }
        const target = index.resolve(name, usings, decl.namespace);
        if (!target || target.fullName === self) {
          continue;
        }
        addEdge(self, target.fullName, 'reference', 'medium', { file, line }, memberName);
      }
    };

    for (const property of decl.properties) {
      emitReference(property.rawType, property.name, property.line);
    }
    for (const field of decl.fields) {
      emitReference(field.rawType, field.name, field.line);
    }
    for (const method of decl.methods) {
      emitReference(method.rawReturnType, method.name, method.line);
      for (const parameterType of method.rawParamTypes) {
        emitReference(parameterType, method.name, method.line);
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
        { file, line: mention.line }, `${mention.via} ${mention.name}`);
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
      .filter((property) => property.type.endsWith('ViewModel'))
      .map((property) => property.name);

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
  const finalNodes = kept ? ranked.filter((node) => kept.has(node.id)) : ranked;
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
    },
    warnings,
  };
}
