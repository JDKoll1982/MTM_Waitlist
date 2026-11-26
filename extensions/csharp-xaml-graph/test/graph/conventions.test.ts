import { describe, expect, it } from 'vitest';
import {
  compileConventions,
  evaluateLayerRules,
  hasNamespaceSegment,
  isViewModelType,
  type CompiledConventions,
} from '../../src/graph/conventions';
import { builtInProfile, type LayerRule, type RepositoryProfile } from '../../src/graph/profile';
import { typeDecl } from '../support/model';

const builtIn: CompiledConventions = compileConventions();

const layerOf = (partial: Parameters<typeof typeDecl>[0], conventions: CompiledConventions = builtIn) =>
  evaluateLayerRules(conventions.layerRules, conventions.layerDefault, partial);

describe('the built-in rule list keeps today’s classification', () => {
  const conventions = compileConventions(builtInProfile());

  it('classifies a service interface', () => {
    expect(layerOf({ name: 'IDataService', kind: 'interface' }, conventions)).toBe('service');
    expect(layerOf({ name: 'IPartCatalog', kind: 'interface' }, conventions)).toBe('service');
    expect(layerOf({ name: 'IThing', namespace: 'App.Services', kind: 'interface' }, conventions)).toBe('service');
    expect(layerOf({ name: 'IThing', namespace: 'App.Contracts', kind: 'interface' }, conventions)).toBe('service');
    expect(layerOf({ name: 'Service_Thing', kind: 'interface' }, conventions)).toBe('service');
  });

  it('classifies an interface with no service role as a model', () => {
    expect(layerOf({ name: 'IThing', kind: 'interface' }, conventions)).toBe('model');
    // An interface is judged before the naming conventions, so a view-model shaped interface stays a
    // model rather than being tinted like a page's view model.
    expect(layerOf({ name: 'IResettableViewModel', kind: 'interface' }, conventions)).toBe('model');
  });

  it('classifies a view model by suffix and by prefix', () => {
    expect(layerOf({ name: 'SignInViewModel' }, conventions)).toBe('viewmodel');
    expect(layerOf({ name: 'ViewModel_SignIn' }, conventions)).toBe('viewmodel');
    expect(layerOf({ name: 'VM_SignIn' }, conventions)).toBe('viewmodel');
  });

  it('classifies a view model by a whole ViewModels namespace segment', () => {
    expect(layerOf({ name: 'SignIn', namespace: 'App.ViewModels' }, conventions)).toBe('viewmodel');
    // A substring test for `Models` would match `ViewModels`; whole segments are what keep them apart.
    expect(layerOf({ name: 'SignIn', namespace: 'App.Models' }, conventions)).toBe('model');
  });

  it('keeps a class whose name reads as data a model even in a ViewModels namespace', () => {
    for (const name of [
      'SignInRow', 'SetupCompletionNavigationData', 'AnswerOptionCard', 'SignInCredentials',
      'SetUpRequestedEventArgs', 'DunnageCatalogueLoadResult', 'ImageOverridePayload',
    ]) {
      expect(layerOf({ name, namespace: 'App.ViewModels' }, conventions)).toBe('model');
    }
  });

  it('classifies an enum as a model, even in a ViewModels namespace', () => {
    expect(layerOf({ name: 'SortOrder', namespace: 'App.ViewModels', kind: 'enum' }, conventions)).toBe('model');
  });

  it('classifies a service by suffix, prefix and namespace', () => {
    expect(layerOf({ name: 'SetupService' }, conventions)).toBe('service');
    expect(layerOf({ name: 'RunHistoryStore' }, conventions)).toBe('service');
    expect(layerOf({ name: 'Dao_Part' }, conventions)).toBe('service');
    expect(layerOf({ name: 'Helper', namespace: 'App.Services' }, conventions)).toBe('service');
  });

  it('lets a view-model name beat the Services namespace', () => {
    expect(layerOf({ name: 'PanelViewModel', namespace: 'App.Services' }, conventions)).toBe('viewmodel');
  });

  it('falls back to the built-in default for anything else', () => {
    expect(layerOf({ name: 'Part', namespace: 'App' }, conventions)).toBe('model');
    expect(conventions.layerDefault).toBe('model');
  });

  it('names a view model through the exported predicate, with and without a namespace', () => {
    expect(isViewModelType(conventions, { name: 'SignInViewModel' })).toBe(true);
    expect(isViewModelType(conventions, { name: 'Part' })).toBe(false);
    expect(isViewModelType(conventions, { name: 'SignIn', namespace: 'App.ViewModels' })).toBe(true);
  });

  it('matches whole namespace segments only', () => {
    expect(hasNamespaceSegment('App.ViewModels', 'ViewModels')).toBe(true);
    expect(hasNamespaceSegment('App.ViewModels.Extra', 'ViewModels')).toBe(true);
    expect(hasNamespaceSegment('App.MyViewModels', 'ViewModels')).toBe(false);
    expect(hasNamespaceSegment('', 'ViewModels')).toBe(false);
  });
});

describe('a profile’s own rules', () => {
  it('wins over a built-in rule when it is placed first', () => {
    const profile: RepositoryProfile = {
      version: 1,
      layers: {
        rules: [
          { layer: 'service', nameSuffixes: ['Screen'] },
          { layer: 'viewmodel', namespaceSegments: ['ViewModels'] },
        ],
        default: 'model',
      },
    };
    const conventions = compileConventions(profile);
    // A type carrying both signals lands where the profile's order says.
    expect(layerOf({ name: 'Screen', namespace: 'App.ViewModels' }, conventions)).toBe('service');
    // A present array replaces the built-in array rather than extending it.
    expect(conventions.layerRules).toHaveLength(2);
  });

  it('places a type by a suffix the built-in set has never seen', () => {
    const conventions = compileConventions({
      version: 1,
      layers: { rules: [{ layer: 'service', nameSuffixes: ['Handler'] }], default: 'model' },
    });
    expect(layerOf({ name: 'DunnageHandler' }, conventions)).toBe('service');
    expect(layerOf({ name: 'SetupService' }, conventions)).toBe('model');
  });

  it('keeps a row model a model through unlessNameSuffixes', () => {
    const conventions = compileConventions({
      version: 1,
      layers: {
        rules: [{
          layer: 'viewmodel',
          namespaceSegments: ['Screens'],
          unlessNameSuffixes: ['Row'],
        }],
        default: 'model',
      },
    });
    expect(layerOf({ name: 'OrderScreen', namespace: 'App.Screens' }, conventions)).toBe('viewmodel');
    expect(layerOf({ name: 'OrderRow', namespace: 'App.Screens' }, conventions)).toBe('model');
  });

  it('applies a rule with kinds only to that kind', () => {
    const conventions = compileConventions({
      version: 1,
      layers: { rules: [{ layer: 'viewmodel', kinds: ['record'], nameSuffixes: ['Screen'] }] },
    });
    expect(layerOf({ name: 'OrderScreen', kind: 'record' }, conventions)).toBe('viewmodel');
    expect(layerOf({ name: 'OrderScreen', kind: 'class' }, conventions)).toBe('model');
  });

  it('uses the profile’s default for a type no rule matches', () => {
    const conventions = compileConventions({
      version: 1,
      layers: { rules: [{ layer: 'service', nameSuffixes: ['Handler'] }], default: 'service' },
    });
    expect(conventions.layerDefault).toBe('service');
    expect(layerOf({ name: 'Part' }, conventions)).toBe('service');
  });

  it('treats an empty condition list as a condition nothing satisfies', () => {
    const rules: LayerRule[] = [{ layer: 'service', nameSuffixes: [] }];
    expect(evaluateLayerRules(rules, 'model', { name: 'SetupService' })).toBe('model');
  });

  it('reads a rule with no conditions as matching every type', () => {
    const rules: LayerRule[] = [{ layer: 'view' }];
    expect(evaluateLayerRules(rules, 'model', { name: 'Anything' })).toBe('view');
  });
});

describe('compileConventions', () => {
  it('fills every link the profile leaves out from the built-in conventions', () => {
    const conventions = compileConventions({ version: 1, links: { genericPairCalls: ['Wire'] } });
    expect([...conventions.genericPairCalls]).toEqual(['Wire']);
    expect(conventions.typeofPairCalls.has('RegisterRoute')).toBe(true);
    expect(conventions.dependencyPropertyCall).toBe('Register');
    expect(conventions.addCallPattern.test('AddScoped')).toBe(true);
    expect(conventions.serviceLocator.has('GetService')).toBe(true);
    expect(conventions.dataContextCasts.has('is')).toBe(true);
  });

  it('fills the findings and files a profile leaves out', () => {
    const conventions = compileConventions({ version: 1, findings: { entryPoints: ['Boot'] } });
    expect(conventions.entryPoints).toEqual(new Set(['Boot']));
    expect(conventions.transparentBases.has('UserControl')).toBe(true);
    expect(conventions.generatedMembers).toHaveLength(2);
    expect(conventions.include).toEqual(['**/*.cs', '**/*.xaml']);
    expect(conventions.exclude).toContain('**/bin/**');
  });

  it('compiles a profile that declares a section as empty without reading it as absent', () => {
    const conventions = compileConventions({ version: 1, findings: { entryPoints: [] } });
    expect(conventions.entryPoints.size).toBe(0);
  });

  it('reads a profile that declares nothing beyond the version as the built-in conventions', () => {
    const bare = compileConventions({ version: 1 });
    expect(bare.layerRules).toEqual(builtIn.layerRules);
    expect([...bare.entryPoints]).toEqual([...builtIn.entryPoints]);
    expect(bare.include).toEqual(builtIn.include);
  });
});
