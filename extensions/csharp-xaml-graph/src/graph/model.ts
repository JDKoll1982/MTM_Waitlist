/** Node layers used for colour coding and filtering in the webview. */
export type NodeLayer = 'view' | 'viewmodel' | 'service' | 'model' | 'external';

export interface SourceLocation {
  /** Workspace-relative path, forward slashes. */
  file: string;
  /** 1-based line number. */
  line: number;
}

export interface GraphNode {
  id: string;
  /** Short name shown on the node. */
  label: string;
  fullName: string;
  layer: NodeLayer;
  /** Top-level module bucket, used for compound grouping. */
  module: string;
  /**
   * How the type was declared, or for a view the root element that makes it one:
   * `class | record | struct | interface | enum` for C#, and the lower-cased XAML root tag for a view
   * (`page`, `usercontrol`, `window`, `windowex`, `application`, `resourcestyledictionary`, …).
   */
  kind: string;
  location?: SourceLocation;
  /** XAML file that declares this view, when the node is a view. */
  xamlFile?: string;
  interfaces?: string[];
  baseType?: string;
  /** View-model property names declared by a view, e.g. ["ViewModel"], ["Panel", "PasswordChange"]. */
  viewModelProperties?: string[];
  /** Names of view models whose resolution was inferred from XAML bindings only. */
  boundViewModelProperties?: string[];
  /** True when the type is registered with the DI container. */
  diRegistered?: boolean;
  /** True when the type participates in a dependency cycle. */
  inCycle?: boolean;
  /** Number of cleanup findings that point at this node. */
  problemCount?: number;
}

export type EdgeKind =
  /** pageService.Configure<TViewModel, TPage>() */
  | 'resolves'
  /** App.GetService<T>() / GetRequiredService<T>() inside a view */
  | 'getservice'
  /** constructor parameter typed as a view model */
  | 'injects'
  /** readonly field assignment of a view model */
  | 'field'
  /** new XViewModel(...) */
  | 'constructs'
  /** x:Bind <root>.Member confirmed a view-model property */
  | 'binds'
  | 'implements'
  | 'inherits'
  /** a view model / service holds another as a property or constructs it */
  | 'composes'
  /**
   * A plain type reference: a property, field, return type or parameter typed as another workspace
   * type, including generic arguments. These carry no DI meaning, but they are what makes structural
   * cycles visible — a class holding another as a property draws nothing without them.
   */
  | 'reference'
  /** DataTemplate x:DataType row model (informational only) */
  | 'template';

export type EdgeConfidence = 'high' | 'medium' | 'low';

export interface GraphEdge {
  id: string;
  source: string;
  target: string;
  kind: EdgeKind;
  confidence: EdgeConfidence;
  label?: string;
  evidence?: SourceLocation;
}

/* ------------------------------------------------------------------ findings */

export type FindingKind =
  /** Types that depend on each other in a loop. */
  | 'cycle'
  /** An x:Bind path that does not resolve against the type it is bound to. */
  | 'dead-binding'
  /** A view whose markup binds a view model it never sets. */
  | 'unbound-view'
  /** A declared type nothing in the workspace references. */
  | 'unused-type'
  /** A private member nothing in the workspace references. */
  | 'unused-member';

export type FindingSeverity = 'high' | 'medium' | 'low';

export interface Finding {
  id: string;
  kind: FindingKind;
  severity: FindingSeverity;
  /** One line, shown in the list. */
  title: string;
  /** The supporting sentence: why this was flagged. */
  detail: string;
  /** Nodes to highlight when the finding is selected. */
  nodeIds: string[];
  /** Edge ids that form the cycle, when the finding is a cycle. */
  edgeIds?: string[];
  location?: SourceLocation;
}

export interface CleanupSummary {
  cycles: number;
  deadBindings: number;
  unboundViews: number;
  unusedTypes: number;
  unusedMembers: number;
}

/**
 * Counts from the parse and resolution passes. These are deliberately separate from the graph: they
 * say what the analyzer *saw* and what it could not place, which is exactly what you need when an edge
 * or a finding is missing.
 */
export interface ParserTally {
  typesDeclared: number;
  /** Types declared in more than one file (partials). */
  partialTypes: number;
  /** Simple names carried by more than one type, so name resolution cannot be a guess. */
  ambiguousSimpleNames: number;
  namespaces: number;
  modules: number;
  /** XAML files with an `x:Class`, and files without one — the usual cause of a missing view node. */
  xamlWithClass: number;
  xamlWithoutClass: number;
  bindings: number;
  templateBindings: number;
  /** `{Binding}` / `{TemplateBinding}` expressions, which bind against a run-time DataContext. */
  classicBindings: number;
  elementTags: number;
  handlerNames: number;
  dataTypes: number;
  /** Edge attempts rejected because they were duplicates, self-references or unnamed. */
  edgesDropped: number;
}

export interface GraphStats {
  filesAnalysed: number;
  csharpFiles: number;
  xamlFiles: number;
  parseErrors: number;
  parseWarnings: number;
  elapsedMs: number;
  truncated: boolean;
  parser?: ParserTally;
}

export interface GraphModel {
  workspace: string;
  generatedAt: string;
  nodes: GraphNode[];
  edges: GraphEdge[];
  findings: Finding[];
  cleanup: CleanupSummary;
  stats: GraphStats;
  warnings: string[];
}

export const EDGE_KIND_LABEL: Record<EdgeKind, string> = {
  resolves: 'DI registration',
  getservice: 'service locator',
  injects: 'constructor injection',
  field: 'field-backed',
  constructs: 'constructed by',
  binds: 'XAML binding',
  implements: 'implements',
  inherits: 'inherits',
  composes: 'composes',
  reference: 'type reference',
  template: 'template row model',
};

/** Every edge kind, in the order the filter checkboxes list it. */
export const EDGE_KINDS: EdgeKind[] = [
  'resolves', 'getservice', 'binds', 'injects', 'field', 'constructs', 'composes', 'reference', 'inherits', 'implements',
  'template',
];

export const NODE_LAYERS: NodeLayer[] = ['view', 'viewmodel', 'service', 'model', 'external'];

export const FINDING_LABEL: Record<FindingKind, string> = {
  cycle: 'Cycle',
  'dead-binding': 'Dead binding',
  'unbound-view': 'Unbound view',
  'unused-type': 'Unused type',
  'unused-member': 'Unused member',
};
