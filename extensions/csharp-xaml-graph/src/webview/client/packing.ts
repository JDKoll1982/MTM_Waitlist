import type { Core, NodeSingular } from 'cytoscape';

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

function measure(component: NodeSingular[]): ComponentInfo {
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

  return { nodes: component, width: maxX - minX, height: maxY - minY, local };
}

/**
 * Lays the graph out as packed components: each connected piece is arranged internally, then the
 * pieces tile across the panel so the result fills both axes.
 */
export function packedLayout(core: Core, options: { gap?: number; aspect?: number } = {}): void {
  const gap = options.gap ?? 70;
  const aspect = options.aspect ?? 1.7;
  const nodes = core.nodes().toArray() as NodeSingular[];
  if (nodes.length === 0) {
    return;
  }

  const components = weaklyConnected(nodes, core)
    .map((group) => {
      if (group.length > 1) {
        const collection = core.collection().add(group);
        const sub = collection.union(collection.nodes().edgesWith(collection.nodes()));
        core.layout({
          name: 'dagre',
          rankDir: 'LR',
          nodeSep: 18,
          rankSep: 80,
          edgeSep: 10,
          animate: false,
          fit: false,
          eles: sub,
        } as never).run();
      }
      return measure(group);
    })
    .sort((left, right) => right.width * right.height - left.width * left.height);

  const placements = packBoxes(
    components.map((component) => ({ width: component.width, height: component.height })),
    gap,
    aspect,
  );

  components.forEach((component, index) => {
    const placed = placements[index];
    for (const node of component.nodes) {
      const offset = component.local.get(node.id());
      if (!offset) {
        continue;
      }
      node.position({ x: placed.x + offset.x, y: placed.y + offset.y });
    }
  });
}
