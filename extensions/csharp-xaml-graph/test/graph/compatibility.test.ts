import { describe, expect, it } from 'vitest';
import {
  ANALYSIS_FILE_CAP,
  CHANGES_FOR_REASON,
  assessCompatibility,
  buildUpdateRequest,
  changesFor,
  slugFor,
  type CompatibilityVerdict,
  type StructureFacts,
} from '../../src/graph/compatibility';

const facts = (partial: Partial<StructureFacts> = {}): StructureFacts => ({
  csharpFiles: 12,
  xamlFiles: 3,
  projectFiles: ['src/App.csproj'],
  solutionFiles: ['App.sln'],
  truncated: false,
  ...partial,
});

const codes = (verdict: CompatibilityVerdict) => verdict.reasons.map((reason) => reason.code);

describe('assessCompatibility', () => {
  it('accepts a folder with C#, XAML, a project file and a solution', () => {
    const verdict = assessCompatibility(facts(), ANALYSIS_FILE_CAP, 'app');

    expect(verdict.verdict).toBe('compatible');
    expect(verdict.reasons).toEqual([]);
    expect(verdict.repository).toBe('app');
    expect(verdict.schemaVersion).toBe(1);
    expect(verdict.limits).toEqual({ analysisFileCap: ANALYSIS_FILE_CAP });
  });

  it('records a project file and a solution as facts that never decide the verdict', () => {
    const bare = assessCompatibility(facts({ projectFiles: [], solutionFiles: [] }), ANALYSIS_FILE_CAP);
    expect(bare.verdict).toBe('compatible');
    expect(bare.facts.projectFiles).toEqual([]);
    expect(bare.facts.solutionFiles).toEqual([]);
  });

  it('refuses a folder with no C# files, naming that structure', () => {
    const verdict = assessCompatibility(facts({ csharpFiles: 0 }), ANALYSIS_FILE_CAP);

    expect(verdict.verdict).toBe('incompatible');
    expect(codes(verdict)).toContain('no-csharp');
    const reason = verdict.reasons.find((entry) => entry.code === 'no-csharp')!;
    expect(reason.structure).toBe('no C# files');
    expect(reason.message).toContain('no C# files');
    expect(reason.evidence.length).toBeGreaterThan(0);
  });

  it('refuses a folder with no XAML files', () => {
    const verdict = assessCompatibility(facts({ xamlFiles: 0 }), ANALYSIS_FILE_CAP);

    const reason = verdict.reasons.find((entry) => entry.code === 'no-xaml')!;
    expect(verdict.verdict).toBe('incompatible');
    expect(reason.structure).toBe('no XAML files');
    expect(reason.message).toContain('no XAML files');
  });

  it('refuses a repository at the file cap, naming the counts', () => {
    const verdict = assessCompatibility(facts({ csharpFiles: 19999, xamlFiles: 1 }), ANALYSIS_FILE_CAP);

    const reason = verdict.reasons.find((entry) => entry.code === 'over-analysis-cap')!;
    expect(reason.message).toContain(`${ANALYSIS_FILE_CAP}`);
    expect(reason.evidence.some((line) => line.includes(`${ANALYSIS_FILE_CAP}`))).toBe(true);
  });

  it('refuses a repository the walk stopped on, even below the cap', () => {
    const verdict = assessCompatibility(facts({ csharpFiles: 5, xamlFiles: 1, truncated: true }), ANALYSIS_FILE_CAP);
    expect(codes(verdict)).toContain('over-analysis-cap');
  });

  it.each(['src/Other.fsproj', 'Other.vbproj', 'Other.vcxproj'])(
    'refuses %s, naming the project path',
    (project) => {
      const verdict = assessCompatibility(facts({ projectFiles: [project] }), ANALYSIS_FILE_CAP);

      expect(verdict.verdict).toBe('incompatible');
      const reason = verdict.reasons.find((entry) => entry.code === 'unsupported-project')!;
      expect(reason.structure).toBe(project);
      expect(reason.message).toContain(project.slice(project.lastIndexOf('.')));
      expect(reason.evidence.length).toBeGreaterThan(0);
    },
  );

  it('names at most three unsupported projects and counts the rest', () => {
    const verdict = assessCompatibility(
      facts({ projectFiles: ['a/x.fsproj', 'b/y.fsproj', 'c/z.fsproj', 'd/w.vbproj'] }),
      ANALYSIS_FILE_CAP,
    );

    const named = verdict.reasons.filter((entry) => entry.code === 'unsupported-project');
    expect(named.map((entry) => entry.structure)).toEqual(['a/x.fsproj', 'b/y.fsproj', 'c/z.fsproj']);
    expect(named.at(-1)!.evidence.some((line) => line.includes('4'))).toBe(true);
  });

  it('reports every failed observation rather than the first', () => {
    const verdict = assessCompatibility(
      facts({ csharpFiles: 0, xamlFiles: 0, projectFiles: ['other.fsproj'] }),
      ANALYSIS_FILE_CAP,
    );

    expect(codes(verdict)).toEqual(['no-csharp', 'no-xaml', 'unsupported-project']);
  });

  it('answers unknown, with one note, when the facts could not be read at all', () => {
    const verdict = assessCompatibility(undefined, ANALYSIS_FILE_CAP, 'app');

    expect(verdict.verdict).toBe('unknown');
    expect(verdict.reasons).toHaveLength(1);
    expect(verdict.reasons[0].code).toBe('unreadable-structure');
    expect(verdict.reasons[0].message).toContain('could not be listed');
    expect(verdict.facts.csharpFiles).toBe(0);
  });

  it('carries evidence on every reason and names what cannot be handled', () => {
    const verdict = assessCompatibility(
      facts({ csharpFiles: 0, xamlFiles: 0, projectFiles: ['other.fsproj'] }),
      ANALYSIS_FILE_CAP,
    );

    for (const reason of verdict.reasons) {
      expect(reason.structure.length, reason.code).toBeGreaterThan(0);
      expect(reason.message.length, reason.code).toBeGreaterThan(10);
      expect(reason.evidence.length, reason.code).toBeGreaterThan(0);
      expect(reason.evidence.some((line) => line.trim().length > 0), reason.code).toBe(true);
      // Portable: no drive letter, no machine path, no clock reading.
      expect(reason.evidence.join(' '), reason.code).not.toMatch(/^[A-Za-z]:\\/);
    }
  });
});

describe('the changes every refusal asks for', () => {
  it('maps every reason code to at least one change', () => {
    for (const [code, changes] of Object.entries(CHANGES_FOR_REASON)) {
      expect(changes.length, code).toBeGreaterThan(0);
      for (const change of changes) {
        expect(change.because).toBe(code);
        expect(change.title.length).toBeGreaterThan(10);
        expect(change.change.length).toBeGreaterThan(20);
      }
    }
  });

  it('carries the reason’s own evidence on every change', () => {
    const verdict = assessCompatibility(facts({ xamlFiles: 0 }), ANALYSIS_FILE_CAP);
    const reason = verdict.reasons.find((entry) => entry.code === 'no-xaml')!;

    for (const change of changesFor(reason)) {
      expect(change.evidence).toEqual(reason.evidence);
    }
  });
});

describe('the request text', () => {
  it('names the repository, states the command second, and closes with what was not changed', () => {
    const verdict = assessCompatibility(facts({ xamlFiles: 0 }), ANALYSIS_FILE_CAP, 'other-repo');
    const lines = buildUpdateRequest(verdict).split('\n');

    expect(lines[0]).toBe('# Extension change request: other-repo');
    expect(lines[2]).toBe('Run this with `/speckit-companion-specify`.');
    expect(lines).toContain('## What was not changed');
    expect(lines.at(-2)).toBe(
      'Nothing in other-repo was created or changed. This file is a request against the extension.',
    );
  });

  it('gives each reason a section in the verdict’s order, opening with the sentence shown', () => {
    const verdict = assessCompatibility(
      facts({ csharpFiles: 0, xamlFiles: 0, projectFiles: ['other.fsproj'] }),
      ANALYSIS_FILE_CAP,
      'other-repo',
    );
    const text = buildUpdateRequest(verdict);
    const positions = verdict.reasons.map((reason) => text.indexOf(`## ${reason.message}`));

    expect(positions.every((at) => at > 0)).toBe(true);
    expect([...positions].sort((left, right) => left - right)).toEqual(positions);
  });

  it('gives each change a subsection with at least one evidence line', () => {
    const verdict = assessCompatibility(facts({ xamlFiles: 0 }), ANALYSIS_FILE_CAP, 'other-repo');
    const text = buildUpdateRequest(verdict);

    for (const change of changesFor(verdict.reasons[0])) {
      const at = text.indexOf(`### ${change.title}`);
      expect(at).toBeGreaterThan(0);
      expect(text.indexOf('Evidence:', at)).toBeGreaterThan(at);
      expect(text.slice(at)).toContain(`- ${change.evidence[0]}`);
    }
  });

  it('carries no absolute path, machine name or timestamp', () => {
    const verdict = assessCompatibility(facts({ xamlFiles: 0 }), ANALYSIS_FILE_CAP, 'other-repo');
    const text = buildUpdateRequest(verdict);

    expect(text).not.toMatch(/[A-Za-z]:\\/);
    expect(text).not.toMatch(/\d{4}-\d{2}-\d{2}T/);
    expect(text).not.toContain(process.cwd());
  });
});

describe('slugFor', () => {
  it('lower-cases, hyphenates every run of other characters, and trims the ends', () => {
    expect(slugFor('MTM_Waitlist')).toBe('mtm-waitlist');
    expect(slugFor('WinUI3LearningLab')).toBe('winui3learninglab');
    expect(slugFor('MTM_Receiving_Application')).toBe('mtm-receiving-application');
    expect(slugFor('  spaced  name  ')).toBe('spaced-name');
    expect(slugFor('already-hyphenated')).toBe('already-hyphenated');
  });
});
