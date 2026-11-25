import * as vscode from 'vscode';
import type { GraphModel } from '../graph/model';
import type { DebugSection, WebviewFacts } from '../debug/model';
import { collectHostSnapshot, withHost } from '../debug/collect';
import { buildSections } from '../debug/report';
import { renderDebugHtml, sectionText } from '../debug/reportHtml';
import { makeNonce } from './html';
import type { GraphPanel } from './panel';

export interface DebugPanelOptions {
  /** The cached model, if the graph has been analysed. */
  getModel: () => GraphModel | undefined;
  getModelAge: () => number | undefined;
  isCacheInvalidated: () => boolean;
  /** Commands registered at activation. */
  registeredCommands: string[];
  /** The open graph panel, if there is one, for the canvas half of the report. */
  getGraphPanel: () => GraphPanel | undefined;
  /** Clears the cache and re-reads the workspace. */
  reanalyze: () => Promise<void>;
}

type DebugMessage = {
  type?: string;
  section?: string | null;
};

/**
 * The debug report window.
 *
 * It is a debug *menu* in the sense that matters while working on the extension: one command opens a
 * page whose sections are the extension's subsystems, each labelled with the files that own it and
 * each with a one-click action to re-collect, re-analyse or copy that section out.
 */
export class DebugPanel {
  private static current: DebugPanel | undefined;

  private sections: DebugSection[] = [];

  private constructor(
    private readonly panel: vscode.WebviewPanel,
    private readonly context: vscode.ExtensionContext,
    private readonly options: DebugPanelOptions,
  ) {
    this.panel.webview.onDidReceiveMessage(
      (message: DebugMessage) => this.onMessage(message),
      undefined,
      context.subscriptions,
    );

    this.panel.onDidDispose(() => {
      DebugPanel.current = undefined;
    }, undefined, context.subscriptions);
  }

  static show(context: vscode.ExtensionContext, options: DebugPanelOptions): DebugPanel {
    if (DebugPanel.current) {
      DebugPanel.current.panel.reveal(undefined, false);
      void DebugPanel.current.refresh();
      return DebugPanel.current;
    }

    const panel = vscode.window.createWebviewPanel(
      'csharpXamlGraphDebug',
      'C#/XAML Graph — Diagnostics',
      vscode.ViewColumn.Beside,
      { enableScripts: true, retainContextWhenHidden: true },
    );

    DebugPanel.current = new DebugPanel(panel, context, options);
    void DebugPanel.current.refresh();
    return DebugPanel.current;
  }

  /** Re-collects both halves of the report and swaps the page in whole. */
  async refresh(): Promise<void> {
    const snapshot = await collectHostSnapshot(this.context);
    const graphPanel = this.options.getGraphPanel();
    const webview = graphPanel ? await graphPanel.requestDiagnostics() : undefined;

    this.sections = buildSections(withHost(snapshot, {
      model: this.options.getModel(),
      modelAgeMs: this.options.getModelAge(),
      cacheInvalidated: this.options.isCacheInvalidated(),
      webview: webview as WebviewFacts | undefined,
      registeredCommands: this.options.registeredCommands,
    }));

    this.panel.webview.html = renderDebugHtml(this.sections, makeNonce());
  }

  private async onMessage(message: DebugMessage): Promise<void> {
    switch (message?.type) {
      case 'debugRefresh':
        await this.refresh();
        break;

      case 'debugReanalyze':
        await this.options.reanalyze();
        await this.refresh();
        break;

      case 'debugCopy': {
        const text = sectionText(this.sections, message.section ?? null);
        await vscode.env.clipboard.writeText(text);
        await this.panel.webview.postMessage({
          type: 'debugCopied',
          text: message.section ? `Copied “${message.section}”` : 'Copied the whole report',
        });
        break;
      }

      case 'debugOpenSettings':
        await vscode.commands.executeCommand('csharpXamlGraph.openSettings');
        break;

      case 'debugOpenMenu':
        await vscode.commands.executeCommand('csharpXamlGraph.openMenu');
        break;

      default:
        break;
    }
  }
}
