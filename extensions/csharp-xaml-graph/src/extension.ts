import * as path from 'node:path';
import * as vscode from 'vscode';
import type { AnalyzeInput } from './graph/analyze';
import { analyzeWorkspace } from './graph/analyze';
import { compileConventions, type CompiledConventions } from './graph/conventions';
import type { GraphModel, ProfileEvidence } from './graph/model';
import {
  PROFILE_FILE_NAME,
  formatProfile,
  mergeProfile,
  parseProfile,
  profileWarningText,
  recordsNothing,
  type ProfileProblem,
  type RepositoryProfile,
} from './graph/profile';
import { buildRefreshPrompt, buildSetupPrompt, nothingToProfile } from './graph/profilePrompt';
import {
  ANALYSIS_FILE_CAP,
  assessCompatibility,
  buildUpdateRequest,
  compatibilityNote,
  compatibilityWarningText,
  slugFor,
} from './graph/compatibility';
import { gatherStructure, writeUpdateRequest } from './structure';
import { MenuViewProvider } from './menu/menuView';
import { candidatesForFile, candidatesForPosition, type Candidate } from './symbols/resolve';
import { DebugPanel } from './webview/debugPanel';
import { GraphPanel, type ModelLoader } from './webview/panel';

/**
 * The number of files `findFiles` will return before it stops.
 *
 * It is the same constant the compatibility verdict counts against, so a repository the walk calls too
 * large is one this analysis would also have truncated.
 */


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

async function collectSources(
  folder: vscode.WorkspaceFolder,
  conventions: CompiledConventions,
): Promise<{ csharp: AnalyzeInput[]; xaml: AnalyzeInput[]; warning?: string }> {
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
  // A pattern the person configured wins over the profile's file selection, because that setting is
  // the escape hatch when a profile is wrong. Each list is decided on its own.
  const include = explicitlyConfigured(configuration, 'include')
    ? configuration.get<string[]>('include', conventions.include)
    : conventions.include;
  const exclude = explicitlyConfigured(configuration, 'exclude')
    ? configuration.get<string[]>('exclude', conventions.exclude)
    : conventions.exclude;

  const includePattern = include.length === 1 ? include[0] : `{${include.join(',')}}`;
  const excludePattern = exclude.length === 0
    ? undefined
    : (exclude.length === 1 ? exclude[0] : `{${exclude.join(',')}}`);

  const uris = await vscode.workspace.findFiles(
    new vscode.RelativePattern(folder, includePattern),
    excludePattern,
    ANALYSIS_FILE_CAP,
  );

  // A full result set means the remainder was dropped silently, which reads as a missing type later.
  if (uris.length >= ANALYSIS_FILE_CAP) {
    void vscode.window.showWarningMessage(
      `The analysis stopped at ${ANALYSIS_FILE_CAP} files; the rest were not read. Narrow csharpXamlGraph.include or add`
      + ' patterns to csharpXamlGraph.exclude.',
    );
  }

  const csharpUris = uris.filter((uri) => uri.path.toLowerCase().endsWith('.cs'));
  const xamlUris = uris.filter((uri) => uri.path.toLowerCase().endsWith('.xaml'));

  // An empty canvas with no explanation reads as a broken tool rather than as a selection that
  // matched nothing, so the reader is told which patterns produced nothing.
  const warning = uris.length === 0
    ? `No file matched ${include.length > 0 ? include.join(', ') : 'the include list'}. Nothing was analysed;`
      + ' check the patterns in csharpXamlGraph.include and in the profile.'
    : undefined;

  return {
    csharp: await toInputs(csharpUris),
    xaml: await toInputs(xamlUris),
    warning,
  };
}

/**
 * True when the person set this value, rather than it being the contributed default.
 *
 * The profile's file selection has to lose to a setting the reader wrote, and only to that: reading
 * the effective value cannot tell the two apart, so the levels are inspected instead.
 */
function explicitlyConfigured(
  configuration: vscode.WorkspaceConfiguration,
  key: string,
): boolean {
  const inspected = configuration.inspect<unknown>(key);
  if (!inspected) {
    return false;
  }
  return inspected.globalValue !== undefined
    || inspected.workspaceValue !== undefined
    || inspected.workspaceFolderValue !== undefined;
}

/**
 * Reads the profile beside the analysed folder.
 *
 * A folder with no profile is the normal case, not a failure, so a missing file produces no problem
 * at all. A file that exists but cannot be used produces a problem per thing that could not be read,
 * and the analysis carries on with the built-in conventions for whatever was lost.
 */
async function loadProfile(
  folder: vscode.WorkspaceFolder,
): Promise<{ profile?: RepositoryProfile; problems: ProfileProblem[] }> {
  const uri = vscode.Uri.joinPath(folder.uri, PROFILE_FILE_NAME);
  let text: string;
  try {
    const bytes = await vscode.workspace.fs.readFile(uri);
    text = Buffer.from(bytes).toString('utf8');
  } catch {
    return { problems: [] };
  }
  return parseProfile(text);
}

/** How one profile problem reads in the model's warning list: the file, the path in it, and why. */
const profileWarning = profileWarningText;

/**
 * The workbench command that fills the chat input with a prepared question.
 *
 * It is an internal command rather than a published API, so every call site checks that this host
 * has it before using it; when it is absent the prompt goes to the clipboard instead.
 */
const CHAT_OPEN = 'workbench.action.chat.open';

/**
 * Opens VS Code's chat with the prompt in the input box, unsent.
 *
 * `isPartialQuery` fills the box without submitting, so nothing leaves the machine until the
 * developer presses Enter — the extension never calls a model itself.
 */
async function openChat(prompt: string): Promise<boolean> {
  const available = await vscode.commands.getCommands(true);
  if (!available.includes(CHAT_OPEN)) {
    return false;
  }
  try {
    await vscode.commands.executeCommand(CHAT_OPEN, { query: prompt, isPartialQuery: true });
    return true;
  } catch {
    return false;
  }
}

/** The path taken when this host has no chat: the prompt is put where it can still be used. */
async function offerPromptByClipboard(prompt: string): Promise<void> {
  await vscode.env.clipboard.writeText(prompt);
  void vscode.window.showInformationMessage(
    'Copilot Chat is not available here. The prompt is on your clipboard; paste it into any assistant,'
    + ' then run Import Profile Reply from the C#/XAML Graph panel.',
  );
}

/** The first fenced block of a reply, or the whole reply when it carries none. */
function replyText(reply: string): string {
  const fenced = /```(?:json)?\s*\r?\n([\s\S]*?)```/i.exec(reply);
  return fenced ? fenced[1] : reply;
}

/** Why a reply was refused, in the words of the problems that caused it. */
function refusal(problems: ProfileProblem[], profile: RepositoryProfile): string {
  const first = problems.find((problem) => problem.severity === 'error') ?? problems[0];
  if (first) {
    return profileWarningText(first);
  }
  return profile.layers || profile.links || profile.findings || profile.files
    ? `${PROFILE_FILE_NAME} could not be read from the clipboard.`
    : `${PROFILE_FILE_NAME} would record nothing: the reply names no layer rule, link, findings rule`
      + ' or file selection.';
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
  onProgress?.('Reading the profile', 0, 0);
  const { profile, problems } = await loadProfile(folder);
  const conventions = compileConventions(profile);
  onProgress?.('Reading the files', 0, 0);
  const { csharp, xaml, warning } = await collectSources(folder, conventions);
  onProgress?.(`Read ${csharp.length + xaml.length} files`, 0, csharp.length + xaml.length);

  const model = await analyzeWorkspace({
    workspaceRoot: folder.uri.fsPath,
    csharp,
    xaml,
    grammarWasmPath: path.join(context.extensionUri.fsPath, 'media', 'tree-sitter-c_sharp.wasm'),
    includeExternalTypes: configuration.get<boolean>('includeExternalTypes', true),
    maxNodes: configuration.get<number>('maxNodes', 4000),
    progress: onProgress,
    conventions,
  });

  // A profile problem costs a warning, never the graph: what could be read has already been applied,
  // and each problem names the part of the file that could not be.
  model.warnings.push(...problems.map(profileWarningText));
  if (warning) {
    model.warnings.push(warning);
  }
  return model;
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

      const model = await buildModel(context, onProgress);
      // Applied on every analysis, so a reason from the compatibility gate survives a re-analyse.
      applyNotes(model);
      return model;
    },
  );

  const loader: ModelLoader = async (force) => {
    if (force || !cached) {
      cached = await analyse();
      cachedAt = Date.now();
      invalidated = false;
      void offerProfileOnce();
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

  /* ---------------------------------------------------------------- the profile */

  const folder = () => vscode.workspace.workspaceFolders?.[0];

  /**
   * The evidence a prompt quotes.
   *
   * The analysis attaches it to the model. A model built by an older cached run has none, and then
   * the prompts still work: they ask the same questions and quote only the file counts.
   */
  const evidenceOf = (model: GraphModel): ProfileEvidence => model.profileEvidence ?? {
    nameSuffixes: [], namePrefixes: [], namespaceSegments: [], calls: [], baseTypes: [],
    methodAttributes: [], fieldAttributes: [], xamlRootTags: [],
    csharpFiles: model.stats.csharpFiles,
    xamlFiles: model.stats.xamlFiles,
  };

  /**
   * The compatibility note each folder's verdict produced, kept for the session.
   *
   * It lives here rather than on one model because a re-analyse builds a new model: the reason has to
   * survive the dialog, the dismissal, and the next analysis, or FR-028 is met only until the reader
   * presses Re-analyse.
   */
  const sessionNotes = new Map<string, string[]>();

  /** Folders the offer has been put to, so it is asked once per folder per session. */
  const askedRequests = new Set<string>();

  /** Replaces the compatibility notes on a model with what this session recorded for the folder. */
  const applyNotes = (model: GraphModel): void => {
    const current = folder();
    const notes = current ? sessionNotes.get(current.uri.fsPath) ?? [] : [];
    model.warnings = model.warnings
      .filter((warning) => compatibilityNote(warning) === undefined)
      .concat(notes);
  };

  /**
   * Issues the verdict before any convention is gathered, and returns whether profile work may go on.
   *
   * `compatible` proceeds exactly as it did before this gate existed. `unknown` proceeds too, with one
   * note saying the structure could not be checked. `incompatible` names each structure it cannot
   * handle and asks one question, and nothing is written until that question is answered.
   */
  const gate = async (current: vscode.WorkspaceFolder): Promise<boolean> => {
    const verdict = assessCompatibility(
      gatherStructure(current.uri.fsPath),
      ANALYSIS_FILE_CAP,
      slugFor(current.name),
    );
    sessionNotes.set(
      current.uri.fsPath,
      verdict.verdict === 'compatible' ? [] : verdict.reasons.map(compatibilityWarningText),
    );

    if (verdict.verdict !== 'incompatible') {
      if (verdict.verdict === 'unknown') {
        applyNotes(await loader(false));
      }
      return true;
    }

    // The reason is on the model before the question is asked, so it outlives the dialog that goes on
    // to ask it.
    applyNotes(await loader(false));
    void GraphPanel.instance()?.refresh();

    if (askedRequests.has(current.uri.fsPath)) {
      return false;
    }
    askedRequests.add(current.uri.fsPath);

    const choice = await vscode.window.showWarningMessage(
      [
        `${current.name} cannot be analysed by this extension.`,
        ...verdict.reasons.map((reason) => reason.message),
      ].join('\n'),
      'Start a new specification',
      'Not now',
    );
    // A dismissed notification is an answer, and the answer is no: nothing is written.
    if (choice !== 'Start a new specification') {
      return false;
    }

    const request = buildUpdateRequest(verdict);
    const written = writeUpdateRequest(
      context.extensionUri.fsPath,
      slugFor(current.name),
      request,
    );
    if (written.ok) {
      void vscode.window.showInformationMessage(
        `The request for ${current.name} is in ${written.path}. Run it with /speckit-companion-specify.`,
      );
    } else {
      // The extension folder can be read-only, and a request that vanishes because of that is worse
      // than one on the clipboard: the path is named either way.
      await vscode.env.clipboard.writeText(request);
      void vscode.window.showInformationMessage(
        `${written.path} could not be written (${written.error}). The request is on your clipboard; paste it`
        + ' into that file, then run it with /speckit-companion-specify.',
      );
    }
    return false;
  };

  const openSetupChat = async (): Promise<void> => {
    const current = folder();
    if (!current) {
      void vscode.window.showWarningMessage('Open the workspace folder that contains the C# and XAML you want to profile.');
      return;
    }
    if (!await gate(current)) {
      return;
    }
    const model = await loader(false);
    const evidence = evidenceOf(model);
    if (evidence.csharpFiles + evidence.xamlFiles === 0) {
      void vscode.window.showInformationMessage(nothingToProfile(current.name));
      return;
    }
    const prompt = buildSetupPrompt(evidence, current.name);
    if (!await openChat(prompt)) {
      await offerPromptByClipboard(prompt);
    }
  };

  register('csharpXamlGraph.profile.initialize', openSetupChat);

  /**
   * Offers to create the profile once per session, without blocking the analysis.
   *
   * A repository with no profile is analysed by the built-in conventions, which is often exactly
   * right; the offer is there for the repository that has never had the chance. Dismissing it is an
   * answer, so it is not asked again until the window is reloaded.
   */
  let offeredProfile = false;
  const offerProfileOnce = async (): Promise<void> => {
    if (offeredProfile) {
      return;
    }
    offeredProfile = true;
    const current = folder();
    if (!current) {
      return;
    }
    if ((await loadProfile(current)).profile) {
      return;
    }
    const choice = await vscode.window.showInformationMessage(
      `${current.name} has no conventions profile yet, so the graph is reading it by the built-in`
      + ' conventions. You can record what this repository actually does and commit the file with the code.',
      'Set up profile',
      'Not now',
    );
    if (choice === 'Set up profile') {
      await openSetupChat();
    }
  };

  /**
   * Watches the profile so a change made outside the import command is picked up in this session.
   *
   * A hand-edited or freshly pulled profile would otherwise be ignored until the reader pressed
   * Re-analyse, which reads as the profile having no effect.
   */
  const profileWatcher = () => {
    const current = folder();
    if (!current) {
      return undefined;
    }
    const watcher = vscode.workspace.createFileSystemWatcher(
      new vscode.RelativePattern(current, PROFILE_FILE_NAME),
    );
    // A profile can appear (a checkout, a pull, a teammate's copy) or disappear as well as change,
    // and both leave the cached model describing a file that is no longer what is on disk.
    const invalidate = (): void => {
      cached = undefined;
      cachedAt = undefined;
      invalidated = true;
      void GraphPanel.instance()?.refresh(true);
    };
    watcher.onDidChange(invalidate);
    watcher.onDidCreate(invalidate);
    watcher.onDidDelete(invalidate);
    return watcher;
  };
  const watcher = profileWatcher();
  if (watcher) {
    context.subscriptions.push(watcher);
  }

  register('csharpXamlGraph.profile.import', async () => {
    const current = folder();
    if (!current) {
      void vscode.window.showWarningMessage('Open the workspace folder whose profile you want to import into.');
      return;
    }

    const parsed = parseProfile(replyText(await vscode.env.clipboard.readText()));
    if (!parsed.profile) {
      // Nothing is written: a half-understood reply is worse than none, because the file is committed.
      void vscode.window.showWarningMessage(
        `Nothing was written: ${refusal(parsed.problems, { version: 1 })}`,
      );
      return;
    }

    // A reply can parse and still record nothing — a section whose contents were all unusable, or a
    // reply with only a version. Writing that would leave a file that looks authoritative and says
    // nothing, so it is refused with the part that could not be read named. The judgement is made on
    // the merged profile, because a reply that adds nothing to a file that already says something is
    // a harmless no-op rather than an empty file.
    const existing = await loadProfile(current);
    const merged = mergeProfile(existing.profile ?? { version: 1 }, parsed.profile);
    if (recordsNothing(merged)) {
      void vscode.window.showWarningMessage(
        `Nothing was written: ${refusal(parsed.problems, parsed.profile)}`,
      );
      return;
    }

    const uri = vscode.Uri.joinPath(current.uri, PROFILE_FILE_NAME);
    await vscode.workspace.fs.writeFile(uri, Buffer.from(formatProfile(merged), 'utf8'));

    const document = await vscode.workspace.openTextDocument(uri);
    await vscode.window.showTextDocument(document, { preview: true });

    // The file just changed under the cache, so the graph reads it again in this session.
    cached = undefined;
    cachedAt = undefined;
    invalidated = true;
    await GraphPanel.instance()?.refresh(true);

    // The message says what the merge could not carry over, so nothing is dropped without a word.
    const skipped = parsed.problems.filter((problem) => problem.severity === 'warning');
    const inherited = existing.problems.filter((problem) => problem.severity === 'warning');
    const notes = [
      skipped.length > 0
        ? `${skipped.length} part(s) of the reply were skipped, starting at ${skipped[0].path}.`
        : '',
      inherited.length > 0
        ? `The file already held ${inherited.length} part(s) that could not be read; they are not in`
          + ' the merged file.'
        : '',
    ].filter(Boolean).join(' ');

    void vscode.window.showInformationMessage(
      `${PROFILE_FILE_NAME} saved in ${current.name}. Review it, then commit it with the code.`
      + (notes ? ` ${notes}` : ''),
    );
  });

  register('csharpXamlGraph.profile.refresh', async () => {
    const current = folder();
    if (!current) {
      void vscode.window.showWarningMessage('Open the workspace folder whose profile you want to update.');
      return;
    }
    if (!await gate(current)) {
      return;
    }

    const existing = await loadProfile(current);
    // Nothing to update yet, so the refresh becomes the setup rather than asking about a file that
    // is not there.
    if (!existing.profile) {
      await openSetupChat();
      return;
    }

    const model = await loader(false);
    const prompt = buildRefreshPrompt(evidenceOf(model), existing.profile, current.name);
    if (!await openChat(prompt)) {
      await offerPromptByClipboard(prompt);
    }
  });
}

export function deactivate(): void {
  // Nothing to release: the panel and the panel view own their own disposal.
}
