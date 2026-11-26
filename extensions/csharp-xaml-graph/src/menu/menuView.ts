import * as vscode from 'vscode';
import {
  DEFAULT_EXCLUDE,
  DEFAULT_INCLUDE,
  makeNonce,
  renderMenuHtml,
  type MenuMessage,
  type MenuSettings,
} from './menuHtml';
import { DEFAULT_GRAPH_SETTINGS, toUnpackedLayout } from '../graph/settings';

/**
 * The panel tab's command names, mapped to the commands they run.
 *
 * The page posts a short name; the extension owns the full id, so a renamed command is one edit here
 * and nothing in the page. An unknown name falls back to export, which is the one action that never
 * surprises the reader.
 */
const COMMANDS: Record<string, string> = {
  show: 'csharpXamlGraph.show',
  refresh: 'csharpXamlGraph.refresh',
  export: 'csharpXamlGraph.export',
  openSettings: 'csharpXamlGraph.openSettings',
  debug: 'csharpXamlGraph.debug',
  profileInitialize: 'csharpXamlGraph.profile.initialize',
  profileImport: 'csharpXamlGraph.profile.import',
};

/** Reads the settings the form shows. Falls back to the contributed defaults. */
export function readSettings(): MenuSettings {
  const configuration = vscode.workspace.getConfiguration('csharpXamlGraph');
  return {
    include: configuration.get<string[]>('include', DEFAULT_INCLUDE),
    exclude: configuration.get<string[]>('exclude', DEFAULT_EXCLUDE),
    includeExternalTypes: configuration.get<boolean>('includeExternalTypes', true),
    maxNodes: configuration.get<number>('maxNodes', 4000),
    unpackedLayout: toUnpackedLayout(configuration.get('unpackedLayout', DEFAULT_GRAPH_SETTINGS.unpackedLayout)),
  };
}

/** Hosts the panel tab. Every action is forwarded to a command, so nothing is duplicated here. */
export class MenuViewProvider implements vscode.WebviewViewProvider {
  static readonly viewId = 'csharpXamlGraph.menu';

  private view: vscode.WebviewView | undefined;

  constructor(private readonly context: vscode.ExtensionContext) {}

  resolveWebviewView(view: vscode.WebviewView): void {
    this.view = view;
    view.webview.options = { enableScripts: true };
    view.webview.html = renderMenuHtml(readSettings(), makeNonce());

    view.webview.onDidReceiveMessage((message: MenuMessage) => this.onMessage(message));
    this.context.subscriptions.push(
      vscode.workspace.onDidChangeConfiguration((event) => {
        if (event.affectsConfiguration('csharpXamlGraph')) {
          this.refresh();
        }
      }),
      view.onDidDispose(() => {
        this.view = undefined;
      }),
    );
  }

  /** Re-reads the settings into the form. */
  refresh(): void {
    if (this.view) {
      this.view.webview.html = renderMenuHtml(readSettings(), makeNonce());
    }
  }

  private async onMessage(message: MenuMessage): Promise<void> {
    switch (message?.type) {
      case 'command':
        await this.runCommand(message.command);
        break;
      case 'save':
        await this.save(message.settings);
        break;
      case 'reset':
        await this.reset();
        break;
      default:
        break;
    }
  }

  private async runCommand(command: string): Promise<void> {
    await vscode.commands.executeCommand(COMMANDS[command] ?? 'csharpXamlGraph.export');
  }

  private async save(settings: MenuSettings): Promise<void> {
    const configuration = vscode.workspace.getConfiguration('csharpXamlGraph');
    const target = vscode.workspace.workspaceFolders && vscode.workspace.workspaceFolders.length > 0
      ? vscode.ConfigurationTarget.Workspace
      : vscode.ConfigurationTarget.Global;

    /**
     * Writes a key only when the reader changed it.
     *
     * `include` and `exclude` are the two lists a profile is allowed to choose, and an explicitly
     * configured pattern wins over it. Saving them on every submit would therefore pin them the
     * first time the reader touched any other field, and silently stop the profile's `files`
     * section from ever applying.
     */
    const writeChanged = async (key: string, value: unknown, fallback: unknown): Promise<void> => {
      if (JSON.stringify(configuration.get(key, fallback)) === JSON.stringify(value)) {
        return;
      }
      await configuration.update(key, value, target);
    };

    try {
      await writeChanged('include', settings.include, DEFAULT_INCLUDE);
      await writeChanged('exclude', settings.exclude, DEFAULT_EXCLUDE);
      await writeChanged('includeExternalTypes', settings.includeExternalTypes, true);
      await writeChanged('maxNodes', settings.maxNodes, 4000);
      await writeChanged('unpackedLayout', settings.unpackedLayout, DEFAULT_GRAPH_SETTINGS.unpackedLayout);
      await this.view?.webview.postMessage({ type: 'saved', settings: readSettings() });
    } catch (error) {
      const text = error instanceof Error ? error.message : String(error);
      await this.view?.webview.postMessage({ type: 'failed', message: text });
    }
  }

  private async reset(): Promise<void> {
    const configuration = vscode.workspace.getConfiguration('csharpXamlGraph');
    const target = vscode.workspace.workspaceFolders && vscode.workspace.workspaceFolders.length > 0
      ? vscode.ConfigurationTarget.Workspace
      : vscode.ConfigurationTarget.Global;

    await configuration.update('include', undefined, target);
    await configuration.update('exclude', undefined, target);
    await configuration.update('includeExternalTypes', undefined, target);
    await configuration.update('maxNodes', undefined, target);
    await configuration.update('unpackedLayout', undefined, target);
    await this.view?.webview.postMessage({ type: 'saved', settings: readSettings() });
  }
}

