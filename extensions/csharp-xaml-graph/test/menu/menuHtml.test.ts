import { describe, expect, it } from 'vitest';
import {
  DEFAULT_EXCLUDE,
  DEFAULT_INCLUDE,
  makeNonce,
  renderMenuHtml,
  type MenuSettings,
} from '../../src/menu/menuHtml';
import { UNPACKED_LAYOUTS } from '../../src/graph/settings';

const settings = (partial: Partial<MenuSettings> = {}): MenuSettings => ({
  include: DEFAULT_INCLUDE,
  exclude: DEFAULT_EXCLUDE,
  includeExternalTypes: true,
  maxNodes: 4000,
  unpackedLayout: 'layered-tb',
  ...partial,
});

describe('menu defaults', () => {
  it('analyses every C# and XAML file and skips build output, tests and agent samples', () => {
    expect(DEFAULT_INCLUDE).toEqual(['**/*.cs', '**/*.xaml']);
    expect(DEFAULT_EXCLUDE).toEqual([
      '**/bin/**', '**/obj/**', '**/node_modules/**', '**/*.Tests/**', '**/.github/**', '**/.specify/**',
    ]);
  });
});

describe('renderMenuHtml', () => {
  const html = (partial: Partial<MenuSettings> = {}) => renderMenuHtml(settings(partial), 'nonce-1');

  it('is a nonce-locked page', () => {
    const page = html();
    expect(page).toContain("default-src 'none'");
    expect(page).toContain("script-src 'nonce-nonce-1'");
    expect(page).toContain('<script nonce="nonce-1">');
  });

  it('opens with what the tab is for and the three steps to start', () => {
    const page = html();
    expect(page).toContain('<h1>C#/XAML Dependency Graph</h1>');
    expect(page).toContain('Draw what binds to what in this workspace');
    expect(page).toContain('<h2>Start here</h2>');
    expect(page).toContain('Right-click a shape');
  });

  it('offers a shortcut for every contributed command', () => {
    const page = html();
    for (const id of ['show', 'reanalyze', 'export', 'profile-initialize', 'profile-import', 'open-settings', 'debug']) {
      expect(page).toContain(`id="${id}"`);
    }
    for (const command of [
      'Show Dependency Graph',
      'Refresh Dependency Graph',
      'Show Dependency Graph for Selection',
      'Show Dependency Graph for File',
      'Open Debug Report',
      'Set up Repository Profile',
      'Import Profile Reply',
    ]) {
      expect(page).toContain(`<td class="key">${command}</td>`);
    }
  });

  it('offers the two profile actions, saying setup asks before proposing', () => {
    const page = html();
    expect(page).toContain('>Set up repository profile</button>');
    expect(page).toContain('>Import profile reply</button>');
    // The row explains what the assistant is asked to do, in the order it does it.
    expect(page).toContain('The prompt asks before proposing');
    expect(page).toContain('what marks a service, what marks a view model, how registration happens');
    expect(page).toContain('A reply that would not make a usable profile is refused');
    // Both buttons reach the extension through the same command message the other shortcuts use.
    expect(page).toContain("post('command', { command: 'profileInitialize' })");
    expect(page).toContain("post('command', { command: 'profileImport' })");
  });

  it('shows the include and exclude patterns one per line', () => {
    const page = html();
    expect(page).toContain('<textarea id="include" spellcheck="false" rows="3">**/*.cs\n**/*.xaml</textarea>');
    expect(page).toContain('**/bin/**');
    expect(page).toContain('**/.specify/**');
  });

  it('escapes a pattern that contains markup', () => {
    const page = html({ include: ['<script>'] });
    expect(page).toContain('&lt;script&gt;');
  });

  it('offers every unpacked layout and marks the configured one selected', () => {
    const page = html({ unpackedLayout: 'grid' });
    for (const layout of UNPACKED_LAYOUTS) {
      expect(page).toContain(`<option value="${layout}"`);
    }
    expect(page).toContain('<option value="grid" selected>Grid</option>');
  });

  it('describes the layout that is currently selected', () => {
    expect(html({ unpackedLayout: 'circle' })).toContain('One ring. Only legible for a small group.');
  });

  it('reflects the node cap and the external-types checkbox', () => {
    const page = html({ maxNodes: 1234, includeExternalTypes: false });
    expect(page).toContain('value="1234"');
    expect(page).toMatch(/<input id="include-external" type="checkbox" \/>/);
    expect(html({ includeExternalTypes: true })).toContain(
      '<input id="include-external" type="checkbox" checked />',
    );
  });

  it('posts a save message naming every field the form edits', () => {
    const page = html();
    expect(page).toContain("post('save', {");
    for (const field of ['include', 'exclude', 'includeExternalTypes', 'maxNodes', 'unpackedLayout']) {
      expect(page).toContain(field);
    }
    expect(page).toContain("document.getElementById('settings-form').addEventListener('submit'");
  });

  it('posts reset, command and status messages', () => {
    const page = html();
    expect(page).toContain("post('reset')");
    expect(page).toContain("post('command', { command: 'show' })");
    expect(page).toContain('Settings saved for this workspace.');
    expect(page).toContain('Could not save the settings.');
  });
});

describe('menu makeNonce', () => {
  it('is a 32-character safe token, different each call', () => {
    const first = makeNonce();
    expect(first).toMatch(/^[A-Za-z0-9]{32}$/);
    expect(makeNonce()).not.toBe(first);
  });
});
