import type { DebugSection, RowTone } from './model';

const escapeHtml = (value: unknown): string => String(value)
  .replace(/&/g, '&amp;')
  .replace(/</g, '&lt;')
  .replace(/>/g, '&gt;')
  .replace(/"/g, '&quot;');

const TONE_COLOUR: Record<RowTone, string> = {
  bad: '#ef4444',
  warn: '#f59e0b',
  good: '#2fbf71',
  plain: 'inherit',
};

/** The heading word and title, rendered as `## Host — Host and workspace`. */
function headingOf(section: DebugSection): string {
  return `${section.category} — ${section.title}`;
}

/**
 * Renders one section, or every section, as Markdown for the clipboard.
 *
 * A single section is copied on its own, which is what makes a bug report short: the reporter pastes
 * the part that is wrong rather than the whole page.
 */
export function sectionText(sections: DebugSection[], id: string | null): string {
  const selected = id === null ? sections : sections.filter((section) => section.id === id);
  const blocks: string[] = [];

  if (id === null) {
    blocks.push('# C#/XAML Graph — Diagnostics');
    blocks.push('');
  }

  for (const section of selected) {
    blocks.push(`## ${headingOf(section)}`);
    blocks.push('');
    blocks.push(section.purpose);
    blocks.push('');
    if (section.files.length > 0) {
      blocks.push(`Files: ${section.files.map((file) => `\`${file}\``).join(', ')}`);
      blocks.push('');
    }
    for (const row of section.rows) {
      const tone = row.tone && row.tone !== 'plain' ? ` _(${row.tone})_` : '';
      blocks.push(`- **${row.label}** — ${row.value}${tone}`);
    }
    blocks.push('');
  }

  return blocks.join('\n').trimEnd();
}

function renderSection(section: DebugSection): string {
  const rows = section.rows.map((row) => {
    const colour = TONE_COLOUR[row.tone ?? 'plain'];
    const style = row.tone && row.tone !== 'plain' ? ` style="color:${colour}"` : '';
    return `<tr><th scope="row">${escapeHtml(row.label)}</th><td${style}>${escapeHtml(row.value)}</td></tr>`;
  }).join('\n');

  const files = section.files.length === 0
    ? ''
    : `<p class="files">${section.files.map((file) => `<code>${escapeHtml(file)}</code>`).join(' ')}</p>`;

  return [
    `<section class="section" id="section-${escapeHtml(section.id)}" data-section="${escapeHtml(section.id)}">`,
    '<header>',
    `<h2>${escapeHtml(section.category)} <span>${escapeHtml(section.title)}</span></h2>`,
    `<p class="purpose">${escapeHtml(section.purpose)}</p>`,
    files,
    `<div class="section-actions"><button type="button" data-copy="${escapeHtml(section.id)}">Copy this section</button></div>`,
    '</header>',
    '<table>',
    rows,
    '</table>',
    '</section>',
  ].join('\n');
}

/** Builds the whole diagnostics page: a nav list, a copy action per section, and the sections. */
export function renderDebugHtml(sections: DebugSection[], nonce: string): string {
  const nav = sections
    .map((section) => `<a href="#section-${escapeHtml(section.id)}">${escapeHtml(section.category)}</a>`)
    .join('\n      ');

  const body = sections.map(renderSection).join('\n');

  return `<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="UTF-8" />
<meta http-equiv="Content-Security-Policy" content="default-src 'none'; style-src 'unsafe-inline'; script-src 'nonce-${nonce}'" />
<meta name="viewport" content="width=device-width, initial-scale=1.0" />
<title>C#/XAML Graph — Diagnostics</title>
<style>
  body { font-family: var(--vscode-font-family, "Segoe UI", sans-serif); font-size: 13px;
         color: var(--vscode-foreground, #ccc); background: var(--vscode-editor-background, #1e1e1e);
         margin: 0; padding: 0 18px 40px; }
  h1 { font-size: 17px; margin: 16px 0 4px; }
  .subtitle { opacity: 0.65; margin: 0 0 12px; }
  nav { display: flex; flex-wrap: wrap; gap: 4px 12px; padding: 8px 0 12px;
        border-bottom: 1px solid var(--vscode-panel-border, #333); position: sticky; top: 0;
        background: var(--vscode-editor-background, #1e1e1e); }
  nav a { color: var(--vscode-textLink-foreground, #4a9eff); text-decoration: none; font-size: 12px; }
  nav a:hover { text-decoration: underline; }
  .toolbar { display: flex; gap: 8px; flex-wrap: wrap; margin: 12px 0; }
  button { padding: 3px 10px; font: inherit; font-size: 12px; cursor: pointer; border-radius: 2px;
           border: 1px solid transparent; color: var(--vscode-button-secondaryForeground, #ccc);
           background: var(--vscode-button-secondaryBackground, #3a3d41); }
  button.primary { color: var(--vscode-button-foreground, #fff);
                   background: var(--vscode-button-background, #0e639c); }
  .section { margin: 22px 0 0; }
  .section h2 { font-size: 14px; margin: 0 0 4px; }
  .section h2 span { font-weight: 400; opacity: 0.75; }
  .purpose { margin: 0 0 6px; opacity: 0.8; line-height: 1.45; }
  .files { margin: 0 0 8px; opacity: 0.7; }
  .files code { font-family: var(--vscode-editor-font-family, monospace); font-size: 11px;
                background: rgba(255,255,255,0.07); padding: 1px 5px; border-radius: 3px; }
  .section-actions { margin: 0 0 8px; }
  .section-actions button { font-size: 11px; padding: 2px 8px; }
  table { border-collapse: collapse; width: 100%; }
  tr:nth-child(odd) { background: rgba(255,255,255,0.03); }
  th, td { text-align: left; padding: 4px 8px; vertical-align: top; font-size: 12px; }
  th { font-weight: 400; opacity: 0.72; white-space: nowrap; width: 1%; }
  td { font-family: var(--vscode-editor-font-family, monospace); word-break: break-word; }
  #toast { position: fixed; bottom: 14px; right: 14px; padding: 6px 12px; border-radius: 3px;
           background: var(--vscode-badge-background, #4d4d4d); color: var(--vscode-badge-foreground, #fff);
           opacity: 0; transition: opacity 140ms ease; font-size: 12px; }
  #toast.visible { opacity: 1; }
</style>
</head>
<body data-section="host">
<h1>C#/XAML Graph — Diagnostics</h1>
<p class="subtitle">One section per subsystem, each naming the files that own it. Times are local to this window.</p>

<nav>
      ${nav}
</nav>

<div class="toolbar">
  <button id="refresh" type="button" class="primary">Refresh report</button>
  <button id="reanalyze" type="button">Re-analyse the workspace</button>
  <button id="copyAll" type="button">Copy the whole report</button>
  <button id="openSettings" type="button">Extension settings</button>
  <button id="openMenu" type="button">Commands and settings panel</button>
</div>

${body}

<div id="toast" role="status" aria-live="polite"></div>

<script nonce="${nonce}">
  const api = acquireVsCodeApi();
  const send = (message) => api.postMessage(message);

  const toast = document.getElementById('toast');
  const announce = (text) => {
    toast.textContent = text;
    toast.classList.add('visible');
    setTimeout(() => toast.classList.remove('visible'), 1600);
  };

  document.getElementById('refresh').addEventListener('click', () => send({ type: 'debugRefresh' }));
  document.getElementById('reanalyze').addEventListener('click', () => send({ type: 'debugReanalyze' }));
  document.getElementById('copyAll').addEventListener('click', () => send({ type: 'debugCopy', section: null }));
  document.getElementById('openSettings').addEventListener('click', () => send({ type: 'debugOpenSettings' }));
  document.getElementById('openMenu').addEventListener('click', () => send({ type: 'debugOpenMenu' }));

  for (const button of document.querySelectorAll('button[data-copy]')) {
    button.addEventListener('click', () => send({ type: 'debugCopy', section: button.dataset.copy }));
  }

  window.addEventListener('message', (event) => {
    const message = event.data;
    if (message && message.type === 'debugCopied') {
      announce(message.text || 'Copied');
    }
  });
</script>
</body>
</html>`;
}
