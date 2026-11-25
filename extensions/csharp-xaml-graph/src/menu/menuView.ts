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
    const target = command === 'show'
      ? 'csharpXamlGraph.show'
      : command === 'refresh'
        ? 'csharpXamlGraph.refresh'
        : command === 'openSettings'
          ? 'csharpXamlGraph.openSettings'
          : command === 'debug'
            ? 'csharpXamlGraph.debug'
            : 'csharpXamlGraph.export';
    await vscode.commands.executeCommand(target);
  }

  private async save(settings: MenuSettings): Promise<void> {
    const configuration = vscode.workspace.getConfiguration('csharpXamlGraph');
    const target = vscode.workspace.workspaceFolders && vscode.workspace.workspaceFolders.length > 0
      ? vscode.ConfigurationTarget.Workspace
      : vscode.ConfigurationTarget.Global;

    try {
      await configuration.update('include', settings.include, target);
      await configuration.update('exclude', settings.exclude, target);
      await configuration.update('includeExternalTypes', settings.includeExternalTypes, target);
      await configuration.update('maxNodes', settings.maxNodes, target);
      await configuration.update('unpackedLayout', settings.unpackedLayout, target);
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

