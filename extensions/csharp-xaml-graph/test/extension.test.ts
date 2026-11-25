import { beforeEach, describe, expect, it } from 'vitest';
import * as fs from 'node:fs';
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
];

const flush = async () => {
  for (let index = 0; index < 8; index += 1) {
    await new Promise((resolve) => setTimeout(resolve, 0));
  }
};

const run = (command: string, ...args: unknown[]) =>
  (state.commandHandlers[command] as (...rest: unknown[]) => Promise<unknown>)(...args);

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
    expect(reports[0]).toContain('Reading the files');
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
      '{**/bin/**,**/obj/**,**/node_modules/**,**/*.Tests/**,**/.github/**,**/.specify/**}',
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
    expect(state.messages.at(-1)).toMatchObject({
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

    expect(state.messages.at(-1)).toMatchObject({
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
    expect(state.messages.at(-1)).toMatchObject({ message: 'Select a C# or XAML file first.' });
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
    expect(state.messages.at(-1)).toMatchObject({
      message: 'That file declares nothing the analysis can place on the graph.',
    });
  });

  it('ignores an argument that is not a Uri', async () => {
    activate(makeContext(extensionRoot) as never);
    state.activeTextEditor = undefined;
    await run('csharpXamlGraph.showForFile', 'not-a-uri');
    expect(state.messages.at(-1)).toMatchObject({ message: 'Select a C# or XAML file first.' });
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
