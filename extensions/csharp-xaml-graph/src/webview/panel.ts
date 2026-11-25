import * as vscode from 'vscode';
import type { GraphModel } from '../graph/model';
import type { GraphRequest } from '../graph/request';
import { DEFAULT_GRAPH_SETTINGS, toUnpackedLayout, type GraphSettings } from '../graph/settings';
import { makeNonce, renderHtml } from './html';

export type ModelLoader = (force?: boolean) => PromiseLike<GraphModel>;

/** The settings the panel acts on. Read by the extension, applied by the webview. */
export function readGraphSettings(): GraphSettings {
  const configuration = vscode.workspace.getConfiguration('csharpXamlGraph');
  return {
    unpackedLayout: toUnpackedLayout(configuration.get('unpackedLayout', DEFAULT_GRAPH_SETTINGS.unpackedLayout)),
  };
}

interface PanelMessage {
  type?: string;
  file?: string;
  line?: number;
  text?: string;
  message?: string;
  /** Present on `export`: the visible subgraph the webview built, not the whole model. */
  payload?: unknown;
  /** Present on `diagnostics`: the canvas state the debug report needs. */
  facts?: Record<string, unknown>;
}

export class GraphPanel {
  private static current: GraphPanel | undefined;

  private readonly panel: vscode.WebviewPanel;

  private model: GraphModel | undefined;

  private busy = false;

  private ready = false;

  private pendingExport = false;

  private diagnosticsWaiter: ((facts: Record<string, unknown>) => void) | undefined;

  private pendingRequest: GraphRequest | undefined;

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
      (message: PanelMessage) => this.onMessage(message),
      undefined,
      this.context.subscriptions,
    );

    this.panel.onDidDispose(() => {
      GraphPanel.current = undefined;
    }, undefined, this.context.subscriptions);
  }

  static show(context: vscode.ExtensionContext, loader: ModelLoader): GraphPanel {    if (GraphPanel.current) {
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

  /** The open panel, or undefined when it has been closed. */
  static instance(): GraphPanel | undefined {
    return GraphPanel.current;
  }

  /** Asks the webview to hand back the visible subgraph, which is what gets written. */
  async requestExport(): Promise<void> {
    this.panel.reveal(undefined, false);
    if (!this.ready) {
      this.pendingExport = true;
      return;
    }
    await this.panel.webview.postMessage({ type: 'requestExport' });
  }

  /**
   * Asks the canvas for the state only it knows: the filters in force, what is drawn, and the recent
   * event log. Resolves undefined when the panel is closed or does not answer in time, so a debug
   * report still renders when the graph has never been opened.
   */
  async requestDiagnostics(timeoutMs = 1500): Promise<Record<string, unknown> | undefined> {
    if (!this.ready) {
      return undefined;
    }
    return new Promise((resolve) => {
      const timer = setTimeout(() => {
        this.diagnosticsWaiter = undefined;
        resolve(undefined);
      }, timeoutMs);
      this.diagnosticsWaiter = (facts) => {
        clearTimeout(timer);
        resolve(facts);
      };
      void this.panel.webview.postMessage({ type: 'requestDiagnostics' });
    });
  }

  /** Shows the panel and narrows it to what the reader asked for. */
  async focus(request: GraphRequest): Promise<void> {
    this.pendingRequest = request;
    this.panel.reveal(undefined, false);
    if (!this.model) {
      await this.refresh(false);
    }
    await this.flushRequest();
  }

  /** Sends a queued request once the webview has announced it is listening. */
  private async flushRequest(): Promise<void> {
    if (!this.ready || !this.pendingRequest) {
      return;
    }
    const request = this.pendingRequest;
    this.pendingRequest = undefined;
    await this.panel.webview.postMessage({ type: 'request', request });
  }

  /** Pushes the current settings into the panel. Called at startup and whenever they change. */
  async pushSettings(): Promise<void> {
    if (!this.ready) {
      return;
    }
    await this.panel.webview.postMessage({ type: 'settings', settings: readGraphSettings() });
  }

  /**
   * Mirrors the analysis progress onto the canvas, so an open panel shows the same number the
   * notification bar does. `known` is false while the phase has no measurable total yet.
   */
  showProgress(message: string, percent: number, known: boolean): void {
    if (!this.ready) {
      return;
    }
    void this.panel.webview.postMessage({ type: 'progress', message, percent, known });
  }
  async refresh(force = false): Promise<void> {
    if (this.busy) {
      // Silently dropping the request left the reader unsure whether anything happened.
      void vscode.window.setStatusBarMessage('C#/XAML Graph: analysis already running', 4000);
      return;
    }
    this.busy = true;
    this.showProgress('Starting analysis', 0, false);

    try {
      this.model = await this.loader(force);
      await this.panel.webview.postMessage({ type: 'model', model: this.model });
      await this.panel.webview.postMessage({ type: 'progress', message: '', percent: 100, known: true, done: true });
    } catch (error) {
      const message = error instanceof Error ? error.message : String(error);
      await this.panel.webview.postMessage({ type: 'progress', message: '', percent: 0, known: false, done: true });
      await this.panel.webview.postMessage({ type: 'error', message });
      void vscode.window.showErrorMessage(`Dependency graph analysis failed: ${message}`);
    } finally {
      this.busy = false;
    }
  }

  private async onMessage(message: PanelMessage): Promise<void> {
    switch (message?.type) {
      case 'ready':
        this.ready = true;
        await this.pushSettings();
        if (this.model) {
          await this.panel.webview.postMessage({ type: 'model', model: this.model });
        } else {
          await this.refresh();
        }
        await this.flushRequest();
        if (this.pendingExport) {
          this.pendingExport = false;
          await this.panel.webview.postMessage({ type: 'requestExport' });
        }
        break;

      case 'reanalyze':
        await this.refresh(true);
        break;

      case 'export':
        await this.exportJson(message.payload);
        break;

      case 'reveal':
        await GraphPanel.reveal(message.file, message.line);
        break;

      case 'copy':
        if (message.text) {
          await vscode.env.clipboard.writeText(message.text);
        }
        break;

      case 'diagnostics':
        this.diagnosticsWaiter?.(message.facts ?? {});
        this.diagnosticsWaiter = undefined;
        break;

      case 'notice':
        if (message.text) {
          void vscode.window.showInformationMessage(message.text);
        }
        break;

      default:
        break;
    }
  }

  /**
   * Writes the graph the reader is looking at. `payload` is the visible subgraph built by the
   * webview; the full model is only written when no payload arrived (an older webview, or a request
   * made before the graph loaded), which keeps export honest about what is on screen.
   */
  private async exportJson(payload?: unknown): Promise<void> {
    const subject = payload ?? this.model;
    if (!subject) {
      void vscode.window.showWarningMessage('Nothing to export yet.');
      return;
    }

    const visible = payload !== undefined;
    // Save beside the code, not inside the extensions directory: that directory is read-only in some
    // remote and managed setups and is never where an export is wanted.
    const folder = vscode.workspace.workspaceFolders?.[0];
    const defaultUri = folder
      ? vscode.Uri.joinPath(folder.uri, visible ? 'dependency-graph-view.json' : 'dependency-graph.json')
      : vscode.Uri.joinPath(this.context.extensionUri, visible ? 'dependency-graph-view.json' : 'dependency-graph.json');

    const target = await vscode.window.showSaveDialog({
      filters: { JSON: ['json'] },
      saveLabel: visible ? 'Export visible graph' : 'Export graph',
      defaultUri,
    });

    if (!target) {
      return;
    }

    const bytes = Buffer.from(JSON.stringify(subject, null, 2), 'utf8');
    await vscode.workspace.fs.writeFile(target, bytes);

    const counts = payload as { stats?: { visibleNodes?: number; visibleEdges?: number } } | undefined;
    const summary = counts?.stats
      ? ` (${counts.stats.visibleNodes ?? 0} nodes, ${counts.stats.visibleEdges ?? 0} edges)`
      : '';
    void vscode.window.showInformationMessage(`Visible graph exported to ${target.fsPath}${summary}`);
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
