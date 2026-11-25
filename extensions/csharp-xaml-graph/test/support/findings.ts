import { TypeIndex } from '../../src/graph/csharp/symbols';
import type { FindingInput } from '../../src/graph/findings';
import type { GraphEdge, GraphNode } from '../../src/graph/model';
import type { TypeDecl } from '../../src/graph/csharp/parser';
import type { XamlBinding, XamlFile } from '../../src/graph/xaml/parser';
import { graphNode } from './model';

export interface FindingInputOptions {
  nodes?: GraphNode[];
  edges?: GraphEdge[];
  decls?: TypeDecl[];
  /** Overrides the derived `src/<Name>.cs` for a type, or adds a mapping for one. */
  files?: Record<string, string>;
  declarationCounts?: Record<string, number>;
  xamlFiles?: XamlFile[];
  sources?: string[];
  usings?: Record<string, string[]>;
}

/** A `FindingInput` with every field filled, so a test states only what it is about. */
export function makeFindingInput(options: FindingInputOptions = {}): FindingInput {
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
    files.set(name, `src/${name.split('.').pop()}.cs`);
  }
  for (const [name, file] of Object.entries(options.files ?? {})) {
    files.set(name, file);
  }

  const declarationCounts = new Map<string, number>();
  for (const name of decls.keys()) {
    declarationCounts.set(name, options.declarationCounts?.[name] ?? 1);
  }

  return {
    nodes: options.nodes ?? [...decls.keys()].map((id) => graphNode({ id })),
    edges: options.edges ?? [],
    decls,
    files,
    declarationCounts,
    index,
    usingsFor: (fullName: string) => options.usings?.[fullName] ?? [],
    xamlFiles: options.xamlFiles ?? [],
    sources: options.sources ?? [],
  };
}

/** A `XamlFile` with every field filled. */
export function xamlFile(partial: Partial<XamlFile> & { path: string }): XamlFile {
  return {
    className: null,
    rootTag: 'Page',
    elementNames: [],
    elementTags: [],
    dataTypes: [],
    bindings: [],
    xmlnsUsing: {},
    handlerNames: [],
    ...partial,
  };
}

/** A `XamlBinding` with every field filled; `x:Bind` at page level by default. */
export function xamlBinding(partial: Partial<XamlBinding> & { expression: string }): XamlBinding {
  return {
    dialect: 'x:Bind',
    root: null,
    path: [],
    line: 10,
    inTemplate: false,
    templateDataType: null,
    templateTypeNamespace: null,
    ...partial,
  };
}
