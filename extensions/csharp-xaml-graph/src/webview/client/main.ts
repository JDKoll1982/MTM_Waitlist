import cytoscape, { type Core, type ElementDefinition, type LayoutOptions, type NodeSingular } from 'cytoscape';
import dagre from 'cytoscape-dagre';
import type { EdgeKind, Finding, GraphModel, NodeLayer } from '../../graph/model';
import { EDGE_KIND_LABEL, EDGE_KINDS, FINDING_LABEL, NODE_LAYERS } from '../../graph/model';
import { computeVisible, moduleSummary, type ViewOptions } from './view';
import { packedLayout } from './packing';

cytoscape.use(dagre);

declare function acquireVsCodeApi(): {
  postMessage(message: unknown): void;
  getState(): unknown;
  setState(state: unknown): void;
};

const vscode = acquireVsCodeApi();

type LayoutName = 'packed' | 'layered-lr' | 'layered-tb' | 'force' | 'radial' | 'grid' | 'circle';
type DensityName = 'compact' | 'normal' | 'roomy';
type LabelName = 'zoom' | 'always' | 'hover' | 'none';

interface Geometry {
  nodeSize: number;
  viewWidth: number;
  viewHeight: number;
  fontSize: number;
  nodeSep: number;
  rankSep: number;
  textMaxWidth: number;
}

const GEOMETRY: Record<DensityName, Geometry> = {
  compact: { nodeSize: 10, viewWidth: 20, viewHeight: 13, fontSize: 9, nodeSep: 10, rankSep: 55, textMaxWidth: 70 },
  normal: { nodeSize: 16, viewWidth: 30, viewHeight: 20, fontSize: 11, nodeSep: 20, rankSep: 95, textMaxWidth: 95 },
  roomy: { nodeSize: 24, viewWidth: 42, viewHeight: 26, fontSize: 12, nodeSep: 34, rankSep: 145, textMaxWidth: 125 },
};

const LAYER_COLOURS: Record<NodeLayer, string> = {
  view: '#4a9eff',
  viewmodel: '#2fbf71',
  service: '#f0a030',
  model: '#9b8cff',
  external: '#8a8a8a',
};

/** Zoom below which labels are suppressed in `zoom` mode, so a busy graph stays readable. */
const LABEL_ZOOM_THRESHOLD = 0.62;
/** Spacing multiplier for the layouts that space nodes using their rendered size. */
const DENSITY_SPACING: Record<DensityName, number> = { compact: 1.1, normal: 1.35, roomy: 1.8 };
/** Zoom the view falls back to when fitting a large graph would shrink it past legibility. */
const MIN_READABLE_ZOOM = 0.34;

const ui = {
  search: '',
  layers: new Set<NodeLayer>(['view', 'viewmodel']),
  edgeKinds: new Set<EdgeKind>(['resolves', 'getservice', 'binds', 'injects', 'field', 'constructs', 'composes']),
  // Type references are numerous (2,600+ here), so they stay off the canvas by default; cycle
  // detection always considers them, which is why the Problems tab can report cycles the canvas
  // does not draw.
  module: 'all',
  layout: 'packed' as LayoutName,
  density: 'normal' as DensityName,
  labels: 'zoom' as LabelName,
  hideOrphans: true,
  onlyProblems: false,
  focus: null as string | null,
  focusDepth: 1,
};

let model: GraphModel | undefined;
let cy: Core | undefined;
let labelsVisible = true;
let panning = false;
let lastPointer = { x: 0, y: 0 };
let zoomTimer: ReturnType<typeof setTimeout> | undefined;

const element = <T extends HTMLElement>(id: string): T => document.getElementById(id) as T;

const escapeHtml = (value: unknown): string => String(value)
  .replace(/&/g, '&amp;')
  .replace(/</g, '&lt;')
  .replace(/>/g, '&gt;')
  .replace(/"/g, '&quot;');

const options = (): ViewOptions => ({
  layers: [...ui.layers],
  edgeKinds: [...ui.edgeKinds],
  module: ui.module,
  search: ui.search,
  focus: ui.focus,
  focusDepth: ui.focusDepth,
  hideOrphans: ui.hideOrphans,
  onlyProblems: ui.onlyProblems,
});

/* ---------------------------------------------------------------- styling */

function buildStyle(): cytoscape.StylesheetStyle[] {
  const geometry = GEOMETRY[ui.density];
  const label = labelsVisible ? 'data(label)' : '';

  return [
    {
      selector: 'node',
      style: {
        label,
        'font-size': geometry.fontSize,
        color: '#e8e8e8',
        'text-valign': 'bottom',
        'text-halign': 'center',
        'text-margin-y': 3,
        'text-max-width': `${geometry.textMaxWidth}px`,
        'text-wrap': 'ellipsis',
        'background-color': (node) => LAYER_COLOURS[node.data('layer') as NodeLayer] ?? '#888888',
        width: geometry.nodeSize,
        height: geometry.nodeSize,
        'border-width': 1,
        'border-color': '#141414',
        'text-outline-color': '#0d0d0d',
        'text-outline-width': 2,
        'overlay-opacity': 0,
        'min-zoomed-font-size': 6,
      },
    },
    {
      selector: 'node.layer-view',
      style: {
        shape: 'round-rectangle',
        width: geometry.viewWidth,
        height: geometry.viewHeight,
        'border-width': 2,
      },
    },
    {
      selector: 'node.layer-viewmodel',
      style: { shape: 'round-rectangle' },
    },
    {
      selector: 'node.layer-external',
      style: {
        'background-opacity': 0.1,
        'border-style': 'dashed',
        'border-color': '#9a9a9a',
        color: '#b6b6b6',
      },
    },
    {
      selector: 'node[?diRegistered]',
      style: { 'border-width': 3, 'border-color': '#ffffff' },
    },
    {
      selector: 'node.flagged',
      style: { 'border-width': 2, 'border-color': '#f59e0b', 'border-style': 'dashed' },
    },
    {
      selector: 'node.in-cycle',
      style: { 'border-width': 3.5, 'border-color': '#ef4444', 'border-style': 'solid' },
    },
    {
      selector: 'node.focused',
      style: { 'border-width': 4, 'border-color': '#ffffff', 'overlay-opacity': 0.12, 'overlay-color': '#ffffff' },
    },
    {
      selector: 'node.hovered',
      style: { label: 'data(label)', 'text-opacity': 1 },
    },
    {
      selector: 'node.hover-source',
      style: { 'border-width': 4, 'border-color': '#ffffff' },
    },
    {
      selector: 'edge',
      style: {
        width: 1.4,
        'line-color': '#6b6b6b',
        'target-arrow-color': '#6b6b6b',
        'target-arrow-shape': 'triangle',
        'curve-style': 'bezier',
        'arrow-scale': 0.8,
        opacity: 0.7,
        'min-zoomed-font-size': 6,
      },
    },
    { selector: 'edge.edge-resolves', style: { 'line-color': '#2fbf71', 'target-arrow-color': '#2fbf71', width: 2.2 } },
    { selector: 'edge.edge-getservice', style: { 'line-color': '#4a9eff', 'target-arrow-color': '#4a9eff', width: 2.2 } },
    { selector: 'edge.edge-binds', style: { 'line-color': '#38bdf8', 'target-arrow-color': '#38bdf8', 'line-style': 'dashed', width: 1.8 } },
    { selector: 'edge.edge-injects', style: { 'line-color': '#f0a030', 'target-arrow-color': '#f0a030' } },
    { selector: 'edge.edge-field', style: { 'line-style': 'dashed', 'line-color': '#4a9eff', 'target-arrow-color': '#4a9eff' } },
    { selector: 'edge.edge-constructs', style: { 'line-color': '#c084fc', 'target-arrow-color': '#c084fc' } },
    { selector: 'edge.edge-composes', style: { 'line-style': 'dotted', 'line-color': '#c084fc', 'target-arrow-color': '#c084fc' } },
    { selector: 'edge.edge-implements', style: { 'line-style': 'dashed', 'line-color': '#8a8a8a', 'target-arrow-color': '#8a8a8a', 'target-arrow-shape': 'diamond' } },
    { selector: 'edge.edge-inherits', style: { 'line-color': '#8a8a8a', 'target-arrow-color': '#8a8a8a' } },
    { selector: 'edge.edge-template', style: { 'line-style': 'dotted', 'line-color': '#555555', opacity: 0.3 } },
    { selector: 'edge.neighbour', style: { opacity: 1, width: 2.6 } },
  ];
}

function refreshStyle(): void {
  cy?.style(buildStyle());
}

/** Keeps the label rule in step with the current zoom, layout mode and label choice. */
function syncLabels(): void {
  if (!cy) {
    return;
  }

  let shouldShow: boolean;
  if (ui.labels === 'always') {
    shouldShow = true;
  } else if (ui.labels === 'none' || ui.labels === 'hover') {
    shouldShow = false;
  } else {
    shouldShow = cy.zoom() >= LABEL_ZOOM_THRESHOLD;
  }

  if (shouldShow !== labelsVisible) {
    labelsVisible = shouldShow;
    refreshStyle();
  }
}

/* ---------------------------------------------------------------- framing */

function visibleElements(): cytoscape.CollectionReturnValue {
  return cy!.elements();
}

/**
 * Frames the graph without ever shrinking it into an unreadable smear.
 *
 * Fitting always runs first so a graph that legitimately fits fills the panel. The clamp only bites
 * when fitting would push the zoom below legibility, which is when the view pans instead of fit.
 */
function frame(focusId: string | null): void {
  if (!cy) {
    return;
  }

  const elements = visibleElements();
  if (elements.nodes().length === 0) {
    return;
  }

  cy.fit(elements, 60);

  if (cy.zoom() < MIN_READABLE_ZOOM) {
    cy.zoom(MIN_READABLE_ZOOM);
    const target = focusId ? cy.getElementById(focusId) : undefined;
    if (target && target.nonempty()) {
      cy.center(target);
    } else {
      cy.center(elements);
    }
  }

  syncLabels();
}

/* ---------------------------------------------------------------- layout */

function layoutFor(name: LayoutName): LayoutOptions {
  const geometry = GEOMETRY[ui.density];
  const common = { animate: false, fit: false, padding: 40 } as const;

  switch (name) {
    case 'layered-tb':
      return {
        name: 'dagre',
        rankDir: 'TB',
        nodeSep: geometry.nodeSep,
        rankSep: geometry.rankSep,
        edgeSep: 12,
        ...common,
      } as unknown as LayoutOptions;
    case 'force':
      return {
        name: 'cose',
        idealEdgeLength: 80,
        nodeRepulsion: 9000,
        nodeOverlap: 14,
        gravity: 60,
        numIter: 900,
        ...common,
      } as unknown as LayoutOptions;
    case 'radial':
      return {
        name: 'concentric',
        concentric: (node: cytoscape.NodeSingular) => {
          const layer = node.data('layer') as NodeLayer;
          return layer === 'view' ? 4 : layer === 'viewmodel' ? 3 : layer === 'service' ? 2 : 1;
        },
        levelWidth: () => 1,
        minNodeSpacing: geometry.nodeSep + 12,
        ...common,
      } as unknown as LayoutOptions;
    case 'grid':
      return {
        name: 'grid',
        avoidOverlap: true,
        condense: false,
        spacingFactor: DENSITY_SPACING[ui.density],
        ...common,
      } as unknown as LayoutOptions;
    case 'circle':
      return {
        name: 'circle',
        avoidOverlap: true,
        spacingFactor: DENSITY_SPACING[ui.density],
        ...common,
      } as unknown as LayoutOptions;
    case 'layered-lr':
    default:
      return {
        name: 'dagre',
        rankDir: 'LR',
        nodeSep: geometry.nodeSep,
        rankSep: geometry.rankSep,
        edgeSep: 12,
        ...common,
      } as unknown as LayoutOptions;
  }
}

function runLayout(): void {
  if (!cy) {
    return;
  }
  const elements = visibleElements();
  if (elements.nodes().length === 0) {
    return;
  }

  if (ui.layout === 'packed') {
    packedLayout(cy, { gap: GEOMETRY[ui.density].rankSep * 0.7, aspect: canvasAspect() });
  } else {
    cy.layout(layoutFor(ui.layout)).run();
  }

  frame(ui.focus);
  syncLabels();
}

/** Roughly the shape of the canvas, so packed components tile to the panel rather than to a square. */
function canvasAspect(): number {
  const width = cy?.width() ?? 0;
  const height = cy?.height() ?? 0;
  if (width <= 0 || height <= 0) {
    return 1.7;
  }
  return Math.min(3.2, Math.max(0.8, width / height));
}

/* ---------------------------------------------------------------- render */

function elementsFor(visible: { nodeIds: Set<string>; edges: { id: string; source: string; target: string; kind: EdgeKind; label?: string }[] }): ElementDefinition[] {
  const nodes: ElementDefinition[] = [];
  for (const node of model!.nodes) {
    if (!visible.nodeIds.has(node.id)) {
      continue;
    }
    nodes.push({
      group: 'nodes' as const,
      data: {
        id: node.id,
        label: node.label,
        fullName: node.fullName,
        layer: node.layer,
        module: node.module,
        kind: node.kind,
        file: node.location?.file ?? '',
        line: node.location?.line ?? 1,
        xamlFile: node.xamlFile ?? '',
        diRegistered: node.diRegistered === true,
        problemCount: node.problemCount ?? 0,
      },
      classes: [
        `layer-${node.layer}`,
        ...(node.inCycle ? ['in-cycle'] : []),
        ...(!node.inCycle && (node.problemCount ?? 0) > 0 ? ['flagged'] : []),
      ],
    });
  }

  const visibleEdges = new Set(visible.edges.map((edge) => edge.id));
  const edges: ElementDefinition[] = [];
  for (const edge of model!.edges) {
    if (!visibleEdges.has(edge.id)) {
      continue;
    }
    edges.push({
      group: 'edges' as const,
      data: {
        id: edge.id,
        source: edge.source,
        target: edge.target,
        kind: edge.kind,
        label: edge.label ? `${EDGE_KIND_LABEL[edge.kind]}: ${edge.label}` : EDGE_KIND_LABEL[edge.kind],
        confidence: edge.confidence,
        file: edge.evidence?.file ?? '',
        line: edge.evidence?.line ?? 1,
      },
      classes: [`edge-${edge.kind}`],
    });
  }

  return [...nodes, ...edges];
}

function applyView(relayout: boolean): void {
  if (!cy || !model) {
    return;
  }

  const visible = computeVisible(model, options());
  cy.elements().remove();
  cy.add(elementsFor(visible));
  refreshStyle();

  if (ui.focus) {
    const focused = cy.getElementById(ui.focus);
    if (focused.nonempty()) {
      focused.addClass('focused');
    }
  }

  updateCounters(visible.nodeIds.size, visible.edges.length, visible.hiddenOrphans);
  updateFocusBar();

  if (relayout) {
    runLayout();
  } else {
    frame(ui.focus);
  }
}

function updateCounters(nodeCount: number, edgeCount: number, hidden: number): void {
  if (!model) {
    return;
  }
  const parts = [
    `${nodeCount} of ${model.nodes.length} nodes`,
    `${edgeCount} of ${model.edges.length} edges`,
  ];
  if (hidden > 0) {
    parts.push(`${hidden} unconnected hidden`);
  }
  parts.push(`${model.stats.elapsedMs} ms`);
  if (model.stats.parseErrors) {
    parts.push(`${model.stats.parseErrors} parse errors`);
  }
  element('stats').textContent = parts.join(' · ');
}

function updateFocusBar(): void {
  const bar = element<HTMLDivElement>('focus-bar');
  if (!ui.focus) {
    bar.hidden = true;
    bar.textContent = '';
    return;
  }
  const node = model?.nodes.find((candidate) => candidate.id === ui.focus);
  bar.hidden = false;
  bar.innerHTML = `Focused on <strong>${escapeHtml(node?.label ?? ui.focus)}</strong>`;
  const clear = document.createElement('button');
  clear.type = 'button';
  clear.textContent = 'Show everything';
  clear.addEventListener('click', () => {
    ui.focus = null;
    applyView(true);
  });
  bar.appendChild(clear);
}

function mount(): void {
  if (cy) {
    cy.destroy();
  }

  cy = cytoscape({
    container: element<HTMLDivElement>('cy'),
    elements: [],
    style: buildStyle(),
    wheelSensitivity: 0.22,
    minZoom: 0.05,
    maxZoom: 3,
    boxSelectionEnabled: false,
    selectionType: 'single',
  });

  cy.on('tap', 'node', (event) => {
    const node = event.target as NodeSingular;
    showDetails(node);
  });

  cy.on('dbltap', 'node', (event) => {
    const node = event.target as NodeSingular;
    vscode.postMessage({
      type: 'reveal',
      file: node.data('file'),
      line: Number(node.data('line') ?? 1),
    });
  });

  cy.on('tap', (event) => {
    if (event.target === cy) {
      showDetails(undefined);
    }
  });

  cy.on('mouseover', 'node', (event) => {
    const node = event.target as NodeSingular;
    node.addClass('hovered');
    node.connectedEdges().addClass('neighbour');
    node.connectedEdges().connectedNodes().addClass('hover-source');
  });

  cy.on('mouseout', 'node', (event) => {
    const node = event.target as NodeSingular;
    node.removeClass('hovered');
    cy!.elements().removeClass('neighbour').removeClass('hover-source');
  });

  cy.on('zoom', () => {
    if (ui.labels !== 'zoom') {
      return;
    }
    if (zoomTimer) {
      clearTimeout(zoomTimer);
    }
    zoomTimer = setTimeout(() => {
      syncLabels();
    }, 90);
  });

  // A dropped pointer cancels the pan cursor if the mouseup happened outside the canvas.
  window.addEventListener('mouseup', () => {
    panning = false;
    element('cy').classList.remove('panning');
  });

  const observer = new ResizeObserver(() => {
    cy?.resize();
  });
  observer.observe(element<HTMLDivElement>('cy'));
}

/* ---------------------------------------------------------------- details */

function showDetails(node: NodeSingular | undefined): void {
  const details = element<HTMLDivElement>('details');
  if (!node || !model) {
    details.innerHTML = '<p class="muted">Select a node to inspect it. Double-click to open the source.</p>';
    return;
  }

  const data = node.data();
  const outgoing = model.edges.filter((edge) => edge.source === data.id);
  const incoming = model.edges.filter((edge) => edge.target === data.id);

  const renderList = (edges: GraphModel['edges'], direction: 'to' | 'from'): string => {
    if (edges.length === 0) {
      return '<p class="muted">none</p>';
    }
    return `<ul>${edges.map((edge) => {
      const otherId = direction === 'to' ? edge.target : edge.source;
      const other = model!.nodes.find((candidate) => candidate.id === otherId);
      const name = other ? other.label : otherId;
      const location = edge.evidence ? ` <span class="muted">@${edge.evidence.line}</span>` : '';
      return `<li><span class="kind">${escapeHtml(EDGE_KIND_LABEL[edge.kind])}</span> `
        + `${direction === 'to' ? '→' : '←'} `
        + `<button class="neighbour-link" data-id="${escapeHtml(otherId)}">${escapeHtml(name)}</button>${location}</li>`;
    }).join('')}</ul>`;
  };

  const location = data.file
    ? `<button class="link" data-file="${escapeHtml(data.file)}" data-line="${data.line}">${escapeHtml(data.file)}:${data.line}</button>`
    : '<span class="muted">no source location</span>';
  const xaml = data.xamlFile
    ? `<br /><span class="muted">markup:</span> <button class="link" data-file="${escapeHtml(data.xamlFile)}" data-line="1">${escapeHtml(data.xamlFile)}</button>`
    : '';

  details.innerHTML = `
    <h2>${escapeHtml(data.label)}</h2>
    <p class="muted">${escapeHtml(data.fullName)}</p>
    <p>
      <span class="chip layer-${escapeHtml(data.layer)}">${escapeHtml(data.layer)}</span>
      <span class="chip">${escapeHtml(data.kind)}</span>
      ${data.diRegistered ? '<span class="chip">DI registered</span>' : ''}
    </p>
    <p>${location}${xaml}</p>
    <p>
      <button id="focus-here" type="button" class="primary small">Focus here</button>
      <button id="expand-here" type="button" class="small">Expand 2 hops</button>
    </p>
    <h3>Depends on (${outgoing.length})</h3>
    ${renderList(outgoing, 'to')}
    <h3>Used by (${incoming.length})</h3>
    ${renderList(incoming, 'from')}
  `;

  details.querySelectorAll<HTMLButtonElement>('button.link').forEach((button) => {
    button.addEventListener('click', () => {
      vscode.postMessage({
        type: 'reveal',
        file: button.dataset.file,
        line: Number(button.dataset.line ?? 1),
      });
    });
  });

  details.querySelectorAll<HTMLButtonElement>('button.neighbour-link').forEach((button) => {
    button.addEventListener('click', () => {
      const id = button.dataset.id;
      if (!id) {
        return;
      }
      ui.focus = id;
      showSelected(id);
    });
  });

  details.querySelector<HTMLButtonElement>('#focus-here')?.addEventListener('click', () => {
    ui.focus = data.id as string;
    showSelected(ui.focus);
  });

  details.querySelector<HTMLButtonElement>('#expand-here')?.addEventListener('click', () => {
    ui.focus = data.id as string;
    ui.focusDepth = 2;
    (element<HTMLSelectElement>('depth')).value = '2';
    applyView(true);
    showSelected(ui.focus);
  });
}

/** Selects a node by id, applies the focus, and refreshes the details panel for it. */
function showSelected(id: string): void {
  applyView(true);
  if (!cy) {
    return;
  }
  const node = cy.getElementById(id);
  if (node.nonempty()) {
    cy.elements().unselect();
    node.select();
    showDetails(node as NodeSingular);
  }
}

/* ---------------------------------------------------------------- problems */

let activeTab: 'details' | 'problems' = 'details';

function setTab(tab: 'details' | 'problems'): void {
  activeTab = tab;
  element('tab-details').classList.toggle('active', tab === 'details');
  element('tab-problems').classList.toggle('active', tab === 'problems');
  element('details').classList.toggle('active', tab === 'details');
  element('problems').classList.toggle('active', tab === 'problems');
}

const SEVERITY_ORDER: Record<Finding['severity'], number> = { high: 0, medium: 1, low: 2 };

function renderProblems(): void {
  const container = element<HTMLDivElement>('problems');
  const badge = element<HTMLSpanElement>('problem-count');

  if (!model) {
    container.innerHTML = '<p class="muted">Run the analysis to see cleanup findings.</p>';
    badge.textContent = '0';
    return;
  }

  badge.textContent = String(model.findings.length);

  if (model.findings.length === 0) {
    container.innerHTML = '<p class="muted">No cleanup findings.</p>';
    return;
  }

  const summary = [
    `cycles ${model.cleanup.cycles}`,
    `dead bindings ${model.cleanup.deadBindings}`,
    `unbound views ${model.cleanup.unboundViews}`,
    `unused types ${model.cleanup.unusedTypes}`,
    `unused members ${model.cleanup.unusedMembers}`,
  ].join(' · ');

  const ordered = [...model.findings].sort((left, right) =>
    SEVERITY_ORDER[left.severity] - SEVERITY_ORDER[right.severity]);

  const rows = ordered.map((finding) => {
    const where = finding.location
      ? `<button class="link" data-file="${escapeHtml(finding.location.file)}" `
        + `data-line="${finding.location.line}">${escapeHtml(finding.location.file)}:${finding.location.line}</button>`
      : '';
    return `<li class="finding severity-${finding.severity}" data-id="${escapeHtml(finding.id)}">`
      + `<div class="finding-head">`
      + `<span class="chip danger-${finding.severity}">${escapeHtml(FINDING_LABEL[finding.kind])}</span>`
      + `<span class="finding-title">${escapeHtml(finding.title)}</span>`
      + `</div>`
      + `<p class="finding-detail">${escapeHtml(finding.detail)}</p>`
      + `<div class="finding-foot">${where}</div>`
      + `</li>`;
  }).join('');

  container.innerHTML = `<p class="muted summary">${escapeHtml(summary)}</p><ul class="finding-list">${rows}</ul>`;

  container.querySelectorAll<HTMLButtonElement>('button.link').forEach((button) => {
    button.addEventListener('click', (event) => {
      event.stopPropagation();
      vscode.postMessage({
        type: 'reveal',
        file: button.dataset.file,
        line: Number(button.dataset.line ?? 1),
      });
    });
  });

  container.querySelectorAll<HTMLLIElement>('li.finding').forEach((item) => {
    item.addEventListener('click', () => {
      const finding = model?.findings.find((candidate) => candidate.id === item.dataset.id);
      if (!finding) {
        return;
      }
      // Selecting a finding locates it in the graph and shows its details; opening the editor is the
      // job of the file link, so a click here never steals focus from the panel.
      const first = finding.nodeIds[0];
      if (first) {
        setTab('details');
        showSelected(first);
      }
    });
  });
}

/* ---------------------------------------------------------------- legend */

function setLegend(open: boolean): void {
  const legend = element<HTMLDivElement>('legend');
  legend.hidden = !open;
}

/* ---------------------------------------------------------------- visibility flyout */

const PRESETS: Record<string, { layers: NodeLayer[]; edges: EdgeKind[] }> = {
  mvvm: {
    layers: ['view', 'viewmodel'],
    edges: ['resolves', 'getservice', 'binds', 'injects', 'field', 'constructs', 'composes'],
  },
  structure: {
    layers: ['view', 'viewmodel', 'service', 'model'],
    edges: ['reference', 'inherits', 'implements'],
  },
  cleanup: {
    layers: ['view', 'viewmodel', 'service', 'model'],
    edges: ['reference', 'injects', 'getservice', 'resolves', 'binds', 'field', 'constructs', 'composes'],
  },
  all: {
    layers: [...NODE_LAYERS],
    edges: [...EDGE_KINDS],
  },
};

function setVisibilityOpen(open: boolean): void {
  element<HTMLDivElement>('visibility').hidden = !open;
  element<HTMLButtonElement>('visibility-open').setAttribute('aria-expanded', String(open));
}

/** Fills the per-layer and per-edge counts shown beside each checkbox. */
function updateVisibilityCounts(): void {
  if (!model) {
    return;
  }

  for (const layer of NODE_LAYERS) {
    const count = model.nodes.filter((node) => node.layer === layer).length;
    const target = document.getElementById(`count-layer-${layer}`);
    if (target) {
      target.textContent = String(count);
    }
  }

  for (const kind of EDGE_KINDS) {
    const count = model.edges.filter((edge) => edge.kind === kind).length;
    const target = document.getElementById(`count-edge-${kind}`);
    if (target) {
      target.textContent = String(count);
    }
  }
}

/** Pushes the current state back into the checkboxes, so a preset visibly ticks them. */
function syncVisibilityControls(): void {
  for (const layer of NODE_LAYERS) {
    const checkbox = document.getElementById(`layer-${layer}`) as HTMLInputElement | null;
    if (checkbox) {
      checkbox.checked = ui.layers.has(layer);
    }
  }
  for (const kind of EDGE_KINDS) {
    const checkbox = document.getElementById(`edge-${kind}`) as HTMLInputElement | null;
    if (checkbox) {
      checkbox.checked = ui.edgeKinds.has(kind);
    }
  }
}

function applyPreset(name: keyof typeof PRESETS): void {
  const preset = PRESETS[name];
  ui.layers = new Set(preset.layers);
  ui.edgeKinds = new Set(preset.edges);
  syncVisibilityControls();
  applyView(true);
}



/* ---------------------------------------------------------------- controls */

function populateModules(): void {
  const select = element<HTMLSelectElement>('module');
  const current = ui.module;
  select.innerHTML = '<option value="all">All modules</option>';
  if (!model) {
    return;
  }
  for (const entry of moduleSummary(model)) {
    const option = document.createElement('option');
    option.value = entry.name;
    option.textContent = `${entry.name} (${entry.count})`;
    select.appendChild(option);
  }
  select.value = current;
}

function toggleSet<T>(set: Set<T>, value: T, on: boolean): void {
  if (on) {
    set.add(value);
  } else {
    set.delete(value);
  }
}

function wireControls(): void {
  const search = element<HTMLInputElement>('search');
  search.addEventListener('input', () => {
    ui.search = search.value.trim();
    applyView(false);
  });
  search.addEventListener('keydown', (event) => {
    if (event.key === 'Escape') {
      search.value = '';
      ui.search = '';
      applyView(false);
    }
  });

  for (const layer of NODE_LAYERS) {
    const checkbox = document.getElementById(`layer-${layer}`) as HTMLInputElement | null;
    if (!checkbox) {
      continue;
    }
    checkbox.checked = ui.layers.has(layer);
    checkbox.addEventListener('change', () => {
      toggleSet(ui.layers, layer, checkbox.checked);
      applyView(false);
    });
  }

  for (const kind of EDGE_KINDS) {
    const checkbox = document.getElementById(`edge-${kind}`) as HTMLInputElement | null;
    if (!checkbox) {
      continue;
    }
    checkbox.checked = ui.edgeKinds.has(kind);
    checkbox.addEventListener('change', () => {
      toggleSet(ui.edgeKinds, kind, checkbox.checked);
      applyView(false);
    });
  }

  const moduleSelect = element<HTMLSelectElement>('module');
  moduleSelect.addEventListener('change', () => {
    ui.module = moduleSelect.value;
    ui.focus = null;
    applyView(true);
  });

  const layout = element<HTMLSelectElement>('layout');
  layout.value = ui.layout;
  layout.addEventListener('change', () => {
    ui.layout = layout.value as LayoutName;
    runLayout();
  });

  const density = element<HTMLSelectElement>('density');
  density.value = ui.density;
  density.addEventListener('change', () => {
    ui.density = density.value as DensityName;
    refreshStyle();
    runLayout();
  });

  const labels = element<HTMLSelectElement>('labels');
  labels.value = ui.labels;
  labels.addEventListener('change', () => {
    ui.labels = labels.value as LabelName;
    syncLabels();
  });

  const depth = element<HTMLSelectElement>('depth');
  depth.value = String(ui.focusDepth);
  depth.addEventListener('change', () => {
    ui.focusDepth = Number(depth.value);
    if (ui.focus) {
      applyView(true);
    }
  });

  const orphans = element<HTMLInputElement>('hide-orphans');
  orphans.checked = ui.hideOrphans;
  orphans.addEventListener('change', () => {
    ui.hideOrphans = orphans.checked;
    applyView(false);
  });

  const onlyProblems = element<HTMLInputElement>('only-problems');
  onlyProblems.checked = ui.onlyProblems;
  onlyProblems.addEventListener('change', () => {
    ui.onlyProblems = onlyProblems.checked;
    applyView(true);
  });

  element<HTMLButtonElement>('tab-details').addEventListener('click', () => setTab('details'));
  element<HTMLButtonElement>('tab-problems').addEventListener('click', () => setTab('problems'));
  element<HTMLButtonElement>('legend-open').addEventListener('click', () => setLegend(true));
  element<HTMLButtonElement>('legend-close').addEventListener('click', () => setLegend(false));

  element<HTMLButtonElement>('visibility-open').addEventListener('click', () => {
    setVisibilityOpen(element<HTMLDivElement>('visibility').hidden);
  });
  element<HTMLButtonElement>('visibility-close').addEventListener('click', () => setVisibilityOpen(false));

  element<HTMLButtonElement>('preset-mvvm').addEventListener('click', () => applyPreset('mvvm'));
  element<HTMLButtonElement>('preset-structure').addEventListener('click', () => applyPreset('structure'));
  element<HTMLButtonElement>('preset-cleanup').addEventListener('click', () => applyPreset('cleanup'));
  element<HTMLButtonElement>('preset-all').addEventListener('click', () => applyPreset('all'));

  // The flyout owns the layer and edge checkboxes; they live in markup, so the wiring happens here
  // once rather than per open.
  for (const layer of NODE_LAYERS) {
    const checkbox = document.getElementById(`layer-${layer}`) as HTMLInputElement | null;
    checkbox?.addEventListener('change', () => {
      toggleSet(ui.layers, layer, checkbox.checked);
      applyView(false);
    });
  }
  for (const kind of EDGE_KINDS) {
    const checkbox = document.getElementById(`edge-${kind}`) as HTMLInputElement | null;
    checkbox?.addEventListener('change', () => {
      toggleSet(ui.edgeKinds, kind, checkbox.checked);
      applyView(false);
    });
  }
  syncVisibilityControls();

  element<HTMLButtonElement>('fit').addEventListener('click', () => {
    frame(ui.focus);
  });

  element<HTMLButtonElement>('reanalyze').addEventListener('click', () => {
    vscode.postMessage({ type: 'reanalyze' });
  });

  element<HTMLButtonElement>('export').addEventListener('click', () => {
    vscode.postMessage({ type: 'export' });
  });

  const panelToggle = element<HTMLButtonElement>('toggle-panel');
  panelToggle.addEventListener('click', () => {
    const shell = element<HTMLDivElement>('main');
    const hidden = shell.classList.toggle('panel-hidden');
    panelToggle.textContent = hidden ? 'Show details' : 'Hide details';
    panelToggle.setAttribute('aria-pressed', String(hidden));
    setTimeout(() => cy?.resize(), 60);
  });

  document.addEventListener('keydown', (event) => {
    if (event.key !== 'Escape') {
      return;
    }
    const visibility = element<HTMLDivElement>('visibility');
    if (!visibility.hidden) {
      setVisibilityOpen(false);
      return;
    }
    const legend = element<HTMLDivElement>('legend');
    if (!legend.hidden) {
      setLegend(false);
      return;
    }
    if (ui.focus) {
      ui.focus = null;
      applyView(true);
    }
  });
}

/* ---------------------------------------------------------------- boot */

window.addEventListener('message', (event: MessageEvent) => {
  const message = event.data as { type?: string; model?: GraphModel; message?: string };
  if (message?.type === 'model' && message.model) {
    model = message.model;
    ui.focus = null;
    populateModules();
    mount();
    applyView(true);
    renderProblems();
  }
  if (message?.type === 'error') {
    element('details').innerHTML = `<p class="error">${escapeHtml(message.message ?? 'Analysis failed')}</p>`;
  }
});

wireControls();
setTab('details');
vscode.postMessage({ type: 'ready' });
