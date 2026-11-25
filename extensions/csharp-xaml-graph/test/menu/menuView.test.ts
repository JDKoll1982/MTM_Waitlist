import { beforeEach, describe, expect, it } from 'vitest';
import { MenuViewProvider, readSettings } from '../../src/menu/menuView';
import { makeContext, resetVscodeState, state } from '../support/vscode';

interface FakeView {
  webview: {
    options: Record<string, unknown>;
    html: string;
    posted: unknown[];
    postMessage: (message: unknown) => Promise<boolean>;
    onDidReceiveMessage: (callback: (message: unknown) => unknown) => { dispose: () => void };
  };
  onDidDispose: (callback: () => void) => { dispose: () => void };
  dispose: () => void;
  send: (message: unknown) => Promise<void>;
  disposeListeners: (() => void)[];
}

function fakeView(): FakeView {
  const listeners: ((message: unknown) => unknown)[] = [];
  const disposeListeners: (() => void)[] = [];
  return {
    webview: {
      options: {},
      html: '',
      posted: [],
      async postMessage(message: unknown) {
        view.webview.posted.push(message);
        return true;
      },
      onDidReceiveMessage(callback: (message: unknown) => unknown) {
        listeners.push(callback);
        return { dispose: () => undefined };
      },
    },
    onDidDispose(callback: () => void) {
      disposeListeners.push(callback);
      return { dispose: () => undefined };
    },
    dispose() {
      for (const listener of disposeListeners) {
        listener();
      }
    },
    disposeListeners,
    async send(message: unknown) {
      for (const listener of listeners) {
        await listener(message);
      }
    },
  } as FakeView;
}

let view: FakeView;
let provider: MenuViewProvider;

const open = () => {
  view = fakeView();
  provider = new MenuViewProvider(makeContext('/ext') as never);
  provider.resolveWebviewView(view as never);
  return view;
};

beforeEach(() => {
  resetVscodeState();
});

describe('readSettings', () => {
  it('falls back to the contributed defaults', () => {
    expect(readSettings()).toEqual({
      include: ['**/*.cs', '**/*.xaml'],
      exclude: ['**/bin/**', '**/obj/**', '**/node_modules/**', '**/*.Tests/**', '**/.github/**', '**/.specify/**'],
      includeExternalTypes: true,
      maxNodes: 4000,
      unpackedLayout: 'layered-tb',
    });
  });

  it('reads the configured values and normalises an unknown layout', () => {
    state.configValues['csharpXamlGraph.include'] = ['src/**/*.cs'];
    state.configValues['csharpXamlGraph.exclude'] = [];
    state.configValues['csharpXamlGraph.includeExternalTypes'] = false;
    state.configValues['csharpXamlGraph.maxNodes'] = 900;
    state.configValues['csharpXamlGraph.unpackedLayout'] = 'banana';

    expect(readSettings()).toEqual({
      include: ['src/**/*.cs'],
      exclude: [],
      includeExternalTypes: false,
      maxNodes: 900,
      unpackedLayout: 'layered-tb',
    });
  });

  it('keeps a layout the extension knows', () => {
    state.configValues['csharpXamlGraph.unpackedLayout'] = 'grid';
    expect(readSettings().unpackedLayout).toBe('grid');
  });
});

describe('MenuViewProvider: the view', () => {
  it('is registered under the view id the manifest contributes', () => {
    expect(MenuViewProvider.viewId).toBe('csharpXamlGraph.menu');
  });

  it('enables scripts and renders the page into the view', () => {
    const created = open();
    expect(created.webview.options).toEqual({ enableScripts: true });
    expect(created.webview.html).toContain('<h1>C#/XAML Dependency Graph</h1>');
  });

  it('re-renders when the configuration changes', () => {
    open();
    const first = view.webview.html;
    state.configValues['csharpXamlGraph.maxNodes'] = 250;
    for (const listener of [...state.configurationListeners]) {
      listener({ affectsConfiguration: (section) => section === 'csharpXamlGraph' });
    }
    expect(view.webview.html).not.toBe(first);
    expect(view.webview.html).toContain('value="250"');
  });

  it('ignores a configuration change outside the extension', () => {
    open();
    const first = view.webview.html;
    state.configValues['csharpXamlGraph.maxNodes'] = 250;
    for (const listener of [...state.configurationListeners]) {
      listener({ affectsConfiguration: () => false });
    }
    expect(view.webview.html).toBe(first);
  });

  it('does nothing when refreshed without a view', () => {
    const lone = new MenuViewProvider(makeContext('/ext') as never);
    expect(() => lone.refresh()).not.toThrow();
  });

  it('forgets the view when it is disposed', () => {
    open();
    view.dispose();
    const before = view.webview.html;
    provider.refresh();
    expect(view.webview.html).toBe(before);
  });
});

describe('MenuViewProvider: commands and settings messages', () => {
  it('maps every shortcut to the command it runs', async () => {
    open();
    const cases: [string, string][] = [
      ['show', 'csharpXamlGraph.show'],
      ['refresh', 'csharpXamlGraph.refresh'],
      ['openSettings', 'csharpXamlGraph.openSettings'],
      ['debug', 'csharpXamlGraph.debug'],
      ['export', 'csharpXamlGraph.export'],
      ['anythingElse', 'csharpXamlGraph.export'],
    ];
    for (const [command, expected] of cases) {
      await view.send({ type: 'command', command });
      expect(state.executedCommands.at(-1)?.command).toBe(expected);
    }
  });

  it('saves every field to the workspace scope and confirms', async () => {
    state.folders = [{ uri: { scheme: 'file', path: '/work' } as never, name: 'work', index: 0 }];
    open();
    await view.send({
      type: 'save',
      settings: {
        include: ['a.cs'],
        exclude: [],
        includeExternalTypes: false,
        maxNodes: 123,
        unpackedLayout: 'grid',
      },
    });

    expect(state.configUpdates.map((update) => `${update.key}=${String(update.value)}@${String(update.target)}`)).toEqual([
      'include=a.cs@2',
      'exclude=@2',
      'includeExternalTypes=false@2',
      'maxNodes=123@2',
      'unpackedLayout=grid@2',
    ]);
    expect(view.webview.posted.at(-1)).toMatchObject({ type: 'saved' });
  });

  it('saves to the user scope when no folder is open', async () => {
    open();
    await view.send({
      type: 'save',
      settings: { include: [], exclude: [], includeExternalTypes: true, maxNodes: 1, unpackedLayout: 'force' },
    });
    expect(state.configUpdates.every((update) => update.target === 1)).toBe(true);
  });

  it('reports a save failure to the page instead of throwing', async () => {
    state.updateError = new Error('read-only settings');
    open();
    await view.send({
      type: 'save',
      settings: { include: [], exclude: [], includeExternalTypes: true, maxNodes: 1, unpackedLayout: 'force' },
    });
    expect(view.webview.posted.at(-1)).toEqual({ type: 'failed', message: 'read-only settings' });
  });

  it('reports a non-Error save failure as text', async () => {
    state.updateError = 'nope' as unknown as Error;
    open();
    await view.send({
      type: 'save',
      settings: { include: [], exclude: [], includeExternalTypes: true, maxNodes: 1, unpackedLayout: 'force' },
    });
    expect(view.webview.posted.at(-1)).toEqual({ type: 'failed', message: 'nope' });
  });

  it('resets every key to its default and confirms', async () => {
    state.configValues['csharpXamlGraph.maxNodes'] = 5;
    open();
    await view.send({ type: 'reset' });

    expect(state.configUpdates.map((update) => `${update.key}=${String(update.value)}`)).toEqual([
      'include=undefined',
      'exclude=undefined',
      'includeExternalTypes=undefined',
      'maxNodes=undefined',
      'unpackedLayout=undefined',
    ]);
    expect(view.webview.posted.at(-1)).toMatchObject({ type: 'saved' });
  });

  it('ignores an unknown message', async () => {
    open();
    await view.send({ type: 'mystery' });
    await view.send(undefined);
    expect(view.webview.posted).toEqual([]);
  });
});

describe('MenuViewProvider: reset without a workspace', () => {
  it('resets to the user scope when no folder is open', async () => {
    open();
    await view.send({ type: 'reset' });
    expect(state.configUpdates.length).toBe(5);
    expect(state.configUpdates.every((update) => update.target === 1)).toBe(true);
  });
});

describe('MenuViewProvider: reset into the workspace scope', () => {
  it('resets the workspace values when a folder is open', async () => {
    state.folders = [{ uri: { scheme: 'file', path: '/work' } as never, name: 'work', index: 0 }];
    open();
    await view.send({ type: 'reset' });
    expect(state.configUpdates.every((update) => update.target === 2)).toBe(true);
  });
});
