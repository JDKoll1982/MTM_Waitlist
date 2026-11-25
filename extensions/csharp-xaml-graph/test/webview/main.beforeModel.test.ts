/**
 * The panel before it has ever received a model.
 *
 * This is the one state the main suite cannot reach, because it always loads a model first: the
 * canvas exists, the toolbar works, export has nothing to write and a request has to wait.
 */
import { beforeAll, describe, expect, it } from 'vitest';
import { renderHtml } from '../../src/webview/html';
import { graphEdge, graphModel, graphNode } from '../support/model';
import { Uri } from '../support/vscode';

const posted: Record<string, unknown>[] = [];

const el = <T extends HTMLElement>(id: string): T => document.getElementById(id) as T;
const send = (data: Record<string, unknown>): void => {
  window.dispatchEvent(new MessageEvent('message', { data }));
};
const tick = (ms = 0): Promise<void> => new Promise((resolve) => setTimeout(resolve, ms));

const model = graphModel({
  nodes: [
    graphNode({ id: 'N.A', label: 'A', layer: 'view', module: 'M', location: { file: 'A.cs', line: 1 } }),
    graphNode({ id: 'N.B', label: 'B', layer: 'viewmodel', module: 'M', location: { file: 'B.cs', line: 1 } }),
  ],
  edges: [graphEdge({ id: 'e1', source: 'N.A', target: 'N.B', kind: 'binds' })],
});

beforeAll(async () => {
  const html = renderHtml({
    webview: { cspSource: 'test', asWebviewUri: (uri: Uri) => uri } as never,
    scriptUri: Uri.file('main.js'),
    styleUri: Uri.file('style.css'),
    nonce: 'n',
  });
  document.body.innerHTML = new DOMParser().parseFromString(html, 'text/html').body.innerHTML;
  (window as unknown as { acquireVsCodeApi: () => unknown }).acquireVsCodeApi = () => ({
    postMessage: (message: Record<string, unknown>) => posted.push(message),
    getState: () => undefined,
    setState: () => undefined,
  });
  await import('../../src/webview/client/main');
  await tick(10);
});

describe('every control before a model exists', () => {
  it('tolerates the toolbar, the zoom stack and every message in that state', () => {
    for (const id of [
      'fit', 'repack', 'view-reset', 'zoom-region', 'tab-problems', 'tab-details',
      'legend-open', 'legend-close', 'visibility-open', 'visibility-close',
      'preset-mvvm', 'preset-structure', 'preset-cleanup', 'preset-all',
      'toggle-panel', 'reanalyze', 'export',
    ]) {
      el<HTMLButtonElement>(id).click();
    }

    const change = (id: string, value: string): void => {
      const select = el<HTMLSelectElement>(id);
      select.value = value;
      select.dispatchEvent(new Event('change'));
    };
    change('layout', 'layered-tb');
    change('layout', 'packed');
    change('density', 'roomy');
    change('density', 'normal');
    change('labels', 'always');
    change('labels', 'zoom');
    change('depth', '2');

    const search = el<HTMLInputElement>('search');
    search.value = 'anything';
    search.dispatchEvent(new Event('input'));
    search.value = '';
    search.dispatchEvent(new Event('input'));

    const slider = el<HTMLInputElement>('zoom-slider');
    slider.value = '20';
    slider.dispatchEvent(new Event('input'));

    for (const id of ['hide-orphans', 'only-problems'] as const) {
      const box = el<HTMLInputElement>(id);
      box.checked = !box.checked;
      box.dispatchEvent(new Event('change'));
    }

    document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }));
    send({ type: 'requestDiagnostics' });
    send({ type: 'progress', message: 'Reading', percent: 3, known: true });
    send({ type: 'progress', done: true });
    send({ type: 'settings', settings: { unpackedLayout: 'grid' } });
    send({ type: 'notice', message: 'before the model' });
    send({ type: 'error', message: 'before the model' });

    expect(el('problems').textContent).toContain('Run the analysis to see cleanup findings.');
    expect(posted.some((message) => message.type === 'diagnostics')).toBe(true);
  });

  it('refuses to select a node when there is nothing to select', () => {
    send({ type: 'request', request: { nodeIds: [], focus: 'N.Nothing' } });
    expect(el('focus-bar').hidden).toBe(true);
  });
});

describe('the zoom stack before a model exists', () => {
  it('ignores a dragged region before a model has been drawn', () => {
    const canvas = el<HTMLDivElement>('cy');
    const button = el<HTMLButtonElement>('zoom-region');
    if (button.getAttribute('aria-pressed') !== 'true') {
      button.click();
    }
    canvas.setPointerCapture = () => {};

    const fire = (type: string, x: number, y: number): void => {
      canvas.dispatchEvent(new MouseEvent(type, { bubbles: true, button: 0, clientX: x, clientY: y }));
    };
    fire('pointerdown', 10, 10);
    fire('pointermove', 60, 60);
    fire('pointerup', 60, 60);

    expect(button.getAttribute('aria-pressed')).toBe('false');
    expect(el<HTMLDivElement>('zoom-rect').hidden).toBe(true);
  });
});
describe('before any model arrives', () => {
  it('has asked the host to start the analysis', () => {
    expect(posted[0]).toEqual({ type: 'ready' });
  });

  it('has nothing to export and says nothing when the button is pressed', () => {
    posted.length = 0;
    el<HTMLButtonElement>('export').click();
    send({ type: 'requestExport' });
    expect(posted).toEqual([]);
  });

  it('holds a request until a model exists, then applies it', async () => {
    send({
      type: 'request',
      request: { title: 'A from the editor', nodeIds: ['N.A'], focus: 'N.A', depth: 1 },
    });
    expect(el('focus-bar').hidden).toBe(true);

    const container = el<HTMLDivElement>('cy');
    container.style.padding = '0px';
    Object.defineProperty(container, 'clientWidth', { value: 800, configurable: true });
    Object.defineProperty(container, 'clientHeight', { value: 600, configurable: true });

    send({ type: 'model', model });
    await tick(20);

    expect(el('focus-bar').hidden).toBe(false);
    expect(el('stats').textContent).toContain('of 2 nodes');
  });
});

