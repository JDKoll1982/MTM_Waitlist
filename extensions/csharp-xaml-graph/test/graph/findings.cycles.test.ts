import { describe, expect, it } from 'vitest';
import { computeFindings, type FindingInput } from '../../src/graph/findings';
import { TypeIndex } from '../../src/graph/csharp/symbols';
import type { GraphEdge, GraphNode } from '../../src/graph/model';
import type { TypeDecl } from '../../src/graph/csharp/parser';
import { compileConventions, type CompiledConventions } from '../../src/graph/conventions';
import type { XamlFile } from '../../src/graph/xaml/parser';
import { finding, graphEdge, graphNode, typeDecl } from '../support/model';

interface Options {
  nodes?: GraphNode[];
  edges?: GraphEdge[];
  decls?: TypeDecl[];
  files?: Record<string, string>;
  declarationCounts?: Record<string, number>;
  xamlFiles?: XamlFile[];
  sources?: string[];
  /** The conventions the rules are judged by. Absent means the built-in conventions. */
  conventions?: CompiledConventions;
}

function makeInput(options: Options = {}): FindingInput {
  const decls = new Map<string, TypeDecl>();
  for (const decl of options.decls ?? []) {
    decls.set(decl.fullName, decl);
  }

  const index = new TypeIndex();
  for (const decl of decls.values()) {
    index.add(decl);
  }

  const files = new Map<string, string>();
  for (const name of decls.keys()) {
    files.set(name, options.files?.[name] ?? `src/${name.split('.').pop()}.cs`);
  }
  for (const [name, file] of Object.entries(options.files ?? {})) {
    files.set(name, file);
  }

  const declarationCounts = new Map<string, number>();
  for (const name of decls.keys()) {
    declarationCounts.set(name, options.declarationCounts?.[name] ?? 1);
  }

  return {
    nodes: options.nodes ?? [...decls.keys()].map((id) => graphNode({ id, layer: 'model' })),
    edges: options.edges ?? [],
    decls,
    files,
    declarationCounts,
    index,
    usingsFor: () => [],
    xamlFiles: options.xamlFiles ?? [],
    sources: options.sources ?? [],
    conventions: options.conventions ?? compileConventions(),
  };
}

const xamlFile = (partial: Partial<XamlFile> & { path: string }): XamlFile => ({
  className: null,
  rootTag: 'Page',
  elementNames: [],
  elementTags: [],
  dataTypes: [],
  bindings: [],
  xmlnsUsing: {},
  handlerNames: [],
  ...partial,
});

const bindingsOf = (input: FindingInput) => {
  const { findings } = computeFindings(input);
  return findings.filter((entry) => entry.kind === 'dead-binding');
};

describe('computeFindings: cycles', () => {
  const twoNodes = (kind: GraphEdge['kind'], confidence: GraphEdge['confidence'] = 'high') => makeInput({
    decls: [typeDecl({ name: 'A', namespace: 'N' }), typeDecl({ name: 'B', namespace: 'N' })],
    edges: [
      graphEdge({ source: 'N.A', target: 'N.B', kind, confidence }),
      graphEdge({ source: 'N.B', target: 'N.A', kind, confidence }),
    ],
  });

  it('reports a mutual construction dependency as a cycle that blocks construction', () => {
    const { findings, inCycle } = computeFindings(twoNodes('injects'));

    expect(findings).toHaveLength(1);
    expect(findings[0]).toMatchObject({
      kind: 'cycle',
      severity: 'high',
      title: 'Cycle of 2: A ↔ B',
    });
    expect(findings[0].detail).toContain('form a loop');
    expect(findings[0].detail).toContain('those construction edges form a loop of their own');
    expect(findings[0].edgeIds).toHaveLength(2);
    expect([...inCycle].sort()).toEqual(['N.A', 'N.B']);
  });

  it('does not claim construction is blocked when only one direction constructs', () => {
    const input = makeInput({
      decls: [typeDecl({ name: 'A', namespace: 'N' }), typeDecl({ name: 'B', namespace: 'N' })],
      edges: [
        graphEdge({ source: 'N.A', target: 'N.B', kind: 'injects' }),
        graphEdge({ source: 'N.B', target: 'N.A', kind: 'reference', confidence: 'medium' }),
      ],
    });
    const { findings } = computeFindings(input);

    expect(findings[0].severity).toBe('medium');
    expect(findings[0].detail).toContain('do not themselves form a loop');
  });

  it('describes a reference-only loop as a mutual dependency, not a build failure', () => {
    const { findings } = computeFindings(twoNodes('reference', 'medium'));

    expect(findings[0].severity).toBe('medium');
    expect(findings[0].detail).toContain('all type references');
    expect(findings[0].detail).toContain('mutual dependency');
  });

  it('reports a loop closed by weak references separately, as a low-severity knot', () => {
    const { findings } = computeFindings(twoNodes('reference', 'low'));

    expect(findings).toHaveLength(1);
    expect(findings[0]).toMatchObject({ kind: 'cycle', severity: 'low', title: 'Cycle of 2: A ↔ B' });
    expect(findings[0].detail).toContain('low-confidence references');
  });

  it('reports a self-dependency of a single type', () => {
    const input = makeInput({
      decls: [typeDecl({ name: 'A', namespace: 'N' })],
      edges: [graphEdge({ source: 'N.A', target: 'N.A', kind: 'injects' })],
    });
    const { findings } = computeFindings(input);

    expect(findings).toHaveLength(1);
    expect(findings[0]).toMatchObject({
      kind: 'cycle',
      severity: 'high',
      title: 'Self-dependency: A',
      detail: 'The type depends on itself.',
    });
  });

  it('ignores a component of one type with no self edge', () => {
    const input = makeInput({
      decls: [typeDecl({ name: 'A', namespace: 'N' })],
      edges: [graphEdge({ source: 'N.A', target: 'N.B', kind: 'injects' })],
    });
    expect(computeFindings(input).findings.filter((entry) => entry.kind === 'cycle')).toEqual([]);
  });

  it('does not report the same group twice when the reference pass adds nothing', () => {
    const { findings } = computeFindings(twoNodes('injects'));
    expect(findings.filter((entry) => entry.kind === 'cycle')).toHaveLength(1);
  });

  it('samples a large reference knot and says so', () => {
    const count = 34;
    const decls = Array.from({ length: count }, (_, index) => typeDecl({ name: `T${String(index).padStart(2, '0')}`, namespace: 'N' }));
    const edges = decls.map((decl, index) => graphEdge({
      source: decl.fullName,
      target: decls[(index + 1) % count].fullName,
      kind: 'reference',
      confidence: 'low',
    }));

    const input = makeInput({ decls, edges });
    const { findings } = computeFindings(input);

    expect(findings).toHaveLength(1);
    expect(findings[0].severity).toBe('low');
    expect(findings[0].title).toBe(
      'Mutual references across 34 types: T00 ↔ T01 ↔ T02 ↔ T03 …',
    );
    expect(findings[0].nodeIds).toHaveLength(30);
    expect(findings[0].detail).toContain('Only 30 members are listed');
    // Edges pointing outside the sample are not attached to the finding.
    expect(findings[0].edgeIds!.every((id) => {
      const edge = edges.find((candidate) => candidate.id === id)!;
      return findings[0].nodeIds.includes(edge.source) && findings[0].nodeIds.includes(edge.target);
    })).toBe(true);
  });

  it('titles a small reference knot with its whole member list', () => {
    const decls = ['A', 'B', 'C'].map((name) => typeDecl({ name, namespace: 'N' }));
    const edges = [
      graphEdge({ source: 'N.A', target: 'N.B', kind: 'reference', confidence: 'low' }),
      graphEdge({ source: 'N.B', target: 'N.C', kind: 'reference', confidence: 'low' }),
      graphEdge({ source: 'N.C', target: 'N.A', kind: 'reference', confidence: 'low' }),
    ];
    const { findings } = computeFindings(makeInput({ decls, edges }));
    expect(findings[0].title).toBe('Cycle of 3: A ↔ B ↔ C');
  });

  it('carries the location of the component member Tarjan completes first', () => {
    const { findings } = computeFindings(twoNodes('injects'));
    expect(findings[0].location).toEqual({ file: 'src/B.cs', line: 1 });
  });

  it('ignores an edge whose endpoints are not both nodes', () => {
    const input = makeInput({
      decls: [typeDecl({ name: 'A', namespace: 'N' }), typeDecl({ name: 'B', namespace: 'N' })],
      edges: [
        graphEdge({ source: 'N.A', target: 'N.B', kind: 'injects' }),
        graphEdge({ source: 'N.B', target: 'N.A', kind: 'injects' }),
        graphEdge({ source: 'N.A', target: 'Missing', kind: 'injects' }),
      ],
    });
    expect(computeFindings(input).findings[0].edgeIds).toHaveLength(2);
  });
});


describe('computeFindings: cycles are unaffected by how types are classified', () => {
  /** A fixed fixture: one real construction loop and one acyclic branch beside it. */
  const fixture = () => ({
    nodes: ['A', 'B', 'C', 'D'].map((id) => graphNode({ id, layer: 'service' as const })),
    edges: [
      graphEdge({ id: 'e1', source: 'A', target: 'B', kind: 'injects' }),
      graphEdge({ id: 'e2', source: 'B', target: 'C', kind: 'injects' }),
      graphEdge({ id: 'e3', source: 'C', target: 'A', kind: 'injects' }),
      graphEdge({ id: 'e4', source: 'A', target: 'D', kind: 'injects' }),
    ],
  });

  const cycleShape = (options: Parameters<typeof makeInput>[0]) =>
    computeFindings(makeInput(options)).findings
      .filter((entry) => entry.kind === 'cycle')
      .map((entry) => `${entry.title}|${[...entry.nodeIds].sort().join(',')}`);

  it('reports the same cycles with a built-in profile as with none', () => {
    expect(cycleShape({ ...fixture(), conventions: compileConventions() }))
      .toEqual(cycleShape(fixture()));
  });

  it('leaves cycle detection alone when a profile reclassifies the types in the loop', () => {
    const before = cycleShape(fixture());
    const after = cycleShape({
      ...fixture(),
      nodes: ['A', 'B', 'C', 'D'].map((id) => graphNode({ id, layer: 'viewmodel' as const })),
      conventions: compileConventions({
        version: 1,
        layers: { rules: [{ layer: 'viewmodel', nameSuffixes: [] }], default: 'viewmodel' },
      }),
    });
    // Edges drive the report, not layers: the same three types are in the same one loop.
    expect(after).toEqual(before);
    expect(after).toHaveLength(1);
    expect(after[0]).toContain('A,B,C');
  });
});
