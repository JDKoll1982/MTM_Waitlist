import { readdirSync, readFileSync, statSync } from 'node:fs';
import { dirname, join, resolve } from 'node:path';
import { analyzeWorkspace, type AnalyzeInput } from './graph/analyze';
import type { GraphModel } from './graph/model';
import type { Finding } from './graph/model';
import { EDGE_KINDS, NODE_LAYERS } from './graph/model';
import { computeVisible, moduleSummary } from './webview/client/view';
import { componentGroups, groupRootFor, packedGroups, packedLayout } from './webview/client/packing';
import { renderHtml } from './webview/html';
import { renderMenuHtml } from './menu/menuHtml';
import { DEFAULT_GRAPH_SETTINGS } from './graph/settings';
import { buildSections } from './debug/report';
import { renderDebugHtml, sectionText } from './debug/reportHtml';
import { renderEdgeTooltip, renderTooltip, summariseEdge, summariseNode } from './webview/client/tooltip';
import { existsSync } from 'node:fs';

/** The tooltip escapes the text it renders, so a comparison has to escape the same way. */
const escapeForTest = (value: string): string => String(value)
  .replace(/&/g, '&amp;')
  .replace(/</g, '&lt;')
  .replace(/>/g, '&gt;')
  .replace(/"/g, '&quot;');
import { computeFindings } from './graph/findings';
import { TypeIndex, moduleOf } from './graph/csharp/symbols';
import { classifyType } from './graph/layers';

const extensionRoot = resolve(__dirname, '..');
/** The repository this extension lives in, used when no root argument is given. */
const defaultWorkspaceRoot = resolve(extensionRoot, '..', '..');
/**
 * Any repository with C# and XAML can be validated: `node out/selftest.js <path>`.
 *
 * The assertions that describe MTM_Waitlist itself run only against that repository; the generic
 * invariants below run against whatever root was given, because those are the checks that catch a
 * parser or resolver defect on a codebase that does not follow this one's conventions.
 */
const workspaceRoot = process.argv[2] ? resolve(process.argv[2]) : defaultWorkspaceRoot;
const isDefaultWorkspace = workspaceRoot.toLowerCase() === defaultWorkspaceRoot.toLowerCase();

function bounds(positions: { x: number; y: number }[]): {
  minX: number; minY: number; maxX: number; maxY: number; width: number; height: number;
} {
  const xs = positions.map((position) => position.x);
  const ys = positions.map((position) => position.y);
  const minX = xs.length > 0 ? Math.min(...xs) : 0;
  const maxX = xs.length > 0 ? Math.max(...xs) : 0;
  const minY = ys.length > 0 ? Math.min(...ys) : 0;
  const maxY = ys.length > 0 ? Math.max(...ys) : 0;
  return { minX, minY, maxX, maxY, width: maxX - minX, height: maxY - minY };
}

const SKIP_DIRECTORIES = new Set(['bin', 'obj', 'node_modules', '.git', '.vs', 'TestResults', 'out', '.playwright-mcp', '.github', '.specify']);

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
  /** True when the check holds for any repository, so it runs even against a foreign root. */
  generic?: boolean;
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

  /* ---- generic invariants: these hold for every repository, not just this one ---- */

  checks.push({
    name: 'no edge points at a node that does not exist',
    passed: model.edges.every((edge) => model.nodes.some((node) => node.id === edge.source)
      && model.nodes.some((node) => node.id === edge.target)),
    detail: `${model.edges.length} edges`,
    generic: true,
  });

  const nodeIdSet = new Set(model.nodes.map((node) => node.id));
  checks.push({
    name: 'no finding points at a node that does not exist',
    passed: model.findings.every((finding) => finding.nodeIds.every((id) => nodeIdSet.has(id))),
    detail: `${model.findings.length} findings`,
    generic: true,
  });

  checks.push({
    name: 'no node id is emitted twice',
    passed: nodeIdSet.size === model.nodes.length,
    detail: `${nodeIdSet.size} unique of ${model.nodes.length}`,
    generic: true,
  });

  // A line outside the file it names sends the reader to nowhere. This is the check that catches a
  // merged partial attributing evidence to the wrong file.
  const lineCache = new Map<string, number>();
  const lineCount = (file: string): number => {
    const cached = lineCache.get(file);
    if (cached !== undefined) {
      return cached;
    }
    let count = 0;
    try {
      count = readFileSync(join(workspaceRoot, ...file.split('/')), 'utf8').split('\n').length;
    } catch {
      count = -1;
    }
    lineCache.set(file, count);
    return count;
  };
  const outOfRange = [
    ...model.nodes.flatMap((node) => (node.location ? [{ file: node.location.file, line: node.location.line }] : [])),
    ...model.edges.flatMap((edge) => (edge.evidence ? [{ file: edge.evidence.file, line: edge.evidence.line }] : [])),
    ...model.findings.flatMap((finding) => (finding.location
      ? [{ file: finding.location.file, line: finding.location.line }]
      : [])),
  ].filter((entry) => {
    const total = lineCount(entry.file);
    return total < 0 || entry.line < 1 || entry.line > total;
  });
  checks.push({
    name: 'every location lies inside the file it names',
    passed: outOfRange.length === 0,
    detail: outOfRange.length === 0
      ? `${lineCache.size} files checked`
      : `${outOfRange.length} out of range, e.g. ${outOfRange[0].file}:${outOfRange[0].line}`,
    generic: true,
  });

  // A partial type spread over N files must contribute the union of its members. Dropping one member
  // list during the merge is invisible in every other check but breaks whole classes of finding, so
  // this is proved on a two-file fixture rather than on whatever a given repository happens to contain.
  const fixtureCsharp: AnalyzeInput[] = [
      {
        path: 'src/A.cs',
        text: [
          'namespace N;',
          'public partial class Thing',
          '{',
          '    public string Alpha { get; set; } = string.Empty;',
          '}',
        ].join('\n'),
      },
      {
        path: 'src/B.cs',
        text: [
          'namespace N;',
          'public partial class Thing',
          '{',
          '    private readonly Helper _holder = new Helper();',
          '',
          '    private void Second() => MarkerOnly.Reset();',
          '}',
          '',
          'public partial class ThingViewModel',
          '{',
          '    public string Status { get; set; } = string.Empty;',
          '',
          '    [RelayCommand]',
          '    private void Save() { }',
          '}',
          '',
          'public class Helper { }',
          'public static class MarkerOnly { public static void Reset() { } }',
        ].join('\n'),
      },
      {
        path: 'src/ThingView.xaml.cs',
        text: [
          'namespace N;',
          'public sealed partial class ThingView : Page',
          '{',
          '    public ThingViewModel ViewModel { get; }',
          '}',
        ].join('\n'),
      },
  ];

  const fixtureXaml: AnalyzeInput[] = [
    {
      path: 'src/ThingView.xaml',
      text: [
        '<Page x:Class="N.ThingView" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">',
        '  <TextBlock Text="{x:Bind ViewModel.SaveCommand}" />',
        '  <TextBlock Text="{x:Bind ViewModel.Missing}" />',
        '  <TextBlock Text="{Binding Status}" />',
        '</Page>',
      ].join('\n'),
    },
  ];

  const fixture = await analyzeWorkspace({
    workspaceRoot: '/virtual',
    csharp: fixtureCsharp,
    xaml: fixtureXaml,
    grammarWasmPath: join(extensionRoot, 'media', 'tree-sitter-c_sharp.wasm'),
    includeExternalTypes: false,
    maxNodes: 1000,
  });

  const fixtureThing = fixture.nodes.find((node) => node.label === 'Thing');
  const mergedEdge = fixture.edges.find((edge) =>
    edge.source === fixtureThing?.id && edge.target.endsWith('.Helper'));
  checks.push({
    name: 'a member declared in a later partial is merged, and its edge is produced',
    passed: mergedEdge !== undefined,
    detail: mergedEdge ? `${mergedEdge.kind} @ ${mergedEdge.evidence?.file}:${mergedEdge.evidence?.line}` : 'no edge',
    generic: true,
  });
  checks.push({
    name: 'evidence for a merged member names the file that declares it',
    passed: mergedEdge?.evidence?.file === 'src/B.cs',
    detail: mergedEdge?.evidence ? `${mergedEdge.evidence.file}:${mergedEdge.evidence.line}` : 'no evidence',
    generic: true,
  });
  // F3: the command is generated by [RelayCommand] in the second partial; the binding is not dead.
  // `{Binding Status}` binds a DataContext this extension does not model, so it is never judged.
  const fixtureDeadBindings = fixture.findings
    .filter((finding) => finding.kind === 'dead-binding')
    .map((finding) => finding.title);
  checks.push({
    name: 'a generated member from a later partial is not a dead binding, and {Binding} is never judged',
    passed: fixtureDeadBindings.length === 1 && fixtureDeadBindings[0].includes('Missing'),
    detail: fixtureDeadBindings.join(' | ') || 'none',
    generic: true,
  });
  checks.push({
    name: 'layer classification understands the ViewModel_ prefix convention',
    passed: classifyType({ name: 'ViewModel_Workflow', namespace: 'X.ViewModels' } as never) === 'viewmodel'
      && classifyType({ name: 'ThingViewModel', namespace: 'X' } as never) === 'viewmodel'
      && classifyType({ name: 'IResettableViewModel', namespace: 'X', kind: 'interface' } as never) === 'model'
      && classifyType({ name: 'Dao_Parts', namespace: 'X' } as never) === 'service',
    generic: true,
  });
  checks.push({
    name: 'a module bucket skips container segments such as src',
    passed: moduleOf('src/Feature/Thing.cs') === 'Feature'
      && moduleOf('Module_Setup/Views/Page.xaml') === 'Module_Setup',
    generic: true,
  });

  /* ---- analysis progress: a real bar, fed by the parse loops ---- */

  const reports: { phase: string; completed: number; total: number }[] = [];
  const probed = await analyzeWorkspace({
    workspaceRoot: '/virtual',
    csharp: fixtureCsharp,
    xaml: fixtureXaml,
    grammarWasmPath: join(extensionRoot, 'media', 'tree-sitter-c_sharp.wasm'),
    includeExternalTypes: false,
    maxNodes: 1000,
    progress: (phase, completed, total) => reports.push({ phase, completed, total }),
  });

  const parseReports = reports.filter((entry) => entry.total > 0);
  const lastParse = parseReports[parseReports.length - 1];
  checks.push({
    name: 'the analysis reports its progress with a real completed/total',
    passed: probed.nodes.length > 0
      && reports.length >= 3
      && parseReports.length > 0
      && lastParse !== undefined
      && lastParse.completed === lastParse.total
      && parseReports.every((entry) => entry.completed <= entry.total),
    detail: reports.map((entry) => `${entry.phase} ${entry.completed}/${entry.total}`).join(' → '),
    generic: true,
  });

  const seen = new Set<string>();
  for (const entry of fixtureCsharp) {
    seen.add(entry.path);
  }
  checks.push({
    name: 'the progress bar advances without ever going backwards',
    passed: parseReports.every((entry, index) =>
      index === 0 || entry.completed >= parseReports[index - 1].completed),
    detail: `${parseReports.length} measured reports`,
    generic: true,
  });

  if (isDefaultWorkspace) {
    const viewNode = model.nodes.find((node) => node.id === 'MTM_Waitlist.Module_Setup.Views.SetupWorkCenterPage');
    checks.push({
      name: 'SetupWorkCenterPage is a View node with its XAML file',
      passed: viewNode?.layer === 'view' && viewNode?.xamlFile === 'Module_Setup/Views/SetupWorkCenterPage.xaml',
      detail: viewNode ? `layer=${viewNode.layer} xaml=${viewNode.xamlFile}` : 'node missing',
    });
  }

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
    layers: ['view', 'viewmodel', 'service'] as const,
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
    passed: initial.nodeIds.size > 40 && initial.nodeIds.size < 1500,
    detail: `${initial.nodeIds.size} nodes, ${initial.edges.length} edges`,
  });

  // The default must carry services, or each view and its view model floats with nothing attached.
  const defaultViewModels = model.nodes.filter((node) => node.layer === 'viewmodel');
  const defaultVisibleViewModels = defaultViewModels.filter((node) => initial.nodeIds.has(node.id));
  const orphanedPairs = defaultVisibleViewModels.filter((node) => {
    const hasServiceEdge = model.edges.some((edge) =>
      edge.source === node.id
      && initial.nodeIds.has(edge.target)
      && model.nodes.find((candidate) => candidate.id === edge.target)?.layer === 'service');
    const hasViewEdge = model.edges.some((edge) =>
      edge.target === node.id
      && initial.nodeIds.has(edge.source)
      && model.nodes.find((candidate) => candidate.id === edge.source)?.layer === 'view');
    return hasServiceEdge && !hasViewEdge;
  });
  checks.push({
    name: 'the default tree keeps services, so a view model is never left without its view',
    // A few view models are deliberately nested inside another (a settings hub holds its editors) and
    // have no page of their own, so the check is that the overwhelming majority are anchored.
    passed: defaultVisibleViewModels.length > 40
      && orphanedPairs.length <= Math.ceil(defaultVisibleViewModels.length * 0.05),
    detail: `${defaultVisibleViewModels.length} view models visible, ${orphanedPairs.length} without their view`,
  });

  const serviceNodes = [...initial.nodeIds].filter((id) =>
    model.nodes.find((node) => node.id === id)?.layer === 'service').length;
  checks.push({
    name: 'services appear in the default tree',
    passed: serviceNodes > 0,
    detail: `${serviceNodes} services drawn`,
  });

  const modelLayerVisible = [...initial.nodeIds].some((id) =>
    model.nodes.find((node) => node.id === id)?.layer === 'model');
  checks.push({
    name: 'row models stay out of the default tree',
    passed: !modelLayerVisible,
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
    // The DI ring is described by role rather than by colour: on a light theme the ring is dark, so
    // "white ring" was only ever true of the dark palette.
    passed: html.includes('#ffffff') && html.includes('#ef4444') && html.includes('#f59e0b')
      && html.includes('Bright ring') && html.includes('Red ring') && html.includes('Amber dashed ring'),
  });

  checks.push({
    name: 'the legend documents the shapes, the region frames and the overview',
    passed: html.includes('Every layer has its own silhouette')
      && html.includes('Regions drawn over the canvas')
      && html.includes('Group frame')
      && html.includes('Module frame')
      && html.includes('Unlinked band')
      && html.includes('Overview.')
      && html.includes('<polygon'),
  });

  checks.push({
    name: 'the panel carries the overview, the region toggles and the inheritance preset',
    passed: ['minimap', 'dim-selection', 'group-captions', 'module-frames', 'island-tray', 'preset-inheritance']
      .every((id) => html.includes(`id="${id}"`)),
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

  // Layout geometry is judged on a readable slice: a 600-node default is a graph you pan, and asking
  // a layout to make it fill one screen would be asking for the smear this was written to prevent.
  const geometryIds = [...initial.nodeIds].sort().slice(0, 120);
  const geometrySet = new Set(geometryIds);
  const layoutElements = geometryIds.map((id) => ({ group: 'nodes' as const, data: { id } }));
  const layoutEdges = initial.edges
    .filter((edge) => geometrySet.has(edge.source) && geometrySet.has(edge.target))
    .map((edge) => ({
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
  if (initial.nodeIds.size === 0) {
    checks.push({
      name: 'canvas geometry checks skipped: the default scope draws no shape',
      passed: true,
      detail: `${initial.nodeIds.size} nodes in the default scope`,
      generic: true,
    });
  }
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
      passed: result.distinct >= geometryIds.length && (!strict || result.crowded === 0),
      detail: `${result.distinct}/${geometryIds.length} distinct, ${result.crowded} crowded, `
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

  /* ---- unpacking: a right-clicked shape is drawn from its root, above the packed tiles ---- */

  const unpackGraph = cytoscape({ headless: true, elements: [...layoutElements, ...layoutEdges] });
  const groups = componentGroups(unpackGraph);
  const target = groups
    .filter((group) => group.nodeIds.length >= 3)
    .sort((left, right) => right.nodeIds.length - left.nodeIds.length)[0]
    ?? { root: '', nodeIds: [] as string[] };
  // A WinForms repository has no XAML, so its default canvas is empty and the geometry checks below
  // have nothing to measure. They still run for any repository that draws a shape.
  const hasChart = target.nodeIds.length >= 3;
  const otherIds = new Set(groups
    .filter((group) => group.root !== target.root)
    .flatMap((group) => group.nodeIds));

  packedLayout(unpackGraph, { gap: 66, aspect: 1.7 });
  const packedBox = bounds(unpackGraph.nodes().map((node) => node.position()));

  packedLayout(unpackGraph, {
    gap: 66,
    aspect: 1.7,
    nodeSep: 20,
    rankSep: 95,
    unpacked: new Set([target.root]),
  });

  const bandPositions = target.nodeIds.map((id) => unpackGraph.getElementById(id).position());
  const bandBox = bounds(bandPositions);
  const tilePositions = [...otherIds].map((id) => unpackGraph.getElementById(id).position());
  const tileBox = bounds(tilePositions);
  const bandDistinct = new Set(bandPositions.map((position) => `${Math.round(position.x)}:${Math.round(position.y)}`)).size;

  checks.push({
    name: 'every node in a packed shape resolves to the same group root',
    passed: target.nodeIds.every((id) => groupRootFor(unpackGraph, id) === target.root),
    detail: `root ${target.root.split('.').pop()} · ${target.nodeIds.length} nodes`,
  });

  checks.push({
    name: 'unpacking a shape draws it as its own band above the remaining tiles',
    // The band is asserted on its own geometry rather than against a tile box: hubs sit outside both,
    // so a tile box built from the pre-split grouping would mix the two.
    passed: hasChart
      && bandPositions.length >= 3
      && bandDistinct === bandPositions.length
      && bandBox.minY <= tileBox.minY
      && bandBox.width > packedBox.width * 0.05,
    detail: `band ${Math.round(bandBox.width)}×${Math.round(bandBox.height)} px at y=${Math.round(bandBox.minY)}, `
      + `tiles from y=${Math.round(tileBox.minY)}`,
  });

  const unpackedSource = readFileSync(join(extensionRoot, 'src', 'webview', 'client', 'main.ts'), 'utf8');
  checks.push({
    name: 'the canvas offers unpack on right-click and a way back',
    passed: unpackedSource.includes("'cxttap'")
      && unpackedSource.includes('Unpack this group')
      && unpackedSource.includes('Re-pack every group')
      && html.includes('id="node-menu"')
      && html.includes('id="repack"'),
  });

  checks.push({
    name: 'unpacking shows the group alone, with no hop expansion back to its partners',
    passed: hasChart
      && unpackedSource.includes('ui.scopeExact = true')
      && unpackedSource.includes('ui.scope = [...group.nodeIds]')
      && computeVisible(model, {
        ...defaults,
        scope: [...target.nodeIds],
        scopeExact: true,
        hideOrphans: false,
      }).nodeIds.size === target.nodeIds.length
      && computeVisible(model, {
        ...defaults,
        scope: [...target.nodeIds],
        scopeExact: false,
        hideOrphans: false,
      }).nodeIds.size > target.nodeIds.length,
    detail: `${target.nodeIds.length} types in the shape`,
  });

  /* ---- the zoom stack in the upper-left corner ---- */

  const viewControlIds = ['view-controls', 'zoom-region', 'zoom-slider', 'view-reset', 'zoom-rect'];
  const missingViewControls = viewControlIds.filter((id) => !html.includes(`id="${id}"`));
  checks.push({
    name: 'the canvas carries a zoom stack: region zoom, a zoom slider, and a reset',
    passed: missingViewControls.length === 0
      && html.indexOf('id="view-controls"') < html.indexOf('id="focus-bar"')
      && unpackedSource.includes('pointerdown')
      && unpackedSource.includes('applyRegionZoom(')
      && unpackedSource.includes('function resetView('),
    detail: missingViewControls.length === 0 ? `${viewControlIds.length} controls` : `missing: ${missingViewControls.join(', ')}`,
  });

  checks.push({
    name: 'the zoom slider covers the whole zoom range and follows wheel zoom',
    passed: unpackedSource.includes('const ZOOM_MIN = 0.08')
      && unpackedSource.includes('const ZOOM_MAX = 3')
      && unpackedSource.includes('function sliderToZoom(')
      && unpackedSource.includes('syncZoomSlider()'),
  });

  /* ---- accuracy guards: findings this workspace would otherwise get wrong ---- */

  const flagged = new Set(model.findings.filter((finding) => finding.kind === 'unused-type').map((finding) => finding.nodeIds[0]));
  const registeredPages = ['GeneralSettingsPage', 'OperationsSettingsPage', 'PicturesSettingsPage', 'DevicesSettingsPage',
    'DiagnosticsSettingsPage', 'AdministrationSettingsPage'];
  const hostedControls = ['ReadStatusIndicator', 'SetUpPanel', 'ImageOverrideEditorControl', 'PermissionHoldersView', 'DeveloperLogPanelView'];
  const wronglyFlagged = [...registeredPages, ...hostedControls].filter((label) => {
    const node = model.nodes.find((candidate) => candidate.label === label);
    return node !== undefined && flagged.has(node.id);
  });

  checks.push({
    name: 'a page registered against a route, or a control hosted by another view, is not reported unused',
    passed: wronglyFlagged.length === 0,
    detail: wronglyFlagged.length === 0
      ? `${registeredPages.length} registered pages, ${hostedControls.length} hosted controls`
      : `flagged anyway: ${wronglyFlagged.join(', ')}`,
  });

  const referenceOnlyCycles = model.findings.filter((finding) => finding.kind === 'cycle'
    && (finding.edgeIds ?? []).every((edgeId) => model.edges.find((edge) => edge.id === edgeId)?.kind === 'reference'));
  checks.push({
    name: 'a cycle of plain type references is not claimed to block construction',
    passed: referenceOnlyCycles.length > 0
      && referenceOnlyCycles.every((finding) => finding.severity !== 'high'
        && !finding.detail.includes('cannot be built first')),
    detail: `${referenceOnlyCycles.length} reference-only cycles`,
  });

  const constructionCycles = model.findings.filter((finding) => finding.kind === 'cycle'
    && (finding.edgeIds ?? []).some((edgeId) => {
      const kind = model.edges.find((edge) => edge.id === edgeId)?.kind;
      return kind === 'injects' || kind === 'constructs' || kind === 'composes';
    }));
  checks.push({
    name: 'a cycle that mentions construction edges says whether those edges themselves loop',
    passed: constructionCycles.length === 0
      || constructionCycles.every((finding) => finding.detail.includes('do not themselves form a loop')
        || finding.detail.includes('construction edges form a loop of their own')),
    detail: `${constructionCycles.length} cycles with construction edges`,
  });

  const docSamples = model.nodes.filter((node) => node.location?.file.startsWith('.github/'));
  checks.push({
    name: 'documentation samples are not analysed as application code',
    passed: docSamples.length === 0,
    detail: docSamples.length === 0 ? 'none analysed' : `${docSamples.length} nodes from .github`,
  });

  // The unpacked shape must adopt the chosen layout, so Down and Right are measurably different.
  const shapeOf = (layout: 'layered-tb' | 'layered-lr'): { width: number; height: number } => {
    const graph = cytoscape({ headless: true, elements: [...layoutElements, ...layoutEdges] });
    packedLayout(graph, {
      gap: 66,
      aspect: 1.7,
      nodeSep: 20,
      rankSep: 95,
      unpacked: new Set([target.root]),
      unpackedLayout: layout,
    });
    const box = bounds(target.nodeIds.map((id) => graph.getElementById(id).position()));
    graph.destroy();
    return box;
  };

  const down = shapeOf('layered-tb');
  const right = shapeOf('layered-lr');
  checks.push({
    name: 'unpacking follows the configured layout, and Layered Down stacks rather than spreads',
    passed: hasChart
      && down.height > down.width && right.width > right.height
      && DEFAULT_GRAPH_SETTINGS.unpackedLayout === 'layered-tb',
    detail: `down ${Math.round(down.width)}×${Math.round(down.height)}, `
      + `right ${Math.round(right.width)}×${Math.round(right.height)}`,
  });

  const manifestText = readFileSync(join(extensionRoot, 'package.json'), 'utf8');
  checks.push({
    name: 'the unpacked-shape layout is a contributed setting defaulting to Layered Down',
    passed: DEFAULT_GRAPH_SETTINGS.unpackedLayout === 'layered-tb'
      && manifestText.includes('"csharpXamlGraph.unpackedLayout"')
      && /"csharpXamlGraph\.unpackedLayout"[\s\S]*?"default": "layered-tb"/.test(manifestText),
  });

  // The canvas captions each packed shape, so the group boxes have to be real rectangles over the
  // nodes they name — a box of zero size would caption nothing, and one that misses its nodes would
  // caption the wrong part of the picture.
  const captionGraph = cytoscape({ headless: true, elements: [...layoutElements, ...layoutEdges] });
  packedLayout(captionGraph, { gap: 66, aspect: 1.7 });
  const captions = packedGroups(captionGraph);
  const badBoxes = captions.filter((group) => {
    const box = group.box;
    if (!(box.width > 0) || !(box.height > 0)) {
      return true;
    }
    return group.nodeIds.some((id) => {
      const position = captionGraph.getElementById(id).position();
      return position.x < box.x - 1 || position.x > box.x + box.width + 1
        || position.y < box.y - 1 || position.y > box.y + box.height + 1;
    });
  });
  checks.push({
    name: 'every packed shape reports a caption box that covers the types it names',
    passed: hasChart && captions.length > 0 && badBoxes.length === 0,
    detail: `${captions.length} groups, ${badBoxes.length} with a box that misses its own nodes`,
  });
  captionGraph.destroy();

  unpackGraph.destroy();

  /* ---- the panel tab beside Terminal: commands and the settings form ---- */

  const menuHtml = renderMenuHtml(
    {
      include: ['**/*.cs'],
      exclude: ['**/bin/**'],
      includeExternalTypes: false,
      maxNodes: 2500,
      unpackedLayout: 'layered-tb',
    },
    'nonce-value',
  );
  const menuControls = ['show', 'reanalyze', 'export', 'open-settings', 'settings-form', 'include', 'exclude',
    'max-nodes', 'include-external', 'unpacked-layout', 'save', 'reset', 'status'];
  const missingMenuControls = menuControls.filter((id) => !menuHtml.includes(`id="${id}"`));
  checks.push({
    name: 'the panel tab carries every command shortcut and settings control',
    passed: missingMenuControls.length === 0,
    detail: missingMenuControls.length === 0 ? `${menuControls.length} controls` : `missing: ${missingMenuControls.join(', ')}`,
  });

  checks.push({
    name: 'the panel tab shows the current settings, not placeholders',
    passed: menuHtml.includes('**/*.cs') && menuHtml.includes('**/bin/**')
      && menuHtml.includes('value="2500"') && !menuHtml.includes('id="include-external" type="checkbox" checked'),
  });

  checks.push({
    name: 'the panel tab carries a nonce-restricted script',
    passed: menuHtml.includes("script-src 'nonce-nonce-value'"),
  });

  const manifest = JSON.parse(readFileSync(join(extensionRoot, 'package.json'), 'utf8')) as {
    activationEvents?: string[];
    contributes?: {
      commands?: { command: string }[];
      menus?: Record<string, { command: string }[]>;
      viewsContainers?: { panel?: { id: string; title: string; icon: string }[] };
      views?: Record<string, { id: string; type: string }[]>;
    };
  };
  const activationSource = readFileSync(join(extensionRoot, 'src', 'extension.ts'), 'utf8');
  const contributed = (manifest.contributes?.commands ?? []).map((entry) => entry.command);
  const unregistered = contributed.filter((id) => !activationSource.includes(`registerCommand('${id}'`)
    && !activationSource.includes(`register('${id}'`));
  checks.push({
    name: 'every contributed command is registered by the extension',
    passed: contributed.length >= 7 && unregistered.length === 0,
    detail: unregistered.length === 0 ? `${contributed.length} commands` : `unregistered: ${unregistered.join(', ')}`,
  });

  const panelContainer = manifest.contributes?.viewsContainers?.panel?.find((entry) => entry.id === 'csharpXamlGraphPanel');
  const menuView = manifest.contributes?.views?.csharpXamlGraphPanel?.find((entry) => entry.id === 'csharpXamlGraph.menu');
  checks.push({
    name: 'a panel tab is contributed next to Terminal, with shortcuts and settings',
    passed: panelContainer !== undefined
      && panelContainer.icon === 'assets/graph.svg'
      && menuView?.type === 'webview'
      && (manifest.activationEvents ?? []).includes('onView:csharpXamlGraph.menu'),
    detail: panelContainer ? `${panelContainer.title} · ${menuView?.type}` : 'missing',
  });

  const editorMenu = (manifest.contributes?.menus?.['editor/context'] ?? []).map((entry) => entry.command);
  const explorerMenu = (manifest.contributes?.menus?.['explorer/context'] ?? []).map((entry) => entry.command);
  checks.push({
    name: 'the editor and Explorer offer a dependency graph for what was right-clicked',
    passed: editorMenu.includes('csharpXamlGraph.showForSelection')
      && editorMenu.includes('csharpXamlGraph.showForFile')
      && explorerMenu.includes('csharpXamlGraph.showForFile'),
    detail: `${editorMenu.length} editor, ${explorerMenu.length} explorer`,
  });

  const resolverSource = readFileSync(join(extensionRoot, 'src', 'symbols', 'resolve.ts'), 'utf8');
  checks.push({
    name: 'a right-click resolves through definitions and offers a picker for multiple matches',
    passed: resolverSource.includes('vscode.executeDefinitionProvider')
      && resolverSource.includes('vscode.executeDocumentSymbolProvider')
      && resolverSource.includes('Whole file')
      && activationSource.includes('showQuickPick'),
  });

  checks.push({
    name: 'the panel tab offers the unpacked-shape layout with Layered Down selected',
    passed: menuHtml.includes('id="unpacked-layout"')
      && menuHtml.includes('<option value="layered-tb" selected>Layered Down</option>')
      && menuHtml.includes('<option value="layered-lr">Layered Right</option>'),
  });

  /* ---- toolbar: three rows, checkboxes on their own row ---- */

  const inputsRow = html.slice(html.indexOf('<div class="row inputs">'), html.indexOf('<div class="row checks">'));
  const checksRow = html.slice(html.indexOf('<div class="row checks">'), html.indexOf('<div class="row actions">'));
  checks.push({
    name: 'the toolbar is three rows: inputs, checkboxes, then actions',
    passed: html.includes('<div class="row inputs">')
      && html.includes('<div class="row checks">')
      && html.includes('<div class="row actions">')
      && !html.includes('<div class="row status">')
      // The combo boxes stay on the first row and the checkboxes on the second.
      && inputsRow.includes('id="module"') && inputsRow.includes('id="layout"') && inputsRow.includes('id="search"')
      && !inputsRow.includes('id="hide-orphans"') && !inputsRow.includes('id="only-problems"')
      && checksRow.includes('id="hide-orphans"') && checksRow.includes('id="only-problems"')
      && !checksRow.includes('<select'),
  });

  checks.push({
    name: 'the status line and focus bar moved onto the canvas so the toolbar stays three rows',
    passed: html.includes('<span id="stats">') && html.includes('<span id="focus-bar" hidden>')
      && html.indexOf('id="stats"') > html.indexOf('id="canvas"'),
  });

  /* ---- hover tooltip: what the shape is, and what is wrong with it ---- */

  const tooltipSource = readFileSync(join(extensionRoot, 'src', 'webview', 'client', 'main.ts'), 'utf8');
  checks.push({
    name: 'the canvas owns a hover tooltip that is shown on mouseover and cleared on mouseout',
    passed: html.includes('id="tooltip"')
      && tooltipSource.includes("cy.on('mouseover', 'node'")
      && tooltipSource.includes('scheduleTooltip(')
      && tooltipSource.includes("cy.on('mouseout', 'node'")
      && tooltipSource.includes('hideTooltip()'),
  });

  checks.push({
    name: 'a line hover states the relationship, its evidence and how sure the analyzer is',
    passed: tooltipSource.includes("cy.on('mouseover', 'edge'")
      && tooltipSource.includes('edgeTooltipMarkup(')
      && tooltipSource.includes("cy.on('mouseout', 'edge'")
      && tooltipSource.includes("cy.on('dbltap', 'edge'")
      && tooltipSource.includes('hovered-edge'),
  });

  const sampleEdge = model.edges.find((edge) => edge.evidence !== undefined && edge.confidence === 'high')
    ?? model.edges[0];
  const sampleSource = model.nodes.find((node) => node.id === sampleEdge.source);
  const sampleTarget = model.nodes.find((node) => node.id === sampleEdge.target);
  const edgeSummary = summariseEdge({
    kind: sampleEdge.kind,
    sourceLabel: sampleSource?.label ?? sampleEdge.source,
    sourceFullName: sampleSource?.fullName ?? sampleEdge.source,
    targetLabel: sampleTarget?.label ?? sampleEdge.target,
    targetFullName: sampleTarget?.fullName ?? sampleEdge.target,
    confidence: sampleEdge.confidence,
    member: sampleEdge.label,
    file: sampleEdge.evidence?.file,
    line: sampleEdge.evidence?.line,
  });
  const edgeTooltipHtml = renderEdgeTooltip(edgeSummary);
  checks.push({
    name: 'the line tooltip names both types, the edge kind and the source line it came from',
    passed: edgeTooltipHtml.includes(edgeSummary.kindLabel)
      && edgeTooltipHtml.includes(escapeForTest(edgeSummary.sourceLabel))
      && edgeTooltipHtml.includes(escapeForTest(edgeSummary.targetLabel))
      && (sampleEdge.evidence === undefined
        || edgeTooltipHtml.includes(escapeForTest(sampleEdge.evidence.file)))
      && edgeTooltipHtml.includes(edgeSummary.colour)
      && !edgeTooltipHtml.includes('No cleanup findings'),
    detail: `${edgeSummary.kindLabel}: ${edgeSummary.sourceLabel} → ${edgeSummary.targetLabel}`,
  });

  const flaggedId = model.findings[0]?.nodeIds[0];
  const flaggedNode = model.nodes.find((node) => node.id === flaggedId);
  const flaggedSummary = flaggedNode
    ? summariseNode({
      ...flaggedNode,
      edges: model.edges,
      findings: model.findings.filter((finding) => finding.nodeIds.includes(flaggedNode.id)),
    })
    : undefined;
  const cleanNode = model.nodes.find((node) =>
    node.location !== undefined && !model.findings.some((finding) => finding.nodeIds.includes(node.id)));
  const cleanSummary = cleanNode
    ? summariseNode({ ...cleanNode, edges: model.edges, findings: [] })
    : undefined;

  checks.push({
    name: 'the hover tooltip lists the issues found for that type',
    passed: flaggedSummary !== undefined
      && flaggedSummary.issues.length > 0
      && renderTooltip(flaggedSummary).includes('tip-issue')
      && renderTooltip(flaggedSummary).includes(flaggedSummary.issues[0].kind)
      && !renderTooltip(flaggedSummary).includes('No cleanup findings'),
    detail: flaggedNode ? `${flaggedNode.label}: ${flaggedSummary?.issues.length ?? 0} issues` : 'no flagged node',
  });

  checks.push({
    name: 'the hover tooltip says so plainly when a type has no findings',
    passed: cleanSummary !== undefined && renderTooltip(cleanSummary).includes('No cleanup findings.'),
  });

  checks.push({
    name: 'the hover tooltip counts the dependencies both ways',
    passed: cleanSummary !== undefined
      && renderTooltip(cleanSummary).includes(`${cleanSummary.out} dependenc`)
      && renderTooltip(cleanSummary).includes(`${cleanSummary.incoming} used by`),
    detail: cleanNode ? `${cleanSummary?.out} out, ${cleanSummary?.incoming} in` : 'no clean node',
  });

  /* ---- every edge handed to the canvas must have both endpoints on the canvas ---- */

  const optionSets: Array<{ name: string; options: Record<string, unknown> }> = [
    { name: 'default view', options: { ...defaults } },
    { name: 'hide unconnected off', options: { ...defaults, hideOrphans: false } },
    { name: 'exact scope', options: { ...defaults, scope: [...target.nodeIds], scopeExact: true } },
    { name: 'exact scope, hide unconnected off', options: { ...defaults, scope: [...target.nodeIds], scopeExact: true, hideOrphans: false } },
    { name: 'module scope', options: { ...defaults, module: model.nodes[0].module } },
    { name: 'only problems', options: { ...defaults, onlyProblems: true } },
    { name: 'focus 2 hops', options: { ...defaults, focus: target.root, focusDepth: 2 } },
    { name: 'every layer and edge kind', options: { ...defaults, layers: [...NODE_LAYERS], edgeKinds: [...EDGE_KINDS] } },
  ];

  const danglingSets = optionSets.filter((entry) => {
    const visible = computeVisible(model, entry.options as never);
    return visible.edges.some((edge) => !visible.nodeIds.has(edge.source) || !visible.nodeIds.has(edge.target));
  });

  checks.push({
    name: 'no visible edge ever points at a node that is not drawn',
    passed: danglingSets.length === 0,
    detail: danglingSets.length === 0
      ? `${optionSets.length} filter combinations`
      : `dangling in: ${danglingSets.map((entry) => entry.name).join('; ')}`,
  });

  checks.push({
    name: 'a filter change keeps the graph in place instead of rebuilding it at the origin',
    passed: unpackedSource.includes('lastPositions')
      && unpackedSource.includes('position: lastPositions.get(node.id)')
      && unpackedSource.includes('capturePositions()'),
  });

  /* ---- export writes the visible graph, not the whole workspace ---- */

  checks.push({
    name: 'export is built from the visible subgraph, with the applied filters recorded',
    passed: unpackedSource.includes('function visibleExport(')
      && unpackedSource.includes('const visible = computeVisible(model, options())')
      && unpackedSource.includes('model.nodes.filter((node) => visible.nodeIds.has(node.id))')
      && unpackedSource.includes('edges: visible.edges')
      && unpackedSource.includes('const findings = model.findings.filter')
      && unpackedSource.includes('vscode.postMessage({ type: \'export\', payload })'),
  });

  const panelSource = readFileSync(join(extensionRoot, 'src', 'webview', 'panel.ts'), 'utf8');
  const extensionSource = readFileSync(join(extensionRoot, 'src', 'extension.ts'), 'utf8');
  checks.push({
    name: 'the host shows a real notification progress bar with increments',
    passed: extensionSource.includes('vscode.ProgressLocation.Notification')
      && extensionSource.includes('progress.report({ message, increment:')
      && extensionSource.includes('GraphPanel.instance()?.showProgress('),
  });

  const htmlProgressIds = ['progress', 'progress-message', 'progress-percent', 'progress-fill'];
  const missingProgress = htmlProgressIds.filter((id) => !html.includes(`id="${id}"`));
  checks.push({
    name: 'the canvas carries a progress overlay that can be shown and hidden',
    passed: missingProgress.length === 0
      && unpackedSource.includes('function setProgress(')
      && unpackedSource.includes("message?.type === 'progress'")
      && unpackedSource.includes('indeterminate')
      && panelSource.includes('showProgress(message: string, percent: number, known: boolean)')
      && panelSource.includes("type: 'progress', message: '', percent: 100, known: true, done: true"),
    detail: missingProgress.length === 0 ? `${htmlProgressIds.length} elements` : `missing: ${missingProgress.join(', ')}`,
  });

  checks.push({
    name: 'the host writes the payload the panel sent, and asks the panel before exporting',
    passed: panelSource.includes('private async exportJson(payload?: unknown)')
      && panelSource.includes('const subject = payload ?? this.model;')
      && panelSource.includes("type: 'requestExport'")
      && activationSource.includes('await panel.requestExport()'),
  });

  /* ---- the debug report: one section per subsystem, each naming the files that own it ---- */

  const debugSections = buildSections({
    host: {
      extensionVersion: '0.1.6',
      vscodeVersion: '1.90.0',
      extensionMode: 'development',
      appHost: 'desktop',
      uiKind: 'Desktop',
      remoteName: '',
      isTrusted: true,
      platform: 'win32',
      arch: 'x64',
      nodeVersion: '20.0.0',
      locale: 'en',
      extensionPath: 'C:/ext',
      workspaceFolders: ['C:/repo'],
    },
    settings: [
      { key: 'csharpXamlGraph.maxNodes', value: '40000', source: 'workspace' },
      { key: 'csharpXamlGraph.include', value: '**/*.cs, **/*.xaml', source: 'default' },
    ],
    model,
    modelAgeMs: 5000,
    cacheInvalidated: false,
    webview: {
      ready: true,
      modelLoadedAt: new Date().toISOString(),
      nodesDrawn: 139,
      edgesDrawn: 194,
      hiddenOrphans: 11,
      layout: 'packed',
      density: 'normal',
      labels: 'zoom',
      zoom: 0.8,
      canvasWidth: 1200,
      canvasHeight: 800,
      module: 'all',
      search: '',
      layers: ['view', 'viewmodel'],
      edgeKinds: ['resolves', 'binds'],
      focus: null,
      focusDepth: 1,
      scope: null,
      scopeExact: false,
      unpacked: [],
      history: [{ at: new Date().toISOString(), kind: 'model', detail: 'loaded' }],
    },
    registeredCommands: ['csharpXamlGraph.show', 'csharpXamlGraph.debug'],
    contributedCommands: ['csharpXamlGraph.show', 'csharpXamlGraph.debug', 'csharpXamlGraph.export'],
    contributedMenus: [{ menu: 'editor/context', commands: ['csharpXamlGraph.showForSelection'] }],
    assets: [{ path: 'media/tree-sitter-c_sharp.wasm', exists: true, bytes: 7_000_000 }],
    activationEvents: ['onCommand:csharpXamlGraph.show'],
    views: [{ id: 'csharpXamlGraph.menu', type: 'webview' }],
  });

  const expectedCategories = ['Host', 'Settings', 'Analysis', 'Parsers', 'Findings', 'Canvas', 'Commands', 'Packaging'];
  const debugCategories = debugSections.map((section) => section.category);
  checks.push({
    name: 'the debug report has one section per subsystem, in reading order',
    passed: JSON.stringify(debugCategories) === JSON.stringify(expectedCategories)
      && debugSections.every((section) => section.files.length > 0 && section.purpose.length > 40),
    detail: debugCategories.join(' → '),
  });

  const debugFilePaths = debugSections.flatMap((section) => section.files);
  const existingDebugFiles = debugFilePaths.filter((file) => file === 'package.json'
    || existsSync(join(extensionRoot, ...file.split('/'))));
  checks.push({
    name: 'every file a debug section points at actually exists',
    passed: debugFilePaths.length >= 12 && existingDebugFiles.length === debugFilePaths.length,
    detail: `${existingDebugFiles.length} of ${debugFilePaths.length} paths`,
  });

  const parserSection = debugSections.find((section) => section.id === 'parsers');
  const parserRows = (parserSection?.rows ?? []).map((row) => row.label).join(' | ');
  checks.push({
    name: 'the parser section reports what the parse pass saw, not just what the graph kept',
    passed: model.stats.parser !== undefined
      && parserRows.includes('Types declared')
      && parserRows.includes('XAML with x:Class')
      && parserRows.includes('Edges dropped')
      && (model.stats.parser?.typesDeclared ?? 0) > 0,
    detail: model.stats.parser
      ? `${model.stats.parser.typesDeclared} types, ${model.stats.parser.bindings} x:Bind`
      : 'no tally',
  });

  const canvasSection = debugSections.find((section) => section.id === 'canvas');
  checks.push({
    name: 'the canvas section names the filters in force and what is drawn',
    passed: (canvasSection?.rows ?? []).some((row) => row.label === 'Layers' && row.value.includes('view'))
      && (canvasSection?.rows ?? []).some((row) => row.label === 'Drawn' && row.value.includes('139')),
  });

  const commandsSection = debugSections.find((section) => section.id === 'commands');
  checks.push({
    name: 'the commands section compares the manifest with what activation registered',
    passed: (commandsSection?.rows ?? []).some((row) => row.label === 'Contributed but not registered'
      && row.value.includes('csharpXamlGraph.export') && row.tone === 'bad'),
  });

  const missingAssetReport = buildSections({
    host: {
      extensionVersion: '0.1.6', vscodeVersion: '1.90.0', extensionMode: 'development', appHost: 'desktop',
      uiKind: 'Desktop', remoteName: '', isTrusted: true, platform: 'win32', arch: 'x64',
      nodeVersion: '20.0.0', locale: 'en', extensionPath: 'C:/ext', workspaceFolders: [],
    },
    settings: [],
    cacheInvalidated: true,
    registeredCommands: [],
    contributedCommands: [],
    contributedMenus: [],
    assets: [{ path: 'media/tree-sitter-c_sharp.wasm', exists: false }],
    activationEvents: [],
    views: [],
  });
  checks.push({
    name: 'a missing or invalidated asset is reported in red and in words',
    passed: (missingAssetReport.find((section) => section.id === 'packaging')?.rows ?? [])
      .some((row) => row.tone === 'bad' && row.value.startsWith('MISSING'))
      && (missingAssetReport.find((section) => section.id === 'analysis')?.rows ?? [])
        .some((row) => row.value.includes('not analysed yet')),
  });

  const debugHtml = renderDebugHtml(debugSections, 'nonce-value');
  checks.push({
    name: 'the debug page renders a nav link and a copy button for every section',
    passed: debugSections.every((section) => debugHtml.includes(`href="#section-${section.id}"`)
      && debugHtml.includes(`data-section="${section.id}"`))
      && debugHtml.includes("script-src 'nonce-nonce-value'")
      && debugHtml.includes('Refresh report'),
  });

  const plain = sectionText(debugSections, 'parsers');
  checks.push({
    name: 'copying one section yields that section only',
    passed: plain.includes('Parsers — C# and XAML parsers')
      && !plain.includes('Packaging — Bundles'),
  });

  const manifestForDebug = JSON.parse(readFileSync(join(extensionRoot, 'package.json'), 'utf8')) as {
    contributes?: { commands?: { command: string }[] };
  };
  checks.push({
    name: 'the debug report is a contributed, registered command reachable from the panel tab',
    passed: (manifestForDebug.contributes?.commands ?? []).some((entry) => entry.command === 'csharpXamlGraph.debug')
      && activationSource.includes("register('csharpXamlGraph.debug'")
      && menuHtml.includes('id="debug"'),
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
  const reported = isDefaultWorkspace ? checks : checks.filter((check) => check.generic === true);
  if (!isDefaultWorkspace) {
    console.log(`validating ${workspaceRoot} — repository-specific assertions skipped\n`);
  }
  for (const check of reported) {
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
