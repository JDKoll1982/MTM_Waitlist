import type { Core, NodeSingular } from 'cytoscape';
import type { UnpackedLayout } from '../../graph/settings';
import { DEFAULT_GRAPH_SETTINGS } from '../../graph/settings';

export interface Box {
  width: number;
  height: number;
}

export interface Placement {
  x: number;
  y: number;
}

/**
 * Packs boxes into rows, aimng for a target aspect ratio.
 *
 * A dependency graph is not one shape but many small shapes: a view with its view model, a view
 * model with its services. Laid out as a single graph, dagre stacks those disconnected pieces into
 * one tall column. Packing the pieces side by side is what makes the result fill the panel.
 */
export function packBoxes(boxes: readonly Box[], gap: number, targetAspect: number): Placement[] {
  if (boxes.length === 0) {
    return [];
  }

  const area = boxes.reduce((sum, box) => sum + (box.width + gap) * (box.height + gap), 0);
  const rowWidth = Math.max(
    Math.max(...boxes.map((box) => box.width)),
    Math.sqrt(area * Math.max(0.2, targetAspect)),
  );

  const order = boxes
    .map((box, index) => ({ box, index }))
    .sort((left, right) => right.box.height - left.box.height || left.index - right.index);

  const placements: Placement[] = new Array(boxes.length);
  let cursorX = 0;
  let cursorY = 0;
  let rowHeight = 0;

  for (const entry of order) {
    if (cursorX > 0 && cursorX + entry.box.width > rowWidth) {
      cursorX = 0;
      cursorY += rowHeight + gap;
      rowHeight = 0;
    }
    placements[entry.index] = { x: cursorX, y: cursorY };
    cursorX += entry.box.width + gap;
    rowHeight = Math.max(rowHeight, entry.box.height);
  }

  return placements;
}

interface ComponentInfo {
  nodes: NodeSingular[];
  width: number;
  height: number;
  local: Map<string, Placement>;
  root: string;
  unpacked: boolean;
}

/** A packed shape: one connected piece of the graph, named by the node its dependencies start at. */
export interface ComponentGroup {
  /** The node nothing inside the group points at — where the group's chain starts. */
  root: string;
  nodeIds: string[];
}

function weaklyConnected(nodes: NodeSingular[], core: Core): NodeSingular[][] {
  const adjacency = new Map<string, string[]>();
  const link = (from: string, to: string): void => {
    const list = adjacency.get(from);
    if (list) {
      list.push(to);
    } else {
      adjacency.set(from, [to]);
    }
  };
  for (const edge of core.edges()) {
    link(edge.source().id(), edge.target().id());
    link(edge.target().id(), edge.source().id());
  }

  const seen = new Set<string>();
  const components: NodeSingular[][] = [];
  const sorted = [...nodes].sort((left, right) => left.id().localeCompare(right.id()));

  for (const node of sorted) {
    if (seen.has(node.id())) {
      continue;
    }
    const group: NodeSingular[] = [];
    const queue = [node.id()];
    seen.add(node.id());
    while (queue.length > 0) {
      const id = queue.shift() as string;
      const current = core.getElementById(id);
      if (current.nonempty()) {
        group.push(current as NodeSingular);
      }
      for (const neighbour of adjacency.get(id) ?? []) {
        if (!seen.has(neighbour)) {
          seen.add(neighbour);
          queue.push(neighbour);
        }
      }
    }
    components.push(group);
  }

  return components;
}

function measure(component: NodeSingular[], root: string, unpacked: boolean): ComponentInfo {
  let minX = Number.POSITIVE_INFINITY;
  let minY = Number.POSITIVE_INFINITY;
  let maxX = Number.NEGATIVE_INFINITY;
  let maxY = Number.NEGATIVE_INFINITY;

  for (const node of component) {
    const position = node.position();
    const halfWidth = (node.outerWidth() || 20) / 2;
    const halfHeight = (node.outerHeight() || 20) / 2;
    minX = Math.min(minX, position.x - halfWidth);
    maxX = Math.max(maxX, position.x + halfWidth);
    minY = Math.min(minY, position.y - halfHeight);
    maxY = Math.max(maxY, position.y + halfHeight);
  }

  const local = new Map<string, Placement>();
  for (const node of component) {
    const position = node.position();
    local.set(node.id(), { x: position.x - minX, y: position.y - minY });
  }

  return { nodes: component, width: maxX - minX, height: maxY - minY, local, root, unpacked };
}

/**
 * The root of a group: the node no other node in the group points at. A group with no such node
 * (every node is pointed at) falls back to the most depended-on node, then to the lowest id, so a
 * right-click always resolves to the same root.
 */
function rootOf(component: NodeSingular[], core: Core): string {
  const ids = new Set(component.map((node) => node.id()));
  const inDegree = new Map<string, number>();
  for (const id of ids) {
    inDegree.set(id, 0);
  }
  for (const edge of core.edges()) {
    const target = edge.target().id();
    const source = edge.source().id();
    if (ids.has(target) && ids.has(source) && source !== target) {
      inDegree.set(target, (inDegree.get(target) ?? 0) + 1);
    }
  }

  return [...ids].sort((left, right) =>
    (inDegree.get(left) ?? 0) - (inDegree.get(right) ?? 0) || left.localeCompare(right))[0] as string;
}

/** The weakly connected pieces currently on the canvas, each with the node its chain starts at. */
export function componentGroups(core: Core): ComponentGroup[] {
  const nodes = core.nodes().toArray() as NodeSingular[];
  return weaklyConnected(nodes, core)
    .map((group) => ({ root: rootOf(group, core), nodeIds: group.map((node) => node.id()) }))
    .sort((left, right) => left.root.localeCompare(right.root));
}

/** The root of the group that contains `nodeId` — what right-click → unpack acts on. */
export function groupRootFor(core: Core, nodeId: string): string {
  return componentGroups(core).find((group) => group.nodeIds.includes(nodeId))?.root ?? nodeId;
}

export interface PackedOptions {
  gap?: number;
  aspect?: number;
  /** Spacing inside an unpacked group, which is drawn at the panel's own density instead of packed tight. */
  nodeSep?: number;
  rankSep?: number;
  /** Group roots to draw as their own full-width band above the packed tiles. */
  unpacked?: ReadonlySet<string>;
  /** Layout used inside an unpacked group. Packed tiles always use a tight layered arrangement. */
  unpackedLayout?: UnpackedLayout;
}

/** Tight internal spacing used inside a packed tile. */
const TILE_NODE_SEP = 18;
const TILE_RANK_SEP = 80;

/**
 * An unpacked shape is laid out at roughly twice a tile's spacing. Without that gap the only visible
 * difference from the packed arrangement would be a 90° rotation, which on a star-shaped group reads
 * as "nothing happened".
 */
const UNPACKED_SPACING = 2;

/** The cytoscape options for a layout, restricted to the given elements so it never reflows the graph. */
function layoutOptionsFor(name: string, nodeSep: number, rankSep: number): Record<string, unknown> {
  const common = { animate: false, fit: false, padding: 20 };

  switch (name) {
    case 'layered-tb':
      return { name: 'dagre', rankDir: 'TB', nodeSep, rankSep, edgeSep: 10, ...common };
    case 'layered-lr':
      return { name: 'dagre', rankDir: 'LR', nodeSep, rankSep, edgeSep: 10, ...common };
    case 'force':
      return {
        name: 'cose',
        idealEdgeLength: rankSep,
        nodeRepulsion: 9000,
        nodeOverlap: 14,
        gravity: 60,
        numIter: 900,
        ...common,
      };
    case 'radial':
      return {
        name: 'concentric',
        concentric: (node: NodeSingular) => {
          const layer = node.data('layer') as string;
          return layer === 'view' ? 4 : layer === 'viewmodel' ? 3 : layer === 'service' ? 2 : 1;
        },
        levelWidth: () => 1,
        minNodeSpacing: nodeSep + 12,
        ...common,
      };
    case 'grid':
      return { name: 'grid', avoidOverlap: true, condense: false, spacingFactor: 1.35, ...common };
    case 'circle':
      return { name: 'circle', avoidOverlap: true, spacingFactor: 1.8, ...common };
    default:
      return { name: 'dagre', rankDir: 'LR', nodeSep, rankSep, edgeSep: 10, ...common };
  }
}

function layoutGroup(
  core: Core,
  component: NodeSingular[],
  layout: string,
  nodeSep: number,
  rankSep: number,
): void {
  if (component.length < 2) {
    return;
  }
  const collection = core.collection().add(component);
  const sub = collection.union(collection.nodes().edgesWith(collection.nodes()));
  core.layout({
    ...layoutOptionsFor(layout, nodeSep, rankSep),
    eles: sub,
  } as never).run();
}

/**
 * Lays the graph out as packed components: each connected piece is arranged internally, then the
 * pieces tile across the panel so the result fills both axes.
 *
 * A group named in `unpacked` opts out of tiling. It is laid out at the panel's own density from its
 * root to the end of the group, in the layout the reader chose (Layered Down by default), and given a
 * full-width band above the tiles rather than being compressed into one.
 */
export function packedLayout(core: Core, options: PackedOptions = {}): void {
  const gap = options.gap ?? 70;
  const aspect = options.aspect ?? 1.7;
  const unpacked = options.unpacked ?? new Set<string>();
  const unpackedLayout = options.unpackedLayout ?? DEFAULT_GRAPH_SETTINGS.unpackedLayout;
  const nodes = core.nodes().toArray() as NodeSingular[];
  if (nodes.length === 0) {
    return;
  }

  const components = weaklyConnected(nodes, core)
    .map((group) => {
      const root = rootOf(group, core);
      const isUnpacked = unpacked.has(root);
      layoutGroup(
        core,
        group,
        isUnpacked ? unpackedLayout : 'layered-lr',
        isUnpacked ? (options.nodeSep ?? TILE_NODE_SEP) * UNPACKED_SPACING : TILE_NODE_SEP,
        isUnpacked ? (options.rankSep ?? TILE_RANK_SEP) * UNPACKED_SPACING : TILE_RANK_SEP,
      );
      return measure(group, root, isUnpacked);
    })
    .sort((left, right) => right.width * right.height - left.width * left.height);

  const place = (component: ComponentInfo, x: number, y: number): void => {
    for (const node of component.nodes) {
      const offset = component.local.get(node.id()) as Placement;
      node.position({ x: x + offset.x, y: y + offset.y });
    }
  };

  const tiles = components.filter((entry) => !entry.unpacked);
  const placements = packBoxes(
    tiles.map((component) => ({ width: component.width, height: component.height })),
    gap,
    aspect,
  );

  // An unpacked shape is wider or taller than a tile, so centring it over the tiles keeps the band
  // reading as part of the same picture instead of hanging off the left edge.
  const tilesWidth = placements.reduce(
    (widest, placement, index) => Math.max(widest, placement.x + tiles[index].width),
    0,
  );

  let bandY = 0;
  for (const component of components.filter((entry) => entry.unpacked)) {
    place(component, Math.max(0, (tilesWidth - component.width) / 2), bandY);
    bandY += component.height + gap;
  }

  tiles.forEach((component, index) => {
    place(component, placements[index].x, bandY + placements[index].y);
  });
}

