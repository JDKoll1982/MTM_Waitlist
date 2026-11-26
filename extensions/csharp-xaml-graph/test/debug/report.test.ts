import { describe, expect, it } from 'vitest';
import type { BuildSectionsInput, DebugRow, DebugSection, HostFacts, RowTone, WebviewFacts } from '../../src/debug/model';
import { buildSections } from '../../src/debug/report';
import { renderDebugHtml, sectionText } from '../../src/debug/reportHtml';
import type { CleanupSummary, Finding, GraphEdge, GraphModel, GraphNode, GraphStats, ParserTally } from '../../src/graph/model';

/* -------------------------------------------------------------- fixtures */

const SECTION_IDS = ['host', 'settings', 'analysis', 'parsers', 'findings', 'canvas', 'commands', 'packaging'];

const EMPTY_TALLY: ParserTally = {
  typesDeclared: 0,
  partialTypes: 0,
  ambiguousSimpleNames: 0,
  namespaces: 0,
  modules: 0,
  xamlWithClass: 0,
  xamlWithoutClass: 0,
  bindings: 0,
  templateBindings: 0,
  classicBindings: 0,
  elementTags: 0,
  handlerNames: 0,
  dataTypes: 0,
  edgesDropped: 0,
  edgesMerged: 0,
};

const CLEAN_CLEANUP: CleanupSummary = {
  cycles: 0,
  deadBindings: 0,
  unboundViews: 0,
  unusedTypes: 0,
  unusedMembers: 0,
};

const NODES: GraphNode[] = [
  { id: 'Shop.Views.MainPage', label: 'MainPage', fullName: 'Shop.Views.MainPage', layer: 'view', module: 'Shop', kind: 'page' },
  {
    id: 'Shop.ViewModels.MainViewModel',
    label: 'MainViewModel',
    fullName: 'Shop.ViewModels.MainViewModel',
    layer: 'viewmodel',
    module: 'Shop',
    kind: 'class',
  },
  {
    id: 'Shop.Services.DataService',
    label: 'DataService',
    fullName: 'Shop.Services.DataService',
    layer: 'service',
    module: 'Shop',
    kind: 'class',
  },
];

const EDGES: GraphEdge[] = [
  { id: 'e1', source: 'Shop.Views.MainPage', target: 'Shop.ViewModels.MainViewModel', kind: 'binds', confidence: 'high' },
  {
    id: 'e2',
    source: 'Shop.ViewModels.MainViewModel',
    target: 'Shop.Services.DataService',
    kind: 'injects',
    confidence: 'high',
  },
];

const STATS: GraphStats = {
  filesAnalysed: 12,
  csharpFiles: 8,
  xamlFiles: 4,
  parseErrors: 0,
  parseWarnings: 0,
  elapsedMs: 42,
  truncated: false,
  parser: EMPTY_TALLY,
};

const HOST: HostFacts = {
  extensionVersion: '0.4.0',
  vscodeVersion: '1.104.0',
  extensionMode: 'development',
  appHost: 'desktop',
  uiKind: 'Desktop',
  remoteName: '',
  isTrusted: true,
  platform: 'win32',
  arch: 'x64',
  nodeVersion: 'v20.18.0',
  locale: 'en-us',
  extensionPath: 'C:\\ext\\csharp-xaml-graph',
  workspaceFolders: ['C:\\work\\shop', 'C:\\work\\shop.Tests'],
};

const WEBVIEW: WebviewFacts = {
  ready: true,
  modelLoadedAt: '2026-11-25T11:58:05.000',
  nodesDrawn: 3,
  edgesDrawn: 2,
  hiddenOrphans: 0,
  layout: 'layered-tb',
  density: 'normal',
  labels: 'zoom',
  zoom: 1.25,
  canvasWidth: 800.4,
  canvasHeight: 599.6,
  module: 'Shop',
  search: '',
  layers: ['view', 'viewmodel'],
  edgeKinds: ['resolves', 'injects'],
  focus: null,
  focusDepth: 1,
  scope: null,
  scopeExact: false,
  unpacked: [],
  history: [],
};

function cleanup(overrides: Partial<CleanupSummary> = {}): CleanupSummary {
  return { ...CLEAN_CLEANUP, ...overrides };
}

function tally(overrides: Partial<ParserTally> = {}): ParserTally {
  return { ...EMPTY_TALLY, ...overrides };
}

function graphModel(overrides: Partial<GraphModel> = {}, stats: Partial<GraphStats> = {}): GraphModel {
  return {
    workspace: 'C:\\work\\shop',
    generatedAt: '2026-11-25T11:58:00.000Z',
    nodes: NODES,
    edges: EDGES,
    findings: [],
    cleanup: cleanup(),
    stats: { ...STATS, ...stats },
    warnings: [],
    ...overrides,
  };
}

function findings(count: number): Finding[] {
  return Array.from({ length: count }, (_, index) => ({
    id: `f${index}`,
    kind: 'cycle' as const,
    severity: 'high' as const,
    title: `cycle ${index}`,
    detail: `cycle ${index}`,
    nodeIds: [],
  }));
}

type Overrides = Omit<Partial<BuildSectionsInput>, 'host'> & { host?: Partial<HostFacts> };

/** A snapshot with nothing wrong with it, so each test states only the fact it is about. */
function input(overrides: Overrides = {}): BuildSectionsInput {
  const base: BuildSectionsInput = {
    host: HOST,
    settings: [{ key: 'csharpXamlGraph.maxNodes', value: '4000', source: 'default' }],
    contributedCommands: ['csharpXamlGraph.show', 'csharpXamlGraph.debug'],
    contributedMenus: [{ menu: 'editor/context', commands: ['csharpXamlGraph.showForSelection'] }],
    assets: [{ path: 'out/extension.js', exists: true, bytes: 2048 }],
    activationEvents: ['onCommand:csharpXamlGraph.show'],
    views: [{ id: 'csharpXamlGraph.menu', type: 'webview' }],
    model: graphModel(),
    modelAgeMs: 30_000,
    cacheInvalidated: false,
    webview: { ...WEBVIEW },
    registeredCommands: ['csharpXamlGraph.show', 'csharpXamlGraph.debug'],
  };

  return { ...base, ...overrides, host: { ...HOST, ...overrides.host } };
}

function section(sections: DebugSection[], id: string): DebugSection {
  const found = sections.find((entry) => entry.id === id);
  expect(found, `section ${id} is missing`).toBeDefined();
  return found!;
}

function row(sections: DebugSection[], id: string, label: string): DebugRow {
  const found = section(sections, id).rows.find((entry) => entry.label === label);
  expect(found, `row "${label}" is missing from ${id}`).toBeDefined();
  return found!;
}

/** Asserts what the report says: this named row carries this value, read with this tone. */
function expectRow(sections: DebugSection[], id: string, label: string, value: string, tone?: RowTone): void {
  const found = row(sections, id, label);
  expect(found.value).toBe(value);
  expect(found.tone).toBe(tone);
}

const WEBVIEW_ONLY = (overrides: Partial<WebviewFacts>): BuildSectionsInput => input({ webview: { ...WEBVIEW, ...overrides } });

/* --------------------------------------------------------------- sections */

describe('buildSections — shape', () => {
  it('reports the eight sections in reading order, each with a purpose, owning files and at least one row', () => {
    const sections = buildSections(input());

    expect(sections.map((entry) => entry.id)).toEqual(SECTION_IDS);

    for (const entry of sections) {
      expect(entry.rows.length, `${entry.id} has no rows`).toBeGreaterThan(0);
      expect(entry.category.length).toBeGreaterThan(0);
      expect(entry.title.length).toBeGreaterThan(0);
      expect(entry.purpose.length).toBeGreaterThan(0);
      expect(entry.files.length, `${entry.id} names no files`).toBeGreaterThan(0);
    }
  });

  it('leaves a healthy snapshot with no warning or fault row at all', () => {
    const rows = buildSections(input()).flatMap((entry) => entry.rows);

    expect(rows.length).toBeGreaterThan(20);
    for (const entry of rows) {
      expect(['plain', 'good', undefined], `row "${entry.label}" reads ${entry.tone}`).toContain(entry.tone);
    }
  });
});

describe('buildSections — host', () => {
  it('names the build, the editor and the machine in one place', () => {
    const sections = buildSections(input());

    expectRow(sections, 'host', 'Extension version', '0.4.0');
    expectRow(sections, 'host', 'VS Code', '1.104.0');
    expectRow(sections, 'host', 'Host', 'desktop');
    expectRow(sections, 'host', 'UI kind', 'Desktop');
    expectRow(sections, 'host', 'Node', 'v20.18.0');
    expectRow(sections, 'host', 'Locale', 'en-us');
    expectRow(sections, 'host', 'Extension path', 'C:\\ext\\csharp-xaml-graph');
    expectRow(sections, 'host', 'Platform', 'win32 / x64');
  });

  it('reads a production build as plain and a development build as good', () => {
    expectRow(buildSections(input({ host: { extensionMode: 'production' } })), 'host', 'Extension mode', 'production', 'plain');
    expectRow(buildSections(input({ host: { extensionMode: 'development' } })), 'host', 'Extension mode', 'development', 'good');
  });

  it('warns when the workspace is not trusted and reads a trusted one as plain', () => {
    expectRow(buildSections(input({ host: { isTrusted: false } })), 'host', 'Workspace trusted', 'no', 'warn');
    expectRow(buildSections(input()), 'host', 'Workspace trusted', 'yes', 'plain');
  });

  it('lists the open folders, and warns when the window has none open', () => {
    expectRow(buildSections(input()), 'host', 'Workspace folders', 'C:\\work\\shop, C:\\work\\shop.Tests', 'plain');
    expectRow(buildSections(input({ host: { workspaceFolders: [] } })), 'host', 'Workspace folders', 'none open', 'warn');
  });

  it('says none when the window is not remote, and names the remote when it is', () => {
    expectRow(buildSections(input()), 'host', 'Remote', 'none');
    expectRow(buildSections(input({ host: { remoteName: 'ssh-remote plant-host' } })), 'host', 'Remote', 'ssh-remote plant-host');
  });
});

describe('buildSections — settings', () => {
  it('renders one row per setting, with the value in force and where it came from', () => {
    const sections = buildSections(
      input({
        settings: [
          { key: 'csharpXamlGraph.maxNodes', value: '4000', source: 'default' },
          { key: 'csharpXamlGraph.includeExternalTypes', value: 'false', source: 'workspace' },
        ],
      }),
    );

    expectRow(sections, 'settings', 'csharpXamlGraph.maxNodes', '4000  (default)', 'plain');
    expectRow(sections, 'settings', 'csharpXamlGraph.includeExternalTypes', 'false  (workspace)', 'plain');
    expect(section(sections, 'settings').rows).toHaveLength(2);
  });

  it('warns when the manifest contributes no settings at all', () => {
    const sections = buildSections(input({ settings: [] }));

    expect(section(sections, 'settings').rows).toHaveLength(1);
    expectRow(sections, 'settings', 'Settings', 'empty — the manifest contributes no settings', 'warn');
  });
});

describe('buildSections — analysis', () => {
  it('summarises the model, the files behind it and when it was generated', () => {
    const sections = buildSections(input());

    expectRow(sections, 'analysis', 'Model', '3 types, 2 edges, 0 findings');
    expectRow(sections, 'analysis', 'Files', '8 C#, 4 XAML in 42 ms');
    expectRow(sections, 'analysis', 'Workspace', 'C:\\work\\shop');
    expectRow(sections, 'analysis', 'Generated', '2026-11-25T11:58:00.000Z');
  });

  it('explains an empty model, and separately an empty model whose cache was invalidated', () => {
    const empty = buildSections(input({ model: undefined }));
    expectRow(empty, 'analysis', 'Model', 'not analysed yet — the model is empty', 'warn');
    expect(section(empty, 'analysis').rows).toHaveLength(2);

    const invalidated = buildSections(input({ model: undefined, cacheInvalidated: true }));
    expectRow(invalidated, 'analysis', 'Model', 'not analysed yet — the cache was invalidated, so the model is empty', 'warn');
  });

  it('faults any parse error, and warns about parse warnings only', () => {
    expectRow(buildSections(input()), 'analysis', 'Parse errors', '0', 'good');
    expectRow(buildSections(input({ model: graphModel({}, { parseErrors: 1 }) })), 'analysis', 'Parse errors', '1', 'bad');

    expectRow(buildSections(input()), 'analysis', 'Parse warnings', '0', 'good');
    expectRow(buildSections(input({ model: graphModel({}, { parseWarnings: 2 }) })), 'analysis', 'Parse warnings', '2', 'warn');
  });

  it('warns only when the node cap actually truncated the model', () => {
    expectRow(buildSections(input()), 'analysis', 'Truncated', 'no', 'plain');
    expectRow(
      buildSections(input({ model: graphModel({}, { truncated: true }) })),
      'analysis',
      'Truncated',
      'yes — the node cap was reached',
      'warn',
    );
  });

  it('warns about a stale model only once it is older than ten minutes', () => {
    expectRow(buildSections(input({ modelAgeMs: 600_000 })), 'analysis', 'Age', '10.0 min old', 'plain');
    expectRow(buildSections(input({ modelAgeMs: 600_001 })), 'analysis', 'Age', '10.0 min old', 'warn');
  });

  it('reads a valid cache as good and an invalidated one as a warning', () => {
    expectRow(buildSections(input({ cacheInvalidated: false })), 'analysis', 'Cache', 'valid', 'good');
    expectRow(buildSections(input({ cacheInvalidated: true })), 'analysis', 'Cache', 'invalidated — next open re-reads', 'warn');
  });

  describe('age formatting', () => {
    const age = (modelAgeMs: number | undefined): DebugRow =>
      row(buildSections(input({ modelAgeMs })), 'analysis', 'Age');

    it('says never analysed when no age was recorded', () => {
      expect(age(undefined).value).toBe('never analysed');
      expect(age(undefined).tone).toBe('plain');
    });

    it('reports a model younger than a minute in seconds', () => {
      expect(age(12_000).value).toBe('12.0 s old');
    });

    it('keeps counting seconds up to ninety of them', () => {
      expect(age(89_000).value).toBe('89.0 s old');
    });

    it('switches to minutes at ninety seconds', () => {
      expect(age(90_000).value).toBe('1.5 min old');
    });

    it('reports minutes while the model is under ninety minutes old', () => {
      expect(age(600_000).value).toBe('10.0 min old');
    });

    it('switches to hours at ninety minutes', () => {
      expect(age(5_400_000).value).toBe('1.5 h old');
    });

    it('reports hours for a model far older than that', () => {
      const ninetyDays = 90 * 24 * 60 * 60 * 1000;
      expect(age(ninetyDays).value).toBe('2160.0 h old');
      expect(age(ninetyDays).tone).toBe('warn');
    });
  });
});

describe('buildSections — parsers', () => {
  it('counts what the parse pass saw, reading clean counters as good or plain', () => {
    const sections = buildSections(
      input({
        model: graphModel({}, {
          parser: tally({
            typesDeclared: 40,
            partialTypes: 2,
            ambiguousSimpleNames: 0,
            namespaces: 6,
            modules: 3,
            xamlWithClass: 12,
            xamlWithoutClass: 0,
            bindings: 90,
            templateBindings: 4,
            classicBindings: 7,
            elementTags: 300,
            handlerNames: 22,
            dataTypes: 9,
            edgesDropped: 0,
            edgesMerged: 0,
          }),
        }),
      }),
    );

    expectRow(sections, 'parsers', 'Types declared', '40');
    expectRow(sections, 'parsers', 'Partial types', '2');
    expectRow(sections, 'parsers', 'Namespaces', '6');
    expectRow(sections, 'parsers', 'Modules', '3');
    expectRow(sections, 'parsers', 'XAML with x:Class', '12');
    expectRow(sections, 'parsers', 'x:Bind', '90');
    expectRow(sections, 'parsers', 'Element tags', '300');
    expectRow(sections, 'parsers', 'Handler names', '22');
    expectRow(sections, 'parsers', 'Data types', '9');
    expectRow(sections, 'parsers', 'Ambiguous simple names', '0', 'good');
    expectRow(sections, 'parsers', 'XAML without x:Class', '0', 'plain');
    expectRow(sections, 'parsers', 'Edges dropped', '0', 'good');
  });

  it('warns on ambiguous names, classless XAML and dropped edges', () => {
    const sections = buildSections(
      input({ model: graphModel({}, { parser: tally({ ambiguousSimpleNames: 1, xamlWithoutClass: 1, edgesDropped: 3 }) }) }),
    );

    expectRow(sections, 'parsers', 'Ambiguous simple names', '1', 'warn');
    expectRow(sections, 'parsers', 'XAML without x:Class', '1', 'warn');
    expectRow(sections, 'parsers', 'Edges dropped', '3', 'warn');
  });

  it('warns when this build recorded no parser tally at all', () => {
    const sections = buildSections(input({ model: graphModel({}, { parser: undefined }) }));

    expect(section(sections, 'parsers').rows).toHaveLength(1);
    expectRow(sections, 'parsers', 'Parser pass', 'no tally recorded by this build', 'warn');
  });
});

describe('buildSections — findings', () => {
  it('reads every clean count and an empty finding list as good', () => {
    const sections = buildSections(input());

    expectRow(sections, 'findings', 'Cycles', '0', 'good');
    expectRow(sections, 'findings', 'Dead bindings', '0', 'good');
    expectRow(sections, 'findings', 'Unbound views', '0', 'good');
    expectRow(sections, 'findings', 'Unused types', '0', 'good');
    expectRow(sections, 'findings', 'Unused members', '0', 'good');
    expectRow(sections, 'findings', 'Total findings', '0', 'good');
  });

  it('faults cycles, dead bindings and unbound views, and only warns about unused declarations', () => {
    const sections = buildSections(
      input({
        model: graphModel({
          findings: findings(2),
          cleanup: cleanup({ cycles: 2, deadBindings: 1, unboundViews: 3, unusedTypes: 4, unusedMembers: 5 }),
        }),
      }),
    );

    expectRow(sections, 'findings', 'Cycles', '2', 'bad');
    expectRow(sections, 'findings', 'Dead bindings', '1', 'bad');
    expectRow(sections, 'findings', 'Unbound views', '3', 'bad');
    expectRow(sections, 'findings', 'Unused types', '4', 'warn');
    expectRow(sections, 'findings', 'Unused members', '5', 'warn');
    expectRow(sections, 'findings', 'Total findings', '2', 'warn');
  });

  it('warns that there is nothing to report before anything has been analysed', () => {
    const sections = buildSections(input({ model: undefined }));

    expect(section(sections, 'findings').rows).toHaveLength(1);
    expectRow(sections, 'findings', 'Findings', 'nothing analysed yet', 'warn');
  });
});

describe('buildSections — canvas', () => {
  it('warns when no graph panel is open', () => {
    const sections = buildSections(input({ webview: undefined }));

    expect(section(sections, 'canvas').rows).toHaveLength(1);
    expectRow(sections, 'canvas', 'Canvas', 'not open — no graph panel is showing', 'warn');
  });

  it('reads a ready panel as good and an unready one as a fault', () => {
    expectRow(buildSections(WEBVIEW_ONLY({ ready: true })), 'canvas', 'Ready', 'yes', 'good');
    expectRow(buildSections(WEBVIEW_ONLY({ ready: false })), 'canvas', 'Ready', 'no', 'bad');
  });

  it('counts what was drawn and rounds the canvas it was drawn on', () => {
    const sections = buildSections(input());

    expectRow(sections, 'canvas', 'Drawn', '3 nodes, 2 edges');
    expectRow(sections, 'canvas', 'Canvas', '800 × 600');
    expectRow(sections, 'canvas', 'Zoom', '1.25');
    expectRow(sections, 'canvas', 'Layout', 'layered-tb (normal)');
    expectRow(sections, 'canvas', 'Labels', 'zoom');
    expectRow(sections, 'canvas', 'Module', 'Shop');
  });

  it('lists the layers and edge kinds in use, or none when nothing is filtered or shown', () => {
    expectRow(buildSections(input()), 'canvas', 'Layers', 'view, viewmodel');
    expectRow(buildSections(input()), 'canvas', 'Edges shown', 'resolves, injects');
    expectRow(buildSections(WEBVIEW_ONLY({ layers: [], edgeKinds: [] })), 'canvas', 'Layers', 'none');
    expectRow(buildSections(WEBVIEW_ONLY({ layers: [], edgeKinds: [] })), 'canvas', 'Edges shown', 'none');
  });

  it('counts hidden unconnected nodes as plain information either way', () => {
    expectRow(buildSections(input()), 'canvas', 'Hidden unconnected', '0', 'plain');
    expectRow(buildSections(WEBVIEW_ONLY({ hiddenOrphans: 7 })), 'canvas', 'Hidden unconnected', '7', 'plain');
  });

  it('shows (none) for an empty search and the typed text otherwise', () => {
    expectRow(buildSections(input()), 'canvas', 'Search', '(none)');
    expectRow(buildSections(WEBVIEW_ONLY({ search: 'MainPage' })), 'canvas', 'Search', 'MainPage');
  });

  it('shows (none) for no focus and the hop count when a node is focused', () => {
    expectRow(buildSections(input()), 'canvas', 'Focus', '(none)');
    expectRow(
      buildSections(WEBVIEW_ONLY({ focus: 'Shop.Views.MainPage', focusDepth: 2 })),
      'canvas',
      'Focus',
      'Shop.Views.MainPage (2 hops)',
    );
  });

  it('describes the scope as whole workspace, plain, and exact', () => {
    expectRow(buildSections(input()), 'canvas', 'Scope', '(whole workspace)');
    expectRow(buildSections(WEBVIEW_ONLY({ scope: 'Shop' })), 'canvas', 'Scope', 'Shop');
    expectRow(buildSections(WEBVIEW_ONLY({ scope: 'Shop', scopeExact: true })), 'canvas', 'Scope', 'Shop (exact)');
  });

  it('warns when shapes are unpacked, naming only the first six', () => {
    expectRow(buildSections(input()), 'canvas', 'Unpacked', 'none', 'plain');
    expectRow(
      buildSections(WEBVIEW_ONLY({ unpacked: ['a', 'b', 'c', 'd', 'e', 'f', 'g', 'h'] })),
      'canvas',
      'Unpacked',
      'a, b, c, d, e, f',
      'warn',
    );
  });

  it('shows never when the model has not loaded, and the load time when it has', () => {
    expectRow(buildSections(input()), 'canvas', 'Model loaded', '2026-11-25T11:58:05.000');
    expectRow(buildSections(WEBVIEW_ONLY({ modelLoadedAt: null })), 'canvas', 'Model loaded', 'never');
  });

  it('says none for an empty event log and lists only the last four events otherwise', () => {
    expectRow(buildSections(input()), 'canvas', 'Canvas events', 'none');

    const history = ['one', 'two', 'three', 'four', 'five', 'six'].map((detail, index) => ({
      at: `2026-11-25T11:58:0${index}.000`,
      kind: index < 3 ? 'filter' : 'zoom',
      detail,
    }));

    expectRow(
      buildSections(WEBVIEW_ONLY({ history })),
      'canvas',
      'Canvas events',
      'filter: three | zoom: four | zoom: five | zoom: six',
    );
  });
});

describe('buildSections — commands', () => {
  it('counts contributed and registered commands without counting a duplicate twice', () => {
    const sections = buildSections(
      input({
        contributedCommands: ['csharpXamlGraph.show', 'csharpXamlGraph.debug', 'csharpXamlGraph.show'],
        registeredCommands: ['csharpXamlGraph.show', 'csharpXamlGraph.debug'],
      }),
    );

    expectRow(sections, 'commands', 'Contributed', '2');
    expectRow(sections, 'commands', 'Registered', '2');
  });

  it('reads both comparisons as good when the manifest and activation agree', () => {
    const sections = buildSections(input());

    expectRow(sections, 'commands', 'Contributed but not registered', 'none', 'good');
    expectRow(sections, 'commands', 'Registered but not contributed', 'none', 'good');
  });

  it('faults a contributed command that activation never registered, in sorted order', () => {
    const sections = buildSections(
      input({
        contributedCommands: ['csharpXamlGraph.show', 'csharpXamlGraph.export', 'csharpXamlGraph.debug'],
        registeredCommands: ['csharpXamlGraph.show', 'csharpXamlGraph.debug'],
      }),
    );

    expectRow(sections, 'commands', 'Contributed but not registered', 'csharpXamlGraph.export', 'bad');
  });

  it('warns about a registered command the manifest never contributed', () => {
    const sections = buildSections(
      input({ registeredCommands: ['csharpXamlGraph.show', 'csharpXamlGraph.debug', 'csharpXamlGraph.hidden'] }),
    );

    expectRow(sections, 'commands', 'Registered but not contributed', 'csharpXamlGraph.hidden', 'warn');
  });

  it('lists menus, activation events and views, each separated the way the report reads them', () => {
    const sections = buildSections(
      input({
        contributedMenus: [
          { menu: 'editor/context', commands: ['csharpXamlGraph.showForSelection', 'csharpXamlGraph.showForFile'] },
          { menu: 'explorer/context', commands: ['csharpXamlGraph.showForFile'] },
        ],
        activationEvents: ['onCommand:csharpXamlGraph.show', 'onView:csharpXamlGraph.menu'],
        views: [
          { id: 'csharpXamlGraph.menu', type: 'webview' },
          { id: 'csharpXamlGraph.graph', type: 'webview' },
        ],
      }),
    );

    expectRow(
      sections,
      'commands',
      'Menus',
      'editor/context: csharpXamlGraph.showForSelection, csharpXamlGraph.showForFile | explorer/context: csharpXamlGraph.showForFile',
    );
    expectRow(sections, 'commands', 'Activation events', 'onCommand:csharpXamlGraph.show, onView:csharpXamlGraph.menu');
    expectRow(sections, 'commands', 'Views', 'csharpXamlGraph.menu (webview), csharpXamlGraph.graph (webview)');
  });

  it('says none for menus, activation events and views the manifest does not declare', () => {
    const sections = buildSections(input({ contributedMenus: [], activationEvents: [], views: [] }));

    expectRow(sections, 'commands', 'Menus', 'none');
    expectRow(sections, 'commands', 'Activation events', 'none');
    expectRow(sections, 'commands', 'Views', 'none');
  });
});

describe('buildSections — packaging', () => {
  it('reads a present asset as good and reports its size in whole kilobytes', () => {
    const sections = buildSections(input({ assets: [{ path: 'out/extension.js', exists: true, bytes: 2048 }] }));

    expectRow(sections, 'packaging', 'out/extension.js', 'present (2 KB)', 'good');
    expectRow(sections, 'packaging', 'Bundle health', 'every asset present', 'good');
  });

  it('reports a present asset with no recorded size as present, with no size', () => {
    const sections = buildSections(input({ assets: [{ path: 'out/webview/main.js', exists: true }] }));

    expectRow(sections, 'packaging', 'out/webview/main.js', 'present', 'good');
  });

  it('faults a missing asset and counts it against bundle health', () => {
    const sections = buildSections(input({ assets: [{ path: 'media/tree-sitter-c_sharp.wasm', exists: false }] }));

    expectRow(sections, 'packaging', 'media/tree-sitter-c_sharp.wasm', 'MISSING — media/tree-sitter-c_sharp.wasm is not on disk', 'bad');
    expectRow(sections, 'packaging', 'Bundle health', '1 asset(s) missing — rebuild before packaging', 'bad');
  });

  it('counts every missing asset when more than one is gone', () => {
    const sections = buildSections(
      input({
        assets: [
          { path: 'out/extension.js', exists: true, bytes: 4096 },
          { path: 'media/tree-sitter-c_sharp.wasm', exists: false },
          { path: 'out/webview/main.js', exists: false },
        ],
      }),
    );

    expectRow(sections, 'packaging', 'out/extension.js', 'present (4 KB)', 'good');
    expectRow(sections, 'packaging', 'Bundle health', '2 asset(s) missing — rebuild before packaging', 'bad');
  });

  it('says bundle health is good when no asset is missing, even with no assets listed', () => {
    const sections = buildSections(input({ assets: [] }));

    expect(section(sections, 'packaging').rows).toHaveLength(1);
    expectRow(sections, 'packaging', 'Bundle health', 'every asset present', 'good');
  });
});

/* ------------------------------------------------------------- sectionText */

describe('sectionText', () => {
  it('renders one known section with its heading, purpose, files and rows — and no document title', () => {
    const text = sectionText(buildSections(input()), 'packaging');

    expect(text.startsWith('## Packaging — Bundles, assets and the grammar\n\nWhether the bundles')).toBe(true);
    expect(text).toContain('Files: `scripts/build.mjs`, `.vscodeignore`, `package.json`');
    expect(text).toContain('- **out/extension.js** — present (2 KB) _(good)_');
    expect(text).toContain('- **Bundle health** — every asset present _(good)_');
    expect(text).not.toContain('# C#/XAML Graph — Diagnostics');
    expect(text.endsWith('\n')).toBe(false);
  });

  it('annotates a row that is not plain and leaves a plain row unannotated', () => {
    const text = sectionText(buildSections(input({ host: { extensionMode: 'production', isTrusted: false } })), 'host');

    expect(text).toContain('- **Workspace trusted** — no _(warn)_');
    expect(text).toContain('- **Extension mode** — production\n');
  });

  it('renders every section under a document title when no id is given', () => {
    const sections = buildSections(input());
    const text = sectionText(sections, null);

    expect(text.startsWith('# C#/XAML Graph — Diagnostics\n\n## Host — Host and workspace')).toBe(true);
    for (const entry of sections) {
      expect(text).toContain(`## ${entry.category} — ${entry.title}`);
      expect(text).toContain(entry.purpose);
    }
    expect(text.endsWith('\n')).toBe(false);
  });

  it('returns nothing for an id no section carries', () => {
    expect(sectionText(buildSections(input()), 'does-not-exist')).toBe('');
  });

  it('omits the files line for a section that owns no files', () => {
    const bare: DebugSection = {
      id: 'bare',
      category: 'Bare',
      title: 'No files named',
      purpose: 'Still readable.',
      files: [],
      rows: [{ label: 'Only', value: 'one' }],
    };

    expect(sectionText([bare], 'bare')).toBe('## Bare — No files named\n\nStill readable.\n\n- **Only** — one');
  });
});

/* ---------------------------------------------------------- renderDebugHtml */

describe('renderDebugHtml', () => {
  it('gives every section a nav link, a heading and an anchor of its own', () => {
    const sections = buildSections(input());
    const html = renderDebugHtml(sections, 'nonce-abc');

    expect(html.startsWith('<!DOCTYPE html>')).toBe(true);
    expect(html).toContain('<title>C#/XAML Graph — Diagnostics</title>');
    for (const entry of sections) {
      expect(html).toContain(`<a href="#section-${entry.id}">${entry.category}</a>`);
      expect(html).toContain(`<h2>${entry.category} <span>${entry.title}</span></h2>`);
      expect(html).toContain(`id="section-${entry.id}"`);
    }
  });

  it('escapes markup in ids, headings, purposes, file paths, labels and values', () => {
    const hostile: DebugSection = {
      id: 'x&y',
      category: 'Odd <one>',
      title: 'A "quoted" & angled title',
      purpose: 'Explains <b>nothing</b> & pauses.',
      files: ['src/<file>&.ts'],
      rows: [{ label: '<b>label</b>', value: '<script>&"', tone: 'bad' }],
    };

    const html = renderDebugHtml([hostile], 'nonce-abc');

    expect(html).toContain('id="section-x&amp;y"');
    expect(html).toContain('data-section="x&amp;y"');
    expect(html).toContain('<a href="#section-x&amp;y">Odd &lt;one&gt;</a>');
    expect(html).toContain('<h2>Odd &lt;one&gt; <span>A &quot;quoted&quot; &amp; angled title</span></h2>');
    expect(html).toContain('<p class="purpose">Explains &lt;b&gt;nothing&lt;/b&gt; &amp; pauses.</p>');
    expect(html).toContain('<code>src/&lt;file&gt;&amp;.ts</code>');
    expect(html).toContain('<th scope="row">&lt;b&gt;label&lt;/b&gt;</th>');
    expect(html).toContain('<td style="color:#ef4444">&lt;script&gt;&amp;&quot;</td>');
    expect(html).not.toContain('<script>&');
    expect(html).not.toContain('<b>label</b>');
  });

  it('carries the nonce it was given, in the policy and on the script', () => {
    const html = renderDebugHtml(buildSections(input()), 'nonce-7f3a');

    expect(html).toContain("script-src 'nonce-nonce-7f3a'");
    expect(html).toContain('<script nonce="nonce-7f3a">');
    expect(html.split('nonce-7f3a')).toHaveLength(3);
  });

  it('colours a fault, a warning and a healthy row, and leaves a plain row unstyled', () => {
    const tones: DebugSection = {
      id: 'tones',
      category: 'Tones',
      title: 'Every tone',
      purpose: 'Four rows, one per tone.',
      files: ['src/debug/reportHtml.ts'],
      rows: [
        { label: 'Fault', value: 'fault', tone: 'bad' },
        { label: 'Warning', value: 'warning', tone: 'warn' },
        { label: 'Healthy', value: 'healthy', tone: 'good' },
        { label: 'Information', value: 'information', tone: 'plain' },
        { label: 'Silent', value: 'silent' },
      ],
    };

    const html = renderDebugHtml([tones], 'nonce-abc');

    expect(html).toContain('<td style="color:#ef4444">fault</td>');
    expect(html).toContain('<td style="color:#f59e0b">warning</td>');
    expect(html).toContain('<td style="color:#2fbf71">healthy</td>');
    expect(html).toContain('<td>information</td>');
    expect(html).toContain('<td>silent</td>');
    expect(html).not.toContain('color:inherit');
  });

  it('omits the files paragraph for a section that owns no files', () => {
    const bare: DebugSection = {
      id: 'bare',
      category: 'Bare',
      title: 'No files named',
      purpose: 'Still readable.',
      files: [],
      rows: [{ label: 'Only', value: 'one' }],
    };

    const html = renderDebugHtml([bare], 'nonce-abc');

    expect(html).not.toContain('class="files"');
    expect(html).toContain('<td>one</td>');
  });
});
