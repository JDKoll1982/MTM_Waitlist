import { describe, expect, it } from 'vitest';
import { buildRefreshPrompt, buildSetupPrompt, nothingToProfile } from '../../src/graph/profilePrompt';
import type { ProfileEvidence } from '../../src/graph/model';
import { formatProfile, type RepositoryProfile } from '../../src/graph/profile';
import { PROFILE_CONTRACT, type ProfileField } from '../../src/graph/profileContract';

const evidence = (partial: Partial<ProfileEvidence> = {}): ProfileEvidence => ({
  nameSuffixes: [
    { name: 'Handler', examples: ['DunnageHandler', 'PickHandler'] },
    { name: 'Screen', examples: ['HomeScreen'] },
  ],
  namePrefixes: [{ name: 'Dao_', examples: ['Dao_Part'] }],
  namespaceSegments: [
    { segment: 'Screens', types: 12 },
    { segment: 'Handlers', types: 4 },
  ],
  calls: [
    { name: 'Wire', typeArguments: 2, example: 'App/App.cs:14' },
    { name: 'Resolve', typeArguments: 1, example: 'App/View.cs:22' },
  ],
  baseTypes: ['Page', 'ObservableObject'],
  methodAttributes: ['TellCommand'],
  fieldAttributes: ['Reveal'],
  xamlRootTags: [{ tag: 'Page', files: 9 }],
  csharpFiles: 120,
  xamlFiles: 9,
  ...partial,
});

const QUESTIONS = [
  'What marks a service',
  'What marks a view model',
  'How is a type registered or resolved',
  'Which types start the application',
  'Which files should the analysis read',
];

describe('buildSetupPrompt', () => {
  it('asks every question before proposing anything', () => {
    const prompt = buildSetupPrompt(evidence(), 'Dunnage');
    const replyShape = prompt.indexOf('Reply with one JSON object only');

    expect(replyShape).toBeGreaterThan(0);
    for (const question of QUESTIONS) {
      const at = prompt.indexOf(question);
      expect(at).toBeGreaterThan(0);
      expect(at).toBeLessThan(replyShape);
    }
    // The example profile, which is the only proposal in the text, comes after every question.
    expect(prompt.indexOf('"version": 1')).toBeGreaterThan(replyShape);
  });

  it('quotes the repository’s own evidence for each question', () => {
    const prompt = buildSetupPrompt(evidence(), 'Dunnage');

    // A type name for the service and view-model questions.
    expect(prompt).toContain('Handler (DunnageHandler, PickHandler)');
    expect(prompt).toContain('Screen (HomeScreen)');
    expect(prompt).toContain('Dao_ (Dao_Part)');
    // A namespace segment with how many types carry it.
    expect(prompt).toContain('Screens (12 types)');
    // A call with its type-argument count and one example site.
    expect(prompt).toContain('Wire with 2 type arguments (App/App.cs:14)');
    expect(prompt).toContain('Resolve with 1 type argument (App/View.cs:22)');
    // Base types, attributes and root elements.
    expect(prompt).toContain('Page, ObservableObject');
    expect(prompt).toContain('TellCommand');
    expect(prompt).toContain('Reveal');
    expect(prompt).toContain('Page (9)');
  });

  it('says what it read, so the answer is written against a known scope', () => {
    const prompt = buildSetupPrompt(evidence(), 'Dunnage');
    expect(prompt).toContain('"Dunnage"');
    expect(prompt).toContain('120 C# files and 9 XAML files read');
  });

  it('says so plainly when the evidence is thin, instead of quoting nothing', () => {
    const prompt = buildSetupPrompt(evidence({
      nameSuffixes: [], namePrefixes: [], namespaceSegments: [], calls: [], baseTypes: [],
      methodAttributes: [], fieldAttributes: [], xamlRootTags: [],
    }), 'Dunnage');
    expect(prompt).toContain('none seen in this repository');
    expect(prompt).not.toContain('Queries about');
  });

  it('produces the nothing-to-profile sentence for a folder with no C# or XAML', () => {
    const empty = evidence({ csharpFiles: 0, xamlFiles: 0 });
    const prompt = buildSetupPrompt(empty, 'Empty');

    expect(prompt).toBe(nothingToProfile('Empty'));
    expect(prompt).toContain('no C# or XAML files in scope');
    expect(prompt).toContain('Nothing was written');
    expect(prompt).not.toContain('What marks a service');
  });
});

describe('buildRefreshPrompt', () => {
  const fullProfile: RepositoryProfile = {
    version: 1,
    repository: 'Dunnage',
    layers: { rules: [{ layer: 'service', nameSuffixes: ['Handler'] }], default: 'model' },
    links: { genericPairCalls: ['Wire'] },
    findings: { entryPoints: ['App'] },
    files: { include: ['**/*.cs'] },
  };

  it('carries the existing profile exactly as the file holds it', () => {
    const prompt = buildRefreshPrompt(evidence(), fullProfile, 'Dunnage');
    expect(prompt).toContain('```json');
    expect(prompt).toContain(formatProfile(fullProfile).trimEnd());
    expect(prompt).toContain('csharp-xaml-graph.profile.json');
  });

  it('asks only about the parts the profile does not cover', () => {
    const partial: RepositoryProfile = { version: 1, layers: { rules: [{ layer: 'service', nameSuffixes: ['Handler'] }] } };
    const prompt = buildRefreshPrompt(evidence(), partial, 'Dunnage');

    expect(prompt).toContain('It says nothing about:');
    expect(prompt).toContain('which calls register or resolve a type');
    expect(prompt).toContain('which files are read');
    // The layer rules are covered, so they are listed as declared rather than asked about.
    expect(prompt).toContain('layer rules (1)');
    expect(prompt).not.toContain('which name endings and namespaces place a type in a layer');
  });

  it('lists what the profile declares and still asks for what the code has gained', () => {
    const prompt = buildRefreshPrompt(evidence(), fullProfile, 'Dunnage');
    expect(prompt).toContain('- layer rules (1)');
    expect(prompt).toContain('- link detection');
    expect(prompt).toContain('- findings rules');
    expect(prompt).toContain('- file selection');
    expect(prompt).not.toContain('It says nothing about:');
    expect(prompt).toContain('Has the repository gained a convention since this profile was written?');
    // Even a complete profile is asked about new evidence, which is what a refresh is for.
    expect(prompt).toContain('Wire with 2 type arguments (App/App.cs:14)');
  });

  it('says a profile with nothing in it declares nothing yet', () => {
    const prompt = buildRefreshPrompt(evidence(), { version: 1 }, 'Dunnage');
    expect(prompt).toContain('- nothing yet');
    expect(prompt).toContain('It says nothing about:');
  });

  it('keeps every declaration, saying that additions only are wanted', () => {
    const prompt = buildRefreshPrompt(evidence(), fullProfile, 'Dunnage');
    expect(prompt).toContain('Every declaration already in the file is kept as written');
    expect(prompt).toContain('Include only');
  });
});

describe('the contract both prompts state', () => {
  const rows = (fields: ProfileField[] = PROFILE_CONTRACT): ProfileField[] =>
    fields.flatMap((field) => [field, ...rows(field.fields ?? [])]);

  const ALL = rows();

  /** Every dotted key path a JSON document uses, with `[]` where a list of objects intervenes. */
  function pathsOf(value: unknown, prefix = ''): string[] {
    if (Array.isArray(value)) {
      const first = value[0];
      if (first === undefined || typeof first !== 'object' || first === null) {
        return [];
      }
      return pathsOf(first, `${prefix}[]`);
    }
    if (typeof value !== 'object' || value === null) {
      return [];
    }
    return Object.entries(value).flatMap(([key, child]) => {
      const path = prefix ? `${prefix}.${key}` : key;
      return [path, ...pathsOf(child, path)];
    });
  }

  const partial: RepositoryProfile = {
    version: 1,
    layers: { rules: [{ layer: 'service', nameSuffixes: ['Handler'] }] },
  };

  it('states every row in the create prompt, before the first question', () => {
    const prompt = buildSetupPrompt(evidence(), 'Dunnage');
    const firstQuestion = prompt.indexOf('1. What marks a service');

    expect(firstQuestion).toBeGreaterThan(0);
    for (const row of ALL) {
      const at = prompt.indexOf(`- ${row.path} (`);
      expect(at, row.path).toBeGreaterThanOrEqual(0);
      expect(at, row.path).toBeLessThan(firstQuestion);
      expect(prompt, row.path).toContain(row.meaning);
      expect(prompt, row.path).toContain(`If absent: ${row.absent}`);
    }
  });

  it('states the requiredness and the value shape of every row', () => {
    const prompt = buildSetupPrompt(evidence(), 'Dunnage');
    for (const row of ALL) {
      expect(prompt, row.path).toContain(`(${row.shape}, ${row.required ? 'required' : 'optional'})`);
    }
  });

  it('states the whole contract in the refresh prompt, before the request to propose', () => {
    const prompt = buildRefreshPrompt(evidence(), partial, 'Dunnage');
    const proposeAt = prompt.indexOf('Answer from the evidence below before proposing anything');

    expect(proposeAt).toBeGreaterThan(0);
    for (const row of ALL) {
      const at = prompt.indexOf(`- ${row.path} (`);
      expect(at, row.path).toBeGreaterThanOrEqual(0);
      expect(at, row.path).toBeLessThan(proposeAt);
    }
  });

  it('states the whole contract even when the profile answers only some sections', () => {
    const prompt = buildRefreshPrompt(evidence(), { version: 1 }, 'Dunnage');
    for (const row of ALL) {
      expect(prompt, row.path).toContain(`- ${row.path} (`);
    }
  });

  it('uses only described keys in the reply example it hands back', () => {
    const prompt = buildSetupPrompt(evidence(), 'Dunnage');
    const fenced = /```json\s*\r?\n([\s\S]*?)```/.exec(prompt);
    expect(fenced).not.toBeNull();
    const used = pathsOf(JSON.parse(fenced![1]));
    const described = new Set(ALL.map((row) => row.path));
    expect(used.filter((path) => !described.has(path))).toEqual([]);
  });

  it('keeps the questions, their evidence and the nothing-to-profile sentence unchanged', () => {
    const prompt = buildSetupPrompt(evidence(), 'Dunnage');
    for (const question of QUESTIONS) {
      expect(prompt).toContain(question);
    }
    expect(prompt).toContain('Handler (DunnageHandler, PickHandler)');
    expect(buildSetupPrompt(evidence({ csharpFiles: 0, xamlFiles: 0 }), 'Empty')).toBe(nothingToProfile('Empty'));
  });
});