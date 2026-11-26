import { beforeAll, describe, expect, it } from 'vitest';
import * as path from 'node:path';
import {
  initCSharpGrammar,
  normalizeTypeText,
  parseCSharpFile,
} from '../../src/graph/csharp/parser';

const grammarPath = path.join(process.cwd(), 'media', 'tree-sitter-c_sharp.wasm');

const parse = (source: string, file = 'src/Sample.cs') => parseCSharpFile(file, source);

describe('C# grammar loading', () => {
  it('refuses to parse before the grammar is loaded, rather than parsing nothing', () => {
    expect(() => parseCSharpFile('a.cs', 'class A {}')).toThrow(
      'C# grammar is not loaded; call initCSharpGrammar first.',
    );
  });

  it('loads the grammar once and tolerates a second initialisation', async () => {
    await initCSharpGrammar(grammarPath);
    await expect(initCSharpGrammar(grammarPath)).resolves.toBeUndefined();
  });
});

describe('normalizeTypeText', () => {
  it('drops generics, nullability, arrays and the global:: prefix', () => {
    expect(normalizeTypeText('global::System.Collections.Generic.List<Foo>')).toBe(
      'System.Collections.Generic.List',
    );
    expect(normalizeTypeText('Foo?')).toBe('Foo');
    expect(normalizeTypeText('Foo[]')).toBe('Foo');
    expect(normalizeTypeText('Foo[,]')).toBe('Foo');
    expect(normalizeTypeText('  Foo.Bar  ')).toBe('Foo.Bar');
  });

  it('keeps a name that is only a qualified name', () => {
    expect(normalizeTypeText('System.String')).toBe('System.String');
  });

  it('treats a missing type as empty', () => {
    expect(normalizeTypeText('')).toBe('');
    expect(normalizeTypeText(undefined as unknown as string)).toBe('');
  });
});

describe('parseCSharpFile: declarations', () => {
  beforeAll(async () => {
    await initCSharpGrammar(grammarPath);
  });

  it('records a file-scoped namespace for the types that follow it', () => {
    const file = parse(`
using System;
namespace Sample.App;

public class First {}
public class Second {}
`);
    expect(file.usings).toEqual(['System']);
    expect(file.types.map((type) => type.fullName)).toEqual(['Sample.App.First', 'Sample.App.Second']);
    expect(file.hasSyntaxError).toBe(false);
  });

  it('combines block, nested and file-scoped namespaces', () => {
    const file = parse(`
namespace Outer.Inner
{
    namespace Deeper
    {
        class Thing {}
    }
}
`);
    expect(file.types.map((type) => type.fullName)).toEqual(['Outer.Inner.Deeper.Thing']);
  });

  it('leaves a type in the global namespace with its bare name', () => {
    const file = parse('class Alone {}');
    expect(file.types[0].fullName).toBe('Alone');
    expect(file.types[0].namespace).toBe('');
  });

  it('resolves a nested type through its outer type, not by joining loose names', () => {
    const file = parse(`
namespace Sample
{
    class Outer
    {
        class Inner {}
        struct Deeper { class Deepest {} }
    }
}
`);
    expect(file.types.map((type) => type.fullName)).toEqual([
      'Sample.Outer',
      'Sample.Outer.Inner',
      'Sample.Outer.Deeper',
      'Sample.Outer.Deeper.Deepest',
    ]);
  });

  it('ignores a using alias, which names no namespace', () => {
    const file = parse(`
using System;
using Alias = Some.Thing;
namespace N { class A {} }
`);
    expect(file.usings).toEqual(['System']);
  });

  it('classifies every declaration kind', () => {
    const file = parse(`
namespace N
{
    public class A {}
    public record B(int Value);
    public record struct C(int Value);
    public struct D {}
    public interface IE {}
    public enum E { One, Two }
}
`);
    expect(file.types.map((type) => `${type.name}:${type.kind}`)).toEqual([
      'A:class', 'B:record', 'C:record', 'D:struct', 'IE:interface', 'E:enum',
    ]);
  });

  it('flags partial and abstract types, and normalises the base list', () => {
    const file = parse(`
namespace N
{
    public abstract partial class Base<T> : Middle<T>, IThing where T : class {}
    public class Middle<T> {}
    public interface IThing {}
}
`);
    const base = file.types[0];
    expect(base.isPartial).toBe(true);
    expect(base.isAbstract).toBe(true);
    expect(base.bases).toEqual(['Middle', 'IThing']);
  });

  it('reports a syntax error rather than silently dropping the file', () => {
    const file = parse('namespace N { class Broken { void M( } }');
    expect(file.hasSyntaxError).toBe(true);
  });
});

describe('parseCSharpFile: members', () => {
  beforeAll(async () => {
    await initCSharpGrammar(grammarPath);
  });

  const memberFile = () => parse(`
namespace N
{
    public class Sample : Base
    {
        private readonly string _name = "x";
        private int _count, _other;
        [ObservableProperty]
        private string _title = "";

        [JsonIgnore]
        public string Name => _name;

        [RelayCommand]
        private async Task SaveAsync() {}

        public Sample(IDep dep, int count) { }

        private static void Helper(ref int value) { }
    }
}
`);

  it('reads a constructor with typed parameters and its line', () => {
    const sample = memberFile().types[0];
    const constructor = sample.constructors[0];
    expect(constructor.params).toEqual([
      { type: 'IDep', name: 'dep' },
      { type: 'int', name: 'count' },
    ]);
    expect(constructor.line).toBe(17);
  });

  it('reads every declarator on a field, with modifiers, visibility and attributes', () => {
    const sample = memberFile().types[0];
    const names = sample.fields.map((field) => field.name);
    expect(names).toEqual(['_name', '_count', '_other', '_title']);

    const name = sample.fields[0];
    expect(name.type).toBe('string');
    expect(name.isReadonly).toBe(true);
    expect(name.visibility).toBe('private');

    const title = sample.fields.find((field) => field.name === '_title');
    expect(title?.attributes).toEqual(['ObservableProperty']);
  });

  it('reads properties with attributes, visibility and the ObservableProperty flag', () => {
    const sample = memberFile().types[0];
    const name = sample.properties.find((property) => property.name === 'Name');
    expect(name).toMatchObject({
      type: 'string',
      rawType: 'string',
      isPublic: true,
      visibility: 'public',
      isObservable: false,
      attributes: ['JsonIgnore'],
    });

    const generated = sample.properties.find((property) => property.name === '_title');
    expect(generated).toBeUndefined();
  });

  it('reads a method’s modifiers, attributes and parameter types', () => {
    const sample = memberFile().types[0];
    const save = sample.methods.find((method) => method.name === 'SaveAsync');
    expect(save).toMatchObject({
      returnType: 'Task',
      rawReturnType: 'Task',
      rawParamTypes: [],
      visibility: 'private',
      isAsync: true,
      isStatic: false,
      isOverride: false,
      isPartial: false,
      isExtension: false,
      attributes: ['RelayCommand'],
    });

    const helper = sample.methods.find((method) => method.name === 'Helper');
    expect(helper?.isStatic).toBe(true);
    expect(helper?.rawParamTypes).toEqual(['int']);
  });

  it('marks an extension method by its `this` parameter', () => {
    const file = parse(`
namespace N
{
    public static class Extensions
    {
        public static string Shout(this string value) => value;
    }
}
`);
    expect(file.types[0].methods[0].isExtension).toBe(true);
  });

  it('treats an interface member as public and a class member as private', () => {
    const file = parse(`
namespace N
{
    public interface IThing { void Do(); string Name { get; } }
    public class Thing { void Hidden() {} }
}
`);
    const iface = file.types[0];
    expect(iface.methods[0].visibility).toBe('public');
    expect(iface.properties[0].visibility).toBe('public');
    expect(file.types[1].methods[0].visibility).toBe('private');
  });

  it('turns a positional record parameter list into public properties', () => {
    const file = parse(`
namespace N
{
    public record Cell(string Title, string Detail);
    public record Cell2(string Title) { public string Title { get; init; } = Title; }
}
`);
    const cell = file.types[0];
    expect(cell.properties.map((property) => `${property.name}:${property.visibility}`)).toEqual([
      'Title:public', 'Detail:public',
    ]);

    // An explicitly declared property of the same name is not duplicated.
    const declared = file.types[1].properties.filter((property) => property.name === 'Title');
    expect(declared).toHaveLength(1);
  });
});

describe('parseCSharpFile: what the body then does', () => {
  beforeAll(async () => {
    await initCSharpGrammar(grammarPath);
  });

  const bodyFile = () => parse(`using System;
namespace N
{
  public class S
  {
    private readonly Dep _dep;
    private IDep? _other;

    public S(Dep dep)
    {
      _dep = dep ?? throw new ArgumentNullException(nameof(dep));
      _other = App.GetService<IDep>();
      ViewModel = new SampleViewModel();
      Name = "literal";
    }

    public void Run()
    {
      _dep.Do<Thing>(1);
      var local = new Other();
      var cast = (CastTarget)obj;
      var isCheck = obj is PatternType pattern;
      var isPlain = obj is PlainType;
      var asCheck = obj as AsTarget;
      var arr = new ArrTarget[2];
      var t = typeof(TypeTarget);
      try { } catch (CatchType error) { }
      StaticHolder.Go();
      Thing.Prop = 1;
    }
  }
}`);

  it('resolves a field’s declared type instead of the whole declaration text', () => {
    const fields = bodyFile().types[0].fields;
    expect(fields.map((field) => `${field.name}=${field.type}`)).toEqual([
      '_dep=Dep', '_other=IDep',
    ]);
    expect(fields[1].rawType).toBe('IDep?');
  });

  it('records assignments by shape: constructor variable, service locator, new instance, other', () => {
    const assignments = bodyFile().types[0].assignments;
    expect(assignments.map((site) => `${site.target}:${site.value.kind}`)).toEqual([
      '_dep:variable', '_other:service-locator', 'ViewModel:new', 'Name:other',
    ]);
    expect(assignments[0].value).toEqual({ kind: 'variable', name: 'dep' });
    expect(assignments[1].value).toEqual({ kind: 'service-locator', typeName: 'IDep' });
    expect(assignments[2].value).toEqual({ kind: 'new', typeName: 'SampleViewModel' });
    expect(assignments[3].value).toEqual({ kind: 'other', text: '"literal"' });
  });

  it('records generic member calls with their type arguments', () => {
    expect(bodyFile().types[0].calls).toEqual([
      { receiver: 'App', method: 'GetService', typeArgs: ['IDep'], typeOfArgs: [], line: 12 },
      { receiver: '_dep', method: 'Do', typeArgs: ['Thing'], typeOfArgs: [], line: 19 },
    ]);
  });

  it('records every object creation, including one inside a throw expression', () => {
    expect(bodyFile().types[0].creations).toEqual([
      { typeName: 'ArgumentNullException', line: 11 },
      { typeName: 'SampleViewModel', line: 13 },
      { typeName: 'Other', line: 20 },
    ]);
  });

  it('records a typeof reference once per type', () => {
    expect(bodyFile().types[0].typeofRefs).toEqual(['TypeTarget']);
  });

  it('names every type the body mentions, with the construct that named it', () => {
    const mentions = bodyFile().types[0].mentions;
    const via = (name: string): string[] =>
      mentions.filter((mention) => mention.name === name).map((mention) => mention.via);

    expect(via('Dep')).toEqual(['local', 'parameter']);
    expect(via('CastTarget')).toEqual(['cast']);
    expect(via('PatternType')).toEqual(['is']);
    expect(via('PlainType')).toEqual(['is']);
    expect(via('AsTarget')).toEqual(['as']);
    expect(via('ArrTarget')).toEqual(['new array']);
    expect(via('TypeTarget')).toEqual(['typeof']);
    expect(via('CatchType')).toEqual(['catch']);
    expect(via('StaticHolder')).toEqual(['static access']);
    expect(via('Thing')).toEqual(['static access']);
    expect(via('ArgumentNullException')).toEqual(['new']);
    expect(via('App')).toEqual(['static access']);
  });

  it('attributes, generic constraints and events are mentions too', () => {
    const file = parse(`
using System;
namespace N
{
    [Serializable]
    public class Holder<T> where T : ConstraintBase
    {
        public event EventHandler Changed;
        [Obsolete]
        public void Run() { }
    }
}
`);
    const mentions = file.types[0].mentions;
    expect(mentions.some((mention) => mention.name === 'Serializable' && mention.via === 'attribute')).toBe(true);
    expect(mentions.some((mention) => mention.name === 'Obsolete' && mention.via === 'attribute')).toBe(true);
    expect(mentions.some((mention) => mention.name === 'EventHandler')).toBe(true);
    expect(mentions.some((mention) => mention.via === 'constraint')).toBe(true);
  });

  it('names a static access only when the receiver can be a type', () => {
    const file = parse(`
namespace N
{
    public class S
    {
        void Run()
        {
            lowerCase.Property = 1;
            Upper.Property = 1;
        }
    }
}
`);
    const names = file.types[0].mentions.filter((mention) => mention.via === 'static access').map((mention) => mention.name);
    expect(names).toEqual(['Upper']);
  });

  it('does not treat a literal or an operator left of `=` as an assignment target', () => {
    const file = parse(`
namespace N
{
    public class S
    {
        void Run()
        {
            var list = new List<int>();
            this.Field = 1;
            list[0] = 2;
        }
    }
}
`);
    expect(file.types[0].assignments).toEqual([]);
  });

  it('records a multi-declarator statement once per declarator', () => {
    const file = parse(`
namespace N
{
    public class S
    {
        private int _a = 1, _b = 2;
    }
}
`);
    expect(file.types[0].fields.map((field) => field.name)).toEqual(['_a', '_b']);
  });
});

describe('parseCSharpFile: visibility, attributes and call shapes', () => {
  beforeAll(async () => {
    await initCSharpGrammar(grammarPath);
  });

  it('records protected and internal visibility', () => {
    const file = parse(`
namespace N
{
    public class Holder
    {
        protected void Protected() { }
        internal void Internal() { }
    }
}
`);
    expect(file.types[0].methods.map((method) => `${method.name}:${method.visibility}`)).toEqual([
      'Protected:protected',
      'Internal:internal',
    ]);
  });

  it('keeps an attribute that carries arguments, and drops the namespace and Attribute suffix', () => {
    const file = parse(`
namespace N
{
    [System.SerializableAttribute]
    public class Holder
    {
        [Obsolete("use something else")]
        public void Old() { }
    }
}
`);
    expect(file.types[0].methods[0].attributes).toEqual(['Obsolete']);
    expect(file.types[0].mentions.some((mention) => mention.name === 'Serializable')).toBe(true);
  });

  it('reads a service locator called without a receiver', () => {
    const file = parse(`
namespace N
{
    public class Holder
    {
        private IDep? _dep;
        void Run() { _dep = GetService<IDep>(); }
    }
    public interface IDep { }
}
`);
    expect(file.types[0].assignments[0].value).toEqual({ kind: 'service-locator', typeName: 'IDep' });
  });

  it('records a non-generic call assignment with its text', () => {
    const file = parse(`
namespace N
{
    public class Holder
    {
        private string _name;
        void Run() { _name = Builder.Create(); }
    }
}
`);
    expect(file.types[0].assignments[0].value).toEqual({ kind: 'other', text: 'Builder.Create' });
  });

  it('walks a compilation unit of top-level statements', () => {
    const file = parse('System.Console.WriteLine("hello");');
    expect(file.types).toEqual([]);
    expect(file.usings).toEqual([]);
  });

  it('reads a constructor that has no parameter list at all', () => {
    const file = parse('namespace N { public class Holder { static Holder() { } } }');
    expect(file.types[0].constructors[0].params).toEqual([]);
  });

  it('records an event field as a mention of its handler type', () => {
    const file = parse('using System;\nnamespace N { public class Holder { public event EventHandler? Changed; } }');
    expect(file.types[0].mentions.some((mention) => mention.name === 'EventHandler')).toBe(true);
  });
});

describe('parse: an assignment whose right side is a plain call', () => {
  it('records the call text when the callee is neither qualified nor generic', () => {
    const file = parse(`namespace N
{
    public class Holder
    {
        private string _name;
        void Run() { _name = Build(); }
    }
}
`);
    expect(file.types[0].assignments[0].value).toEqual({ kind: 'other', text: 'Build()' });
  });
});

describe('parse: a declaration with no body', () => {
  it('records a record declared without braces', () => {
    const file = parse('namespace N { public record Marker; }');
    expect(file.types.map((type) => type.name)).toEqual(['Marker']);
  });
});
