import type { GraphModel } from '../graph/model';

/** How a row reads at a glance: a fault, a caution, a healthy value, or plain information. */
export type RowTone = 'bad' | 'warn' | 'good' | 'plain';

export interface DebugRow {
  label: string;
  value: string;
  tone?: RowTone;
}

export interface DebugSection {
  /** Stable anchor, used for the nav link, the copy target and the page fragment. */
  id: string;
  /** Heading word, e.g. `Host`; the section text renders `## <category> — <title>`. */
  category: string;
  title: string;
  /** One sentence saying what this section is for, so the page explains itself. */
  purpose: string;
  /** Extension-relative paths that own this subsystem. */
  files: string[];
  rows: DebugRow[];
}

export interface HostFacts {
  extensionVersion: string;
  vscodeVersion: string;
  extensionMode: string;
  appHost: string;
  uiKind: string;
  remoteName: string;
  isTrusted: boolean;
  platform: string;
  arch: string;
  nodeVersion: string;
  locale: string;
  extensionPath: string;
  workspaceFolders: string[];
}

export interface SettingFact {
  key: string;
  value: string;
  /** `default`, `user`, `workspace` or `folder` — where the value in force came from. */
  source: string;
}

export interface AssetFact {
  /** Extension-relative path. */
  path: string;
  exists: boolean;
  bytes?: number;
}

export interface MenuFact {
  menu: string;
  commands: string[];
}

export interface ViewFact {
  id: string;
  type: string;
}

/** Everything read from the extension host, before any graph fact is added. */
export interface HostSnapshot {
  host: HostFacts;
  settings: SettingFact[];
  contributedCommands: string[];
  contributedMenus: MenuFact[];
  assets: AssetFact[];
  activationEvents: string[];
  views: ViewFact[];
}

/** What the graph panel reports about its own canvas. */
export interface WebviewFacts {
  ready: boolean;
  modelLoadedAt: string | null;
  nodesDrawn: number;
  edgesDrawn: number;
  hiddenOrphans: number;
  layout: string;
  density: string;
  labels: string;
  zoom: number;
  canvasWidth: number;
  canvasHeight: number;
  module: string;
  search: string;
  layers: string[];
  edgeKinds: string[];
  focus: string | null;
  focusDepth: number;
  scope: string | null;
  scopeExact: boolean;
  unpacked: string[];
  history: { at: string; kind: string; detail: string }[];
}

/** The input {@link buildSections} needs: the host snapshot plus the analysis half. */
export interface BuildSectionsInput extends HostSnapshot {
  model?: GraphModel;
  modelAgeMs?: number;
  cacheInvalidated: boolean;
  webview?: WebviewFacts;
  registeredCommands: string[];
}
