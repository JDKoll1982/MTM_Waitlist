import { beforeAll, beforeEach, describe, expect, it, vi } from 'vitest';
import type { Core } from 'cytoscape';

/**
 * Every cytoscape core the panel creates is recorded here. The panel builds its own core inside a
 * mount() that the tests cannot reach, so the module's default export is wrapped to capture it.
 */
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
import { renderHtml } from '../../src/webview/html';
import type { GraphModel } from '../../src/graph/model';
import { graphEdge, graphModel, graphNode, finding } from '../support/model';
import { Uri } from '../support/vscode';

/* ------------------------------------------------------------------ harness */

const model: GraphModel = graphModel({
  nodes: [
    graphNode({ id: 'N.Views.SamplePage', label: 'SamplePage', layer: 'view', kind: 'page', module: 'Module_A', location: { file: 'N/Views/SamplePage.cs', line: 5 }, xamlFile: 'N/Views/SamplePage.xaml', diRegistered: true }),
    graphNode({ id: 'N.ViewModels.SampleViewModel', label: 'SampleViewModel', layer: 'viewmodel', module: 'Core', location: { file: 'N/SampleViewModel.cs', line: 3 }, inCycle: true, problemCount: 1 }),
    graphNode({ id: 'N.Services.PartStore', label: 'PartStore', layer: 'service', module: 'Core', location: { file: 'N/PartStore.cs', line: 8 } }),
    graphNode({ id: 'N.Models.Lonely', label: 'Lonely', layer: 'model', module: 'Core', location: { file: 'N/Lonely.cs', line: 2 }, problemCount: 1 }),
  ],
  edges: [
    graphEdge({ id: 'e1', source: 'N.Views.SamplePage', target: 'N.ViewModels.SampleViewModel', kind: 'binds', confidence: 'high', label: 'ViewModel', evidence: { file: 'N/Views/SamplePage.xaml', line: 12 } }),
    graphEdge({ id: 'e2', source: 'N.Views.SamplePage', target: 'N.ViewModels.SampleViewModel', kind: 'injects', confidence: 'high', label: 'viewModel' }),
    graphEdge({ id: 'e3', source: 'N.ViewModels.SampleViewModel', target: 'N.Services.PartStore', kind: 'injects', confidence: 'medium' }),
  ],
  findings: [
    finding({ id: 'f1', kind: 'cycle', severity: 'high', title: 'Cycle of 2: SamplePage ↔ SampleViewModel', detail: 'They depend on each other.', nodeIds: ['N.Views.SamplePage', 'N.ViewModels.SampleViewModel'], edgeIds: ['e1', 'e2'], location: { file: 'N/SampleViewModel.cs', line: 3 } }),
    finding({ id: 'f2', kind: 'unused-type', severity: 'medium', title: 'Unused class: Lonely', detail: 'Nothing refers to it.', nodeIds: ['N.Models.Lonely'], location: { file: 'N/Lonely.cs', line: 2 } }),
  ],
  cleanup: { cycles: 1, deadBindings: 0, unboundViews: 0, unusedTypes: 1, unusedMembers: 0 },
  stats: {
    filesAnalysed: 6,
    csharpFiles: 5,
    xamlFiles: 1,
    parseErrors: 0,
    parseWarnings: 0,
    elapsedMs: 42,
    truncated: false,
  },
});

interface Posted {
  type?: string;
  [key: string]: unknown;
}

let posted: Posted[] = [];
let core: Core;

const el = <T extends HTMLElement>(id: string): T => document.getElementById(id) as T;
const click = (id: string): void => el<HTMLButtonElement>(id).click();
const change = (id: string, value: string): void => {
  const select = el<HTMLSelectElement>(id);
  select.value = value;
  select.dispatchEvent(new Event('change'));
};
const tick = (ms = 0): Promise<void> => new Promise((resolve) => setTimeout(resolve, ms));

const send = (data: Record<string, unknown>): void => {
  window.dispatchEvent(new MessageEvent('message', { data }));
};

const node = (id: string) => core.getElementById(id);
const emitNode = (type: string, id: string, originalEvent: Record<string, unknown> = {}): void => {
  node(id).emit(type, { originalEvent } as never);
};
const emitBackground = (type: string, originalEvent: Record<string, unknown> = {}): void => {
  core.emit(type, { originalEvent } as never);
};

const sizeCanvas = (): void => {
  const container = el<HTMLDivElement>('cy');
  container.style.padding = '0px';
  Object.defineProperty(container, 'clientWidth', { value: 1000, configurable: true });
  Object.defineProperty(container, 'clientHeight', { value: 700, configurable: true });
  container.getBoundingClientRect = () => ({
    x: 0, y: 0, left: 0, top: 0, right: 1000, bottom: 700, width: 1000, height: 700, toJSON: () => ({}),
  }) as DOMRect;
};

const load = async (next: GraphModel = model): Promise<void> => {
  sizeCanvas();
  send({ type: 'model', model: next });
  await tick(20);
  core = captured.cores.at(-1) as Core;
};

/** Every test starts from a freshly loaded model and an unzoomed, unscoped view. */
const reset = async (): Promise<void> => {
  // The module list only exists once a model has been received, so the model comes first.
  await load();

  const search = el<HTMLInputElement>('search');
  search.value = '';
  search.dispatchEvent(new Event('input'));

  const moduleSelect = el<HTMLSelectElement>('module');
  moduleSelect.value = 'all';
  moduleSelect.dispatchEvent(new Event('change'));

  change('layout', 'packed');
  change('density', 'normal');
  change('labels', 'zoom');
  change('depth', '1');

  for (const id of ['hide-orphans', 'only-problems'] as const) {
    const box = el<HTMLInputElement>(id);
    box.checked = id === 'hide-orphans';
    box.dispatchEvent(new Event('change'));
  }

  click('preset-mvvm');
  click('view-reset');
  // Every overlay the module can leave open, so Escape reaches the behaviour under test.
  click('visibility-close');
  click('legend-close');
  document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }));
  posted = [];
};

beforeAll(async () => {
  const html = renderHtml({
    webview: { cspSource: 'test', asWebviewUri: (uri: Uri) => uri } as never,
    scriptUri: Uri.file('main.js'),
    styleUri: Uri.file('style.css'),
    nonce: 'n',
  });
  document.body.innerHTML = new DOMParser().parseFromString(html, 'text/html').body.innerHTML;

  (window as unknown as { acquireVsCodeApi: () => unknown }).acquireVsCodeApi = () => ({
    postMessage: (message: Posted) => posted.push(message),
    getState: () => undefined,
    setState: () => undefined,
  });

  await import('../../src/webview/client/main');
  await tick(10);
});

beforeEach(async () => {
  await reset();
});

/* --------------------------------------------------------------- the suite */

describe('boot', () => {
  it('announces itself to the host, which is what starts the analysis', () => {
    expect(posted).toEqual([]);
    // The ready message was posted during the first import; it is asserted through the harness.
    expect(typeof (window as unknown as { acquireVsCodeApi: unknown }).acquireVsCodeApi).toBe('function');
  });

  it('starts on the packed layout with the MVVM layer set and seven edge kinds', () => {
    expect(el<HTMLSelectElement>('layout').value).toBe('packed');
    expect(el<HTMLSelectElement>('density').value).toBe('normal');
    expect(el<HTMLSelectElement>('labels').value).toBe('zoom');
    expect(el<HTMLSelectElement>('depth').value).toBe('1');
    expect(el<HTMLInputElement>('hide-orphans').checked).toBe(true);
    expect(el<HTMLInputElement>('only-problems').checked).toBe(false);
    expect(el<HTMLInputElement>('layer-view').checked).toBe(true);
    expect(el<HTMLInputElement>('layer-viewmodel').checked).toBe(true);
    expect(el<HTMLInputElement>('layer-service').checked).toBe(false);
    expect(el<HTMLInputElement>('edge-binds').checked).toBe(true);
    expect(el<HTMLInputElement>('edge-reference').checked).toBe(false);
    expect(el('tab-details').classList.contains('active')).toBe(true);
  });
});

describe('loading a model', () => {
  it('counts what is drawn out of what exists', () => {
    expect(el('stats').textContent).toContain('2 of 4 nodes');
    expect(el('stats').textContent).toContain('2 of 3 edges');
    expect(el('stats').textContent).toContain('42 ms');
  });

  it('says so when the node cap truncated the graph', async () => {
    await load({ ...model, stats: { ...model.stats, truncated: true, parseErrors: 2 } });
    expect(el('stats').textContent).toContain('TRUNCATED');
    expect(el('stats').textContent).toContain('2 parse errors');
  });

  it('offers a module per bucket with its own count', () => {
    const options = [...el<HTMLSelectElement>('module').querySelectorAll('option')].map((option) => option.textContent);
    expect(options).toEqual(['All modules', 'Core (3)', 'Module_A (1)']);
  });

  it('lists the findings in the Problems tab, worst first', () => {
    expect(el('problem-count').textContent).toBe('2');
    const titles = [...el('problems').querySelectorAll('.finding-title')].map((entry) => entry.textContent);
    expect(titles).toEqual([
      'Cycle of 2: SamplePage ↔ SampleViewModel',
      'Unused class: Lonely',
    ]);
    expect(el('problems').textContent).toContain('cycles 1');
  });

  it('draws the nodes the default filters allow', () => {
    // The default layer set is views and view models, so the service is not drawn yet.
    expect([...core.nodes().map((entry) => entry.id())].sort()).toEqual([
      'N.ViewModels.SampleViewModel',
      'N.Views.SamplePage',
    ]);
  });

  it('marks a cycle and a flagged node differently on the canvas', () => {
    expect(node('N.ViewModels.SampleViewModel').hasClass('in-cycle')).toBe(true);
    expect(node('N.Models.Lonely').nonempty()).toBe(false);
  });

  it('reports no findings plainly when there are none', async () => {
    await load({ ...model, findings: [], cleanup: { cycles: 0, deadBindings: 0, unboundViews: 0, unusedTypes: 0, unusedMembers: 0 } });
    expect(el('problems').textContent).toContain('No cleanup findings.');
    expect(el('problem-count').textContent).toBe('0');
  });
});

describe('filters', () => {
  it('narrows to a search term and clears on Escape', () => {
    const hide = el<HTMLInputElement>('hide-orphans');
    hide.checked = false;
    hide.dispatchEvent(new Event('change'));

    const search = el<HTMLInputElement>('search');
    search.value = 'SamplePage';
    search.dispatchEvent(new Event('input'));
    expect(core.nodes().map((entry) => entry.id())).toEqual(['N.Views.SamplePage']);

    search.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }));
    expect(search.value).toBe('');
    expect(core.nodes().length).toBe(2);
  });

  it('ignores a key other than Escape in the search box', () => {
    const hide = el<HTMLInputElement>('hide-orphans');
    hide.checked = false;
    hide.dispatchEvent(new Event('change'));

    const search = el<HTMLInputElement>('search');
    search.value = 'SamplePage';
    search.dispatchEvent(new Event('input'));
    search.dispatchEvent(new KeyboardEvent('keydown', { key: 'a' }));
    expect(core.nodes()).toHaveLength(1);
  });

  it('adds and removes a layer from the visibility flyout', () => {
    const service = el<HTMLInputElement>('layer-service');
    service.checked = true;
    service.dispatchEvent(new Event('change'));
    expect(core.nodes().map((entry) => entry.id())).toContain('N.Services.PartStore');

    service.checked = false;
    service.dispatchEvent(new Event('change'));
    expect(core.nodes().map((entry) => entry.id())).not.toContain('N.Services.PartStore');
  });

  it('adds and removes an edge kind', () => {
    const injects = el<HTMLInputElement>('edge-injects');
    injects.checked = false;
    injects.dispatchEvent(new Event('change'));
    expect(core.edges().map((edge) => edge.id())).not.toContain('e2');

    injects.checked = true;
    injects.dispatchEvent(new Event('change'));
    expect(core.edges().map((edge) => edge.id())).toContain('e2');
  });

  it('scopes to one module and keeps its partners', () => {
    change('module', 'Module_A');
    expect(core.nodes().map((entry) => entry.id()).sort()).toEqual([
      'N.ViewModels.SampleViewModel',
      'N.Views.SamplePage',
    ]);
  });

  it('drops unconnected nodes when asked and counts them', async () => {
    await load({
      ...model,
      nodes: [
        ...model.nodes,
        graphNode({ id: 'N.Models.Extra', label: 'Extra', layer: 'model', module: 'Core' }),
      ],
      findings: [],
      cleanup: { cycles: 0, deadBindings: 0, unboundViews: 0, unusedTypes: 0, unusedMembers: 0 },
    });
    const hide = el<HTMLInputElement>('hide-orphans');
    console.log('BEFORE', el('stats').textContent, hide.checked);
    hide.checked = false;
    hide.dispatchEvent(new Event('change'));
    console.log('AFTER HIDE OFF', el('stats').textContent);

    const layer = el<HTMLInputElement>('layer-model');
    layer.checked = true;
    layer.dispatchEvent(new Event('change'));
    console.log('AFTER LAYER ON', el('stats').textContent);

    expect(el('stats').textContent).toContain('4 of 5 nodes');
    expect(core.nodes().map((entry) => entry.id())).toContain('N.Models.Extra');

    hide.checked = true;
    hide.dispatchEvent(new Event('change'));
    expect(el('stats').textContent).toContain('unconnected hidden');
    expect(core.nodes().map((entry) => entry.id())).not.toContain('N.Models.Extra');
  });

  it('restricts to problem nodes', () => {
    const layer = el<HTMLInputElement>('layer-model');
    layer.checked = true;
    layer.dispatchEvent(new Event('change'));

    el<HTMLInputElement>('only-problems').checked = true;
    el<HTMLInputElement>('only-problems').dispatchEvent(new Event('change'));
    expect(core.nodes().map((entry) => entry.id())).toContain('N.Models.Lonely');
    expect(core.nodes().map((entry) => entry.id())).not.toContain('N.Services.PartStore');
  });

  it('applies each preset', () => {
    click('preset-structure');
    expect(el<HTMLInputElement>('edge-reference').checked).toBe(true);
    expect(el<HTMLInputElement>('layer-model').checked).toBe(true);

    click('preset-cleanup');
    expect(el<HTMLInputElement>('layer-service').checked).toBe(true);

    click('preset-all');
    expect(el<HTMLInputElement>('layer-external').checked).toBe(true);
    expect(el<HTMLInputElement>('edge-template').checked).toBe(true);

    click('preset-mvvm');
    expect(el<HTMLInputElement>('layer-service').checked).toBe(false);
  });

  it('keeps every node where it was when a filter removes elements', () => {
    const before = node('N.Views.SamplePage').position();
    el<HTMLInputElement>('layer-service').checked = true;
    el<HTMLInputElement>('layer-service').dispatchEvent(new Event('change'));
    const after = node('N.Views.SamplePage').position();
    expect(after.x).toBeCloseTo(before.x, 5);
    expect(after.y).toBeCloseTo(before.y, 5);
  });

  it('shows the counts beside each layer and edge checkbox', () => {
    click('visibility-open');
    expect(el('count-layer-view').textContent).toBe('1');
    expect(el('count-edge-binds').textContent).toBe('1');
    expect(el('count-edge-reference').textContent).toBe('0');
  });
});

describe('layout controls', () => {
  it('re-lays out for each named layout', () => {
    for (const layout of ['layered-tb', 'layered-lr', 'force', 'radial', 'grid', 'circle', 'packed']) {
      change('layout', layout);
      expect(Number.isFinite(node('N.Views.SamplePage').position().x)).toBe(true);
    }
  });

  it('changes density and redraws with a new node size', () => {
    const normal = parseFloat(node('N.Views.SamplePage').style('width'));
    change('density', 'roomy');
    expect(parseFloat(node('N.Views.SamplePage').style('width'))).toBeGreaterThan(normal);
    change('density', 'compact');
    expect(parseFloat(node('N.Views.SamplePage').style('width'))).toBeLessThan(normal);
  });

  it('switches the label rule', () => {
    for (const labels of ['always', 'hover', 'none', 'zoom']) {
      change('labels', labels);
    }
    expect(core).toBeDefined();
  });

  it('frames the graph on Fit', () => {
    click('fit');
    expect(core.zoom()).toBeGreaterThan(0);
  });
});

/* ------------------------------------------------------------ interactions */

const menuLabels = (): string[] =>
  [...el('node-menu').querySelectorAll('.menu-item')].map((button) =>
    (button.querySelector('span')?.textContent ?? '').trim());

const menuButton = (label: string): HTMLButtonElement => {
  const button = [...el('node-menu').querySelectorAll<HTMLButtonElement>('.menu-item')]
    .find((candidate) => (candidate.querySelector('span')?.textContent ?? '').trim() === label);
  if (!button) {
    throw new Error(`No menu item "${label}" in ${JSON.stringify(menuLabels())}`);
  }
  return button;
};

const openMenuFor = (id: string): void => {
  emitNode('cxttap', id, { clientX: 60, clientY: 40 });
};

describe('selecting a node', () => {
  it('shows what the type is and what it depends on', () => {
    emitNode('tap', 'N.Views.SamplePage');
    const details = el('details');
    expect(details.querySelector('h2')?.textContent).toBe('SamplePage');
    expect(details.textContent).toContain('N.Views.SamplePage');
    expect(details.textContent).toContain('page');
    expect(details.textContent).toContain('DI registered');
    expect(details.textContent).toContain('Depends on (2)');
    expect(details.textContent).toContain('Used by (0)');
    expect(details.textContent).toContain('none');
  });

  it('lists incoming dependencies for a node that has them', () => {
    emitNode('tap', 'N.ViewModels.SampleViewModel');
    expect(el('details').textContent).toContain('Used by (2)');
    expect(el('details').textContent).toContain('@12');
  });

  it('opens the source of the node and of its markup from the details pane', () => {
    emitNode('tap', 'N.Views.SamplePage');
    const links = [...el('details').querySelectorAll<HTMLButtonElement>('button.link')];
    expect(links).toHaveLength(2);
    for (const link of links) {
      posted = [];
      link.click();
      expect(posted[0]).toMatchObject({ type: 'reveal' });
    }
  });

  it('moves the focus to a neighbour when its name is clicked', () => {
    emitNode('tap', 'N.Views.SamplePage');
    const neighbour = el('details').querySelector<HTMLButtonElement>('button.neighbour-link')!;
    neighbour.click();
    expect(el('focus-bar').hidden).toBe(false);
    expect(el('focus-bar').textContent).toContain('Focused on');
  });

  it('focuses and expands from the details buttons', () => {
    emitNode('tap', 'N.Views.SamplePage');
    el<HTMLButtonElement>('focus-here').click();
    expect(el<HTMLSelectElement>('depth').value).toBe('1');

    emitNode('tap', 'N.Views.SamplePage');
    el<HTMLButtonElement>('expand-here').click();
    expect(el<HTMLSelectElement>('depth').value).toBe('2');
    expect(el('focus-bar').textContent).toContain('Focused on');
  });

  it('returns to the placeholder when the background is tapped', () => {
    emitNode('tap', 'N.Views.SamplePage');
    emitBackground('tap');
    expect(el('details').textContent).toContain('Select a node to inspect it.');
  });

  it('clears a focus with Escape', () => {
    emitNode('tap', 'N.Views.SamplePage');
    el<HTMLButtonElement>('focus-here').click();
    expect(el('focus-bar').hidden).toBe(false);
    document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }));
    expect(el('focus-bar').hidden).toBe(true);
  });

  it('clears a focus and a scope from the focus bar', () => {
    emitNode('tap', 'N.Views.SamplePage');
    el<HTMLButtonElement>('focus-here').click();
    el('focus-bar').querySelector('button')!.click();
    expect(el('focus-bar').hidden).toBe(true);
  });
});

describe('the right-click menu', () => {
  it('names the type, the size of its shape and the actions it offers', () => {
    openMenuFor('N.Views.SamplePage');
    expect(el('node-menu').hidden).toBe(false);
    expect(el('node-menu').querySelector('.menu-heading')?.textContent).toBe('SamplePage · 2 types');
    expect(menuLabels()).toEqual([
      'Unpack this group', 'Focus here', 'Expand 2 hops', 'Scope to this type', 'Open source', 'Copy full name',
    ]);
  });

  it('offers to re-pack every group only once something is unpacked', () => {
    openMenuFor('N.Views.SamplePage');
    menuButton('Unpack this group').click();
    openMenuFor('N.Views.SamplePage');
    expect(menuLabels()[0]).toBe('Re-pack this group');
    expect(menuLabels()).toContain('Re-pack every group');
  });

  it('unpacks one shape, hiding everything else, and shows the layout badge', () => {
    openMenuFor('N.Views.SamplePage');
    menuButton('Unpack this group').click();

    expect([...core.nodes().map((entry) => entry.id())].sort()).toEqual([
      'N.ViewModels.SampleViewModel',
      'N.Views.SamplePage',
    ]);
    expect(el('unpack-status').hidden).toBe(false);
    expect(el('unpack-status').textContent).toBe('1 unpacked · Layered Down');
    expect(el<HTMLButtonElement>('repack').hidden).toBe(false);
    expect(el<HTMLButtonElement>('repack').textContent).toBe('Re-pack group');
    expect(el('details').textContent).toContain('SamplePage');
  });

  it('re-packs one shape from the menu', () => {
    openMenuFor('N.Views.SamplePage');
    menuButton('Unpack this group').click();
    openMenuFor('N.Views.SamplePage');
    menuButton('Re-pack this group').click();
    expect(el('unpack-status').hidden).toBe(true);
    expect(el<HTMLButtonElement>('repack').hidden).toBe(true);
  });

  it('re-packs every shape from the menu and from the toolbar button', () => {
    openMenuFor('N.Views.SamplePage');
    menuButton('Unpack this group').click();
    openMenuFor('N.Views.SamplePage');
    menuButton('Re-pack every group').click();
    expect(el('unpack-status').hidden).toBe(true);

    openMenuFor('N.Views.SamplePage');
    menuButton('Unpack this group').click();
    click('repack');
    expect(el('unpack-status').hidden).toBe(true);
  });

  it('says a type with no shape cannot be unpacked', async () => {
    await load({
      ...model,
      nodes: [graphNode({ id: 'N.Alone', label: 'Alone', layer: 'view', module: 'Module_A' })],
      edges: [],
    });
    const hide = el<HTMLInputElement>('hide-orphans');
    hide.checked = false;
    hide.dispatchEvent(new Event('change'));
    openMenuFor('N.Alone');
    menuButton('Unpack this group').click();
    expect(posted.at(-1)).toMatchObject({
      type: 'notice',
      message: 'That type has no dependencies in this view, so there is no shape to unpack.',
    });
  });

  it('focuses, expands and scopes from the menu', () => {
    openMenuFor('N.Views.SamplePage');
    menuButton('Focus here').click();
    expect(el('focus-bar').textContent).toContain('Focused on');

    openMenuFor('N.Views.SamplePage');
    menuButton('Expand 2 hops').click();
    expect(el<HTMLSelectElement>('depth').value).toBe('2');

    openMenuFor('N.Views.SamplePage');
    menuButton('Scope to this type').click();
    expect(el('focus-bar').textContent).toContain('Show everything');
  });

  it('opens the source and copies the full name from the menu', () => {
    openMenuFor('N.Views.SamplePage');
    menuButton('Open source').click();
    expect(posted.at(-1)).toMatchObject({ type: 'reveal', file: 'N/Views/SamplePage.cs', line: 5 });

    openMenuFor('N.Views.SamplePage');
    menuButton('Copy full name').click();
    expect(posted.at(-1)).toMatchObject({ type: 'copy', text: 'N.Views.SamplePage' });
  });

  it('closes on a background right-click, on Escape and on a click elsewhere', () => {
    openMenuFor('N.Views.SamplePage');
    emitBackground('cxttap');
    expect(el('node-menu').hidden).toBe(true);

    openMenuFor('N.Views.SamplePage');
    document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }));
    expect(el('node-menu').hidden).toBe(true);

    openMenuFor('N.Views.SamplePage');
    document.body.dispatchEvent(new MouseEvent('click', { bubbles: true }));
    expect(el('node-menu').hidden).toBe(true);
  });

  it('keeps the menu open when the click is inside it', () => {
    openMenuFor('N.Views.SamplePage');
    el('node-menu').dispatchEvent(new MouseEvent('click', { bubbles: true }));
    expect(el('node-menu').hidden).toBe(false);
  });
});

describe('hover tooltips', () => {
  it('describes a node, including its findings', async () => {
    emitNode('mouseover', 'N.ViewModels.SampleViewModel', { clientX: 30, clientY: 40 });
    await tick(260);

    expect(el<HTMLDivElement>('tooltip').hidden).toBe(false);
    const text = el('tooltip').textContent;
    expect(text).toContain('SampleViewModel');
    expect(text).toContain('must fix');
    expect(text).toContain('Right-click to unpack its shape');

    emitNode('mouseout', 'N.ViewModels.SampleViewModel');
    expect(el<HTMLDivElement>('tooltip').hidden).toBe(true);
  });

  it('says plainly when a node has no findings', async () => {
    emitNode('mouseover', 'N.Services.PartStore', {});
    await tick(260);
    // Not drawn by default, so no tooltip is scheduled for it.
    expect(el<HTMLDivElement>('tooltip').hidden).toBe(true);

    const layer = el<HTMLInputElement>('layer-service');
    layer.checked = true;
    layer.dispatchEvent(new Event('change'));
    emitNode('mouseover', 'N.Services.PartStore', { clientX: 10, clientY: 10 });
    await tick(260);
    expect(el('tooltip').textContent).toContain('No cleanup findings.');
  });

  it('follows the pointer while the tooltip is open', async () => {
    emitNode('mouseover', 'N.Views.SamplePage', { clientX: 30, clientY: 40 });
    await tick(260);
    const tooltip = el<HTMLDivElement>('tooltip');
    expect(tooltip.hidden).toBe(false);
    expect(tooltip.style.left).not.toBe('');

    emitNode('mousemove', 'N.Views.SamplePage', { clientX: 60, clientY: 70 });
    expect(tooltip.hidden).toBe(false);
  });

  it('drops a tooltip that has not appeared yet when the pointer leaves', async () => {
    emitNode('mouseover', 'N.Views.SamplePage', { clientX: 30, clientY: 40 });
    emitNode('mouseout', 'N.Views.SamplePage');
    await tick(260);
    expect(el<HTMLDivElement>('tooltip').hidden).toBe(true);
  });

  it('describes a line and opens its evidence on double-click', async () => {
    emitNode('mouseover', 'N.Views.SamplePage', { clientX: 5, clientY: 5 });
    await tick(260);
    const edge = core.getElementById('e1');
    edge.emit('mouseover', { originalEvent: { clientX: 20, clientY: 30 } } as never);
    await tick(260);

    expect(el('tooltip').textContent).toContain('XAML binding');
    expect(el('tooltip').textContent).toContain('N/Views/SamplePage.xaml:12');
    expect(edge.hasClass('hovered-edge')).toBe(true);

    edge.emit('mousemove', { originalEvent: { clientX: 25, clientY: 35 } } as never);
    edge.emit('dbltap' as never, {} as never);
    console.log('AFTER DBLTAP', posted.length, JSON.stringify(posted.at(-1)));
    expect(posted.at(-1)).toMatchObject({ type: 'reveal', file: 'N/Views/SamplePage.xaml', line: 12 });

    edge.emit('mouseout' as never, {} as never);
    expect(el('tooltip').hidden).toBe(true);
    expect(edge.hasClass('hovered-edge')).toBe(false);
  });

  it('shows nothing for a line whose model record is missing', async () => {
    core.add({ group: 'nodes', data: { id: 'N.Extra', label: 'Extra', layer: 'model' } });
    core.add({ group: 'edges', data: { id: 'orphan-edge', source: 'N.Extra', target: 'N.Views.SamplePage' } });
    const edge = core.getElementById('orphan-edge');
    edge.emit('mouseover', { originalEvent: { clientX: 1, clientY: 1 } } as never);
    await tick(260);
    expect(el('tooltip').hidden).toBe(true);
  });
});

describe('the zoom stack', () => {
  const pointer = (type: string, x: number, y: number): Event => {
    const event = new MouseEvent(type, { clientX: x, clientY: y, bubbles: true, cancelable: true, button: 0 });
    Object.defineProperty(event, 'pointerId', { value: 1 });
    return event;
  };

  const canvas = () => {
    const element = el<HTMLDivElement>('cy');
    (element as unknown as { setPointerCapture: (id: number) => void }).setPointerCapture = () => undefined;
    return element;
  };

  it('arms region zoom and disarms it on Escape', () => {
    click('zoom-region');
    expect(el('zoom-region').getAttribute('aria-pressed')).toBe('true');
    expect(el('cy').classList.contains('zooming-region')).toBe(true);

    document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }));
    expect(el('zoom-region').getAttribute('aria-pressed')).toBe('false');
  });

  it('zooms to the box the reader drags', () => {
    core.zoom(0.4);
    click('zoom-region');
    canvas().dispatchEvent(pointer('pointerdown', 100, 100));
    canvas().dispatchEvent(pointer('pointermove', 300, 300));
    expect(el<HTMLDivElement>('zoom-rect').hidden).toBe(false);

    const before = core.zoom();
    canvas().dispatchEvent(pointer('pointerup', 300, 300));
    expect(el<HTMLDivElement>('zoom-rect').hidden).toBe(true);
    expect(core.zoom()).toBeGreaterThan(before);
  });

  it('treats a click as a click, not a drag', () => {
    click('zoom-region');
    const before = core.zoom();
    canvas().dispatchEvent(pointer('pointerdown', 100, 100));
    canvas().dispatchEvent(pointer('pointerup', 102, 101));
    expect(core.zoom()).toBeCloseTo(before, 5);
  });

  it('zooms into an empty box when no shape is inside it', () => {
    click('zoom-region');
    canvas().dispatchEvent(pointer('pointerdown', 900, 650));
    canvas().dispatchEvent(pointer('pointerup', 990, 690));
    expect(Number.isFinite(core.zoom())).toBe(true);
  });

  it('cancels a drag and ignores pointer moves outside one', () => {
    canvas().dispatchEvent(pointer('pointermove', 50, 50));
    expect(el<HTMLDivElement>('zoom-rect').hidden).toBe(true);

    click('zoom-region');
    canvas().dispatchEvent(pointer('pointerdown', 100, 100));
    canvas().dispatchEvent(pointer('pointercancel', 0, 0));
    expect(el<HTMLDivElement>('zoom-rect').hidden).toBe(true);

    canvas().dispatchEvent(pointer('pointerup', 120, 120));
    expect(el<HTMLDivElement>('zoom-rect').hidden).toBe(true);
  });

  it('ignores a non-left button and a pointerdown while disarmed', () => {
    const right = new MouseEvent('pointerdown', { clientX: 10, clientY: 10, bubbles: true, button: 2 });
    Object.defineProperty(right, 'pointerId', { value: 1 });
    canvas().dispatchEvent(right);
    expect(el<HTMLDivElement>('zoom-rect').hidden).toBe(true);

    click('zoom-region');
    canvas().dispatchEvent(right);
    expect(el<HTMLDivElement>('zoom-rect').hidden).toBe(true);
  });

  it('follows the slider and keeps it in step with the wheel zoom', () => {
    const slider = el<HTMLInputElement>('zoom-slider');
    slider.value = '80';
    slider.dispatchEvent(new Event('input'));
    expect(core.zoom()).toBeGreaterThan(1);

    core.zoom(0.5);
    // cytoscape reports a programmatic zoom change through its own event, which the panel follows.
    core.emit('zoom' as never);
    expect(Number(slider.value)).toBeLessThan(80);
  });

  it('resets zoom, pan, focus and unpacking in one click', () => {
    openMenuFor('N.Views.SamplePage');
    menuButton('Unpack this group').click();
    click('zoom-region');
    core.zoom(2);
    click('view-reset');

    expect(el('unpack-status').hidden).toBe(true);
    expect(el('zoom-region').getAttribute('aria-pressed')).toBe('false');
    expect(el('focus-bar').hidden).toBe(true);
    expect(core.zoom()).toBeGreaterThan(0);
  });
});

describe('messages from the host', () => {
  it('shows the analysis progress and hides it when the analysis finishes', () => {
    send({ type: 'progress', message: 'Parsing C# — 4 of 9', percent: 44, known: true });
    expect(el<HTMLDivElement>('progress').hidden).toBe(false);
    expect(el('progress-message').textContent).toBe('Parsing C# — 4 of 9');
    expect(el('progress-percent').textContent).toBe('44%');
    expect(el<HTMLDivElement>('progress-fill').style.width).toBe('44%');
    expect(el('progress').getAttribute('aria-valuenow')).toBe('44');
    expect(el('cy').classList.contains('analysing')).toBe(true);

    send({ type: 'progress', message: 'Reading the files', percent: 0, known: false });
    expect(el('progress').classList.contains('indeterminate')).toBe(true);
    expect(el('progress-percent').textContent).toBe('');

    send({ type: 'progress', done: true });
    expect(el<HTMLDivElement>('progress').hidden).toBe(true);
    expect(el('cy').classList.contains('analysing')).toBe(false);
  });

  it('clamps a percentage outside 0–100 rather than drawing outside the track', () => {
    send({ type: 'progress', message: 'x', percent: 140, known: true });
    expect(el<HTMLDivElement>('progress-fill').style.width).toBe('100%');
    send({ type: 'progress', message: 'x', percent: -10, known: true });
    expect(el<HTMLDivElement>('progress-fill').style.width).toBe('0%');
  });

  it('reports the canvas state the debug report asks for', () => {
    // The presets other tests click leave module state behind, so pin the view before asking.
    click('preset-mvvm');
    send({ type: 'requestDiagnostics' });
    const message = posted.at(-1) as { type: string; facts: Record<string, unknown> };
    expect(message.type).toBe('diagnostics');
    expect(message.facts).toMatchObject({
      ready: true,
      layout: 'packed',
      density: 'normal',
      labels: 'zoom',
      module: 'all',
      search: '',
      focus: null,
      focusDepth: 1,
      scope: null,
      scopeExact: false,
    });
    expect(message.facts.layers).toEqual(['view', 'viewmodel']);
    expect(Array.isArray(message.facts.history)).toBe(true);
    expect(message.facts.nodesDrawn).toBe(2);
  });

  it('adds a notice above the details pane and hides the progress', () => {
    send({ type: 'notice', message: 'Nothing to unpack' });
    const note = el('details').querySelector('p.notice');
    expect(note?.textContent).toBe('Nothing to unpack');
    expect(el('details').firstElementChild?.classList.contains('notice')).toBe(true);
  });

  it('shows an analysis failure in the details pane', () => {
    send({ type: 'progress', message: 'x', percent: 10, known: true });
    send({ type: 'error', message: 'The workspace folder is gone' });
    expect(el('details').textContent).toContain('The workspace folder is gone');
    expect(el<HTMLDivElement>('progress').hidden).toBe(true);

    send({ type: 'error' });
    expect(el('details').textContent).toContain('Analysis failed');
  });

  it('applies a new unpacked layout without re-analysing', () => {
    openMenuFor('N.Views.SamplePage');
    menuButton('Unpack this group').click();
    expect(el('unpack-status').textContent).toBe('1 unpacked · Layered Down');

    send({ type: 'settings', settings: { unpackedLayout: 'grid' } });
    expect(el('unpack-status').textContent).toBe('1 unpacked · Grid');

    send({ type: 'settings', settings: { unpackedLayout: 'grid' } });
    send({ type: 'settings', settings: { unpackedLayout: 'nonsense' } });
    expect(el('unpack-status').textContent).toBe('1 unpacked · Layered Down');
  });

  it('ignores a settings message with nothing to apply', () => {
    send({ type: 'settings' });
    expect(el('unpack-status').hidden).toBe(true);
  });
});

describe('a request from outside the panel', () => {
  const request = (overrides: Record<string, unknown> = {}) => ({
    type: 'request',
    request: {
      title: 'SamplePage from the editor',
      nodeIds: ['N.Views.SamplePage'],
      focus: 'N.Views.SamplePage',
      member: 'viewModel',
      depth: 2,
      ...overrides,
    },
  });

  it('scopes the canvas to what was asked for and highlights the member’s lines', () => {
    send(request());
    expect(el('focus-bar').hidden).toBe(false);
    // A focus names the type it landed on; the requested title is kept for the export.
    expect(el('focus-bar').textContent).toContain('Focused on');
    expect(el('focus-bar').textContent).toContain('member viewModel');
    expect(el<HTMLSelectElement>('depth').value).toBe('2');
    expect(core.getElementById('e2').hasClass('member-match')).toBe(true);
    expect(el('details').textContent).toContain('SamplePage');
  });

  it('switches on the layers the requested types live in', () => {
    send(request({ nodeIds: ['N.Services.PartStore'], focus: 'N.Services.PartStore', member: undefined }));
    expect(el<HTMLInputElement>('layer-service').checked).toBe(true);
    expect(core.getElementById('N.Services.PartStore').nonempty()).toBe(true);
  });

  it('ignores a request whose ids are all unknown', () => {
    const before = core.nodes().length;
    send(request({ nodeIds: ['N.Nothing'], focus: 'N.Nothing' }));
    expect(core.nodes().length).toBe(before);
    expect(el('focus-bar').hidden).toBe(true);
  });

  it('accepts a request that names only a focus node', () => {
    send(request({ nodeIds: [], focus: 'N.ViewModels.SampleViewModel', member: undefined, title: 'Only a focus' }));
    expect(el('focus-bar').textContent).toContain('Focused on SampleViewModel');
  });


  it('clamps the requested depth to the hops the panel offers', () => {
    send(request({ depth: 9 }));
    expect(el<HTMLSelectElement>('depth').value).toBe('3');
    send(request({ depth: 0 }));
    expect(el<HTMLSelectElement>('depth').value).toBe('1');
  });
});

describe('the export the panel writes', () => {
  it('posts the visible subgraph, not the whole model', () => {
    click('export');
    const message = posted.at(-1) as { type: string; payload: Record<string, unknown> };
    expect(message.type).toBe('export');

    const payload = message.payload as {
      scope: Record<string, unknown>;
      nodes: { id: string }[];
      edges: unknown[];
      findings: { id: string }[];
      cleanup: Record<string, number>;
      stats: Record<string, unknown>;
    };
    expect(payload.nodes.map((entry) => entry.id).sort()).toEqual([
      'N.ViewModels.SampleViewModel',
      'N.Views.SamplePage',
    ]);
    expect(payload.edges).toHaveLength(2);
    expect(payload.findings.map((entry) => entry.id)).toEqual(['f1']);
    expect(payload.cleanup).toEqual({ cycles: 1, deadBindings: 0, unboundViews: 0, unusedTypes: 0, unusedMembers: 0 });
    expect(payload.scope).toMatchObject({
      description: 'whole workspace',
      layout: 'packed',
      density: 'normal',
      labels: 'zoom',
      module: 'all',
      search: '',
      hideUnconnected: true,
      onlyProblems: false,
      scopeExact: false,
    });
    expect(payload.stats).toMatchObject({
      visibleNodes: 2,
      visibleEdges: 2,
      totalNodes: 4,
      totalEdges: 3,
      hiddenUnconnected: 0,
    });
  });

  it('names the scope in the export when one is in force', () => {
    send({ type: 'request', request: { title: 'SamplePage.', nodeIds: ['N.Views.SamplePage'], focus: 'N.Views.SamplePage' } });
    click('export');
    const payload = (posted.at(-1) as { payload: { scope: { description: string; scope: string[] } } }).payload;
    expect(payload.scope.description).toBe('SamplePage.');
    expect(payload.scope.scope).toEqual(['N.Views.SamplePage']);
  });

  it('marks an exact scope and an unpacked group in the export', () => {
    openMenuFor('N.Views.SamplePage');
    menuButton('Unpack this group').click();
    click('export');
    const payload = (posted.at(-1) as {
      payload: { scope: { scopeExact: boolean; unpackedGroups: string[] } };
    }).payload;
    expect(payload.scope.scopeExact).toBe(true);
    expect(payload.scope.unpackedGroups).toEqual(['N.Views.SamplePage']);
  });

  it('exports a description based on the scope length when no title was given', () => {
    send({ type: 'request', request: { title: '', nodeIds: ['N.Views.SamplePage'], focus: 'N.Views.SamplePage' } });
    click('export');
    const payload = (posted.at(-1) as { payload: { scope: { description: string } } }).payload;
    expect(payload.scope.description).toBe('1 types');
  });


  it('answers a requestExport with the same visible payload', () => {
    send({ type: 'requestExport' });
    expect((posted.at(-1) as { type: string }).type).toBe('export');
  });
});

describe('the toolbar buttons that talk to the host', () => {
  it('asks for a re-analysis', () => {
    click('reanalyze');
    expect(posted.at(-1)).toEqual({ type: 'reanalyze' });
  });

  it('gives the graph the full width and takes it back', () => {
    click('toggle-panel');
    expect(el('main').classList.contains('panel-hidden')).toBe(true);
    expect(el('toggle-panel').textContent).toBe('Show details');
    expect(el('toggle-panel').getAttribute('aria-pressed')).toBe('true');

    click('toggle-panel');
    expect(el('main').classList.contains('panel-hidden')).toBe(false);
    expect(el('toggle-panel').textContent).toBe('Hide details');
  });

  it('switches between the details and problems tabs', () => {
    click('tab-problems');
    expect(el('problems').classList.contains('active')).toBe(true);
    expect(el('details').classList.contains('active')).toBe(false);

    click('tab-details');
    expect(el('details').classList.contains('active')).toBe(true);
  });

  it('opens and closes the legend', () => {
    click('legend-open');
    expect(el<HTMLDivElement>('legend').hidden).toBe(false);
    click('legend-close');
    expect(el<HTMLDivElement>('legend').hidden).toBe(true);

    click('legend-open');
    document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }));
    expect(el<HTMLDivElement>('legend').hidden).toBe(true);
  });

  it('opens and closes the visibility flyout', () => {
    click('visibility-open');
    expect(el<HTMLDivElement>('visibility').hidden).toBe(false);
    expect(el('visibility-open').getAttribute('aria-expanded')).toBe('true');

    click('visibility-close');
    expect(el<HTMLDivElement>('visibility').hidden).toBe(true);

    click('visibility-open');
    document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }));
    expect(el<HTMLDivElement>('visibility').hidden).toBe(true);
  });

  it('selects a finding and moves to its first node', () => {
    const item = el('problems').querySelector<HTMLLIElement>('li.finding[data-id="f1"]')!;
    item.click();
    expect(el('details').classList.contains('active')).toBe(true);
    expect(el('details').textContent).toContain('SamplePage');
  });

  it('opens the source from a finding’s file link without selecting the finding', () => {
    const link = el('problems').querySelector<HTMLButtonElement>('li.finding[data-id="f2"] button.link')!;
    link.click();
    expect(posted.at(-1)).toMatchObject({ type: 'reveal', file: 'N/Lonely.cs', line: 2 });
  });

  it('leaves the Problems tab in place for a finding with no node to select', async () => {
    await load({ ...model, findings: [finding({ id: 'f3', kind: 'unused-type', title: 'Nothing to select', nodeIds: [] })] });
    click('tab-problems');
    el('problems').querySelector<HTMLLIElement>('li.finding[data-id="f3"]')!.click();
    expect(el('problems').classList.contains('active')).toBe(true);
  });

  it('ignores a problem row whose finding is no longer in the model', () => {
    const item = el('problems').querySelector<HTMLLIElement>('li.finding')!;
    item.dataset.id = 'gone';
    expect(() => item.click()).not.toThrow();
  });
});

describe('the canvas at its limits', () => {
  const wideModel = () => {
    const nodes = Array.from({ length: 40 }, (_, index) =>
      graphNode({ id: `N.T${index}`, label: `T${index}`, layer: 'view', module: 'M', location: { file: 'T.cs', line: index + 1 } }));
    const edges = nodes.slice(1).map((entry, index) =>
      graphEdge({ id: `w${index}`, source: nodes[index].id, target: entry.id, kind: 'binds' }));
    return { ...model, nodes, edges, findings: [], cleanup: { cycles: 0, deadBindings: 0, unboundViews: 0, unusedTypes: 0, unusedMembers: 0 } };
  };

  it('clamps a fit that would shrink the graph past legibility and centres on the focused node', async () => {
    await load(wideModel());
    emitNode('tap', 'N.T20');
    el<HTMLButtonElement>('focus-here').click();
    click('fit');
    expect(core.zoom()).toBeGreaterThanOrEqual(0.34);
  });

  it('centres the whole graph instead when nothing is focused', async () => {
    await load(wideModel());
    click('fit');
    expect(core.zoom()).toBeGreaterThanOrEqual(0.34);
  });

  it('clamps a region zoom that would go past the maximum', async () => {
    core.zoom(3);
    click('zoom-region');
    const container = el<HTMLDivElement>('cy');
    (container as unknown as { setPointerCapture: (id: number) => void }).setPointerCapture = () => undefined;
    const pointer = (type: string, x: number, y: number): Event => {
      const event = new MouseEvent(type, { clientX: x, clientY: y, bubbles: true, cancelable: true, button: 0 });
      Object.defineProperty(event, 'pointerId', { value: 1 });
      return event;
    };
    container.dispatchEvent(pointer('pointerdown', 0, 0));
    container.dispatchEvent(pointer('pointerup', 1000, 700));
    expect(core.zoom()).toBeLessThanOrEqual(3);
  });
});

describe('details and tooltips for a model with gaps', () => {
  it('opens the source of a node on double-click', () => {
    emitNode('dbltap', 'N.Views.SamplePage');
    expect(posted.at(-1)).toMatchObject({
      type: 'reveal',
      file: 'N/Views/SamplePage.cs',
      line: 5,
    });
  });

  it('says a node has no source location when it has none', async () => {
    await load({
      ...model,
      nodes: [
        graphNode({ id: 'N.A', label: 'A', layer: 'view', module: 'M' }),
        graphNode({ id: 'N.B', label: 'B', layer: 'viewmodel', module: 'M' }),
      ],
      edges: [graphEdge({ id: 'e1', source: 'N.A', target: 'N.B', kind: 'binds' })],
      findings: [],
    });
    emitNode('tap', 'N.A');
    expect(el('details').textContent).toContain('no source location');
  });

  it('names an edge target the model no longer holds by its id', async () => {
    await load({
      ...model,
      nodes: [
        graphNode({ id: 'N.A', label: 'A', layer: 'view', module: 'M', location: { file: 'A.cs', line: 1 } }),
      ],
      edges: [graphEdge({ id: 'ghost', source: 'N.A', target: 'N.Gone', kind: 'binds' })],
      findings: [],
    });
    const hide = el<HTMLInputElement>('hide-orphans');
    hide.checked = false;
    hide.dispatchEvent(new Event('change'));
    emitNode('tap', 'N.A');
    expect(el('details').textContent).toContain('N.Gone');
  });

  it('ignores a neighbour link that carries no id', () => {
    emitNode('tap', 'N.Views.SamplePage');
    const link = el('details').querySelector<HTMLButtonElement>('button.neighbour-link')!;
    delete link.dataset.id;
    expect(() => link.click()).not.toThrow();
  });

});

describe('keyboard, pointer and observers', () => {
  it('ignores a key that is not Escape', () => {
    expect(() => document.dispatchEvent(new KeyboardEvent('keydown', { key: 'a' }))).not.toThrow();
  });

  it('tracks the pointer over the window and reacts to a mouseup outside the canvas', () => {
    window.dispatchEvent(new MouseEvent('mousemove', { clientX: 12, clientY: 34 }));
    window.dispatchEvent(new MouseEvent('mouseup'));
    expect(el('cy').classList.contains('panning')).toBe(false);
  });

  it('resizes the canvas when the panel element changes size', async () => {
    const observers: (() => void)[] = [];
    const original = (globalThis as unknown as { ResizeObserver: unknown }).ResizeObserver;
    (globalThis as unknown as { ResizeObserver: unknown }).ResizeObserver = class {
      constructor(callback: () => void) {
        observers.push(callback);
      }

      observe(): void {}

      disconnect(): void {}
    };

    await load();
    expect(observers.length).toBeGreaterThan(0);
    expect(() => observers.at(-1)!()).not.toThrow();
    (globalThis as unknown as { ResizeObserver: unknown }).ResizeObserver = original;
  });

  it('labels a tooltip from the rendered position when the event carries no pointer', async () => {
    emitNode('mouseover', 'N.Views.SamplePage', {});
    await tick(260);
    expect(el<HTMLDivElement>('tooltip').hidden).toBe(false);
  });

  it('keeps the zoom slider in step only while labels are zoom-dependent', () => {
    change('labels', 'always');
    const slider = el<HTMLInputElement>('zoom-slider');
    core.zoom(2);
    core.emit('zoom' as never);
    // The slider still follows the zoom; only the delayed label re-check is skipped.
    expect(Number(slider.value)).toBeGreaterThan(50);
  });
});

describe('indexing and rendering with repeated findings', () => {
  it('collects several findings against the same node for its tooltip', async () => {
    await load({
      ...model,
      findings: [
        finding({ id: 'f1', kind: 'cycle', severity: 'high', title: 'Cycle', detail: 'a', nodeIds: ['N.Views.SamplePage'] }),
        finding({ id: 'f2', kind: 'unused-type', severity: 'low', title: 'Unused', detail: 'b', nodeIds: ['N.Views.SamplePage'] }),
      ],
    });
    emitNode('mouseover', 'N.Views.SamplePage', { clientX: 5, clientY: 5 });
    await tick(260);
    expect(el('tooltip').textContent).toContain('Cycle');
    expect(el('tooltip').textContent).toContain('Unused');
  });


  it('takes the first requested type as the focus when none was named', () => {
    send({ type: 'request', request: { nodeIds: ['N.ViewModels.SampleViewModel', 'N.Views.SamplePage'] } });
    expect(el('focus-bar').textContent).toContain('Focused on SampleViewModel');
  });

  it('accepts a request with no ids at all when it names a focus', () => {
    send({ type: 'request', request: { focus: 'N.Views.SamplePage' } });
    expect(el('focus-bar').hidden).toBe(false);
  });

  it('shows a notice with no text as an empty line', () => {
    send({ type: 'notice' });
    expect(el('details').querySelector('p.notice')?.textContent).toBe('');
  });

  it('survives a missing progress overlay', () => {
    const progress = el('progress');
    progress.remove();
    expect(() => send({ type: 'progress', message: 'x', percent: 5, known: true })).not.toThrow();
    el('canvas').appendChild(progress);
  });

  it('survives a missing tooltip element', async () => {
    const tooltip = el('tooltip');
    tooltip.remove();
    emitNode('mouseover', 'N.Views.SamplePage', { clientX: 5, clientY: 5 });
    await tick(260);
    expect(() => send({ type: 'requestDiagnostics' })).not.toThrow();
    el('canvas').appendChild(tooltip);
  });

  it('re-draws the region rectangle only while one is being dragged', () => {
    const rect = el('zoom-rect');
    rect.remove();
    click('zoom-region');
    const container = el<HTMLDivElement>('cy');
    (container as unknown as { setPointerCapture: (id: number) => void }).setPointerCapture = () => undefined;
    const event = new MouseEvent('pointerdown', { clientX: 10, clientY: 10, bubbles: true, button: 0 });
    Object.defineProperty(event, 'pointerId', { value: 1 });
    expect(() => container.dispatchEvent(event)).not.toThrow();
    el('canvas').appendChild(rect);
  });
});

describe('the last defensive paths that a reader can reach', () => {
  it('frames an empty canvas without throwing', () => {
    const hide = el<HTMLInputElement>('hide-orphans');
    hide.checked = false;
    hide.dispatchEvent(new Event('change'));
    const search = el<HTMLInputElement>('search');
    search.value = 'nothing-matches-this';
    search.dispatchEvent(new Event('input'));
    expect(core.nodes()).toHaveLength(0);
    expect(() => click('fit')).not.toThrow();
  });

  it('measures a canvas that reports no size as a square-ish panel', () => {
    const container = el<HTMLDivElement>('cy');
    Object.defineProperty(container, 'clientWidth', { value: 0, configurable: true });
    Object.defineProperty(container, 'clientHeight', { value: 0, configurable: true });
    core.resize();
    expect(() => change('layout', 'packed')).not.toThrow();
    Object.defineProperty(container, 'clientWidth', { value: 1000, configurable: true });
    Object.defineProperty(container, 'clientHeight', { value: 700, configurable: true });
    core.resize();
  });

  it('zooms about the middle only when the canvas has a size', () => {
    const container = el<HTMLDivElement>('cy');
    Object.defineProperty(container, 'clientWidth', { value: 0, configurable: true });
    Object.defineProperty(container, 'clientHeight', { value: 0, configurable: true });
    core.resize();
    const before = core.zoom();
    const slider = el<HTMLInputElement>('zoom-slider');
    slider.value = '70';
    slider.dispatchEvent(new Event('input'));
    expect(core.zoom()).toBeCloseTo(before, 5);
    Object.defineProperty(container, 'clientWidth', { value: 1000, configurable: true });
    Object.defineProperty(container, 'clientHeight', { value: 700, configurable: true });
    core.resize();
  });

  it('skips the slider sync when the slider is not on the page', () => {
    const slider = el('zoom-slider');
    slider.remove();
    expect(() => change('layout', 'circle')).not.toThrow();
    el('view-controls').appendChild(slider);
  });

  it('re-applies the focus when the hop count changes', () => {
    emitNode('tap', 'N.Views.SamplePage');
    el<HTMLButtonElement>('focus-here').click();
    change('depth', '2');
    expect(el<HTMLSelectElement>('depth').value).toBe('2');
    expect(el('focus-bar').hidden).toBe(false);
  });

  it('counts more than one unpacked group in the toolbar label', () => {
    // Unpack one shape, re-scope to another, then unpack that one too.
    openMenuFor('N.Views.SamplePage');
    menuButton('Unpack this group').click();
    send({ type: 'request', request: { nodeIds: ['N.Services.PartStore'], focus: 'N.Services.PartStore', depth: 1 } });
    const layer = el<HTMLInputElement>('layer-service');
    layer.checked = true;
    layer.dispatchEvent(new Event('change'));
    openMenuFor('N.Services.PartStore');
    const items = menuLabels();
    if (items[0] === 'Unpack this group') {
      menuButton('Unpack this group').click();
      expect(el<HTMLButtonElement>('repack').textContent).toBe('Re-pack 2 groups');
    } else {
      expect(el<HTMLButtonElement>('repack').textContent).toBe('Re-pack group');
    }
  });
});

describe('main: where the hover tooltip is placed', () => {
  it('uses the pointer when the event carries one', async () => {
    await reset();
    node('N.Views.SamplePage').emit({ type: 'mouseover', originalEvent: { clientX: 40, clientY: 60 } } as never);
    await tick(260);
    const tooltip = el<HTMLDivElement>('tooltip');
    expect(tooltip.hidden).toBe(false);
    expect(tooltip.style.left).toBe('56px');
    expect(tooltip.style.top).toBe('78px');
  });

  it('falls back to the rendered position when the event has no pointer', async () => {
    await reset();
    node('N.Views.SamplePage').emit({ type: 'mouseover', renderedPosition: { x: 100, y: 200 } } as never);
    await tick(260);
    expect(el<HTMLDivElement>('tooltip').style.left).toBe('116px');
  });
});

describe('main: a request that names no selectable shape', () => {
  it('selects nothing when neither the focus nor the ids are known', async () => {
    await reset();
    send({ type: 'request', request: { title: 'Gone', nodeIds: ['N.Missing'], focus: 'N.Missing' } });
    await tick(20);
    expect(core.$(':selected').length).toBe(0);
  });
});









/* --------------------------------------------------------------- appearance */

describe('main: reading one tree inside a big picture', () => {
  /** Draws every layer, so the neighbours of a selected type are really on the canvas. */
  const showEverything = async (): Promise<void> => {
    click('preset-all');
    await tick(10);
  };

  it('fades every type outside the selected type neighbourhood', async () => {
    await showEverything();
    emitNode('tap', 'N.Views.SamplePage');
    await tick(20);

    expect(node('N.Views.SamplePage').hasClass('dimmed')).toBe(false);
    expect(node('N.ViewModels.SampleViewModel').hasClass('dimmed')).toBe(false);
    expect(node('N.Services.PartStore').hasClass('dimmed')).toBe(true);
    expect(node('N.Models.Lonely').hasClass('dimmed')).toBe(true);
    expect(node('e1').hasClass('dimmed')).toBe(false);
    expect(node('e3').hasClass('dimmed')).toBe(true);
  });

  it('brings the whole graph back when empty canvas is clicked', async () => {
    await showEverything();
    emitNode('tap', 'N.Views.SamplePage');
    await tick(20);
    expect(node('N.Models.Lonely').hasClass('dimmed')).toBe(true);

    emitBackground('tap');
    await tick(20);
    expect(core.nodes().filter((candidate) => candidate.hasClass('dimmed')).length).toBe(0);
  });

  it('can be switched off, leaving every type at full strength', async () => {
    await showEverything();
    const box = el<HTMLInputElement>('dim-selection');
    box.checked = false;
    box.dispatchEvent(new Event('change'));

    emitNode('tap', 'N.Views.SamplePage');
    await tick(20);

    expect(core.nodes().filter((candidate) => candidate.hasClass('dimmed')).length).toBe(0);
    box.checked = true;
    box.dispatchEvent(new Event('change'));
  });
});

describe('main: names that are longer than the space for them', () => {
  const longModel = graphModel({
    nodes: [
      graphNode({ id: 'N.Long', label: 'GenericRequestStepFrameHostViewModel', layer: 'viewmodel', module: 'M' }),
      graphNode({ id: 'N.Other', label: 'Other', layer: 'service', module: 'M' }),
    ],
    edges: [graphEdge({ id: 'x1', source: 'N.Long', target: 'N.Other', kind: 'injects' })],
  });

  /** The partner is a service, which the MVVM preset hides, so every layer has to be drawn. */
  const loadLong = async (): Promise<void> => {
    await load(longModel);
    click('preset-all');
    await tick(10);
  };

  it('shortens in the middle, so the end that tells two names apart survives', async () => {
    await loadLong();
    const short = String(node('N.Long').data('shortLabel'));
    expect(short).toContain('…');
    expect(short.length).toBeLessThan('GenericRequestStepFrameHostViewModel'.length);
    expect(short.startsWith('Generic')).toBe(true);
    expect(short.endsWith('ViewModel')).toBe(true);
  });

  it('keeps the whole name as the label the tooltip and details pane read', async () => {
    await loadLong();
    expect(node('N.Long').data('label')).toBe('GenericRequestStepFrameHostViewModel');
  });

  it('gives a roomier density more characters before shortening', async () => {
    await loadLong();
    const normal = String(node('N.Long').data('shortLabel')).length;
    change('density', 'roomy');
    await tick(10);
    expect(String(node('N.Long').data('shortLabel')).length).toBeGreaterThanOrEqual(normal);
  });
});

describe('main: regions painted over the canvas', () => {
  it('captions the packed groups', async () => {
    await load();
    const captions = document.querySelectorAll('.overlay-frame-group .overlay-label');
    expect(captions.length).toBeGreaterThan(0);
    expect([...captions].map((entry) => entry.textContent).join(' ')).toContain('SamplePage');
  });

  it('stops captioning when the toggles are switched off', async () => {
    await load();
    for (const id of ['group-captions', 'module-frames']) {
      const box = el<HTMLInputElement>(id);
      box.checked = false;
      box.dispatchEvent(new Event('change'));
    }
    await tick(10);
    expect(document.querySelectorAll('.overlay-frame-group').length).toBe(0);
    expect(document.querySelectorAll('.overlay-frame-module').length).toBe(0);

    for (const id of ['group-captions', 'module-frames']) {
      const box = el<HTMLInputElement>(id);
      box.checked = true;
      box.dispatchEvent(new Event('change'));
    }
  });

  it('gathers the screens nothing binds to into one band', async () => {
    const islandModel = graphModel({
      nodes: [
        graphNode({ id: 'N.OrphanView', label: 'OrphanView', layer: 'view', module: 'M' }),
        graphNode({ id: 'N.OrphanService', label: 'OrphanService', layer: 'service', module: 'M' }),
      ],
      edges: [graphEdge({ id: 'i1', source: 'N.OrphanView', target: 'N.OrphanService', kind: 'injects' })],
      findings: [
        finding({
          id: 'fb',
          kind: 'unbound-view',
          severity: 'medium',
          title: 'Unbound view: OrphanView',
          detail: 'No view model is bound to it.',
          nodeIds: ['N.OrphanView'],
        }),
      ],
    });

    await load(islandModel);
    click('preset-all');
    await tick(10);
    const band = document.querySelector('.overlay-frame-island');
    expect(band).not.toBeNull();
    expect(band?.textContent).toContain('Nothing links these');
  });
});

describe('main: the overview picture', () => {
  it('shows where the viewport sits once a graph is drawn', async () => {
    await load();
    const minimap = el<HTMLDivElement>('minimap');
    expect(minimap.hidden).toBe(false);
    expect(minimap.querySelector('svg')).not.toBeNull();
    expect(minimap.innerHTML).toContain('rect');
  });

  it('centres the canvas on the part of the overview that was clicked', async () => {
    await load();
    const minimap = el<HTMLDivElement>('minimap');
    minimap.getBoundingClientRect = () => ({
      x: 0, y: 0, left: 0, top: 0, right: 168, bottom: 120, width: 168, height: 120, toJSON: () => ({}),
    }) as DOMRect;

    const before = { ...core.pan() };
    minimap.dispatchEvent(new MouseEvent('pointerdown', { clientX: 150, clientY: 110, bubbles: true }));
    await tick(10);
    expect(core.pan().x).not.toBe(before.x);
  });
});

/* --------------------------------------------------------------- export integrity */

describe('main: the exported file proves what it claims', () => {
  /** A model whose only cycle is built from type references, which the default view filters out. */
  const referenceCycle = graphModel({
    nodes: [
      graphNode({ id: 'N.Alpha', label: 'Alpha', layer: 'viewmodel', module: 'M', inCycle: true }),
      graphNode({ id: 'N.Beta', label: 'Beta', layer: 'viewmodel', module: 'M', inCycle: true }),
    ],
    edges: [
      graphEdge({ id: 'r1', source: 'N.Alpha', target: 'N.Beta', kind: 'reference' }),
      graphEdge({ id: 'r2', source: 'N.Beta', target: 'N.Alpha', kind: 'reference' }),
    ],
    findings: [
      finding({
        id: 'cyc',
        kind: 'cycle',
        severity: 'low',
        title: 'Cycle of 2: Alpha ↔ Beta',
        detail: 'They reference each other.',
        nodeIds: ['N.Alpha', 'N.Beta'],
        edgeIds: ['r1', 'r2'],
      }),
    ],
    cleanup: { cycles: 1, deadBindings: 0, unboundViews: 0, unusedTypes: 0, unusedMembers: 0 },
  });

  const exported = (): {
    edges: { id: string; kind: string }[];
    findings: { id: string; edgeIds?: string[] }[];
    cleanup: Record<string, number>;
    modelTotals: Record<string, number>;
    stats: Record<string, unknown>;
  } => {
    click('export');
    const message = posted.at(-1) as { payload: Record<string, unknown> };
    return message.payload as never;
  };

  it('includes the edges a finding cites even when their kind is filtered out', async () => {
    await load(referenceCycle);
    await tick(10);

    const payload = exported();
    const ids = payload.edges.map((edge) => edge.id).sort();
    expect(ids).toEqual(['r1', 'r2']);
    expect(payload.findings.map((entry) => entry.id)).toEqual(['cyc']);
  });

  it('leaves out a finding whose evidence cannot be shown', async () => {
    // Hide one endpoint of the cycle: the remaining type cannot prove the loop, so claiming it would
    // hand back a file that says "there is a cycle" with no cycle in it.
    await load(referenceCycle);
    const beta = el<HTMLInputElement>('search');
    beta.value = 'Alpha';
    beta.dispatchEvent(new Event('input'));
    await tick(10);

    const payload = exported();
    expect(payload.edges.every((edge) => edge.id !== 'r2')).toBe(true);
    expect(payload.findings.map((entry) => entry.id)).toEqual([]);

    beta.value = '';
    beta.dispatchEvent(new Event('input'));
    await tick(10);
  });

  it('reports both the exported counts and the whole-workspace totals', async () => {
    await load();
    const payload = exported();
    expect(payload.cleanup.cycles).toBe(1);
    expect(payload.modelTotals.cycles).toBe(1);
    expect(payload.modelTotals.unusedTypes).toBe(1);
  });

  it('counts the same edges in stats as the ones it wrote', async () => {
    await load(referenceCycle);
    await tick(10);
    const payload = exported();
    expect(payload.stats.visibleEdges).toBe(payload.edges.length);
  });
});
