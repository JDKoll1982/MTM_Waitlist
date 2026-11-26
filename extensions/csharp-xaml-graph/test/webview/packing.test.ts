import { afterEach, describe, expect, it } from 'vitest';
import type { Core } from 'cytoscape';
import {
  componentGroups,
  groupRootFor,
  packBoxes,
  packedGroups,
  packedLayout,
} from '../../src/webview/client/packing';
import { createCore, edgeData, nodeData } from '../support/cytoscape';

let core: Core | undefined;

afterEach(() => {
  core?.destroy();
  core = undefined;
});

describe('packBoxes', () => {
  it('returns nothing for no boxes', () => {
    expect(packBoxes([], 10, 1)).toEqual([]);
  });

  it('places a single box at the origin', () => {
    expect(packBoxes([{ width: 100, height: 50 }], 10, 1)).toEqual([{ x: 0, y: 0 }]);
  });

  it('fills a row until it would overflow, then starts a new one', () => {
    const boxes = [
      { width: 100, height: 100 },
      { width: 100, height: 100 },
      { width: 100, height: 100 },
      { width: 100, height: 100 },
    ];
    const placements = packBoxes(boxes, 10, 1);

    // A square target for four 110×110 cells gives a row width of ~220, so two fit per row.
    expect(placements[0]).toEqual({ x: 0, y: 0 });
    expect(placements[1]).toEqual({ x: 110, y: 0 });
    expect(placements[2]).toEqual({ x: 0, y: 110 });
    expect(placements[3]).toEqual({ x: 110, y: 110 });
  });

  it('puts the tallest box first so rows are packed tightly', () => {
    const placements = packBoxes([
      { width: 40, height: 10 },
      { width: 40, height: 90 },
    ], 0, 3.2);

    // Both fit on one row; the taller one is placed first, at the left.
    expect(placements[1]).toEqual({ x: 0, y: 0 });
    expect(placements[0]).toEqual({ x: 40, y: 0 });
  });

  it('keeps a very wide box on its own row', () => {
    const placements = packBoxes([
      { width: 500, height: 20 },
      { width: 20, height: 20 },
    ], 10, 1);

    expect(placements[0]).toEqual({ x: 0, y: 0 });
    expect(placements[1].y).toBe(30);
  });

  it('clamps a degenerate aspect ratio instead of dividing by it', () => {
    const placements = packBoxes([{ width: 10, height: 10 }, { width: 10, height: 10 }], 5, 0);
    expect(placements).toHaveLength(2);
    expect(Number.isFinite(placements[1].x)).toBe(true);
  });
});

describe('componentGroups', () => {
  it('groups the weakly connected pieces and names each by its root', () => {
    core = createCore([
      nodeData('N.A'),
      nodeData('N.B'),
      nodeData('N.C'),
      edgeData('N.A', 'N.B'),
      edgeData('N.B', 'N.C'),
    ]);

    expect(componentGroups(core)).toEqual([
      { root: 'N.A', nodeIds: ['N.A', 'N.B', 'N.C'] },
    ]);
  });

  it('treats a node with no edges as its own group', () => {
    core = createCore([nodeData('N.Lonely'), nodeData('N.Other')]);
    expect(componentGroups(core).map((group) => group.root)).toEqual(['N.Lonely', 'N.Other']);
  });

  it('falls back to the lowest id when every node is depended on', () => {
    core = createCore([
      nodeData('N.A'),
      nodeData('N.B'),
      edgeData('N.A', 'N.B'),
      edgeData('N.B', 'N.A'),
    ]);
    expect(componentGroups(core)[0].root).toBe('N.A');
  });

  it('reports an empty list for an empty canvas', () => {
    core = createCore([]);
    expect(componentGroups(core)).toEqual([]);
  });
});

describe('groupRootFor', () => {
  it('finds the root of the group a node belongs to', () => {
    core = createCore([nodeData('N.A'), nodeData('N.B'), edgeData('N.A', 'N.B')]);
    expect(groupRootFor(core, 'N.B')).toBe('N.A');
  });

  it('returns the node itself when no group holds it', () => {
    core = createCore([]);
    expect(groupRootFor(core, 'N.Missing')).toBe('N.Missing');
  });
});

describe('packedLayout', () => {
  const threeGroups = () => createCore([
    nodeData('N.A1'), nodeData('N.A2'), edgeData('N.A1', 'N.A2'),
    nodeData('N.B1'), nodeData('N.B2'), edgeData('N.B1', 'N.B2'),
    nodeData('N.C1'), nodeData('N.C2'), edgeData('N.C1', 'N.C2'),
  ]);

  it('does nothing on an empty canvas', () => {
    core = createCore([]);
    expect(() => packedLayout(core!)).not.toThrow();
  });

  it('gives every node a finite position', () => {
    core = threeGroups();
    packedLayout(core);

    for (const node of core.nodes()) {
      const position = node.position();
      expect(Number.isFinite(position.x) && Number.isFinite(position.y)).toBe(true);
    }
  });

  it('lays each tile out horizontally, so a pair sits side by side', () => {
    core = createCore([nodeData('N.A'), nodeData('N.B'), edgeData('N.A', 'N.B')]);
    packedLayout(core);

    const a = core.getElementById('N.A').position();
    const b = core.getElementById('N.B').position();
    expect(a.y).toBe(b.y);
    expect(Math.abs(a.x - b.x)).toBeGreaterThan(20);
  });

  it('tiles the groups instead of stacking them into one column', () => {
    core = threeGroups();
    packedLayout(core);

    const ys = ['N.A1', 'N.B1', 'N.C1'].map((id) => core!.getElementById(id).position().y);
    const xs = ['N.A1', 'N.B1', 'N.C1'].map((id) => core!.getElementById(id).position().x);
    expect(new Set(ys).size + new Set(xs).size).toBeGreaterThan(2);
  });

  it('draws an unpacked group in its own band above the tiles', () => {
    core = threeGroups();
    packedLayout(core, { unpacked: new Set(['N.C1']), unpackedLayout: 'layered-tb' });

    const unpacked = ['N.C1', 'N.C2'].map((id) => core!.getElementById(id).position());
    const tiled = ['N.A1', 'N.B1'].map((id) => core!.getElementById(id).position());
    expect(Math.min(...unpacked.map((position) => position.y)))
      .toBeLessThan(Math.min(...tiled.map((position) => position.y)));
  });

  it('puts an unpacked group’s chain top to bottom in Layered Down', () => {
    core = createCore([
      nodeData('N.Root'), nodeData('N.Leaf'), edgeData('N.Root', 'N.Leaf'),
    ]);
    packedLayout(core, { unpacked: new Set(['N.Root']), unpackedLayout: 'layered-tb' });

    expect(core.getElementById('N.Leaf').position().y)
      .toBeGreaterThan(core.getElementById('N.Root').position().y);
  });

  it('puts an unpacked group’s chain left to right in Layered Right', () => {
    core = createCore([
      nodeData('N.Root'), nodeData('N.Leaf'), edgeData('N.Root', 'N.Leaf'),
    ]);
    packedLayout(core, { unpacked: new Set(['N.Root']), unpackedLayout: 'layered-lr' });

    expect(core.getElementById('N.Leaf').position().x)
      .toBeGreaterThan(core.getElementById('N.Root').position().x);
  });

  it('lays a single unpacked node out without a layout pass', () => {
    core = createCore([nodeData('N.Alone')]);
    packedLayout(core, { unpacked: new Set(['N.Alone']) });
    // Placed at its own half-size: the box measurement runs, no layout engine does.
    expect(core.getElementById('N.Alone').position()).toEqual({ x: 20, y: 10 });
  });

  it('accepts every unpacked layout without throwing', () => {
    for (const layout of ['layered-tb', 'layered-lr', 'force', 'radial', 'grid', 'circle'] as const) {
      core?.destroy();
      core = createCore([nodeData('N.Root'), nodeData('N.Leaf'), edgeData('N.Root', 'N.Leaf')]);
      expect(() => packedLayout(core!, { unpacked: new Set(['N.Root']), unpackedLayout: layout })).not.toThrow();
      expect(Number.isFinite(core.getElementById('N.Leaf').position().x)).toBe(true);
    }
  });
});

describe('packedLayout: an unknown layout name', () => {
  it('falls back to Layered Right rather than leaving the nodes unplaced', () => {
    core = createCore([nodeData('N.Root'), nodeData('N.Leaf'), edgeData('N.Root', 'N.Leaf')]);
    packedLayout(core, { unpacked: new Set(['N.Root']), unpackedLayout: 'mystery' as never });
    expect(core.getElementById('N.Leaf').position().x)
      .toBeGreaterThan(core.getElementById('N.Root').position().x);
  });
});

describe('packedLayout: the island band', () => {
  const threeGroups = () => createCore([
    nodeData('N.A1'), nodeData('N.A2'), edgeData('N.A1', 'N.A2'),
    nodeData('N.B1'), nodeData('N.B2'), edgeData('N.B1', 'N.B2'),
    nodeData('N.C1'), nodeData('N.C2'), edgeData('N.C1', 'N.C2'),
  ]);

  it('draws island groups below every tiled group', () => {
    core = threeGroups();
    const result = packedLayout(core, { islands: new Set(['N.C1']) });

    const island = ['N.C1', 'N.C2'].map((id) => core!.getElementById(id).position().y);
    const tiled = ['N.A1', 'N.B1'].map((id) => core!.getElementById(id).position().y);
    expect(Math.min(...island)).toBeGreaterThan(Math.max(...tiled));
    expect(result.island).not.toBeNull();
  });

  it('reports the island rectangle around the nodes it holds', () => {
    core = threeGroups();
    const result = packedLayout(core, { islands: new Set(['N.C1']) });
    const positions = ['N.C1', 'N.C2'].map((id) => core!.getElementById(id).position());

    expect(result.island!.x).toBeLessThanOrEqual(Math.min(...positions.map((p) => p.x)));
    expect(result.island!.y).toBeLessThanOrEqual(Math.min(...positions.map((p) => p.y)));
    expect(result.island!.x + result.island!.width).toBeGreaterThanOrEqual(Math.max(...positions.map((p) => p.x)));
  });

  it('reports no island band when nothing is marked', () => {
    core = threeGroups();
    expect(packedLayout(core).island).toBeNull();
  });

  it('keeps an unpacked group above the tiles even when islands exist', () => {
    core = threeGroups();
    packedLayout(core, { unpacked: new Set(['N.B1']), islands: new Set(['N.C1']) });

    const unpacked = core.getElementById('N.B1').position().y;
    const tiled = core.getElementById('N.A1').position().y;
    const island = core.getElementById('N.C1').position().y;
    expect(unpacked).toBeLessThan(tiled);
    expect(tiled).toBeLessThan(island);
  });
});

describe('packedLayout: hubs', () => {
  /** A graph big enough for the hub threshold, with one type wired to everything. */
  const hubGraph = (): Core => {
    const elements = [nodeData('N.Hub')];
    for (let index = 0; index < 130; index += 1) {
      elements.push(nodeData(`N.Leaf${index}`), edgeData('N.Hub', `N.Leaf${index}`));
    }
    return createCore(elements);
  };

  it('marks the high-degree types so the layout decision is visible', () => {
    core = hubGraph();
    const result = packedLayout(core);
    expect(result.hubs).toContain('N.Hub');
    expect(core.getElementById('N.Hub').hasClass('hub')).toBe(true);
  });

  it('clears a stale mark on the next run', () => {
    core = createCore([nodeData('N.Root'), nodeData('N.Leaf'), edgeData('N.Root', 'N.Leaf')]);
    core.getElementById('N.Root').addClass('hub');
    packedLayout(core);
    expect(core.getElementById('N.Root').hasClass('hub')).toBe(false);
  });

  /** Two hubs wired to the same leaves share a centroid, which is where the collision happens. */
  const twinHubs = (): Core => {
    const elements = [nodeData('N.HubA'), nodeData('N.HubB')];
    for (let index = 0; index < 120; index += 1) {
      elements.push(
        nodeData(`N.Leaf${index}`),
        edgeData('N.HubA', `N.Leaf${index}`),
        edgeData('N.HubB', `N.Leaf${index}`),
      );
    }
    return createCore(elements);
  };

  it('moves a hub off a type it would otherwise land on top of', () => {
    core = twinHubs();
    const result = packedLayout(core);

    expect(result.hubs).toContain('N.HubA');
    expect(result.hubs).toContain('N.HubB');
    const a = core.getElementById('N.HubA').position();
    const b = core.getElementById('N.HubB').position();
    const apart = Math.abs(a.x - b.x) >= 20 || Math.abs(a.y - b.y) >= 20;
    expect(apart).toBe(true);
  });

  it('leaves a hub where it is when every neighbour sits in an unpacked band', () => {
    // With no tiled neighbour there is no centroid to move to; the hub must simply stay put rather
    // than be dragged into the band that owns the rest of the picture.
    const elements = [nodeData('N.Hub')];
    for (let index = 0; index < 120; index += 1) {
      elements.push(nodeData(`N.Leaf${index}`), edgeData('N.Hub', `N.Leaf${index}`));
      if (index > 0) {
        elements.push(edgeData(`N.Leaf${index - 1}`, `N.Leaf${index}`));
      }
    }
    core = createCore(elements);

    const roots = componentGroups(core).map((group) => group.root);
    expect(() => packedLayout(core!, { unpacked: new Set(roots) })).not.toThrow();
    expect(Number.isFinite(core.getElementById('N.Hub').position().x)).toBe(true);
  });
});

describe('packedGroups', () => {
  it('reports each group with the rectangle it occupies', () => {
    core = createCore([
      nodeData('N.A'), nodeData('N.B'), edgeData('N.A', 'N.B'),
      nodeData('N.C'),
    ]);
    packedLayout(core);
    const groups = packedGroups(core);

    expect(groups.map((group) => group.root)).toEqual(['N.A', 'N.C']);
    const first = groups[0];
    expect(first.nodeIds).toEqual(['N.A', 'N.B']);
    expect(first.box.width).toBeGreaterThan(0);
    expect(first.box.height).toBeGreaterThan(0);
  });

  it('reads the positions already on the canvas rather than laying anything out', () => {
    core = createCore([nodeData('N.A'), nodeData('N.B'), edgeData('N.A', 'N.B')]);
    core.getElementById('N.A').position({ x: 100, y: 200 });
    core.getElementById('N.B').position({ x: 300, y: 200 });

    const box = packedGroups(core)[0].box;
    expect(box.x).toBeLessThan(100);
    expect(box.x + box.width).toBeGreaterThan(300);
  });

  it('reports nothing for an empty canvas', () => {
    core = createCore([]);
    expect(packedGroups(core)).toEqual([]);
  });
});
