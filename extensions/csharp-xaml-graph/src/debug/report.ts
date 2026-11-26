import type {
  BuildSectionsInput,
  DebugRow,
  DebugSection,
  WebviewFacts,
} from './model';

const HEALTHY_LIMIT = 0;

function formatAge(milliseconds: number | undefined): string {
  if (milliseconds === undefined) {
    return 'never analysed';
  }
  const seconds = milliseconds / 1000;
  if (seconds < 90) {
    return `${seconds.toFixed(1)} s old`;
  }
  const minutes = seconds / 60;
  if (minutes < 90) {
    return `${minutes.toFixed(1)} min old`;
  }
  return `${(minutes / 60).toFixed(1)} h old`;
}

function hostSection(input: BuildSectionsInput): DebugSection {
  const { host } = input;
  const rows: DebugRow[] = [
    { label: 'Extension version', value: host.extensionVersion },
    { label: 'VS Code', value: host.vscodeVersion },
    { label: 'Extension mode', value: host.extensionMode, tone: host.extensionMode === 'production' ? 'plain' : 'good' },
    { label: 'Host', value: host.appHost },
    { label: 'UI kind', value: host.uiKind },
    { label: 'Remote', value: host.remoteName || 'none' },
    { label: 'Workspace trusted', value: host.isTrusted ? 'yes' : 'no', tone: host.isTrusted ? 'plain' : 'warn' },
    { label: 'Platform', value: `${host.platform} / ${host.arch}` },
    { label: 'Node', value: host.nodeVersion },
    { label: 'Locale', value: host.locale },
    { label: 'Extension path', value: host.extensionPath },
    {
      label: 'Workspace folders',
      value: host.workspaceFolders.length === 0 ? 'none open' : host.workspaceFolders.join(', '),
      tone: host.workspaceFolders.length === 0 ? 'warn' : 'plain',
    },
  ];

  return {
    id: 'host',
    category: 'Host',
    title: 'Host and workspace',
    purpose: 'Which VS Code, which extension build and which folders this window is running against.',
    files: ['package.json', 'src/extension.ts'],
    rows,
  };
}

function settingsSection(input: BuildSectionsInput): DebugSection {
  const rows: DebugRow[] = input.settings.length === 0
    ? [{ label: 'Settings', value: 'empty — the manifest contributes no settings', tone: 'warn' }]
    : input.settings.map((setting) => ({
      label: setting.key,
      value: `${setting.value}  (${setting.source})`,
      tone: 'plain' as const,
    }));

  return {
    id: 'settings',
    category: 'Settings',
    title: 'Settings in force',
    purpose: 'Each contributed setting with the value actually being used and whether it came from the user or the workspace.',
    files: ['src/graph/settings.ts', 'package.json'],
    rows,
  };
}

function analysisSection(input: BuildSectionsInput): DebugSection {
  const rows: DebugRow[] = [];
  const { model } = input;

  if (!model) {
    rows.push({
      label: 'Model',
      value: input.cacheInvalidated
        ? 'not analysed yet — the cache was invalidated, so the model is empty'
        : 'not analysed yet — the model is empty',
      tone: 'warn',
    });
  } else {
    rows.push({
      label: 'Model',
      value: `${model.nodes.length} types, ${model.edges.length} edges, ${model.findings.length} findings`,
    });
    rows.push({
      label: 'Age',
      value: formatAge(input.modelAgeMs),
      tone: input.modelAgeMs !== undefined && input.modelAgeMs > 10 * 60 * 1000 ? 'warn' : 'plain',
    });
    rows.push({
      label: 'Files',
      value: `${model.stats.csharpFiles} C#, ${model.stats.xamlFiles} XAML in ${model.stats.elapsedMs} ms`,
    });
    rows.push({
      label: 'Parse errors',
      value: String(model.stats.parseErrors),
      tone: model.stats.parseErrors > HEALTHY_LIMIT ? 'bad' : 'good',
    });
    rows.push({
      label: 'Parse warnings',
      value: String(model.stats.parseWarnings),
      tone: model.stats.parseWarnings > HEALTHY_LIMIT ? 'warn' : 'good',
    });
    rows.push({
      label: 'Truncated',
      value: model.stats.truncated ? 'yes — the node cap was reached' : 'no',
      tone: model.stats.truncated ? 'warn' : 'plain',
    });
    rows.push({ label: 'Workspace', value: model.workspace });
    rows.push({ label: 'Generated', value: model.generatedAt });
  }

  rows.push({
    label: 'Cache',
    value: input.cacheInvalidated ? 'invalidated — next open re-reads' : 'valid',
    tone: input.cacheInvalidated ? 'warn' : 'good',
  });

  return {
    id: 'analysis',
    category: 'Analysis',
    title: 'What the last run saw',
    purpose: 'The size, age and health of the analysed model, so a stale or truncated result is obvious before it is trusted.',
    files: ['src/graph/analyze.ts', 'src/graph/model.ts'],
    rows,
  };
}

function parsersSection(input: BuildSectionsInput): DebugSection {
  const tally = input.model?.stats.parser;
  const rows: DebugRow[] = tally
    ? [
      { label: 'Types declared', value: String(tally.typesDeclared) },
      { label: 'Partial types', value: String(tally.partialTypes) },
      {
        label: 'Ambiguous simple names',
        value: String(tally.ambiguousSimpleNames),
        tone: tally.ambiguousSimpleNames > 0 ? 'warn' : 'good',
      },
      { label: 'Namespaces', value: String(tally.namespaces) },
      { label: 'Modules', value: String(tally.modules) },
      { label: 'XAML with x:Class', value: String(tally.xamlWithClass) },
      {
        label: 'XAML without x:Class',
        value: String(tally.xamlWithoutClass),
        tone: tally.xamlWithoutClass > 0 ? 'warn' : 'plain',
      },
      { label: 'x:Bind', value: String(tally.bindings) },
      { label: 'Template bindings', value: String(tally.templateBindings) },
      { label: 'Classic bindings', value: String(tally.classicBindings) },
      { label: 'Element tags', value: String(tally.elementTags) },
      { label: 'Handler names', value: String(tally.handlerNames) },
      { label: 'Data types', value: String(tally.dataTypes) },
      {
        label: 'Edges dropped',
        value: String(tally.edgesDropped),
        tone: tally.edgesDropped > 0 ? 'warn' : 'good',
      },
    ]
    : [{ label: 'Parser pass', value: 'no tally recorded by this build', tone: 'warn' }];

  return {
    id: 'parsers',
    category: 'Parsers',
    title: 'C# and XAML parsers',
    purpose: 'What the parse pass actually saw — declarations, bindings and dropped edges — rather than only what the graph kept.',
    files: ['src/graph/csharp/parser.ts', 'src/graph/xaml/parser.ts', 'src/graph/csharp/symbols.ts'],
    rows,
  };
}

function findingsSection(input: BuildSectionsInput): DebugSection {
  const cleanup = input.model?.cleanup;
  const rows: DebugRow[] = cleanup
    ? [
      { label: 'Cycles', value: String(cleanup.cycles), tone: cleanup.cycles > 0 ? 'bad' : 'good' },
      { label: 'Dead bindings', value: String(cleanup.deadBindings), tone: cleanup.deadBindings > 0 ? 'bad' : 'good' },
      { label: 'Unbound views', value: String(cleanup.unboundViews), tone: cleanup.unboundViews > 0 ? 'bad' : 'good' },
      { label: 'Unused types', value: String(cleanup.unusedTypes), tone: cleanup.unusedTypes > 0 ? 'warn' : 'good' },
      { label: 'Unused members', value: String(cleanup.unusedMembers), tone: cleanup.unusedMembers > 0 ? 'warn' : 'good' },
      {
        label: 'Total findings',
        value: String(input.model?.findings.length ?? 0),
        tone: (input.model?.findings.length ?? 0) > 0 ? 'warn' : 'good',
      },
    ]
    : [{ label: 'Findings', value: 'nothing analysed yet', tone: 'warn' }];

  return {
    id: 'findings',
    category: 'Findings',
    title: 'Cleanup findings',
    purpose: 'How many cycles, dead bindings and unused declarations the last run reported, by category.',
    files: ['src/graph/findings.ts'],
    rows,
  };
}

function canvasRows(webview: WebviewFacts | undefined): DebugRow[] {
  if (!webview) {
    return [{ label: 'Canvas', value: 'not open — no graph panel is showing', tone: 'warn' }];
  }

  return [
    {
      label: 'Ready',
      value: webview.ready ? 'yes' : 'no',
      tone: webview.ready ? 'good' : 'bad',
    },
    { label: 'Drawn', value: `${webview.nodesDrawn} nodes, ${webview.edgesDrawn} edges` },
    {
      label: 'Hidden unconnected',
      value: String(webview.hiddenOrphans),
      tone: webview.hiddenOrphans > 0 ? 'plain' : 'plain',
    },
    { label: 'Layers', value: webview.layers.length === 0 ? 'none' : webview.layers.join(', ') },
    { label: 'Edges shown', value: webview.edgeKinds.length === 0 ? 'none' : webview.edgeKinds.join(', ') },
    { label: 'Layout', value: `${webview.layout} (${webview.density})` },
    { label: 'Labels', value: webview.labels },
    { label: 'Zoom', value: webview.zoom.toFixed(2) },
    { label: 'Canvas', value: `${Math.round(webview.canvasWidth)} × ${Math.round(webview.canvasHeight)}` },
    { label: 'Module', value: webview.module },
    { label: 'Search', value: webview.search || '(none)' },
    {
      label: 'Focus',
      value: webview.focus ? `${webview.focus} (${webview.focusDepth} hops)` : '(none)',
    },
    {
      label: 'Scope',
      value: webview.scope ? `${webview.scope}${webview.scopeExact ? ' (exact)' : ''}` : '(whole workspace)',
    },
    {
      label: 'Unpacked',
      value: webview.unpacked.length === 0 ? 'none' : webview.unpacked.slice(0, 6).join(', '),
      tone: webview.unpacked.length === 0 ? 'plain' : 'warn',
    },
    { label: 'Model loaded', value: webview.modelLoadedAt ?? 'never' },
    {
      label: 'Canvas events',
      value: webview.history.length === 0
        ? 'none'
        : webview.history.slice(-4).map((entry) => `${entry.kind}: ${entry.detail}`).join(' | '),
    },
  ];
}

function canvasSection(input: BuildSectionsInput): DebugSection {
  return {
    id: 'canvas',
    category: 'Canvas',
    title: 'What the panel is drawing',
    purpose: 'The filters and view state the open graph is using, so a surprising picture can be explained from facts rather than guessed at.',
    files: ['src/webview/client/main.ts', 'src/webview/client/view.ts', 'src/webview/client/packing.ts'],
    rows: canvasRows(input.webview),
  };
}

function commandsSection(input: BuildSectionsInput): DebugSection {
  const contributed = new Set(input.contributedCommands);
  const registered = new Set(input.registeredCommands);

  const notRegistered = [...contributed].filter((command) => !registered.has(command)).sort();
  const notContributed = [...registered].filter((command) => !contributed.has(command)).sort();

  const rows: DebugRow[] = [
    { label: 'Contributed', value: String(contributed.size) },
    { label: 'Registered', value: String(registered.size) },
    {
      label: 'Contributed but not registered',
      value: notRegistered.length === 0 ? 'none' : notRegistered.join(', '),
      tone: notRegistered.length === 0 ? 'good' : 'bad',
    },
    {
      label: 'Registered but not contributed',
      value: notContributed.length === 0 ? 'none' : notContributed.join(', '),
      tone: notContributed.length === 0 ? 'good' : 'warn',
    },
    {
      label: 'Menus',
      value: input.contributedMenus.length === 0
        ? 'none'
        : input.contributedMenus.map((menu) => `${menu.menu}: ${menu.commands.join(', ')}`).join(' | '),
    },
    {
      label: 'Activation events',
      value: input.activationEvents.length === 0 ? 'none' : input.activationEvents.join(', '),
    },
    {
      label: 'Views',
      value: input.views.length === 0
        ? 'none'
        : input.views.map((view) => `${view.id} (${view.type})`).join(', '),
    },
  ];

  return {
    id: 'commands',
    category: 'Commands',
    title: 'Commands, menus and views',
    purpose: 'Compares the manifest with what activation actually registered, which is where a command that does nothing shows up.',
    files: ['src/extension.ts', 'src/menu/menuView.ts', 'src/menu/menuHtml.ts'],
    rows,
  };
}

function packagingSection(input: BuildSectionsInput): DebugSection {
  const rows: DebugRow[] = input.assets.map((asset) => {
    if (!asset.exists) {
      return { label: asset.path, value: `MISSING — ${asset.path} is not on disk`, tone: 'bad' as const };
    }
    const size = asset.bytes === undefined ? '' : ` (${(asset.bytes / 1024).toFixed(0)} KB)`;
    return { label: asset.path, value: `present${size}`, tone: 'good' as const };
  });

  const missing = input.assets.filter((asset) => !asset.exists).length;
  rows.push({
    label: 'Bundle health',
    value: missing === 0 ? 'every asset present' : `${missing} asset(s) missing — rebuild before packaging`,
    tone: missing === 0 ? 'good' : 'bad',
  });

  return {
    id: 'packaging',
    category: 'Packaging',
    title: 'Bundles, assets and the grammar',
    purpose: 'Whether the bundles and the C# grammar the extension loads at run time are actually present in this build.',
    files: ['scripts/build.mjs', '.vscodeignore', 'package.json'],
    rows,
  };
}

/**
 * Builds the eight report sections, in reading order: host, settings, analysis, parsers, findings,
 * canvas, commands, packaging.
 *
 * Each section says what it is for and names the files that own it, so the report is a map of the
 * extension as well as a snapshot of it.
 */
export function buildSections(input: BuildSectionsInput): DebugSection[] {
  return [
    hostSection(input),
    settingsSection(input),
    analysisSection(input),
    parsersSection(input),
    findingsSection(input),
    canvasSection(input),
    commandsSection(input),
    packagingSection(input),
  ];
}
