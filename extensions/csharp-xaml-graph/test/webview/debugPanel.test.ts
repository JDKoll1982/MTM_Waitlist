import { beforeEach, describe, expect, it, vi } from 'vitest';
import { DebugPanel, type DebugPanelOptions } from '../../src/webview/debugPanel';
import { graphModel, graphNode } from '../support/model';
import { makeContext, resetVscodeState, state } from '../support/vscode';

const options = (partial: Partial<DebugPanelOptions> = {}): DebugPanelOptions => ({
  getModel: () => undefined,
  getModelAge: () => undefined,
  isCacheInvalidated: () => false,
  registeredCommands: ['csharpXamlGraph.show'],
  getGraphPanel: () => undefined,
  reanalyze: async () => undefined,
  ...partial,
});

beforeEach(() => {
  for (const panel of [...state.panels]) {
    panel.dispose();
  }
  resetVscodeState();
  state.packageJSON = {
    version: '0.2.1',
    activationEvents: ['onCommand:csharpXamlGraph.show'],
    contributes: { commands: [{ command: 'csharpXamlGraph.show' }], views: { panel: [{ id: 'menu', type: 'webview' }] } },
  };
});

const flush = async () => {
  for (let index = 0; index < 5; index += 1) {
    await new Promise((resolve) => setTimeout(resolve, 0));
  }
};

describe('DebugPanel.show', () => {
  it('opens a diagnostics page beside the editor', async () => {
    DebugPanel.show(makeContext('/ext') as never, options());
    await flush();

    const panel = state.panels[0];
    expect(panel.viewType).toBe('csharpXamlGraphDebug');
    expect(panel.title).toBe('C#/XAML Graph — Diagnostics');
    expect(panel.options).toMatchObject({ enableScripts: true, retainContextWhenHidden: true });
    expect(panel.webview.html).toContain('C#/XAML Graph — Diagnostics');
    expect(panel.webview.html).toContain('data-section="host"');
    expect(panel.webview.html).toContain('data-section="packaging"');
  });

  it('reuses the open page and refreshes it', async () => {
    DebugPanel.show(makeContext('/ext') as never, options());
    await flush();
    const firstHtml = state.panels[0].webview.html;

    state.packageJSON.version = '9.9.9';
    DebugPanel.show(makeContext('/ext') as never, options());
    await flush();

    expect(state.panels).toHaveLength(1);
    expect(state.panels[0].revealed).toBe(1);
    expect(state.panels[0].webview.html).not.toBe(firstHtml);
    expect(state.panels[0].webview.html).toContain('9.9.9');
  });

  it('reports the analysed model and its age', async () => {
    const model = graphModel({ nodes: [graphNode({ id: 'N.A' }), graphNode({ id: 'N.B' })] });
    DebugPanel.show(makeContext('/ext') as never, options({
      getModel: () => model,
      getModelAge: () => 2000,
      isCacheInvalidated: () => true,
    }));
    await flush();

    const html = state.panels[0].webview.html;
    expect(html).toContain('2 types');
    expect(html).toContain('invalidated — next open re-reads');
  });

  it('asks the graph panel for the canvas half when one is open', async () => {
    const requestDiagnostics = vi.fn(async () => ({
      ready: true,
      modelLoadedAt: '2026-01-01T00:00:00.000Z',
      nodesDrawn: 42,
      edgesDrawn: 7,
      hiddenOrphans: 0,
      layout: 'packed',
      density: 'normal',
      labels: 'zoom',
      zoom: 1,
      canvasWidth: 800,
      canvasHeight: 600,
      module: 'all',
      search: '',
      layers: ['view'],
      edgeKinds: ['binds'],
      focus: null,
      focusDepth: 1,
      scope: null,
      scopeExact: false,
      unpacked: [],
      history: [],
    }));
    DebugPanel.show(makeContext('/ext') as never, options({
      getGraphPanel: () => ({ requestDiagnostics } as never),
    }));
    await flush();

    expect(requestDiagnostics).toHaveBeenCalled();
    expect(state.panels[0].webview.html).toContain('42 nodes');
  });

  it('renders without a canvas half when no graph is open', async () => {
    DebugPanel.show(makeContext('/ext') as never, options());
    await flush();
    expect(state.panels[0].webview.html).toContain('not open');
  });

  it('forgets the page when it is disposed', async () => {
    DebugPanel.show(makeContext('/ext') as never, options());
    await flush();
    state.panels[0].dispose();

    DebugPanel.show(makeContext('/ext') as never, options());
    await flush();
    expect(state.panels).toHaveLength(2);
  });
});

describe('DebugPanel messages', () => {
  const openPanel = async (partial: Partial<DebugPanelOptions> = {}) => {
    DebugPanel.show(makeContext('/ext') as never, options(partial));
    await flush();
    return state.panels[0];
  };

  it('re-collects on refresh', async () => {
    const panel = await openPanel();
    const before = panel.webview.html;
    state.packageJSON.version = '3.0.0';
    await panel.send({ type: 'debugRefresh' });
    expect(panel.webview.html).not.toBe(before);
    expect(panel.webview.html).toContain('3.0.0');
  });

  it('re-analyses and then refreshes', async () => {
    const reanalyze = vi.fn(async () => undefined);
    const panel = await openPanel({ reanalyze });
    await panel.send({ type: 'debugReanalyze' });
    expect(reanalyze).toHaveBeenCalledTimes(1);
    expect(panel.webview.html).toContain('data-section="analysis"');
  });

  it('copies one section to the clipboard and confirms it', async () => {
    const panel = await openPanel();
    await panel.send({ type: 'debugCopy', section: 'host' });

    expect(state.clipboard[0]).toContain('## Host — Host and workspace');
    expect(state.clipboard[0]).not.toContain('## Packaging');
    expect(panel.posted.at(-1)).toEqual({ type: 'debugCopied', text: 'Copied “host”' });
  });

  it('copies the whole report when no section is named', async () => {
    const panel = await openPanel();
    await panel.send({ type: 'debugCopy', section: null });

    expect(state.clipboard[0]).toContain('## Host — Host and workspace');
    expect(state.clipboard[0]).toContain('## Packaging — Bundles, assets and the grammar');
    expect(panel.posted.at(-1)).toEqual({ type: 'debugCopied', text: 'Copied the whole report' });
  });

  it('treats a missing section as the whole report', async () => {
    const panel = await openPanel();
    await panel.send({ type: 'debugCopy' });
    expect(state.clipboard[0]).toContain('## Packaging');
  });

  it('opens the extension settings and the panel tab through their commands', async () => {
    const panel = await openPanel();
    await panel.send({ type: 'debugOpenSettings' });
    await panel.send({ type: 'debugOpenMenu' });

    expect(state.executedCommands.map((entry) => entry.command)).toEqual([
      'csharpXamlGraph.openSettings',
      'csharpXamlGraph.openMenu',
    ]);
  });

  it('ignores a message it does not know', async () => {
    const panel = await openPanel();
    await panel.send({ type: 'mystery' });
    await panel.send(undefined);
    expect(panel.posted).toEqual([]);
  });
});
