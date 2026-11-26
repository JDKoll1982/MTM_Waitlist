import { describe, expect, it } from 'vitest';
import {
  PROFILE_FILE_NAME,
  formatProfile,
  parseProfile,
  profileProblemNote,
  profileWarningText,
  type RepositoryProfile,
} from '../../src/graph/profile';
import { compileConventions } from '../../src/graph/conventions';

/**
 * Every way a profile can be missing, partial or broken, proved through the same parse and compile
 * surface the extension uses. The rule each case checks is the same one: the graph opens, what could
 * be read applies, and what could not is named.
 */

const read = (text: string) => parseProfile(text);

describe('a profile that is not there', () => {
  it('reads a blank file as absent with a message, not as a parse failure', () => {
    const result = read('   \n\n');
    expect(result.profile).toBeUndefined();
    expect(result.problems).toHaveLength(1);
    expect(result.problems[0].message).toContain('empty');
    expect(result.problems[0].message).not.toContain('JSON');
    // Nothing is thrown, so the caller can carry the message and analyse anyway.
    expect(compileConventions(result.profile).layerRules.length).toBeGreaterThan(0);
  });

  it('reads a missing file as nothing at all, with nothing to report', () => {
    // The host does not call the parser for a file it could not read; this is that state.
    const conventions = compileConventions(undefined);
    expect(conventions.entryPoints.has('App')).toBe(true);
  });
});

describe('a partial profile', () => {
  it('applies the answered parts and falls back for the rest', () => {
    const result = read('{ "version": 1, "layers": { "rules": [{ "layer": "service", "nameSuffixes": ["Handler"] }] } }');
    expect(result.problems).toEqual([]);
    const conventions = compileConventions(result.profile);
    expect(conventions.layerRules).toEqual([{ layer: 'service', nameSuffixes: ['Handler'] }]);
    // The sections the profile does not mention keep the built-in conventions.
    expect(conventions.genericPairCalls.has('Configure')).toBe(true);
    expect(conventions.entryPoints.has('Program')).toBe(true);
    expect(conventions.include).toEqual(['**/*.cs', '**/*.xaml']);
  });

  it('applies one section while another is malformed, naming the section and the reason', () => {
    const result = read(`{
      "version": 1,
      "layers": { "rules": "everything" },
      "findings": { "entryPoints": ["Boot"] }
    }`);
    expect(result.profile?.findings?.entryPoints).toEqual(['Boot']);
    expect(result.problems).toHaveLength(1);
    expect(result.problems[0].path).toBe('layers.rules');
    expect(result.problems[0].message).toContain('Expected a list of layer rules');
    expect(result.problems[0].message).toContain('built-in layer rules applied instead');
  });

  it('reports something it does not recognise and carries on', () => {
    const result = read('{ "version": 1, "futureSection": { "a": 1 }, "findings": { "entryPoints": ["Boot"] } }');
    expect(result.profile?.findings?.entryPoints).toEqual(['Boot']);
    expect(result.problems).toHaveLength(1);
    expect(result.problems[0].path).toBe('futureSection');
    expect(result.problems[0].severity).toBe('warning');
  });
});

describe('a broken profile', () => {
  it('keeps the graph open when the file is not JSON', () => {
    const result = read('{ "version": 1, ');
    expect(result.profile).toBeUndefined();
    expect(result.problems[0].severity).toBe('error');
    expect(compileConventions(result.profile).layerRules.length).toBeGreaterThan(0);
  });

  it('keeps the graph open when the file states no version', () => {
    const result = read('{ "layers": { "default": "service" } }');
    expect(result.profile).toBeUndefined();
    expect(result.problems[0].path).toBe('version');
  });

  it('names what was dropped rather than silently changing the answer', () => {
    const result = read(`{
      "version": 1,
      "layers": { "rules": [{ "layer": "service", "kinds": ["class", "delegate"], "nameSuffixes": ["Handler"] }] },
      "findings": { "generatedMembers": [{ "attribute": "Tell", "on": "property", "form": "commandSuffix" }] }
    }`);
    expect(result.profile?.layers?.rules?.[0].kinds).toEqual(['class']);
    expect(result.profile?.findings?.generatedMembers).toEqual([]);
    expect(result.problems.map((problem) => problem.path)).toEqual([
      'layers.rules[0].kinds',
      'findings.generatedMembers[0].on',
    ]);
  });
});

describe('declarations that do not match the repository', () => {
  it('keeps a pattern that currently matches nothing', () => {
    const result = read(`{
      "version": 1,
      "layers": { "rules": [{ "layer": "service", "nameSuffixes": ["Coordinator"] }] },
      "files": { "include": ["nowhere/**/*.cs"] }
    }`);
    expect(result.problems).toEqual([]);
    expect(compileConventions(result.profile).layerRules).toEqual([
      { layer: 'service', nameSuffixes: ['Coordinator'] },
    ]);
    // The selection is carried, so the host can read it and report that it matched nothing.
    expect(compileConventions(result.profile).include).toEqual(['nowhere/**/*.cs']);
  });

  it('applies the same declaration twice once, with no error', () => {
    const result = read(`{
      "version": 1,
      "layers": { "rules": [
        { "layer": "service", "nameSuffixes": ["Handler"] },
        { "layer": "service", "nameSuffixes": ["Handler"] }
      ] },
      "findings": { "entryPoints": ["Boot", "Boot"] }
    }`);
    expect(result.problems).toEqual([]);
    // Both declarations are kept as written (the merge is what folds them), and the repeated name is
    // applied once because a declaration is a set membership, not a count.
    expect(result.profile?.findings?.entryPoints).toEqual(['Boot']);
  });

  it('folds a repeated declaration once when a reply is imported twice', () => {
    const first: RepositoryProfile = { version: 1, findings: { entryPoints: ['Boot'] } };
    const reply: RepositoryProfile = { version: 1, findings: { entryPoints: ['Boot', 'Launch'] } };
    // Importing twice produces the same file as importing once.
    const once = read(formatProfile({ ...first, findings: { entryPoints: ['Boot', 'Launch'] } }));
    expect(once.problems).toEqual([]);
    expect(once.profile?.findings?.entryPoints).toEqual(['Boot', 'Launch']);
  });
});

describe('the warning the model carries', () => {
  it('keeps the dotted path and the sentence apart', () => {
    const text = profileWarningText({ path: 'layers.rules[2].kinds', message: 'Two was not.', severity: 'warning' });
    expect(text).toBe(`${PROFILE_FILE_NAME} › layers.rules[2].kinds: Two was not.`);
    expect(profileProblemNote(text)).toEqual({ path: 'layers.rules[2].kinds', message: 'Two was not.' });
  });

  it('reads a problem about the whole file', () => {
    const text = profileWarningText({ path: PROFILE_FILE_NAME, message: 'Empty.', severity: 'warning' });
    expect(text).toBe(`${PROFILE_FILE_NAME}: Empty.`);
    expect(profileProblemNote(text)).toEqual({ path: '', message: 'Empty.' });
  });

  it('ignores a warning that is about something else', () => {
    expect(profileProblemNote('A/Broken.cs: unexpected end of input')).toBeUndefined();
  });
});
