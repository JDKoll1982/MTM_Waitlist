import type { EdgeKind, GraphEdge, GraphModel, GraphNode, NodeLayer } from '../../graph/model';

/** Everything the visibility calculation needs; all of it is pure data so it can be tested headlessly. */
export interface ViewOptions {
  layers: readonly NodeLayer[];
  edgeKinds: readonly EdgeKind[];
  /** 'all' or a module name. */
  module: string;
  search: string;
  /** Node id to focus on, or null for the whole scope. */
  focus: string | null;
  focusDepth: number;
  hideOrphans: boolean;
  /** Show only nodes that carry a cleanup finding or sit in a cycle. */
  onlyProblems: boolean;
  /**
   * Node ids a symbol search asked for. The graph is restricted to these and everything within
   * `focusDepth` hops, so a right-click on one method shows that method's type and its partners
   * rather than the whole workspace.
   */
  scope?: readonly string[] | null;
  /**
   * Draw the scope exactly, with no hop expansion. Unpacking a shape uses this: the reader asked to
   * see that shape, so partners outside it must not be added back.
   */
  scopeExact?: boolean;
}

export interface VisibleGraph {
  nodeIds: Set<string>;
  edges: GraphEdge[];
  hiddenOrphans: number;
}

function passesLayerAndSearch(node: GraphNode, layers: Set<NodeLayer>, needle: string): boolean {
  if (!layers.has(node.layer)) {
    return false;
  }
  if (!needle) {
    return true;
  }
  return node.label.toLowerCase().includes(needle)
    || node.fullName.toLowerCase().includes(needle)
    || node.module.toLowerCase().includes(needle);
}

function buildAdjacency(edges: readonly GraphEdge[]): Map<string, string[]> {
  const adjacency = new Map<string, string[]>();
  const link = (from: string, to: string): void => {
    const list = adjacency.get(from);
    if (list) {
      list.push(to);
    } else {
      adjacency.set(from, [to]);
    }
  };
  for (const edge of edges) {
    link(edge.source, edge.target);
    link(edge.target, edge.source);
  }
  return adjacency;
}

/**
 * Computes exactly which nodes and edges are on screen.
 *
 * A module scope keeps the partners of its nodes even when they live in another module or project.
 * That is not a nicety: in this codebase every view sits in `Module_*` while its view model sits in a
 * `MTM_Waitlist.*` library, so a strict module filter would show views with nothing attached and then
 * hide them as unconnected.
 */
export function computeVisible(model: GraphModel, options: ViewOptions): VisibleGraph {
  const layers = new Set(options.layers);
  const kinds = new Set(options.edgeKinds);
  const needle = options.search.trim().toLowerCase();
  const adjacency = buildAdjacency(model.edges);

  const base = new Set<string>();
  for (const node of model.nodes) {
    if (!passesLayerAndSearch(node, layers, needle)) {
      continue;
    }
    if (options.onlyProblems && !node.inCycle && (node.problemCount ?? 0) === 0) {
      continue;
    }
    base.add(node.id);
  }

  let nodeIds: Set<string>;
  // An empty module is "no filter", not "a module with no name": the module select reads as empty
  // before the list is populated, and treating that as a scope empties the canvas.
  if (!options.module || options.module === 'all') {
    nodeIds = new Set(base);
  } else {
    nodeIds = new Set<string>();
    for (const node of model.nodes) {
      if (node.module === options.module && base.has(node.id)) {
        nodeIds.add(node.id);
      }
    }
    // One hop out, so a view keeps the view model it resolves and vice versa.
    for (const id of [...nodeIds]) {
      for (const neighbour of adjacency.get(id) ?? []) {
        if (base.has(neighbour)) {
          nodeIds.add(neighbour);
        }
      }
    }
  }

  if (options.focus) {
    if (!nodeIds.has(options.focus) && model.nodes.some((node) => node.id === options.focus)) {
      nodeIds.add(options.focus);
    }

    const reached = new Set<string>([options.focus]);
    let frontier = [options.focus];
    const depth = Math.max(1, options.focusDepth);
    for (let step = 0; step < depth; step += 1) {
      const next: string[] = [];
      for (const id of frontier) {
        for (const neighbour of adjacency.get(id) ?? []) {
          if (!reached.has(neighbour)) {
            reached.add(neighbour);
            next.push(neighbour);
          }
        }
      }
      frontier = next;
    }

    nodeIds = new Set([...nodeIds].filter((id) => reached.has(id)));
    nodeIds.add(options.focus);
  }

  let edges = model.edges.filter((edge) =>
    kinds.has(edge.kind) && nodeIds.has(edge.source) && nodeIds.has(edge.target));

  let scopeIds = new Set<string>();
  if (options.scope && options.scope.length > 0) {
    const known = new Set(model.nodes.map((node) => node.id));
    scopeIds = new Set(options.scope.filter((id) => known.has(id)));

    const reached = new Set<string>(scopeIds);
    if (!options.scopeExact) {
      let frontier = [...scopeIds];
      const depth = Math.max(1, options.focusDepth);
      for (let step = 0; step < depth; step += 1) {
        const next: string[] = [];
        for (const id of frontier) {
          for (const neighbour of adjacency.get(id) ?? []) {
            if (!reached.has(neighbour)) {
              reached.add(neighbour);
              next.push(neighbour);
            }
          }
        }
        frontier = next;
      }
    }

    nodeIds = new Set([...nodeIds].filter((id) => reached.has(id)));
    for (const id of scopeIds) {
      nodeIds.add(id);
    }
  }

  // The node set can only have shrunk since the edges were first collected, so re-filter here. An
  // edge whose endpoint is no longer drawn makes cytoscape reject the whole add, which empties the
  // canvas — the exact failure that made "Hide unconnected" appear to unlink everything.
  edges = edges.filter((edge) => nodeIds.has(edge.source) && nodeIds.has(edge.target));

  let hiddenOrphans = 0;
  if (options.hideOrphans) {
    const connected = new Set<string>();
    for (const edge of edges) {
      connected.add(edge.source);
      connected.add(edge.target);
    }
    const flagged = new Set(model.nodes
      .filter((node) => node.inCycle || (node.problemCount ?? 0) > 0)
      .map((node) => node.id));
    // A view or a view model is the thing the reader came to look at. Its link to its partner may be
    // a kind that is currently filtered out — a view whose only relationship is a type reference or a
    // base class relationship is still a screen — so pruning it as "unconnected" removes real
    // structure from the picture. Only services and models are pruned; they exist to serve the rest.
    const selfEvident = new Set(model.nodes
      .filter((node) => node.layer === 'view' || node.layer === 'viewmodel')
      .map((node) => node.id));
    const kept = new Set<string>();
    for (const id of nodeIds) {
      // A cleanup finding is the thing the reader came for; hiding it as "unconnected" would delete
      // the problem and leave an empty panel.
      if (connected.has(id) || id === options.focus || flagged.has(id) || scopeIds.has(id)
        || selfEvident.has(id)) {
        kept.add(id);
      } else {
        hiddenOrphans += 1;
      }
    }
    nodeIds = kept;
    edges = edges.filter((edge) => nodeIds.has(edge.source) && nodeIds.has(edge.target));
  }

  return { nodeIds, edges, hiddenOrphans };
}

/** Modules present in the model, with how many nodes each one contributes. */
export function moduleSummary(model: GraphModel): { name: string; count: number }[] {
  const counts = new Map<string, number>();
  for (const node of model.nodes) {
    counts.set(node.module, (counts.get(node.module) ?? 0) + 1);
  }
  return [...counts]
    .map(([name, count]) => ({ name, count }))
    .sort((left, right) => left.name.localeCompare(right.name));
}
