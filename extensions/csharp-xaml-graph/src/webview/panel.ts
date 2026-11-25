import * as vscode from 'vscode';
import type { GraphModel } from '../graph/model';
import { makeNonce, renderHtml } from './html';

export type ModelLoader = () => PromiseLike<GraphModel>;

export class GraphPanel {
  private static current: GraphPanel | undefined;

  private readonly panel: vscode.WebviewPanel;

  private model: GraphModel | undefined;

  private busy = false;

  private constructor(
    panel: vscode.WebviewPanel,
    private readonly context: vscode.ExtensionContext,
    private readonly loader: ModelLoader,
  ) {
    this.panel = panel;

    this.panel.webview.html = renderHtml({
      webview: this.panel.webview,
      scriptUri: this.panel.webview.asWebviewUri(
        vscode.Uri.joinPath(this.context.extensionUri, 'out', 'webview', 'main.js'),
      ),
      styleUri: this.panel.webview.asWebviewUri(
        vscode.Uri.joinPath(this.context.extensionUri, 'out', 'webview', 'style.css'),
      ),
      nonce: makeNonce(),
    });

    this.panel.webview.onDidReceiveMessage(
      (message: { type?: string; file?: string; line?: number }) => this.onMessage(message),
      undefined,
      this.context.subscriptions,
    );

    this.panel.onDidDispose(() => {
      GraphPanel.current = undefined;
    }, undefined, this.context.subscriptions);
  }

  static show(context: vscode.ExtensionContext, loader: ModelLoader): GraphPanel {
    if (GraphPanel.current) {
      GraphPanel.current.panel.reveal(undefined, false);
      return GraphPanel.current;
    }

    const panel = vscode.window.createWebviewPanel(
      'csharpXamlGraph',
      'C#/XAML Dependency Graph',
      vscode.ViewColumn.Active,
      {
        enableScripts: true,
        retainContextWhenHidden: true,
        localResourceRoots: [vscode.Uri.joinPath(context.extensionUri, 'out')],
      },
    );

    GraphPanel.current = new GraphPanel(panel, context, loader);
    return GraphPanel.current;
  }

  async refresh(): Promise<void> {
    if (this.busy) {
      return;
    }
    this.busy = true;

    try {
      this.model = await this.loader();
      await this.panel.webview.postMessage({ type: 'model', model: this.model });
    } catch (error) {
      const message = error instanceof Error ? error.message : String(error);
      await this.panel.webview.postMessage({ type: 'error', message });
      void vscode.window.showErrorMessage(`Dependency graph analysis failed: ${message}`);
    } finally {
      this.busy = false;
    }
  }

  private async onMessage(message: { type?: string; file?: string; line?: number }): Promise<void> {
    switch (message?.type) {
      case 'ready':
        if (this.model) {
          await this.panel.webview.postMessage({ type: 'model', model: this.model });
        } else {
          await this.refresh();
        }
        break;

      case 'reanalyze':
        await this.refresh();
        break;

      case 'export':
        await this.exportJson();
        break;

      case 'reveal':
        await GraphPanel.reveal(message.file, message.line);
        break;

      default:
        break;
    }
  }

  private async exportJson(): Promise<void> {
    if (!this.model) {
      void vscode.window.showWarningMessage('Nothing to export yet.');
      return;
    }

    const target = await vscode.window.showSaveDialog({
      filters: { JSON: ['json'] },
      saveLabel: 'Export graph',
      defaultUri: vscode.Uri.joinPath(this.context.extensionUri, '..', 'dependency-graph.json'),
    });

    if (!target) {
      return;
    }

    const payload = Buffer.from(JSON.stringify(this.model, null, 2), 'utf8');
    await vscode.workspace.fs.writeFile(target, payload);
    void vscode.window.showInformationMessage(`Graph exported to ${target.fsPath}`);
  }

  private static async reveal(file: string | undefined, line: number | undefined): Promise<void> {
    const folder = vscode.workspace.workspaceFolders?.[0];
    if (!folder || !file) {
      return;
    }

    const uri = vscode.Uri.joinPath(folder.uri, ...file.split('/'));
    try {
      const document = await vscode.workspace.openTextDocument(uri);
      const position = new vscode.Position(Math.max(0, (line ?? 1) - 1), 0);
      await vscode.window.showTextDocument(document, {
        viewColumn: vscode.ViewColumn.Beside,
        selection: new vscode.Range(position, position),
        preview: true,
      });
    } catch (error) {
      const message = error instanceof Error ? error.message : String(error);
      void vscode.window.showErrorMessage(`Could not open ${file}: ${message}`);
    }
  }
}
