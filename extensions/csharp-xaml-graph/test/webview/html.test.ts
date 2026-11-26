import { describe, expect, it } from 'vitest';
import { makeNonce, renderHtml } from '../../src/webview/html';
import { Uri } from '../support/vscode';

const context = () => ({
  webview: {
    cspSource: 'vscode-webview://abc',
    asWebviewUri: (uri: Uri) => uri,
  } as unknown as Parameters<typeof renderHtml>[0]['webview'],
  scriptUri: Uri.file('/ext/out/webview/main.js'),
  styleUri: Uri.file('/ext/out/webview/style.css'),
  nonce: 'NONCE123',
});

const html = () => renderHtml(context());

describe('renderHtml', () => {
  it('locks the page down with a nonce’d script and the webview origin', () => {
    const page = html();
    expect(page).toContain("default-src 'none'");
    expect(page).toContain("script-src 'nonce-NONCE123'");
    expect(page).toContain('style-src vscode-webview://abc');
    expect(page).toContain('img-src vscode-webview://abc data:');
    expect(page).toContain('font-src vscode-webview://abc');
    expect(page).not.toContain('unsafe-eval');
  });

  it('loads the built stylesheet and script from disk with the nonce on the script', () => {
    const page = html();
    expect(page).toContain('<link rel="stylesheet" href="file:///ext/out/webview/style.css" />');
    expect(page).toContain('<script nonce="NONCE123" src="file:///ext/out/webview/main.js"></script>');
  });

  it('puts the toolbar in three rows: inputs, checks, actions', () => {
    const page = html();
    expect(page.match(/<div class="row [a-z]+">/g)).toEqual([
      '<div class="row inputs">',
      '<div class="row checks">',
      '<div class="row actions">',
    ]);
  });

  it('carries every control the canvas script wires up', () => {
    for (const id of [
      'search', 'module', 'layout', 'density', 'labels', 'depth',
      'hide-orphans', 'only-problems',
      'visibility', 'visibility-open', 'visibility-close',
      'legend', 'legend-open', 'legend-close',
      'fit', 'repack', 'unpack-status', 'toggle-panel', 'reanalyze', 'export', 'update-profile',
      'cy', 'view-controls', 'zoom-region', 'zoom-slider', 'view-reset', 'zoom-rect',
      'progress', 'progress-message', 'progress-percent', 'progress-fill',
      'focus-bar', 'stats', 'tooltip', 'details', 'problems', 'repository-notes', 'finding-problems',
      'tab-details', 'tab-problems', 'problem-count', 'node-menu',
      'preset-mvvm', 'preset-structure', 'preset-cleanup', 'preset-all',
    ]) {
      expect(page(html(), `id="${id}"`)).toBe(true);
    }
  });

  it('puts the profile control last in the actions row, after Export JSON', () => {
    const page = html();
    const start = page.indexOf('<div class="row actions">');
    const row = page.slice(start, page.indexOf('</div>', start));

    expect(row).toContain('id="export"');
    expect(row).toContain('id="update-profile"');
    expect(row.indexOf('id="export"')).toBeLessThan(row.indexOf('id="update-profile"'));
    // Nothing else follows it, so both existing buttons keep their place.
    const buttons = [...row.matchAll(/<button id="([^"]+)"/g)].map((match) => match[1]);
    expect(buttons.at(-1)).toBe('update-profile');
    expect(row).toContain('>Update profile</button>');
  });

  it('heads the Problems pane with the container the profile rows fill', () => {
    const page = html();
    expect(page).toContain('<div id="repository-notes" hidden></div>');
    const problems = page.slice(page.indexOf('<div id="problems" class="pane">'));
    expect(problems.indexOf('id="repository-notes"')).toBeLessThan(problems.indexOf('id="finding-problems"'));
  });

  it('offers every layout and density the script understands', () => {
    const page = html();
    for (const value of ['packed', 'layered-lr', 'layered-tb', 'force', 'radial', 'grid', 'circle']) {
      expect(page).toContain(`<option value="${value}">`);
    }
    for (const value of ['compact', 'normal', 'roomy']) {
      expect(page).toContain(`<option value="${value}">`);
    }
    for (const value of ['zoom', 'always', 'hover', 'none']) {
      expect(page).toContain(`<option value="${value}">`);
    }
  });

  it('lists a checkbox for every layer and every edge kind', () => {
    const page = html();
    for (const layer of ['view', 'viewmodel', 'service', 'model', 'external']) {
      expect(page).toContain(`id="layer-${layer}"`);
      expect(page).toContain(`id="count-layer-${layer}"`);
    }
    for (const kind of [
      'resolves', 'getservice', 'binds', 'injects', 'field', 'constructs', 'composes',
      'reference', 'inherits', 'implements', 'template',
    ]) {
      expect(page).toContain(`id="edge-${kind}"`);
      expect(page).toContain(`id="count-edge-${kind}"`);
    }
  });

  it('explains the legend with the same shapes the canvas draws', () => {
    const page = html();
    expect(page).toContain('What the shapes mean');
    // Layers are told apart by silhouette as well as colour, so the legend says so and draws it.
    expect(page).toContain('Every layer has its own silhouette');
    expect(page).toContain('Rings mark state');
    expect(page).toContain('Lines are the kinds of dependency');
    expect(page).toContain('Regions drawn over the canvas');
    expect(page).toContain('<svg width="26" height="18"');
    expect(page).toContain('<svg width="34" height="12"');
    expect(page).toContain('<polygon'); // the service hexagon
  });

  it('provides the overview, the region layer and the new visibility toggles', () => {
    const page = html();
    expect(page).toMatch(/<div id="minimap" hidden role="img"/);
    for (const id of ['dim-selection', 'group-captions', 'module-frames', 'island-tray']) {
      expect(page).toContain(`id="${id}"`);
    }
    expect(page).toContain('id="preset-inheritance"');
  });

  it('starts the progress overlay hidden and the re-pack button hidden', () => {
    const page = html();
    expect(page).toMatch(/<div id="progress" hidden role="progressbar"/);
    expect(page).toMatch(/<button id="repack" type="button" hidden/);
    expect(page).toMatch(/<span id="unpack-status" class="unpack-status" hidden>/);
  });

  it('provides the details and problems panes the side panel switches between', () => {
    const page = html();
    expect(page).toContain('<div id="details" class="pane active">');
    expect(page).toContain('<div id="problems" class="pane">');
  });
});

function page(source: string, needle: string): boolean {
  return source.includes(needle);
}

describe('makeNonce', () => {
  it('produces 32 characters from the safe alphabet', () => {
    const nonce = makeNonce();
    expect(nonce).toHaveLength(32);
    expect(nonce).toMatch(/^[A-Za-z0-9]{32}$/);
  });

  it('produces a different value each time', () => {
    expect(makeNonce()).not.toBe(makeNonce());
  });
});
