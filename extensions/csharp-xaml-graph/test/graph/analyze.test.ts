import { describe, expect, it } from 'vitest';
import * as path from 'node:path';
import { analyzeWorkspace, type AnalyzeInput, type AnalyzeOptions } from '../../src/graph/analyze';
import type { GraphModel } from '../../src/graph/model';
import { compileConventions } from '../../src/graph/conventions';

const grammarWasmPath = path.join(process.cwd(), 'media', 'tree-sitter-c_sharp.wasm');

const inputs = (files: Record<string, string>): AnalyzeInput[] =>
  Object.entries(files).map(([file, text]) => ({ path: file, text }));

async function analyze(
  csharp: Record<string, string>,
  xaml: Record<string, string> = {},
  options: Partial<AnalyzeOptions> = {},
): Promise<GraphModel> {
  return analyzeWorkspace({
    workspaceRoot: '/workspace',
    csharp: inputs(csharp),
    xaml: inputs(xaml),
    grammarWasmPath,
    ...options,
  });
}

const edgeKeys = (model: GraphModel): string[] =>
  model.edges.map((edge) => `${edge.source} -${edge.kind}-> ${edge.target}`);

const nodeOf = (model: GraphModel, id: string) => model.nodes.find((node) => node.id === id);

describe('analyzeWorkspace: declarations and modules', () => {
  it('turns every declared type into a node with its layer, module, kind and location', async () => {
    const model = await analyze({
      'Module_A/Views/SamplePage.xaml.cs': 'namespace Module_A.Views { public class SamplePage {} }',
      'Module_A/ViewModels/SampleViewModel.cs': 'namespace Module_A.ViewModels { public class SampleViewModel {} }',
      'Module_A/Services/PartStore.cs': 'namespace Module_A.Services { public class PartStore {} }',
      'Module_A/Models/Part.cs': 'namespace Module_A.Models { public record Part(int Id); }',
    });

    expect(model.nodes.map((node) => `${node.label}:${node.layer}:${node.module}:${node.kind}`).sort()).toEqual([
      'Part:model:Module_A:record',
      'PartStore:service:Module_A:class',
      'SamplePage:model:Module_A:class',
      'SampleViewModel:viewmodel:Module_A:class',
    ]);
    expect(nodeOf(model, 'Module_A.Models.Part')?.location).toEqual({
      file: 'Module_A/Models/Part.cs',
      line: 1,
    });
  });

  it('groups a path with a src folder, skipping container segments', async () => {
    const model = await analyze({
      'src/Feature/Thing.cs': 'namespace A { public class Thing {} }',
      'src/App/Other.cs': 'namespace A { public class Other {} }',
    });
    expect(nodeOf(model, 'A.Thing')?.module).toBe('Feature');
    // `app` is a container segment too, so a folder called App names no module.
    expect(nodeOf(model, 'A.Other')?.module).toBe('(root)');
  });

  it('merges a partial type declared in two files and keeps evidence against the declaring file', async () => {
    const model = await analyze({
      'A/Part.First.cs': `namespace N {
    public partial class Part
    {
        private Dep? _dep;
    }
}`,
      'A/Part.Second.cs': `namespace N {
    public partial class Part
    {
        public Part(Dep dep) { }
    }
    public class Dep { }
}`,
    });

    const part = nodeOf(model, 'N.Part');
    expect(part).toBeDefined();
    const injects = model.edges.find((edge) => edge.kind === 'injects');
    expect(injects?.source).toBe('N.Part');
    expect(injects?.target).toBe('N.Dep');
    expect(injects?.evidence).toEqual({ file: 'A/Part.Second.cs', line: 4 });
  });

  it('records a partial type only once even though it was declared twice', async () => {
    const model = await analyze({
      'A/Part.First.cs': 'namespace N { public partial class Part {} }',
      'A/Part.Second.cs': 'namespace N { public partial class Part {} }',
    });
    expect(model.nodes.filter((node) => node.id === 'N.Part')).toHaveLength(1);
    expect(model.stats.parser?.partialTypes).toBe(1);
  });
});

describe('analyzeWorkspace: dependency-injection edges', () => {
  const sources = {
    'App/App.cs': `using App.Hosting;
namespace App
{
    public class App
    {
        void Wire(PageService pageService, IServiceCollection services)
        {
            pageService.Configure<SampleViewModel, SamplePage>();
            services.AddTransient<OneStore>();
            services.AddSingleton<IRepo, Repo>();
        }
    }
    public class PageService { }
    public interface IServiceCollection { }
}`,
    'App/SamplePage.cs': 'namespace App { public class SamplePage { } }',
    'App/SampleViewModel.cs': 'namespace App { public class SampleViewModel { } }',
    'App/OneStore.cs': 'namespace App { public class OneStore { } }',
    'App/IRepo.cs': 'namespace App { public interface IRepo { } public class Repo : IRepo { } }',
  };

  it('registers both arguments of a page registration and draws the page-to-view-model edge', async () => {
    const model = await analyze(sources);

    expect(edgeKeys(model)).toContain('App.SamplePage -resolves-> App.SampleViewModel');
    expect(nodeOf(model, 'App.SamplePage')?.diRegistered).toBe(true);
    expect(nodeOf(model, 'App.SampleViewModel')?.diRegistered).toBe(true);
  });

  it('marks a single-argument registration as registered without inventing an edge', async () => {
    const model = await analyze(sources);
    expect(nodeOf(model, 'App.OneStore')?.diRegistered).toBe(true);
    expect(model.edges.filter((edge) => edge.source === 'App.OneStore')).toEqual([]);
  });

  it('draws the implementation-to-interface edge of a paired registration', async () => {
    const model = await analyze(sources);
    const edge = model.edges.find((candidate) => candidate.label === 'container registration');
    expect(edge).toMatchObject({
      source: 'App.Repo',
      target: 'App.IRepo',
      kind: 'implements',
      confidence: 'medium',
    });
    expect(nodeOf(model, 'App.IRepo')?.diRegistered).toBe(true);
  });

  it('draws constructor injection for a parameter it can resolve', async () => {
    const model = await analyze({
      'A/Page.cs': 'namespace N { public class Page { public Page(ViewModel vm) { } } public class ViewModel { } }',
    });
    expect(edgeKeys(model)).toContain('N.Page -injects-> N.ViewModel');
    expect(model.edges[0].confidence).toBe('high');
  });

  it('shows an unresolved parameter as an external node when external types are on', async () => {
    const model = await analyze({
      'A/Page.cs': 'namespace N { public class Page { public Page(ILogger logger) { } } }',
    });
    expect(nodeOf(model, 'ILogger')?.layer).toBe('external');
    expect(edgeKeys(model)).toContain('N.Page -injects-> ILogger');
  });

  it('drops external types when the setting is off', async () => {
    const model = await analyze(
      { 'A/Page.cs': 'namespace N { public class Page { public Page(ILogger logger) { } } }' },
      {},
      { includeExternalTypes: false },
    );
    expect(model.nodes.map((node) => node.id)).toEqual(['N.Page']);
    expect(model.edges).toEqual([]);
  });
});

describe('analyzeWorkspace: assignments', () => {
  it('covers the four assignment shapes the graph draws', async () => {
    const model = await analyze({
      'A/Page.cs': `using Microsoft.Extensions.DependencyInjection;
namespace N
{
    public class Page
    {
        private readonly ViewModel _vm;
        private IDep? _dep;
        public ViewModel ViewModel { get; set; }

        public Page(ViewModel vm)
        {
            _vm = vm;
            _dep = App.GetService<IDep>();
            ViewModel = new ViewModel();
        }
    }
    public class ViewModel { }
    public interface IDep { }
}`,
    });

    expect(edgeKeys(model)).toEqual(expect.arrayContaining([
      'N.Page -field-> N.ViewModel',
      'N.Page -getservice-> N.IDep',
      'N.Page -constructs-> N.ViewModel',
    ]));
  });

  it('falls back to the target member’s declared type when the service-locator type is unresolved', async () => {
    const model = await analyze({
      'A/Page.cs': `using Microsoft.Extensions.DependencyInjection;
namespace N
{
    public class Page
    {
        private IDep? _dep;
        public Page() { _dep = App.GetRequiredService<EfDep>(); }
    }
    public interface IDep { }
}`,
    });

    // `EfDep` is not in the workspace, so the locator resolves through `_dep`'s declared type.
    expect(edgeKeys(model)).toContain('N.Page -getservice-> N.IDep');
  });

  it('only composes a view model or a service, not a plain row model', async () => {
    const model = await analyze({
      'A/Page.cs': `namespace N
{
    public class Page
    {
        void Run() { var a = new Helper(); var b = new Row(); }
    }
    public class Helper { void Go() { } }
    public class Row { }
}`,
      'A/Heper.Service.cs': 'namespace N { public class Worker { void Run() { var a = new Worker(); } } }',
    });

    expect(edgeKeys(model)).not.toContain('N.Page -composes-> N.Row');
    expect(edgeKeys(model)).not.toContain('N.Page -composes-> N.Helper');
  });

  it('records a composition inside a view model', async () => {
    const model = await analyze({
      'A/ViewModel.cs': `namespace N
{
    public class MainViewModel
    {
        void Run() { var store = new PartStore(); }
    }
    public class PartStore { }
}`,
    });
    expect(edgeKeys(model)).toContain('N.MainViewModel -composes-> N.PartStore');
  });

  it('ignores an assignment whose right-hand side is nothing the graph models', async () => {
    const model = await analyze({
      'A/Page.cs': 'namespace N { public class Page { private string _name; void Run() { _name = "x"; } } }',
    });
    expect(model.edges).toEqual([]);
  });
});

describe('analyzeWorkspace: inheritance and references', () => {
  it('distinguishes inherits from implements, for workspace and external bases', async () => {
    const model = await analyze({
      'A/Types.cs': `namespace N
{
    public class Base { }
    public interface IThing { }
    public class Thing : Base, IThing { }
    public class PageBase : Microsoft.UI.Xaml.Controls.Page { }
    public class Listing : System.Collections.IEnumerable { }
}`,
    });

    expect(edgeKeys(model)).toEqual(expect.arrayContaining([
      'N.Thing -inherits-> N.Base',
      'N.Thing -implements-> N.IThing',
      'N.PageBase -inherits-> Microsoft.UI.Xaml.Controls.Page',
      'N.Listing -implements-> System.Collections.IEnumerable',
    ]));
    const external = model.edges.filter((edge) => edge.confidence === 'low');
    expect(external).toHaveLength(2);
  });

  it('draws one reference edge per pair, keeping the first label it saw', async () => {
    const model = await analyze({
      'A/Types.cs': `namespace N
{
    public class Holder
    {
        private readonly Dep _field;
        public Dep Property { get; set; }
        public Dep Make(Dep input) => input;
    }
    public class Dep { }
}`,
    });

    const references = model.edges.filter((edge) => edge.kind === 'reference');
    expect(references).toHaveLength(1);
    expect(references[0]).toMatchObject({ source: 'N.Holder', target: 'N.Dep', label: 'Property', confidence: 'medium' });
    // The field, the return type, the parameter and the property mention all collapse into it.
    expect(model.stats.parser?.edgesMerged).toBe(6);
    expect(model.stats.parser?.edgesDropped).toBe(0);
  });

  it('draws references from generic arguments and skips a self-reference', async () => {
    const model = await analyze({
      'A/Types.cs': `namespace N
{
    public class Holder
    {
        public System.Collections.Generic.IReadOnlyList<Row> Rows { get; set; }
        public Holder Self { get; set; }
    }
    public class Row { }
}`,
    });

    expect(edgeKeys(model)).toContain('N.Holder -reference-> N.Row');
    expect(model.edges.some((edge) => edge.source === 'N.Holder' && edge.target === 'N.Holder')).toBe(false);
  });

  it('draws a low-confidence reference for a mention, keeping only the first of a pair', async () => {
    const model = await analyze({
      'A/Types.cs': `namespace N
{
    public class Holder
    {
        void Run(object value) { var typed = (Row)value; var type = typeof(Row); }
    }
    public class Row { }
}`,
    });

    const mentions = model.edges.filter((edge) => edge.kind === 'reference' && edge.confidence === 'low');
    expect(mentions).toHaveLength(1);
    expect(mentions[0].label).toBe('cast Row');
    expect(model.stats.parser?.edgesMerged).toBeGreaterThan(0);
  });

  it('counts a repeated pair as merged and a self-reference as dropped', async () => {
    const model = await analyze({
      'A/Types.cs': `namespace N
{
    public class Holder
    {
        public Dep First { get; set; }
        public Dep Second { get; set; }
        public Holder Self { get; set; }
    }
    public class Dep { }
}`,
    });

    // One edge survives per (source, target, kind): the second property and its mentions collapse into
    // the first. A self-reference is not a link between two types at all, so it is a real drop — the
    // distinction is what lets the debug report warn on a fault and stay quiet on the normal case.
    expect(model.edges.filter((edge) => edge.kind === 'reference')).toHaveLength(1);
    expect(model.stats.parser?.edgesMerged).toBe(3);
    expect(model.stats.parser?.edgesDropped).toBe(0);
  });
});

describe('analyzeWorkspace: views and XAML', () => {
  const page = `namespace N.Views
{
    public class SamplePage
    {
        public SampleViewModel ViewModel { get; set; }
    }
    public class SampleViewModel
    {
        public string Status { get; set; }
        public System.Collections.Generic.List<Cell> Cells { get; set; }
    }
    public class Cell { public string Title { get; set; } }
}`;

  const markup = `<?xml version="1.0" encoding="utf-8"?>
<Page x:Class="N.Views.SamplePage"
      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
      xmlns:models="using:N.Views">
    <TextBlock Text="{x:Bind ViewModel.Status}" />
    <ListView ItemsSource="{x:Bind ViewModel.Cells}">
        <ListView.ItemTemplate>
            <DataTemplate x:DataType="models:Cell">
                <TextBlock Text="{x:Bind Title}" />
            </DataTemplate>
        </ListView.ItemTemplate>
    </ListView>
</Page>`;

  it('marks the page as a view and records the markup file', async () => {
    const model = await analyze({ 'A/SamplePage.xaml.cs': page }, { 'A/SamplePage.xaml': markup });
    expect(nodeOf(model, 'N.Views.SamplePage')).toMatchObject({
      layer: 'view',
      kind: 'page',
      xamlFile: 'A/SamplePage.xaml',
    });
  });

  it('draws a binding edge to the view model and records which properties the markup uses', async () => {
    const model = await analyze({ 'A/SamplePage.xaml.cs': page }, { 'A/SamplePage.xaml': markup });
    const bind = model.edges.find((edge) => edge.kind === 'binds');
    expect(bind).toMatchObject({
      source: 'N.Views.SamplePage',
      target: 'N.Views.SampleViewModel',
      confidence: 'high',
      label: 'ViewModel',
      evidence: { file: 'A/SamplePage.xaml', line: 5 },
    });
    expect(nodeOf(model, 'N.Views.SamplePage')?.viewModelProperties).toEqual(['ViewModel']);
    expect(nodeOf(model, 'N.Views.SamplePage')?.boundViewModelProperties).toEqual(['ViewModel']);
  });

  it('records a DataTemplate row type as an informational edge', async () => {
    const model = await analyze({ 'A/SamplePage.xaml.cs': page }, { 'A/SamplePage.xaml': markup });
    const template = model.edges.find((edge) => edge.kind === 'template');
    expect(template).toMatchObject({ source: 'N.Views.SamplePage', target: 'N.Views.Cell', confidence: 'low' });
  });

  it('does not treat a binding whose root is a named element as a view-model binding', async () => {
    const model = await analyze({ 'A/SamplePage.xaml.cs': page }, {
      'A/SamplePage.xaml': `<Page x:Class="N.Views.SamplePage">
    <TextBlock x:Name="ViewModel" Text="{x:Bind ViewModel.Status}" />
</Page>`,
    });
    // The page still reaches its view model through the declared property, so that edge stands, but
    // the markup proved nothing: `ViewModel` is an element on the page, not a bound property.
    const binds = model.edges.filter((edge) => edge.kind === 'binds');
    expect(binds).toHaveLength(1);
    expect(binds[0].evidence?.file).toBe('A/SamplePage.xaml.cs');
    expect(nodeOf(model, 'N.Views.SamplePage')?.boundViewModelProperties).toBeUndefined();
  });

  it('creates a view node for markup whose code-behind is missing', async () => {
    const model = await analyze({}, {
      'A/Ghost.xaml': '<UserControl x:Class="N.Ghost"></UserControl>',
    });
    expect(nodeOf(model, 'N.Ghost')).toMatchObject({
      layer: 'view',
      kind: 'usercontrol',
      module: 'A',
      location: { file: 'A/Ghost.xaml', line: 1 },
    });
  });

  it('maps each recognised root element to a kind and lower-cases anything else', async () => {
    const model = await analyze({}, {
      'A/One.xaml': '<Window x:Class="N.One" />',
      'A/Two.xaml': '<WindowEx x:Class="N.Two" />',
      'A/Three.xaml': '<CustomControl x:Class="N.Three" />',
    });
    expect(model.nodes.map((node) => node.kind).sort()).toEqual(['customcontrol', 'window', 'window']);
  });
});

describe('analyzeWorkspace: limits and bookkeeping', () => {
  const many = Object.fromEntries(Array.from({ length: 6 }, (_, index) => [
    `A/Type${index}.cs`,
    `namespace N { public class Type${index} { public Type${(index + 1) % 6} Next { get; set; } } }`,
  ]));

  it('truncates to the node cap, saying so, and keeps only edges and findings with both ends', async () => {
    const model = await analyze(many, {}, { maxNodes: 2 });

    expect(model.stats.truncated).toBe(true);
    expect(model.nodes).toHaveLength(2);
    const kept = new Set(model.nodes.map((node) => node.id));
    expect(model.edges.every((edge) => kept.has(edge.source) && kept.has(edge.target))).toBe(true);
    expect(model.findings.every((finding) => finding.nodeIds.length > 0)).toBe(true);
  });

  it('prefers views, then view models, then services, then models when cutting', async () => {
    const model = await analyze({
      'A/View.xaml.cs': 'namespace N { public class View { } }',
      'A/ZModel.cs': 'namespace N { public class ZModel { } }',
    }, {
      'A/View.xaml': '<Page x:Class="N.View" />',
    }, { maxNodes: 1 });

    expect(model.nodes.map((node) => node.id)).toEqual(['N.View']);
  });

  it('treats a non-positive cap as the default', async () => {
    const model = await analyze({ 'A/One.cs': 'namespace N { public class One { } }' }, {}, { maxNodes: 0 });
    expect(model.stats.truncated).toBe(false);
  });

  it('tallies what the parsers saw', async () => {
    const model = await analyze(
      {
        'A/SamplePage.xaml.cs': 'namespace N { public class SamplePage { public SampleViewModel ViewModel { get; set; } } public class SampleViewModel { } }',
      },
      {
        'A/SamplePage.xaml': `<Page x:Class="N.SamplePage" xmlns:x="x">
    <Button Click="OnGo" />
    <TextBlock Text="{x:Bind ViewModel.Name}" />
    <TextBlock Text="{Binding Name}" />
    <DataTemplate x:DataType="Row"><TextBlock Text="{x:Bind Title}" /></DataTemplate>
</Page>`,
        'A/Styles.xaml': '<ResourceDictionary />',
      },
    );

    expect(model.stats).toMatchObject({
      filesAnalysed: 3,
      csharpFiles: 1,
      xamlFiles: 2,
      parseErrors: 0,
      parseWarnings: 0,
    });
    expect(model.stats.parser).toMatchObject({
      typesDeclared: 2,
      partialTypes: 0,
      namespaces: 1,
      xamlWithClass: 1,
      xamlWithoutClass: 1,
      bindings: 3,
      templateBindings: 1,
      classicBindings: 1,
      handlerNames: 1,
      dataTypes: 1,
    });
    expect(model.stats.parser?.elementTags).toBeGreaterThan(0);
  });

  it('counts an ambiguous simple name once', async () => {
    const model = await analyze({
      'A/One.cs': 'namespace One { public class Item { } }',
      'A/Two.cs': 'namespace Two { public class Item { } }',
    });
    expect(model.stats.parser?.ambiguousSimpleNames).toBe(1);
  });

  it('reports a file the parser refused as a warning instead of failing the analysis', async () => {
    const model = await analyzeWorkspace({
      workspaceRoot: '/workspace',
      csharp: [
        { path: 'A/Bad.cs', text: null as unknown as string },
        { path: 'A/Good.cs', text: 'namespace N { public class Good { } }' },
      ],
      xaml: [{ path: 'A/Bad.xaml', text: null as unknown as string }],
      grammarWasmPath,
    });

    expect(model.warnings).toHaveLength(2);
    expect(model.warnings[0]).toMatch(/^A\/Bad\.cs: /);
    expect(model.warnings[1]).toMatch(/^A\/Bad\.xaml: /);
    expect(model.stats.parseWarnings).toBe(2);
    expect(model.nodes.map((node) => node.id)).toEqual(['N.Good']);
  });

  it('reports a C# file with a syntax error in the parse-error count', async () => {
    const model = await analyze({ 'A/Broken.cs': 'namespace N { class Broken { void M( } }' });
    expect(model.stats.parseErrors).toBe(1);
  });

  it('reports progress phase by phase and survives a throwing listener', async () => {
    const phases: string[] = [];
    await analyze({ 'A/One.cs': 'namespace N { public class One { } }' }, {}, {
      progress: (phase) => phases.push(phase),
    });

    expect(phases[0]).toBe('Reading the C# grammar');
    expect(phases).toContain('Parsing C# — 1 of 1');
    expect(phases).toContain('Building the graph');
    expect(phases).toContain('Running the cleanup rules');

    const model = await analyze({ 'A/One.cs': 'namespace N { public class One { } }' }, {}, {
      progress: () => {
        throw new Error('listener failed');
      },
    });
    expect(model.nodes).toHaveLength(1);
  });

  it('stamps the workspace and a generation time on the model', async () => {
    const model = await analyze({ 'A/One.cs': 'namespace N { public class One { } }' });
    expect(model.workspace).toBe('/workspace');
    expect(Number.isNaN(Date.parse(model.generatedAt))).toBe(false);
  });

  it('summarises cleanup counts from the findings it produced', async () => {
    const model = await analyze({ 'A/Unused.cs': 'namespace N { public class Unused { private void Hidden() { } } }' });
    expect(model.cleanup.unusedTypes).toBe(1);
    expect(model.findings.filter((finding) => finding.kind === 'unused-type')).toHaveLength(1);
  });
});

describe('analyzeWorkspace: more shapes', () => {
  it('merges a base, a partial and an abstract flag from a second partial file', () => {
    return analyze({
      'A/Thing.First.cs': `namespace N
{
    public class Base { }
    public partial class Thing { private int _a; }
}`,
      'A/Thing.Second.cs': `namespace N
{
    public abstract partial class Thing : Base { private int _b; }
}`,
    }).then((model) => {
      expect(model.nodes.filter((node) => node.id === 'N.Thing')).toHaveLength(1);
      expect(model.edges.some((edge) => edge.source === 'N.Thing' && edge.kind === 'inherits')).toBe(true);
    });
  });

  it('reuses one external node for repeated references to the same unknown type', async () => {
    const model = await analyze({
      'A/Page.cs': 'namespace N { public class Page { public Page(ILogger first, ILogger second) { } } }',
    });
    expect(model.nodes.filter((node) => node.id === 'ILogger')).toHaveLength(1);
  });

  it('registers a view model resolved through the service locator', async () => {
    const model = await analyze({
      'A/Page.cs': `using Microsoft.Extensions.DependencyInjection;
namespace N
{
    public class Page
    {
        private SampleViewModel? ViewModel { get; set; }
        public Page() { ViewModel = App.GetService<SampleViewModel>(); }
    }
    public class SampleViewModel { }
}`,
    });
    expect(nodeOf(model, 'N.SampleViewModel')?.diRegistered).toBe(true);
  });

  it('draws no composition edge for a type the workspace does not declare', async () => {
    const model = await analyze({
      'A/Page.cs': 'namespace N { public class MainViewModel { void Run() { var x = new Ghost(); } } }',
    });
    expect(model.edges.filter((edge) => edge.kind === 'composes')).toEqual([]);
  });

  it('ignores a binding whose root is not a property of the view', async () => {
    const model = await analyze(
      { 'A/SamplePage.xaml.cs': 'namespace N { public class SamplePage { public void Run() { } } }' },
      { 'A/SamplePage.xaml': '<Page x:Class="N.SamplePage"><TextBlock Text="{x:Bind Run}" /></Page>' },
    );
    expect(model.edges.filter((edge) => edge.kind === 'binds')).toEqual([]);
  });

  it('ignores a binding whose property type is not in the workspace', async () => {
    const model = await analyze(
      { 'A/SamplePage.xaml.cs': 'namespace N { public class SamplePage { public Unknown ViewModel { get; set; } } }' },
      { 'A/SamplePage.xaml': '<Page x:Class="N.SamplePage"><TextBlock Text="{x:Bind ViewModel.X}" /></Page>' },
      { includeExternalTypes: false },
    );
    expect(model.edges.filter((edge) => edge.kind === 'binds')).toEqual([]);
  });
});

describe('analyzeWorkspace: a field that does not resolve', () => {
  it('falls back to the constructor parameter’s type for a field-backed edge', async () => {
    const model = await analyze({
      'A/Page.cs': `namespace N
{
    public class Page
    {
        private readonly Unknown _field;
        public Page(Dep dep) { _field = dep; }
    }
    public class Dep { }
}`,
    });
    expect(model.edges.some((edge) => edge.kind === 'field' && edge.target === 'N.Dep')).toBe(true);
  });
});

describe('analyzeWorkspace: a type that refers to itself', () => {
  it('drops the self-reference rather than drawing a loop', async () => {
    const model = await analyze({
      'A/Node.cs': `namespace N
{
    public class Node
    {
        public Node? Next { get; set; }
    }
}`,
    });
    expect(model.edges.filter((edge) => edge.source === edge.target)).toEqual([]);
  });
});

describe('analyzeWorkspace: the DataContext convention', () => {
  const page = `namespace N.Views
{
    public sealed class Shell : Microsoft.UI.Xaml.Controls.Page
    {
        public ShellViewModel? ViewModel
        {
            get => DataContext as ShellViewModel;
            set => DataContext = value;
        }
    }
    public sealed class ShellViewModel
    {
        public string Status { get; set; }
    }
}`;

  const markup = `<Page x:Class="N.Views.Shell">
    <TextBlock Text="{Binding Status}" />
</Page>`;

  it('binds a view to its view-model property even without x:Bind', async () => {
    const model = await analyze({ 'A/Shell.xaml.cs': page }, { 'A/Shell.xaml': markup });
    const binds = model.edges.filter((edge) => edge.kind === 'binds');
    expect(binds).toHaveLength(1);
    expect(binds[0]).toMatchObject({
      source: 'N.Views.Shell',
      target: 'N.Views.ShellViewModel',
      confidence: 'high',
      label: 'ViewModel',
    });
    expect(binds[0].evidence?.file).toBe('A/Shell.xaml.cs');
  });

  it('keeps the markup as the evidence when the markup proves the binding too', async () => {
    const model = await analyze({ 'A/Shell.xaml.cs': page }, {
      'A/Shell.xaml': `<Page x:Class="N.Views.Shell">
    <TextBlock Text="{x:Bind ViewModel.Status}" />
</Page>`,
    });
    const binds = model.edges.filter((edge) => edge.kind === 'binds');
    expect(binds).toHaveLength(1);
    expect(binds[0].evidence?.file).toBe('A/Shell.xaml');
  });
});

describe('analyzeWorkspace: view and view-model registration pairs', () => {
  const types = `namespace N.Views
{
    public sealed class FirstPage : Microsoft.UI.Xaml.Controls.Page { }
    public sealed class SecondPage : Microsoft.UI.Xaml.Controls.Page { }
    public sealed class FirstViewModel { }
    public sealed class SecondViewModel { }
}`;

  it('links a page to the view model registered beside it', async () => {
    const model = await analyze({
      'A/Types.xaml.cs': types,
      'A/Registration.cs': `namespace N.Registration
{
    public class Registration
    {
        public void Register(NavigationService navigation)
        {
            navigation.RegisterRoute(typeof(N.Views.FirstViewModel), typeof(N.Views.FirstPage));
        }
    }
}`,
    }, {
      'A/FirstPage.xaml': '<Page x:Class="N.Views.FirstPage" />',
      'A/SecondPage.xaml': '<Page x:Class="N.Views.SecondPage" />',
    });

    const edge = model.edges.find((entry) => entry.kind === 'resolves');
    expect(edge).toMatchObject({
      source: 'N.Views.FirstPage',
      target: 'N.Views.FirstViewModel',
      confidence: 'medium',
      label: 'route registration',
      evidence: { file: 'A/Registration.cs', line: 7 },
    });
  });

  it('reads a tuple of navigation entries the same way', async () => {
    const model = await analyze({
      'A/Types.xaml.cs': types,
      'A/Registration.cs': `namespace N.Registration
{
    public class Registration
    {
        private static readonly (string, System.Type, System.Type)[] Entries =
        {
            ("First", typeof(N.Views.FirstViewModel), typeof(N.Views.FirstPage)),
        };
    }
}`,
    }, {
      'A/FirstPage.xaml': '<Page x:Class="N.Views.FirstPage" />',
      'A/SecondPage.xaml': '<Page x:Class="N.Views.SecondPage" />',
    });

    expect(model.edges.filter((entry) => entry.kind === 'resolves').map((entry) => entry.target))
      .toEqual(['N.Views.FirstViewModel']);
  });

  it('does not pair a view with a view model that merely shares a line', async () => {
    const model = await analyze({
      'A/Types.xaml.cs': types,
      'A/Uses.cs': `namespace N.Registration
{
    public class Uses
    {
        public bool Both(object value) => value is N.Views.FirstPage && value is N.Views.FirstViewModel;
    }
}`,
    }, {
      'A/FirstPage.xaml': '<Page x:Class="N.Views.FirstPage" />',
      'A/SecondPage.xaml': '<Page x:Class="N.Views.SecondPage" />',
    });

    expect(model.edges.filter((entry) => entry.kind === 'resolves')).toEqual([]);
  });
});

describe('analyzeWorkspace: names two types would otherwise share', () => {
  it('qualifies a repeated label on the canvas but leaves the label itself alone', async () => {
    const model = await analyze({
      'Alpha/Shell.cs': 'namespace App { public class Shell {} }',
      'Beta/Shell.cs': 'namespace Tools { public class Shell {} }',
    });

    // The label is what a symbol search matches and what finding titles read, so it must stay the
    // plain type name; the canvas gets the unambiguous form instead.
    expect(model.nodes.map((node) => node.label).sort()).toEqual(['Shell', 'Shell']);
    expect(model.nodes.map((node) => node.displayLabel).sort())
      .toEqual(['Shell · Alpha', 'Shell · Beta']);
  });

  it('qualifies with the full name when two same-named types share a module', async () => {
    const model = await analyze({
      'One/Same.cs': 'namespace First { public class Shell {} }',
      'One/Other.cs': 'namespace Second { public class Shell {} }',
    });

    expect(model.nodes.map((node) => node.displayLabel).sort())
      .toEqual(['Shell · First.Shell', 'Shell · Second.Shell']);
  });


  it('qualifies with the full name when the module is only a placeholder', async () => {    // A file at the repository root lands in "(root)", which says nothing about where the type lives.
    const model = await analyze({
      'Root.cs': 'namespace App { public class Shell {} }',
      'Alpha/Shell.cs': 'namespace Tools { public class Shell {} }',
    });

    expect(model.nodes.map((node) => node.displayLabel).sort())
      .toEqual(['Shell · Alpha', 'Shell · App.Shell']);
  });

  it('leaves a unique label without a display override', async () => {
    const model = await analyze({
      'A/Types.cs': 'namespace N { public class Holder {} public class Dep {} }',
    });
    expect(model.nodes.every((node) => node.displayLabel === undefined)).toBe(true);
  });
});


describe('analyzeWorkspace: a profile’s conventions', () => {
  const profiled = (profile: Parameters<typeof compileConventions>[0]) => ({
    conventions: compileConventions(profile),
  });

  it('draws a registration through a call name only the profile declares', async () => {
    const sources = {
      'App/App.cs': `namespace App
{
    public class App
    {
        void Wire(Pages pages)
        {
            pages.Wire<SampleViewModel, SamplePage>();
        }
    }
    public class Pages { }
}`,
      'App/SamplePage.cs': 'namespace App { public class SamplePage { } }',
      'App/SampleViewModel.cs': 'namespace App { public class SampleViewModel { } }',
    };

    const model = await analyze(sources, {}, profiled({
      version: 1,
      links: { genericPairCalls: ['Wire'] },
      layers: { rules: [{ layer: 'viewmodel', nameSuffixes: ['ViewModel'] }], default: 'model' },
    }));

    expect(edgeKeys(model)).toContain('App.SamplePage -resolves-> App.SampleViewModel');
    // Both sides of the pair are registered, so neither reads as an unused declaration.
    expect(nodeOf(model, 'App.SamplePage')?.diRegistered).toBe(true);
    expect(nodeOf(model, 'App.SampleViewModel')?.diRegistered).toBe(true);
    const unused = model.findings.filter((finding) => finding.kind === 'unused-type').map((finding) => finding.title);
    expect(unused.join(' ')).not.toContain('SamplePage');
    expect(unused.join(' ')).not.toContain('SampleViewModel');
  });

  it('draws a route through a typeof pair call name only the profile declares', async () => {
    const sources = {
      'App/Routes.cs': `namespace App
{
    public class Routes
    {
        void MapAll(Navigation navigation)
        {
            navigation.Map(typeof(SampleViewModel), typeof(SamplePage));
        }
    }
    public class Navigation { }
}`,
      'App/SamplePage.cs': 'namespace App { public class SamplePage { } }',
      'App/SampleViewModel.cs': 'namespace App { public class SampleViewModel { } }',
    };

    const withProfile = await analyze(sources, {}, profiled({
      version: 1,
      links: { typeofPairCalls: ['Map'] },
    }));
    expect(edgeKeys(withProfile)).toContain('App.SamplePage -resolves-> App.SampleViewModel');
    expect(nodeOf(withProfile, 'App.SampleViewModel')?.diRegistered).toBe(true);

    // The built-in conventions know no such call, so nothing is drawn from it.
    const without = await analyze(sources);
    expect(edgeKeys(without)).not.toContain('App.SamplePage -resolves-> App.SampleViewModel');
  });

  it('gates the dependency-property edge on the profile’s view-model test, not on a suffix', async () => {
    const sources = {
      'App/HomePage.cs': `namespace App
{
    public class HomePage
    {
        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register(typeof(ViewModelLabel), typeof(HomeScreen), typeof(HomePage));
    }
    public class DependencyProperty { }
    public class ViewModelLabel { }
}`,
      'App/HomeScreen.cs': 'namespace App { public class HomeScreen { } }',
    };

    const conventions = profiled({
      version: 1,
      layers: { rules: [{ layer: 'viewmodel', nameSuffixes: ['Screen'] }], default: 'model' },
    });
    const model = await analyze(sources, {}, conventions);

    expect(edgeKeys(model)).toContain('App.HomePage -getservice-> App.HomeScreen');
    expect(nodeOf(model, 'App.HomeScreen')?.layer).toBe('viewmodel');
    expect(nodeOf(model, 'App.HomeScreen')?.diRegistered).toBe(true);

    // With the built-in rules a `Screen` is a plain model, so the same call draws nothing.
    const without = await analyze(sources);
    expect(edgeKeys(without)).not.toContain('App.HomePage -getservice-> App.HomeScreen');
  });

  it('draws the data-context binding edge through the cast keyword the profile declares', async () => {
    const sources = {
      'App/GridControl.cs': `namespace App
{
    public class GridControl
    {
        void Bind()
        {
            var commands = (IGridCommands)DataContext;
        }
    }
    public interface IGridCommands { }
}`,
    };

    const model = await analyze(sources, {}, profiled({
      version: 1,
      links: { dataContextCasts: ['cast'] },
    }));
    const binds = model.edges.filter((edge) => edge.kind === 'binds');
    expect(binds.map((edge) => `${edge.source}->${edge.target}`)).toContain('App.GridControl->App.IGridCommands');

    // `is` and `as` are the built-in casts, so a plain cast is an ordinary reference instead.
    const without = await analyze(sources);
    expect(without.edges.some((edge) => edge.kind === 'binds')).toBe(false);
  });

  it('produces the same graph for a built-in profile as for no profile at all', async () => {
    const sources = {
      'App/App.cs': `namespace App
{
    public class App
    {
        void Wire(PageService pageService, IServiceCollection services)
        {
            pageService.Configure<SampleViewModel, SamplePage>();
            services.AddSingleton<IRepo, Repo>();
        }
    }
    public class PageService { }
    public interface IServiceCollection { }
}`,
      'App/SamplePage.cs': 'namespace App { public class SamplePage { } }',
      'App/SampleViewModel.cs': 'namespace App { public class SampleViewModel { } }',
      'App/IRepo.cs': 'namespace App { public interface IRepo { } public class Repo : IRepo { } }',
    };

    const none = await analyze(sources);
    const builtIn = await analyze(sources, {}, profiled(undefined));
    expect(edgeKeys(builtIn)).toEqual(edgeKeys(none));
    expect(builtIn.nodes.map((node) => `${node.id}:${node.layer}`).sort())
      .toEqual(none.nodes.map((node) => `${node.id}:${node.layer}`).sort());
    expect(builtIn.findings.map((finding) => finding.title).sort())
      .toEqual(none.findings.map((finding) => finding.title).sort());
  });
});
