import { describe, expect, it } from 'vitest';
import { classifyType, isViewModelLike, isViewModelName } from '../../src/graph/layers';
import { compileConventions, type CompiledConventions } from '../../src/graph/conventions';
import { builtInProfile, type LayerRule, type RepositoryProfile } from '../../src/graph/profile';
import { typeDecl } from '../support/model';

type TypeDecl = ReturnType<typeof typeDecl>;

const DEFAULT: CompiledConventions = compileConventions();

/** A repository whose conventions are the built-in ones, which is what no profile gets. */
const builtIn = () => compileConventions(builtInProfile());

/** A repository that declares its own rules, in the order the profile gives them. */
const profiled = (rules: LayerRule[], fallback: 'view' | 'viewmodel' | 'service' | 'model' = 'model') => {
  const profile: RepositoryProfile = { version: 1, layers: { rules, default: fallback } };
  return compileConventions(profile);
};

const classify = (
  name: string,
  namespace = 'App',
  kind: TypeDecl['kind'] = 'class',
  conventions: CompiledConventions = DEFAULT,
) => classifyType(typeDecl({ name, namespace, kind }), conventions);

describe('isViewModelName', () => {
  it('recognises the suffix and both prefixes used by this workspace', () => {
    expect(isViewModelName('SampleViewModel', DEFAULT)).toBe(true);
    expect(isViewModelName('ViewModel_Sample', DEFAULT)).toBe(true);
    expect(isViewModelName('VM_Sample', DEFAULT)).toBe(true);
  });

  it('does not treat a model or a service as a view model', () => {
    expect(isViewModelName('Sample', DEFAULT)).toBe(false);
    expect(isViewModelName('SampleService', DEFAULT)).toBe(false);
    expect(isViewModelName('SampleViewModelFactory', DEFAULT)).toBe(false);
  });

  it('follows a profile that calls its view models something else', () => {
    const conventions = profiled([{ layer: 'viewmodel', nameSuffixes: ['Screen'] }]);
    expect(isViewModelName('SignInScreen', conventions)).toBe(true);
    expect(isViewModelName('SignInViewModel', conventions)).toBe(false);
  });
});

describe('classifyType: no profile', () => {
  it('classifies a view model by name suffix', () => {
    expect(classify('SignInViewModel')).toBe('viewmodel');
  });

  it('classifies a view model by prefix', () => {
    expect(classify('ViewModel_SignIn')).toBe('viewmodel');
    expect(classify('VM_SignIn')).toBe('viewmodel');
  });

  it('classifies a view model by a whole ViewModels namespace segment', () => {
    expect(classify('SignIn', 'App.ViewModels')).toBe('viewmodel');
  });

  it('keeps a record in a ViewModels namespace as data', () => {
    // Row models, records and navigation payloads live beside the view models that own them, so the
    // namespace alone is not enough: only a class whose name does not say "I am data" counts.
    expect(classify('SignInRow', 'App.ViewModels', 'record')).toBe('model');
    expect(classify('SignInResult', 'App.ViewModels', 'record')).toBe('model');
  });

  it('keeps a data-shaped class in a ViewModels namespace as a model', () => {
    for (const name of [
      'PartStockLocationItem', 'SetupCompletionNavigationData', 'AnswerOptionCard',
      'GenericRequestSummaryRow', 'PartPictureSystemOption', 'SignInCredentials',
      'SetUpRequestedEventArgs', 'SetUpEnterOutcome', 'DunnageCatalogueLoadResult',
    ]) {
      expect(classify(name, 'App.ViewModels')).toBe('model');
    }
  });

  it('still prefers a ViewModel name wherever it is declared', () => {
    expect(classify('SignInViewModel', 'App.ViewModels', 'record')).toBe('viewmodel');
    expect(classify('ViewModel_DatabaseDeployment', 'App.Models')).toBe('viewmodel');
  });

  it('keeps an enum in a ViewModels namespace an enum', () => {
    expect(classify('SortOrder', 'App.ViewModels', 'enum')).toBe('model');
  });

  it('does not read ViewModels as Models', () => {
    expect(classify('SignIn', 'App.Models')).toBe('model');
    expect(classify('SignIn', 'App.ViewModels')).toBe('viewmodel');
  });

  it('classifies a service by suffix', () => {
    for (const name of ['SetupService', 'RunHistoryStore', 'ThemeProvider', 'PartReader', 'ImageCatalog']) {
      expect(classify(name)).toBe('service');
    }
  });

  it('classifies a service by prefix', () => {
    expect(classify('Dao_Part')).toBe('service');
    expect(classify('Service_Label')).toBe('service');
    expect(classify('Repository_WorkOrder')).toBe('service');
  });

  it('classifies a service by its namespace', () => {
    expect(classify('Helper', 'App.Services')).toBe('service');
  });

  it('lets a view-model name beat the Services namespace', () => {
    expect(classify('PanelViewModel', 'App.Services')).toBe('viewmodel');
  });

  it('keeps an interface with a service role in the service layer', () => {
    expect(classify('IDataService', 'App', 'interface')).toBe('service');
    expect(classify('IPartCatalog', 'App', 'interface')).toBe('service');
    expect(classify('IThing', 'App.Services', 'interface')).toBe('service');
    expect(classify('IThing', 'App.Contracts', 'interface')).toBe('service');
  });

  it('judges an interface before a view-model naming convention', () => {
    expect(classify('IResettableViewModel', 'App', 'interface')).toBe('model');
  });

  it('classifies an enum as a model', () => {
    expect(classify('Status', 'App', 'enum')).toBe('model');
  });

  it('classifies anything else as a model', () => {
    expect(classify('Part', 'App')).toBe('model');
    expect(classify('Part', 'App.Models')).toBe('model');
  });

  it('reads an explicitly built-in profile exactly as no profile at all', () => {
    const cases: [string, string, TypeDecl['kind']][] = [
      ['SignInViewModel', 'App', 'class'],
      ['SignIn', 'App.ViewModels', 'class'],
      ['SortOrder', 'App.ViewModels', 'enum'],
      ['IResettableViewModel', 'App', 'interface'],
      ['IDataService', 'App', 'interface'],
      ['Dao_Part', 'App', 'class'],
      ['Helper', 'App.Services', 'class'],
      ['Part', 'App', 'class'],
    ];
    for (const [name, namespace, kind] of cases) {
      expect(classify(name, namespace, kind, builtIn())).toBe(classify(name, namespace, kind));
    }
  });
});

describe('classifyType: a profile’s own rules', () => {
  it('places a type by a suffix only the profile names', () => {
    const conventions = profiled([{ layer: 'service', nameSuffixes: ['Handler'] }]);
    expect(classify('DunnageHandler', 'App.Dunnage', 'class', conventions)).toBe('service');
    // The built-in suffix list is replaced, not extended: a profile that names one suffix reads
    // every other ending as it would read any unknown name.
    expect(classify('SetupService', 'App', 'class', conventions)).toBe('model');
  });

  it('lets a profile rule placed first win over the ViewModels namespace rule', () => {
    const conventions = profiled([
      { layer: 'service', nameSuffixes: ['Screen'] },
      { layer: 'viewmodel', namespaceSegments: ['ViewModels'] },
    ]);
    expect(classify('OrderScreen', 'App.ViewModels', 'class', conventions)).toBe('service');
    // A type the first rule does not match still reaches the namespace rule.
    expect(classify('Order', 'App.ViewModels', 'class', conventions)).toBe('viewmodel');
  });

  it('keeps a row model a model through unlessNameSuffixes', () => {
    const conventions = profiled([
      { layer: 'viewmodel', namespaceSegments: ['ViewModels'], unlessNameSuffixes: ['Row', 'Card'] },
    ]);
    expect(classify('OrderScreen', 'App.ViewModels', 'class', conventions)).toBe('viewmodel');
    expect(classify('OrderRow', 'App.ViewModels', 'class', conventions)).toBe('model');
    expect(classify('OrderCard', 'App.ViewModels', 'class', conventions)).toBe('model');
  });

  it('applies a rule with kinds only to that kind', () => {
    const conventions = profiled([{ layer: 'viewmodel', kinds: ['record'], nameSuffixes: ['Screen'] }]);
    expect(classify('OrderScreen', 'App', 'record', conventions)).toBe('viewmodel');
    expect(classify('OrderScreen', 'App', 'class', conventions)).toBe('model');
  });

  it('uses the profile’s default for the type no rule matches', () => {
    const conventions = profiled([{ layer: 'service', nameSuffixes: ['Handler'] }], 'view');
    expect(classify('Part', 'App', 'class', conventions)).toBe('view');
  });
});

describe('isViewModelLike', () => {
  it('uses the declaration when there is one', () => {
    expect(isViewModelLike(typeDecl({ name: 'SampleViewModel' }), 'ignored', DEFAULT)).toBe(true);
    expect(isViewModelLike(typeDecl({ name: 'Part' }), 'SampleViewModel', DEFAULT)).toBe(false);
  });

  it('falls back to the written name when nothing was resolved', () => {
    expect(isViewModelLike(undefined, 'SampleViewModel', DEFAULT)).toBe(true);
    expect(isViewModelLike(undefined, 'Part', DEFAULT)).toBe(false);
  });

  it('judges a declaration by the whole rule list, so a namespace can decide', () => {
    // A type in a ViewModels folder is a view model even when its name says nothing about it, which
    // is what the dependency-property edge asks when it gates on the view-model test.
    expect(isViewModelLike(typeDecl({ name: 'SignIn', namespace: 'App.ViewModels' }), 'SignIn', DEFAULT)).toBe(true);
    expect(isViewModelLike(undefined, 'SignIn', DEFAULT)).toBe(false);
  });
});

describe('classifyType: a declaration with no namespace', () => {
  it('treats an empty namespace as no namespace', () => {
    expect(classifyType(typeDecl({ name: 'Part', namespace: '' }), DEFAULT)).toBe('model');
    expect(classifyType(typeDecl({ name: 'PartService', namespace: '' }), DEFAULT)).toBe('service');
  });
});
