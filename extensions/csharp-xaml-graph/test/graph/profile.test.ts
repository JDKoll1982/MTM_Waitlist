import { describe, expect, it } from 'vitest';
import {
  PROFILE_FILE_NAME,
  builtInProfile,
  formatProfile,
  mergeProfile,
  parseProfile,
  type RepositoryProfile,
} from '../../src/graph/profile';
import { compileConventions } from '../../src/graph/conventions';

const read = (profile: RepositoryProfile) => parseProfile(formatProfile(profile));

/** What a chat assistant is asked to answer in. */
const MINIMAL = '{\n  "version": 1\n}\n';

describe('the built-in profile', () => {
  it('reads back with no problems', () => {
    const result = parseProfile(formatProfile(builtInProfile()));
    expect(result.profile).toEqual(builtInProfile());
    expect(result.problems).toEqual([]);
  });

  it('states the format version the import insists on', () => {
    expect(builtInProfile().version).toBe(1);
  });

  it('names the file a repository commits', () => {
    expect(PROFILE_FILE_NAME).toBe('csharp-xaml-graph.profile.json');
  });

  it('carries today’s conventions, so an unprofiled repository reads the same', () => {
    const conventions = compileConventions();
    expect(conventions.genericPairCalls.has('Configure')).toBe(true);
    expect(conventions.typeofPairCalls.has('RegisterRoute')).toBe(true);
    expect(conventions.dependencyPropertyCall).toBe('Register');
    expect(conventions.addCallPattern.test('AddTransient')).toBe(true);
    expect(conventions.serviceLocator.has('GetRequiredService')).toBe(true);
    expect(conventions.dataContextCasts.has('as')).toBe(true);
    expect(conventions.entryPoints).toEqual(new Set(['App', 'Program', 'Startup', 'Main']));
    expect(conventions.transparentBases.has('Page')).toBe(true);
    expect(conventions.generatedMembers).toEqual([
      { attribute: 'RelayCommand', on: 'method', form: 'commandSuffix' },
      { attribute: 'ObservableProperty', on: 'field', form: 'pascalCaseWithoutPrefix' },
    ]);
    expect(conventions.include).toEqual(['**/*.cs', '**/*.xaml']);
    expect(conventions.exclude).toContain('**/obj/**');
  });
});

describe('parseProfile: a file that cannot be used', () => {
  it('treats a blank file as absent with a message rather than a parse failure', () => {
    for (const text of ['', '   \n\t\n']) {
      const result = parseProfile(text);
      expect(result.profile).toBeUndefined();
      expect(result.problems).toHaveLength(1);
      expect(result.problems[0].severity).toBe('warning');
      expect(result.problems[0].message).toContain('empty');
    }
  });

  it('refuses text that is not JSON, naming the file', () => {
    const result = parseProfile('{ not json');
    expect(result.profile).toBeUndefined();
    expect(result.problems[0]).toMatchObject({ path: PROFILE_FILE_NAME, severity: 'error' });
    expect(result.problems[0].message).toContain('could not be read as JSON');
  });

  it('refuses a document that is not an object', () => {
    for (const text of ['42', '"text"', 'null', '[1, 2]']) {
      const result = parseProfile(text);
      expect(result.profile).toBeUndefined();
      expect(result.problems[0]).toMatchObject({ path: PROFILE_FILE_NAME, severity: 'error' });
    }
  });

  it('refuses a reply without version 1, naming the missing part', () => {
    const missing = parseProfile('{ "repository": "sample" }');
    expect(missing.profile).toBeUndefined();
    expect(missing.problems[0]).toMatchObject({ path: 'version', severity: 'error' });
    expect(missing.problems[0].message).toContain('"version": 1');

    const wrong = parseProfile('{ "version": 2 }');
    expect(wrong.profile).toBeUndefined();
    expect(wrong.problems[0].path).toBe('version');
    expect(wrong.problems[0].message).toContain('built-in conventions apply');
  });

  it('accepts a profile that declares nothing beyond the version', () => {
    const result = parseProfile(MINIMAL);
    expect(result.problems).toEqual([]);
    expect(result.profile).toEqual({ version: 1 });
  });
});

describe('parseProfile: partial and unrecognised content', () => {
  it('reports an unknown key and keeps the rest', () => {
    const result = parseProfile('{ "version": 1, "nonsense": true, "repository": "sample" }');
    expect(result.profile?.repository).toBe('sample');
    expect(result.problems).toHaveLength(1);
    expect(result.problems[0]).toMatchObject({ path: 'nonsense', severity: 'warning' });
  });

  it('reports an unknown key inside a section', () => {
    const result = parseProfile('{ "version": 1, "layers": { "extra": 1, "default": "service" } }');
    expect(result.profile?.layers?.default).toBe('service');
    expect(result.problems.map((problem) => problem.path)).toContain('layers.extra');
  });

  it('keeps the other sections when one is malformed', () => {
    const result = parseProfile(`{
      "version": 1,
      "layers": "not a section",
      "findings": { "entryPoints": ["Boot"] }
    }`);
    expect(result.profile?.layers).toBeUndefined();
    expect(result.profile?.findings?.entryPoints).toEqual(['Boot']);
    expect(result.problems[0]).toMatchObject({ path: 'layers', severity: 'warning' });
    // The layer rules fall back to the built-in list, which still classifies what the profile omits.
    expect(compileConventions(result.profile).layerRules.length).toBeGreaterThan(0);
  });

  it('keeps the other rules of a section whose list has a bad entry', () => {
    const result = parseProfile(`{
      "version": 1,
      "layers": { "rules": [
        { "layer": "service", "nameSuffixes": ["Handler"] },
        { "layer": "not-a-layer" },
        "not a rule"
      ] }
    }`);
    expect(result.profile?.layers?.rules).toEqual([{ layer: 'service', nameSuffixes: ['Handler'] }]);
    expect(result.problems.map((problem) => problem.path)).toEqual([
      'layers.rules[1].layer',
      'layers.rules[2]',
    ]);
  });

  it('skips a rule that names no layer', () => {
    const result = parseProfile('{ "version": 1, "layers": { "rules": [{ "nameSuffixes": ["Handler"] }] } }');
    expect(result.profile?.layers?.rules).toEqual([]);
    expect(result.problems[0].path).toBe('layers.rules[0].layer');
    expect(result.problems[0].message).toContain('states the layer');
  });

  it('drops a declaration kind the format does not name, keeping the others', () => {
    const result = parseProfile(`{
      "version": 1,
      "layers": { "rules": [{ "layer": "service", "kinds": ["class", "delegate"] }] }
    }`);
    expect(result.profile?.layers?.rules?.[0].kinds).toEqual(['class']);
    expect(result.problems.some((problem) => problem.message.includes('not a declaration kind'))).toBe(true);
  });

  it('drops a list entry that is not a string, keeping the strings around it', () => {
    const result = parseProfile('{ "version": 1, "findings": { "entryPoints": ["App", 7, "Boot"] } }');
    expect(result.profile?.findings?.entryPoints).toEqual(['App', 'Boot']);
    expect(result.problems[0].path).toBe('findings.entryPoints[1]');
  });

  it('applies a repeated declaration once', () => {
    const result = parseProfile('{ "version": 1, "findings": { "entryPoints": ["App", " App ", "App"] } }');
    expect(result.profile?.findings?.entryPoints).toEqual(['App']);
    expect(result.problems).toEqual([]);
  });

  it('reports a value of the wrong type instead of carrying it', () => {
    const result = parseProfile('{ "version": 1, "repository": 7, "files": { "include": "**/*.cs" } }');
    expect(result.profile?.repository).toBeUndefined();
    expect(result.profile?.files?.include).toBeUndefined();
    expect(result.problems.map((problem) => problem.path)).toEqual([
      'repository',
      'files.include',
    ]);
  });

  it('describes what it found, whatever the value was', () => {
    const cases: [string, string][] = [
      ['{ "version": 1, "files": { "include": null } }', 'null'],
      ['{ "version": 1, "files": { "include": {} } }', 'an object'],
      ['{ "version": 1, "files": { "include": true } }', 'a boolean'],
      ['{ "version": 1, "files": { "include": [[]] } }', 'a list'],
    ];
    for (const [text, found] of cases) {
      expect(parseProfile(text).problems[0].message).toContain(found);
    }
  });

  it('refuses a registration pattern that is not a usable expression', () => {
    const result = parseProfile('{ "version": 1, "links": { "addCallPattern": "^(Add" } }');
    expect(result.profile?.links?.addCallPattern).toBeUndefined();
    expect(result.problems[0]).toMatchObject({ path: 'links.addCallPattern', severity: 'warning' });
    // The built-in pattern is what the analysis then applies.
    expect(compileConventions(result.profile).addCallPattern.test('AddSingleton')).toBe(true);
  });

  it('keeps a usable registration pattern', () => {
    const result = parseProfile('{ "version": 1, "links": { "addCallPattern": "^Wire" } }');
    expect(result.profile?.links?.addCallPattern).toBe('^Wire');
    expect(result.problems).toEqual([]);
  });
});

describe('parseProfile: generated members', () => {
  it('reads a complete rule', () => {
    const result = parseProfile(`{
      "version": 1,
      "findings": { "generatedMembers": [
        { "attribute": "RelayCommand", "on": "method", "form": "commandSuffix" }
      ] }
    }`);
    expect(result.profile?.findings?.generatedMembers).toEqual([
      { attribute: 'RelayCommand', on: 'method', form: 'commandSuffix' },
    ]);
  });

  it('skips a rule that does not say where the attribute sits', () => {
    const result = parseProfile(`{
      "version": 1,
      "findings": { "generatedMembers": [{ "attribute": "RelayCommand", "form": "commandSuffix" }] }
    }`);
    expect(result.profile?.findings?.generatedMembers).toEqual([]);
    expect(result.problems.map((problem) => problem.path)).toEqual(['findings.generatedMembers[0]']);
    expect(result.problems[0].message).toContain('whether it sits on a method or a field');
  });

  it('reports a naming form the format does not name', () => {
    const result = parseProfile(`{
      "version": 1,
      "findings": { "generatedMembers": [
        { "attribute": "X", "on": "field", "form": "camelCase" }
      ] }
    }`);
    expect(result.profile?.findings?.generatedMembers).toEqual([]);
    expect(result.problems[0].message).toContain('pascalCaseWithoutPrefix');
  });

  it('skips an entry that is not a rule at all', () => {
    const result = parseProfile('{ "version": 1, "findings": { "generatedMembers": ["RelayCommand"] } }');
    expect(result.profile?.findings?.generatedMembers).toEqual([]);
    expect(result.problems[0].path).toBe('findings.generatedMembers[0]');
  });

  it('refuses a list of rules that is not a list', () => {
    const result = parseProfile('{ "version": 1, "findings": { "generatedMembers": {} } }');
    expect(result.profile?.findings?.generatedMembers).toBeUndefined();
    expect(result.problems[0].path).toBe('findings.generatedMembers');
  });

  it('applies a repeated rule once and reports nothing', () => {
    const result = parseProfile(`{
      "version": 1,
      "findings": { "generatedMembers": [
        { "attribute": "RelayCommand", "on": "method", "form": "commandSuffix" },
        { "attribute": "relaycommand", "on": "method", "form": "commandSuffix" }
      ] }
    }`);
    expect(result.profile?.findings?.generatedMembers).toHaveLength(1);
    expect(result.problems).toEqual([]);
  });

  it('reports a key inside a rule it does not know', () => {
    const result = parseProfile(`{
      "version": 1,
      "findings": { "generatedMembers": [
        { "attribute": "X", "on": "method", "form": "commandSuffix", "note": "hi" }
      ] }
    }`);
    expect(result.problems[0].path).toBe('findings.generatedMembers[0].note');
    expect(result.profile?.findings?.generatedMembers).toHaveLength(1);
  });
});

describe('parseProfile: each section can be malformed on its own', () => {
  it('refuses a links section that is not an object', () => {
    const result = parseProfile('{ "version": 1, "links": 3 }');
    expect(result.profile?.links).toBeUndefined();
    expect(result.problems[0].path).toBe('links');
  });

  it('refuses a findings section that is not an object', () => {
    const result = parseProfile('{ "version": 1, "findings": [] }');
    expect(result.profile?.findings).toBeUndefined();
    expect(result.problems[0].path).toBe('findings');
  });

  it('refuses a files section that is not an object', () => {
    const result = parseProfile('{ "version": 1, "files": "everything" }');
    expect(result.profile?.files).toBeUndefined();
    expect(result.problems[0].path).toBe('files');
  });

  it('refuses layer rules that are not a list', () => {
    const result = parseProfile('{ "version": 1, "layers": { "rules": {} } }');
    expect(result.profile?.layers?.rules).toBeUndefined();
    expect(result.problems[0].path).toBe('layers.rules');
  });

  it('reports a default layer the format does not name', () => {
    const result = parseProfile('{ "version": 1, "layers": { "default": "external" } }');
    expect(result.profile?.layers?.default).toBeUndefined();
    expect(result.problems[0]).toMatchObject({ path: 'layers.default', severity: 'warning' });
  });

  it('reads every key of a full profile', () => {
    const source = `{
      "version": 1,
      "repository": "sample",
      "layers": {
        "rules": [{ "layer": "service", "kinds": ["class"], "nameSuffixes": ["Handler"],
                    "namePrefixes": ["Svc_"], "namespaceSegments": ["Handlers"],
                    "unlessNameSuffixes": ["Row"] }],
        "default": "service"
      },
      "links": {
        "genericPairCalls": ["Wire"],
        "typeofPairCalls": ["Map"],
        "dependencyPropertyCall": "Bind",
        "addCallPattern": "^Use",
        "serviceLocator": ["Resolve"],
        "dataContextCasts": ["as"]
      },
      "findings": {
        "entryPoints": ["Boot"],
        "transparentBases": ["Base"],
        "generatedMembers": [{ "attribute": "RelayCommand", "on": "method", "form": "commandSuffix" }]
      },
      "files": { "include": ["src/**/*.cs"], "exclude": ["**/obj/**"] }
    }`;
    const result = parseProfile(source);
    expect(result.problems).toEqual([]);
    expect(result.profile).toEqual(JSON.parse(source));
  });
});

describe('formatProfile', () => {
  it('writes two-space JSON with a trailing newline and nothing else', () => {
    const text = formatProfile({ version: 1, repository: 'sample' });
    expect(text).toBe('{\n  "version": 1,\n  "repository": "sample"\n}\n');
  });

  it('writes the same bytes for the same profile, whatever order the keys were built in', () => {
    const first = read({
      version: 1,
      files: { exclude: ['**/obj/**'], include: ['**/*.cs'] },
      findings: { entryPoints: ['App'] },
      layers: { default: 'model', rules: [{ layer: 'service', nameSuffixes: ['Handler'] }] },
    });
    const second = read(first.profile as RepositoryProfile);
    expect(second.profile).toEqual(first.profile);
    expect(formatProfile(second.profile as RepositoryProfile))
      .toBe(formatProfile(first.profile as RepositoryProfile));
  });

  it('omits a section it has nothing to say about', () => {
    const text = formatProfile({ version: 1, layers: { rules: [] }, links: {}, findings: {}, files: {} });
    expect(text).toBe('{\n  "version": 1\n}\n');
  });

  it('writes a layer rule’s conditions in reading order', () => {
    const text = formatProfile({
      version: 1,
      layers: {
        rules: [{
          layer: 'viewmodel',
          unlessNameSuffixes: ['Row'],
          namespaceSegments: ['Screens'],
          namePrefixes: ['VM_'],
          nameSuffixes: ['Screen'],
          kinds: ['class'],
        }],
      },
    });
    expect(text.indexOf('"layer"')).toBeLessThan(text.indexOf('"kinds"'));
    expect(text.indexOf('"kinds"')).toBeLessThan(text.indexOf('"nameSuffixes"'));
    expect(text.indexOf('"nameSuffixes"')).toBeLessThan(text.indexOf('"namePrefixes"'));
    expect(text.indexOf('"namePrefixes"')).toBeLessThan(text.indexOf('"namespaceSegments"'));
    expect(text.indexOf('"namespaceSegments"')).toBeLessThan(text.indexOf('"unlessNameSuffixes"'));
  });

  it('writes a profile that reads back to the profile it was given', () => {
    const profile: RepositoryProfile = {
      version: 1,
      repository: 'sample',
      layers: { rules: [{ layer: 'service', namespaceSegments: ['Handlers'] }], default: 'model' },
      links: {
        genericPairCalls: ['Wire'],
        dependencyPropertyCall: 'Bind',
        addCallPattern: '^Use',
        dataContextCasts: ['as'],
      },
      findings: {
        entryPoints: ['Boot'],
        generatedMembers: [{ attribute: 'RelayCommand', on: 'method', form: 'commandSuffix' }],
      },
      files: { exclude: ['**/obj/**'] },
    };
    const result = parseProfile(formatProfile(profile));
    expect(result.problems).toEqual([]);
    expect(result.profile).toEqual(profile);
  });
});

describe('mergeProfile', () => {
  it('keeps every declaration the existing profile holds', () => {
    const existing: RepositoryProfile = {
      version: 1,
      repository: 'sample',
      layers: { rules: [{ layer: 'service', nameSuffixes: ['Handler'] }], default: 'model' },
      findings: { entryPoints: ['App'], transparentBases: ['Page'] },
    };
    const merged = mergeProfile(existing, { version: 1 });
    expect(merged.layers).toEqual(existing.layers);
    expect(merged.findings).toEqual(existing.findings);
    expect(merged.repository).toBe('sample');
  });

  it('adds a new declaration the reply brings, once', () => {
    const existing: RepositoryProfile = { version: 1, findings: { entryPoints: ['App'] } };
    const incoming: RepositoryProfile = { version: 1, findings: { entryPoints: ['App', 'Boot'] } };
    const merged = mergeProfile(existing, incoming);
    expect(merged.findings?.entryPoints).toEqual(['App', 'Boot']);

    // The same reply imported twice adds nothing the second time.
    const again = mergeProfile(merged, incoming);
    expect(again.findings?.entryPoints).toEqual(['App', 'Boot']);
  });

  it('adds a new layer rule without disturbing the ones already declared', () => {
    const existing: RepositoryProfile = {
      version: 1,
      layers: { rules: [{ layer: 'service', nameSuffixes: ['Handler'] }] },
    };
    const incoming: RepositoryProfile = {
      version: 1,
      layers: { rules: [{ layer: 'service', nameSuffixes: ['Handler'] }, { layer: 'viewmodel', nameSuffixes: ['Screen'] }] },
    };
    expect(mergeProfile(existing, incoming).layers?.rules).toEqual([
      { layer: 'service', nameSuffixes: ['Handler'] },
      { layer: 'viewmodel', nameSuffixes: ['Screen'] },
    ]);
  });

  it('lets the existing value win where both state one', () => {
    const existing: RepositoryProfile = {
      version: 1,
      links: { dependencyPropertyCall: 'Bind' },
      layers: { default: 'model' },
    };
    const incoming: RepositoryProfile = {
      version: 1,
      links: { dependencyPropertyCall: 'Register' },
      layers: { default: 'service' },
    };
    const merged = mergeProfile(existing, incoming);
    expect(merged.links?.dependencyPropertyCall).toBe('Bind');
    expect(merged.layers?.default).toBe('model');
  });

  it('fills in a value only the reply states', () => {
    const merged = mergeProfile({ version: 1 }, {
      version: 1,
      repository: 'sample',
      links: { dependencyPropertyCall: 'Bind', addCallPattern: '^Use' },
      layers: { default: 'service' },
      files: { include: ['src/**/*.cs'], exclude: ['**/obj/**'] },
    });
    expect(merged.repository).toBe('sample');
    expect(merged.links?.dependencyPropertyCall).toBe('Bind');
    expect(merged.links?.addCallPattern).toBe('^Use');
    expect(merged.layers?.default).toBe('service');
    expect(merged.files).toEqual({ include: ['src/**/*.cs'], exclude: ['**/obj/**'] });
  });

  it('unions every link list', () => {
    const merged = mergeProfile({
      version: 1,
      links: {
        genericPairCalls: ['Configure'],
        typeofPairCalls: ['RegisterRoute'],
        serviceLocator: ['GetService'],
        dataContextCasts: ['as'],
      },
    }, {
      version: 1,
      links: {
        genericPairCalls: ['Wire'],
        typeofPairCalls: ['Map'],
        serviceLocator: ['Resolve'],
        dataContextCasts: ['is'],
      },
    });
    expect(merged.links).toEqual({
      genericPairCalls: ['Configure', 'Wire'],
      typeofPairCalls: ['RegisterRoute', 'Map'],
      serviceLocator: ['GetService', 'Resolve'],
      dataContextCasts: ['as', 'is'],
    });
  });

  it('unions generated-member rules without duplicating one', () => {
    const merged = mergeProfile({
      version: 1,
      findings: { generatedMembers: [{ attribute: 'RelayCommand', on: 'method', form: 'commandSuffix' }] },
    }, {
      version: 1,
      findings: {
        generatedMembers: [
          { attribute: 'RelayCommand', on: 'method', form: 'commandSuffix' },
          { attribute: 'ObservableProperty', on: 'field', form: 'pascalCaseWithoutPrefix' },
        ],
      },
    });
    expect(merged.findings?.generatedMembers).toHaveLength(2);
  });

  it('produces a usable profile from two empty ones', () => {
    expect(mergeProfile({ version: 1 }, { version: 1 })).toEqual({ version: 1 });
  });
});
