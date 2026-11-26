import cytoscape, { type Core, type ElementDefinition, type LayoutOptions, type NodeSingular } from 'cytoscape';
import dagre from 'cytoscape-dagre';
import type { EdgeKind, Finding, GraphModel, NodeLayer } from '../../graph/model';
import { EDGE_KIND_LABEL, EDGE_KINDS, FINDING_LABEL, NODE_LAYERS } from '../../graph/model';
import { computeVisible, moduleSummary, type ViewOptions } from './view';
import { componentGroups, groupRootFor, packedGroups, packedLayout, type GroupBox } from './packing';
import { renderEdgeTooltip, renderTooltip, summariseEdge, summariseNode } from './tooltip';
import { readTheme, type CanvasTheme } from './theme';
import { renderMinimap } from './minimap';
import { createOverlay, type Overlay, type OverlayFrame } from './overlay';
import { DEFAULT_GRAPH_SETTINGS, UNPACKED_LAYOUT_LABEL, toUnpackedLayout, type GraphSettings } from '../../graph/settings';

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

/**
 * Characters a label may keep before it is shortened in the middle.
 *
 * A name is cut from its middle rather than its end because the end is what distinguishes two types:
 * `SettingsPageBase` and `SettingsPageHost` share a useless prefix and differ in their last four
 * letters, so an end truncation makes them indistinguishable.
 */
const LABEL_LIMIT: Record<DensityName, number> = { compact: 16, normal: 24, roomy: 30 };

const LAYER_COLOURS: Record<NodeLayer, string> = {
  view: '#4a9eff',
  viewmodel: '#2fbf71',
  service: '#f0a030',
  model: '#9b8cff',
  external: '#8a8a8a',
};

/** Shortens a label in the middle, keeping both ends, so two similar names stay distinguishable. */
function shortenLabel(label: string, limit: number): string {
  if (label.length <= limit || limit < 5) {
    return label;
  }
  const head = Math.ceil((limit - 1) / 2);
  const tail = Math.floor((limit - 1) / 2);
  return `${label.slice(0, head)}…${label.slice(label.length - tail)}`;
}

/** Zoom below which labels are suppressed in `zoom` mode, so a busy graph stays readable. */
const LABEL_ZOOM_THRESHOLD = 0.62;
/** Spacing multiplier for the layouts that space nodes using their rendered size. */
const DENSITY_SPACING: Record<DensityName, number> = { compact: 1.1, normal: 1.35, roomy: 1.8 };
/** Zoom the view falls back to when fitting a large graph would shrink it past legibility. */
const MIN_READABLE_ZOOM = 0.34;

/**
 * The canvas, or nothing once the panel has been disposed.
 *
 * `cy` keeps pointing at a destroyed core after `mount()` replaces it, and the webview can be torn
 * down while a short timer is still pending — a zoom settling its labels, a hidden details panel
 * asking for a resize. Every deferred entry point asks this instead of `cy`, so late work is dropped
 * rather than run against a renderer that no longer exists.
 */
function liveCanvas(): cytoscape.Core | undefined {
  return cy && !cy.destroyed() ? cy : undefined;
}

const ui = {
  search: '',
  // A view pair alone is an island: every view model injects the services it works through, so
  // hiding services leaves each view and its view model floating with nothing attached. Services are
  // therefore part of the default tree; models stay off because they are mostly row types.
  layers: new Set<NodeLayer>(['view', 'viewmodel', 'service']),
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
  /** Fade everything outside the selected type's neighbourhood, so the local tree reads on its own. */
  dimSelection: true,
  /** Caption each packed group with its root type and size. */
  groupCaptions: true,
  /** Outline each module's region, where the region is coherent enough to be worth drawing. */
  moduleFrames: true,
  /** Collect groups holding an unlinked view into one band below the tiles. */
  islandTray: true,
  focus: null as string | null,
  focusDepth: 1,
  /** Group roots the reader unpacked by right-clicking them. */
  unpacked: new Set<string>(),
  /** Layout used inside an unpacked shape; a setting, defaulting to Layered Down. */
  unpackedLayout: DEFAULT_GRAPH_SETTINGS.unpackedLayout,
  /** Node ids a symbol search asked for, or null for the whole workspace. */
  scope: null as string[] | null,
  /** Draw the scope exactly, with no hop expansion (unpacking a shape uses this). */
  scopeExact: false,
  /** Member name to highlight on the graph, set when a search started from a method or field. */
  member: null as string | null,
  /** Human-readable origin of the current scope, shown in the focus bar. */
  scopeLabel: '',
};

let model: GraphModel | undefined;
let cy: Core | undefined;
let labelsVisible = true;
/** True once the reader picks a density, so the automatic size stops overriding them. */
let densityChosen = false;
let panning = false;
let lastPointer = { x: 0, y: 0 };
let zoomTimer: ReturnType<typeof setTimeout> | undefined;
let tooltipTimer: ReturnType<typeof setTimeout> | undefined;

/** Findings that point at each node, so a hover can say what is wrong with the type under it. */
let findingsByNode = new Map<string, Finding[]>();

/** The canvas palette, re-read whenever the editor theme changes. */
let theme: CanvasTheme = readTheme();

/** The DOM layer that paints group, island and module frames over the canvas. */
let overlay: Overlay | undefined;

/** The id of the type the reader last selected, which is what the dimming is centred on. */
let selectedId: string | null = null;

/** Rectangle of the island band from the last packed run, so its caption lands on the band. */
let islandBox: GroupBox | null = null;

/** Node ids collected into the island band, kept so the caption can count them. */
let islandIds: string[] = [];

/** The captions and region frames, in model coordinates; only re-projected on a pan or a zoom. */
let overlayFrames: OverlayFrame[] = [];

/**
 * Last known position of every node.
 *
 * `cy.elements().remove()` then `cy.add()` destroys positions, and re-assigning them afterwards is
 * not dependable while a layout may still be settling. Holding the coordinates here lets each new
 * element be *created* at the right place, so a filter change — Hide unconnected, a layer toggle, a
 * search keystroke — never collapses the graph into one spot.
 */
const lastPositions = new Map<string, { x: number; y: number }>();

/**
 * A short event log for the debug report. The canvas is the one place where "it worked a minute ago"
 * cannot be reconstructed from the model, so the last few actions are recorded as they happen.
 */
const eventLog: { at: string; kind: string; detail: string }[] = [];

function logEvent(kind: string, detail: string): void {
  eventLog.push({ at: new Date().toISOString(), kind, detail });
  if (eventLog.length > 40) {
    eventLog.shift();
  }
}

/** What the debug report asks for: the filters in force and what is actually drawn. */
function diagnosticsFacts(): Record<string, unknown> {
  const visible = model ? computeVisible(model, options()) : undefined;
  return {
    ready: Boolean(model && cy),
    modelLoadedAt: modelLoadedAt ?? 'not loaded',
    nodesDrawn: visible?.nodeIds.size ?? 0,
    edgesDrawn: visible?.edges.length ?? 0,
    hiddenOrphans: visible?.hiddenOrphans ?? 0,
    layout: ui.layout,
    density: ui.density,
    labels: ui.labels,
    zoom: cy?.zoom() ?? 0,
    canvasWidth: cy?.width() ?? 0,
    canvasHeight: cy?.height() ?? 0,
    module: ui.module,
    search: ui.search,
    layers: [...ui.layers],
    edgeKinds: [...ui.edgeKinds],
    focus: ui.focus,
    focusDepth: ui.focusDepth,
    scope: ui.scope,
    scopeExact: ui.scopeExact,
    unpacked: [...ui.unpacked],
    selectedId,
    islandNodes: islandIds.length,
    dimSelection: ui.dimSelection,
    groupCaptions: ui.groupCaptions,
    moduleFrames: ui.moduleFrames,
    islandTray: ui.islandTray,
    themeLight: theme.light,
    history: [...eventLog].reverse().slice(0, 12),
  };
}

let modelLoadedAt: string | undefined;


function capturePositions(): void {
  cy?.nodes().forEach((node) => {
    const position = node.position();
    if (Number.isFinite(position.x) && Number.isFinite(position.y)) {
      lastPositions.set(node.id(), { x: position.x, y: position.y });
    }
  });
}

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
  scope: ui.scope,
  scopeExact: ui.scopeExact,
});

/* ---------------------------------------------------------------- styling */

function buildStyle(): cytoscape.StylesheetStyle[] {
  const geometry = GEOMETRY[ui.density];
  const label = labelsVisible
    ? (node: cytoscape.NodeSingular) => String(node.data('shortLabel') ?? node.data('label') ?? '')
    : '';
  // Roomy density has the space to break a long identifier across lines, so nothing is lost; the
  // tighter densities rely on the middle-shortened label instead.
  const wraps = ui.density === 'roomy';

  return [
    {
      selector: 'node',
      style: {
        label,
        'font-size': geometry.fontSize,
        color: theme.label,
        'text-valign': 'bottom',
        'text-halign': 'center',
        'text-margin-y': 3,
        'text-max-width': `${geometry.textMaxWidth}px`,
        'text-wrap': wraps ? 'wrap' : 'ellipsis',
        'text-overflow-wrap': wraps ? 'anywhere' : 'whitespace',
        'background-color': (node) => LAYER_COLOURS[node.data('layer') as NodeLayer] ?? '#888888',
        width: geometry.nodeSize,
        height: geometry.nodeSize,
        'border-width': 1,
        'border-color': theme.nodeBorder,
        'text-outline-color': theme.labelOutline,
        'text-outline-width': 2,
        'overlay-opacity': 0,
        'min-zoomed-font-size': 6,
      },
    },
    // Each layer gets its own silhouette, so the layer survives a low zoom where the label is not
    // drawn and survives colour blindness, where five hues are not five categories.
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
      selector: 'node.layer-service',
      style: { shape: 'hexagon' },
    },
    {
      selector: 'node.layer-model',
      style: { shape: 'ellipse' },
    },
    {
      selector: 'node.layer-external',
      style: {
        shape: 'ellipse',
        'background-opacity': 0.1,
        'border-style': 'dashed',
        'border-color': theme.edgeMuted,
      },
    },
    {
      selector: 'node[?diRegistered]',
      style: { 'border-width': 3, 'border-color': theme.highlight },
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
      // A hub is a type almost everything touches. Marking it explains why the layout pulled it out
      // of the tiles and put it between the groups it serves.
      selector: 'node.hub',
      style: { 'border-width': 3, 'border-color': theme.edgeMuted, 'border-style': 'double' },
    },
    {
      selector: 'node.focused',
      style: { 'border-width': 4, 'border-color': theme.highlight, 'overlay-opacity': 0.12, 'overlay-color': theme.highlight },
    },
    {
      selector: 'node.hovered',
      style: { label: 'data(label)', 'text-opacity': 1 },
    },
    {
      selector: 'node.hover-source',
      style: { 'border-width': 4, 'border-color': theme.highlight },
    },
    {
      // Dimming is what makes one tree legible inside a picture of nine hundred nodes: the rest of
      // the graph stays as context instead of competing for the same attention.
      selector: 'node.dimmed',
      style: { opacity: 0.16 },
    },
    {
      selector: 'edge',
      style: {
        width: 1.4,
        'line-color': theme.edge,
        'target-arrow-color': theme.edge,
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
    { selector: 'edge.edge-implements', style: { 'line-style': 'dashed', 'line-color': theme.edgeMuted, 'target-arrow-color': theme.edgeMuted, 'target-arrow-shape': 'diamond' } },
    { selector: 'edge.edge-inherits', style: { 'line-color': theme.edgeMuted, 'target-arrow-color': theme.edgeMuted } },
    { selector: 'edge.edge-template', style: { 'line-style': 'dotted', 'line-color': theme.edgeMuted, opacity: 0.3 } },
    { selector: 'edge.neighbour', style: { opacity: 1, width: 2.6 } },
    {
      selector: 'edge.hovered-edge',
      style: { opacity: 1, width: 3.4, 'line-color': theme.highlight, 'target-arrow-color': theme.highlight, 'z-index': 20 },
    },
    { selector: 'edge.member-match', style: { opacity: 1, width: 3.2, 'line-color': theme.highlight, 'target-arrow-color': theme.highlight } },
    { selector: 'edge.dimmed', style: { opacity: 0.06 } },
    { selector: 'node.unpacked-group', style: { 'underlay-color': theme.underlay, 'underlay-opacity': 0.07, 'underlay-padding': 26 } },
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

/** Re-reads the palette and repaints: what an editor theme switch needs. */
function applyTheme(): void {
  theme = readTheme();
  refreshStyle();
  // A theme switch can arrive while the workspace is still being analysed, so this runs through the
  // same no-canvas guards as a paint from any other source.
  paintOverlay();
  refreshOverlayFrames();
  paintMinimap();
}

/**
 * Watches the body for a theme change.
 *
 * VS Code signals a theme switch by rewriting the body class (`vscode-light`, `vscode-dark`,
 * `vscode-high-contrast`) and the `--vscode-*` custom properties, not by sending a message, so the
 * attribute is the only reliable signal.
 */
function watchTheme(): void {
  const observer = new MutationObserver(() => applyTheme());
  observer.observe(document.body, {
    attributes: true,
    attributeFilter: ['class', 'data-vscode-theme-kind'],
  });
}

/* ---------------------------------------------------------------- dimming */

/**
 * Fades everything outside the selected type's immediate neighbourhood.
 *
 * In a picture of nine hundred nodes every line competes equally, so the one tree the reader picked
 * cannot be seen. Dimming rather than hiding keeps the rest as context: the shape of the whole graph
 * survives, and the local tree is the only thing at full strength.
 */
function applyDimming(): void {
  if (!cy) {
    return;
  }
  cy.elements().removeClass('dimmed');
  if (!ui.dimSelection) {
    return;
  }

  const anchor = selectedId ?? ui.focus;
  if (!anchor) {
    return;
  }
  const node = cy.getElementById(anchor);
  if (node.empty()) {
    return;
  }

  const keep = new Set<string>([anchor]);
  (node as NodeSingular).connectedEdges().connectedNodes().forEach((neighbour) => {
    keep.add(neighbour.id());
  });

  cy.nodes().forEach((candidate) => {
    if (!keep.has(candidate.id())) {
      candidate.addClass('dimmed');
    }
  });
  cy.edges().forEach((edge) => {
    if (!keep.has(edge.source().id()) || !keep.has(edge.target().id())) {
      edge.addClass('dimmed');
    }
  });
}

/* ---------------------------------------------------------------- overlay */

/** Model coordinates to the transform the overlay needs to place a frame. */

/** The type a group starts at, without its namespace. */
function groupCaption(root: string): string {
  const node = model?.nodes.find((candidate) => candidate.id === root);
  return node?.displayLabel ?? node?.label ?? root.split('.').pop() ?? root;
}

/** Fraction of the smaller of two boxes that the two share. */
function overlapRatio(left: GroupBox, right: GroupBox): number {
  const width = Math.min(left.x + left.width, right.x + right.width) - Math.max(left.x, right.x);
  const height = Math.min(left.y + left.height, right.y + right.height) - Math.max(left.y, right.y);
  if (width <= 0 || height <= 0) {
    return 0;
  }
  const smaller = Math.min(left.width * left.height, right.width * right.height);
  return smaller <= 0 ? 0 : (width * height) / smaller;
}

/**
 * Module boundaries, but only the honest ones.
 *
 * Packed tiling does not respect modules, so one module's types can be spread across the whole
 * picture. Drawing a box around them would then claim territory another module also sits in, which is
 * worse than drawing nothing: a frame is kept only when it does not share most of its area with
 * another module's frame.
 */
function moduleFrames(core: Core): OverlayFrame[] {
  const boxes = new Map<string, GroupBox & { nodes: number }>();
  core.nodes().forEach((node) => {
    const name = String(node.data('module') ?? '');
    if (!name) {
      return;
    }
    const position = node.position();
    const half = 16;
    const existing = boxes.get(name);
    if (existing) {
      const minX = Math.min(existing.x, position.x - half);
      const minY = Math.min(existing.y, position.y - half);
      existing.width = Math.max(existing.x + existing.width, position.x + half) - minX;
      existing.height = Math.max(existing.y + existing.height, position.y + half) - minY;
      existing.x = minX;
      existing.y = minY;
      existing.nodes += 1;
    } else {
      boxes.set(name, {
        x: position.x - half,
        y: position.y - half,
        width: half * 2,
        height: half * 2,
        nodes: 1,
      });
    }
  });

  // A module of one or two types is a label on a dot, not a region.
  const candidates = [...boxes].filter(([, box]) => box.nodes >= 3);
  const frames: OverlayFrame[] = [];
  for (const [name, box] of candidates) {
    const contested = candidates.some(([other, otherBox]) =>
      other !== name && overlapRatio(box, otherBox) > 0.5);
    if (!contested && frames.length < 12) {
      frames.push({
        id: `module:${name}`,
        label: name,
        x: box.x,
        y: box.y,
        width: box.width,
        height: box.height,
        tone: 'module',
      });
    }
  }
  return frames;
}

/** Paints the captions and region frames for the current viewport. */
function paintOverlay(): void {
  const core = liveCanvas();
  if (!overlay || !core) {
    overlay?.clear();
    return;
  }
  const pan = core.pan();
  overlay.paint(overlayFrames, { zoom: core.zoom(), panX: pan.x, panY: pan.y }, {
    width: core.width(),
    height: core.height(),
  });
}

/**
 * Recomputes the frames from the graph.
 *
 * Frames live in model coordinates, so a pan or a zoom only has to re-project them. Recomputing the
 * connected pieces on every frame of a drag would do real work sixty times a second for a picture
 * that has not changed, which is why this runs on a layout or a filter change instead.
 */
function refreshOverlayFrames(): void {
  const core = liveCanvas();
  if (!core || !model) {
    overlayFrames = [];
    overlay?.clear();
    return;
  }

  const frames: OverlayFrame[] = [];
  const groups = packedGroups(core);

  if (ui.moduleFrames && ui.module === 'all') {
    frames.push(...moduleFrames(core));
  }
  if (ui.groupCaptions) {
    // Past a few dozen captions the picture becomes a wall of text, so only the groups big enough to
    // be worth naming keep a label.
    const minimum = groups.length > 40 ? 4 : 2;
    for (const group of groups.filter((entry) => entry.nodeIds.length >= minimum).slice(0, 80)) {
      frames.push({
        id: `group:${group.root}`,
        label: `${groupCaption(group.root)} · ${group.nodeIds.length}`,
        ...group.box,
        tone: 'group',
      });
    }
  }
  if (ui.islandTray && islandBox && islandIds.length > 0) {
    frames.push({
      id: 'island',
      label: `Nothing links these to a view model · ${islandIds.length}`,
      ...islandBox,
      tone: 'island',
    });
  }

  overlayFrames = frames;
  paintOverlay();
}

/* ---------------------------------------------------------------- minimap */

/** Size of the overview box; the height follows the graph's own aspect so the mapping stays linear. */
const MINIMAP_WIDTH = 168;

/** The markup currently in the overview, so an unchanged picture is not rewritten. */
let lastMinimapMarkup = '';

/** Repaints the overview picture: dots for every type, a box for what is on screen. */
function paintMinimap(): void {
  const host = document.getElementById('minimap');
  if (!host) {
    return;
  }
  const core = liveCanvas();
  if (!core || core.nodes().length === 0 || core.width() <= 0 || core.height() <= 0) {
    host.hidden = true;
    host.innerHTML = '';
    return;
  }

  const bounds = core.elements().boundingBox();
  const spanX = Math.max(1, bounds.x2 - bounds.x1);
  const spanY = Math.max(1, bounds.y2 - bounds.y1);
  // Matching the box to the graph's aspect means no letterboxing, so a click maps linearly.
  const height = Math.max(64, Math.min(240, Math.round(MINIMAP_WIDTH / (spanX / spanY))));
  const viewport = core.extent();

  const markup = renderMinimap({
    nodes: core.nodes().map((node) => ({
      x: node.position().x,
      y: node.position().y,
      layer: String(node.data('layer') ?? ''),
    })),
    bounds: { minX: bounds.x1, minY: bounds.y1, maxX: bounds.x2, maxY: bounds.y2 },
    viewport: { x1: viewport.x1, y1: viewport.y1, x2: viewport.x2, y2: viewport.y2 },
    width: MINIMAP_WIDTH,
    height,
    colours: LAYER_COLOURS,
    background: theme.background,
  });

  host.hidden = !markup;
  // A pan changes only the viewport rectangle, so most frames produce identical markup; rewriting
  // the same SVG several times a second is pure DOM churn.
  if (markup !== lastMinimapMarkup) {
    host.innerHTML = markup;
    lastMinimapMarkup = markup;
  }
  host.style.width = `${MINIMAP_WIDTH}px`;
  host.style.height = `${height}px`;
}

/** Centres the canvas on a point given as a fraction of the overview box. */
function centreOnMinimapFraction(fx: number, fy: number): void {
  const core = liveCanvas();
  if (!core) {
    return;
  }
  const bounds = core.elements().boundingBox();
  const modelX = bounds.x1 + Math.min(1, Math.max(0, fx)) * (bounds.x2 - bounds.x1);
  const modelY = bounds.y1 + Math.min(1, Math.max(0, fy)) * (bounds.y2 - bounds.y1);
  const zoom = core.zoom();
  core.pan({ x: core.width() / 2 - modelX * zoom, y: core.height() / 2 - modelY * zoom });
  paintOverlay();
  paintMinimap();
  syncZoomSlider();
}

/** The overlay follows every pan frame; the overview is cheaper to refresh a little less often. */
let viewportFrame: number | null = null;
let minimapTimer: ReturnType<typeof setTimeout> | undefined;

function scheduleViewportPaint(): void {
  if (viewportFrame === null) {
    const run = (): void => {
      viewportFrame = null;
      if (liveCanvas()) {
        paintOverlay();
      }
    };
    viewportFrame = typeof requestAnimationFrame === 'function'
      ? requestAnimationFrame(run)
      : (setTimeout(run, 16) as unknown as number);
  }
  if (minimapTimer === undefined) {
    minimapTimer = setTimeout(() => {
      minimapTimer = undefined;
      if (liveCanvas()) {
        paintMinimap();
      }
    }, 120);
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
  applyDimming();
  refreshOverlayFrames();
  paintMinimap();
}

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

/**
 * Group roots that hold a view nothing binds to.
 *
 * These are the screens with no view model: genuinely orphaned, and the reason the picture looks like
 * it has holes. Collecting each such group into one band is what turns "where are the unlinked
 * screens" into a single place to look.
 */
function islandRoots(core: Core): Set<string> {
  const roots = new Set<string>();
  if (!ui.islandTray || !model) {
    return roots;
  }

  const unlinked = new Set<string>();
  for (const finding of model.findings) {
    if (finding.kind === 'unbound-view') {
      for (const id of finding.nodeIds) {
        unlinked.add(id);
      }
    }
  }
  if (unlinked.size === 0) {
    return roots;
  }

  for (const group of componentGroups(core)) {
    if (group.nodeIds.some((id) => unlinked.has(id))) {
      roots.add(group.root);
    }
  }
  return roots;
}

function runLayout(): void {
  const core = liveCanvas();
  if (!core) {
    return;
  }
  const elements = core.elements();
  if (elements.nodes().length === 0) {
    return;
  }

  if (ui.layout === 'packed') {
    const geometry = GEOMETRY[ui.density];
    const islands = islandRoots(core);
    const result = packedLayout(core, {
      gap: geometry.rankSep * 0.7,
      aspect: canvasAspect(),
      nodeSep: geometry.nodeSep,
      rankSep: geometry.rankSep,
      unpacked: ui.unpacked,
      unpackedLayout: ui.unpackedLayout,
      islands,
    });
    islandBox = result.island;
    islandIds = islands.size === 0
      ? []
      : result.groups.filter((group) => islands.has(group.root)).flatMap((group) => group.nodeIds);
    markUnpackedGroups();
  } else {
    core.nodes().removeClass('unpacked-group').removeClass('hub');
    islandBox = null;
    islandIds = [];
    core.layout(layoutFor(ui.layout)).run();
  }

  frame(ui.focus);
  syncLabels();
  syncToolbarState();
  syncZoomSlider();
  capturePositions();
}

/**
 * The toolbar has to say what mode it is in. The Re-pack button appearing is the reader's only
 * affordance to undo an unpack, and the status line names the layout the band was drawn in.
 */
function syncToolbarState(): void {
  const repack = document.getElementById('repack') as HTMLButtonElement | null;
  if (repack) {
    repack.hidden = ui.unpacked.size === 0;
    repack.textContent = ui.unpacked.size > 1
      ? `Re-pack ${ui.unpacked.size} groups`
      : 'Re-pack group';
  }

  const status = document.getElementById('unpack-status');
  if (status) {
    const count = ui.unpacked.size;
    status.hidden = count === 0;
    status.textContent = count === 0
      ? ''
      : `${count} unpacked · ${UNPACKED_LAYOUT_LABEL[ui.unpackedLayout]}`;
  }
}

/** A soft background behind the nodes of every unpacked group, so the band reads as one shape. */
function markUnpackedGroups(): void {
  if (!cy) {
    return;
  }
  cy.nodes().removeClass('unpacked-group');
  if (ui.layout !== 'packed' || ui.unpacked.size === 0) {
    return;
  }
  for (const group of componentGroups(cy)) {
    if (!ui.unpacked.has(group.root)) {
      continue;
    }
    for (const id of group.nodeIds) {
      cy.getElementById(id).addClass('unpacked-group');
    }
  }
}

/**
 * Unpacks the group the right-clicked node belongs to and shows that group alone, drawn in the
 * configured unpacked layout from its root to the end of the chain. Partners outside the group are
 * deliberately left out: the reader asked to see this shape, not the workspace around it.
 */
function unpackGroup(node: NodeSingular): void {
  if (!cy) {
    return;
  }
  const group = componentGroups(cy).find((entry) => entry.nodeIds.includes(node.id()));
  if (!group || group.nodeIds.length < 2) {
    void vscode.postMessage({
      type: 'notice',
      message: 'That type has no dependencies in this view, so there is no shape to unpack.',
    });
    return;
  }

  ui.unpacked.add(group.root);
  ui.scope = [...group.nodeIds];
  ui.scopeExact = true;
  ui.focus = null;
  ui.scopeLabel = `${String(node.data('label'))} · ${group.nodeIds.length} types`;
  logEvent('unpack', `root ${group.root.split('.').pop()} · ${group.nodeIds.length} types`);

  applyView(true);
  selectNode(String(node.id()));
}

/** Puts one shape back into the tiles and restores the whole graph. */
function repackGroup(node: NodeSingular): void {
  if (!cy) {
    return;
  }
  ui.unpacked.delete(groupRootFor(cy, node.id()));
  ui.scope = null;
  ui.scopeExact = false;
  ui.scopeLabel = '';
  logEvent('repack', 'one group');
  applyView(true);
}

/** Puts every unpacked shape back and returns to the whole graph. */
function repackAll(): void {
  ui.unpacked.clear();
  ui.scope = null;
  ui.scopeExact = false;
  ui.scopeLabel = '';
  logEvent('repack', 'all groups');
  applyView(true);
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
        // Middle-shortened for the canvas; the full name is what the tooltip and the details pane use.
        // A name two types share is qualified in `displayLabel` so the picture can tell them apart.
        shortLabel: shortenLabel(node.displayLabel ?? node.label, LABEL_LIMIT[ui.density]),
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
      // Born in place: re-adding an element without this drops it at the origin.
      position: lastPositions.get(node.id),
    });
  }

  const visibleEdges = new Set(visible.edges.map((edge) => edge.id));
  const edges: ElementDefinition[] = [];
  for (const edge of model!.edges) {
    if (!visibleEdges.has(edge.id)) {
      continue;
    }
    // Defensive: cytoscape refuses the entire add if an endpoint is missing, so never hand it one.
    if (!visible.nodeIds.has(edge.source) || !visible.nodeIds.has(edge.target)) {
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

  if (!relayout) {
    capturePositions();
  }

  const visible = computeVisible(model, options());

  // A big tree is unreadable at normal spacing, so the node size follows the graph size unless the
  // reader has chosen a density themselves.
  if (!densityChosen) {
    const wanted: DensityName = visible.nodeIds.size > 600 ? 'compact' : 'normal';
    if (wanted !== ui.density) {
      ui.density = wanted;
      element<HTMLSelectElement>('density').value = wanted;
    }
  }

  // A node the canvas has never placed has no entry yet, so the layout must run for it. Without
  // this, a filter that reveals new nodes would leave them stacked at the origin.
  const needsLayout = relayout
    || [...visible.nodeIds].some((id) => !lastPositions.has(id));

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
  highlightMember();
  syncToolbarState();

  if (needsLayout) {
    runLayout();
  } else {
    frame(ui.focus);
  }
}

/** Marks the dependencies a member search came from, so the member's own lines stand out. */
function highlightMember(): void {
  if (!cy) {
    return;
  }
  cy.edges().removeClass('member-match');
  const member = ui.member?.toLowerCase();
  if (!member) {
    return;
  }
  cy.edges().forEach((edge) => {
    const label = String(edge.data('label') ?? '').toLowerCase();
    if (label.includes(member)) {
      edge.addClass('member-match');
    }
  });
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
  // A truncated graph looks complete otherwise: say so where the reader is looking.
  if (model.stats.truncated) {
    parts.push(`TRUNCATED to ${model.nodes.length} of the workspace — raise maxNodes`);
  }
  parts.push(`${model.stats.elapsedMs} ms`);
  if (model.stats.parseErrors) {
    parts.push(`${model.stats.parseErrors} parse errors`);
  }
  element('stats').textContent = parts.join(' · ');
}

function updateFocusBar(): void {
  const bar = element<HTMLDivElement>('focus-bar');
  const scoped = (ui.scope?.length ?? 0) > 0;
  if (!ui.focus && !scoped) {
    bar.hidden = true;
    bar.textContent = '';
    return;
  }
  const node = model?.nodes.find((candidate) => candidate.id === ui.focus);
  bar.hidden = false;

  const parts: string[] = [];
  if (node) {
    parts.push(`Focused on <strong>${escapeHtml(node.label)}</strong>`);
  } else if (scoped) {
    parts.push(`Scoped to <strong>${escapeHtml(ui.scopeLabel || `${ui.scope!.length} types`)}</strong>`);
  }
  if (ui.member) {
    parts.push(`member <strong>${escapeHtml(ui.member)}</strong>`);
  }
  bar.innerHTML = parts.join(' · ');

  const clear = document.createElement('button');
  clear.type = 'button';
  clear.textContent = scoped ? 'Show everything' : 'Clear focus';
  clear.addEventListener('click', () => {
    ui.focus = null;
    ui.scope = null;
    ui.scopeExact = false;
    ui.scopeLabel = '';
    ui.member = null;
    applyView(true);
  });
  bar.appendChild(clear);
}

function mount(): void {
  hideTooltip();
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
    hideTooltip();
    const node = event.target as NodeSingular;
    selectedId = node.id();
    showDetails(node);
    applyDimming();
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
      selectedId = null;
      showDetails(undefined);
      applyDimming();
    }
  });

  cy.on('mouseover', 'node', (event) => {
    const node = event.target as NodeSingular;
    node.addClass('hovered');
    node.connectedEdges().addClass('neighbour');
    node.connectedEdges().connectedNodes().addClass('hover-source');
    const pointer = pointerOf(event);
    lastPointer = pointer;
    scheduleTooltip(() => tooltipMarkup(node), pointer.x, pointer.y);
  });

  cy.on('mouseout', 'node', () => {
    hideTooltip();
    cy!.elements().removeClass('hovered').removeClass('neighbour').removeClass('hover-source');
  });

  // Hovering a line says what the dependency is and where it comes from. Edges are thin, so the
  // line is also drawn thicker while hovered to show which one the tooltip describes.
  cy.on('mouseover', 'edge', (event) => {
    const edge = event.target as cytoscape.EdgeSingular;
    edge.addClass('hovered-edge');
    const pointer = pointerOf(event);
    lastPointer = pointer;
    scheduleTooltip(() => edgeTooltipMarkup(edge), pointer.x, pointer.y);
  });

  cy.on('mouseout', 'edge', (event) => {
    (event.target as cytoscape.EdgeSingular).removeClass('hovered-edge');
    hideTooltip();
  });

  cy.on('mousemove', 'edge', (event) => {
    const pointer = pointerOf(event);
    lastPointer = pointer;
    const tooltip = document.getElementById('tooltip') as HTMLDivElement | null;
    if (tooltip && !tooltip.hidden) {
      placeTooltip(tooltip, pointer.x, pointer.y);
    }
  });

  cy.on('dbltap', 'edge', (event) => {
    const edge = event.target as cytoscape.EdgeSingular;
    const record = model?.edges.find((candidate) => candidate.id === edge.id());
    if (record?.evidence) {
      hideTooltip();
      vscode.postMessage({ type: 'reveal', file: record.evidence.file, line: record.evidence.line });
    }
  });

  cy.on('mousemove', 'node', (event) => {
    const pointer = pointerOf(event);
    lastPointer = pointer;
    const tooltip = document.getElementById('tooltip') as HTMLDivElement | null;
    if (tooltip && !tooltip.hidden) {
      placeTooltip(tooltip, pointer.x, pointer.y);
    }
  });

  cy.on('cxttap', 'node', (event) => {
    hideTooltip();
    const node = event.target as NodeSingular;
    const original = event.originalEvent as MouseEvent | undefined;
    openNodeMenu(node, original?.clientX ?? 0, original?.clientY ?? 0);
  });

  cy.on('cxttap', (event) => {
    if (event.target === cy) {
      closeNodeMenu();
    }
  });

  cy.on('pan zoom', () => {
    closeNodeMenu();
    hideTooltip();
    scheduleViewportPaint();
  });

  cy.on('zoom', () => {
    syncZoomSlider();
    if (ui.labels !== 'zoom') {
      return;
    }
    if (zoomTimer) {
      clearTimeout(zoomTimer);
    }
    zoomTimer = setTimeout(() => {
      if (liveCanvas()) {
        syncLabels();
      }
    }, 90);
  });

  // A dropped pointer cancels the pan cursor if the mouseup happened outside the canvas.
  window.addEventListener('mousemove', (event) => {
    lastPointer = { x: event.clientX, y: event.clientY };
  }, { passive: true });

  window.addEventListener('mouseup', () => {
    panning = false;
    element('cy').classList.remove('panning');
  });

  const observer = new ResizeObserver(() => {
    liveCanvas()?.resize();
    scheduleViewportPaint();
    paintMinimap();
  });
  observer.observe(element<HTMLDivElement>('cy'));

  const canvasHost = document.getElementById('canvas');
  // Created once: a re-analysis replaces the cytoscape core but not the canvas, and a second overlay
  // layer would leave the previous one's frames painted underneath the new graph.
  if (canvasHost && !overlay) {
    overlay = createOverlay(canvasHost);
  }
}

/* ---------------------------------------------------------------- context menu */

function closeNodeMenu(): void {
  const menu = element<HTMLDivElement>('node-menu');
  menu.hidden = true;
  menu.innerHTML = '';
}

interface MenuItem {
  label: string;
  hint?: string;
  run: () => void;
}

/** Right-click menu for a node: unpack its packed shape, focus, scope, or open the source. */
function openNodeMenu(node: NodeSingular, clientX: number, clientY: number): void {
  const menu = element<HTMLDivElement>('node-menu');
  const id = node.id();
  const packed = ui.layout === 'packed';
  const group = cy ? componentGroups(cy).find((entry) => entry.nodeIds.includes(id)) : undefined;
  const root = group?.root ?? id;
  const groupSize = group?.nodeIds.length ?? 1;
  const unpacked = ui.unpacked.has(root);

  menu.innerHTML = '';
  const heading = document.createElement('div');
  heading.className = 'menu-heading';
  heading.textContent = `${node.data('label') as string} · ${groupSize} type${groupSize === 1 ? '' : 's'}`;
  menu.appendChild(heading);

  const items: MenuItem[] = [];

  if (packed) {
    items.push({
      label: unpacked ? 'Re-pack this group' : 'Unpack this group',
      hint: unpacked
        ? 'back into the packed tiles'
        : `${UNPACKED_LAYOUT_LABEL[ui.unpackedLayout]} · ${groupSize} type${groupSize === 1 ? '' : 's'}`,
      run: () => (unpacked ? repackGroup(node) : unpackGroup(node)),
    });
    if (ui.unpacked.size > 0) {
      items.push({
        label: 'Re-pack every group',
        hint: `${ui.unpacked.size} unpacked`,
        run: repackAll,
      });
    }
  }

  items.push(
    {
      label: 'Focus here',
      hint: `${ui.focusDepth} hop${ui.focusDepth === 1 ? '' : 's'}`,
      run: () => {
        ui.focus = id;
        showSelected(id);
      },
    },
    {
      label: 'Expand 2 hops',
      run: () => {
        ui.focus = id;
        ui.focusDepth = 2;
        element<HTMLSelectElement>('depth').value = '2';
        showSelected(id);
      },
    },
    {
      label: 'Scope to this type',
      hint: 'this type and its partners',
      run: () => {
        ui.scope = [id];
        ui.focus = id;
        ui.member = null;
        ui.scopeLabel = String(node.data('label'));
        applyView(true);
        showSelected(id);
      },
    },
    {
      label: 'Open source',
      run: () => vscode.postMessage({ type: 'reveal', file: node.data('file'), line: Number(node.data('line') ?? 1) }),
    },
    {
      label: 'Copy full name',
      run: () => vscode.postMessage({ type: 'copy', text: node.data('fullName') }),
    },
  );

  for (const entry of items) {
    const button = document.createElement('button');
    button.type = 'button';
    button.className = 'menu-item';
    button.innerHTML = `<span>${escapeHtml(entry.label)}</span>`
      + (entry.hint ? `<span class="muted">${escapeHtml(entry.hint)}</span>` : '');
    button.addEventListener('click', () => {
      closeNodeMenu();
      entry.run();
    });
    menu.appendChild(button);
  }

  menu.hidden = false;
  const bounds = menu.getBoundingClientRect();
  const left = Math.max(8, Math.min(clientX, window.innerWidth - bounds.width - 8));
  const top = Math.max(8, Math.min(clientY, window.innerHeight - bounds.height - 8));
  menu.style.left = `${left}px`;
  menu.style.top = `${top}px`;
}

/* ---------------------------------------------------------------- hover tooltip */

function indexFindings(): void {
  findingsByNode = new Map();
  for (const finding of model?.findings ?? []) {
    for (const id of finding.nodeIds) {
      const list = findingsByNode.get(id);
      if (list) {
        list.push(finding);
      } else {
        findingsByNode.set(id, [finding]);
      }
    }
  }
}

function tooltipMarkup(node: NodeSingular): string {
  const data = node.data();
  return renderTooltip(summariseNode({
    id: String(data.id),
    label: String(data.label ?? ''),
    fullName: String(data.fullName ?? ''),
    layer: String(data.layer ?? 'model'),
    kind: String(data.kind ?? ''),
    module: String(data.module ?? ''),
    file: data.file ? String(data.file) : undefined,
    line: data.file ? Number(data.line ?? 1) : undefined,
    diRegistered: data.diRegistered === true,
    inCycle: data.inCycle === true,
    edges: model?.edges ?? [],
    findings: findingsByNode.get(String(data.id)) ?? [],
  }));
}

/** Builds the line tooltip from the model edge, so it carries the evidence location and member. */
function edgeTooltipMarkup(edge: cytoscape.EdgeSingular): string {
  const record = model?.edges.find((candidate) => candidate.id === edge.id());
  if (!record || !model) {
    return '';
  }
  const source = model.nodes.find((node) => node.id === record.source);
  const target = model.nodes.find((node) => node.id === record.target);
  return renderEdgeTooltip(summariseEdge({
    kind: record.kind,
    sourceLabel: source?.label ?? record.source,
    sourceFullName: source?.fullName ?? record.source,
    targetLabel: target?.label ?? record.target,
    targetFullName: target?.fullName ?? record.target,
    confidence: record.confidence,
    member: record.label,
    file: record.evidence?.file,
    line: record.evidence?.line,
  }));
}

function hideTooltip(): void {
  if (tooltipTimer) {
    clearTimeout(tooltipTimer);
    tooltipTimer = undefined;
  }
  const tooltip = document.getElementById('tooltip') as HTMLDivElement | null;
  if (tooltip) {
    tooltip.hidden = true;
    tooltip.innerHTML = '';
  }
}

function placeTooltip(tooltip: HTMLDivElement, x: number, y: number): void {
  const bounds = tooltip.getBoundingClientRect();
  const left = Math.max(8, Math.min(x + 16, window.innerWidth - bounds.width - 8));
  const top = Math.max(8, Math.min(y + 18, window.innerHeight - bounds.height - 8));
  tooltip.style.left = `${left}px`;
  tooltip.style.top = `${top}px`;
}

/** Hovering says what the shape is before the reader commits a click. */
function scheduleTooltip(build: () => string, x: number, y: number): void {
  hideTooltip();
  tooltipTimer = setTimeout(() => {
    tooltipTimer = undefined;
    const tooltip = document.getElementById('tooltip') as HTMLDivElement | null;
    if (!tooltip) {
      return;
    }
    const markup = build();
    if (!markup) {
      return;
    }
    tooltip.innerHTML = markup;
    tooltip.hidden = false;
    placeTooltip(tooltip, x, y);
  }, 220);
}

function pointerOf(event: cytoscape.EventObject): { x: number; y: number } {
  const original = event.originalEvent as MouseEvent | undefined;
  if (original && typeof original.clientX === 'number') {
    return { x: original.clientX, y: original.clientY };
  }
  const rendered = (event as unknown as { renderedPosition?: { x: number; y: number } }).renderedPosition;
  const container = cy ? cy.container() : null;
  const rect = container ? container.getBoundingClientRect() : undefined;
  if (rendered && rect) {
    return { x: rect.left + rendered.x, y: rect.top + rendered.y };
  }
  return lastPointer;
}

/* ---------------------------------------------------------------- view controls */

/** Zoom range the slider covers. The bounds match the cytoscape instance's own limits. */
const ZOOM_MIN = 0.08;
const ZOOM_MAX = 3;

let regionArmed = false;
let regionStart: { x: number; y: number } | null = null;
let syncingSlider = false;

/** The slider is logarithmic, so the middle of the travel is a usable zoom rather than 1.5×. */
function sliderToZoom(value: number): number {
  const t = Math.min(1, Math.max(0, value / 100));
  return ZOOM_MIN * Math.pow(ZOOM_MAX / ZOOM_MIN, t);
}

function zoomToSlider(level: number): number {
  const clamped = Math.min(ZOOM_MAX, Math.max(ZOOM_MIN, level));
  return Math.round((Math.log(clamped / ZOOM_MIN) / Math.log(ZOOM_MAX / ZOOM_MIN)) * 100);
}

function syncZoomSlider(): void {
  const slider = document.getElementById('zoom-slider') as HTMLInputElement | null;
  if (!slider || !cy || syncingSlider) {
    return;
  }
  syncingSlider = true;
  slider.value = String(zoomToSlider(cy.zoom()));
  syncingSlider = false;
}

/** Zooms about the middle of the canvas, so the view stays where the reader is looking. */
function zoomAboutCentre(level: number, centre?: { x: number; y: number }): void {
  if (!cy) {
    return;
  }
  const width = cy.width();
  const height = cy.height();
  if (width <= 0 || height <= 0) {
    return;
  }

  const anchor = centre
    ?? { x: width / 2, y: height / 2 };
  const model = {
    x: (anchor.x - cy.pan().x) / cy.zoom(),
    y: (anchor.y - cy.pan().y) / cy.zoom(),
  };
  const next = Math.min(ZOOM_MAX, Math.max(ZOOM_MIN, level));
  cy.zoom({ level: next, renderedPosition: anchor });
  cy.pan({
    x: anchor.x - model.x * next,
    y: anchor.y - model.y * next,
  });
  syncZoomSlider();
}

function setRegionArmed(armed: boolean): void {
  regionArmed = armed;
  const button = document.getElementById('zoom-region') as HTMLButtonElement | null;
  if (button) {
    button.setAttribute('aria-pressed', String(armed));
  }
  const canvas = document.getElementById('cy');
  canvas?.classList.toggle('zooming-region', armed);
  const rect = document.getElementById('zoom-rect');
  if (rect) {
    rect.hidden = true;
  }
  regionStart = null;
  if (armed) {
    hideTooltip();
  }
}

/** Local canvas coordinates for a pointer event, so the rectangle and the zoom agree. */
function canvasPoint(event: PointerEvent | MouseEvent): { x: number; y: number } {
  const rect = document.getElementById('cy')?.getBoundingClientRect();
  return {
    x: event.clientX - (rect?.left ?? 0),
    y: event.clientY - (rect?.top ?? 0),
  };
}

function drawRegion(from: { x: number; y: number }, to: { x: number; y: number }): void {
  const rect = document.getElementById('zoom-rect');
  if (!rect) {
    return;
  }
  rect.hidden = false;
  rect.style.left = `${Math.min(from.x, to.x)}px`;
  rect.style.top = `${Math.min(from.y, to.y)}px`;
  rect.style.width = `${Math.abs(to.x - from.x)}px`;
  rect.style.height = `${Math.abs(to.y - from.y)}px`;
}

/**
 * Applies a dragged box as the new view: the box becomes the panel. Shapes inside it are what the
 * reader framed, so fitting their bounding box keeps them all visible with a little margin.
 */
function applyRegionZoom(from: { x: number; y: number }, to: { x: number; y: number }): void {
  if (!cy) {
    return;
  }

  const width = Math.abs(to.x - from.x);
  const height = Math.abs(to.y - from.y);
  if (width < 8 || height < 8) {
    // A click rather than a drag: leave the view alone.
    return;
  }

  const left = Math.min(from.x, to.x);
  const top = Math.min(from.y, to.y);
  const zoom = cy.zoom();
  const pan = cy.pan();

  const inside = cy.nodes().filter((node) => {
    const position = node.renderedPosition();
    const halfWidth = (node.renderedOuterWidth() || 20) / 2;
    const halfHeight = (node.renderedOuterHeight() || 20) / 2;
    return position.x + halfWidth >= left
      && position.x - halfWidth <= left + width
      && position.y + halfHeight >= top
      && position.y - halfHeight <= top + height;
  });

  if (inside.length > 0) {
    cy.fit(inside, 70);
    if (cy.zoom() > ZOOM_MAX) {
      cy.zoom({ level: ZOOM_MAX, renderedPosition: { x: cy.width() / 2, y: cy.height() / 2 } });
    }
    syncZoomSlider();
    return;
  }

  // Nothing inside the box: zoom to the box itself, so the drag still means something.
  const level = Math.min(cy.width() / width, cy.height() / height) * zoom;
  const modelX = (left + width / 2 - pan.x) / zoom;
  const modelY = (top + height / 2 - pan.y) / zoom;
  const next = Math.min(ZOOM_MAX, Math.max(ZOOM_MIN, level));
  cy.zoom(next);
  cy.pan({ x: cy.width() / 2 - modelX * next, y: cy.height() / 2 - modelY * next });
  syncZoomSlider();
}

/** Resets zoom, pan, focus, scope and unpacking: the view the panel opens with. */
function resetView(): void {
  if (!cy) {
    return;
  }
  setRegionArmed(false);
  hideTooltip();
  logEvent('reset-view', 'zoom, focus, scope and unpacking cleared');
  ui.focus = null;
  ui.scope = null;
  ui.scopeExact = false;
  ui.scopeLabel = '';
  ui.member = null;
  ui.unpacked.clear();
  selectedId = null;
  applyView(true);
}

function wireViewControls(): void {
  element<HTMLButtonElement>('zoom-region').addEventListener('click', () => {
    setRegionArmed(!regionArmed);
  });

  element<HTMLButtonElement>('view-reset').addEventListener('click', () => resetView());

  const slider = element<HTMLInputElement>('zoom-slider');
  slider.value = String(zoomToSlider(1));
  slider.addEventListener('input', () => {
    if (!cy || syncingSlider) {
      return;
    }
    zoomAboutCentre(sliderToZoom(Number(slider.value)));
  });

  const canvas = element<HTMLDivElement>('cy');

  canvas.addEventListener('pointerdown', (event) => {
    if (!regionArmed || event.button !== 0) {
      return;
    }
    regionStart = canvasPoint(event);
    drawRegion(regionStart, regionStart);
    canvas.setPointerCapture(event.pointerId);
    event.preventDefault();
  });

  canvas.addEventListener('pointermove', (event) => {
    if (!regionArmed || !regionStart) {
      return;
    }
    drawRegion(regionStart, canvasPoint(event));
  });

  canvas.addEventListener('pointerup', (event) => {
    if (!regionArmed || !regionStart) {
      return;
    }
    const start = regionStart;
    const end = canvasPoint(event);
    regionStart = null;
    const rect = document.getElementById('zoom-rect');
    if (rect) {
      rect.hidden = true;
    }
    applyRegionZoom(start, end);
    setRegionArmed(false);
  });

  canvas.addEventListener('pointercancel', () => {
    regionStart = null;
    const rect = document.getElementById('zoom-rect');
    if (rect) {
      rect.hidden = true;
    }
  });
}

/* ---------------------------------------------------------------- analysis progress */

/**
 * Shows, updates or hides the progress overlay.
 *
 * `known` is false while a phase has no measurable total (reading files, building the graph), which
 * switches the bar to an indeterminate sweep instead of reporting a percentage it cannot know.
 */
function setProgress(visible: boolean, message?: string, percent?: number, known?: boolean): void {
  const panel = document.getElementById('progress') as HTMLDivElement | null;
  const fill = document.getElementById('progress-fill') as HTMLDivElement | null;
  const label = document.getElementById('progress-message');
  const readout = document.getElementById('progress-percent');
  if (!panel || !fill) {
    return;
  }

  panel.hidden = !visible;
  document.getElementById('cy')?.classList.toggle('analysing', visible);
  if (!visible) {
    return;
  }

  if (label && message) {
    label.textContent = message;
  }
  const measured = known !== false && typeof percent === 'number';
  panel.classList.toggle('indeterminate', !measured);
  fill.style.width = measured ? `${Math.max(0, Math.min(100, percent ?? 0))}%` : '';
  if (readout) {
    readout.textContent = measured ? `${Math.round(percent ?? 0)}%` : '';
  }
  if (measured) {
    panel.setAttribute('aria-valuenow', String(Math.round(percent ?? 0)));
  }
}

/* ---------------------------------------------------------------- details */

function showDetails(node: NodeSingular | undefined): void {
  const details = element<HTMLDivElement>('details');
  if (!node || !model) {
    details.innerHTML = '<p class="muted">Select a node to inspect it. Double-click to open the source. Right-click to unpack its shape.</p>';
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
  selectedId = id;
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
  inheritance: {
    // The class hierarchy is invisible in every other preset because base types and interfaces are
    // structural, not behavioural. This is the one view that shows what derives from what.
    layers: [...NODE_LAYERS],
    edges: ['inherits', 'implements', 'template', 'reference'],
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
    logEvent('search', ui.search || '(cleared)');
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
    logEvent('module', ui.module);
    applyView(true);
  });

  const layout = element<HTMLSelectElement>('layout');
  layout.value = ui.layout;
  layout.addEventListener('change', () => {
    ui.layout = layout.value as LayoutName;
    logEvent('layout', ui.layout);
    runLayout();
  });

  const density = element<HTMLSelectElement>('density');
  density.value = ui.density;
  density.addEventListener('change', () => {
    densityChosen = true;
    ui.density = density.value as DensityName;
    logEvent('density', ui.density);
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
    logEvent('hide-unconnected', String(ui.hideOrphans));
    applyView(false);
  });

  const onlyProblems = element<HTMLInputElement>('only-problems');
  onlyProblems.checked = ui.onlyProblems;
  onlyProblems.addEventListener('change', () => {
    ui.onlyProblems = onlyProblems.checked;
    applyView(true);
  });

  const wireCheck = (
    id: string,
    value: () => boolean,
    set: (next: boolean) => void,
    onChange: () => void,
  ): void => {
    const box = document.getElementById(id) as HTMLInputElement | null;
    if (!box) {
      return;
    }
    box.checked = value();
    box.addEventListener('change', () => {
      set(box.checked);
      onChange();
    });
  };

  wireCheck('dim-selection', () => ui.dimSelection, (next) => { ui.dimSelection = next; }, applyDimming);
  wireCheck('group-captions', () => ui.groupCaptions, (next) => { ui.groupCaptions = next; }, refreshOverlayFrames);
  wireCheck('module-frames', () => ui.moduleFrames, (next) => { ui.moduleFrames = next; }, refreshOverlayFrames);
  wireCheck('island-tray', () => ui.islandTray, (next) => { ui.islandTray = next; }, () => applyView(true));

  const minimap = document.getElementById('minimap');
  minimap?.addEventListener('pointerdown', (event) => {
    const rect = minimap.getBoundingClientRect();
    if (rect.width <= 0 || rect.height <= 0) {
      return;
    }
    centreOnMinimapFraction(
      (event.clientX - rect.left) / rect.width,
      (event.clientY - rect.top) / rect.height,
    );
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
  element<HTMLButtonElement>('preset-inheritance').addEventListener('click', () => applyPreset('inheritance'));
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

  element<HTMLButtonElement>('repack').addEventListener('click', () => repackAll());

  document.addEventListener('click', (event) => {
    const menu = element<HTMLDivElement>('node-menu');
    if (!menu.hidden && !menu.contains(event.target as Node)) {
      closeNodeMenu();
    }
  });

  element<HTMLButtonElement>('reanalyze').addEventListener('click', () => {
    vscode.postMessage({ type: 'reanalyze' });
  });

  element<HTMLButtonElement>('export').addEventListener('click', () => {
    sendVisibleExport();
  });

  const panelToggle = element<HTMLButtonElement>('toggle-panel');
  panelToggle.addEventListener('click', () => {
    const shell = element<HTMLDivElement>('main');
    const hidden = shell.classList.toggle('panel-hidden');
    panelToggle.textContent = hidden ? 'Show details' : 'Hide details';
    panelToggle.setAttribute('aria-pressed', String(hidden));
    setTimeout(() => liveCanvas()?.resize(), 60);
  });

  document.addEventListener('keydown', (event) => {
    if (event.key !== 'Escape') {
      return;
    }
    if (regionArmed) {
      setRegionArmed(false);
      return;
    }
    if (!element<HTMLDivElement>('node-menu').hidden) {
      closeNodeMenu();
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

/**
 * Everything currently drawn, in the same shape as the full model, plus a record of the filters that
 * produced it. Exporting the whole workspace would hand back types the reader is not looking at.
 */
function visibleExport(): Record<string, unknown> | undefined {
  if (!model) {
    return undefined;
  }

  const visible = computeVisible(model, options());
  const nodes = model.nodes.filter((node) => visible.nodeIds.has(node.id));
  const allFindings = model.findings;

  /* A finding cites the edges that prove it, and some of those are a kind the reader has filtered
     out — a cycle built from type references is the usual case. Exporting the findings without those
     edges produced a file that claimed seven cycles and contained none of them, which is worse than
     saying nothing. Every cited edge whose endpoints are drawn is therefore included whatever its
     kind, and a finding that still cannot show all of its evidence is left out. */
  const drawnEdges = new Map(visible.edges.map((edge) => [edge.id, edge]));
  for (const finding of model.findings) {
    for (const id of finding.edgeIds ?? []) {
      if (drawnEdges.has(id)) {
        continue;
      }
      const record = model.edges.find((edge) => edge.id === id);
      if (record && visible.nodeIds.has(record.source) && visible.nodeIds.has(record.target)) {
        drawnEdges.set(id, record);
      }
    }
  }
  const edges = [...drawnEdges.values()];

  const findings = allFindings
    .filter((finding) => finding.nodeIds.some((id) => visible.nodeIds.has(id)))
    .filter((finding) => (finding.edgeIds ?? []).every((id) => drawnEdges.has(id)));
  const count = (kind: string): number => findings.filter((finding) => finding.kind === kind).length;
  const total = (kind: string): number => allFindings.filter((finding) => finding.kind === kind).length;

  return {
    exportedAt: new Date().toISOString(),
    scope: {
      description: ui.scopeLabel || (ui.scope ? `${ui.scope.length} types` : 'whole workspace'),
      layout: ui.layout,
      density: ui.density,
      labels: ui.labels,
      layers: [...ui.layers],
      edgeKinds: [...ui.edgeKinds],
      module: ui.module,
      search: ui.search,
      focus: ui.focus,
      focusDepth: ui.focusDepth,
      hideUnconnected: ui.hideOrphans,
      onlyProblems: ui.onlyProblems,
      scope: ui.scope,
      scopeExact: ui.scopeExact,
      unpackedGroups: [...ui.unpacked],
    },
    workspace: model.workspace,
    generatedAt: model.generatedAt,
    nodes,
    edges,
    findings,
    // Counted over what this file contains. `modelTotals` carries the whole-workspace figures, so a
    // reader can tell a quiet workspace from a narrow export.
    cleanup: {
      cycles: count('cycle'),
      deadBindings: count('dead-binding'),
      unboundViews: count('unbound-view'),
      unusedTypes: count('unused-type'),
      unusedMembers: count('unused-member'),
    },
    modelTotals: {
      cycles: total('cycle'),
      deadBindings: total('dead-binding'),
      unboundViews: total('unbound-view'),
      unusedTypes: total('unused-type'),
      unusedMembers: total('unused-member'),
    },
    stats: {
      ...model.stats,
      visibleNodes: nodes.length,
      visibleEdges: edges.length,
      hiddenUnconnected: visible.hiddenOrphans,
      totalNodes: model.nodes.length,
      totalEdges: model.edges.length,
    },
    warnings: model.warnings,
  };
}

/** Asks the host to save what is on screen. Nothing leaves the panel without a visible graph. */
function sendVisibleExport(): void {
  const payload = visibleExport();
  if (!payload) {
    return;
  }
  vscode.postMessage({ type: 'export', payload });
}

interface GraphRequestMessage {
  title?: string;
  nodeIds?: string[];
  focus?: string;
  member?: string;
  depth?: number;
}

let pendingRequest: GraphRequestMessage | undefined;

/** Applies a request that started outside the panel (a right-click in the editor or Explorer). */
function applyRequest(request: GraphRequestMessage): void {
  if (!model) {
    pendingRequest = request;
    return;
  }

  const known = new Set(model.nodes.map((node) => node.id));
  const ids = (request.nodeIds ?? []).filter((id) => known.has(id));
  if (ids.length === 0 && !(request.focus && known.has(request.focus))) {
    return;
  }

  // The requested types must be drawable, so their layers and the edge kinds a symbol search
  // depends on are switched on before the scope is applied.
  for (const node of model.nodes) {
    if (ids.includes(node.id) || node.id === request.focus) {
      ui.layers.add(node.layer);
    }
  }
  if (request.member) {
    ui.edgeKinds.add('reference');
  }
  syncVisibilityControls();

  ui.scope = ids.length > 0 ? ids : null;
  ui.scopeExact = false;
  ui.focus = request.focus && known.has(request.focus) ? request.focus : (ids[0] ?? null);
  ui.member = request.member ?? null;
  ui.scopeLabel = request.title ?? '';
  ui.focusDepth = Math.max(1, Math.min(3, request.depth ?? 1));
  element<HTMLSelectElement>('depth').value = String(ui.focusDepth);

  applyView(true);
  selectNode(ui.focus);
}

function selectNode(id: string | null): void {
  if (!cy || !id) {
    return;
  }
  const node = cy.getElementById(id);
  if (node.nonempty()) {
    cy.elements().unselect();
    node.select();
    showDetails(node as NodeSingular);
  }
}

window.addEventListener('message', (event: MessageEvent) => {
  const message = event.data as {
    type?: string;
    model?: GraphModel;
    message?: string;
    request?: GraphRequestMessage;
    settings?: Partial<GraphSettings>;
    percent?: number;
    known?: boolean;
    done?: boolean;
  };
  if (message?.type === 'settings' && message.settings) {
    const next = toUnpackedLayout(message.settings.unpackedLayout);
    if (next !== ui.unpackedLayout) {
      ui.unpackedLayout = next;
      // Only the unpacked shapes move, so the graph is redrawn in place rather than re-analysed.
      if (ui.unpacked.size > 0) {
        runLayout();
      }
    }
  }
  if (message?.type === 'progress') {
    if (message.done) {
      setProgress(false);
    } else {
      setProgress(true, message.message, message.percent, message.known);
    }
  }
  if (message?.type === 'model' && message.model) {
    setProgress(false);
    model = message.model;
    modelLoadedAt = new Date().toISOString();
    logEvent('model', `${model.nodes.length} nodes, ${model.edges.length} edges`);
    ui.focus = null;
    ui.scope = null;
    ui.scopeExact = false;
    ui.member = null;
    ui.scopeLabel = '';
    ui.unpacked.clear();
    selectedId = null;
    lastPositions.clear();
    populateModules();
    mount();
    indexFindings();
    updateVisibilityCounts();
    applyView(true);
    renderProblems();
    if (pendingRequest) {
      const request = pendingRequest;
      pendingRequest = undefined;
      applyRequest(request);
    }
  }
  if (message?.type === 'request' && message.request) {
    applyRequest(message.request);
  }
  if (message?.type === 'requestExport') {
    sendVisibleExport();
  }
  if (message?.type === 'requestDiagnostics') {
    vscode.postMessage({ type: 'diagnostics', facts: diagnosticsFacts() });
  }
  if (message?.type === 'notice') {
    const details = element<HTMLDivElement>('details');
    const note = document.createElement('p');
    note.className = 'muted notice';
    note.textContent = message.message ?? '';
    details.prepend(note);
    setTimeout(() => note.remove(), 6000);
  }
  if (message?.type === 'error') {
    setProgress(false);
    element('details').innerHTML = `<p class="error">${escapeHtml(message.message ?? 'Analysis failed')}</p>`;
  }
});

wireControls();
wireViewControls();
watchTheme();
setTab('details');
vscode.postMessage({ type: 'ready' });

