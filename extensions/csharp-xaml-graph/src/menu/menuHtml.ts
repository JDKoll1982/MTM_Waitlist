import { UNPACKED_LAYOUT_DESCRIPTION, UNPACKED_LAYOUT_LABEL, UNPACKED_LAYOUTS, type UnpackedLayout } from '../graph/settings';

/** The values the settings form edits, already read from the configuration. */
export interface MenuSettings {
  include: string[];
  exclude: string[];
  includeExternalTypes: boolean;
  maxNodes: number;
  unpackedLayout: UnpackedLayout;
}

export const DEFAULT_INCLUDE = ['**/*.cs', '**/*.xaml'];
export const DEFAULT_EXCLUDE = [
  '**/bin/**', '**/obj/**', '**/node_modules/**', '**/*.Tests/**',
  '**/.github/**', '**/.specify/**',
];

const escapeHtml = (value: unknown): string => String(value)
  .replace(/&/g, '&amp;')
  .replace(/</g, '&lt;')
  .replace(/>/g, '&gt;')
  .replace(/"/g, '&quot;');

export function makeNonce(): string {
  const alphabet = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789';
  let value = '';
  for (let index = 0; index < 32; index += 1) {
    value += alphabet.charAt(Math.floor(Math.random() * alphabet.length));
  }
  return value;
}

/**
 * The panel tab that sits beside Terminal: shortcuts to every command, and the extension's
 * settings in a form the reader can change without leaving the workspace.
 *
 * Written for the person using the graph, so it opens with what the tab is for, then what each
 * button does, then the settings with their effect in plain words.
 */
export function renderMenuHtml(settings: MenuSettings, nonce: string): string {
  const csp = [
    "default-src 'none'",
    "style-src 'unsafe-inline'",
    `script-src 'nonce-${nonce}'`,
  ].join('; ');

  return `<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="UTF-8" />
<meta http-equiv="Content-Security-Policy" content="${csp}" />
<meta name="viewport" content="width=device-width, initial-scale=1.0" />
<title>C#/XAML Graph</title>
<style>
  :root { color-scheme: light dark; }
  * { box-sizing: border-box; }
  body {
    margin: 0;
    padding: 10px 14px 20px;
    font-family: var(--vscode-font-family, "Segoe UI", sans-serif);
    font-size: var(--vscode-font-size, 13px);
    color: var(--vscode-foreground, #ccc);
    background: var(--vscode-panel-background, var(--vscode-editor-background, #1e1e1e));
  }
  h1 { font-size: 14px; margin: 0 0 4px; font-weight: 600; }
  h2 { font-size: 12px; margin: 18px 0 6px; text-transform: uppercase; letter-spacing: 0.06em; opacity: 0.7; }
  p { margin: 0 0 8px; line-height: 1.45; }
  .purpose { opacity: 0.8; max-width: 78ch; }
  ol { margin: 0 0 4px 18px; padding: 0; line-height: 1.6; }
  code { font-family: var(--vscode-editor-font-family, monospace); font-size: 11.5px; }
  .actions { display: flex; flex-wrap: wrap; gap: 6px; margin: 6px 0 2px; }
  button {
    font: inherit;
    padding: 4px 10px;
    border: 1px solid var(--vscode-button-border, transparent);
    border-radius: 3px;
    background: var(--vscode-button-background, #0e639c);
    color: var(--vscode-button-foreground, #fff);
    cursor: pointer;
  }
  button.secondary {
    background: var(--vscode-button-secondaryBackground, #3a3d41);
    color: var(--vscode-button-secondaryForeground, #fff);
  }
  button:hover { filter: brightness(1.12); }
  table { width: 100%; border-collapse: collapse; margin: 4px 0 2px; }
  th, td { text-align: left; padding: 4px 8px 4px 0; vertical-align: top; border-bottom: 1px solid var(--vscode-panel-border, #333); }
  th { font-weight: 600; font-size: 11px; opacity: 0.75; }
  td.key { white-space: nowrap; font-family: var(--vscode-editor-font-family, monospace); font-size: 11.5px; }
  label.field { display: grid; gap: 3px; margin: 0 0 10px; }
  label.field span.name { font-weight: 600; }
  label.field span.note { opacity: 0.7; font-size: 11.5px; }
  textarea, input[type="number"], select {
    font-family: var(--vscode-editor-font-family, monospace);
    font-size: 11.5px;
    padding: 4px 6px;
    border: 1px solid var(--vscode-input-border, #3c3c3c);
    border-radius: 2px;
    background: var(--vscode-input-background, #3c3c3c);
    color: var(--vscode-input-foreground, #ccc);
    width: 100%;
  }
  textarea { min-height: 58px; resize: vertical; }
  .check { display: flex; align-items: flex-start; gap: 7px; margin: 0 0 10px; }
  .check input { margin-top: 3px; }
  .status { margin-top: 8px; min-height: 18px; font-size: 11.5px; opacity: 0.8; }
  .status.error { color: var(--vscode-errorForeground, #f48771); }
</style>
</head>
<body>

<h1>C#/XAML Dependency Graph</h1>
<p class="purpose">Draw what binds to what in this workspace: each view with the view model the
container gives it, and each type with what it injects, inherits and constructs.</p>

<h2>Start here</h2>
<ol>
  <li><b>Show the graph.</b> Draws the whole workspace, which takes a few seconds the first time.</li>
  <li><b>Pick a module</b> in the graph's toolbar to narrow it to one part of the application.</li>
  <li><b>Right-click a shape</b> and choose <b>Unpack this group</b> to draw that shape at full size,
      from its starting root to the end of the group.</li>
</ol>
<div class="actions">
  <button id="show">Show dependency graph</button>
  <button id="reanalyze" class="secondary">Re-analyse now</button>
  <button id="export" class="secondary">Export JSON</button>
  <button id="open-settings" class="secondary">Open VS Code settings</button>
  <button id="debug" class="secondary">Debug report</button>
</div>

<h2>Commands</h2>
<table>
  <tr><th>Command</th><th>Where to find it</th><th>What it does</th></tr>
  <tr>
    <td class="key">Show Dependency Graph</td>
    <td>Command Palette</td>
    <td>Draws the whole workspace and opens the graph panel.</td>
  </tr>
  <tr>
    <td class="key">Refresh Dependency Graph</td>
    <td>Command Palette, or <b>Re-analyse</b> in the graph</td>
    <td>Reads the files again and redraws. Use it after changing a constructor or a page registration.</td>
  </tr>
  <tr>
    <td class="key">Show Dependency Graph for Selection</td>
    <td>Right-click in the editor</td>
    <td>Draws the graph around the type, method, property or field under the cursor. If more than one
        graph could match, a picker asks which one you mean.</td>
  </tr>
  <tr>
    <td class="key">Show Dependency Graph for File</td>
    <td>Right-click in the editor, or on a file in Explorer</td>
    <td>Draws the types declared in that file and their partners. A file with several types offers the
        whole file or one type at a time.</td>
  </tr>
  <tr>
    <td class="key">Open Debug Report</td>
    <td>Command Palette, or <b>Debug report</b> above</td>
    <td>Opens a diagnostics page organised by subsystem: host, settings, analysis, parsers, findings,
        canvas state, commands and packaging — each labelled with the files that own it.</td>
  </tr>
</table>

<h2>Settings</h2>
<p class="purpose">These are stored in this workspace, so a change affects only this project. Leave a
setting at its default and it follows the extension instead.</p>

<form id="settings-form">
  <label class="field">
    <span class="name">Files to analyse</span>
    <textarea id="include" spellcheck="false" rows="3">${escapeHtml(settings.include.join('\n'))}</textarea>
    <span class="note">One glob per line, relative to the workspace folder. Default: every <code>.cs</code> and <code>.xaml</code> file.</span>
  </label>

  <label class="field">
    <span class="name">Files to skip</span>
    <textarea id="exclude" spellcheck="false" rows="4">${escapeHtml(settings.exclude.join('\n'))}</textarea>
    <span class="note">One glob per line. Default: build output and the test project. Remove the test
    pattern if you want test usage counted before a member is called unused.</span>
  </label>

  <label class="field">
    <span class="name">Largest graph to draw</span>
    <input id="max-nodes" type="number" min="100" step="100" value="${escapeHtml(settings.maxNodes)}" />
    <span class="note">A safety cap on how many types are sent to the panel. Lower it if drawing feels slow.</span>
  </label>

  <label class="field">
    <span class="name">Layout of an unpacked shape</span>
    <select id="unpacked-layout">
      ${UNPACKED_LAYOUTS.map((value) => `<option value="${value}"${settings.unpackedLayout === value ? ' selected' : ''}>${UNPACKED_LAYOUT_LABEL[value]}</option>`).join('\n      ')}
    </select>
    <span class="note">${escapeHtml(UNPACKED_LAYOUT_DESCRIPTION[settings.unpackedLayout])}</span>
  </label>

  <label class="check">
    <input id="include-external" type="checkbox"${settings.includeExternalTypes ? ' checked' : ''} />
    <span>
      <span class="name">Show framework and package types</span><br />
      <span class="note">Draws base classes and package interfaces the workspace uses but does not declare.
      Turn it off for a graph of this repository's own types only.</span>
    </span>
  </label>

  <div class="actions">
    <button id="save" type="submit">Save settings</button>
    <button id="reset" type="button" class="secondary">Reset to defaults</button>
  </div>
  <div id="status" class="status" role="status"></div>
</form>

<script nonce="${nonce}">
  const vscode = acquireVsCodeApi();
  const status = document.getElementById('status');

  const say = (text, isError) => {
    status.textContent = text;
    status.className = isError ? 'status error' : 'status';
  };

  const post = (type, payload) => vscode.postMessage(Object.assign({ type }, payload));

  document.getElementById('show').addEventListener('click', () => post('command', { command: 'show' }));
  document.getElementById('reanalyze').addEventListener('click', () => post('command', { command: 'refresh' }));
  document.getElementById('export').addEventListener('click', () => post('command', { command: 'export' }));
  document.getElementById('open-settings').addEventListener('click', () => post('command', { command: 'openSettings' }));
  document.getElementById('debug').addEventListener('click', () => post('command', { command: 'debug' }));

  document.getElementById('settings-form').addEventListener('submit', (event) => {
    event.preventDefault();
    post('save', {
      settings: {
        include: document.getElementById('include').value.split('\\n').map((line) => line.trim()).filter(Boolean),
        exclude: document.getElementById('exclude').value.split('\\n').map((line) => line.trim()).filter(Boolean),
        includeExternalTypes: document.getElementById('include-external').checked,
        maxNodes: Number(document.getElementById('max-nodes').value) || 4000,
        unpackedLayout: document.getElementById('unpacked-layout').value,
      },
    });
    say('Saving…');
  });

  document.getElementById('reset').addEventListener('click', () => post('reset'));

  window.addEventListener('message', (event) => {
    const message = event.data || {};
    if (message.type === 'saved') {
      say('Settings saved for this workspace.');
      if (message.settings) {
        document.getElementById('include').value = message.settings.include.join('\\n');
        document.getElementById('exclude').value = message.settings.exclude.join('\\n');
        document.getElementById('max-nodes').value = message.settings.maxNodes;
        document.getElementById('include-external').checked = message.settings.includeExternalTypes;
        document.getElementById('unpacked-layout').value = message.settings.unpackedLayout;
      }
    }
    if (message.type === 'failed') {
      say(message.message || 'Could not save the settings.', true);
    }
  });
</script>
</body>
</html>`;
}

export type MenuMessage =
  | { type: 'command'; command: string }
  | { type: 'save'; settings: MenuSettings }
  | { type: 'reset' };


