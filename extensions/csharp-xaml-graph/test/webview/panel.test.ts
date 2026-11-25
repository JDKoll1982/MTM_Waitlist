import { beforeEach, describe, expect, it, vi } from 'vitest';
import { GraphPanel, readGraphSettings } from '../../src/webview/panel';
import type { GraphModel } from '../../src/graph/model';
import { graphEdge, graphModel, graphNode } from '../support/model';
import { makeContext, resetVscodeState, state, Uri } from '../support/vscode';

const model: GraphModel = graphModel({
  nodes: [graphNode({ id: 'N.A' }), graphNode({ id: 'N.B' })],
  edges: [graphEdge({ id: 'e1', source: 'N.A', target: 'N.B', kind: 'binds' })],
});

const loaderFor = (loaded: GraphModel = model, error?: Error) =>
  vi.fn(async () => {
    if (error) {
      throw error;
    }
    return loaded;
  });

let loader: ReturnType<typeof loaderFor>;

function open() {
  loader = loaderFor();
  return GraphPanel.show(makeContext('/ext') as never, loader as never);
}

beforeEach(() => {
  // The panel is a singleton: dispose whatever the previous test opened so its `onDidDispose`
  // clears the static handle before the next test starts.
  for (const panel of [...state.panels]) {
    panel.dispose();
  }
  resetVscodeState();
  state.folders = [{ uri: Uri.file('/work'), name: 'work', index: 0 }];
});

describe('readGraphSettings', () => {
  it('defaults the unpacked layout to Layered Down', () => {
    expect(readGraphSettings()).toEqual({ unpackedLayout: 'layered-tb' });
  });

  it('normalises an unknown configured layout', () => {
    state.configValues['csharpXamlGraph.unpackedLayout'] = 'nonsense';
    expect(readGraphSettings().unpackedLayout).toBe('layered-tb');
  });

  it('passes through a configured layout', () => {
    state.configValues['csharpXamlGraph.unpackedLayout'] = 'circle';
    expect(readGraphSettings().unpackedLayout).toBe('circle');
  });
});

describe('GraphPanel.show', () => {
  it('creates one script-enabled panel rooted at the extension output', () => {
    const panel = open();
    const created = state.panels[0];
    expect(created.viewType).toBe('csharpXamlGraph');
    expect(created.title).toBe('C#/XAML Dependency Graph');
    expect(created.options).toMatchObject({ enableScripts: true, retainContextWhenHidden: true });
    expect(created.webview.html).toContain('<div id="cy"></div>');
    expect(GraphPanel.instance()).toBe(panel);
  });

  it('reuses and reveals the open panel rather than creating a second one', () => {
    const first = open();
    const second = GraphPanel.show(makeContext('/ext') as never, loader as never);
    expect(second).toBe(first);
    expect(state.panels).toHaveLength(1);
    expect(state.panels[0].revealed).toBe(1);
  });

  it('forgets the panel when it is disposed', () => {
    const panel = open();
    state.panels[0].dispose();
    expect(GraphPanel.instance()).toBeUndefined();
    expect(panel).toBeDefined();
  });
});

describe('GraphPanel: ready and analysis', () => {
  it('pushes the settings and then analyses when no model is loaded', async () => {
    const panel = open();
    await state.panels[0].send({ type: 'ready' });

    expect(state.panels[0].posted[0]).toMatchObject({ type: 'settings' });
    expect(loader).toHaveBeenCalledWith(false);
    const types = state.panels[0].posted.map((message) => (message as { type: string }).type);
    expect(types).toEqual(['settings', 'progress', 'model', 'progress']);
    expect(panel).toBeDefined();
  });

  it('starts the analysis with an indeterminate progress message', async () => {
    open();
    await state.panels[0].send({ type: 'ready' });
    expect(state.panels[0].posted[1]).toEqual({
      type: 'progress', message: 'Starting analysis', percent: 0, known: false,
    });
  });

  it('ends the progress with a done flag even when the analysis failed', async () => {
    loader = loaderFor(model, new Error('boom'));
    GraphPanel.show(makeContext('/ext') as never, loader as never);
    await state.panels[0].send({ type: 'ready' });

    const posted = state.panels[0].posted.map((message) => message as Record<string, unknown>);
    expect(posted.some((message) => message.type === 'progress' && message.done === true)).toBe(true);
    expect(posted.at(-1)).toEqual({ type: 'error', message: 'boom' });
    expect(state.messages.some((message) => message.level === 'error' && message.message.includes('boom'))).toBe(true);
  });

  it('reports a non-Error failure as text', async () => {
    loader = vi.fn(async () => {
      throw 'plain string';
    }) as never;
    GraphPanel.show(makeContext('/ext') as never, loader as never);
    await state.panels[0].send({ type: 'ready' });
    expect(state.panels[0].posted.at(-1)).toEqual({ type: 'error', message: 'plain string' });
  });

  it('sends the cached model again when the page reloads', async () => {
    open();
    await state.panels[0].send({ type: 'ready' });
    const before = state.panels[0].posted.length;
    await state.panels[0].send({ type: 'ready' });

    expect(loader).toHaveBeenCalledTimes(1);
    const after = state.panels[0].posted.slice(before).filter(
      (message) => (message as { type: string }).type === 'model');
    expect(after).toHaveLength(1);
  });

  it('re-reads on reanalyze', async () => {
    open();
    await state.panels[0].send({ type: 'ready' });
    await state.panels[0].send({ type: 'reanalyze' });
    expect(loader).toHaveBeenLastCalledWith(true);
  });

  it('says an analysis is already running rather than dropping the request silently', async () => {
    let release: (value: GraphModel) => void = () => undefined;
    const slow = vi.fn(() => new Promise<GraphModel>((resolve) => { release = resolve; }));
    GraphPanel.show(makeContext('/ext') as never, slow as never);

    const first = state.panels[0].send({ type: 'ready' });
    // Let the ready handler reach its first analysis before asking for a second one.
    await new Promise((resolve) => setTimeout(resolve, 0));
    await state.panels[0].send({ type: 'reanalyze' });
    expect(state.messages.some((message) => message.message === 'C#/XAML Graph: analysis already running')).toBe(true);
    expect(slow).toHaveBeenCalledTimes(1);

    release(model);
    await first;
  });
});

describe('GraphPanel: export', () => {
  it('writes the visible payload the page handed back', async () => {
    const panel = open();
    await state.panels[0].send({ type: 'ready' });
    state.saveTarget = Uri.file('/work/dependency-graph-view.json');

    const payload = { nodes: [{ id: 'N.A' }], stats: { visibleNodes: 1, visibleEdges: 0 } };
    await state.panels[0].send({ type: 'export', payload });

    expect(state.saveDialogOptions).toMatchObject({ saveLabel: 'Export visible graph' });
    expect(state.writtenFiles[0].path).toBe('/work/dependency-graph-view.json');
    expect(JSON.parse(state.writtenFiles[0].text)).toEqual(payload);
    expect(state.messages.at(-1)?.message).toBe(
      'Visible graph exported to /work/dependency-graph-view.json (1 nodes, 0 edges)',
    );
    expect(panel).toBeDefined();
  });

  it('writes the whole model when no payload arrives, naming the file for that', async () => {
    open();
    await state.panels[0].send({ type: 'ready' });
    state.saveTarget = Uri.file('/work/dependency-graph.json');
    await state.panels[0].send({ type: 'export' });

    expect(state.saveDialogOptions).toMatchObject({ saveLabel: 'Export graph' });
    expect(JSON.parse(state.writtenFiles[0].text)).toMatchObject({ nodes: expect.any(Array) });
    expect(state.messages.at(-1)?.message).toBe('Visible graph exported to /work/dependency-graph.json');
  });

  it('warns when there is nothing to export yet', async () => {
    open();
    await state.panels[0].send({ type: 'export' });
    expect(state.messages.at(-1)).toMatchObject({ level: 'warning', message: 'Nothing to export yet.' });
  });

  it('does nothing when the save dialog is cancelled', async () => {
    open();
    await state.panels[0].send({ type: 'ready' });
    state.saveTarget = undefined;
    await state.panels[0].send({ type: 'export', payload: { nodes: [] } });
    expect(state.writtenFiles).toEqual([]);
  });

  it('falls back to the extension folder when no workspace is open', async () => {
    state.folders = [];
    open();
    await state.panels[0].send({ type: 'ready' });
    await state.panels[0].send({ type: 'export', payload: { nodes: [] } });
    expect((state.saveDialogOptions as { defaultUri: Uri }).defaultUri.path).toBe('/ext/dependency-graph-view.json');
  });

  it('queues an export asked for before the page is listening', async () => {
    open();
    await GraphPanel.instance()!.requestExport();
    expect(state.panels[0].posted.some((message) => (message as { type: string }).type === 'requestExport')).toBe(false);

    await state.panels[0].send({ type: 'ready' });
    expect(state.panels[0].posted.some((message) => (message as { type: string }).type === 'requestExport')).toBe(true);
  });

  it('asks a ready page straight away', async () => {
    open();
    await state.panels[0].send({ type: 'ready' });
    await GraphPanel.instance()!.requestExport();
    expect(state.panels[0].posted.at(-1)).toEqual({ type: 'requestExport' });
  });
});

describe('GraphPanel: diagnostics and settings', () => {
  it('returns undefined when the page has not said it is ready', async () => {
    open();
    expect(await GraphPanel.instance()!.requestDiagnostics()).toBeUndefined();
  });

  it('resolves the facts the page sends back', async () => {
    open();
    await state.panels[0].send({ type: 'ready' });
    const pending = GraphPanel.instance()!.requestDiagnostics();
    await state.panels[0].send({ type: 'diagnostics', facts: { nodesDrawn: 3 } });
    expect(await pending).toEqual({ nodesDrawn: 3 });
  });

  it('resolves undefined when the page does not answer in time', async () => {
    open();
    await state.panels[0].send({ type: 'ready' });
    expect(await GraphPanel.instance()!.requestDiagnostics(5)).toBeUndefined();
  });

  it('defaults missing facts to an empty object', async () => {
    open();
    await state.panels[0].send({ type: 'ready' });
    const pending = GraphPanel.instance()!.requestDiagnostics();
    await state.panels[0].send({ type: 'diagnostics' });
    expect(await pending).toEqual({});
  });

  it('pushes settings only once the page is listening', async () => {
    open();
    await GraphPanel.instance()!.pushSettings();
    expect(state.panels[0].posted).toEqual([]);

    await state.panels[0].send({ type: 'ready' });
    state.configValues['csharpXamlGraph.unpackedLayout'] = 'grid';
    await GraphPanel.instance()!.pushSettings();
    expect(state.panels[0].posted.at(-1)).toEqual({ type: 'settings', settings: { unpackedLayout: 'grid' } });
  });

  it('mirrors progress only once the page is listening', async () => {
    open();
    GraphPanel.instance()!.showProgress('Reading', 10, true);
    expect(state.panels[0].posted).toEqual([]);

    await state.panels[0].send({ type: 'ready' });
    GraphPanel.instance()!.showProgress('Parsing — 4 of 9', 44, true);
    expect(state.panels[0].posted.at(-1)).toEqual({
      type: 'progress', message: 'Parsing — 4 of 9', percent: 44, known: true,
    });
  });
});

describe('GraphPanel: focus', () => {
  it('re-reads the model when nothing is loaded, then sends the request once the page is ready', async () => {
    open();
    await GraphPanel.instance()!.focus({ title: 'N.A', nodeIds: ['N.A'] });
    expect(loader).toHaveBeenCalledWith(false);
    expect(state.panels[0].posted.some((message) => (message as { type: string }).type === 'request')).toBe(false);

    await state.panels[0].send({ type: 'ready' });
    expect(state.panels[0].posted.at(-1)).toEqual({
      type: 'request',
      request: { title: 'N.A', nodeIds: ['N.A'] },
    });
  });

  it('queues the request until the page is ready', async () => {
    open();
    await state.panels[0].send({ type: 'ready' });
    const focusing = GraphPanel.instance()!.focus({ title: 'N.B', nodeIds: ['N.B'] });
    await focusing;
    expect(state.panels[0].posted.at(-1)).toMatchObject({ type: 'request' });
  });

  it('keeps a request made before the page is ready until it is', async () => {
    open();
    const focusing = GraphPanel.instance()!.focus({ title: 'N.A', nodeIds: ['N.A'] });
    await focusing;
    expect(state.panels[0].posted.some((message) => (message as { type: string }).type === 'request')).toBe(false);

    await state.panels[0].send({ type: 'ready' });
    expect(state.panels[0].posted.some((message) => (message as { type: string }).type === 'request')).toBe(true);
  });
});

describe('GraphPanel: messages from the page', () => {
  it('reveals a file at the requested line', async () => {
    open();
    await state.panels[0].send({ type: 'reveal', file: 'A/B/Thing.cs', line: 12 });

    expect(state.shownDocuments).toHaveLength(1);
    expect(state.shownDocuments[0].path).toBe('/work/A/B/Thing.cs');
    const options = state.shownDocuments[0].options as { selection: { start: { line: number } } };
    expect(options.selection.start.line).toBe(11);
  });

  it('treats a missing line as the first line and a missing file as nothing to do', async () => {
    open();
    await state.panels[0].send({ type: 'reveal', file: 'A.cs' });
    expect((state.shownDocuments[0].options as { selection: { start: { line: number } } }).selection.start.line).toBe(0);

    await state.panels[0].send({ type: 'reveal' });
    expect(state.shownDocuments).toHaveLength(1);
  });

  it('says why a file could not be opened', async () => {
    open();
    state.openDocumentError = new Error('gone');
    await state.panels[0].send({ type: 'reveal', file: 'A.cs', line: 1 });
    expect(state.messages.at(-1)).toMatchObject({ level: 'error', message: 'Could not open A.cs: gone' });
  });

  it('reports a non-Error open failure as text', async () => {
    open();
    state.openDocumentError = 'gone' as unknown as Error;
    await state.panels[0].send({ type: 'reveal', file: 'A.cs' });
    expect(state.messages.at(-1)).toMatchObject({ level: 'error', message: 'Could not open A.cs: gone' });
  });

  it('does nothing to reveal a file when no workspace is open', async () => {
    state.folders = [];
    open();
    await state.panels[0].send({ type: 'reveal', file: 'A.cs' });
    expect(state.shownDocuments).toEqual([]);
  });

  it('copies the text the page sends and ignores an empty copy', async () => {
    open();
    await state.panels[0].send({ type: 'copy', text: 'N.Full.Name' });
    await state.panels[0].send({ type: 'copy' });
    expect(state.clipboard).toEqual(['N.Full.Name']);
  });

  it('shows a notice the page asks for and ignores an empty one', async () => {
    open();
    await state.panels[0].send({ type: 'notice', text: 'nothing to unpack' });
    await state.panels[0].send({ type: 'notice' });
    expect(state.messages).toEqual([
      { level: 'information', message: 'nothing to unpack', actions: [] },
    ]);
  });

  it('ignores a message it does not know', async () => {
    open();
    await state.panels[0].send({ type: 'mystery' });
    await state.panels[0].send(undefined);
    expect(state.panels[0].posted).toEqual([]);
  });
});

describe('GraphPanel: export corner cases', () => {
  it('writes the whole model to the extension folder when no workspace is open', async () => {
    state.folders = [];
    open();
    await state.panels[0].send({ type: 'ready' });
    await state.panels[0].send({ type: 'export' });
    expect((state.saveDialogOptions as { defaultUri: Uri }).defaultUri.path).toBe('/ext/dependency-graph.json');
  });

  it('leaves the summary off when the payload carries no stats', async () => {
    open();
    await state.panels[0].send({ type: 'ready' });
    state.saveTarget = Uri.file('/work/graph.json');
    await state.panels[0].send({ type: 'export', payload: { nodes: [] } });
    expect(state.messages.at(-1)?.message).toBe('Visible graph exported to /work/graph.json');
  });
});
