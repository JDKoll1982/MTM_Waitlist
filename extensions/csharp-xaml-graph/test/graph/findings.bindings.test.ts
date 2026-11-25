import { describe, expect, it } from 'vitest';
import { computeFindings, type FindingInput } from '../../src/graph/findings';
import { TypeIndex } from '../../src/graph/csharp/symbols';
import type { TypeDecl } from '../../src/graph/csharp/parser';
import type { XamlFile, XamlBinding } from '../../src/graph/xaml/parser';
import type { GraphNode } from '../../src/graph/model';
import { graphNode, typeDecl } from '../support/model';

function input(options: {
  decls: TypeDecl[];
  xamlFiles?: XamlFile[];
  nodes?: GraphNode[];
}): FindingInput {
  const decls = new Map(options.decls.map((decl) => [decl.fullName, decl]));
  const index = new TypeIndex();
  for (const decl of decls.values()) {
    index.add(decl);
  }
  const files = new Map([...decls.keys()].map((name) => [name, `src/${name.split('.').pop()}.cs`]));
  const declarationCounts = new Map([...decls.keys()].map((name) => [name, 1]));

  return {
    nodes: options.nodes ?? [...decls.keys()].map((id) => graphNode({ id })),
    edges: [],
    decls,
    files,
    declarationCounts,
    index,
    usingsFor: () => [],
    xamlFiles: options.xamlFiles ?? [],
    sources: [],
  };
}

const xaml = (partial: Partial<XamlFile> & { path: string }): XamlFile => ({
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

const binding = (partial: Partial<XamlBinding> & { expression: string }): XamlBinding => ({
  dialect: 'x:Bind',
  root: null,
  path: [],
  line: 10,
  inTemplate: false,
  templateDataType: null,
  templateTypeNamespace: null,
  ...partial,
});

const dead = (findInput: FindingInput) =>
  computeFindings(findInput).findings.filter((entry) => entry.kind === 'dead-binding');

const page = typeDecl({
  name: 'SamplePage',
  namespace: 'N.Views',
  properties: [
    { name: 'ViewModel', type: 'SampleViewModel', rawType: 'SampleViewModel', line: 3, isObservable: false, isPublic: true, visibility: 'public', attributes: [] },
    { name: 'Row', type: 'Cell', rawType: 'Cell', line: 4, isObservable: false, isPublic: true, visibility: 'public', attributes: [] },
  ],
  methods: [
    { name: 'SaveAsync', returnType: 'Task', rawReturnType: 'Task', rawParamTypes: [], line: 6, visibility: 'private', attributes: ['RelayCommand'], isOverride: false, isStatic: false, isAsync: true, isPartial: false, isExtension: false },
    { name: 'OnClick', returnType: 'void', rawReturnType: 'void', rawParamTypes: [], line: 7, visibility: 'private', attributes: [], isOverride: false, isStatic: false, isAsync: false, isPartial: false, isExtension: false },
  ],
  fields: [
    { name: '_title', type: 'string', rawType: 'string', line: 8, isReadonly: false, visibility: 'private', attributes: ['ObservableProperty'] },
    { name: '_hidden', type: 'string', rawType: 'string', line: 9, isReadonly: false, visibility: 'private', attributes: [] },
  ],
});

const viewModel = typeDecl({
  name: 'SampleViewModel',
  namespace: 'N.ViewModels',
  properties: [
    { name: 'Status', type: 'string', rawType: 'string', line: 3, isObservable: true, isPublic: true, visibility: 'public', attributes: [] },
    { name: 'Cells', type: 'Cell', rawType: 'Cell', line: 4, isObservable: false, isPublic: true, visibility: 'public', attributes: [] },
  ],
});

const cell = typeDecl({
  name: 'Cell',
  namespace: 'N.Models',
  properties: [
    { name: 'Title', type: 'string', rawType: 'string', line: 3, isObservable: false, isPublic: true, visibility: 'public', attributes: [] },
  ],
});

const baseDecls = [page, viewModel, cell];

describe('computeFindings: page-level dead bindings', () => {
  it('accepts a root that the view declares', () => {
    const findings = dead(input({
      decls: baseDecls,
      xamlFiles: [xaml({ path: 'Views/SamplePage.xaml', className: 'N.Views.SamplePage', bindings: [binding({ expression: 'ViewModel', root: 'ViewModel' })] })],
    }));
    expect(findings).toEqual([]);
  });

  it('accepts a root the MVVM generators create', () => {
    const findings = dead(input({
      decls: baseDecls,
      xamlFiles: [xaml({
        path: 'Views/SamplePage.xaml',
        className: 'N.Views.SamplePage',
        bindings: [
          binding({ expression: 'SaveCommand', root: 'SaveCommand' }),
          binding({ expression: 'Title', root: 'Title' }),
        ],
      })],
    }));
    expect(findings).toEqual([]);
  });

  it('reports a missing root as a high-severity dead binding', () => {
    const findings = dead(input({
      decls: baseDecls,
      xamlFiles: [xaml({ path: 'Views/SamplePage.xaml', className: 'N.Views.SamplePage', bindings: [binding({ expression: 'nothing', root: 'nothing', line: 12 })] })],
    }));

    expect(findings).toHaveLength(1);
    expect(findings[0]).toMatchObject({
      severity: 'high',
      title: 'SamplePage: x:Bind nothing has no such member',
      nodeIds: ['N.Views.SamplePage'],
      location: { file: 'Views/SamplePage.xaml', line: 12 },
    });
    expect(findings[0].detail).toContain('declares no property, field, method or generated member named `nothing`');
    expect(findings[0].detail).not.toContain('no element of that name exists');
  });

  it('adds the element hint when the missing root looks like a type or element name', () => {
    const findings = dead(input({
      decls: baseDecls,
      xamlFiles: [xaml({ path: 'Views/SamplePage.xaml', className: 'N.Views.SamplePage', bindings: [binding({ expression: 'Missing', root: 'Missing' })] })],
    }));
    expect(findings[0].detail).toContain('and no element of that name exists');
  });

  it('does not claim a dead binding for a root that names an element in the markup', () => {
    const findings = dead(input({
      decls: baseDecls,
      xamlFiles: [xaml({
        path: 'Views/SamplePage.xaml',
        className: 'N.Views.SamplePage',
        elementNames: ['Root'],
        bindings: [binding({ expression: 'Root', root: 'Root' })],
      })],
    }));
    expect(findings).toEqual([]);
  });

  it('reports one finding per distinct path, not one per occurrence', () => {
    const findings = dead(input({
      decls: baseDecls,
      xamlFiles: [xaml({
        path: 'Views/SamplePage.xaml',
        className: 'N.Views.SamplePage',
        bindings: [
          binding({ expression: 'missing', root: 'missing' }),
          binding({ expression: 'missing', root: 'missing', line: 20 }),
        ],
      })],
    }));
    expect(findings).toHaveLength(1);
  });

  it('makes no claim when the view has a base it cannot resolve', () => {
    const findings = dead(input({
      decls: [
        typeDecl({ name: 'SamplePage', namespace: 'N.Views', bases: ['UnknownBase'] }),
        viewModel,
      ],
      xamlFiles: [xaml({ path: 'Views/SamplePage.xaml', className: 'N.Views.SamplePage', bindings: [binding({ expression: 'anything', root: 'anything' })] })],
    }));
    expect(findings).toEqual([]);
  });

  it('does not treat a transparent framework base as an unresolvable one', () => {
    const findings = dead(input({
      decls: [typeDecl({ name: 'SamplePage', namespace: 'N.Views', bases: ['Page'] })],
      xamlFiles: [xaml({ path: 'Views/SamplePage.xaml', className: 'N.Views.SamplePage', bindings: [binding({ expression: 'anything', root: 'anything' })] })],
    }));
    expect(findings).toHaveLength(1);
  });

  it('ignores markup whose class has no declaration in the workspace', () => {
    const findings = dead(input({
      decls: [viewModel],
      xamlFiles: [xaml({ path: 'Views/Ghost.xaml', className: 'N.Views.Ghost', bindings: [binding({ expression: 'anything', root: 'anything' })] })],
    }));
    expect(findings).toEqual([]);
  });

  it('ignores a binding with no root, such as {Binding}', () => {
    const findings = dead(input({
      decls: baseDecls,
      xamlFiles: [xaml({
        path: 'Views/SamplePage.xaml',
        className: 'N.Views.SamplePage',
        bindings: [binding({ expression: 'Status', root: null, dialect: 'Binding', path: ['Status'] })],
      })],
    }));
    expect(findings).toEqual([]);
  });
});

describe('computeFindings: binding paths', () => {
  it('follows a path one segment at a time and reports the broken segment on its owner', () => {
    const findings = dead(input({
      decls: baseDecls,
      xamlFiles: [xaml({
        path: 'Views/SamplePage.xaml',
        className: 'N.Views.SamplePage',
        bindings: [binding({ expression: 'Row.Missing', root: 'Row', path: ['Missing'] })],
      })],
    }));

    expect(findings).toHaveLength(1);
    expect(findings[0]).toMatchObject({
      severity: 'medium',
      title: 'SamplePage: x:Bind Row.Missing is not on Cell',
      nodeIds: ['N.Views.SamplePage', 'N.Models.Cell'],
    });
    expect(findings[0].detail).toContain('`Missing`');
  });

  it('says nothing when every segment resolves', () => {
    const findings = dead(input({
      decls: baseDecls,
      xamlFiles: [xaml({
        path: 'Views/SamplePage.xaml',
        className: 'N.Views.SamplePage',
        bindings: [binding({ expression: 'Row.Title', root: 'Row', path: ['Title'] })],
      })],
    }));
    expect(findings).toEqual([]);
  });

  it('stops walking when the next type cannot be resolved', () => {
    const findings = dead(input({
      decls: [
        typeDecl({ name: 'SamplePage', namespace: 'N.Views', properties: [{ name: 'Row', type: 'Cell', rawType: 'Cell', line: 3, isObservable: false, isPublic: true, visibility: 'public', attributes: [] }] }),
      ],
      xamlFiles: [xaml({ path: 'Views/SamplePage.xaml', className: 'N.Views.SamplePage', bindings: [binding({ expression: 'Row.Deep.Missing', root: 'Row', path: ['Deep', 'Missing'] })] })],
    }));
    expect(findings).toEqual([]);
  });

  it('strips a call or indexer from a segment before judging it', () => {
    const findings = dead(input({
      decls: baseDecls,
      xamlFiles: [xaml({
        path: 'Views/SamplePage.xaml',
        className: 'N.Views.SamplePage',
        bindings: [binding({ expression: 'Row.Format(1)', root: 'Row', path: ['Format(1)'] })],
      })],
    }));
    expect(findings).toHaveLength(1);
    expect(findings[0].title).toBe('SamplePage: x:Bind Row.Format(1) is not on Cell');
  });

  it('stops at an empty segment', () => {
    const findings = dead(input({
      decls: baseDecls,
      xamlFiles: [xaml({
        path: 'Views/SamplePage.xaml',
        className: 'N.Views.SamplePage',
        bindings: [binding({ expression: 'Row..', root: 'Row', path: ['', 'Missing'] })],
      })],
    }));
    expect(findings).toEqual([]);
  });

  it('resolves a member declared on a base type', () => {
    const findings = dead(input({
      decls: [
        typeDecl({ name: 'SamplePage', namespace: 'N.Views', properties: [{ name: 'Row', type: 'Derived', rawType: 'Derived', line: 3, isObservable: false, isPublic: true, visibility: 'public', attributes: [] }] }),
        typeDecl({ name: 'Derived', namespace: 'N.Models', bases: ['Base'] }),
        typeDecl({ name: 'Base', namespace: 'N.Models', properties: [{ name: 'Inherited', type: 'string', rawType: 'string', line: 1, isObservable: false, isPublic: true, visibility: 'public', attributes: [] }] }),
      ],
      xamlFiles: [xaml({ path: 'Views/SamplePage.xaml', className: 'N.Views.SamplePage', bindings: [binding({ expression: 'Row.Inherited', root: 'Row', path: ['Inherited'] })] })],
    }));
    expect(findings).toEqual([]);
  });

  it('makes no claim when a base chain is deeper than the walk allows', () => {
    const chain: TypeDecl[] = [typeDecl({ name: 'SamplePage', namespace: 'N.Views', properties: [{ name: 'Row', type: 'L0', rawType: 'L0', line: 3, isObservable: false, isPublic: true, visibility: 'public', attributes: [] }] })];
    for (let level = 0; level < 10; level += 1) {
      chain.push(typeDecl({ name: `L${level}`, namespace: 'N.Models', bases: level < 9 ? [`L${level + 1}`] : [] }));
    }
    const findings = dead(input({
      decls: chain,
      xamlFiles: [xaml({ path: 'Views/SamplePage.xaml', className: 'N.Views.SamplePage', bindings: [binding({ expression: 'Row.Inherited', root: 'Row', path: ['Inherited'] })] })],
    }));
    expect(findings).toEqual([]);
  });
});

describe('computeFindings: dead bindings inside a DataTemplate', () => {
  const template = (partial: Partial<XamlBinding>) => xaml({
    path: 'Views/SamplePage.xaml',
    className: 'N.Views.SamplePage',
    bindings: [binding({
      expression: 'Title',
      root: 'Title',
      inTemplate: true,
      templateDataType: 'models:Cell',
      templateTypeNamespace: 'N.Models.Cell',
      ...partial,
    })],
  });

  it('accepts a row member that exists on the template type', () => {
    expect(dead(input({ decls: baseDecls, xamlFiles: [template({})] }))).toEqual([]);
  });

  it('reports a row member that does not exist on the template type', () => {
    const findings = dead(input({ decls: baseDecls, xamlFiles: [template({ expression: 'Nope', root: 'Nope' })] }));
    expect(findings).toHaveLength(1);
    expect(findings[0]).toMatchObject({
      severity: 'medium',
      title: "Cell: x:Bind Nope is not on the template's row type",
      nodeIds: ['N.Models.Cell'],
    });
    expect(findings[0].detail).toContain('x:DataType is models:Cell');
  });

  it('falls back to the written data type when the namespace form does not resolve', () => {
    const findings = dead(input({
      decls: baseDecls,
      xamlFiles: [template({ expression: 'Nope', root: 'Nope', templateTypeNamespace: 'N.Unknown.Cell' })],
    }));
    expect(findings).toHaveLength(1);
  });

  it('does not check a template with no x:DataType', () => {
    expect(dead(input({
      decls: baseDecls,
      xamlFiles: [template({ templateDataType: null, templateTypeNamespace: null })],
    }))).toEqual([]);
  });

  it('does not check a template whose type is not in the workspace', () => {
    expect(dead(input({
      decls: baseDecls,
      xamlFiles: [template({ templateDataType: 'Ghost', templateTypeNamespace: undefined as unknown as string })],
    }))).toEqual([]);
  });

  it('does not check a row type with an unresolvable base', () => {
    const decls = [...baseDecls, typeDecl({ name: 'Cell', namespace: 'N.Models', bases: ['UnknownRowBase'] })];
    expect(dead(input({ decls, xamlFiles: [template({})] }))).toEqual([]);
  });
});

describe('computeFindings: a template row type with a transparent base', () => {
  it('reports a row member that neither the row type nor a transparent base supplies', () => {
    const rowType = typeDecl({
      name: 'Row',
      namespace: 'N.Models',
      bases: ['ObservableObject'],
      properties: [
        { name: 'Title', type: 'string', rawType: 'string', line: 3, isObservable: false, isPublic: true, visibility: 'public', attributes: [] },
      ],
    });

    const findings = dead(input({
      decls: [page, viewModel, cell, rowType],
      xamlFiles: [xaml({
        path: 'Views/SamplePage.xaml',
        className: 'N.Views.SamplePage',
        bindings: [binding({
          expression: 'Missing',
          root: 'Missing',
          inTemplate: true,
          templateDataType: 'models:Row',
          templateTypeNamespace: 'N.Models.Row',
        })],
      })],
    }));

    expect(findings).toHaveLength(1);
    expect(findings[0].title).toBe("Row: x:Bind Missing is not on the template's row type");
    expect(findings[0].nodeIds).toEqual(['N.Models.Row']);
  });
});

describe('computeFindings: a member inherited from a workspace base beside an external one', () => {
  it('finds the inherited member past the transparent base', () => {
    const contracts = typeDecl({
      name: 'IViewMembers',
      kind: 'interface',
      namespace: 'N.Views',
      properties: [
        { name: 'Row', type: 'Cell', rawType: 'Cell', line: 3, isObservable: false, isPublic: true, visibility: 'public', attributes: [] },
      ],
    });
    const inheritingPage = typeDecl({
      name: 'DerivedPage',
      namespace: 'N.Views',
      bases: ['ObservableObject', 'IViewMembers'],
    });

    const findings = dead(input({
      decls: [inheritingPage, contracts, cell],
      xamlFiles: [xaml({
        path: 'Views/DerivedPage.xaml',
        className: 'N.Views.DerivedPage',
        bindings: [binding({ expression: 'Row.Title', root: 'Row', path: ['Title'] })],
      })],
    }));

    expect(findings).toEqual([]);
  });
});
