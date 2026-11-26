/**
 * Which files a run reads, decided in one place.
 *
 * The host asks VS Code for the files, and the headless validator walks the folder itself; both have
 * to agree, so the built-in selection lives here and the matcher below is what decides whether a path
 * is read. The grammar is deliberately three tokens — `**`, `*` and `?` — because a pattern that
 * means something subtly different from what a person expected is worse than one reported as
 * unsupported.
 *
 * It is pure: nothing here imports `vscode` or `node:fs`, so the walk, the host and the test suite all
 * reach the same answer from the same function.
 */

/** The patterns one run reads by. A present list replaces the built-in list rather than extending it. */
export interface FileSelection {
  include: string[];
  exclude: string[];
}

/**
 * The selection used when a repository declares nothing.
 *
 * `bin`, `obj`, `node_modules`, `out` and `TestResults` hold build output; `.vs`, `.github` and
 * `.specify` hold editor state, workflow files and agent configuration, all of which carry C# samples
 * that are not application code; a `*.Tests` folder is a test project.
 */
export const BUILT_IN_SELECTION: FileSelection = {
  include: ['**/*.cs', '**/*.xaml'],
  exclude: [
    '**/bin/**',
    '**/obj/**',
    '**/node_modules/**',
    '**/out/**',
    '**/TestResults/**',
    '**/*.Tests/**',
    '**/.vs/**',
    '**/.github/**',
    '**/.specify/**',
  ],
};

/** Characters that mean something in another glob dialect, so a pattern using one is not guessable. */
const OUTSIDE_GRAMMAR = /[[\]{}()!+@|^$\\]/;

function escapeRegExp(text: string): string {
  return text.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
}

/**
 * The reason one pattern is not in the grammar, or undefined when it is.
 *
 * `**`, `*` and `?` are the whole of it. A pattern carrying a character from another dialect is
 * reported rather than silently matched, because matching it wrongly hides files without saying so.
 */
export function patternProblem(pattern: string): string | undefined {
  const trimmed = pattern.trim();
  if (trimmed.length === 0) {
    return 'a pattern cannot be empty';
  }
  const foreign = [...trimmed].find((character) => OUTSIDE_GRAMMAR.test(character));
  if (foreign) {
    return `\`${foreign}\` is not part of the pattern grammar, which knows only **, * and ?`;
  }
  return undefined;
}

/** A pattern as a regular expression over a workspace-relative path with forward slashes. */
export function globToRegExp(pattern: string): RegExp {
  let source = '';
  let index = 0;
  while (index < pattern.length) {
    const character = pattern[index];
    if (character === '*' && pattern[index + 1] === '*') {
      // `**/` also matches no directory at all, which is what makes `**/*.cs` find a file at the root.
      if (pattern[index + 2] === '/') {
        source += '(?:.*/)?';
        index += 3;
        continue;
      }
      source += '.*';
      index += 2;
      continue;
    }
    if (character === '*') {
      source += '[^/]*';
      index += 1;
      continue;
    }
    if (character === '?') {
      source += '[^/]';
      index += 1;
      continue;
    }
    source += escapeRegExp(character);
    index += 1;
  }
  return new RegExp(`^${source}$`);
}

/** True when one pattern matches one workspace-relative path. A pattern outside the grammar matches nothing. */
export function matchesPath(pattern: string, relativePath: string): boolean {
  if (patternProblem(pattern)) {
    return false;
  }
  return globToRegExp(pattern.trim()).test(relativePath);
}

/** True when the file is read: it matches an include pattern and no exclude pattern. */
export function selectsFile(selection: FileSelection, relativePath: string): boolean {
  return selection.include.some((pattern) => matchesPath(pattern, relativePath))
    && !selection.exclude.some((pattern) => matchesPath(pattern, relativePath));
}

/**
 * True when no file below this directory can be read, so the walk can skip it entirely.
 *
 * Pruning is what makes an exclude pattern cheap: an obj-folder pattern is tested once for the folder
 * rather than once per generated file inside it.
 */
export function prunesDirectory(
  selection: FileSelection,
  relativeDirectory: string,
): boolean {
  return selection.exclude.some((pattern) => matchesPath(pattern, relativeDirectory)
    || matchesPath(pattern, `${relativeDirectory}/pruned`));
}

/** Every include and exclude pattern that is not in the grammar, so the reader is told rather than left guessing. */
export function selectionProblems(selection: FileSelection): { path: string; message: string }[] {
  const problems: { path: string; message: string }[] = [];
  for (const key of ['include', 'exclude'] as const) {
    selection[key].forEach((pattern, index) => {
      const problem = patternProblem(pattern);
      if (problem) {
        problems.push({ path: `files.${key}[${index}]`, message: `\`${pattern}\`: ${problem} It was not used.` });
      }
    });
  }
  return problems;
}
