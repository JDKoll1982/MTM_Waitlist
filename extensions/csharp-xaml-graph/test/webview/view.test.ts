import { describe, expect, it } from 'vitest';
import { computeVisible, moduleSummary, type ViewOptions } from '../../src/webview/client/view';
import { graphEdge, graphModel, graphNode } from '../support/model';

const model = graphModel({
  nodes: [
    graphNode({ id: 'A.View', label: 'View', layer: 'view', module: 'Module_A' }),
    graphNode({ id: 'A.ViewModel', label: 'ViewModel', layer: 'viewmodel', module: 'Core' }),
    graphNode({ id: 'A.Service', label: 'Service', layer: 'service', module: 'Core' }),
    graphNode({ id: 'A.Model', label: 'Model', layer: 'model', module: 'Core', problemCount: 1 }),
    graphNode({ id: 'A.Lonely', label: 'Lonely', layer: 'viewmodel', module: 'Core' }),
  ],
  edges: [
    graphEdge({ id: 'e1', source: 'A.View', target: 'A.ViewModel', kind: 'binds' }),
    graphEdge({ id: 'e2', source: 'A.ViewModel', target: 'A.Service', kind: 'injects' }),
    graphEdge({ id: 'e3', source: 'A.ViewModel', target: 'A.Model', kind: 'reference' }),
  ],
});

const options = (partial: Partial<ViewOptions> = {}): ViewOptions => ({
  layers: ['view', 'viewmodel', 'service', 'model'],
  edgeKinds: ['binds', 'injects', 'reference'],
  module: 'all',
  search: '',
  focus: null,
  focusDepth: 1,
  hideOrphans: false,
  onlyProblems: false,
  scope: null,
  scopeExact: false,
  ...partial,
});

const visible = (partial: Partial<ViewOptions> = {}) => computeVisible(model, options(partial));

describe('computeVisible: layers, search and problems', () => {
  it('keeps only nodes in the enabled layers', () => {
    expect([...visible({ layers: ['view'] }).nodeIds]).toEqual(['A.View']);
  });

  it('drops the edges whose nodes are not drawn', () => {
    expect(visible({ layers: ['view'] }).edges).toEqual([]);
  });

  it('matches the search against the label, the full name and the module, case-insensitively', () => {
    expect([...visible({ search: 'VIEWM' }).nodeIds]).toEqual(['A.ViewModel']);
    expect([...visible({ search: 'a.service' }).nodeIds]).toEqual(['A.Service']);
    expect([...visible({ search: 'module_a' }).nodeIds]).toEqual(['A.View']);
  });

  it('ignores surrounding whitespace in the search', () => {
    expect([...visible({ search: '  Lonely  ' }).nodeIds]).toEqual(['A.Lonely']);
  });

  it('restricts to flagged nodes when Only problems is on', () => {
    expect([...visible({ onlyProblems: true }).nodeIds]).toEqual(['A.Model']);
  });

  it('keeps a node that is in a cycle even with no problem count', () => {
    const cycled = graphModel({
      nodes: [graphNode({ id: 'A.X', inCycle: true })],
    });
    expect(computeVisible(cycled, options({ onlyProblems: true })).nodeIds.has('A.X')).toBe(true);
  });
});

describe('computeVisible: module scope', () => {
  it('treats an empty module as no filter', () => {
    // The module select reads as empty until the list is populated; a scope of '' used to match no
    // node at all, which emptied the canvas instead of showing the whole graph.
    expect([...visible({ module: '' }).nodeIds].sort()).toEqual([...visible({ module: 'all' }).nodeIds].sort());
  });

  it('keeps one hop of partners outside the module', () => {
    const result = visible({ module: 'Module_A' });
    expect([...result.nodeIds].sort()).toEqual(['A.View', 'A.ViewModel']);
  });

  it('does not reach partners that the layer filter removed', () => {
    const result = visible({ module: 'Module_A', layers: ['view'] });
    expect([...result.nodeIds]).toEqual(['A.View']);
  });

  it('drops everything when the module names nothing in the model', () => {
    expect(visible({ module: 'Missing' }).nodeIds.size).toBe(0);
  });
});

describe('computeVisible: focus', () => {
  it('keeps the focused node and the requested number of hops', () => {
    expect([...visible({ focus: 'A.ViewModel', focusDepth: 1 }).nodeIds].sort())
      .toEqual(['A.Model', 'A.Service', 'A.View', 'A.ViewModel']);
  });

  it('reaches further at depth 2', () => {
    expect([...visible({ focus: 'A.View', focusDepth: 2 }).nodeIds].sort())
      .toEqual(['A.Model', 'A.Service', 'A.View', 'A.ViewModel']);
  });

  it('treats a depth below one as one hop', () => {
    expect([...visible({ focus: 'A.View', focusDepth: 0 }).nodeIds].sort())
      .toEqual(['A.View', 'A.ViewModel']);
  });

  it('adds the focus node even when the layer filter excluded it', () => {
    const result = visible({ focus: 'A.Service', layers: ['view'], edgeKinds: ['binds'] });
    expect([...result.nodeIds]).toEqual(['A.Service']);
  });

  it('drops candidates the focus cannot reach', () => {
    const result = visible({ focus: 'A.Lonely' });
    expect([...result.nodeIds]).toEqual(['A.Lonely']);
  });
});

describe('computeVisible: a symbol scope', () => {
  it('keeps the scope and one hop of partners by default', () => {
    const result = visible({ scope: ['A.View'] });
    expect([...result.nodeIds].sort()).toEqual(['A.View', 'A.ViewModel']);
  });

  it('draws the scope exactly when asked, adding no partner', () => {
    const result = visible({ scope: ['A.View'], scopeExact: true });
    expect([...result.nodeIds]).toEqual(['A.View']);
    expect(result.edges).toEqual([]);
  });

  it('ignores ids the model does not know', () => {
    const result = visible({ scope: ['A.View', 'Ghost'], scopeExact: true });
    expect([...result.nodeIds]).toEqual(['A.View']);
  });

  it('treats an empty scope as no scope', () => {
    expect(visible({ scope: [] }).nodeIds.size).toBe(5);
  });

  it('adds every scoped node back even when the layer filter excluded it', () => {
    const result = visible({ scope: ['A.Service'], scopeExact: true, layers: ['view'] });
    expect([...result.nodeIds]).toEqual(['A.Service']);
  });
});

describe('computeVisible: hide unconnected', () => {
  const orphanModel = graphModel({
    nodes: [
      graphNode({ id: 'S.Used', layer: 'service' }),
      graphNode({ id: 'S.Orphan', layer: 'service' }),
      graphNode({ id: 'S.Caller', layer: 'service' }),
      graphNode({ id: 'M.OrphanModel', layer: 'model' }),
    ],
    edges: [graphEdge({ source: 'S.Caller', target: 'S.Used', kind: 'injects' })],
  });

  it('removes a service with no edge in the current view and counts it', () => {
    const result = computeVisible(orphanModel, options({ hideOrphans: true, layers: ['service'] }));
    expect([...result.nodeIds].sort()).toEqual(['S.Caller', 'S.Used']);
    expect(result.hiddenOrphans).toBe(1);
  });

  it('removes an unconnected model too, so only the types that serve something are left', () => {
    const result = computeVisible(orphanModel, options({ hideOrphans: true, layers: ['service', 'model'] }));
    expect(result.nodeIds.has('M.OrphanModel')).toBe(false);
    expect(result.hiddenOrphans).toBe(2);
  });

  it('keeps a view or a view model that has no edge in the current view', () => {
    // Its link to its partner may be a kind that is filtered out; a screen is still a screen, and
    // pruning it as "unconnected" removes real structure from the picture.
    const result = visible({ hideOrphans: true, layers: ['view', 'viewmodel'], edgeKinds: [] });
    expect([...result.nodeIds].sort()).toEqual(['A.Lonely', 'A.View', 'A.ViewModel']);
    expect(result.hiddenOrphans).toBe(0);
  });

  it('keeps a flagged node, because the finding is why the panel was opened', () => {
    const result = visible({ hideOrphans: true, layers: ['model'] });
    expect([...result.nodeIds]).toEqual(['A.Model']);
    expect(result.hiddenOrphans).toBe(0);
  });

  it('keeps the focused node even with nothing attached', () => {
    const result = visible({ hideOrphans: true, focus: 'A.Lonely' });
    expect(result.nodeIds.has('A.Lonely')).toBe(true);
  });

  it('keeps a scoped node even with nothing attached', () => {
    const result = visible({ hideOrphans: true, scope: ['A.Lonely'], scopeExact: true });
    expect(result.nodeIds.has('A.Lonely')).toBe(true);
  });

  it('never leaves an edge pointing at a node it removed', () => {
    const result = visible({ hideOrphans: true });
    for (const edge of result.edges) {
      expect(result.nodeIds.has(edge.source) && result.nodeIds.has(edge.target)).toBe(true);
    }
  });
});

describe('moduleSummary', () => {
  it('counts the nodes in each module and sorts by name', () => {
    expect(moduleSummary(model)).toEqual([
      { name: 'Core', count: 4 },
      { name: 'Module_A', count: 1 },
    ]);
  });
});

describe('computeVisible: a node with no adjacency entry', () => {
  it('keeps a focused node that has no partner at all', () => {
    const lonely = graphModel({ nodes: [graphNode({ id: 'N.Lonely', label: 'Lonely', layer: 'view' })] });
    const result = computeVisible(lonely, options({ focus: 'N.Lonely' }));
    expect([...result.nodeIds]).toEqual(['N.Lonely']);
  });

  it('keeps a module’s node that has no partner at all', () => {
    const lonely = graphModel({ nodes: [graphNode({ id: 'N.Lonely', label: 'Lonely', layer: 'view', module: 'Only' })] });
    const result = computeVisible(lonely, options({ module: 'Only' }));
    expect([...result.nodeIds]).toEqual(['N.Lonely']);
  });
});

describe('computeVisible: a scope that is not exact', () => {
  it('keeps the partners of a scoped type', () => {
    const result = computeVisible(model, options({ scope: ['A.View'], focusDepth: 1 }));
    expect([...result.nodeIds].sort()).toEqual(['A.View', 'A.ViewModel']);
  });

  it('keeps only the scoped type when the scope is exact', () => {
    const result = computeVisible(model, options({ scope: ['A.View'], scopeExact: true }));
    expect([...result.nodeIds]).toEqual(['A.View']);
  });
});
