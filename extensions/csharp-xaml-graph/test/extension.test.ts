import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import * as fs from 'node:fs';
import { existsSync, mkdirSync, mkdtempSync, readFileSync, readdirSync, rmSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { dirname, join } from 'node:path';
import { activate, deactivate } from '../src/extension';
import {
  Position, ProgressLocation, Range, Selection, Uri, fireConfigurationChange, makeContext, resetVscodeState, state,
} from './support/vscode';

const extensionRoot = process.cwd();

const CONTRIBUTED = [
  'csharpXamlGraph.show',
  'csharpXamlGraph.refresh',
  'csharpXamlGraph.debug',
  'csharpXamlGraph.showForSelection',
  'csharpXamlGraph.showForFile',
  'csharpXamlGraph.export',
  'csharpXamlGraph.openSettings',
  'csharpXamlGraph.openMenu',
  'csharpXamlGraph.profile.initialize',
  'csharpXamlGraph.profile.import',
  'csharpXamlGraph.profile.refresh',
];

const flush = async () => {
  for (let index = 0; index < 8; index += 1) {
    await new Promise((resolve) => setTimeout(resolve, 0));
  }
};

const run = (command: string, ...args: unknown[]) =>
  (state.commandHandlers[command] as (...rest: unknown[]) => Promise<unknown>)(...args);

/**
 * The last message of one level. The no-profile offer is an information message raised beside the
 * analysis, so an assertion about a warning asks for the last warning rather than the last message.
 */
const lastOf = (messages: { level: string }[], level: string) =>
  messages.filter((message) => message.level === level).at(-1);

function workspaceWith(entries: Record<string, string>, folder = '/work'): void {
  state.folders = [{ uri: Uri.file(folder), name: folder.split('/').pop() ?? folder, index: 0 }];
  const uris: Uri[] = [];
  for (const [relative, text] of Object.entries(entries)) {
    const path = `${folder}/${relative}`;
    state.fileContents[path] = text;
    uris.push(Uri.file(path));
  }
  state.findFilesResult = uris;
}

/** Closes any panel a previous test left open, so the singletons start clean. */
function closePanels(): void {
  for (const panel of [...state.panels]) {
    panel.dispose();
  }
}

beforeEach(() => {
  closePanels();
  resetVscodeState();
});

describe('activate', () => {
  it('registers every contributed command, the panel-tab view and the settings listener', () => {
    const context = makeContext(extensionRoot);
    activate(context as never);

    expect(state.registeredCommands).toEqual(CONTRIBUTED);
    expect(state.viewProviders).toHaveLength(1);
    expect(state.viewProviders[0].id).toBe('csharpXamlGraph.menu');
    expect(state.configurationListeners).toHaveLength(1);
    expect(context.subscriptions.length).toBeGreaterThanOrEqual(10);
  });

  it('has nothing to release on deactivate', () => {
    expect(() => deactivate()).not.toThrow();
  });

  it('declares in the manifest exactly the commands activation registers', () => {
    const manifest = JSON.parse(fs.readFileSync(`${extensionRoot}/package.json`, 'utf8')) as {
      version: string;
      activationEvents: string[];
      contributes: { commands: { command: string; title: string; category: string }[] };
    };
    expect(manifest.contributes.commands.map((entry) => entry.command).sort())
      .toEqual([...CONTRIBUTED].sort());
    for (const entry of manifest.contributes.commands) {
      expect(entry.category).toBe('C#/XAML Graph');
      expect(entry.title.length).toBeGreaterThan(0);
    }
    expect(manifest.activationEvents).toContain('onCommand:csharpXamlGraph.profile.initialize');
    expect(manifest.activationEvents).toContain('onCommand:csharpXamlGraph.profile.import');
    expect(manifest.activationEvents).toContain('onCommand:csharpXamlGraph.profile.refresh');
    expect(manifest.version).toBe('0.6.0');
  });
});

describe('show and refresh', () => {
  it('analyses the workspace behind a notification progress bar and opens the panel', async () => {
    workspaceWith({
      'A/Thing.cs': 'namespace N { public class Thing { } }',
      'A/View.xaml': '<Page x:Class="N.View" />',
    });
    activate(makeContext(extensionRoot) as never);
    await run('csharpXamlGraph.show');

    expect(state.progressOptions).toEqual({
      location: ProgressLocation.Notification,
      title: 'C#/XAML dependency graph',
      cancellable: false,
    });
    const reports = state.progressReports.map((report) => report.message ?? '');
    expect(reports[0]).toContain('Reading the profile');
    expect(reports.some((message) => message.includes('Reading the files'))).toBe(true);
    expect(reports.some((message) => message.includes('Read 2 files'))).toBe(true);
    expect(reports.some((message) => message.includes('Parsing C#'))).toBe(true);
    expect(reports.some((message) => message.includes('Running the cleanup rules'))).toBe(true);
    // The percentages reported to the bar only ever move forward.
    const increments = state.progressReports
      .map((report) => report.increment)
      .filter((increment): increment is number => typeof increment === 'number');
    expect(increments.length).toBeGreaterThan(0);
    expect(increments.reduce((sum, value) => sum + value, 0)).toBeLessThanOrEqual(100);

    const modelMessages = state.panels[0].posted.filter(
      (message) => (message as { type: string }).type === 'model');
    expect(modelMessages).toHaveLength(1);
    expect((modelMessages[0] as { model: { nodes: unknown[] } }).model.nodes).toHaveLength(2);
  });

  it('reuses the analysed model for Show and re-reads the files for Refresh', async () => {
    workspaceWith({ 'A/Thing.cs': 'namespace N { public class Thing { } }' });
    activate(makeContext(extensionRoot) as never);

    await run('csharpXamlGraph.show');
    expect(state.findFilesCalls).toHaveLength(1);

    await run('csharpXamlGraph.show');
    expect(state.findFilesCalls).toHaveLength(1);

    await run('csharpXamlGraph.refresh');
    expect(state.findFilesCalls).toHaveLength(2);
  });

  it('re-reads after a setting that changes what is analysed', async () => {
    workspaceWith({ 'A/Thing.cs': 'namespace N { public class Thing { } }' });
    activate(makeContext(extensionRoot) as never);
    await run('csharpXamlGraph.show');
    expect(state.findFilesCalls).toHaveLength(1);

    fireConfigurationChange(['csharpXamlGraph.include']);
    await run('csharpXamlGraph.show');
    expect(state.findFilesCalls).toHaveLength(2);
  });

  it('only redraws for a presentational setting, pushing the new value to the open panel', async () => {
    workspaceWith({ 'A/Thing.cs': 'namespace N { public class Thing { } }' });
    activate(makeContext(extensionRoot) as never);
    await run('csharpXamlGraph.show');
    await state.panels[0].send({ type: 'ready' });

    state.configValues['csharpXamlGraph.unpackedLayout'] = 'circle';
    fireConfigurationChange(['csharpXamlGraph.unpackedLayout']);
    await flush();

    expect(state.findFilesCalls).toHaveLength(1);
    expect(state.panels[0].posted.at(-1)).toEqual({ type: 'settings', settings: { unpackedLayout: 'circle' } });
  });

  it('ignores a configuration change outside the extension', async () => {
    workspaceWith({ 'A/Thing.cs': 'namespace N { public class Thing { } }' });
    activate(makeContext(extensionRoot) as never);
    await run('csharpXamlGraph.show');

    fireConfigurationChange(['editor.fontSize']);
    await run('csharpXamlGraph.show');
    expect(state.findFilesCalls).toHaveLength(1);
  });

  it('reports a missing workspace folder as an analysis failure', async () => {
    state.folders = [];
    activate(makeContext(extensionRoot) as never);
    await run('csharpXamlGraph.show');

    const error = state.messages.find((message) => message.level === 'error');
    expect(error?.message).toContain('Open the workspace folder');
  });

  it('warns once that a multi-root workspace analyses only the first folder', async () => {
    workspaceWith({ 'A/Thing.cs': 'namespace N { public class Thing { } }' }, '/multi-root-a');
    state.folders.push({ uri: Uri.file('/multi-root-b'), name: 'b', index: 1 });
    activate(makeContext(extensionRoot) as never);

    await run('csharpXamlGraph.show');
    const warnings = () => state.messages.filter((message) => message.message.includes('folders. The dependency graph analyses the first'));
    expect(warnings()).toHaveLength(1);

    await run('csharpXamlGraph.refresh');
    expect(warnings()).toHaveLength(1);
  });

  it('asks for exactly the include and exclude patterns the settings give it', async () => {
    workspaceWith({ 'A/Thing.cs': 'namespace N { public class Thing { } }' });
    activate(makeContext(extensionRoot) as never);
    await run('csharpXamlGraph.show');

    expect((state.findFilesCalls[0].include as { pattern: string }).pattern).toBe('{**/*.cs,**/*.xaml}');
    expect(state.findFilesCalls[0].exclude).toBe(
      '{**/bin/**,**/obj/**,**/node_modules/**,**/out/**,**/TestResults/**,**/*.Tests/**,**/.vs/**,'
      + '**/.github/**,**/.specify/**}',
    );
    expect(state.findFilesCalls[0].max).toBe(20000);
  });

  it('passes a single pattern through and omits an empty exclude', async () => {
    workspaceWith({ 'A/Thing.cs': 'namespace N { public class Thing { } }' });
    state.configValues['csharpXamlGraph.include'] = ['**/*.cs'];
    state.configValues['csharpXamlGraph.exclude'] = [];
    activate(makeContext(extensionRoot) as never);
    await run('csharpXamlGraph.show');

    expect((state.findFilesCalls[0].include as { pattern: string }).pattern).toBe('**/*.cs');
    expect(state.findFilesCalls[0].exclude).toBeUndefined();
  });

  it('warns when the file list hits its cap instead of dropping files silently', async () => {
    workspaceWith({ 'A/Empty.cs': '' });
    const empty = Uri.file('/work/A/Empty.cs');
    state.findFilesResult = Array.from({ length: 20000 }, () => empty);
    activate(makeContext(extensionRoot) as never);
    await run('csharpXamlGraph.show');

    expect(state.messages.some((message) => message.message.includes('stopped at 20000 files'))).toBe(true);
  });

  it('respects the external-types and node-cap settings', async () => {
    workspaceWith({ 'A/Thing.cs': 'namespace N { public class Thing : Missing { } }' });
    state.configValues['csharpXamlGraph.includeExternalTypes'] = false;
    state.configValues['csharpXamlGraph.maxNodes'] = 1;
    activate(makeContext(extensionRoot) as never);
    await run('csharpXamlGraph.show');

    const model = (state.panels[0].posted.find(
      (message) => (message as { type: string }).type === 'model') as { model: { nodes: unknown[]; stats: { truncated: boolean } } }).model;
    expect(model.nodes).toHaveLength(1);
    expect(model.stats.truncated).toBe(false);
  });
});

describe('showForSelection', () => {
  const editorAt = (path: string, word: string | undefined, selectionEmpty = true) => ({
    document: {
      uri: Uri.file(path),
      getWordRangeAtPosition: () => (word ? new Range(new Position(0, 0), new Position(0, word.length)) : undefined),
      getText: () => word ?? '',
    },
    selection: {
      isEmpty: selectionEmpty,
      active: new Position(2, 4),
      start: new Position(1, 0),
    },
  });

  it('warns when no editor is open', async () => {
    activate(makeContext(extensionRoot) as never);
    await run('csharpXamlGraph.showForSelection');
    expect(lastOf(state.messages, 'warning')).toMatchObject({
      level: 'warning',
      message: 'Open a C# or XAML file, then right-click the type you want to see.',
    });
  });

  it('opens the panel on the type under the cursor', async () => {
    workspaceWith({
      'A/Thing.cs': 'namespace N { public class Thing { } }',
      'A/Other.cs': 'namespace N { public class Other { } }',
    });
    activate(makeContext(extensionRoot) as never);
    state.activeTextEditor = editorAt('/work/A/Thing.cs', 'Thing');
    await run('csharpXamlGraph.showForSelection');

    expect(state.panels).toHaveLength(1);
    expect(state.panels[0].posted.some((message) =>
      (message as { type?: string }).type === 'progress')).toBe(true);
  });

  it('uses the start of a non-empty selection', async () => {
    workspaceWith({ 'A/Thing.cs': 'namespace N { public class Thing { } }' });
    activate(makeContext(extensionRoot) as never);
    state.activeTextEditor = editorAt('/work/A/Thing.cs', 'Thing', false);
    await run('csharpXamlGraph.showForSelection');
    expect(state.panels).toHaveLength(1);
  });

  it('warns when nothing under the cursor maps to a type', async () => {
    workspaceWith({ 'A/Thing.cs': 'namespace N { public class Thing { } }' });
    activate(makeContext(extensionRoot) as never);
    state.activeTextEditor = editorAt('/work/A/Thing.cs', undefined);
    await run('csharpXamlGraph.showForSelection');

    expect(lastOf(state.messages, 'warning')).toMatchObject({
      level: 'warning',
      message: 'Nothing on that line maps to a type in the analysed workspace.',
    });
    expect(state.panels).toHaveLength(0);
  });

  it('offers a picker when the selection could mean more than one graph', async () => {
    workspaceWith({
      'A/SampleViewModel.cs': 'namespace N { public class SampleViewModel { } }',
      'A/Deeper.cs': 'namespace M { public class SampleViewModel { } }',
    });
    activate(makeContext(extensionRoot) as never);
    state.activeTextEditor = editorAt('/work/A/SampleViewModel.cs', 'SampleViewModel');
    Object.defineProperty(state, 'quickPickResult', {
      configurable: true,
      get: () => (state.quickPickItems as unknown[] | undefined)?.[1],
    });

    await run('csharpXamlGraph.showForSelection');

    expect(state.quickPickCalls).toHaveLength(1);
    expect(state.quickPickCalls[0].options).toMatchObject({ title: 'Show dependency graph' });
    expect(state.panels).toHaveLength(1);
    delete (state as { quickPickResult?: unknown }).quickPickResult;
  });

  it('does nothing when the picker is dismissed', async () => {
    workspaceWith({
      'A/SampleViewModel.cs': 'namespace N { public class SampleViewModel { } }',
      'A/Deeper.cs': 'namespace M { public class SampleViewModel { } }',
    });
    activate(makeContext(extensionRoot) as never);
    state.activeTextEditor = editorAt('/work/A/SampleViewModel.cs', 'SampleViewModel');
    state.quickPickResult = undefined;

    await run('csharpXamlGraph.showForSelection');
    expect(state.panels).toHaveLength(0);
  });
});

describe('showForFile', () => {
  it('warns when no file is named and no editor is open', async () => {
    activate(makeContext(extensionRoot) as never);
    await run('csharpXamlGraph.showForFile');
    expect(lastOf(state.messages, 'warning')).toMatchObject({ message: 'Select a C# or XAML file first.' });
  });

  it('uses the Explorer uri it was given', async () => {
    workspaceWith({ 'A/Thing.cs': 'namespace N { public class Thing { } }' });
    activate(makeContext(extensionRoot) as never);
    await run('csharpXamlGraph.showForFile', Uri.file('/work/A/Thing.cs'));
    expect(state.panels).toHaveLength(1);
  });

  it('falls back to the active editor’s file', async () => {
    workspaceWith({ 'A/Thing.cs': 'namespace N { public class Thing { } }' });
    activate(makeContext(extensionRoot) as never);
    state.activeTextEditor = { document: { uri: Uri.file('/work/A/Thing.cs') }, selection: { isEmpty: true, active: new Position(0, 0) } };
    await run('csharpXamlGraph.showForFile');
    expect(state.panels).toHaveLength(1);
  });

  it('warns when the file declares nothing the analysis can place', async () => {
    workspaceWith({ 'A/Thing.cs': 'namespace N { public class Thing { } }' });
    activate(makeContext(extensionRoot) as never);
    await run('csharpXamlGraph.showForFile', Uri.file('/work/A/Unknown.cs'));
    expect(lastOf(state.messages, 'warning')).toMatchObject({
      message: 'That file declares nothing the analysis can place on the graph.',
    });
  });

  it('ignores an argument that is not a Uri', async () => {
    activate(makeContext(extensionRoot) as never);
    state.activeTextEditor = undefined;
    await run('csharpXamlGraph.showForFile', 'not-a-uri');
    expect(lastOf(state.messages, 'warning')).toMatchObject({ message: 'Select a C# or XAML file first.' });
  });
});

describe('export, settings, menu and the debug report', () => {
  it('export opens the panel and asks it for the visible graph once the page is listening', async () => {
    workspaceWith({ 'A/Thing.cs': 'namespace N { public class Thing { } }' });
    activate(makeContext(extensionRoot) as never);
    await run('csharpXamlGraph.export');

    expect(state.panels).toHaveLength(1);
    // The page was not ready yet, so the request is held and sent when it announces itself.
    expect(state.panels[0].posted.some((message) =>
      (message as { type: string }).type === 'requestExport')).toBe(false);

    await state.panels[0].send({ type: 'ready' });
    expect(state.panels[0].posted.some((message) =>
      (message as { type: string }).type === 'requestExport')).toBe(true);
  });

  it('openSettings and openMenu delegate to the workbench commands', async () => {
    activate(makeContext(extensionRoot) as never);
    await run('csharpXamlGraph.openSettings');
    await run('csharpXamlGraph.openMenu');

    expect(state.executedCommands.map((entry) => entry.command)).toEqual([
      'workbench.action.openSettings',
      'workbench.view.extension.csharpXamlGraphPanel',
    ]);
    expect(state.executedCommands[0].args).toEqual(['@ext:mtm-local.csharp-xaml-graph']);
  });

  it('opens a debug report wired to the cache, the configuration and the graph panel', async () => {
    workspaceWith({ 'A/Thing.cs': 'namespace N { public class Thing { } }' });
    state.packageJSON = { version: '0.2.1', contributes: { commands: CONTRIBUTED.map((command) => ({ command })) } };
    activate(makeContext(extensionRoot) as never);
    await run('csharpXamlGraph.show');
    await flush();

    await run('csharpXamlGraph.debug');
    await flush();

    const debugPanel = state.panels.at(-1)!;
    expect(debugPanel.viewType).toBe('csharpXamlGraphDebug');
    expect(debugPanel.webview.html).toContain('data-section="analysis"');
    expect(debugPanel.webview.html).toContain('1 type');
  });

  it('re-analyses when the debug report asks it to', async () => {
    workspaceWith({ 'A/Thing.cs': 'namespace N { public class Thing { } }' });
    activate(makeContext(extensionRoot) as never);
    await run('csharpXamlGraph.show');
    await run('csharpXamlGraph.debug');
    await flush();

    state.findFilesCalls.length = 0;
    await state.panels.at(-1)!.send({ type: 'debugReanalyze' });
    await flush();
    expect(state.findFilesCalls).toHaveLength(1);
  });
});

describe('the files the extension loads', () => {
  it('the grammar asset it loads really exists in this checkout', () => {
    expect(fs.existsSync(`${extensionRoot}/media/tree-sitter-c_sharp.wasm`)).toBe(true);
  });
});

describe('activate: the debug report before any analysis', () => {
  it('reports a cache age of nothing when no model has been built', async () => {
    state.packageJSON = { version: '0.2.1' };
    activate(makeContext(extensionRoot) as never);
    await run('csharpXamlGraph.debug');
    await flush();

    const debugPanel = state.panels.at(-1)!;
    expect(debugPanel.webview.html).toContain('not analysed yet');
    expect(debugPanel.webview.html).toContain('empty');
  });
});

describe('show: a single exclude pattern', () => {
  it('passes a single pattern through instead of a brace list', async () => {
    workspaceWith({ 'A/Thing.cs': 'namespace N { public class Thing { } }' });
    state.configValues['csharpXamlGraph.exclude'] = ['**/bin/**'];
    activate(makeContext(extensionRoot) as never);
    await run('csharpXamlGraph.show');
    expect(state.findFilesCalls[0].exclude).toBe('**/bin/**');
  });
});


/* ---------------------------------------------------------------- the profile */

const PROFILE = 'csharp-xaml-graph.profile.json';

/** The model the panel was last handed. */
function lastModel(): { nodes: { id: string; layer: string }[]; warnings: string[] } {
  const message = state.panels
    .flatMap((panel) => panel.posted)
    .filter((entry) => (entry as { type?: string }).type === 'model')
    .at(-1) as { model: { nodes: { id: string; layer: string }[]; warnings: string[] } };
  return message.model;
}

/** The classification the analysis gave a type, by its simple name. */
function layerOf(label: string): string | undefined {
  return lastModel().nodes.find((node) => node.id.endsWith(`.${label}`) || node.id === label)?.layer;
}

describe('the repository profile', () => {
  it('reads the conventions the folder declares, so its own naming rules decide the layer', async () => {
    workspaceWith({
      [PROFILE]: JSON.stringify({
        version: 1,
        layers: { rules: [{ layer: 'service', nameSuffixes: ['Handler'] }], default: 'model' },
      }),
      'A/DunnageHandler.cs': 'namespace N { public class DunnageHandler { } }',
    });
    activate(makeContext(extensionRoot) as never);
    await run('csharpXamlGraph.show');

    expect(layerOf('DunnageHandler')).toBe('service');
  });

  it('leaves the built-in conventions in place when there is no profile', async () => {
    workspaceWith({ 'A/DunnageHandler.cs': 'namespace N { public class DunnageHandler { } }' });
    activate(makeContext(extensionRoot) as never);
    await run('csharpXamlGraph.show');

    expect(layerOf('DunnageHandler')).toBe('model');
    expect(lastModel().warnings).toEqual([]);
  });

  it('still produces a model from a malformed profile, naming the part it could not read', async () => {
    workspaceWith({
      [PROFILE]: '{ "version": 1, "layers": "not a section" }',
      'A/Thing.cs': 'namespace N { public class Thing { } }',
    });
    activate(makeContext(extensionRoot) as never);
    await run('csharpXamlGraph.show');

    const model = lastModel();
    expect(model.nodes.length).toBeGreaterThan(0);
    expect(model.warnings.some((warning) => warning.includes(PROFILE) && warning.includes('layers'))).toBe(true);
  });

  it('names a profile that cannot be parsed as JSON and still draws the graph', async () => {
    workspaceWith({
      [PROFILE]: '{ not json',
      'A/Thing.cs': 'namespace N { public class Thing { } }',
    });
    activate(makeContext(extensionRoot) as never);
    await run('csharpXamlGraph.show');

    expect(layerOf('Thing')).toBe('model');
    expect(lastModel().warnings.some((warning) => warning.includes('could not be read as JSON'))).toBe(true);
  });

  it('reads the files the profile selects when nothing is configured', async () => {
    workspaceWith({
      [PROFILE]: JSON.stringify({ version: 1, files: { include: ['src/**/*.cs'], exclude: [] } }),
      'src/A/Thing.cs': 'namespace N { public class Thing { } }',
    });
    activate(makeContext(extensionRoot) as never);
    await run('csharpXamlGraph.show');

    expect((state.findFilesCalls[0].include as { pattern: string }).pattern).toBe('src/**/*.cs');
    expect(state.findFilesCalls[0].exclude).toBeUndefined();
  });

  it('lets an explicitly configured include and exclude win over the profile', async () => {
    workspaceWith({
      [PROFILE]: JSON.stringify({ version: 1, files: { include: ['src/**/*.cs'], exclude: ['**/gen/**'] } }),
      'A/Thing.cs': 'namespace N { public class Thing { } }',
    });
    state.configValues['csharpXamlGraph.include'] = ['**/*.cs', '**/*.xaml'];
    state.configValues['csharpXamlGraph.exclude'] = ['**/bin/**'];
    activate(makeContext(extensionRoot) as never);
    await run('csharpXamlGraph.show');

    expect((state.findFilesCalls[0].include as { pattern: string }).pattern).toBe('{**/*.cs,**/*.xaml}');
    expect(state.findFilesCalls[0].exclude).toBe('**/bin/**');
  });

  it('says a file selection that matched nothing produced no files', async () => {
    workspaceWith({ 'A/Thing.cs': 'namespace N { public class Thing { } }' });
    state.findFilesResult = [];
    activate(makeContext(extensionRoot) as never);
    await run('csharpXamlGraph.show');

    expect(lastModel().warnings.some((warning) => warning.includes('No file matched'))).toBe(true);
  });

  it('watches the profile path so a change made outside the import re-reads in this session', () => {
    workspaceWith({ 'A/Thing.cs': 'namespace N { public class Thing { } }' });
    activate(makeContext(extensionRoot) as never);

    expect(state.watchers).toHaveLength(1);
    expect(state.watchers[0].pattern).toBe(PROFILE);
  });
});


describe('creating and importing a profile', () => {
  const profileInFolder = (text: string) => {
    state.fileContents[`/work/${PROFILE}`] = text;
  };

  it('opens chat with the setup prompt when the host has the chat command', async () => {
    workspaceWith({ 'A/Thing.cs': 'namespace N { public class Thing { } }' });
    state.availableCommands = ['workbench.action.chat.open'];
    activate(makeContext(extensionRoot) as never);
    await run('csharpXamlGraph.profile.initialize');

    const chat = state.executedCommands.find((entry) => entry.command === 'workbench.action.chat.open');
    expect(chat).toBeDefined();
    const request = chat!.args[0] as { query: string; isPartialQuery: boolean };
    expect(request.isPartialQuery).toBe(true);
    expect(request.query).toContain('Questions about "work"');
    expect(request.query).toContain('What marks a service');
    // Every question comes before the shape of the reply.
    expect(request.query.indexOf('What marks a service')).toBeLessThan(request.query.indexOf('"version": 1'));
  });

  it('puts the prompt on the clipboard when the chat command is absent', async () => {
    workspaceWith({ 'A/Thing.cs': 'namespace N { public class Thing { } }' });
    state.availableCommands = [];
    activate(makeContext(extensionRoot) as never);
    await run('csharpXamlGraph.profile.initialize');

    expect(state.clipboard.at(-1)).toContain('What marks a service');
    const notice = state.messages.filter((message) => message.level === 'information').at(-1);
    expect(notice?.message).toContain('on your clipboard');
  });

  it('falls back to the clipboard when the host rejects the chat command', async () => {
    workspaceWith({ 'A/Thing.cs': 'namespace N { public class Thing { } }' });
    state.availableCommands = ['workbench.action.chat.open'];
    state.commandHandlers['workbench.action.chat.open'] = () => {
      throw new Error('command unavailable');
    };
    activate(makeContext(extensionRoot) as never);
    await run('csharpXamlGraph.profile.initialize');

    expect(state.clipboard.at(-1)).toContain('What marks a service');
  });

  it('says there is nothing to profile when the folder has no C# or XAML', async () => {
    workspaceWith({});
    state.findFilesResult = [];
    activate(makeContext(extensionRoot) as never);
    await run('csharpXamlGraph.profile.initialize');

    expect(state.clipboard).toHaveLength(0);
    expect(state.messages.some((message) => message.message.includes('no C# or XAML files in scope'))).toBe(true);
  });

  it('refuses a reply it cannot use and writes nothing', async () => {
    workspaceWith({ 'A/Thing.cs': 'namespace N { public class Thing { } }' });
    activate(makeContext(extensionRoot) as never);
    state.clipboard.push('{ "repository": "sample" }');
    await run('csharpXamlGraph.profile.import');

    expect(state.writtenFiles).toHaveLength(0);
    const warning = state.messages.filter((message) => message.level === 'warning').at(-1);
    expect(warning?.message).toContain('Nothing was written');
    expect(warning?.message).toContain('version');
  });

  it('writes the merged profile from the first fenced block and re-reads', async () => {
    workspaceWith({ 'A/Thing.cs': 'namespace N { public class Thing { } }' });
    profileInFolder(JSON.stringify({
      version: 1,
      findings: { entryPoints: ['App'] },
    }));
    activate(makeContext(extensionRoot) as never);
    await run('csharpXamlGraph.show');
    expect(state.findFilesCalls).toHaveLength(1);

    state.clipboard.push('```json\n'
      + JSON.stringify({ version: 1, findings: { entryPoints: ['App', 'Boot'] } })
      + '\n```\nThat is the profile.');
    await run('csharpXamlGraph.profile.import');
    await flush();

    const written = state.writtenFiles.at(-1);
    expect(written?.path).toBe(`/work/${PROFILE}`);
    const written_profile = JSON.parse(written!.text) as { version: number; findings: { entryPoints: string[] } };
    expect(written_profile.version).toBe(1);
    // Everything the file already declared survives, and the addition lands once.
    expect(written_profile.findings.entryPoints).toEqual(['App', 'Boot']);
    expect(written!.text.endsWith('\n')).toBe(true);

    expect(state.shownDocuments.at(-1)?.path).toBe(`/work/${PROFILE}`);
    expect(state.findFilesCalls.length).toBeGreaterThan(1);
  });

  it('re-reads in the same session when the profile changes on disk', async () => {
    workspaceWith({ 'A/Thing.cs': 'namespace N { public class Thing { } }' });
    activate(makeContext(extensionRoot) as never);
    await run('csharpXamlGraph.show');
    expect(state.findFilesCalls).toHaveLength(1);

    const { fireWatcherChange } = await import('./support/vscode');
    fireWatcherChange();
    await flush();

    expect(state.findFilesCalls.length).toBeGreaterThan(1);
  });

  it('offers to create a profile once, names the action, and is dismissible', async () => {
    workspaceWith({ 'A/Thing.cs': 'namespace N { public class Thing { } }' });
    activate(makeContext(extensionRoot) as never);
    await run('csharpXamlGraph.show');
    await flush();

    const offers = state.messages.filter((message) => message.message.includes('has no conventions profile yet'));
    expect(offers).toHaveLength(1);
    expect(offers[0].actions).toEqual(['Set up profile', 'Not now']);

    // Dismissed: the offer is not raised again in this session.
    await run('csharpXamlGraph.refresh');
    await flush();
    expect(state.messages.filter((message) => message.message.includes('has no conventions profile yet')))
      .toHaveLength(1);
  });

  it('does not offer a profile the folder already has, and runs setup when the offer is accepted', async () => {
    workspaceWith({ 'A/Thing.cs': 'namespace N { public class Thing { } }' });
    profileInFolder(JSON.stringify({ version: 1 }));
    activate(makeContext(extensionRoot) as never);
    await run('csharpXamlGraph.show');
    await flush();
    expect(state.messages.some((message) => message.message.includes('has no conventions profile yet'))).toBe(false);
  });

  it('runs the setup when the offer is accepted', async () => {
    workspaceWith({ 'A/Thing.cs': 'namespace N { public class Thing { } }' });
    state.availableCommands = [];
    state.messageResults.push('Set up profile');
    activate(makeContext(extensionRoot) as never);
    await run('csharpXamlGraph.show');
    await flush();

    expect(state.clipboard.at(-1)).toContain('What marks a service');
  });
});

describe('importing a reply that records nothing', () => {
  const ready = () => {
    workspaceWith({ 'A/Thing.cs': 'namespace N { public class Thing { } }' });
    activate(makeContext(extensionRoot) as never);
  };

  it('refuses a reply whose only section could not be read, naming the part', async () => {
    ready();
    state.clipboard.push('{ "version": 1, "layers": "not a section" }');
    await run('csharpXamlGraph.profile.import');

    expect(state.writtenFiles).toHaveLength(0);
    const warning = state.messages.filter((message) => message.level === 'warning').at(-1);
    expect(warning?.message).toContain('Nothing was written');
    expect(warning?.message).toContain('layers');
  });

  it('refuses a reply that states only a version, saying it would record nothing', async () => {
    ready();
    state.clipboard.push('{ "version": 1 }');
    await run('csharpXamlGraph.profile.import');

    expect(state.writtenFiles).toHaveLength(0);
    const warning = state.messages.filter((message) => message.level === 'warning').at(-1);
    expect(warning?.message).toContain('would record nothing');
  });

  it('says what an existing file could not carry over when a good reply replaces it', async () => {
    workspaceWith({ 'A/Thing.cs': 'namespace N { public class Thing { } }' });
    state.fileContents[`/work/${PROFILE}`] = '{ "version": 1, "links": "not a section" }';
    activate(makeContext(extensionRoot) as never);

    state.clipboard.push(JSON.stringify({ version: 1, findings: { entryPoints: ['Boot'] } }));
    await run('csharpXamlGraph.profile.import');
    await flush();

    const written = state.writtenFiles.at(-1);
    expect(written?.path).toBe(`/work/${PROFILE}`);
    const notice = state.messages.filter((message) => message.level === 'information').at(-1);
    expect(notice?.message).toContain('could not be read');
    expect(notice?.message).toContain('not in the merged file');
  });
});

describe('the fixes the review found', () => {
  it('re-reads when a profile appears where there was none, and when one goes away', async () => {
    workspaceWith({ 'A/Thing.cs': 'namespace N { public class Thing { } }' });
    activate(makeContext(extensionRoot) as never);
    await run('csharpXamlGraph.show');
    expect(state.findFilesCalls).toHaveLength(1);

    const { fireWatcherCreate, fireWatcherDelete } = await import('./support/vscode');
    fireWatcherCreate();
    await flush();
    expect(state.findFilesCalls.length).toBeGreaterThan(1);

    const afterCreate = state.findFilesCalls.length;
    fireWatcherDelete();
    await flush();
    expect(state.findFilesCalls.length).toBeGreaterThan(afterCreate);
  });

  it('refuses a reply whose only rule was unusable, rather than writing an empty profile', async () => {
    workspaceWith({ 'A/Thing.cs': 'namespace N { public class Thing { } }' });
    activate(makeContext(extensionRoot) as never);
    // The rule forgot `layer`, so it is dropped and the section parses to an empty list.
    state.clipboard.push('{ "version": 1, "layers": { "rules": [{ "nameSuffixes": ["Handler"] }] } }');
    await run('csharpXamlGraph.profile.import');

    expect(state.writtenFiles).toHaveLength(0);
    const warning = state.messages.filter((message) => message.level === 'warning').at(-1);
    expect(warning?.message).toContain('Nothing was written');
    expect(warning?.message).toContain('states the layer');
  });

  it('still writes a reply that adds to a file which already declares something', async () => {
    workspaceWith({ 'A/Thing.cs': 'namespace N { public class Thing { } }' });
    state.fileContents[`/work/${PROFILE}`] = JSON.stringify({ version: 1, findings: { entryPoints: ['App'] } });
    activate(makeContext(extensionRoot) as never);
    // A reply that records nothing on its own is a harmless no-op against a file that says something.
    state.clipboard.push('{ "version": 1, "layers": { "rules": [] } }');
    await run('csharpXamlGraph.profile.import');
    await flush();

    expect(state.writtenFiles.at(-1)?.path).toBe(`/work/${PROFILE}`);
    expect(JSON.parse(state.writtenFiles.at(-1)!.text).findings.entryPoints).toEqual(['App']);
  });
});

describe('the compatibility gate and its offer', () => {
  const realFolders: string[] = [];

  function realFolder(entries: Record<string, string>, name: string): string {
    const root = mkdtempSync(join(tmpdir(), 'gate-'));
    realFolders.push(root);
    const normalized = root.replace(/\\/g, '/');
    state.folders = [{ uri: Uri.file(normalized), name, index: 0 }];
    const uris: Uri[] = [];
    for (const [relative, text] of Object.entries(entries)) {
      const absolute = join(root, relative);
      mkdirSync(dirname(absolute), { recursive: true });
      writeFileSync(absolute, text, 'utf8');
      state.fileContents[`${normalized}/${relative}`] = text;
      uris.push(Uri.file(`${normalized}/${relative}`));
    }
    state.findFilesResult = uris;
    return root;
  }

  /** A folder the analysis cannot read, with a profile present so the once-per-session offer is quiet. */
  const incompatibleFolder = (name = 'other-repo') => realFolder({
    'src/Other.fsproj': '<Project />',
    'csharp-xaml-graph.profile.json': '{ "version": 1 }',
  }, name);

  const offers = () => state.messages.filter((message) => message.actions.includes('Start a new specification'));

  afterEach(() => {
    for (const root of realFolders.splice(0)) {
      rmSync(root, { recursive: true, force: true });
    }
  });

  it('names each structure it cannot handle and builds no prompt', async () => {
    incompatibleFolder();
    state.availableCommands = ['workbench.action.chat.open'];
    activate(makeContext(extensionRoot) as never);
    await run('csharpXamlGraph.profile.initialize');

    const offer = offers().at(-1);
    expect(offer?.message).toContain('no C# files');
    expect(offer?.message).toContain('src/Other.fsproj');
    expect(offer?.actions).toEqual(['Start a new specification', 'Not now']);
    // No convention question was asked, and no prompt reached the chat or the clipboard.
    expect(state.executedCommands.some((entry) => entry.command === 'workbench.action.chat.open')).toBe(false);
    expect(state.clipboard).toHaveLength(0);
  });

  it('creates nothing before the offer is answered', async () => {
    const root = incompatibleFolder();
    const context = makeContext(root) as never;
    activate(context);
    await run('csharpXamlGraph.profile.initialize');

    expect(existsSync(join(root, 'update_prompts'))).toBe(false);
    // The requesting folder is unchanged too.
    expect(readdirSync(root).sort()).toEqual(['csharp-xaml-graph.profile.json', 'src']);
  });

  it('writes the request on yes, and it states the command verbatim', async () => {
    const root = incompatibleFolder();
    state.messageResults.push('Start a new specification');
    activate(makeContext(root) as never);
    await run('csharpXamlGraph.profile.initialize');

    const request = join(root, 'update_prompts', 'other-repo', 'request.md');
    expect(existsSync(request)).toBe(true);
    const text = readFileSync(request, 'utf8');
    expect(text).toContain('/speckit-companion-specify');
    expect(text.split('\n')[0]).toBe('# Extension change request: other-repo');
    expect(state.messages.some((message) => message.message.includes('update_prompts/other-repo/request.md')))
      .toBe(true);
  });

  it('writes nothing on no, and the reason is still on the model afterwards', async () => {
    const root = incompatibleFolder();
    state.messageResults.push('Not now');
    activate(makeContext(root) as never);
    await run('csharpXamlGraph.profile.initialize');

    expect(existsSync(join(root, 'update_prompts'))).toBe(false);

    // The dialog is gone; the reason is not.
    state.availableCommands = [];
    await run('csharpXamlGraph.show');
    const posted = state.panels[0].posted.filter(
      (message) => (message as { type: string }).type === 'model',
    );
    const model = (posted.at(-1) as { model: { warnings: string[] } }).model;
    expect(model.warnings.filter((warning) => warning.startsWith('not analysed'))).toHaveLength(3);
    expect(model.warnings.join(' ')).toContain('src/Other.fsproj');
  });

  it('treats a dismissed dialog as no', async () => {
    const root = incompatibleFolder();
    // No reply queued: the notification hands back undefined, which is what a dismissal is.
    activate(makeContext(root) as never);
    await run('csharpXamlGraph.profile.initialize');

    expect(state.clipboard).toHaveLength(0);
    expect(existsSync(join(root, 'update_prompts'))).toBe(false);
  });

  it('asks once per folder per session', async () => {
    incompatibleFolder();
    state.messageResults.push('Not now', 'Not now');
    activate(makeContext(extensionRoot) as never);
    await run('csharpXamlGraph.profile.initialize');
    await run('csharpXamlGraph.profile.initialize');

    expect(offers()).toHaveLength(1);
  });

  it('asks nothing of a compatible folder and proceeds as before', async () => {
    realFolder({
      'A/Thing.cs': 'namespace N { public class Thing { } }',
      'A/Thing.xaml': '<Page x:Class="N.Thing" />',
      'A/App.csproj': '<Project />',
      'A/App.sln': '',
      'csharp-xaml-graph.profile.json': '{ "version": 1 }',
    }, 'compatible');
    state.availableCommands = [];
    activate(makeContext(extensionRoot) as never);
    await run('csharpXamlGraph.profile.initialize');

    expect(offers()).toHaveLength(0);
    expect(state.clipboard.at(-1)).toContain('What marks a service');
  });

  it('proceeds with one note when the structure could not be read at all', async () => {
    // `/work` does not exist on disk, so the walk cannot list it: the third answer, not a refusal.
    workspaceWith({ 'A/Thing.cs': 'namespace N { public class Thing { } }' });
    state.availableCommands = [];
    activate(makeContext(extensionRoot) as never);
    await run('csharpXamlGraph.profile.initialize');

    expect(offers()).toHaveLength(0);
    expect(state.clipboard.at(-1)).toContain('What marks a service');

    await run('csharpXamlGraph.show');
    const posted = state.panels[0].posted.filter(
      (message) => (message as { type: string }).type === 'model',
    );
    const model = (posted.at(-1) as { model: { warnings: string[] } }).model;
    expect(model.warnings.filter((warning) => warning.startsWith('not analysed'))).toHaveLength(1);
  });

  it('falls back to the clipboard, naming the relative path, when the extension root is read-only', async () => {
    incompatibleFolder();
    const root = mkdtempSync(join(tmpdir(), 'readonly-'));
    realFolders.push(root);
    // A file where the request folder has to go, so writing it must fail.
    writeFileSync(join(root, 'update_prompts'), 'not a directory');
    state.messageResults.push('Start a new specification');
    activate(makeContext(root) as never);
    await run('csharpXamlGraph.profile.initialize');

    expect(state.clipboard.at(-1)).toContain('/speckit-companion-specify');
    expect(state.messages.some((message) => message.message.includes('update_prompts/other-repo/request.md')))
      .toBe(true);
  });

  it('gates the refresh command the same way', async () => {
    incompatibleFolder();
    state.messageResults.push('Not now');
    activate(makeContext(extensionRoot) as never);
    await run('csharpXamlGraph.profile.refresh');

    expect(offers()).toHaveLength(1);
    expect(state.clipboard).toHaveLength(0);
  });
});