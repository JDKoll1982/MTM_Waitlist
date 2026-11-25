import * as path from 'node:path';
import * as vscode from 'vscode';
import type { AnalyzeInput } from './graph/analyze';
import { analyzeWorkspace } from './graph/analyze';
import type { GraphModel } from './graph/model';
import { GraphPanel } from './webview/panel';

const DEFAULT_EXCLUDE = ['**/bin/**', '**/obj/**', '**/node_modules/**', '**/*.Tests/**'];

function toInputs(uris: vscode.Uri[]): Promise<AnalyzeInput[]> {
  return Promise.all(uris.map(async (uri) => {
    const bytes = await vscode.workspace.fs.readFile(uri);
    return {
      path: vscode.workspace.asRelativePath(uri, false).split(path.sep).join('/'),
      text: Buffer.from(bytes).toString('utf8'),
    };
  }));
}

async function collectSources(): Promise<{ csharp: AnalyzeInput[]; xaml: AnalyzeInput[] }> {
  const folder = vscode.workspace.workspaceFolders?.[0];
  if (!folder) {
    throw new Error('Open the workspace folder that contains the C# and XAML you want to analyse.');
  }

  const configuration = vscode.workspace.getConfiguration('csharpXamlGraph');
  const include = configuration.get<string[]>('include', ['**/*.cs', '**/*.xaml']);
  const exclude = configuration.get<string[]>('exclude', DEFAULT_EXCLUDE);

  const includePattern = include.length === 1 ? include[0] : `{${include.join(',')}}`;
  const excludePattern = exclude.length === 0
    ? undefined
    : (exclude.length === 1 ? exclude[0] : `{${exclude.join(',')}}`);

  const uris = await vscode.workspace.findFiles(includePattern, excludePattern, 20000);

  const csharpUris = uris.filter((uri) => uri.path.toLowerCase().endsWith('.cs'));
  const xamlUris = uris.filter((uri) => uri.path.toLowerCase().endsWith('.xaml'));

  return {
    csharp: await toInputs(csharpUris),
    xaml: await toInputs(xamlUris),
  };
}

async function buildModel(context: vscode.ExtensionContext): Promise<GraphModel> {
  const folder = vscode.workspace.workspaceFolders?.[0];
  if (!folder) {
    throw new Error('Open the workspace folder that contains the C# and XAML you want to analyse.');
  }

  const configuration = vscode.workspace.getConfiguration('csharpXamlGraph');
  const { csharp, xaml } = await collectSources();

  return analyzeWorkspace({
    workspaceRoot: folder.uri.fsPath,
    csharp,
    xaml,
    grammarWasmPath: path.join(context.extensionUri.fsPath, 'media', 'tree-sitter-c_sharp.wasm'),
    includeExternalTypes: configuration.get<boolean>('includeExternalTypes', true),
    maxNodes: configuration.get<number>('maxNodes', 4000),
  });
}

export function activate(context: vscode.ExtensionContext): void {
  const loader = () => vscode.window.withProgress(
    {
      location: vscode.ProgressLocation.Window,
      title: 'Analysing C# and XAML…',
    },
    () => buildModel(context),
  );

  context.subscriptions.push(
    vscode.commands.registerCommand('csharpXamlGraph.show', async () => {
      const panel = GraphPanel.show(context, loader);
      await panel.refresh();
    }),

    vscode.commands.registerCommand('csharpXamlGraph.refresh', async () => {
      const panel = GraphPanel.show(context, loader);
      await panel.refresh();
    }),
  );
}

export function deactivate(): void {
  // Nothing to release: the panel owns its own disposal.
}
