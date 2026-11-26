import { describe, expect, it } from 'vitest';
import { renderRepositoryNotes } from '../../src/webview/client/profileProblems';
import { PROFILE_FILE_NAME } from '../../src/graph/profile';
import { NOT_ANALYSED } from '../../src/graph/compatibility';

const warning = (path: string, message: string) => `${PROFILE_FILE_NAME} › ${path}: ${message}`;

const compatibility = (structure: string, message: string) => `${NOT_ANALYSED} › ${structure}: ${message}`;

describe('renderRepositoryNotes', () => {
  it('renders one row per profile problem, with its path and its sentence', () => {
    const html = renderRepositoryNotes([
      warning('layers.rules[2].kinds', '`delegate` is not a declaration kind. It was ignored.'),
      warning('links.addCallPattern', '`^(Add` is not a usable regular expression.'),
    ]);

    expect(html.match(/class="profile-problem"/g)).toHaveLength(2);
    expect(html).toContain('<code class="profile-problem-path">layers.rules[2].kinds</code>');
    expect(html).toContain('is not a declaration kind');
    expect(html).toContain('<code class="profile-problem-path">links.addCallPattern</code>');
    expect(html).toContain('is not a usable regular expression');
    expect(html).toContain('2 notes');
  });

  it('renders a compatibility note with its structure in the muted path tone', () => {
    const html = renderRepositoryNotes([
      compatibility('no XAML files', 'No XAML files were found in the files the analysis reads.'),
    ]);

    expect(html).toContain('<code class="profile-problem-path">no XAML files</code>');
    expect(html).toContain('No XAML files were found in the files the analysis reads.');
    expect(html).toContain('1 note)');
  });

  it('carries both kinds in one list, above the findings, in the order it was given', () => {
    const html = renderRepositoryNotes([
      compatibility('src/Other.fsproj', 'The analysis reads .csproj projects.'),
      warning('layers', 'Expected a section.'),
      compatibility('no C# files', 'No C# files were found.'),
    ]);

    expect(html.match(/class="profile-problem"/g)).toHaveLength(3);
    expect(html).toContain('3 notes');
    expect(html.indexOf('src/Other.fsproj')).toBeLessThan(html.indexOf('layers'));
    expect(html.indexOf('layers')).toBeLessThan(html.indexOf('no C# files'));
    expect(html.startsWith('<section class="profile-problems"')).toBe(true);
    expect(html.endsWith('</section>')).toBe(true);
    expect(html).toContain('<ul class="profile-problem-list">');
  });

  it('counts one note in the singular and names the block for the repository', () => {
    const html = renderRepositoryNotes([warning('layers', 'Expected a section.')]);
    expect(html).toContain('This repository (1 note)');
    expect(html).toContain('aria-label="Repository notes"');
  });

  it('escapes the text it renders, from either kind', () => {
    const html = renderRepositoryNotes([
      warning('layers.<script>', 'Found <b>markup</b> & "quotes".'),
      compatibility('<img src=x>', 'Cannot handle <b>this</b> & that.'),
    ]);

    expect(html).not.toContain('<script>');
    expect(html).not.toContain('<b>markup</b>');
    expect(html).not.toContain('<img src=x>');
    expect(html).toContain('&lt;script&gt;');
    expect(html).toContain('&lt;b&gt;markup&lt;/b&gt;');
    expect(html).toContain('&lt;img src=x&gt;');
    expect(html).toContain('&amp;');
    expect(html).toContain('&quot;quotes&quot;');
  });

  it('renders nothing rather than an empty box when there is no note of either kind', () => {
    expect(renderRepositoryNotes([])).toBe('');
    expect(renderRepositoryNotes(['A/Broken.cs: unexpected end of input'])).toBe('');
  });

  it('carries a whole-file problem with no path', () => {
    const html = renderRepositoryNotes([`${PROFILE_FILE_NAME}: The profile file is empty.`]);
    expect(html).toContain('The profile file is empty.');
    expect(html).not.toContain('profile-problem-path');
  });
});
