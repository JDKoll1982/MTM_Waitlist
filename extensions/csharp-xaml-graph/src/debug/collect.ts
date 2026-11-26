import { existsSync, readFileSync, statSync } from 'node:fs';
import { join } from 'node:path';
import * as vscode from 'vscode';
import type {
  AssetFact,
  BuildSectionsInput,
  HostFacts,
  HostSnapshot,
  MenuFact,
  SettingFact,
  ViewFact,
  WebviewFacts,
} from './model';
import type { GraphModel } from '../graph/model';

/** Assets the extension cannot work without, in the order the page lists them. */
/** The configuration section this extension owns; every contributed key is declared under it. */
const SECTION = 'csharpXamlGraph';

const ASSET_PATHS = [
  'package.json',
  'out/extension.js',
  'out/webview/main.js',
  'out/webview/style.css',
  'media/tree-sitter-c_sharp.wasm',
  'node_modules/web-tree-sitter/web-tree-sitter.wasm',
];

interface ManifestShape {
  version?: string;
  activationEvents?: string[];
  contributes?: {
    commands?: { command?: string }[];
    menus?: Record<string, { command?: string; submenu?: string }[]>;
    views?: Record<string, { id?: string; type?: string }[]>;
    configuration?: { properties?: Record<string, unknown> };
  };
}

function manifestOf(context: vscode.ExtensionContext): ManifestShape {
  const extension = (context as unknown as { extension?: { packageJSON?: ManifestShape } }).extension;
  return extension?.packageJSON ?? {};
}

function readVersionFromDisk(extensionPath: string): string {
  const file = join(extensionPath, 'package.json');
  try {
    if (!existsSync(file)) {
      return 'unknown';
    }
    const parsed = JSON.parse(readFileSync(file, 'utf8')) as { version?: string };
    return parsed.version ?? 'unknown';
  } catch {
    return 'unknown';
  }
}

function extensionModeName(context: vscode.ExtensionContext): string {
  // `vscode.ExtensionMode` is the enum object; the run's own mode is on the context. Comparing the
  // enum to itself reported "production" for every run, including a development build.
  const current = (context as unknown as { extensionMode?: number }).extensionMode;
  if (current === vscode.ExtensionMode.Development) {
    return 'development';
  }
  if (current === vscode.ExtensionMode.Test) {
    return 'test';
  }
  return 'production';
}

function uiKindName(): string {
  const kind = (vscode.env as unknown as { uiKind?: number }).uiKind;
  return kind === 2 ? 'Web' : 'Desktop';
}

/** Reads the settings the extension contributes, with the source each value came from. */
function collectSettings(context: vscode.ExtensionContext): SettingFact[] {
  const manifest = manifestOf(context);
  const keys = Object.keys(manifest.contributes?.configuration?.properties ?? {});
  if (keys.length === 0) {
    return [];
  }

  let configuration: vscode.WorkspaceConfiguration | undefined;
  try {
    configuration = vscode.workspace.getConfiguration(SECTION);
  } catch {
    configuration = undefined;
  }

  const settings: SettingFact[] = [];
  for (const key of keys.sort()) {
    // A manifest key is fully qualified (`csharpXamlGraph.maxNodes`) while the configuration section
    // is already open, so the section prefix has to come off before asking for the value. Leaving it
    // on asked for `csharpXamlGraph.csharpXamlGraph.maxNodes`, which is never set, and every setting
    // was then reported as its default.
    const name = key.startsWith(`${SECTION}.`) ? key.slice(SECTION.length + 1) : key;
    if (!configuration) {
      settings.push({ key: name, value: 'unavailable', source: 'unknown' });
      continue;
    }

    let source = 'default';
    let value: unknown;
    try {
      const inspected = configuration.inspect(name);
      if (inspected?.workspaceFolderValue !== undefined) {
        source = 'folder';
        value = inspected.workspaceFolderValue;
      } else if (inspected?.workspaceValue !== undefined) {
        source = 'workspace';
        value = inspected.workspaceValue;
      } else if (inspected?.globalValue !== undefined) {
        source = 'user';
        value = inspected.globalValue;
      } else {
        value = inspected?.defaultValue ?? configuration.get(name);
      }
    } catch {
      value = configuration.get(name);
    }

    settings.push({ key: name, value: describeSettingValue(value), source });
  }

  return settings;
}

function describeSettingValue(value: unknown): string {
  if (Array.isArray(value)) {
    return value.length === 0 ? '(empty)' : value.join(', ');
  }
  if (value === undefined) {
    return '(unset)';
  }
  if (value === null) {
    return 'null';
  }
  return String(value);
}

function collectAssets(extensionPath: string): AssetFact[] {
  return ASSET_PATHS.map((relative) => {
    const absolute = join(extensionPath, ...relative.split('/'));
    try {
      if (!existsSync(absolute)) {
        return { path: relative, exists: false };
      }
      return { path: relative, exists: true, bytes: statSync(absolute).size };
    } catch {
      return { path: relative, exists: false };
    }
  });
}

function collectCommands(context: vscode.ExtensionContext): string[] {
  const manifest = manifestOf(context);
  return (manifest.contributes?.commands ?? [])
    .map((entry) => entry.command)
    .filter((command): command is string => Boolean(command))
    .sort();
}

function collectMenus(context: vscode.ExtensionContext): MenuFact[] {
  const manifest = manifestOf(context);
  const menus = manifest.contributes?.menus ?? {};
  return Object.entries(menus)
    .map(([menu, entries]) => ({
      menu,
      commands: entries
        .map((entry) => entry.command ?? (entry.submenu ? `submenu:${entry.submenu}` : ''))
        .filter((command) => command.length > 0),
    }))
    .filter((entry) => entry.commands.length > 0)
    .sort((left, right) => left.menu.localeCompare(right.menu));
}

function collectViews(context: vscode.ExtensionContext): ViewFact[] {
  const manifest = manifestOf(context);
  const views = manifest.contributes?.views ?? {};
  const flattened: ViewFact[] = [];
  for (const entries of Object.values(views)) {
    for (const entry of entries) {
      if (entry.id) {
        flattened.push({ id: entry.id, type: entry.type ?? 'unknown' });
      }
    }
  }
  return flattened.sort((left, right) => left.id.localeCompare(right.id));
}

function collectHostFacts(context: vscode.ExtensionContext): HostFacts {
  const manifest = manifestOf(context);
  const workspaceFolders = (vscode.workspace.workspaceFolders ?? [])
    .map((folder) => folder.uri.fsPath.replace(/\\/g, '/'));

  return {
    extensionVersion: manifest.version ?? readVersionFromDisk(context.extensionPath),
    vscodeVersion: (vscode as unknown as { version?: string }).version ?? 'unknown',
    extensionMode: extensionModeName(context),
    appHost: String((vscode.env as unknown as { appHost?: string }).appHost ?? 'unknown'),
    uiKind: uiKindName(),
    remoteName: String((vscode.env as unknown as { remoteName?: string }).remoteName ?? ''),
    isTrusted: (vscode.workspace as unknown as { isTrusted?: boolean }).isTrusted !== false,
    platform: process.platform,
    arch: process.arch,
    nodeVersion: process.versions.node,
    locale: String((vscode.env as unknown as { language?: string }).language ?? 'unknown'),
    extensionPath: context.extensionPath.replace(/\\/g, '/'),
    workspaceFolders,
  };
}

/**
 * Reads every host fact the debug page reports.
 *
 * Nothing here is cached: the page exists to show what is true *now*, so a refresh re-reads the
 * manifest, the settings and the files on disk.
 */
export async function collectHostSnapshot(context: vscode.ExtensionContext): Promise<HostSnapshot> {
  const manifest = manifestOf(context);
  const host = collectHostFacts(context);

  return {
    host,
    settings: collectSettings(context),
    contributedCommands: collectCommands(context),
    contributedMenus: collectMenus(context),
    assets: collectAssets(context.extensionPath),
    activationEvents: [...(manifest.activationEvents ?? [])],
    views: collectViews(context),
  };
}

/** Joins the host snapshot with the analysis half, so `buildSections` sees one flat input. */
export function withHost(
  snapshot: HostSnapshot,
  extra: {
    model?: GraphModel;
    modelAgeMs?: number;
    cacheInvalidated: boolean;
    webview?: WebviewFacts;
    registeredCommands: string[];
  },
): BuildSectionsInput {
  return { ...snapshot, ...extra };
}
