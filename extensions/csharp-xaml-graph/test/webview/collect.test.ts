import { readFileSync, statSync } from 'node:fs';
import { join, resolve } from 'node:path';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import type { DebugSection, HostSnapshot, WebviewFacts } from '../../src/debug/model';
import { collectHostSnapshot, withHost } from '../../src/debug/collect';
import { buildSections } from '../../src/debug/report';
import { env as hostEnv, makeContext, resetVscodeState, state, Uri, workspace } from '../support/vscode';

/* -------------------------------------------------------------- fixtures */

/** The extension folder itself, which is the one path on disk every checkout carries. */
const extensionRoot = resolve((import.meta as unknown as { dirname: string }).dirname, '..', '..');

/** A path no checkout carries, so every asset read under it misses. */
const EXTENSION_PATH = 'C:\\ext\\csharp-xaml-graph-does-not-exist';

const diskVersion = (): string | undefined =>
  (JSON.parse(readFileSync(join(extensionRoot, 'package.json'), 'utf8')) as { version?: string }).version;

const diskSize = (relative: string): number => statSync(join(extensionRoot, ...relative.split('/'))).size;

const ASSET_PATHS = [
  'package.json',
  'out/extension.js',
  'out/webview/main.js',
  'out/webview/style.css',
  'media/tree-sitter-c_sharp.wasm',
  'node_modules/web-tree-sitter/web-tree-sitter.wasm',
];

function manifest(): Record<string, unknown> {
  return {
    version: '0.3.2',
    activationEvents: ['onCommand:csharpXamlGraph.show', 'onView:csharpXamlGraph.menu'],
    contributes: {
      commands: [
        { command: 'csharpXamlGraph.show' },
        { command: 'csharpXamlGraph.debug' },
        { title: 'a command entry with no command' },
      ],
      menus: {
        'editor/context': [{ command: 'csharpXamlGraph.showForSelection' }, { submenu: 'graph' }, {}],
        'explorer/context': [{ command: 'csharpXamlGraph.showForFile' }],
        'commandPalette': [{}, { when: 'resourceExtname == .cs' }],
      },
      views: {
        csharpXamlGraphPanel: [
          { id: 'csharpXamlGraph.other' },
          { id: 'csharpXamlGraph.menu', type: 'webview' },
          {},
        ],
      },
      configuration: {
        properties: {
          maxNodes: { type: 'number' },
          include: { type: 'array' },
          exclude: { type: 'array' },
          includeExternalTypes: { type: 'boolean' },
        },
      },
    },
  };
}

const webviewFacts: WebviewFacts = {
  ready: true,
  modelLoadedAt: null,
  nodesDrawn: 3,
  edgesDrawn: 2,
  hiddenOrphans: 0,
  layout: 'layered-tb',
  density: 'normal',
  labels: 'zoom',
  zoom: 1,
  canvasWidth: 800,
  canvasHeight: 600,
  module: 'all',
  search: '',
  layers: [],
  edgeKinds: [],
  focus: null,
  focusDepth: 1,
  scope: null,
  scopeExact: false,
  unpacked: [],
  history: [],
};

const context = (extensionPath: string = EXTENSION_PATH) => makeContext(extensionPath) as never;

const sectionsFor = (snapshot: HostSnapshot): DebugSection[] =>
  buildSections(withHost(snapshot, { cacheInvalidated: false, registeredCommands: [] }));

function row(sections: DebugSection[], id: string, label: string) {
  const section = sections.find((entry) => entry.id === id);
  expect(section, `section ${id} is missing`).toBeDefined();
  const found = section!.rows.find((entry) => entry.label === label);
  expect(found, `row "${label}" is missing from ${id}`).toBeDefined();
  return found!;
}

function setting(snapshot: HostSnapshot, key: string) {
  const found = snapshot.settings.find((entry) => entry.key === key);
  expect(found, `setting ${key} is missing`).toBeDefined();
  return found!;
}

beforeEach(() => {
  resetVscodeState();
  state.packageJSON = manifest();
});

afterEach(() => {
  vi.restoreAllMocks();
  hostEnv.appHost = 'desktop';
  hostEnv.remoteName = undefined;
  hostEnv.language = 'en';
  (hostEnv as unknown as { uiKind: number }).uiKind = 1;
});

/* -------------------------------------------------------------- settings */

describe('collectHostSnapshot — settings', () => {
  it('reports each contributed setting sorted, with the value in force and where it came from', async () => {
    state.inspected = {
      'csharpXamlGraph.exclude': { defaultValue: ['**/obj/**'], globalValue: ['**/obj/**', '**/dist/**'] },
      'csharpXamlGraph.include': { defaultValue: ['**/*.cs'], workspaceValue: ['src/**/*.cs'] },
      'csharpXamlGraph.includeExternalTypes': { defaultValue: true },
      'csharpXamlGraph.maxNodes': {
        defaultValue: 4000,
        globalValue: 3000,
        workspaceValue: 3500,
        workspaceFolderValue: 2500,
      },
    };

    const snapshot = await collectHostSnapshot(context());

    expect(snapshot.settings).toEqual([
      { key: 'exclude', value: '**/obj/**, **/dist/**', source: 'user' },
      { key: 'include', value: 'src/**/*.cs', source: 'workspace' },
      { key: 'includeExternalTypes', value: 'true', source: 'default' },
      { key: 'maxNodes', value: '2500', source: 'folder' },
    ]);
    expect(row(sectionsFor(snapshot), 'settings', 'include')).toEqual({
      label: 'include',
      value: 'src/**/*.cs  (workspace)',
      tone: 'plain',
    });
  });

  it('reports a value that matches the schema default as set, not as default', async () => {
    state.inspected['csharpXamlGraph.include'] = {
      defaultValue: ['**/*.cs'],
      workspaceValue: ['**/*.cs'],
    };

    const snapshot = await collectHostSnapshot(context());

    expect(setting(snapshot, 'include')).toEqual({ key: 'include', value: '**/*.cs', source: 'workspace' });
  });

  it('reports the default source and (unset) when no scope sets a value', async () => {
    state.inspected['csharpXamlGraph.maxNodes'] = { defaultValue: 4000 };

    const snapshot = await collectHostSnapshot(context());

    expect(setting(snapshot, 'maxNodes')).toEqual({ key: 'maxNodes', value: '4000', source: 'default' });
    expect(setting(snapshot, 'include')).toEqual({ key: 'include', value: '(unset)', source: 'default' });
    expect(snapshot.settings.map((entry) => entry.source)).toEqual(['default', 'default', 'default', 'default']);
  });

  it('renders an empty array, a list, null and an object the way the report reads them', async () => {
    state.inspected = {
      'csharpXamlGraph.exclude': { workspaceValue: [] },
      'csharpXamlGraph.include': { workspaceValue: ['**/*.cs', '**/*.xaml'] },
      'csharpXamlGraph.includeExternalTypes': { workspaceValue: null },
      'csharpXamlGraph.maxNodes': { workspaceValue: { cap: 10 } },
    };

    const snapshot = await collectHostSnapshot(context());

    expect(snapshot.settings.map((entry) => entry.value)).toEqual([
      '(empty)',
      '**/*.cs, **/*.xaml',
      'null',
      '[object Object]',
    ]);
  });

  it('falls back to the plain read when inspecting a setting throws', async () => {
    const get = vi.fn((key: string) => `read:${key}`);
    vi.spyOn(workspace, 'getConfiguration').mockReturnValue({
      inspect(): never {
        throw new Error('inspect unavailable');
      },
      get,
    } as never);

    const snapshot = await collectHostSnapshot(context());

    expect(setting(snapshot, 'exclude')).toEqual({ key: 'exclude', value: 'read:exclude', source: 'default' });
    expect(get).toHaveBeenCalledWith('exclude');
  });

  it('records every setting as unavailable when the configuration cannot be read, and still reports the rest', async () => {
    vi.spyOn(workspace, 'getConfiguration').mockImplementation(() => {
      throw new Error('no configuration');
    });

    const snapshot = await collectHostSnapshot(context());

    expect(snapshot.settings).toHaveLength(4);
    expect(snapshot.settings.every((entry) => entry.value === 'unavailable' && entry.source === 'unknown')).toBe(true);
    expect(row(sectionsFor(snapshot), 'settings', 'include').value).toBe('unavailable  (unknown)');
    expect(snapshot.contributedCommands).toEqual(['csharpXamlGraph.debug', 'csharpXamlGraph.show']);
    expect(snapshot.activationEvents).toHaveLength(2);
  });

  it('says the manifest contributes no settings rather than showing an empty section', async () => {
    state.packageJSON = { version: '1.0.0', contributes: {} };

    const snapshot = await collectHostSnapshot(context());

    expect(snapshot.settings).toEqual([]);
    expect(row(sectionsFor(snapshot), 'settings', 'Settings')).toEqual({
      label: 'Settings',
      value: 'empty — the manifest contributes no settings',
      tone: 'warn',
    });
  });
});

/* ------------------------------------------------- commands, menus, views */

describe('collectHostSnapshot — commands, menus and views', () => {
  it('reports the contributed commands sorted, dropping an entry with no command', async () => {
    const snapshot = await collectHostSnapshot(context());

    expect(snapshot.contributedCommands).toEqual(['csharpXamlGraph.debug', 'csharpXamlGraph.show']);
    expect(row(sectionsFor(snapshot), 'commands', 'Contributed').value).toBe('2');
  });

  it('reports a contributed command that activation never registered', async () => {
    const snapshot = await collectHostSnapshot(context());
    const rows = buildSections(withHost(snapshot, {
      cacheInvalidated: false,
      registeredCommands: ['csharpXamlGraph.show'],
    }));

    expect(row(rows, 'commands', 'Contributed but not registered')).toEqual({
      label: 'Contributed but not registered',
      value: 'csharpXamlGraph.debug',
      tone: 'bad',
    });
    expect(row(rows, 'commands', 'Registered but not contributed')).toEqual({
      label: 'Registered but not contributed',
      value: 'none',
      tone: 'good',
    });
  });

  it('reports a registered command the manifest never contributed', async () => {
    const snapshot = await collectHostSnapshot(context());
    const rows = buildSections(withHost(snapshot, {
      cacheInvalidated: false,
      registeredCommands: ['csharpXamlGraph.debug', 'csharpXamlGraph.show', 'csharpXamlGraph.mystery'],
    }));

    expect(row(rows, 'commands', 'Contributed but not registered')).toEqual({
      label: 'Contributed but not registered',
      value: 'none',
      tone: 'good',
    });
    expect(row(rows, 'commands', 'Registered but not contributed')).toEqual({
      label: 'Registered but not contributed',
      value: 'csharpXamlGraph.mystery',
      tone: 'warn',
    });
  });

  it('reports menu contributions in menu order, keeping submenus and dropping empty menus', async () => {
    const snapshot = await collectHostSnapshot(context());

    expect(snapshot.contributedMenus).toEqual([
      { menu: 'editor/context', commands: ['csharpXamlGraph.showForSelection', 'submenu:graph'] },
      { menu: 'explorer/context', commands: ['csharpXamlGraph.showForFile'] },
    ]);
    expect(row(sectionsFor(snapshot), 'commands', 'Menus').value).toBe(
      'editor/context: csharpXamlGraph.showForSelection, submenu:graph | '
        + 'explorer/context: csharpXamlGraph.showForFile',
    );
  });

  it('flattens the contributed views across containers, sorting them and falling back to an unknown type', async () => {
    const snapshot = await collectHostSnapshot(context());

    expect(snapshot.views).toEqual([
      { id: 'csharpXamlGraph.menu', type: 'webview' },
      { id: 'csharpXamlGraph.other', type: 'unknown' },
    ]);
    expect(row(sectionsFor(snapshot), 'commands', 'Views').value).toBe(
      'csharpXamlGraph.menu (webview), csharpXamlGraph.other (unknown)',
    );
  });

  it('copies the activation events so a reader cannot change the manifest through the snapshot', async () => {
    const snapshot = await collectHostSnapshot(context());

    expect(snapshot.activationEvents).toEqual(['onCommand:csharpXamlGraph.show', 'onView:csharpXamlGraph.menu']);
    expect(snapshot.activationEvents).not.toBe(state.packageJSON.activationEvents);

    snapshot.activationEvents.push('onStartupFinished');

    expect(state.packageJSON.activationEvents).toEqual([
      'onCommand:csharpXamlGraph.show',
      'onView:csharpXamlGraph.menu',
    ]);
  });
});

/* ------------------------------------------------------------------ host */

describe('collectHostSnapshot — host facts', () => {
  it('reports the window, the build and the folders with forward-slash paths', async () => {
    state.folders = [
      { uri: new Uri('file', 'C:\\ws\\app'), name: 'app', index: 0 },
      { uri: Uri.file('C:/ws/two'), name: 'two', index: 1 },
    ];
    hostEnv.remoteName = 'ssh-remote+plant';

    const snapshot = await collectHostSnapshot(context());

    expect(snapshot.host).toEqual({
      extensionVersion: '0.3.2',
      vscodeVersion: '1.99.0-test',
      extensionMode: 'development',
      appHost: 'desktop',
      uiKind: 'Desktop',
      remoteName: 'ssh-remote+plant',
      isTrusted: true,
      platform: process.platform,
      arch: process.arch,
      nodeVersion: process.versions.node,
      locale: 'en',
      extensionPath: 'C:/ext/csharp-xaml-graph-does-not-exist',
      workspaceFolders: ['C:/ws/app', 'C:/ws/two'],
    });

    const rows = sectionsFor(snapshot);
    expect(row(rows, 'host', 'Remote').value).toBe('ssh-remote+plant');
    expect(row(rows, 'host', 'Workspace folders')).toEqual({
      label: 'Workspace folders',
      value: 'C:/ws/app, C:/ws/two',
      tone: 'plain',
    });
    expect(row(rows, 'host', 'Workspace trusted').value).toBe('yes');
  });

  it('warns about an untrusted window, a web client and no open folder', async () => {
    state.isTrusted = false;
    (hostEnv as unknown as { uiKind: number }).uiKind = 2;

    const snapshot = await collectHostSnapshot(context());

    expect(snapshot.host.isTrusted).toBe(false);
    expect(snapshot.host.uiKind).toBe('Web');
    expect(snapshot.host.remoteName).toBe('');
    expect(snapshot.host.workspaceFolders).toEqual([]);

    const rows = sectionsFor(snapshot);
    expect(row(rows, 'host', 'Workspace trusted')).toEqual({
      label: 'Workspace trusted',
      value: 'no',
      tone: 'warn',
    });
    expect(row(rows, 'host', 'Workspace folders')).toEqual({
      label: 'Workspace folders',
      value: 'none open',
      tone: 'warn',
    });
    expect(row(rows, 'host', 'Remote')).toEqual({ label: 'Remote', value: 'none' });
  });

  it('reports the extension mode the context carries', async () => {
    state.extensionMode = 3;

    const snapshot = await collectHostSnapshot(context());

    expect(snapshot.host.extensionMode).toBe('test');
    expect(row(sectionsFor(snapshot), 'host', 'Extension mode')).toEqual({
      label: 'Extension mode',
      value: 'test',
      // A non-production run is what a maintainer needs to notice first.
      tone: 'good',
    });
  });

  it('reports an unknown host and locale when the API does not answer', async () => {
    delete (hostEnv as { appHost?: string }).appHost;
    delete (hostEnv as { language?: string }).language;

    const snapshot = await collectHostSnapshot(context());

    expect(snapshot.host.appHost).toBe('unknown');
    expect(snapshot.host.locale).toBe('unknown');
    expect(row(sectionsFor(snapshot), 'host', 'Host').value).toBe('unknown');
    expect(row(sectionsFor(snapshot), 'host', 'Locale').value).toBe('unknown');
  });

  it('reports the manifest version even when package.json on disk says something else', async () => {
    const manifestVersion = '0.0.1-from-the-manifest';
    state.packageJSON = { ...manifest(), version: manifestVersion };
    expect(diskVersion()).not.toBe(manifestVersion);

    const snapshot = await collectHostSnapshot(context(extensionRoot));

    expect(snapshot.host.extensionVersion).toBe(manifestVersion);
  });

  it('falls back to the package.json on disk when the manifest states no version', async () => {
    state.packageJSON = { activationEvents: [] };
    const onDisk = diskVersion();
    expect(onDisk).toBeTruthy();

    const snapshot = await collectHostSnapshot(context(extensionRoot));

    expect(snapshot.host.extensionVersion).toBe(onDisk);
  });

  it('reports an unknown version when there is no package.json to read', async () => {
    state.packageJSON = { activationEvents: [] };

    const snapshot = await collectHostSnapshot(context());

    expect(snapshot.host.extensionVersion).toBe('unknown');
    expect(row(sectionsFor(snapshot), 'host', 'Extension version').value).toBe('unknown');
  });

  it('reports empty facts when the context carries no manifest', async () => {
    const bare = { ...makeContext('/ext/none'), extension: undefined } as never;

    const snapshot = await collectHostSnapshot(bare);

    expect(snapshot.host.extensionVersion).toBe('unknown');
    expect(snapshot.settings).toEqual([]);
    expect(snapshot.contributedCommands).toEqual([]);
    expect(snapshot.contributedMenus).toEqual([]);
    expect(snapshot.views).toEqual([]);
    expect(snapshot.activationEvents).toEqual([]);
    expect(snapshot.assets.every((asset) => asset.exists === false)).toBe(true);
    expect(buildSections(withHost(snapshot, { cacheInvalidated: false, registeredCommands: [] }))).toHaveLength(8);
  });
});

/* ---------------------------------------------------------------- assets */

describe('collectHostSnapshot — assets', () => {
  it('reports every asset in page order and the size of the ones it can see', async () => {
    const snapshot = await collectHostSnapshot(context(extensionRoot));

    expect(snapshot.assets.map((asset) => asset.path)).toEqual(ASSET_PATHS);
    expect(snapshot.assets[0]).toEqual({
      path: 'package.json',
      exists: true,
      bytes: diskSize('package.json'),
    });

    const present = row(sectionsFor(snapshot), 'packaging', 'package.json');
    expect(present.tone).toBe('good');
    expect(present.value).toMatch(/^present \(\d+ KB\)$/);
  });

  it('marks every asset as missing and says to rebuild when nothing is on disk', async () => {
    const snapshot = await collectHostSnapshot(context());

    expect(snapshot.assets).toEqual(ASSET_PATHS.map((path) => ({ path, exists: false })));

    const rows = sectionsFor(snapshot);
    expect(row(rows, 'packaging', 'media/tree-sitter-c_sharp.wasm')).toEqual({
      label: 'media/tree-sitter-c_sharp.wasm',
      value: 'MISSING — media/tree-sitter-c_sharp.wasm is not on disk',
      tone: 'bad',
    });
    expect(row(rows, 'packaging', 'Bundle health')).toEqual({
      label: 'Bundle health',
      value: '6 asset(s) missing — rebuild before packaging',
      tone: 'bad',
    });
  });
});

/* --------------------------------------------------------------- failure */

describe('collectHostSnapshot — failure containment', () => {
  it('does not guard a host API that throws outside a settings read, so the whole collection rejects', async () => {
    vi.spyOn(workspace, 'workspaceFolders', 'get').mockImplementation(() => {
      throw new Error('workspace unavailable');
    });

    await expect(collectHostSnapshot(context())).rejects.toThrow('workspace unavailable');
  });
});

/* -------------------------------------------------------------- assembly */

describe('collectHostSnapshot — assembly', () => {
  it('assembles every host fact the report needs', async () => {
    const snapshot = await collectHostSnapshot(context());

    expect(Object.keys(snapshot).sort()).toEqual([
      'activationEvents',
      'assets',
      'contributedCommands',
      'contributedMenus',
      'host',
      'settings',
      'views',
    ]);
    expect(sectionsFor(snapshot).map((section) => section.id)).toEqual([
      'host',
      'settings',
      'analysis',
      'parsers',
      'findings',
      'canvas',
      'commands',
      'packaging',
    ]);
  });
});

/* --------------------------------------------------------------- withHost */

describe('withHost', () => {
  const extra = () => ({
    modelAgeMs: 1500,
    cacheInvalidated: true,
    webview: webviewFacts,
    registeredCommands: ['csharpXamlGraph.show'],
  });

  it('carries both halves into one input for the report', async () => {
    const snapshot = await collectHostSnapshot(context());

    const merged = withHost(snapshot, extra());

    expect(merged).toMatchObject({
      ...snapshot,
      modelAgeMs: 1500,
      cacheInvalidated: true,
      webview: webviewFacts,
      registeredCommands: ['csharpXamlGraph.show'],
    });
    expect(merged.model).toBeUndefined();

    const rows = buildSections(merged);
    expect(row(rows, 'canvas', 'Drawn').value).toBe('3 nodes, 2 edges');
    expect(row(rows, 'analysis', 'Cache')).toEqual({
      label: 'Cache',
      value: 'invalidated — next open re-reads',
      tone: 'warn',
    });
    expect(row(rows, 'commands', 'Registered').value).toBe('1');
  });

  it('leaves the snapshot it was handed untouched', async () => {
    const snapshot = await collectHostSnapshot(context());
    const before = JSON.parse(JSON.stringify(snapshot)) as HostSnapshot;

    withHost(snapshot, extra());

    expect(snapshot).toEqual(before);
  });

  it('lets the analysis half win on a key collision', async () => {
    const snapshot = await collectHostSnapshot(context());
    const clashing = extra() as Record<string, unknown>;
    clashing.host = { extensionVersion: 'from-the-analysis-half' };

    const merged = withHost(snapshot, clashing as never);

    expect(merged.host.extensionVersion).toBe('from-the-analysis-half');
  });
});

/* -------------------------------------------------------------- regressions */

describe('collectHostSnapshot — the shipped manifest', () => {
  it('reads a fully qualified property key from the section it belongs to', async () => {
    // package.json declares `csharpXamlGraph.maxNodes`, not `maxNodes`. Asking the open section for
    // the qualified name resolved nothing and reported every setting as its default.
    const shipped = manifest() as {
      contributes: { configuration: { properties: Record<string, unknown> } };
    };
    shipped.contributes.configuration.properties = {
      'csharpXamlGraph.maxNodes': { type: 'number' },
      'csharpXamlGraph.include': { type: 'array' },
    };
    state.packageJSON = shipped;
    state.inspected['csharpXamlGraph.maxNodes'] = { defaultValue: 4000, workspaceValue: 1234 };

    const snapshot = await collectHostSnapshot(context());

    expect(snapshot.settings).toEqual([
      { key: 'include', value: '(unset)', source: 'default' },
      { key: 'maxNodes', value: '1234', source: 'workspace' },
    ]);
  });

  it('reports the mode the extension is actually running in', async () => {
    // The enum object was compared to its own members, so every run reported production.
    state.extensionMode = 2;
    expect((await collectHostSnapshot(context())).host.extensionMode).toBe('development');

    state.extensionMode = 3;
    expect((await collectHostSnapshot(context())).host.extensionMode).toBe('test');

    state.extensionMode = 1;
    expect((await collectHostSnapshot(context())).host.extensionMode).toBe('production');
  });
});
