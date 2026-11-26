import { describe, expect, it } from 'vitest';
import { computeFindings } from '../../src/graph/findings';
import { graphEdge, graphNode, typeDecl } from '../support/model';
import { makeFindingInput, xamlBinding, xamlFile } from '../support/findings';
import type { TypeDecl } from '../../src/graph/csharp/parser';

const unusedTypes = (options: Parameters<typeof makeFindingInput>[0]) =>
  computeFindings(makeFindingInput(options)).findings.filter((entry) => entry.kind === 'unused-type');

const unusedMembers = (options: Parameters<typeof makeFindingInput>[0]) =>
  computeFindings(makeFindingInput(options)).findings.filter((entry) => entry.kind === 'unused-member');

const method = (name: string, partial: Partial<TypeDecl['methods'][number]> = {}) => ({
  name,
  returnType: 'void',
  rawReturnType: 'void',
  rawParamTypes: [],
  line: 1,
  visibility: 'private' as const,
  attributes: [],
  isOverride: false,
  isStatic: false,
  isAsync: false,
  isPartial: false,
  isExtension: false,
  ...partial,
});

const property = (name: string, partial: Partial<TypeDecl['properties'][number]> = {}) => ({
  name,
  type: 'string',
  rawType: 'string',
  line: 1,
  isObservable: false,
  isPublic: false,
  visibility: 'private' as const,
  attributes: [],
  initializedInline: false,
  readsDataContext: false,
  ...partial,
});

const field = (name: string, partial: Partial<TypeDecl['fields'][number]> = {}) => ({
  name,
  type: 'string',
  rawType: 'string',
  line: 1,
  isReadonly: false,
  visibility: 'private' as const,
  attributes: [],
  ...partial,
});

describe('computeFindings: unbound views', () => {
  const view = (declOverride: Partial<TypeDecl> = {}) => {
    const decl = typeDecl({ name: 'SamplePage', namespace: 'N.Views', ...declOverride });
    return makeFindingInput({
      decls: [decl],
      nodes: [graphNode({ id: 'N.Views.SamplePage', layer: 'view', xamlFile: 'Views/SamplePage.xaml' })],
      xamlFiles: [xamlFile({
        path: 'Views/SamplePage.xaml',
        className: 'N.Views.SamplePage',
        bindings: [xamlBinding({ expression: 'ViewModel.Status', root: 'ViewModel', path: ['Status'] })],
      })],
    });
  };

  const unbound = (options: ReturnType<typeof view>) =>
    computeFindings(options).findings.filter((entry) => entry.kind === 'unbound-view');

  it('reports a view that binds ViewModel without declaring one', () => {
    const findings = unbound(view());
    expect(findings).toHaveLength(1);
    expect(findings[0]).toMatchObject({
      kind: 'unbound-view',
      severity: 'high',
      title: 'SamplePage binds ViewModel but declares none',
      nodeIds: ['N.Views.SamplePage'],
      location: { file: 'Views/SamplePage.xaml', line: 1 },
    });
    expect(findings[0].detail).toContain('has no property of a view-model type');
  });

  it('accepts a declared property', () => {
    expect(unbound(view({ properties: [property('ViewModel', { isPublic: true, visibility: 'public' })] }))).toEqual([]);
  });

  it('accepts a declared field', () => {
    expect(unbound(view({ fields: [field('ViewModel', { isReadonly: true })] }))).toEqual([]);
  });

  it('says nothing about a view that never binds ViewModel', () => {
    const options = view();
    options.xamlFiles[0].bindings = [xamlBinding({ expression: 'Name', root: 'Name' })];
    expect(unbound(options)).toEqual([]);
  });

  it('ignores a view node with no markup recorded', () => {
    const options = makeFindingInput({
      decls: [typeDecl({ name: 'SamplePage', namespace: 'N.Views' })],
      nodes: [graphNode({ id: 'N.Views.SamplePage', layer: 'view' })],
    });
    expect(unbound(options)).toEqual([]);
  });

  it('ignores markup with no matching declaration', () => {
    const options = makeFindingInput({
      nodes: [graphNode({ id: 'N.Views.SamplePage', layer: 'view', xamlFile: 'Views/SamplePage.xaml' })],
      xamlFiles: [xamlFile({
        path: 'Views/SamplePage.xaml',
        className: 'N.Views.SamplePage',
        bindings: [xamlBinding({ expression: 'ViewModel.Status', root: 'ViewModel' })],
      })],
    });
    expect(unbound(options)).toEqual([]);
  });
});

describe('computeFindings: unused types', () => {
  it('counts an incoming edge as use', () => {
    const titles = unusedTypes({
      decls: [typeDecl({ name: 'Used', namespace: 'N' }), typeDecl({ name: 'Caller', namespace: 'N' })],
      edges: [graphEdge({ source: 'N.Caller', target: 'N.Used', kind: 'reference' })],
    }).map((entry) => entry.title);
    expect(titles).toEqual(['Unused class: Caller']);
  });

  it('does not count a template row edge as use', () => {
    const findings = unusedTypes({
      decls: [typeDecl({ name: 'Row', namespace: 'N' }), typeDecl({ name: 'View', namespace: 'N' })],
      edges: [graphEdge({ source: 'N.View', target: 'N.Row', kind: 'template' })],
    });
    expect(findings.map((entry) => entry.title)).toEqual(['Unused class: Row', 'Unused class: View']);
  });

  it('never reports an application entry point', () => {
    const names = ['App', 'Program', 'Startup', 'Main'];
    const findings = unusedTypes({ decls: names.map((name) => typeDecl({ name, namespace: 'N' })) });
    expect(findings).toEqual([]);
  });

  it('counts a typeof reference as use', () => {
    expect(unusedTypes({
      decls: [
        typeDecl({ name: 'Widget', namespace: 'N' }),
        typeDecl({ name: 'Registrar', namespace: 'N', typeofRefs: ['Widget'] }),
      ],
    }).map((entry) => entry.title)).toEqual(['Unused class: Registrar']);
  });

  it('never reports an external node', () => {
    const findings = unusedTypes({
      nodes: [graphNode({ id: 'System.Object', layer: 'external' })],
    });
    expect(findings).toEqual([]);
  });

  it('counts a container registration as use for a view', () => {
    const findings = unusedTypes({
      decls: [typeDecl({ name: 'SamplePage', namespace: 'N' })],
      nodes: [graphNode({ id: 'N.SamplePage', layer: 'view', xamlFile: 'a.xaml', diRegistered: true })],
    });
    expect(findings).toEqual([]);
  });

  it('counts being hosted in another view’s markup as use', () => {
    const titles = unusedTypes({
      decls: [typeDecl({ name: 'SetUpPanel', namespace: 'N' }), typeDecl({ name: 'Host', namespace: 'N' })],
      nodes: [
        graphNode({ id: 'N.SetUpPanel', layer: 'view', xamlFile: 'Panel.xaml' }),
        graphNode({ id: 'N.Host', layer: 'view', xamlFile: 'Host.xaml' }),
      ],
      xamlFiles: [
        xamlFile({ path: 'Panel.xaml', className: 'N.SetUpPanel', elementTags: ['SetUpPanel', 'Grid'] }),
        xamlFile({ path: 'Host.xaml', className: 'N.Host', elementTags: ['SetUpPanel'] }),
      ],
    }).map((entry) => entry.title);
    expect(titles).toEqual(['Unused view: Host']);
  });

  it('does not let a view host itself into being used', () => {
    const findings = unusedTypes({
      decls: [typeDecl({ name: 'Panel', namespace: 'N' })],
      nodes: [graphNode({ id: 'N.Panel', layer: 'view', xamlFile: 'Panel.xaml' })],
      xamlFiles: [xamlFile({ path: 'Panel.xaml', className: 'N.Panel', elementTags: ['Panel'] })],
    });
    expect(findings.map((entry) => entry.title)).toEqual(['Unused view: Panel']);
  });

  it('reports an unused view with the navigation wording', () => {
    const findings = unusedTypes({
      decls: [typeDecl({ name: 'Ghost', namespace: 'N' })],
      nodes: [graphNode({
        id: 'N.Ghost',
        layer: 'view',
        xamlFile: 'Ghost.xaml',
        location: { file: 'src/Ghost.cs', line: 1 },
      })],
    });
    expect(findings).toHaveLength(1);
    expect(findings[0]).toMatchObject({
      kind: 'unused-type',
      severity: 'medium',
      title: 'Unused view: Ghost',
      nodeIds: ['N.Ghost'],
      location: { file: 'src/Ghost.cs', line: 1 },
    });
    expect(findings[0].detail).toContain('It is not registered with the container and nothing navigates to it.');
    expect(findings[0].detail).toContain('A view is normally reached by navigation, so check its registration.');
  });

  it('counts a textual mention beyond the declaration as use for a plain type', () => {
    expect(unusedTypes({
      decls: [typeDecl({ name: 'Part', namespace: 'N' })],
      sources: ['class Part { }', 'Part.From(id)'],
    })).toEqual([]);
  });

  it('counts a mention no more often than its declarations when the type is partial', () => {
    const findings = unusedTypes({
      decls: [typeDecl({ name: 'Part', namespace: 'N', isPartial: true })],
      declarationCounts: { 'N.Part': 2 },
      sources: ['class Part {}', 'class Part {}'],
    });
    expect(findings.map((entry) => entry.title)).toEqual(['Unused class: Part']);
  });

  it('counts a called extension method as use of its holder class', () => {
    const findings = unusedTypes({
      decls: [
        typeDecl({ name: 'Extensions', namespace: 'N', methods: [method('Shout', { isExtension: true, visibility: 'public' })] }),
      ],
      sources: ['static string Shout(this string value) => value;', 'name.Shout()'],
    });
    expect(findings).toEqual([]);
  });

  it('does not count an uncalled extension method as use', () => {
    const findings = unusedTypes({
      decls: [
        typeDecl({ name: 'Extensions', namespace: 'N', methods: [method('Shout', { isExtension: true, visibility: 'public' })] }),
      ],
      sources: ['static string Shout(this string value) => value;'],
    });
    expect(findings.map((entry) => entry.title)).toEqual(['Unused class: Extensions']);
  });

  it('counts a container registration as use for a service', () => {
    expect(unusedTypes({
      decls: [typeDecl({ name: 'PartStore', namespace: 'N' })],
      nodes: [graphNode({ id: 'N.PartStore', layer: 'service', diRegistered: true })],
    })).toEqual([]);
  });

  it('names the declaration kind in a plain type’s title', () => {
    const findings = unusedTypes({
      decls: [typeDecl({ name: 'Status', namespace: 'N', kind: 'enum' })],
      nodes: [graphNode({ id: 'N.Status', kind: 'enum' })],
    });
    expect(findings[0].title).toBe('Unused enum: Status');
    expect(findings[0].detail).toContain('Nothing in the analysed files refers to it, by type name or by edge.');
    expect(findings[0].detail).not.toContain('check its registration');
  });
});

describe('computeFindings: unused members', () => {
  /**
   * Members are only examined on a type that is itself used, so every case here gives `Holder` an
   * incoming edge. Without it the whole type is reported once and its members are skipped.
   */
  const usedHolder = (members: Partial<TypeDecl>, extra: Partial<Parameters<typeof makeFindingInput>[0]> = {}) =>
    makeFindingInput({
      decls: [
        typeDecl({ name: 'Caller', namespace: 'N' }),
        typeDecl({ name: 'Holder', namespace: 'N', ...members }),
      ],
      edges: [graphEdge({ source: 'N.Caller', target: 'N.Holder', kind: 'reference' })],
      ...extra,
    });

  const unusedMembersOfHolder = (members: Partial<TypeDecl>, extra: Partial<Parameters<typeof makeFindingInput>[0]> = {}) =>
    computeFindings(usedHolder(members, extra)).findings.filter((entry) => entry.kind === 'unused-member');

  it('reports an unreferenced private method, field and property', () => {
    const findings = unusedMembersOfHolder({
      methods: [method('Hidden')],
      fields: [field('_hidden')],
      properties: [property('Secret')],
    });
    expect(findings.map((entry) => entry.title)).toEqual([
      'Unused field: Holder._hidden',
      'Unused method: Holder.Hidden',
      'Unused property: Holder.Secret',
    ]);
    expect(findings[0].detail).toContain('declared but never read or called anywhere in the analysed files.');
    expect(findings[0].severity).toBe('low');
  });

  it('counts a second occurrence anywhere in the corpus as use', () => {
    expect(unusedMembersOfHolder(
      { methods: [method('Hidden')] },
      { sources: ['void Hidden() {}', 'this.Hidden();'] },
    )).toEqual([]);
  });

  it('excludes a public, protected or internal member', () => {
    expect(unusedMembersOfHolder({
      methods: [
        method('Public', { visibility: 'public' }),
        method('Protected', { visibility: 'protected' }),
        method('Internal', { visibility: 'internal' }),
      ],
    })).toEqual([]);
  });

  it('excludes an override, a partial method and a generated command', () => {
    expect(unusedMembersOfHolder({
      methods: [
        method('Override', { isOverride: true }),
        method('Partial', { isPartial: true }),
        method('Save', { attributes: ['RelayCommand'], isAsync: true }),
        method('Debug', { attributes: ['Conditional'] }),
        method('get_Value'),
        method('set_Value'),
      ],
    })).toEqual([]);
  });

  it('excludes an entry point', () => {
    const input = makeFindingInput({
      decls: [
        typeDecl({ name: 'Caller', namespace: 'N' }),
        typeDecl({ name: 'Holder', namespace: 'N', methods: [method('Main', { isStatic: true })] }),
        typeDecl({ name: 'Program', namespace: 'N', methods: [method('Main')] }),
      ],
      edges: [
        graphEdge({ source: 'N.Caller', target: 'N.Holder', kind: 'reference' }),
        graphEdge({ source: 'N.Caller', target: 'N.Program', kind: 'reference' }),
      ],
    });
    expect(computeFindings(input).findings.filter((entry) => entry.kind === 'unused-member')).toEqual([]);
  });

  it('still reports an instance method named Main on an ordinary class', () => {
    const findings = unusedMembersOfHolder({ methods: [method('Main')] });
    expect(findings.map((entry) => entry.title)).toEqual(['Unused method: Holder.Main']);
  });

  it('excludes a handler named by markup', () => {
    expect(unusedMembersOfHolder(
      { methods: [method('OnClick')] },
      { xamlFiles: [xamlFile({ path: 'a.xaml', className: 'N.Holder', handlerNames: ['OnClick'] })] },
    )).toEqual([]);
  });

  it('excludes a member a binding names, including a path segment', () => {
    expect(unusedMembersOfHolder(
      { methods: [method('Format')], fields: [field('Name')] },
      {
        xamlFiles: [xamlFile({
          path: 'a.xaml',
          className: 'N.Holder',
          bindings: [xamlBinding({ expression: 'Format(Name)', root: 'Format', path: ['Name'], dialect: 'x:Bind' })],
        })],
      },
    )).toEqual([]);
  });

  it('excludes generated and read-only members', () => {
    expect(unusedMembersOfHolder({
      fields: [field('_title', { attributes: ['ObservableProperty'] }), field('_readonly', { isReadonly: true })],
      properties: [property('Observable', { isObservable: true })],
    })).toEqual([]);
  });

  it('does not report members of a type it already reported as unused', () => {
    const input = makeFindingInput({ decls: [typeDecl({ name: 'Holder', namespace: 'N', methods: [method('Hidden')] })] });
    const kinds = computeFindings(input).findings.map((entry) => entry.kind);
    expect(kinds).toEqual(['unused-type']);
  });

  it('names the member’s own file, falling back to the type’s file', () => {
    const findings = unusedMembersOfHolder({
      methods: [
        method('WithFile', { line: 42, file: 'src/Partial.Second.cs' }),
        method('WithoutFile', { line: 7 }),
      ],
    });
    const byTitle = new Map(findings.map((entry) => [entry.title, entry.location]));
    expect(byTitle.get('Unused method: Holder.WithFile')).toEqual({ file: 'src/Partial.Second.cs', line: 42 });
    expect(byTitle.get('Unused method: Holder.WithoutFile')).toEqual({ file: 'src/Holder.cs', line: 7 });
  });

  it('leaves the location undefined when no file is known at all', () => {
    const input = usedHolder({ methods: [method('Hidden')] });
    input.files.clear();
    const findings = computeFindings(input).findings.filter((entry) => entry.kind === 'unused-member');
    expect(findings).toHaveLength(1);
    expect(findings[0].location).toBeUndefined();
  });
});

describe('computeFindings: ordering', () => {
  it('sorts by severity, then kind, then title', () => {
    const findings = computeFindings(makeFindingInput({
      decls: [
        typeDecl({ name: 'Zed', namespace: 'N' }),
        typeDecl({ name: 'Alpha', namespace: 'N' }),
        typeDecl({ name: 'Cycle1', namespace: 'N' }),
        typeDecl({ name: 'Cycle2', namespace: 'N' }),
      ],
      edges: [
        graphEdge({ source: 'N.Cycle1', target: 'N.Cycle2', kind: 'injects' }),
        graphEdge({ source: 'N.Cycle2', target: 'N.Cycle1', kind: 'injects' }),
      ],
    })).findings;

    expect(findings[0].kind).toBe('cycle');
    expect(findings[0].severity).toBe('high');
    expect(findings.slice(1).map((entry) => entry.title)).toEqual([
      'Unused class: Alpha',
      'Unused class: Zed',
    ]);
  });
});

describe('computeFindings: further corners', () => {
  it('leaves a cycle’s location undefined when neither file nor declaration is known', () => {
    const input = makeFindingInput({
      decls: [typeDecl({ name: 'A', namespace: 'N' }), typeDecl({ name: 'B', namespace: 'N' })],
      edges: [
        graphEdge({ source: 'N.A', target: 'N.B', kind: 'injects' }),
        graphEdge({ source: 'N.B', target: 'N.A', kind: 'injects' }),
      ],
    });
    input.files.clear();
    const cycle = computeFindings(input).findings.find((entry) => entry.kind === 'cycle');
    expect(cycle?.location).toBeUndefined();
  });

  it('follows a two-edge loop between the same pair', () => {
    const input = makeFindingInput({
      decls: [typeDecl({ name: 'A', namespace: 'N' }), typeDecl({ name: 'B', namespace: 'N' })],
      edges: [
        graphEdge({ source: 'N.A', target: 'N.B', kind: 'injects' }),
        graphEdge({ source: 'N.A', target: 'N.B', kind: 'composes' }),
        graphEdge({ source: 'N.B', target: 'N.A', kind: 'injects' }),
      ],
    });
    const cycle = computeFindings(input).findings.find((entry) => entry.kind === 'cycle');
    expect(cycle?.edgeIds).toHaveLength(3);
    expect(cycle?.severity).toBe('high');
  });

  it('walks an inherited member’s type when following a binding path', () => {
    const findings = computeFindings(makeFindingInput({
      decls: [
        typeDecl({
          name: 'SamplePage',
          namespace: 'N.Views',
          properties: [{ name: 'Row', type: 'Derived', rawType: 'Derived', line: 3, isObservable: false, isPublic: true, visibility: 'public', attributes: [], initializedInline: false, readsDataContext: false }],
        }),
        typeDecl({ name: 'Derived', namespace: 'N.Models', bases: ['Base'] }),
        typeDecl({
          name: 'Base',
          namespace: 'N.Models',
          properties: [{ name: 'Middle', type: 'Cell', rawType: 'Cell', line: 1, isObservable: false, isPublic: true, visibility: 'public', attributes: [], initializedInline: false, readsDataContext: false }],
        }),
        typeDecl({ name: 'Cell', namespace: 'N.Models' }),
      ],
      xamlFiles: [xamlFile({
        path: 'Views/SamplePage.xaml',
        className: 'N.Views.SamplePage',
        bindings: [xamlBinding({ expression: 'Row.Middle.Missing', root: 'Row', path: ['Middle', 'Missing'] })],
      })],
    })).findings.filter((entry) => entry.kind === 'dead-binding');
    expect(findings).toHaveLength(1);
    expect(findings[0].title).toBe('SamplePage: x:Bind Row.Middle.Missing is not on Cell');
    expect(findings[0].nodeIds).toEqual(['N.Views.SamplePage', 'N.Models.Cell']);
  });

  it('excludes a private property that markup names', () => {
    const input = makeFindingInput({
      decls: [
        typeDecl({ name: 'Caller', namespace: 'N' }),
        typeDecl({ name: 'Holder', namespace: 'N', properties: [{ name: 'Secret', type: 'string', rawType: 'string', line: 1, isObservable: false, isPublic: false, visibility: 'private', attributes: [], initializedInline: false, readsDataContext: false }] }),
      ],
      edges: [graphEdge({ source: 'N.Caller', target: 'N.Holder', kind: 'reference' })],
      xamlFiles: [xamlFile({
        path: 'a.xaml',
        className: 'N.Holder',
        bindings: [xamlBinding({ expression: 'Secret', root: 'Secret' })],
      })],
    });
    expect(computeFindings(input).findings.filter((entry) => entry.kind === 'unused-member')).toEqual([]);
  });
});
