import * as path from 'node:path';
import * as vscode from 'vscode';
import type { AnalyzeInput } from './graph/analyze';
import { analyzeWorkspace } from './graph/analyze';
import type { GraphModel } from './graph/model';
import { MenuViewProvider } from './menu/menuView';
import { candidatesForFile, candidatesForPosition, type Candidate } from './symbols/resolve';
import { DebugPanel } from './webview/debugPanel';
import { GraphPanel, type ModelLoader } from './webview/panel';

const DEFAULT_EXCLUDE = [
  '**/bin/**', '**/obj/**', '**/node_modules/**', '**/*.Tests/**',
  // Documentation and agent configuration carry C# samples that are not application code.
  '**/.github/**', '**/.specify/**',
];

/** The number of files `findFiles` will return before it stops. */
const FILE_CAP = 20000;

/** Workspaces already warned about multi-root scope, so the notice appears once per session. */
const multiRootWarned = new Set<string>();

function toInputs(uris: vscode.Uri[]): Promise<AnalyzeInput[]> {
  return Promise.all(uris.map(async (uri) => {
    const bytes = await vscode.workspace.fs.readFile(uri);
    return {
      path: vscode.workspace.asRelativePath(uri, false).split(path.sep).join('/'),
      text: Buffer.from(bytes).toString('utf8'),
    };
  }));
}

async function collectSources(folder: vscode.WorkspaceFolder): Promise<{ csharp: AnalyzeInput[]; xaml: AnalyzeInput[] }> {
  const folders = vscode.workspace.workspaceFolders ?? [];

  // Paths are made relative to the analysed folder, so files from any other root would collide with
  // them and could not be opened again. One folder is analysed; the reader is told which.
  if (folders.length > 1 && !multiRootWarned.has(folder.uri.fsPath)) {
    multiRootWarned.add(folder.uri.fsPath);
    void vscode.window.showWarningMessage(
      `This workspace has ${folders.length} folders. The dependency graph analyses the first (${folder.name}) only;`
      + ' open a single folder to analyse another.',
      'Show graph',
    );
  }

  const configuration = vscode.workspace.getConfiguration('csharpXamlGraph');
  const include = configuration.get<string[]>('include', ['**/*.cs', '**/*.xaml']);
  const exclude = configuration.get<string[]>('exclude', DEFAULT_EXCLUDE);

  const includePattern = include.length === 1 ? include[0] : `{${include.join(',')}}`;
  const excludePattern = exclude.length === 0
    ? undefined
    : (exclude.length === 1 ? exclude[0] : `{${exclude.join(',')}}`);

  const uris = await vscode.workspace.findFiles(
    new vscode.RelativePattern(folder, includePattern),
    excludePattern,
    FILE_CAP,
  );

  // A full result set means the remainder was dropped silently, which reads as a missing type later.
  if (uris.length >= FILE_CAP) {
    void vscode.window.showWarningMessage(
      `The analysis stopped at ${FILE_CAP} files; the rest were not read. Narrow csharpXamlGraph.include or add`
      + ' patterns to csharpXamlGraph.exclude.',
    );
  }

  const csharpUris = uris.filter((uri) => uri.path.toLowerCase().endsWith('.cs'));
  const xamlUris = uris.filter((uri) => uri.path.toLowerCase().endsWith('.xaml'));

  return {
    csharp: await toInputs(csharpUris),
    xaml: await toInputs(xamlUris),
  };
}

type ProgressReport = (phase: string, completed: number, total: number) => void;

async function buildModel(
  context: vscode.ExtensionContext,
  onProgress?: ProgressReport,
): Promise<GraphModel> {
  const folder = vscode.workspace.workspaceFolders?.[0];
  if (!folder) {
    throw new Error('Open the workspace folder that contains the C# and XAML you want to analyse.');
  }

  const configuration = vscode.workspace.getConfiguration('csharpXamlGraph');
  onProgress?.('Reading the files', 0, 0);
  const { csharp, xaml } = await collectSources(folder);
  onProgress?.(`Read ${csharp.length + xaml.length} files`, 0, csharp.length + xaml.length);

  return analyzeWorkspace({
    workspaceRoot: folder.uri.fsPath,
    csharp,
    xaml,
    grammarWasmPath: path.join(context.extensionUri.fsPath, 'media', 'tree-sitter-c_sharp.wasm'),
    includeExternalTypes: configuration.get<boolean>('includeExternalTypes', true),
    maxNodes: configuration.get<number>('maxNodes', 4000),
    progress: onProgress,
  });
}

/** Shows the picker when a right-click could mean more than one graph, then opens the panel. */
async function showChosen(
  context: vscode.ExtensionContext,
  loader: ModelLoader,
  candidates: Candidate[],
  emptyMessage: string,
): Promise<void> {
  if (candidates.length === 0) {
    void vscode.window.showWarningMessage(emptyMessage);
    return;
  }

  let chosen = candidates[0];
  if (candidates.length > 1) {
    const picked = await vscode.window.showQuickPick(
      candidates.map((candidate) => ({
        label: candidate.label,
        description: candidate.description,
        detail: candidate.detail,
        candidate,
      })),
      {
        title: 'Show dependency graph',
        placeHolder: 'Which dependency graph do you want to show?',
        matchOnDescription: true,
        matchOnDetail: true,
      },
    );
    if (!picked) {
      return;
    }
    chosen = picked.candidate;
  }

  const panel = GraphPanel.show(context, loader);
  await panel.focus(chosen.request);
}

export function activate(context: vscode.ExtensionContext): void {
  let cached: GraphModel | undefined;
  let cachedAt: number | undefined;
  let invalidated = false;
  const registeredCommands: string[] = [];

  /**
   * Analyses the workspace behind a real progress bar.
   *
   * A notification with `increment` gives the reader a bar they can see even when the panel is behind
   * another editor. Every report also goes to the open graph panel, so the canvas shows the same
   * number instead of a static "analysing…".
   */
  const analyse = () => vscode.window.withProgress(
    {
      location: vscode.ProgressLocation.Notification,
      title: 'C#/XAML dependency graph',
      cancellable: false,
    },
    async (progress) => {
      let lastPercent = -1;
      const onProgress: ProgressReport = (phase, completed, total) => {
        const percent = total > 0 ? Math.min(100, Math.round((completed / total) * 100)) : 0;
        const message = total > 0 ? `${phase} (${percent}%)` : phase;

        // Only move the bar forward; a phase change resets `completed`, and a bar that jumps backwards
        // reads as a restart.
        if (percent > lastPercent && total > 0) {
          progress.report({ message, increment: percent - Math.max(0, lastPercent) });
          lastPercent = percent;
        } else {
          progress.report({ message });
        }

        GraphPanel.instance()?.showProgress(message, percent, total > 0);
      };

      return buildModel(context, onProgress);
    },
  );

  const loader: ModelLoader = async (force) => {
    if (force || !cached) {
      cached = await analyse();
      cachedAt = Date.now();
      invalidated = false;
    }
    return cached;
  };

  // A change to what is read invalidates the analysed model; a presentational change such as the
  // unpacked layout only needs to reach the open panel.
  const ANALYSIS_KEYS = ['include', 'exclude', 'includeExternalTypes', 'maxNodes'];
  context.subscriptions.push(
    vscode.workspace.onDidChangeConfiguration((event) => {
      if (!event.affectsConfiguration('csharpXamlGraph')) {
        return;
      }
      if (ANALYSIS_KEYS.some((key) => event.affectsConfiguration(`csharpXamlGraph.${key}`))) {
        cached = undefined;
        cachedAt = undefined;
        invalidated = true;
      }
      void GraphPanel.instance()?.pushSettings();
    }),
  );

  const register = (command: string, handler: (...args: never[]) => unknown): void => {
    registeredCommands.push(command);
    context.subscriptions.push(
      vscode.commands.registerCommand(command, handler as (...args: unknown[]) => unknown),
    );
  };

  context.subscriptions.push(
    vscode.window.registerWebviewViewProvider(
      MenuViewProvider.viewId,
      new MenuViewProvider(context),
    ),
  );

  register('csharpXamlGraph.show', async () => {
    const panel = GraphPanel.show(context, loader);
    // Show reuses the cached model when there is one; Refresh is the command that forces a re-read.
    await panel.refresh();
  });

  register('csharpXamlGraph.refresh', async () => {
    const panel = GraphPanel.show(context, loader);
    await panel.refresh(true);
  });

  register('csharpXamlGraph.debug', async () => {
    DebugPanel.show(context, {
      getModel: () => cached,
      getModelAge: () => (cachedAt === undefined ? undefined : Date.now() - cachedAt),
      isCacheInvalidated: () => invalidated,
      registeredCommands: [...registeredCommands, 'csharpXamlGraph.debug'],
      getGraphPanel: () => GraphPanel.instance(),
      reanalyze: async () => {
        await loader(true);
      },
    });
  });

  register('csharpXamlGraph.showForSelection', async () => {
    const editor = vscode.window.activeTextEditor;
    if (!editor) {
      void vscode.window.showWarningMessage('Open a C# or XAML file, then right-click the type you want to see.');
      return;
    }

    const model = await loader(false);
    const position = editor.selection.isEmpty ? editor.selection.active : editor.selection.start;
    const candidates = await candidatesForPosition(editor.document, position, model);
    await showChosen(
      context,
      loader,
      candidates,
      'Nothing on that line maps to a type in the analysed workspace.',
    );
  });

  register('csharpXamlGraph.showForFile', async (uri?: unknown) => {
    const target = uri instanceof vscode.Uri ? uri : vscode.window.activeTextEditor?.document.uri;
    if (!target) {
      void vscode.window.showWarningMessage('Select a C# or XAML file first.');
      return;
    }

    const model = await loader(false);
    await showChosen(
      context,
      loader,
      candidatesForFile(target, model),
      'That file declares nothing the analysis can place on the graph.',
    );
  });

  register('csharpXamlGraph.export', async () => {
    const panel = GraphPanel.show(context, loader);
    await panel.refresh();
    await panel.requestExport();
  });

  register('csharpXamlGraph.openSettings', async () => {
    await vscode.commands.executeCommand('workbench.action.openSettings', '@ext:mtm-local.csharp-xaml-graph');
  });

  register('csharpXamlGraph.openMenu', async () => {
    await vscode.commands.executeCommand('workbench.view.extension.csharpXamlGraphPanel');
  });
}

export function deactivate(): void {
  // Nothing to release: the panel and the panel view own their own disposal.
}
