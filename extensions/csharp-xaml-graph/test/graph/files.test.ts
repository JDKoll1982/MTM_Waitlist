import { describe, expect, it } from 'vitest';
import { readdirSync } from 'node:fs';
import { join, resolve } from 'node:path';
import {
  BUILT_IN_SELECTION,
  globToRegExp,
  matchesPath,
  patternProblem,
  prunesDirectory,
  selectionProblems,
  selectsFile,
} from '../../src/graph/files';

const repositoryRoot = resolve(process.cwd(), '..', '..');

/** Walks a tree the way the validator does, pruning a directory the selection can never read. */
function walk(root: string): { csharp: string[]; xaml: string[] } {
  const csharp: string[] = [];
  const xaml: string[] = [];
  const pending = [''];
  while (pending.length > 0) {
    const relative = pending.pop()!;
    for (const entry of readdirSync(relative ? join(root, relative) : root, { withFileTypes: true })) {
      const path = relative ? `${relative}/${entry.name}` : entry.name;
      if (entry.isDirectory()) {
        if (!prunesDirectory(BUILT_IN_SELECTION, path)) {
          pending.push(path);
        }
        continue;
      }
      if (!selectsFile(BUILT_IN_SELECTION, path)) {
        continue;
      }
      if (path.endsWith('.cs')) {
        csharp.push(path);
      } else if (path.endsWith('.xaml')) {
        xaml.push(path);
      }
    }
  }
  return { csharp, xaml };
}

describe('the built-in selection', () => {
  it('reads exactly the files the baseline records for this repository', () => {
    const { csharp, xaml } = walk(repositoryRoot);

    // baseline.md: 815 C# and 93 XAML files, the counts every gate is measured against.
    expect(csharp).toHaveLength(815);
    expect(xaml).toHaveLength(93);
  });

  it('prunes every generated, vendored, workflow and test directory', () => {
    for (const directory of [
      'bin', 'obj', 'node_modules', 'out', 'TestResults', '.vs', '.github', '.specify',
      'extensions/csharp-xaml-graph/bin', 'Module_Waitlist/obj', 'MTM_Waitlist.Tests',
    ]) {
      expect(prunesDirectory(BUILT_IN_SELECTION, directory), directory).toBe(true);
    }
  });

  it('keeps every source directory, and a file inside one is still offered to the walk', () => {
    for (const directory of ['Module_Waitlist', 'extensions/csharp-xaml-graph/src', 'MTM_Database_Deployment.Core']) {
      expect(prunesDirectory(BUILT_IN_SELECTION, directory), directory).toBe(false);
    }
    expect(selectsFile(BUILT_IN_SELECTION, 'Module_Waitlist/Views/HomePage.xaml')).toBe(true);
    expect(selectsFile(BUILT_IN_SELECTION, 'Module_Waitlist/obj/Debug/Generated.cs')).toBe(false);
  });
});

describe('the pattern grammar', () => {
  it('matches a file at the root through the optional leading directory', () => {
    expect(matchesPath('**/*.cs', 'Program.cs')).toBe(true);
    expect(matchesPath('**/*.cs', 'A/B/Program.cs')).toBe(true);
    expect(matchesPath('**/*.cs', 'Program.xaml')).toBe(false);
  });

  it('treats a single star as one path segment only', () => {
    expect(matchesPath('src/*.cs', 'src/Program.cs')).toBe(true);
    expect(matchesPath('src/*.cs', 'src/deep/Program.cs')).toBe(false);
    expect(matchesPath('*/*.cs', 'src/Program.cs')).toBe(true);
  });

  it('matches exactly one character for each question mark', () => {
    expect(matchesPath('View?.cs', 'ViewA.cs')).toBe(true);
    expect(matchesPath('View?.cs', 'View.cs')).toBe(false);
    expect(matchesPath('View?.cs', 'ViewAB.cs')).toBe(false);
    expect(matchesPath('View?.cs', 'View/.cs')).toBe(false);
  });

  it('escapes everything that is not one of its three tokens', () => {
    expect(matchesPath('a.b.cs', 'a.b.cs')).toBe(true);
    expect(matchesPath('a.b.cs', 'axb.cs')).toBe(false);
    expect(matchesPath('**/TestResults/**', 'x/TestResults/y.cs')).toBe(true);
    expect(matchesPath('**/TestResults/**', 'x/Results/y.cs')).toBe(false);
    expect(globToRegExp('*.cs').test('a.cs')).toBe(true);
  });

  it('reports a pattern outside the grammar rather than matching it wrongly', () => {
    for (const pattern of ['**/[abc]/**', 'src/{a,b}/**', '', 'a!(b).cs', 'c:\\src\\**']) {
      expect(patternProblem(pattern), pattern).toBeDefined();
    }
    for (const pattern of ['**/*.cs', 'src/**/*.xaml', 'View?.cs', 'a.b.cs']) {
      expect(patternProblem(pattern), pattern).toBeUndefined();
    }
    // A pattern outside the grammar matches nothing at all.
    expect(matchesPath('**/[abc]/**', 'a/b.cs')).toBe(false);
  });

  it('reports every offending pattern with its path and index', () => {
    const problems = selectionProblems({ include: ['**/*.cs', '['], exclude: ['**/obj/**', '**/{a}/**'] });
    expect(problems.map((problem) => problem.path)).toEqual(['files.include[1]', 'files.exclude[1]']);
    expect(problems[0].message).toContain('It was not used.');
  });
});
