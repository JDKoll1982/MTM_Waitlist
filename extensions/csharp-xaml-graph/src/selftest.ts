import { readdirSync, readFileSync, statSync } from 'node:fs';
import { dirname, join, resolve } from 'node:path';
import { analyzeWorkspace, type AnalyzeInput } from './graph/analyze';
import type { GraphModel } from './graph/model';
import type { Finding } from './graph/model';
import { EDGE_KINDS, NODE_LAYERS } from './graph/model';
import { computeVisible, moduleSummary } from './webview/client/view';
import { packedLayout } from './webview/client/packing';
import { renderHtml } from './webview/html';
import { computeFindings } from './graph/findings';
import { TypeIndex } from './graph/csharp/symbols';

const extensionRoot = resolve(__dirname, '..');
const workspaceRoot = resolve(extensionRoot, '..', '..');

const SKIP_DIRECTORIES = new Set(['bin', 'obj', 'node_modules', '.git', '.vs', 'TestResults', 'out']);

/** Matches the extension's default exclude list, so the self-test sees what the panel will see. */
function skipDirectory(name: string): boolean {
  return SKIP_DIRECTORIES.has(name) || name.endsWith('.Tests');
}

function collect(directory: string, extension: string, results: AnalyzeInput[] = []): AnalyzeInput[] {
  let entries: string[];
  try {
    entries = readdirSync(directory);
  } catch {
    return results;
  }

  for (const entry of entries) {
    const full = join(directory, entry);
    let stats: ReturnType<typeof statSync>;
    try {
      stats = statSync(full);
    } catch {
      continue;
    }
    if (stats.isDirectory()) {
      if (skipDirectory(entry)) {
        continue;
      }
      collect(full, extension, results);
      continue;
    }
    if (!entry.toLowerCase().endsWith(extension)) {
      continue;
    }
    const relative = full.slice(workspaceRoot.length + 1).split('\\').join('/');
    if (relative.startsWith('extensions/')) {
      continue;
    }
    results.push({ path: relative, text: readFileSync(full, 'utf8') });
  }

  return results;
}

interface Check {
  name: string;
  passed: boolean;
  detail?: string;
}

function hasEdge(
  model: GraphModel,
  source: string,
  target: string,
  kinds: string[],
): boolean {
  return model.edges.some((edge) =>
    edge.source === source && edge.target === target && kinds.includes(edge.kind));
}

async function main(): Promise<void> {
  const csharp = collect(workspaceRoot, '.cs');
  const xaml = collect(workspaceRoot, '.xaml');

  const model = await analyzeWorkspace({
    workspaceRoot,
    csharp,
    xaml,
    grammarWasmPath: join(extensionRoot, 'media', 'tree-sitter-c_sharp.wasm'),
    includeExternalTypes: true,
    maxNodes: 20000,
  });

  const byLayer = new Map<string, number>();
  for (const node of model.nodes) {
    byLayer.set(node.layer, (byLayer.get(node.layer) ?? 0) + 1);
  }
  const byKind = new Map<string, number>();
  for (const edge of model.edges) {
    byKind.set(edge.kind, (byKind.get(edge.kind) ?? 0) + 1);
  }

  const checks: Check[] = [];

  const viewNode = model.nodes.find((node) => node.id === 'MTM_Waitlist.Module_Setup.Views.SetupWorkCenterPage');
  checks.push({
    name: 'SetupWorkCenterPage is a View node with its XAML file',
    passed: viewNode?.layer === 'view' && viewNode?.xamlFile === 'Module_Setup/Views/SetupWorkCenterPage.xaml',
    detail: viewNode ? `layer=${viewNode.layer} xaml=${viewNode.xamlFile}` : 'node missing',
  });

  checks.push({
    name: 'SetupWorkCenterPage resolves SetupWorkCenterViewModel via App.GetService',
    passed: hasEdge(model, 'MTM_Waitlist.Module_Setup.Views.SetupWorkCenterPage',
      'MTM_Waitlist.Module_Setup.ViewModels.SetupWorkCenterViewModel', ['getservice']),
  });

  checks.push({
    name: 'SetupWorkCenterPage -> SetupWorkCenterViewModel is also a DI registration',
    passed: hasEdge(model, 'MTM_Waitlist.Module_Setup.Views.SetupWorkCenterPage',
      'MTM_Waitlist.Module_Setup.ViewModels.SetupWorkCenterViewModel', ['resolves']),
  });

  checks.push({
    name: 'SignInPage has two view models (ViewModel + PasswordChange)',
    passed: hasEdge(model, 'MTM_Waitlist.Module_Startup.Views.SignInPage',
      'MTM_Waitlist.Module_Startup.ViewModels.SignInViewModel', ['getservice', 'binds'])
      && hasEdge(model, 'MTM_Waitlist.Module_Startup.Views.SignInPage',
        'MTM_Waitlist.Module_Startup.ViewModels.PasswordChangeViewModel', ['getservice', 'binds']),
  });

  checks.push({
    name: 'SetUpPanel binds its Panel property to SetUpPanelViewModel',
    passed: hasEdge(model, 'MTM_Waitlist.Module_Startup.Views.SetUpPanel',
      'MTM_Waitlist.Module_Startup.ViewModels.SetUpPanelViewModel', ['getservice', 'binds']),
  });

  const templateRoots = ['PermissionMatrixRow', 'SampleOrder', 'HeaderStep'];
  const boundToTemplateRows = model.edges.filter((edge) =>
    edge.kind === 'binds' && templateRoots.some((root) => edge.target.endsWith(root)));
  checks.push({
    name: 'DataTemplate row models are not reported as bound view models',
    passed: boundToTemplateRows.length === 0,
    detail: boundToTemplateRows.map((edge) => `${edge.source}->${edge.target}`).join(', ') || 'none',
  });

  const resolves = byKind.get('resolves') ?? 0;
  checks.push({
    name: 'pageService.Configure produced the DI map',
    passed: resolves >= 40,
    detail: `${resolves} resolves edges`,
  });

  const binds = byKind.get('binds') ?? 0;
  checks.push({
    name: 'XAML bindings confirmed view-model properties',
    passed: binds >= 50,
    detail: `${binds} binds edges`,
  });

  const injects = byKind.get('injects') ?? 0;
  checks.push({
    name: 'Constructor injection produced service edges',
    passed: injects >= 300,
    detail: `${injects} injects edges`,
  });

  for (const edge of model.edges) {
    if (!edge.source || !edge.target) {
      checks.push({ name: 'every edge has both endpoints', passed: false, detail: edge.id });
      break;
    }
  }

  const nodesById = new Set(model.nodes.map((node) => node.id));
  const dangling = model.edges.filter((edge) => !nodesById.has(edge.source) || !nodesById.has(edge.target));
  checks.push({
    name: 'every edge endpoint exists as a node',
    passed: dangling.length === 0,
    detail: dangling.map((edge) => `${edge.source} -> ${edge.target}`).slice(0, 3).join(', ') || 'none',
  });

  /* ---- the view model of the graph: what the webview actually draws ---- */

  const defaults = {
    layers: ['view', 'viewmodel'] as const,
    edgeKinds: ['resolves', 'getservice', 'binds', 'injects', 'field', 'constructs', 'composes'] as const,
    module: 'all',
    search: '',
    focus: null as string | null,
    focusDepth: 1,
    hideOrphans: true,
    onlyProblems: false,
  };

  const initial = computeVisible(model, { ...defaults, layers: [...defaults.layers], edgeKinds: [...defaults.edgeKinds] });
  checks.push({
    name: 'default view is a legible subset, not the whole repository',
    passed: initial.nodeIds.size > 40 && initial.nodeIds.size < 400,
    detail: `${initial.nodeIds.size} nodes, ${initial.edges.length} edges`,
  });

  const noServices = [...initial.nodeIds].every((id) =>
    model.nodes.find((node) => node.id === id)?.layer !== 'service');
  checks.push({
    name: 'default layers exclude services and models',
    passed: noServices,
  });

  const signInFocus = computeVisible(model, {
    ...defaults,
    layers: [...defaults.layers],
    edgeKinds: [...defaults.edgeKinds],
    focus: 'MTM_Waitlist.Module_Startup.Views.SignInPage',
    focusDepth: 1,
  });
  checks.push({
    name: 'focusing SignInPage one hop out keeps both of its view models',
    passed: signInFocus.nodeIds.has('MTM_Waitlist.Module_Startup.ViewModels.SignInViewModel')
      && signInFocus.nodeIds.has('MTM_Waitlist.Module_Startup.ViewModels.PasswordChangeViewModel'),
    detail: `${signInFocus.nodeIds.size} nodes`,
  });

  const moduleOnly = computeVisible(model, {
    ...defaults,
    layers: [...defaults.layers],
    edgeKinds: [...defaults.edgeKinds],
    module: 'Module_Setup',
  });
  const moduleNodes = [...moduleOnly.nodeIds]
    .map((id) => model.nodes.find((candidate) => candidate.id === id))
    .filter((node): node is NonNullable<typeof node> => Boolean(node));
  const moduleNeighbours = new Set<string>();
  for (const edge of model.edges) {
    const source = model.nodes.find((node) => node.id === edge.source);
    const target = model.nodes.find((node) => node.id === edge.target);
    if (source?.module === 'Module_Setup') {
      moduleNeighbours.add(edge.target);
    }
    if (target?.module === 'Module_Setup') {
      moduleNeighbours.add(edge.source);
    }
  }
  checks.push({
    name: 'module filter keeps the module plus the partners it connects to',
    passed: moduleOnly.nodeIds.size > 0 && moduleNodes.every((node) =>
      node.module === 'Module_Setup'
      || node.module === '(external)'
      || moduleNeighbours.has(node.id)),
    detail: `${moduleOnly.nodeIds.size} nodes`,
  });

  const setupViews = moduleNodes.filter((node) => node.layer === 'view').length;
  const setupViewModels = moduleNodes.filter((node) => node.layer === 'viewmodel').length;
  checks.push({
    name: 'a module scope shows its views with their view models, not views alone',
    passed: setupViews > 0 && setupViewModels > 0,
    detail: `${setupViews} views, ${setupViewModels} view models`,
  });

  const withoutTemplates = computeVisible(model, {
    ...defaults,
    layers: ['view', 'viewmodel', 'model'],
    edgeKinds: [...defaults.edgeKinds],
  });
  const withTemplates = computeVisible(model, {
    ...defaults,
    layers: ['view', 'viewmodel', 'model'],
    edgeKinds: [...defaults.edgeKinds, 'template'],
  });
  checks.push({
    name: 'template edges appear only when their filter is enabled',
    passed: withTemplates.edges.length >= withoutTemplates.edges.length
      && withTemplates.edges.some((edge) => edge.kind === 'template')
      && withoutTemplates.edges.every((edge) => edge.kind !== 'template'),
    detail: `${withoutTemplates.edges.length} vs ${withTemplates.edges.length} edges`,
  });

  const orphanAware = computeVisible(model, {
    ...defaults,
    layers: [...defaults.layers],
    edgeKinds: [...defaults.edgeKinds],
    hideOrphans: false,
  });
  checks.push({
    name: 'turning off hide-unconnected reveals more nodes than it hides',
    passed: orphanAware.nodeIds.size >= initial.nodeIds.size,
    detail: `${orphanAware.nodeIds.size} vs ${initial.nodeIds.size}`,
  });

  checks.push({
    name: 'modules are discoverable for the module picker',
    passed: moduleSummary(model).length >= 10,
    detail: `${moduleSummary(model).length} modules`,
  });

  /* ---- the panel: every control the script reaches for must exist in the markup ---- */

  const html = renderHtml({
    webview: { cspSource: 'vscode-resource://test' } as never,
    scriptUri: { toString: () => 'script.js' } as never,
    styleUri: { toString: () => 'style.css' } as never,
    nonce: 'nonce-value',
  });

  const clientSource = readFileSync(join(extensionRoot, 'src', 'webview', 'client', 'main.ts'), 'utf8');
  const referenced = new Set<string>();
  for (const match of clientSource.matchAll(/element(?:<[^>]*>)?\(\s*'([^']+)'\s*\)/g)) {
    referenced.add(match[1]);
  }
  for (const match of clientSource.matchAll(/getElementById\(\s*'([^']+)'\s*\)/g)) {
    referenced.add(match[1]);
  }

  const missing = [...referenced].filter((id) => !html.includes(`id="${id}"`));
  checks.push({
    name: 'every control the script uses exists in the panel markup',
    passed: missing.length === 0,
    detail: missing.length === 0 ? `${referenced.size} controls` : `missing: ${missing.join(', ')}`,
  });

  checks.push({
    name: 'the panel carries a content security policy with a nonce',
    passed: html.includes('Content-Security-Policy') && html.includes("script-src 'nonce-nonce-value'"),
  });

  /* ---- cleanup findings ---- */

  checks.push({
    name: 'findings are produced and summarised',
    passed: model.findings.length > 0
      && model.cleanup.deadBindings + model.cleanup.unusedTypes + model.cleanup.unusedMembers
        + model.cleanup.cycles + model.cleanup.unboundViews === model.findings.length,
    detail: JSON.stringify(model.cleanup),
  });

  checks.push({
    name: 'every finding names at least one node',
    passed: model.findings.every((finding) => finding.nodeIds.length > 0),
  });

  const findingKinds = new Set(model.findings.map((finding) => finding.kind));
  checks.push({
    name: 'findings cover the cleanup categories this workspace exhibits',
    passed: findingKinds.size >= 2,
    detail: [...findingKinds].sort().join(', '),
  });

  // A [RelayCommand] method generates a command property. Flagging that binding would be a false positive.
  const generatedCommandFlagged = model.findings.filter((finding) =>
    (finding.kind === 'dead-binding')
    && /Command\b/.test(finding.title)
    && ['AdministrationSettingsPage', 'DeveloperLogPanelView', 'DiagnosticsSettingsPage']
      .some((view) => finding.title.includes(view)));
  checks.push({
    name: 'x:Bind to a RelayCommand-generated command is not reported as dead',
    passed: generatedCommandFlagged.length === 0,
    detail: generatedCommandFlagged.map((finding) => finding.title).slice(0, 2).join(' | ') || 'none',
  });

  checks.push({
    name: 'the database deployment window is not reported as an unbound view',
    passed: !model.findings.some((finding) => finding.kind === 'unbound-view'
      && finding.title.startsWith('MainWindow')),
  });

  checks.push({
    name: 'unused types stay a short list rather than every type',
    passed: model.cleanup.unusedTypes < model.nodes.length * 0.2,
    detail: `${model.cleanup.unusedTypes} of ${model.nodes.length} nodes`,
  });

  // Cycle detection is proven against a graph built for the purpose, so a real zero is meaningful.
  const cycleNodes = ['A', 'B', 'C', 'D'].map((id) => ({
    id, label: id, fullName: id, layer: 'service' as const, module: 'M', kind: 'class',
  }));
  const cycleEdges = [
    { id: 'e1', source: 'A', target: 'B', kind: 'injects' as const, confidence: 'high' as const },
    { id: 'e2', source: 'B', target: 'C', kind: 'injects' as const, confidence: 'high' as const },
    { id: 'e3', source: 'C', target: 'A', kind: 'injects' as const, confidence: 'high' as const },
    { id: 'e4', source: 'A', target: 'D', kind: 'injects' as const, confidence: 'high' as const },
  ];
  const synthetic = computeFindings({
    nodes: cycleNodes,
    edges: cycleEdges,
    decls: new Map(),
    files: new Map(),
    declarationCounts: new Map(),
    index: new TypeIndex(),
    usingsFor: () => [],
    xamlFiles: [],
    sources: [],
  });
  const cycles = synthetic.findings.filter((finding) => finding.kind === 'cycle');
  checks.push({
    name: 'cycle detection finds a three-node loop and leaves the acyclic branch out',
    passed: cycles.length === 1
      && cycles[0].nodeIds.length === 3
      && !cycles[0].nodeIds.includes('D')
      && synthetic.inCycle.size === 3,
    detail: cycles.length ? `${cycles[0].nodeIds.join(' → ')}` : 'no cycle found',
  });

  const selfCycle = computeFindings({
    nodes: [{ id: 'S', label: 'S', fullName: 'S', layer: 'service', module: 'M', kind: 'class' }],
    edges: [{ id: 's1', source: 'S', target: 'S', kind: 'composes', confidence: 'high' }],
    decls: new Map(),
    files: new Map(),
    declarationCounts: new Map(),
    index: new TypeIndex(),
    usingsFor: () => [],
    xamlFiles: [],
    sources: [],
  });
  checks.push({
    name: 'a self-dependency is reported as its own finding',
    passed: selfCycle.findings.some((finding) => finding.kind === 'cycle' && finding.nodeIds.length === 1),
  });

  const realCycles = model.findings.filter((finding) => finding.kind === 'cycle');
  checks.push({
    name: 'dependency cycles are counted for this workspace',
    passed: realCycles.length > 0,
    detail: realCycles.length === 0
      ? 'none found'
      : `${realCycles.length} cycles, largest involving ${Math.max(...realCycles.map((finding) => finding.nodeIds.length))} types`,
  });

  // The repository is known to contain this mutual static reference; it is the smallest cycle here,
  // so it proves the broader reference graph reaches code the DI-only graph cannot.
  const knownCycle = realCycles.find((finding) => finding.nodeIds.length === 2
    && finding.nodeIds.some((id) => id.endsWith('.SettingsViewModel'))
    && finding.nodeIds.some((id) => id.endsWith('.SettingsSearchCatalog')));
  checks.push({
    name: 'the SettingsViewModel / SettingsSearchCatalog mutual reference is reported as a cycle',
    passed: Boolean(knownCycle),
    detail: knownCycle ? knownCycle.title : 'not found',
  });

  const referenceEdges = model.edges.filter((edge) => edge.kind === 'reference');
  checks.push({
    name: 'type references are collected beyond declarations',
    passed: referenceEdges.length > 1000,
    detail: `${referenceEdges.length} reference edges`,
  });

  const referenceVias = new Set(referenceEdges
    .map((edge) => (edge.label ?? '').split(' ').slice(0, -1).join(' '))
    .filter(Boolean));
  const wantedVias = ['local', 'cast', 'attribute', 'static access', 'typeof', 'new'];
  checks.push({
    name: 'references cover locals, casts, attributes, static access and more',
    passed: wantedVias.filter((via) => referenceVias.has(via)).length >= 5,
    detail: `found: ${wantedVias.filter((via) => referenceVias.has(via)).join(', ')}`,
  });

  checks.push({
    name: 'every cycle member carries the in-cycle mark',
    passed: model.nodes.filter((node) => node.inCycle).length
      === realCycles.reduce((sum, finding) => sum + finding.nodeIds.length, 0),
  });

  /* ---- legend and problem list ---- */

  const layerColours = ['#4a9eff', '#2fbf71', '#f0a030', '#9b8cff'];
  const missingSwatches = layerColours.filter((colour) => !html.includes(colour));
  checks.push({
    name: 'the legend shows a swatch for every node layer',
    passed: missingSwatches.length === 0,
    detail: missingSwatches.length ? `missing ${missingSwatches.join(', ')}` : `${layerColours.length} layers`,
  });

  const legendEdgeText = [
    'DI registration', 'Service locator', 'XAML binding', 'Constructor injection', 'Field-backed',
    'Constructed', 'Composes', 'Inherits', 'Implements', 'Template row',
  ];
  const missingEdgeKinds = legendEdgeText.filter((label) => !html.includes(label));
  checks.push({
    name: 'the legend names every edge kind',
    passed: missingEdgeKinds.length === 0,
    detail: missingEdgeKinds.length ? `missing ${missingEdgeKinds.join(', ')}` : `${legendEdgeText.length} kinds`,
  });

  checks.push({
    name: 'the legend explains the state rings (DI, cycle, finding)',
    passed: html.includes('#ffffff') && html.includes('#ef4444') && html.includes('#f59e0b')
      && html.includes('White ring') && html.includes('Red ring') && html.includes('Amber dashed ring'),
  });

  checks.push({
    name: 'the panel carries a Problems tab with a count badge',
    passed: html.includes('id="tab-problems"') && html.includes('id="problem-count"')
      && html.includes('id="problems"'),
  });

  /* ---- visibility flyout ---- */

  const missingLayerRows = NODE_LAYERS.filter((layer) => !html.includes(`id="layer-${layer}"`));
  checks.push({
    name: 'the flyout has a row for every node layer',
    passed: missingLayerRows.length === 0,
    detail: missingLayerRows.length ? `missing ${missingLayerRows.join(', ')}` : `${NODE_LAYERS.length} layers`,
  });

  const missingEdgeRows = EDGE_KINDS.filter((kind) => !html.includes(`id="edge-${kind}"`));
  checks.push({
    name: 'the flyout has a row for every edge kind, including type references',
    passed: missingEdgeRows.length === 0 && html.includes('id="edge-reference"'),
    detail: missingEdgeRows.length ? `missing ${missingEdgeRows.join(', ')}` : `${EDGE_KINDS.length} kinds`,
  });

  const presets = ['preset-mvvm', 'preset-structure', 'preset-cleanup', 'preset-all'];
  checks.push({
    name: 'the flyout offers visibility presets',
    passed: presets.every((id) => html.includes(`id="${id}"`)),
    detail: presets.join(', '),
  });

  checks.push({
    name: 'the flyout is reachable from the toolbar and closable',
    passed: html.includes('id="visibility-open"') && html.includes('id="visibility"')
      && html.includes('id="visibility-close"'),
  });

  const problemOnly = computeVisible(model, {
    ...defaults,
    layers: [...NODE_LAYERS],
    edgeKinds: [...EDGE_KINDS],
    onlyProblems: true,
  });
  const flagsInModel = model.nodes.filter((node) => node.inCycle || (node.problemCount ?? 0) > 0).length;
  const everyVisibleFlagged = [...problemOnly.nodeIds].every((id) => {
    const node = model.nodes.find((candidate) => candidate.id === id);
    return node && (node.inCycle || (node.problemCount ?? 0) > 0);
  });
  checks.push({
    name: 'the only-problems filter keeps flagged nodes and nothing else',
    passed: flagsInModel > 0 && everyVisibleFlagged && problemOnly.nodeIds.size === flagsInModel,
    detail: `${problemOnly.nodeIds.size} of ${flagsInModel} flagged nodes`,
  });

  const cycleNodeCount = model.nodes.filter((node) => node.inCycle).length;
  checks.push({
    name: 'cycle membership is recorded on the nodes themselves',
    passed: cycleNodeCount === model.findings
      .filter((finding) => finding.kind === 'cycle')
      .reduce((sum, finding) => sum + finding.nodeIds.length, 0),
    detail: `${cycleNodeCount} nodes in cycles`,
  });

  /* ---- layout: the graph must occupy both axes, not collapse into a line ---- */

  const cytoscape = (await import('cytoscape')).default;
  const dagreExtension = (await import('cytoscape-dagre')).default;
  cytoscape.use(dagreExtension);

  const layoutElements = [...initial.nodeIds].map((id) => ({ group: 'nodes' as const, data: { id } }));
  const layoutEdges = initial.edges.map((edge) => ({
    group: 'edges' as const,
    data: { id: edge.id, source: edge.source, target: edge.target },
  }));

  const layoutNames = ['packed', 'layered-lr', 'layered-tb', 'force', 'radial', 'grid', 'circle'] as const;
  const layoutOptions: Record<string, Record<string, unknown>> = {
    packed: { name: 'preset' },
    'layered-lr': { name: 'dagre', rankDir: 'LR', nodeSep: 20, rankSep: 95 },
    'layered-tb': { name: 'dagre', rankDir: 'TB', nodeSep: 20, rankSep: 95 },
    force: { name: 'cose', idealEdgeLength: 80, nodeRepulsion: 9000 },
    radial: { name: 'concentric', levelWidth: () => 1, minNodeSpacing: 30 },
    grid: { name: 'grid', avoidOverlap: true, condense: false, spacingFactor: 1.6 },
    circle: { name: 'circle', avoidOverlap: true, spacingFactor: 2 },
  };

  const measurements = new Map<string, { width: number; height: number; distinct: number; crowded: number }>();
  // Headless cytoscape reports a 3×3 box for every node, so separation is judged against a nominal
  // node size rather than the reported box.
  const NOMINAL_NODE = 18;

  for (const name of layoutNames) {
    const graph = cytoscape({
      headless: true,
      elements: [...layoutElements, ...layoutEdges],
    });

    if (name === 'packed') {
      packedLayout(graph, { gap: 66, aspect: 1.7 });
    } else {
      graph.layout({ ...layoutOptions[name], animate: false, fit: false } as never).run();
    }

    const positions = graph.nodes().map((node) => node.position());
    const width = Math.max(...positions.map((position) => position.x)) - Math.min(...positions.map((position) => position.x));
    const height = Math.max(...positions.map((position) => position.y)) - Math.min(...positions.map((position) => position.y));
    const distinct = new Set(positions.map((position) => `${Math.round(position.x)}:${Math.round(position.y)}`)).size;

    let crowded = 0;
    for (let outer = 0; outer < positions.length; outer += 1) {
      for (let inner = outer + 1; inner < positions.length; inner += 1) {
        if (Math.abs(positions[outer].x - positions[inner].x) < NOMINAL_NODE
          && Math.abs(positions[outer].y - positions[inner].y) < NOMINAL_NODE) {
          crowded += 1;
        }
      }
    }

    measurements.set(name, { width, height, distinct, crowded });
    graph.destroy();
  }

  // Layered and packed layouts place nodes from geometry alone, so their spacing can be judged here.
  // Force, grid and circle space nodes using their rendered size, which headless cytoscape reports as
  // 3×3 — so for those the check is that every node gets its own position, not its spacing.
  const spacingDeterminedByAlgorithm = new Set(['packed', 'layered-lr', 'layered-tb', 'radial']);

  for (const name of layoutNames) {
    const result = measurements.get(name)!;
    const strict = spacingDeterminedByAlgorithm.has(name);
    checks.push({
      name: strict
        ? `layout "${name}" gives every node its own position and spacing`
        : `layout "${name}" gives every node its own position`,
      passed: result.distinct >= initial.nodeIds.size && (!strict || result.crowded === 0),
      detail: `${result.distinct}/${initial.nodeIds.size} distinct, ${result.crowded} crowded, `
        + `${Math.round(result.width)}×${Math.round(result.height)} px`,
    });
  }

  // Only the packed layout has to fill the panel: dagre is expected to be elongated when its input
  // is many disconnected pieces, which is exactly why the default is no longer a plain dagre run.
  const packed = measurements.get('packed')!;
  const packedAspect = packed.width / Math.max(1, packed.height);
  checks.push({
    name: 'packed layout fills both axes instead of collapsing into a line',
    passed: packed.width > 600 && packed.height > 400 && packedAspect > 0.6 && packedAspect < 3.5,
    detail: `${Math.round(packed.width)}×${Math.round(packed.height)} px, aspect ${packedAspect.toFixed(2)}`,
  });

  const plainDagre = measurements.get('layered-lr')!;
  checks.push({
    name: 'plain dagre on disconnected pieces is the elongated shape packed layout replaces',
    passed: plainDagre.width / Math.max(1, plainDagre.height) < 0.5,
    detail: `aspect ${(plainDagre.width / Math.max(1, plainDagre.height)).toFixed(2)}`,
  });

  console.log(`workspace: ${workspaceRoot}`);
  console.log(`files: ${model.stats.csharpFiles} C#, ${model.stats.xamlFiles} XAML in ${model.stats.elapsedMs} ms`);
  console.log(`parse errors: ${model.stats.parseErrors}`);
  console.log(`nodes: ${model.nodes.length} (${[...byLayer].map(([k, v]) => `${k}=${v}`).join(', ')})`);
  console.log(`edges: ${model.edges.length} (${[...byKind].map(([k, v]) => `${k}=${v}`).join(', ')})`);
  if (model.warnings.length > 0) {
    console.log(`warnings: ${model.warnings.slice(0, 3).join(' | ')}`);
  }
  console.log('');

  let failures = 0;
  for (const check of checks) {
    if (!check.passed) {
      failures += 1;
    }
    const detail = check.detail ? `  (${check.detail})` : '';
    console.log(`${check.passed ? 'PASS' : 'FAIL'}  ${check.name}${detail}`);
  }

  console.log('');
  console.log('findings:');
  const kinds: Finding['kind'][] = ['cycle', 'dead-binding', 'unbound-view', 'unused-type', 'unused-member'];
  for (const kind of kinds) {
    const matching = model.findings.filter((finding) => finding.kind === kind);
    console.log(`  ${kind}: ${matching.length}`);
    for (const finding of matching.slice(0, 5)) {
      const where = finding.location ? ` [${finding.location.file}:${finding.location.line}]` : '';
      console.log(`    - ${finding.title}${where}`);
    }
    if (matching.length > 5) {
      console.log(`    … ${matching.length - 5} more`);
    }
  }

  console.log('');
  console.log(failures === 0 ? 'ALL CHECKS PASSED' : `${failures} CHECK(S) FAILED`);
  process.exitCode = failures === 0 ? 0 : 1;
}

void main();
