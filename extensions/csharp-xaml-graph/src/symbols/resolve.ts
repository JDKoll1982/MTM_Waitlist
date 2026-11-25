import * as path from 'node:path';
import * as vscode from 'vscode';
import type { GraphModel, GraphNode } from '../graph/model';
import type { GraphRequest } from '../graph/request';

/** One dependency graph the reader can choose to show. */
export interface Candidate {
  /** Line in the picker. */
  label: string;
  /** Right-hand text in the picker: kind, module, source. */
  description: string;
  /** Extra explanation under the line. */
  detail: string;
  request: GraphRequest;
}

const TYPE_KINDS = new Set<vscode.SymbolKind>([
  vscode.SymbolKind.Class,
  vscode.SymbolKind.Interface,
  vscode.SymbolKind.Struct,
  vscode.SymbolKind.Enum,
  vscode.SymbolKind.Object,
  vscode.SymbolKind.Namespace,
]);

const MEMBER_KINDS = new Set<vscode.SymbolKind>([
  vscode.SymbolKind.Method,
  vscode.SymbolKind.Constructor,
  vscode.SymbolKind.Property,
  vscode.SymbolKind.Field,
  vscode.SymbolKind.Constant,
  vscode.SymbolKind.Variable,
  vscode.SymbolKind.Event,
  vscode.SymbolKind.Function,
]);

/** Workspace-relative path with forward slashes, matching how the model records files. */
export function workspaceRelative(uri: vscode.Uri): string {
  const folder = vscode.workspace.getWorkspaceFolder(uri) ?? vscode.workspace.workspaceFolders?.[0];
  if (!folder) {
    return uri.fsPath.split(path.sep).join('/');
  }
  return vscode.workspace.asRelativePath(uri, false).split(path.sep).join('/');
}

interface Enclosing {
  typeName?: string;
  memberName?: string;
}

function walk(
  symbols: vscode.DocumentSymbol[],
  position: vscode.Position,
  trail: vscode.DocumentSymbol[],
): vscode.DocumentSymbol[] | undefined {
  for (const symbol of symbols) {
    if (!symbol.range.contains(position)) {
      continue;
    }
    const next = [...trail, symbol];
    const deeper = symbol.children.length > 0 ? walk(symbol.children, position, next) : undefined;
    return deeper ?? next;
  }
  return undefined;
}

function summarise(trail: vscode.DocumentSymbol[] | undefined): Enclosing {
  const enclosing: Enclosing = {};
  for (const symbol of trail ?? []) {
    if (TYPE_KINDS.has(symbol.kind)) {
      enclosing.typeName = symbol.name;
    } else if (MEMBER_KINDS.has(symbol.kind)) {
      enclosing.memberName = symbol.name;
    }
  }
  return enclosing;
}

async function enclosingOf(uri: vscode.Uri, position: vscode.Position): Promise<Enclosing> {
  try {
    const symbols = await vscode.commands.executeCommand<vscode.DocumentSymbol[]>(
      'vscode.executeDocumentSymbolProvider',
      uri,
    );
    return summarise(walk(symbols ?? [], position, []));
  } catch {
    return {};
  }
}

interface Target {
  file: string;
  line: number;
}

async function definitionsOf(document: vscode.TextDocument, position: vscode.Position): Promise<Target[]> {
  let results: Array<vscode.Location | vscode.LocationLink> = [];
  try {
    results = await vscode.commands.executeCommand<Array<vscode.Location | vscode.LocationLink>>(
      'vscode.executeDefinitionProvider',
      document.uri,
      position,
    ) ?? [];
  } catch {
    return [];
  }

  return results.map((result) => {
    if ('targetUri' in result) {
      return { file: workspaceRelative(result.targetUri), line: result.targetRange.start.line + 1 };
    }
    return { file: workspaceRelative(result.uri), line: result.range.start.line + 1 };
  });
}

function rootUri(uri: vscode.Uri): vscode.Uri {
  return vscode.workspace.getWorkspaceFolder(uri)?.uri
    ?? vscode.workspace.workspaceFolders?.[0]?.uri
    ?? uri;
}

function candidateFor(node: GraphNode, member: string | undefined, title: string, depth: number): Candidate {
  const where = node.location ? `${node.location.file}:${node.location.line}` : node.module;
  return {
    label: node.label,
    description: `${node.kind} · ${node.module}`,
    detail: `${title}${member ? ` — member ${member}` : ''} · ${where}`,
    request: {
      title: member ? `${node.label}.${member}` : node.label,
      nodeIds: [node.id],
      focus: node.id,
      member,
      depth,
    },
  };
}

function nodesInFile(model: GraphModel, file: string): GraphNode[] {
  return model.nodes
    .filter((node) => node.location?.file === file || node.xamlFile === file)
    .sort((left, right) => (left.location?.line ?? 0) - (right.location?.line ?? 0));
}

/** The type a location sits inside, or the nearest declaration above it in the same file. */
function resolveNode(model: GraphModel, target: Target, typeName: string | undefined): GraphNode | undefined {
  const inFile = nodesInFile(model, target.file);
  if (inFile.length === 0) {
    return undefined;
  }
  if (typeName) {
    const byName = inFile.find((node) => node.label === typeName);
    if (byName) {
      return byName;
    }
  }
  const above = inFile
    .filter((node) => (node.location?.line ?? 0) <= target.line)
    .sort((left, right) => (right.location?.line ?? 0) - (left.location?.line ?? 0));
  return above[0] ?? inFile[0];
}

function dedupe(candidates: Candidate[]): Candidate[] {
  const seen = new Map<string, Candidate>();
  for (const candidate of candidates) {
    const key = candidate.request.nodeIds.join('|');
    const existing = seen.get(key);
    // A candidate that knows the member is more useful than one that only knows the type.
    if (!existing || (!existing.request.member && candidate.request.member)) {
      seen.set(key, candidate);
    }
  }
  return [...seen.values()].sort((left, right) => left.label.localeCompare(right.label));
}

/**
 * Every dependency graph the reader could mean by right-clicking at a position.
 *
 * A selection can name a type, a field, a method or a local, and the same simple name can exist in
 * several namespaces, so this returns a list and the command shows a picker when it has more than one.
 */
export async function candidatesForPosition(
  document: vscode.TextDocument,
  position: vscode.Position,
  model: GraphModel,
): Promise<Candidate[]> {
  const file = workspaceRelative(document.uri);

  if (document.uri.path.toLowerCase().endsWith('.xaml')) {
    return candidatesForFileUri(document.uri, model);
  }

  const candidates: Candidate[] = [];
  const wordRange = document.getWordRangeAtPosition(position, /[A-Za-z_][A-Za-z0-9_]*/);
  const word = wordRange ? document.getText(wordRange) : '';

  const here = await enclosingOf(document.uri, position);
  if (here.typeName) {
    const node = resolveNode(model, { file, line: position.line + 1 }, here.typeName);
    if (node) {
      candidates.push(candidateFor(node, here.memberName, 'selected in the editor', 1));
    }
  }

  for (const target of await definitionsOf(document, position)) {
    const targetUri = vscode.Uri.joinPath(rootUri(document.uri), ...target.file.split('/'));
    const there = await enclosingOf(targetUri, new vscode.Position(Math.max(0, target.line - 1), 0));
    const node = resolveNode(model, target, there.typeName ?? here.typeName);
    if (node) {
      candidates.push(candidateFor(node, there.memberName ?? here.memberName, 'go to definition', 1));
    }
  }

  if (word) {
    const exact = model.nodes.filter((node) => node.label === word || node.fullName === word);
    const loose = exact.length > 0
      ? exact
      : model.nodes.filter((node) => node.label.toLowerCase() === word.toLowerCase());
    for (const node of loose) {
      candidates.push(candidateFor(node, undefined, 'name match', 1));
    }
  }

  return dedupe(candidates);
}

function candidatesForFileUri(uri: vscode.Uri, model: GraphModel): Candidate[] {
  const file = workspaceRelative(uri);
  const declared = nodesInFile(model, file);
  if (declared.length === 0) {
    return [];
  }

  const perType = declared.map((node) => candidateFor(node, undefined, path.posix.basename(file), 1));
  if (declared.length === 1) {
    return perType;
  }

  const wholeFile: Candidate = {
    label: `Whole file — ${declared.length} types`,
    description: path.posix.basename(file),
    detail: `Restrict the graph to the types declared in ${file} and their partners`,
    request: {
      title: `${path.posix.basename(file)} (${declared.length} types)`,
      nodeIds: declared.map((node) => node.id),
      focus: declared[0]?.id,
      depth: 1,
    },
  };

  return [wholeFile, ...perType];
}

/** Every dependency graph a file can be shown as. */
export function candidatesForFile(uri: vscode.Uri, model: GraphModel): Candidate[] {
  return candidatesForFileUri(uri, model);
}
