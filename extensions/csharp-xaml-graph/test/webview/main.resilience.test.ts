/**
 * The panel's defensive edges.
 *
 * Every entry point in the canvas module runs from a timer, a DOM observer or a message, so it can
 * arrive before there is a canvas, before there is a model, or for a type that a filter has just
 * removed. The main suite always has a model; this one drives the states it never reaches, because a
 * thrown error in any of them would take the whole panel down rather than one panel's worth of data.
 */
import { beforeAll, beforeEach, describe, expect, it, vi } from 'vitest';
import type { Core } from 'cytoscape';
import { renderHtml } from '../../src/webview/html';
import type { GraphModel } from '../../src/graph/model';
import { graphEdge, graphModel, graphNode } from '../support/model';
import { Uri } from '../support/vscode';

const posted: Record<string, unknown>[] = [];
const captured = vi.hoisted(() => ({ cores: [] as unknown[] }));

vi.mock('cytoscape', async (importOriginal) => {
  // cytoscape declares `export =`, so its namespace has no typed `default` even though the bundler
    // hands one over. The cast states that shape once instead of leaving the error in every file.
    const actual = await importOriginal<typeof import('cytoscape')>() as unknown as {
      default: typeof import('cytoscape');
    };
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

let core: Core;

const sizeCanvas = (): void => {
  const container = el<HTMLDivElement>('cy');
  container.style.padding = '0px';
  Object.defineProperty(container, 'clientWidth', { value: 900, configurable: true });
  Object.defineProperty(container, 'clientHeight', { value: 640, configurable: true });
  container.getBoundingClientRect = () => ({
    x: 0, y: 0, left: 0, top: 0, right: 900, bottom: 640, width: 900, height: 640, toJSON: () => ({}),
  }) as DOMRect;
};

const load = async (next: GraphModel): Promise<void> => {
  sizeCanvas();
  send({ type: 'model', model: next });
  await tick(20);
  core = captured.cores.at(-1) as Core;
};

const toggle = (id: string, checked: boolean): void => {
  const box = el<HTMLInputElement>(id);
  box.checked = checked;
  box.dispatchEvent(new Event('change'));
};

/** Two modules of three connected types each, so both regions are big enough to be framed. */
const twoModules = (): GraphModel => graphModel({
  nodes: [
    graphNode({ id: 'N.A1', label: 'A1', layer: 'service', module: 'Alpha' }),
    graphNode({ id: 'N.A2', label: 'A2', layer: 'service', module: 'Alpha' }),
    graphNode({ id: 'N.A3', label: 'A3', layer: 'service', module: 'Alpha' }),
    graphNode({ id: 'N.B1', label: 'B1', layer: 'service', module: 'Beta' }),
    graphNode({ id: 'N.B2', label: 'B2', layer: 'service', module: 'Beta' }),
    graphNode({ id: 'N.B3', label: 'B3', layer: 'service', module: 'Beta' }),
  ],
  edges: [
    graphEdge({ id: 'a1', source: 'N.A1', target: 'N.A2', kind: 'injects' }),
    graphEdge({ id: 'a2', source: 'N.A2', target: 'N.A3', kind: 'injects' }),
    graphEdge({ id: 'b1', source: 'N.B1', target: 'N.B2', kind: 'injects' }),
    graphEdge({ id: 'b2', source: 'N.B2', target: 'N.B3', kind: 'injects' }),
  ],
});

/** The same two modules, but interleaved so neither owns the area it is drawn over. */
const contestedModules = (): GraphModel => graphModel({
  nodes: [
    graphNode({ id: 'N.A1', label: 'A1', layer: 'service', module: 'Alpha' }),
    graphNode({ id: 'N.B1', label: 'B1', layer: 'service', module: 'Beta' }),
    graphNode({ id: 'N.A2', label: 'A2', layer: 'service', module: 'Alpha' }),
    graphNode({ id: 'N.B2', label: 'B2', layer: 'service', module: 'Beta' }),
    graphNode({ id: 'N.A3', label: 'A3', layer: 'service', module: 'Alpha' }),
    graphNode({ id: 'N.B3', label: 'B3', layer: 'service', module: 'Beta' }),
  ],
  edges: [
    graphEdge({ id: 'c1', source: 'N.A1', target: 'N.B1', kind: 'injects' }),
    graphEdge({ id: 'c2', source: 'N.B1', target: 'N.A2', kind: 'injects' }),
    graphEdge({ id: 'c3', source: 'N.A2', target: 'N.B2', kind: 'injects' }),
    graphEdge({ id: 'c4', source: 'N.B2', target: 'N.A3', kind: 'injects' }),
    graphEdge({ id: 'c5', source: 'N.A3', target: 'N.B3', kind: 'injects' }),
  ],
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

describe('before a model has ever arrived', () => {
  it('survives every region toggle, the overview and a theme change', async () => {
    for (const id of ['group-captions', 'module-frames', 'island-tray', 'dim-selection']) {
      toggle(id, false);
      toggle(id, true);
    }

    const minimap = el<HTMLDivElement>('minimap');
    minimap.getBoundingClientRect = () => ({
      x: 0, y: 0, left: 0, top: 0, right: 168, bottom: 100, width: 168, height: 100, toJSON: () => ({}),
    }) as DOMRect;
    minimap.dispatchEvent(new MouseEvent('pointerdown', { clientX: 40, clientY: 40, bubbles: true }));

    // The theme observer is the only path into applyTheme; it must not need a canvas to exist.
    document.body.classList.add('vscode-light');
    await tick(20);
    document.body.classList.remove('vscode-light');

    expect(el('stats').textContent).toContain('analysing');
  });
});

describe('with a model drawn', () => {
  // Module filter, layers and toggles are module state that outlives a test, so each one starts from
  // the panel's own opening state.
  beforeEach(() => {
    const moduleSelect = el<HTMLSelectElement>('module');
    if (moduleSelect.options.length > 1) {
      moduleSelect.value = 'all';
      moduleSelect.dispatchEvent(new Event('change'));
    }
    for (const id of ['layer-view', 'layer-viewmodel', 'layer-service', 'layer-model', 'layer-external']) {
      toggle(id, true);
    }
    toggle('edge-reference', false);
    for (const id of ['group-captions', 'module-frames', 'island-tray', 'dim-selection']) {
      toggle(id, true);
    }
    const search = el<HTMLInputElement>('search');
    search.value = '';
    search.dispatchEvent(new Event('input'));
  });

  it('frames each module that owns its region', async () => {
    await load(twoModules());
    await tick(10);
    const frames = document.querySelectorAll('.overlay-frame-module');
    expect(frames.length).toBeGreaterThan(0);
    expect([...frames].map((frame) => frame.textContent).join(' ')).toMatch(/Alpha|Beta/);
  });

  it('draws no module frame when two modules share the same area', async () => {
    await load(contestedModules());
    await tick(10);
    expect(document.querySelectorAll('.overlay-frame-module').length).toBe(0);
  });

  it('draws no module frame while a single module is being inspected', async () => {
    await load(twoModules());
    const select = el<HTMLSelectElement>('module');
    select.value = 'Alpha';
    select.dispatchEvent(new Event('change'));
    await tick(10);
    expect(document.querySelectorAll('.overlay-frame-module').length).toBe(0);
  });

  it('keeps the graph usable when the overview element is missing', async () => {
    el<HTMLDivElement>('minimap').remove();
    await load(twoModules());
    expect(core.nodes().length).toBe(6);
    expect(el('stats').textContent).toContain('of 6 nodes');
  });

  it('clears the dimming when the selected type is filtered away', async () => {
    await load(twoModules());
    core.getElementById('N.A1').emit('tap', { originalEvent: {} } as never);
    await tick(10);

    // Hiding the selected type's whole layer removes it from the canvas while it stays selected.
    toggle('layer-service', false);
    await tick(10);
    toggle('dim-selection', false);
    toggle('dim-selection', true);
    await tick(10);

    expect(core.nodes().filter((node) => node.hasClass('dimmed')).length).toBe(0);
    toggle('layer-service', true);
  });

  it('falls back to a timer when the browser has no animation frames', async () => {
    await load(twoModules());
    const original = window.requestAnimationFrame;
    // A webview never lacks this, but the fallback is what keeps the overlay painting if it did.
    // The function is put back before anything can yield, because cytoscape schedules its own
    // render loop through it and would throw between the two lines.
    delete (window as unknown as { requestAnimationFrame?: unknown }).requestAnimationFrame;
    core.emit('pan', { originalEvent: {} } as never);
    (window as unknown as { requestAnimationFrame: typeof original }).requestAnimationFrame = original;
    await tick(30);
    expect(core.nodes().length).toBe(6);
  });

  it('shrinks the nodes automatically once the visible graph is large', async () => {
    const nodes = [];
    const edges = [];
    for (let index = 0; index < 700; index += 1) {
      nodes.push(graphNode({
        id: `N.N${index}`,
        label: `N${index}`,
        layer: index % 2 === 0 ? 'service' : 'model',
        module: 'Big',
      }));
      if (index > 0) {
        edges.push(graphEdge({ id: `e${index}`, source: `N.N${index - 1}`, target: `N.N${index}`, kind: 'injects' }));
      }
    }
    await load(graphModel({ nodes, edges }));
    expect(el<HTMLSelectElement>('density').value).toBe('compact');
  });

  it('skips an island band when the tray is switched off', async () => {
    toggle('island-tray', false);
    await load(twoModules());
    await tick(10);
    expect(document.querySelectorAll('.overlay-frame-island').length).toBe(0);
    toggle('island-tray', true);
  });
});
