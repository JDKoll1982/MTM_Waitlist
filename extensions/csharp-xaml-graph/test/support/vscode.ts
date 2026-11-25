/**
 * A `vscode` module for the test run.
 *
 * The extension host API is not available outside VS Code, so every source file that imports it is
 * pointed here by the Vitest alias. The implementation is deliberately faithful to the parts the
 * extension uses — a `WebviewPanel` really does deliver messages to its listeners, a configuration
 * really does remember what `update` wrote — so a test exercises the extension's own code rather
 * than a recording stub. Everything a test needs to inspect lives on `state`.
 */

/* ------------------------------------------------------------------ state */

export interface FakeFolder {
  uri: Uri;
  name: string;
  index: number;
}

export interface ConfigUpdate {
  key: string;
  value: unknown;
  target: unknown;
}

export interface RecordedMessage {
  level: 'warning' | 'information' | 'error' | 'status';
  message: string;
  actions: unknown[];
}

interface FakeWebview {
  html: string;
  options: Record<string, unknown>;
  cspSource: string;
  asWebviewUri: (uri: Uri) => Uri;
  postMessage: (message: unknown) => Promise<boolean>;
  onDidReceiveMessage: (callback: (message: unknown) => unknown) => { dispose: () => void };
}

interface FakeDisposable {
  dispose: () => void;
}

export interface FakeWebviewPanel {
  viewType: string;
  title: string;
  viewColumn: unknown;
  options: Record<string, unknown>;
  webview: FakeWebview;
  revealed: number;
  visible: boolean;
  reveal: (column?: unknown, preserveFocus?: boolean) => void;
  onDidDispose: (callback: () => void) => FakeDisposable;
  dispose: () => void;
  /** Messages the extension pushed to the page. */
  posted: unknown[];
  /** Delivers a message from the page to the extension, as the webview bridge does. */
  send: (message: unknown) => Promise<void>;
  disposed: boolean;
}

export const state = {
  folders: [] as FakeFolder[],
  configValues: {} as Record<string, unknown>,
  inspected: {} as Record<string, Record<string, unknown>>,
  configUpdates: [] as ConfigUpdate[],
  updateError: undefined as Error | undefined,
  fileContents: {} as Record<string, string>,
  writtenFiles: [] as { path: string; text: string }[],
  fileStats: {} as Record<string, number>,
  findFilesResult: [] as Uri[],
  findFilesCalls: [] as { include: unknown; exclude: unknown; max: number }[],
  findFilesError: undefined as Error | undefined,
  commandHandlers: {} as Record<string, (...args: unknown[]) => unknown>,
  registeredCommands: [] as string[],
  executedCommands: [] as { command: string; args: unknown[] }[],
  messages: [] as RecordedMessage[],
  clipboard: [] as string[],
  panels: [] as FakeWebviewPanel[],
  viewProviders: [] as { id: string; provider: unknown }[],
  viewViews: [] as unknown[],
  configurationListeners: [] as ((event: { affectsConfiguration: (section: string) => boolean }) => void)[],
  progressOptions: undefined as unknown,
  progressReports: [] as { message?: string; increment?: number }[],
  quickPickItems: undefined as unknown,
  quickPickResult: undefined as unknown,
  quickPickCalls: [] as { items: unknown; options: unknown }[],
  saveTarget: undefined as Uri | undefined,
  saveDialogOptions: undefined as unknown,
  openDocumentError: undefined as Error | undefined,
  shownDocuments: [] as { path: string; options: unknown }[],
  isTrusted: true,
  activeTextEditor: undefined as unknown,
  extensionMode: 2,
  packageJSON: {} as Record<string, unknown>,
  subscriptionSink: [] as { dispose: () => void }[],
};

export function resetVscodeState(): void {
  state.folders = [];
  state.configValues = {};
  state.inspected = {};
  state.configUpdates = [];
  state.updateError = undefined;
  state.fileContents = {};
  state.writtenFiles = [];
  state.fileStats = {};
  state.findFilesResult = [];
  state.findFilesCalls = [];
  state.findFilesError = undefined;
  state.commandHandlers = {};
  state.registeredCommands = [];
  state.executedCommands = [];
  state.messages = [];
  state.clipboard = [];
  state.panels = [];
  state.viewProviders = [];
  state.viewViews = [];
  state.configurationListeners = [];
  state.progressOptions = undefined;
  state.progressReports = [];
  state.quickPickItems = undefined;
  state.quickPickResult = undefined;
  state.quickPickCalls = [];
  state.saveTarget = undefined;
  state.saveDialogOptions = undefined;
  state.openDocumentError = undefined;
  state.shownDocuments = [];
  state.isTrusted = true;
  state.activeTextEditor = undefined;
  state.extensionMode = 2;
  state.packageJSON = {};
  state.subscriptionSink = [];
}

/* -------------------------------------------------------------------- uri */

export class Uri {
  constructor(public readonly scheme: string, public readonly path: string) {}

  static file(fsPath: string): Uri {
    return new Uri('file', fsPath.replace(/\\/g, '/'));
  }

  static parse(value: string): Uri {
    const match = /^([a-zA-Z][\w+.-]*):\/\/(.*)$/.exec(value);
    return match ? new Uri(match[1], `/${match[2]}`) : Uri.file(value);
  }

  static joinPath(base: Uri, ...parts: string[]): Uri {
    const joined = [base.path.replace(/\/+$/, ''), ...parts.map((part) => part.replace(/^\/+/, ''))]
      .filter((part) => part.length > 0)
      .join('/');
    return new Uri(base.scheme, joined);
  }

  get fsPath(): string {
    return this.path;
  }

  toString(): string {
    return `${this.scheme}://${this.path}`;
  }
}

/* ------------------------------------------------------------ positions */

export class Position {
  constructor(public readonly line: number, public readonly character: number) {}

  isBefore(other: Position): boolean {
    return this.line < other.line || (this.line === other.line && this.character < other.character);
  }

  isBeforeOrEqual(other: Position): boolean {
    return this.isBefore(other) || this.isEqual(other);
  }

  isAfter(other: Position): boolean {
    return other.isBefore(this);
  }

  isAfterOrEqual(other: Position): boolean {
    return other.isBeforeOrEqual(this);
  }

  isEqual(other: Position): boolean {
    return this.line === other.line && this.character === other.character;
  }
}

export class Range {
  constructor(public readonly start: Position, public readonly end: Position) {}

  get isEmpty(): boolean {
    return this.start.isEqual(this.end);
  }

  contains(position: Position): boolean {
    return this.start.isBeforeOrEqual(position) && position.isBeforeOrEqual(this.end);
  }
}

export class Selection extends Range {
  constructor(public readonly anchor: Position, public readonly active: Position) {
    super(anchor, active);
  }
}

export class Location {
  constructor(public readonly uri: Uri, public readonly range: Range) {}
}

/* ------------------------------------------------------------ enumerations */

export const SymbolKind = {
  File: 0, Module: 1, Namespace: 2, Package: 3, Class: 4, Method: 5, Property: 6, Field: 7,
  Constructor: 8, Enum: 9, Interface: 10, Function: 11, Variable: 12, Constant: 13, String: 14,
  Number: 15, Boolean: 16, Array: 17, Object: 18, Key: 19, Null: 20, EnumMember: 21, Struct: 22,
  Event: 23, Operator: 24, TypeParameter: 25,
} as const;

export const ViewColumn = { Active: -1, Beside: -2, One: 1, Two: 2 } as const;

export const ProgressLocation = { SourceControl: 1, Window: 10, Notification: 15 } as const;

export const ConfigurationTarget = { Global: 1, Workspace: 2, WorkspaceFolder: 3 } as const;

export const UIKind = { Desktop: 1, Web: 2, 1: 'Desktop', 2: 'Web' } as const;

export const ExtensionMode = { Production: 1, Development: 2, Test: 3 } as const;

export const FileType = { Unknown: 0, File: 1, Directory: 2, SymbolicLink: 64 } as const;

export const StatusBarAlignment = { Left: 1, Right: 2 } as const;

export const version = '1.99.0-test';

/* ------------------------------------------------------------------ config */

function configuration(section: string) {
  return {
    get<T>(key: string, fallback?: T): T {
      const full = `${section}.${key}`;
      return (full in state.configValues ? state.configValues[full] : fallback) as T;
    },
    has(key: string): boolean {
      return `${section}.${key}` in state.configValues;
    },
    inspect(key: string) {
      return state.inspected[`${section}.${key}`] ?? {};
    },
    async update(key: string, value: unknown, target?: unknown): Promise<void> {
      if (state.updateError) {
        throw state.updateError;
      }
      const full = `${section}.${key}`;
      state.configUpdates.push({ key, value, target });
      if (value === undefined) {
        delete state.configValues[full];
      } else {
        state.configValues[full] = value;
      }
    },
  };
}

/* -------------------------------------------------------------- workspace */

export const workspace = {
  get workspaceFolders(): FakeFolder[] | undefined {
    return state.folders.length > 0 ? state.folders : undefined;
  },
  get isTrusted(): boolean {
    return state.isTrusted;
  },
  getConfiguration(section: string) {
    return configuration(section);
  },
  getWorkspaceFolder(uri: Uri): FakeFolder | undefined {
    return state.folders.find((folder) => uri.path === folder.uri.path || uri.path.startsWith(`${folder.uri.path}/`));
  },
  asRelativePath(uri: Uri | string, _includeWorkspaceFolder?: boolean): string {
    const path = typeof uri === 'string' ? uri : uri.path;
    const folder = state.folders.find((entry) => path === entry.uri.path || path.startsWith(`${entry.uri.path}/`));
    if (!folder) {
      return path;
    }
    return path === folder.uri.path ? '' : path.slice(folder.uri.path.length + 1);
  },
  async findFiles(include: unknown, exclude: unknown, maxResults?: number): Promise<Uri[]> {
    if (state.findFilesError) {
      throw state.findFilesError;
    }
    state.findFilesCalls.push({ include, exclude, max: maxResults ?? 0 });
    return state.findFilesResult;
  },
  fs: {
    async readFile(uri: Uri): Promise<Uint8Array> {
      const text = state.fileContents[uri.path];
      if (text === undefined) {
        throw new Error(`ENOENT: ${uri.path}`);
      }
      return new Uint8Array(Buffer.from(text, 'utf8'));
    },
    async writeFile(uri: Uri, content: Uint8Array): Promise<void> {
      state.writtenFiles.push({ path: uri.path, text: Buffer.from(content).toString('utf8') });
    },
    async stat(uri: Uri): Promise<{ size: number; type: number }> {
      const size = state.fileStats[uri.path];
      if (size === undefined) {
        throw new Error(`ENOENT: ${uri.path}`);
      }
      return { size, type: FileType.File };
    },
  },
  async openTextDocument(uri: Uri): Promise<unknown> {
    if (state.openDocumentError) {
      throw state.openDocumentError;
    }
    return { uri, getText: () => state.fileContents[uri.path] ?? '' };
  },
  onDidChangeConfiguration(listener: (event: { affectsConfiguration: (section: string) => boolean }) => void): FakeDisposable {
    state.configurationListeners.push(listener);
    return { dispose: () => undefined };
  },
};

export class RelativePattern {
  constructor(public readonly base: unknown, public readonly pattern: string) {}
}

/** Fires the configuration listeners as VS Code does, with `affectsConfiguration` scoped to `keys`. */
export function fireConfigurationChange(keys: string[]): void {
  for (const listener of [...state.configurationListeners]) {
    listener({ affectsConfiguration: (section) => keys.some((key) => section === key || section.startsWith(`${key}.`) || key.startsWith(`${section}.`)) });
  }
}

/* ------------------------------------------------------------------ window */

function createWebviewPanel(viewType: string, title: string, viewColumn: unknown, options: Record<string, unknown>): FakeWebviewPanel {
  const listeners: ((message: unknown) => unknown)[] = [];
  const disposeListeners: (() => void)[] = [];
  const panel: FakeWebviewPanel = {
    viewType,
    title,
    viewColumn,
    options,
    revealed: 0,
    visible: true,
    posted: [],
    disposed: false,
    webview: {
      html: '',
      options: {},
      cspSource: 'vscode-webview://csp',
      asWebviewUri: (uri: Uri) => uri,
      async postMessage(message: unknown) {
        panel.posted.push(message);
        return true;
      },
      onDidReceiveMessage(callback: (message: unknown) => unknown) {
        listeners.push(callback);
        return { dispose: () => undefined };
      },
    },
    reveal() {
      panel.revealed += 1;
    },
    onDidDispose(callback: () => void) {
      disposeListeners.push(callback);
      return { dispose: () => undefined };
    },
    send: async (message: unknown) => {
      for (const listener of listeners) {
        await listener(message);
      }
    },
    dispose() {
      panel.disposed = true;
      for (const listener of disposeListeners) {
        listener();
      }
    },
  };
  state.panels.push(panel);
  return panel;
}

export const window = {
  get activeTextEditor(): unknown {
    return state.activeTextEditor;
  },
  createWebviewPanel,
  registerWebviewViewProvider(id: string, provider: unknown): FakeDisposable {
    state.viewProviders.push({ id, provider });
    return { dispose: () => undefined };
  },
  showWarningMessage(message: string, ...actions: unknown[]): Promise<unknown> {
    state.messages.push({ level: 'warning', message, actions });
    return Promise.resolve(undefined);
  },
  showInformationMessage(message: string, ...actions: unknown[]): Promise<unknown> {
    state.messages.push({ level: 'information', message, actions });
    return Promise.resolve(undefined);
  },
  showErrorMessage(message: string, ...actions: unknown[]): Promise<unknown> {
    state.messages.push({ level: 'error', message, actions });
    return Promise.resolve(undefined);
  },
  setStatusBarMessage(message: string, _timeout?: number): FakeDisposable {
    state.messages.push({ level: 'status', message, actions: [] });
    return { dispose: () => undefined };
  },
  async showQuickPick(items: unknown, options: unknown): Promise<unknown> {
    state.quickPickItems = items;
    state.quickPickCalls.push({ items, options });
    return state.quickPickResult;
  },
  async showSaveDialog(options: unknown): Promise<Uri | undefined> {
    state.saveDialogOptions = options;
    return state.saveTarget;
  },
  async showTextDocument(document: unknown, options: unknown): Promise<unknown> {
    const uri = (document as { uri?: Uri }).uri;
    state.shownDocuments.push({ path: uri?.path ?? '', options });
    return document;
  },
  async withProgress(options: unknown, task: (progress: unknown, token: unknown) => Promise<unknown>): Promise<unknown> {
    state.progressOptions = options;
    const progress = {
      report(value: { message?: string; increment?: number }) {
        state.progressReports.push(value);
      },
    };
    return task(progress, { isCancellationRequested: false, onCancellationRequested: () => ({ dispose: () => undefined }) });
  },
};

/* ---------------------------------------------------------------- commands */

export const commands = {
  registerCommand(command: string, handler: (...args: unknown[]) => unknown): FakeDisposable {
    state.registeredCommands.push(command);
    state.commandHandlers[command] = handler;
    return { dispose: () => undefined };
  },
  async executeCommand<T>(command: string, ...args: unknown[]): Promise<T> {
    state.executedCommands.push({ command, args });
    const handler = state.commandHandlers[command] ?? state.commandHandlers['*'];
    if (handler) {
      return (await handler(...args)) as T;
    }
    return undefined as unknown as T;
  },
  async getCommands(): Promise<string[]> {
    return Object.keys(state.commandHandlers);
  },
};

/* --------------------------------------------------------------------- env */

export const env = {
  appHost: 'desktop',
  remoteName: undefined as string | undefined,
  language: 'en',
  uiKind: UIKind.Desktop,
  clipboard: {
    async writeText(value: string): Promise<void> {
      state.clipboard.push(value);
    },
    async readText(): Promise<string> {
      return state.clipboard[state.clipboard.length - 1] ?? '';
    },
  },
};

/* --------------------------------------------------------------- helpers */

/** The extension context `activate` receives, wired to the mutable state above. */
export function makeContext(extensionPath = '/ext/csharp-xaml-graph') {
  return {
    subscriptions: [] as FakeDisposable[],
    extensionUri: Uri.file(extensionPath),
    extensionPath,
    extensionMode: state.extensionMode,
    extension: { packageJSON: state.packageJSON },
    globalState: { get: () => undefined, update: async () => undefined },
    workspaceState: { get: () => undefined, update: async () => undefined },
  };
}

export type ExtensionContext = ReturnType<typeof makeContext>;
export type WebviewPanel = FakeWebviewPanel;
export type Webview = FakeWebview;
export type WebviewView = unknown;
export type WebviewViewProvider = { resolveWebviewView: (view: WebviewView) => void };
export type TextDocument = unknown;
export type DocumentSymbol = { name: string; kind: number; range: Range; children: DocumentSymbol[] };
export type LocationLink = unknown;
export type TextEditor = unknown;
export type Disposable = FakeDisposable;
