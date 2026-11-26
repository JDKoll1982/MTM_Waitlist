import { describe, expect, it } from 'vitest';
import { existsSync, readFileSync } from 'node:fs';
import { join } from 'node:path';
import {
  PROFILE_CONTRACT,
  contractKeys,
  renderContract,
  type ProfileField,
  type SectionName,
} from '../../src/graph/profileContract';
import { parseProfile } from '../../src/graph/profile';
import { buildSetupPrompt } from '../../src/graph/profilePrompt';
import type { ProfileEvidence } from '../../src/graph/model';

/** Every row, at every depth, so one assertion covers the whole table. */
function rowsOf(fields: ProfileField[] = PROFILE_CONTRACT): ProfileField[] {
  return fields.flatMap((field) => [field, ...rowsOf(field.fields ?? [])]);
}

const ROWS = rowsOf();

/** A valid value for a row, so a minimal profile can declare just that key. */
function sampleFor(row: ProfileField): unknown {
  if (row.values) {
    return row.shape.startsWith('a list of') ? [row.values[0]] : row.values[0];
  }
  switch (row.key) {
    case 'version':
      return 1;
    case 'layers':
      return { default: 'model' };
    case 'links':
      return { genericPairCalls: ['Wire'] };
    case 'findings':
      return { entryPoints: ['App'] };
    case 'files':
      return { include: ['**/*.cs'] };
    case 'rules':
      return [{ layer: 'model' }];
    case 'generatedMembers':
      return [{ attribute: 'RelayCommand', on: 'method', form: 'commandSuffix' }];
    case 'dependencyPropertyCall':
    case 'addCallPattern':
    case 'attribute':
    case 'repository':
      return 'Wire';
    case 'include':
    case 'exclude':
      return ['**/*.cs'];
    default:
      return ['sample'];
  }
}

/**
 * A minimal profile declaring one row's key and nothing else at its depth.
 *
 * Each part of the file is reached through the objects that hold it, because a key inside a rule
 * cannot be declared without the rule.
 */
function minimalProfile(row: ProfileField): Record<string, unknown> {
  const value = sampleFor(row);
  switch (row.section) {
    case 'root':
      return { version: 1, [row.key]: value };
    case 'layers':
      return { version: 1, layers: { [row.key]: value } };
    case 'layers.rules[]':
      return { version: 1, layers: { rules: [{ layer: 'model', [row.key]: value }] } };
    case 'links':
      return { version: 1, links: { [row.key]: value } };
    case 'findings':
      return { version: 1, findings: { [row.key]: value } };
    case 'findings.generatedMembers[]':
      return {
        version: 1,
        findings: {
          generatedMembers: [{ attribute: 'RelayCommand', on: 'method', form: 'commandSuffix', [row.key]: value }],
        },
      };
    case 'files':
      return { version: 1, files: { [row.key]: value } };
    default:
      throw new Error(`no builder for ${row.section satisfies string}`);
  }
}

/** The path a problem would carry for a row, with the first list index made concrete. */
function instantiated(path: string): string {
  return path.replace('[]', '[0]');
}

const SECTIONS: SectionName[] = [
  'root', 'layers', 'layers.rules[]', 'links', 'findings', 'findings.generatedMembers[]', 'files',
];

describe('the profile contract as the parser’s accepted set', () => {
  it('declares at least one key in every section', () => {
    for (const section of SECTIONS) {
      expect(contractKeys(section).length, section).toBeGreaterThan(0);
    }
  });

  it('parses a minimal profile declaring each row’s key with no problem at that path', () => {
    for (const row of ROWS) {
      const { problems } = parseProfile(JSON.stringify(minimalProfile(row)));
      const at = problems.filter((problem) => problem.path === instantiated(row.path));
      expect(at, `${row.path} was refused: ${JSON.stringify(at)}`).toEqual([]);
    }
  });

  it('marks version alone as required, and describes every other key as optional', () => {
    expect(ROWS.filter((row) => row.required).map((row) => row.path)).toEqual(['version']);
  });

  it('gives each choice-shaped row its closed value set, and no other row one', () => {
    const withValues = ROWS.filter((row) => row.values).map((row) => row.path);
    expect(withValues).toEqual([
      'layers.rules[].layer',
      'layers.rules[].kinds',
      'layers.default',
      'findings.generatedMembers[].on',
      'findings.generatedMembers[].form',
    ]);
    expect(ROWS.find((row) => row.path === 'layers.default')?.values).toEqual([
      'view', 'viewmodel', 'service', 'model',
    ]);
    expect(ROWS.find((row) => row.path === 'layers.rules[].kinds')?.values).toEqual([
      'class', 'interface', 'enum', 'record', 'struct',
    ]);
  });

  it('names each key exactly once, and states its meaning and absent-key behaviour', () => {
    const paths = ROWS.map((row) => row.path);
    expect(new Set(paths).size).toBe(paths.length);
    for (const row of ROWS) {
      expect(row.meaning.length, row.path).toBeGreaterThan(20);
      expect(row.absent.length, row.path).toBeGreaterThan(10);
      expect(row.shape.length, row.path).toBeGreaterThan(3);
    }
  });
});

describe('the profile contract as the prompt’s described set', () => {
  const evidence: ProfileEvidence = {
    nameSuffixes: [{ name: 'Handler', examples: ['PickHandler'] }],
    namePrefixes: [],
    namespaceSegments: [{ segment: 'Services', types: 3 }],
    calls: [{ name: 'Configure', typeArguments: 2, example: 'App/App.cs:1' }],
    baseTypes: ['Page'],
    methodAttributes: [],
    fieldAttributes: [],
    xamlRootTags: [{ tag: 'Page', files: 1 }],
    csharpFiles: 10,
    xamlFiles: 1,
  };

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

  function replyExample(prompt: string): unknown {
    const fenced = /```json\s*\r?\n([\s\S]*?)```/.exec(prompt);
    expect(fenced).not.toBeNull();
    return JSON.parse(fenced![1]);
  }

  it('uses only keys the table describes in the reply example', () => {
    const prompt = buildSetupPrompt(evidence, 'Dunnage');
    const used = pathsOf(replyExample(prompt));
    expect(used.length).toBeGreaterThan(5);
    const described = new Set(ROWS.map((row) => row.path));
    for (const path of used) {
      expect(described.has(path), `${path} is in the reply example but not in the table`).toBe(true);
    }
  });

  it('describes every row in the rendered contract', () => {
    const rendered = renderContract();
    for (const row of ROWS) {
      expect(rendered, row.path).toContain(`- ${row.path} (`);
      expect(rendered, row.path).toContain(row.meaning);
      expect(rendered, row.path).toContain(`If absent: ${row.absent}`);
    }
  });
});

describe('the profile contract against the published format', () => {
  const schemaPath = join(
    process.cwd(), '..', '..', 'specs', '023-extension-repo-profile', 'contracts', 'profile.schema.json',
  );

  /** The property names a JSON-schema document declares, at every depth, following `items` and `$defs`. */
  function declaredNames(node: unknown, into = new Set<string>()): Set<string> {
    if (typeof node !== 'object' || node === null) {
      return into;
    }
    const schema = node as Record<string, unknown>;
    for (const [name, child] of Object.entries((schema.properties ?? {}) as Record<string, unknown>)) {
      into.add(name);
      declaredNames(child, into);
    }
    if (schema.items) {
      declaredNames(schema.items, into);
    }
    for (const definition of Object.values((schema.$defs ?? {}) as Record<string, unknown>)) {
      declaredNames(definition, into);
    }
    return into;
  }

  it('matches the schema property for property, in both directions', () => {
    if (!existsSync(schemaPath)) {
      // The extension's own suite has to stay green without the specification beside it.
      return;
    }
    const declared = declaredNames(JSON.parse(readFileSync(schemaPath, 'utf8')));
    const described = new Set(ROWS.map((row) => row.key));

    expect([...described].filter((key) => !declared.has(key))).toEqual([]);
    expect([...declared].filter((key) => !described.has(key))).toEqual([]);
  });
});
