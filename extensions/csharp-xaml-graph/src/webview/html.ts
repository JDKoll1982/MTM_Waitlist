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
        <button id="preset-inheritance" type="button">Inheritance</button>
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
  const hexagon = (fill: string): string =>
    node(`<polygon points="13,2.5 18.5,5.75 18.5,12.25 13,15.5 7.5,12.25 7.5,5.75" fill="${fill}"/>`);
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
        <h3>Every layer has its own silhouette, not just its own colour</h3>
        <p class="hint">The shape carries the layer when a label is too small to read, or when colour
        alone is not enough to tell two kinds apart.</p>
        <dl>
          <dt>${rect('#4a9eff')}</dt><dd><b>View</b> — a Page, UserControl or Window with XAML. A wide rounded rectangle.</dd>
          <dt>${rect('#2fbf71')}</dt><dd><b>View model</b> — a class ending in <code>ViewModel</code>. A rounded rectangle.</dd>
          <dt>${hexagon('#f0a030')}</dt><dd><b>Service</b> — an interface or a class named for a role: Service, Store, Provider, Reader, Catalog, Policy, Repository. A hexagon.</dd>
          <dt>${circle('#9b8cff')}</dt><dd><b>Model</b> — everything else: models, enums, records, converters, helpers. A circle.</dd>
          <dt>${circle('none', 'stroke="#9a9a9a" stroke-width="1.5" stroke-dasharray="3 2"')}</dt>
            <dd><b>External</b> — referenced but not declared here: framework and package types. A dashed circle. Never expanded.</dd>
        </dl>
      </section>

      <section>
        <h3>Rings mark state</h3>
        <dl>
          <dt>${circle('#2fbf71', 'stroke="#ffffff" stroke-width="2.5"')}</dt>
            <dd><b>Bright ring</b> — the type is registered with the dependency-injection container.</dd>
          <dt>${circle('#2fbf71', 'stroke="#ef4444" stroke-width="2.5"')}</dt>
            <dd><b>Red ring</b> — the type is part of a dependency cycle. See the Problems tab.</dd>
          <dt>${circle('#2fbf71', 'stroke="#f59e0b" stroke-width="2" stroke-dasharray="3 2"')}</dt>
            <dd><b>Amber dashed ring</b> — the type has a cleanup finding: unused, dead binding, or unbound view.</dd>
          <dt>${hexagon('#f0a030')}</dt>
            <dd><b>Double ring</b> — a <i>hub</i>: a type almost everything depends on. The packed layout lifts
            hubs out of the tiles and places each one between the groups it serves, which is why a hub can sit
            between two groups that otherwise never touch.</dd>
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
        <h3>Regions drawn over the canvas</h3>
        <dl>
          <dt>${rect('none', 'stroke="#4a9eff" stroke-width="1.5" stroke-dasharray="4 3"')}</dt>
            <dd><b>Group frame</b> — one packed shape, captioned with the type it starts at and how many
            types it holds. Turn it off with <b>Group captions</b>.</dd>
          <dt>${rect('none', 'stroke="#9b8cff" stroke-width="1.5"')}</dt>
            <dd><b>Module frame</b> — the region one module occupies. A module's frame is drawn only when it
            does not share most of its area with another module, because a frame that claims territory it does
            not own is worse than none. Turn it off with <b>Module frames</b>.</dd>
          <dt>${rect('none', 'stroke="#f59e0b" stroke-width="1.5" stroke-dasharray="6 3"')}</dt>
            <dd><b>Unlinked band</b> — every group holding a view that nothing binds to a view model, gathered
            below the tiles so the legacy screens are one place to look. Turn it off with <b>Unlinked band</b>.</dd>
        </dl>
      </section>

      <section>
        <h3>Reading the layout</h3>
        <p><b>Packed (auto)</b> arranges each connected piece internally, then tiles the pieces across
        and down the panel. A dependency graph is many small shapes, not one big one, so this fills the
        space instead of stacking into a single column.</p>
        <p><b>Dim others.</b> Clicking a type fades everything that is not its immediate neighbour, so one
        tree can be read inside a picture of hundreds of nodes. Click empty canvas to bring the rest back.</p>
        <p><b>Names.</b> A long name is shortened in its middle, keeping the end that tells two similar
        types apart; hovering shows the full name. At <b>Roomy</b> density names wrap onto more than one
        line instead.</p>
        <p><b>Overview.</b> The small picture in the corner shows the whole graph with a box over what is on
        screen. Click anywhere in it to move the view there.</p>
        <p><b>Unpack a shape.</b> Right-click any node and choose <b>Unpack this group</b> to show that
        shape on its own, at full density, from its starting root to the end of the group. Every other
        type is hidden while it is unpacked. The shape uses the layout chosen in <b>Layout of an
        unpacked shape</b> (Layered Down by default, so each type sits above the types it depends on).
        <b>Re-pack group</b> in the toolbar brings the rest back.</p>
        <p><b>Zoom.</b> The upper-left corner of the canvas holds a region-zoom button (drag a box over
        the shapes you want), a vertical zoom slider, and a reset that returns the normal view.</p>
        <p><b>Click</b> a node to inspect it. <b>Hover</b> a node for its tooltip: what it is, what it
        depends on, and any cleanup findings that point at it. <b>Hover a line</b> for the dependency it
        draws — both types, the kind, the confidence, and the source line that produced it; double-click
        the line to open that code. <b>Double-click</b> a node to open its source.
        <b>Esc</b> clears a focus, a scope, or the right-click menu.</p>
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
    <div class="row inputs">
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
    </div>

    <div class="row checks">
      <label class="check"><input id="hide-orphans" type="checkbox" />Hide unconnected</label>
      <label class="check"><input id="only-problems" type="checkbox" />Only problems</label>
      <label class="check"><input id="dim-selection" type="checkbox" title="Fade everything outside the selected type's neighbourhood" />Dim others</label>
      <label class="check"><input id="group-captions" type="checkbox" title="Name each packed group over the canvas" />Group captions</label>
      <label class="check"><input id="module-frames" type="checkbox" title="Outline each module's region where it is coherent" />Module frames</label>
      <label class="check"><input id="island-tray" type="checkbox" title="Collect the groups with an unlinked view into one band" />Unlinked band</label>
    </div>

    <div class="row actions">
      <button id="visibility-open" type="button" aria-expanded="false"
              title="Choose which node layers and edge kinds are visible">Layers &amp; edges</button>
      <button id="legend-open" type="button" title="What the shapes, colours and lines mean">Legend</button>
      <button id="fit" type="button" title="Frame the visible graph">Fit</button>
      <button id="repack" type="button" hidden
              title="Put every unpacked group back into the packed tiles">Re-pack groups</button>
      <span id="unpack-status" class="unpack-status" hidden></span>
      <button id="toggle-panel" type="button" aria-pressed="false" title="Give the graph the full width">Hide details</button>
      <span class="spacer"></span>
      <button id="reanalyze" type="button" class="primary">Re-analyse</button>
      <button id="export" type="button">Export JSON</button>
      <button id="update-profile" type="button"
              title="Ask the assistant to find conventions this repository's profile does not cover">Update profile</button>
    </div>
  </div>

  ${visibilityFlyout()}

  <div id="main">
    <div id="canvas">
      <div id="cy"></div>
      <div id="view-controls" role="group" aria-label="View controls">
        <button id="zoom-region" type="button" aria-pressed="false"
                title="Drag a box over shapes to zoom to them">
          <svg width="16" height="16" viewBox="0 0 16 16" aria-hidden="true">
            <circle cx="7" cy="7" r="4.6" fill="none" stroke="currentColor" stroke-width="1.5" />
            <path d="M10.6 10.6 L14.2 14.2" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" />
          </svg>
        </button>
        <input id="zoom-slider" type="range" min="0" max="100" step="1" value="50"
               aria-label="Zoom" title="Drag up for closer, down for further away" />
        <button id="view-reset" type="button" title="Reset the view to normal">
          <svg width="16" height="16" viewBox="0 0 16 16" aria-hidden="true">
            <path d="M13 8a5 5 0 1 1-1.6-3.7" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" />
            <path d="M13.4 1.9 L13.4 5.1 L10.2 5.1" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round" />
          </svg>
        </button>
      </div>
      <div id="zoom-rect" hidden></div>
      <div id="minimap" hidden role="img" aria-label="Overview of the whole graph; click to move the view"></div>
      <div id="progress" hidden role="progressbar" aria-valuemin="0" aria-valuemax="100">
        <div class="progress-head">
          <span id="progress-message">Analysing the workspace…</span>
          <span id="progress-percent"></span>
        </div>
        <div class="progress-track"><div id="progress-fill"></div></div>
      </div>
      <span id="focus-bar" hidden></span>
      <span id="stats">analysing…</span>
      <div id="tooltip" hidden role="tooltip"></div>
    </div>

    <div id="side">
      <div id="side-tabs">
        <button id="tab-details" type="button" class="tab active">Details</button>
        <button id="tab-problems" type="button" class="tab">Problems <span id="problem-count" class="badge">0</span></button>
      </div>
      <div id="details" class="pane active"><p class="muted">Select a node to inspect it. Double-click to open the source. Right-click to unpack its shape.</p></div>
      <div id="problems" class="pane">
        <div id="repository-notes" hidden></div>
        <div id="finding-problems"><p class="muted">Run the analysis to see cleanup findings.</p></div>
      </div>
    </div>
  </div>

  ${legendMarkup()}

  <div id="node-menu" class="context-menu" hidden role="menu" aria-label="Node actions"></div>

</div>
<script nonce="${context.nonce}" src="${scriptUri.toString()}"></script>
</body>
</html>`;
}
