import { describe, expect, it } from 'vitest';
import { classifyType, isViewModelLike, isViewModelName } from '../../src/graph/layers';
import { typeDecl } from '../support/model';

const classify = (name: string, namespace = 'App', kind: TypeDecl['kind'] = 'class') =>
  classifyType(typeDecl({ name, namespace, kind }));

type TypeDecl = ReturnType<typeof typeDecl>;

describe('isViewModelName', () => {
  it('recognises the suffix and both prefixes used by this workspace', () => {
    expect(isViewModelName('SampleViewModel')).toBe(true);
    expect(isViewModelName('ViewModel_Sample')).toBe(true);
    expect(isViewModelName('VM_Sample')).toBe(true);
  });

  it('does not treat a model or a service as a view model', () => {
    expect(isViewModelName('Sample')).toBe(false);
    expect(isViewModelName('SampleService')).toBe(false);
    expect(isViewModelName('SampleViewModelFactory')).toBe(false);
  });
});

describe('classifyType', () => {
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
});

describe('isViewModelLike', () => {
  it('uses the declaration when there is one', () => {
    expect(isViewModelLike(typeDecl({ name: 'SampleViewModel' }), 'ignored')).toBe(true);
    expect(isViewModelLike(typeDecl({ name: 'Part' }), 'SampleViewModel')).toBe(false);
  });

  it('falls back to the written name when nothing was resolved', () => {
    expect(isViewModelLike(undefined, 'SampleViewModel')).toBe(true);
    expect(isViewModelLike(undefined, 'Part')).toBe(false);
  });
});

describe('classifyType: a declaration with no namespace', () => {
  it('treats an empty namespace as no namespace', () => {
    expect(classifyType(typeDecl({ name: 'Part', namespace: '' }))).toBe('model');
    expect(classifyType(typeDecl({ name: 'PartService', namespace: '' }))).toBe('service');
  });
});
