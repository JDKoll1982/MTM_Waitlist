/**
 * Framing on a canvas far too small for the graph.
 *
 * The panel never shrinks the graph below the readable zoom: it fits, clamps to 0.34 and pans to the
 * focused shape instead. Reaching that branch needs a viewport small enough that fitting would go
 * under the clamp, so this file owns a 100×100 canvas and never resizes the core — a resize schedules
 * a render that would outlive the core when another file replaces it.
 */
import { beforeAll, describe, expect, it, vi } from 'vitest';
import { renderHtml } from '../../src/webview/html';
import { graphEdge, graphModel, graphNode } from '../support/model';
import { Uri } from '../support/vscode';

const captured = vi.hoisted(() => ({ cores: [] as unknown[] }));

vi.mock('cytoscape', async (importOriginal) => {
  const actual = await importOriginal<typeof import('cytoscape')>();
  const real = actual.default;
  const wrapper = ((options: never) => {
    const core = real(options);
    captured.cores.push(core);
    return core;
  }) as unknown as typeof real;
  Object.assign(wrapper, real);
  return { ...actual, default: wrapper };
});

const el = <T extends HTMLElement>(id: string): T => document.getElementById(id) as T;
const tick = (ms = 0): Promise<void> => new Promise((resolve) => setTimeout(resolve, ms));
const send = (data: Record<string, unknown>): void => {
  window.dispatchEvent(new MessageEvent('message', { data }));
};

const star = graphModel({
  nodes: [
    graphNode({ id: 'N.Hub', label: 'Hub', layer: 'view', module: 'Core' }),
    ...Array.from({ length: 24 }, (_, index) => graphNode({
      id: `N.Leaf${index}`,
      label: `Leaf${index}`,
      layer: 'viewmodel',
      module: 'Core',
    })),
  ],
  edges: Array.from({ length: 24 }, (_, index) => graphEdge({
    id: `s${index}`,
    source: 'N.Hub',
    target: `N.Leaf${index}`,
    kind: 'binds',
  })),
});

beforeAll(async () => {
  const html = renderHtml({
    webview: { cspSource: 'test', asWebviewUri: (uri: Uri) => uri } as never,
    scriptUri: Uri.file('main.js'),
    styleUri: Uri.file('style.css'),
    nonce: 'n',
  });
  document.body.innerHTML = new DOMParser().parseFromString(html, 'text/html').body.innerHTML;

  const container = el<HTMLDivElement>('cy');
  container.style.padding = '0px';
  Object.defineProperty(container, 'clientWidth', { value: 100, configurable: true });
  Object.defineProperty(container, 'clientHeight', { value: 100, configurable: true });
  container.getBoundingClientRect = () => ({
    x: 0, y: 0, left: 0, top: 0, right: 100, bottom: 100, width: 100, height: 100, toJSON: () => ({}),
  }) as DOMRect;

  (window as unknown as { acquireVsCodeApi: () => unknown }).acquireVsCodeApi = () => ({
    postMessage: () => undefined,
    getState: () => undefined,
    setState: () => undefined,
  });
  await import('../../src/webview/client/main');
  await tick(10);

  // A layered layout positions every shape synchronously, so nothing is left animating.
  const layout = el<HTMLSelectElement>('layout');
  layout.value = 'layered-tb';
  layout.dispatchEvent(new Event('change'));

  send({ type: 'model', model: star });
  await tick(20);
});

describe('framing on a canvas smaller than the graph', () => {
  it('clamps to the readable zoom and centres the focused shape', () => {
    const core = captured.cores.at(-1) as never as {
      zoom: () => number;
      getElementById: (id: string) => { selected: () => boolean; nonempty: () => boolean };
    };

    send({ type: 'request', request: { title: 'Hub', nodeIds: [], focus: 'N.Hub', depth: 1 } });

    expect(core.zoom()).toBeCloseTo(0.34, 2);
    expect(core.getElementById('N.Hub').selected()).toBe(true);
  });
});
