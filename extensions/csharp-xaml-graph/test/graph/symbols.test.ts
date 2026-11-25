import { describe, expect, it } from 'vitest';
import { TypeIndex, moduleOf } from '../../src/graph/csharp/symbols';
import { typeDecl } from '../support/model';

describe('TypeIndex', () => {
  it('stores the first declaration of a partial type and counts the type once', () => {
    const index = new TypeIndex();
    index.add(typeDecl({ name: 'Part', namespace: 'A', line: 1 }));
    index.add(typeDecl({ name: 'Part', namespace: 'A', line: 40 }));

    expect(index.size).toBe(1);
    expect(index.get('A.Part')?.line).toBe(1);
    expect(index.ambiguity('Part')).toBe(1);
  });

  it('lists every stored declaration', () => {
    const index = new TypeIndex();
    index.add(typeDecl({ name: 'A', namespace: 'N' }));
    index.add(typeDecl({ name: 'B', namespace: 'N' }));
    expect(index.all().map((decl) => decl.fullName)).toEqual(['N.A', 'N.B']);
  });

  it('reports how many distinct declarations carry a simple name', () => {
    const index = new TypeIndex();
    index.add(typeDecl({ name: 'Item', namespace: 'One' }));
    index.add(typeDecl({ name: 'Item', namespace: 'Two' }));
    expect(index.ambiguity('Item')).toBe(2);
    expect(index.ambiguity('Missing')).toBe(0);
  });

  describe('resolve', () => {
    const index = new TypeIndex();
    index.add(typeDecl({ name: 'Part', namespace: 'App.Models' }));
    index.add(typeDecl({ name: 'Part', namespace: 'App.Models.Deeper' }));
    index.add(typeDecl({ name: 'Unique', namespace: 'App.Models' }));
    index.add(typeDecl({ name: 'Shared', namespace: 'One' }));
    index.add(typeDecl({ name: 'Shared', namespace: 'Two' }));
    index.add(typeDecl({ name: 'Outer', namespace: 'Other' }));

    it('prefers an exact fully-qualified name', () => {
      expect(index.resolve('App.Models.Deeper.Part', [], '')?.fullName).toBe('App.Models.Deeper.Part');
    });

    it('walks the enclosing namespace and each of its ancestors', () => {
      expect(index.resolve('Unique', [], 'App.Models.Deeper')?.fullName).toBe('App.Models.Unique');
      expect(index.resolve('Part', [], 'App.Models.Deeper')?.fullName).toBe('App.Models.Deeper.Part');
    });

    it('tries every using before falling back to a fragile name match', () => {
      expect(index.resolve('Outer', ['Other'], 'App')?.fullName).toBe('Other.Outer');
    });

    it('resolves a unique simple name with no help at all', () => {
      expect(index.resolve('Unique', [], '')?.fullName).toBe('App.Models.Unique');
    });

    it('refuses to guess an ambiguous simple name', () => {
      expect(index.resolve('Shared', [], '')).toBeUndefined();
    });

    it('strips global:: and whitespace, and resolves a dotted name by its last segment', () => {
      expect(index.resolve(' global::Unique ', [], '')?.fullName).toBe('App.Models.Unique');
      expect(index.resolve('Some.Namespace.Unique', [], '')?.fullName).toBe('App.Models.Unique');
    });

    it('treats an empty reference as unresolved', () => {
      expect(index.resolve('', [], 'App')).toBeUndefined();
      expect(index.resolve('   ', [], 'App')).toBeUndefined();
      expect(index.resolve(undefined as unknown as string, [], 'App')).toBeUndefined();
    });
  });
});

describe('moduleOf', () => {
  it('uses the first directory as the module', () => {
    expect(moduleOf('Module_Setup/Views/SetupPage.xaml')).toBe('Module_Setup');
  });

  it('skips container directories such as src and lib', () => {
    expect(moduleOf('src/MTM_Waitlist.Core/Nav.cs')).toBe('MTM_Waitlist.Core');
    expect(moduleOf('lib/Feature/Thing.cs')).toBe('Feature');
    expect(moduleOf('tests/Thing/Thing.cs')).toBe('Thing');
  });

  it('treats `app` as a container too, so a folder called App is not a module name', () => {
    expect(moduleOf('SRC/App/Thing.cs')).toBe('(root)');
  });

  it('falls back to (root) when there is no directory to name', () => {
    expect(moduleOf('Thing.cs')).toBe('(root)');
    expect(moduleOf('src/Thing.cs')).toBe('(root)');
  });

  it('ignores the file name when choosing the module', () => {
    expect(moduleOf('A/B/File.cs')).toBe('A');
  });
});
