import type {
  CleanupSummary,
  Finding,
  GraphEdge,
  GraphModel,
  GraphNode,
  ParserTally,
} from '../../src/graph/model';
import type { TypeDecl } from '../../src/graph/csharp/parser';

/** Builds a complete `TypeDecl` from the fields a test cares about. */
export function typeDecl(partial: Partial<TypeDecl> & { name: string }): TypeDecl {
  const namespace = partial.namespace ?? '';
  return {
    namespace,
    fullName: partial.fullName ?? (namespace ? `${namespace}.${partial.name}` : partial.name),
    kind: 'class',
    line: 1,
    isPartial: false,
    isAbstract: false,
    bases: [],
    constructors: [],
    properties: [],
    fields: [],
    methods: [],
    assignments: [],
    calls: [],
    creations: [],
    typeofRefs: [],
    mentions: [],
    ...partial,
  };
}

/** Builds a complete `GraphNode` from the fields a test cares about. */
export function graphNode(partial: Partial<GraphNode> & { id: string }): GraphNode {
  const label = partial.label ?? partial.id.slice(partial.id.lastIndexOf('.') + 1);
  return {
    label,
    fullName: partial.fullName ?? partial.id,
    layer: 'model',
    module: 'Module',
    kind: 'class',
    ...partial,
  };
}

export const EMPTY_TALLY: ParserTally = {
  typesDeclared: 0,
  partialTypes: 0,
  ambiguousSimpleNames: 0,
  namespaces: 0,
  modules: 0,
  xamlWithClass: 0,
  xamlWithoutClass: 0,
  bindings: 0,
  templateBindings: 0,
  classicBindings: 0,
  elementTags: 0,
  handlerNames: 0,
  dataTypes: 0,
  edgesDropped: 0,
  edgesMerged: 0,
};

export const EMPTY_CLEANUP: CleanupSummary = {
  cycles: 0,
  deadBindings: 0,
  unboundViews: 0,
  unusedTypes: 0,
  unusedMembers: 0,
};

/** Builds a complete `GraphModel`, so a test only states what it is about. */
export function graphModel(partial: Partial<GraphModel> = {}): GraphModel {
  return {
    workspace: '/workspace',
    generatedAt: '2026-01-01T00:00:00.000Z',
    nodes: [],
    edges: [],
    findings: [],
    cleanup: { ...EMPTY_CLEANUP },
    stats: {
      filesAnalysed: 0,
      csharpFiles: 0,
      xamlFiles: 0,
      parseErrors: 0,
      parseWarnings: 0,
      elapsedMs: 5,
      truncated: false,
      parser: { ...EMPTY_TALLY },
    },
    warnings: [],
    ...partial,
  };
}

/** Builds a complete `GraphEdge`, deriving the id from its endpoints and kind. */
export function graphEdge(partial: Partial<GraphEdge> & { source: string; target: string }): GraphEdge {
  const kind = partial.kind ?? 'reference';
  return { id: partial.id ?? `${partial.source}->${partial.target}:${kind}`, kind, confidence: 'high', ...partial } as GraphEdge;
}

/** Builds a complete `Finding`. */
export function finding(partial: Partial<Finding> & { kind: Finding['kind']; title: string }): Finding {
  return {
    id: partial.id ?? `f_${partial.kind}_${partial.title}`,
    severity: 'medium',
    detail: partial.title,
    nodeIds: [],
    ...partial,
  };
}
