import { afterEach, describe, expect, it } from 'vitest';
import { existsSync, mkdtempSync, readFileSync, readdirSync, rmSync, writeFileSync, mkdirSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import {
  ANALYSIS_FILE_CAP,
  assessCompatibility,
  buildUpdateRequest,
  changesFor,
  type StructureFacts,
} from '../../src/graph/compatibility';
import { writeUpdateRequest } from '../../src/structure';

const facts = (partial: Partial<StructureFacts> = {}): StructureFacts => ({
  csharpFiles: 12,
  xamlFiles: 3,
  projectFiles: ['src/App.csproj'],
  solutionFiles: ['App.sln'],
  truncated: false,
  ...partial,
});

const incompatible = (repository: string, partial: Partial<StructureFacts> = {}) =>
  assessCompatibility(
    facts({ csharpFiles: 0, xamlFiles: 0, projectFiles: ['src/Other.fsproj'], ...partial }),
    ANALYSIS_FILE_CAP,
    repository,
  );

const scratch: string[] = [];

function makeRoot(prefix: string): string {
  const root = mkdtempSync(join(tmpdir(), prefix));
  scratch.push(root);
  return root;
}

afterEach(() => {
  for (const root of scratch.splice(0)) {
    rmSync(root, { recursive: true, force: true });
  }
});

describe('the request text', () => {
  it('opens with a heading naming the repository', () => {
    const text = buildUpdateRequest(incompatible('other-repo'));
    expect(text.split('\n')[0]).toBe('# Extension change request: other-repo');
  });

  it('states the command verbatim on the second line that carries text', () => {
    const lines = buildUpdateRequest(incompatible('other-repo')).split('\n');
    expect(lines.slice(0, 4)).toEqual([
      '# Extension change request: other-repo',
      '',
      'Run this with `/speckit-companion-specify`.',
      '',
    ]);
    expect(buildUpdateRequest(incompatible('other-repo'))).toContain('/speckit-companion-specify');
  });

  it('names every reason as a section, in the verdict’s order', () => {
    const verdict = incompatible('other-repo');
    const text = buildUpdateRequest(verdict);
    const headings = [...text.matchAll(/^## (.+)$/gm)].map((match) => match[1]);

    expect(headings).toEqual([
      ...verdict.reasons.map((reason) => reason.message),
      'What was not changed',
    ]);
  });

  it('gives every change its own subsection carrying at least one evidence line', () => {
    const verdict = incompatible('other-repo');
    const text = buildUpdateRequest(verdict);

    for (const reason of verdict.reasons) {
      for (const change of changesFor(reason)) {
        const at = text.indexOf(`### ${change.title}`);
        expect(at, change.title).toBeGreaterThan(0);
        const section = text.slice(at, text.indexOf('\n## ', at) < 0 ? undefined : text.indexOf('\n## ', at));
        expect(section).toContain(change.change);
        expect(section).toContain('Evidence:');
        expect(section.trimEnd().split('\n').at(-1)).toMatch(/^- /);
      }
    }
  });

  it('carries the repository’s own counts and relative paths, never a machine path or a clock', () => {
    const text = buildUpdateRequest(incompatible('other-repo'));

    expect(text).toContain('src/Other.fsproj');
    expect(text).toContain('0 C# files');
    expect(text).not.toMatch(/[A-Za-z]:\\/);
    expect(text).not.toMatch(/\d{4}-\d{2}-\d{2}/);
    expect(text).not.toContain(process.cwd());
    expect(text).not.toMatch(/\b(?:19|20)\d{2}-\d{2}-\d{2}T/);
  });

  it('closes by saying nothing in the requesting repository was created or changed', () => {
    const text = buildUpdateRequest(incompatible('other-repo'));
    expect(text).toContain('## What was not changed');
    expect(text.trimEnd().split('\n').at(-1))
      .toBe('Nothing in other-repo was created or changed. This file is a request against the extension.');
  });
});

describe('writeUpdateRequest', () => {
  it('creates update_prompts/<slug>/request.md under the root it is given', () => {
    const root = makeRoot('update-prompts-');
    const result = writeUpdateRequest(root, 'other-repo', '# request\n');

    expect(result).toEqual({ ok: true, path: 'update_prompts/other-repo/request.md' });
    const file = join(root, 'update_prompts', 'other-repo', 'request.md');
    expect(existsSync(file)).toBe(true);
    expect(readFileSync(file, 'utf8')).toBe('# request\n');
  });

  it('replaces an existing request rather than stacking a second one', () => {
    const root = makeRoot('update-prompts-');
    writeUpdateRequest(root, 'other-repo', 'first\n');
    writeUpdateRequest(root, 'other-repo', 'second\n');

    const folder = join(root, 'update_prompts', 'other-repo');
    expect(readdirSync(folder)).toEqual(['request.md']);
    expect(readFileSync(join(folder, 'request.md'), 'utf8')).toBe('second\n');
  });

  it('keeps one folder per repository', () => {
    const root = makeRoot('update-prompts-');
    writeUpdateRequest(root, 'other-repo', 'a\n');
    writeUpdateRequest(root, 'another-repo', 'b\n');

    expect(readdirSync(join(root, 'update_prompts')).sort()).toEqual(['another-repo', 'other-repo']);
  });

  it('reports a failure instead of throwing when the folder cannot be written', () => {
    const root = makeRoot('update-prompts-');
    // A file where the folder has to go, so creating the directory must fail.
    mkdirSync(join(root, 'update_prompts'), { recursive: true });
    writeFileSync(join(root, 'update_prompts', 'other-repo'), 'not a directory');

    const result = writeUpdateRequest(root, 'other-repo', '# request\n');
    expect(result.ok).toBe(false);
    if (!result.ok) {
      expect(result.error.length).toBeGreaterThan(0);
    }
  });
});
