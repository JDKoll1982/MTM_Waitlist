import type { Uri, Webview } from 'vscode';

export interface HtmlContext {
  webview: Webview;
  scriptUri: Uri;
  styleUri: Uri;
  nonce: string;
}

function makeNonce(): string {
  const alphabet = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789';
  let value = '';
  for (let index = 0; index < 32; index += 1) {
    value += alphabet.charAt(Math.floor(Math.random() * alphabet.length));
  }
  return value;
}

export { makeNonce };

/**
 * The legend. Rendered as static markup with inline SVG, so each entry shows the same shape, colour
 * and line style the canvas uses, and stays readable in light, dark and high-contrast themes.
 */
function layerRow(layer: string, label: string, colour: string, shape: 'rect' | 'circle', dashed = false): string {
  const icon = shape === 'rect'
    ? `<svg width="20" height="16" viewBox="0 0 20 16" aria-hidden="true"><rect x="4" y="4" width="12" height="8" rx="2" fill="${colour}"/></svg>`
    : `<svg width="20" height="16" viewBox="0 0 20 16" aria-hidden="true"><circle cx="10" cy="8" r="5.5" fill="${colour}"${dashed ? ' stroke="#9a9a9a" stroke-width="1.2" stroke-dasharray="2.5 1.8" fill-opacity="0.12"' : ''}/></svg>`;
  return `<label class="check"><input id="layer-${layer}" type="checkbox" />${icon}${label}`
    + `<span class="count" id="count-layer-${layer}"></span></label>`;
}

function edgeRow(kind: string, label: string): string {
  return `<label class="check"><input id="edge-${kind}" type="checkbox" />${label}`
    + `<span class="count" id="count-edge-${kind}"></span></label>`;
}

/** The layers-and-edges flyout: replaces two long toolbar rows with one button. */
function visibilityFlyout(): string {
  return `
<div id="visibility" hidden>
  <div class="flyout-card">
    <div class="flyout-head">
      <h2>Visible layers and edges</h2>
      <button id="visibility-close" type="button" title="Close (Esc)">Close</button>
    </div>
    <div class="flyout-body">
      <div class="presets">
        <span class="group-label">Presets</span>
        <button id="preset-mvvm" type="button">MVVM flow</button>
        <button id="preset-structure" type="button">Structure</button>
        <button id="preset-cleanup" type="button">Cleanup</button>
        <button id="preset-all" type="button">Everything</button>
      </div>

      <section>
        <h3>Node layers</h3>
        <div class="grid">
          ${layerRow('view', 'Views', '#4a9eff', 'rect')}
          ${layerRow('viewmodel', 'View models', '#2fbf71', 'rect')}
          ${layerRow('service', 'Services', '#f0a030', 'circle')}
          ${layerRow('model', 'Models', '#9b8cff', 'circle')}
          ${layerRow('external', 'External', '#8a8a8a', 'circle', true)}
        </div>
      </section>

      <section>
        <h3>Edge kinds</h3>
        <div class="grid">
          ${edgeRow('resolves', 'DI registration')}
          ${edgeRow('getservice', 'Service locator')}
          ${edgeRow('binds', 'XAML binding')}
          ${edgeRow('injects', 'Constructor injection')}
          ${edgeRow('field', 'Field-backed')}
          ${edgeRow('constructs', 'Constructed')}
          ${edgeRow('composes', 'Composes')}
          ${edgeRow('reference', 'Type reference')}
          ${edgeRow('inherits', 'Inherits')}
          ${edgeRow('implements', 'Implements')}
          ${edgeRow('template', 'Template rows')}
        </div>
        <p class="hint">Type references are every other mention of a type — a property type, a cast,
        an <code>is</code>, an attribute, a static call. They are what makes structural cycles
        visible, and they are off by default because there are many of them.</p>
      </section>
    </div>
  </div>
</div>`;
}

/**
 * The legend. Rendered as static markup with inline SVG, so each entry shows the same shape, colour
 * and line style the canvas uses, and stays readable in light, dark and high-contrast themes.
 */
function legendMarkup(): string {
  const node = (body: string): string =>
    `<svg width="26" height="18" viewBox="0 0 26 18" aria-hidden="true">${body}</svg>`;
  const edge = (body: string): string =>
    `<svg width="34" height="12" viewBox="0 0 34 12" aria-hidden="true">${body}</svg>`;

  const circle = (fill: string, extra = ''): string =>
    node(`<circle cx="13" cy="9" r="6" fill="${fill}" ${extra}/>`);
  const rect = (fill: string, extra = ''): string =>
    node(`<rect x="7" y="5" width="13" height="9" rx="2" fill="${fill}" ${extra}/>`);
  const line = (colour: string, dash = '', width = 2): string =>
    edge(`<line x1="1" y1="6" x2="27" y2="6" stroke="${colour}" stroke-width="${width}"`
      + `${dash ? ` stroke-dasharray="${dash}"` : ''}/><path d="M27 2.5 L33 6 L27 9.5 Z" fill="${colour}"/>`);

  return `
<div id="legend" hidden>
  <div class="legend-card">
    <div class="legend-head">
      <h2>What the shapes mean</h2>
      <button id="legend-close" type="button" title="Close (Esc)">Close</button>
    </div>

    <div class="legend-body">
      <section>
        <h3>Nodes are types, coloured by layer</h3>
        <dl>
          <dt>${rect('#4a9eff')}</dt><dd><b>View</b> — a Page, UserControl or Window with XAML. Drawn wider than tall.</dd>
          <dt>${rect('#2fbf71')}</dt><dd><b>View model</b> — a class ending in <code>ViewModel</code>.</dd>
          <dt>${circle('#f0a030')}</dt><dd><b>Service</b> — an interface or a class named for a role: Service, Store, Provider, Reader, Catalog, Policy, Repository.</dd>
          <dt>${circle('#9b8cff')}</dt><dd><b>Model</b> — everything else: models, enums, records, converters, helpers.</dd>
          <dt>${circle('none', 'stroke="#9a9a9a" stroke-width="1.5" stroke-dasharray="3 2"')}</dt>
            <dd><b>External</b> — referenced but not declared here: framework and package types. Never expanded.</dd>
        </dl>
      </section>

      <section>
        <h3>Rings mark state</h3>
        <dl>
          <dt>${circle('#2fbf71', 'stroke="#ffffff" stroke-width="2.5"')}</dt>
            <dd><b>White ring</b> — the type is registered with the dependency-injection container.</dd>
          <dt>${circle('#2fbf71', 'stroke="#ef4444" stroke-width="2.5"')}</dt>
            <dd><b>Red ring</b> — the type is part of a dependency cycle. See the Problems tab.</dd>
          <dt>${circle('#2fbf71', 'stroke="#f59e0b" stroke-width="2" stroke-dasharray="3 2"')}</dt>
            <dd><b>Amber dashed ring</b> — the type has a cleanup finding: unused, dead binding, or unbound view.</dd>
        </dl>
      </section>

      <section>
        <h3>Lines are the kinds of dependency</h3>
        <dl>
          <dt>${line('#2fbf71')}</dt><dd><b>DI registration</b> — <code>pageService.Configure&lt;TViewModel, TPage&gt;()</code>. Points from the page to its view model.</dd>
          <dt>${line('#4a9eff')}</dt><dd><b>Service locator</b> — <code>App.GetService&lt;T&gt;()</code> or <code>GetRequiredService&lt;T&gt;()</code>.</dd>
          <dt>${line('#38bdf8', '5 3')}</dt><dd><b>XAML binding</b> — an <code>x:Bind</code> path that resolves to this type.</dd>
          <dt>${line('#f0a030')}</dt><dd><b>Constructor injection</b> — a parameter of this type.</dd>
          <dt>${line('#4a9eff', '5 3')}</dt><dd><b>Field-backed</b> — <code>_viewModel = viewModel ?? throw …</code>.</dd>
          <dt>${line('#c084fc')}</dt><dd><b>Constructed</b> — assigned from a <code>new</code> expression.</dd>
          <dt>${line('#c084fc', '2 3')}</dt><dd><b>Composes</b> — a view model or service creates another.</dd>
          <dt>${line('#8a8a8a')}</dt><dd><b>Inherits</b> — a base class.</dd>
          <dt>${line('#8a8a8a', '4 3')}</dt><dd><b>Implements</b> — an interface, including a container registration pairing.</dd>
          <dt>${line('#555555', '2 3', 1)}</dt><dd><b>Template row</b> — a <code>DataTemplate x:DataType</code> row model. Hidden unless switched on.</dd>
        </dl>
      </section>

      <section>
        <h3>Reading the layout</h3>
        <p><b>Packed (auto)</b> arranges each connected piece internally, then tiles the pieces across
        and down the panel. A dependency graph is many small shapes, not one big one, so this fills the
        space instead of stacking into a single column.</p>
        <p><b>Click</b> a node to inspect it. <b>Double-click</b> to open its source.
        <b>Esc</b> clears a focus.</p>
      </section>
    </div>
  </div>
</div>`;
}

/** Builds the webview document. Every inline resource except the nonce'd script lives on disk. */
export function renderHtml(context: HtmlContext): string {
  const { webview, scriptUri, styleUri } = context;
  const csp = [
    "default-src 'none'",
    `style-src ${webview.cspSource} 'unsafe-inline'`,
    `script-src 'nonce-${context.nonce}'`,
    `img-src ${webview.cspSource} data:`,
    `font-src ${webview.cspSource}`,
  ].join('; ');

  return `<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="UTF-8" />
<meta http-equiv="Content-Security-Policy" content="${csp}" />
<meta name="viewport" content="width=device-width, initial-scale=1.0" />
<link rel="stylesheet" href="${styleUri.toString()}" />
<title>C#/XAML Dependency Graph</title>
</head>
<body>
<div id="shell">

  <div id="toolbar">
    <div class="row">
      <input id="search" type="search" placeholder="Filter by name, namespace or module  (Esc clears)" />
      <label class="field">Module<select id="module" title="Restrict the graph to one module"></select></label>
      <label class="field">Layout<select id="layout" title="How the nodes are arranged">
        <option value="packed">Packed (auto)</option>
        <option value="layered-lr">Layered →</option>
        <option value="layered-tb">Layered ↓</option>
        <option value="force">Force</option>
        <option value="radial">Radial</option>
        <option value="grid">Grid</option>
        <option value="circle">Circle</option>
      </select></label>
      <label class="field">Density<select id="density" title="Node size and spacing">
        <option value="compact">Compact</option>
        <option value="normal">Normal</option>
        <option value="roomy">Roomy</option>
      </select></label>
      <label class="field">Labels<select id="labels" title="When node names are drawn">
        <option value="zoom">When zoomed in</option>
        <option value="always">Always</option>
        <option value="hover">On hover</option>
        <option value="none">Never</option>
      </select></label>
      <label class="field">Hops<select id="depth" title="How far the focus reaches">
        <option value="1">1</option>
        <option value="2">2</option>
        <option value="3">3</option>
      </select></label>
      <label class="check"><input id="hide-orphans" type="checkbox" />Hide unconnected</label>
      <label class="check"><input id="only-problems" type="checkbox" />Only problems</label>
      <span class="spacer"></span>
      <button id="visibility-open" type="button" aria-expanded="false"
              title="Choose which node layers and edge kinds are visible">Layers &amp; edges</button>
      <button id="legend-open" type="button" title="What the shapes, colours and lines mean">Legend</button>
      <button id="fit" type="button" title="Frame the visible graph">Fit</button>
      <button id="toggle-panel" type="button" aria-pressed="false" title="Give the graph the full width">Hide details</button>
      <button id="reanalyze" type="button" class="primary">Re-analyse</button>
      <button id="export" type="button">Export JSON</button>
    </div>

    <div class="row status">
      <span id="stats">analysing…</span>
      <span id="focus-bar" hidden></span>
    </div>
  </div>

  ${visibilityFlyout()}

  <div id="main">
    <div id="canvas"><div id="cy"></div></div>

    <div id="side">
      <div id="side-tabs">
        <button id="tab-details" type="button" class="tab active">Details</button>
        <button id="tab-problems" type="button" class="tab">Problems <span id="problem-count" class="badge">0</span></button>
      </div>
      <div id="details" class="pane active"><p class="muted">Select a node to inspect it. Double-click to open the source.</p></div>
      <div id="problems" class="pane"><p class="muted">Run the analysis to see cleanup findings.</p></div>
    </div>
  </div>

  ${legendMarkup()}

</div>
<script nonce="${context.nonce}" src="${scriptUri.toString()}"></script>
</body>
</html>`;
}
