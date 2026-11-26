import { afterEach, describe, expect, it } from 'vitest';
import { mkdirSync, mkdtempSync, readFileSync, readdirSync, rmSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join, resolve } from 'node:path';

import { analyzeWorkspace } from '../../src/graph/analyze';
import { compileConventions } from '../../src/graph/conventions';
import { BUILT_IN_SELECTION } from '../../src/graph/files';
import {
  collectSources,
  inspectFolder,
} from '../../src/graph/inspect';

const grammarWasmPath = join(process.cwd(), 'media', 'tree-sitter-c_sharp.wasm');
const repositoryRoot = resolve(process.cwd(), '..', '..');

const scratch: string[] = [];

function makeRoot(prefix: string): string {
  const root = mkdtempSync(join(tmpdir(), prefix));
  scratch.push(root);
  return root;
}

function write(root: string, relative: string, text: string): void {
  const target = join(root, relative);
  mkdirSync(join(target, '..'), { recursive: true });
  writeFileSync(target, text, 'utf8');
}

afterEach(() => {
  for (const root of scratch.splice(0)) {
    rmSync(root, { recursive: true, force: true });
  }
});

const fixture = (): string => {
  const root = makeRoot('inspect-');
  write(root, 'src/Views/MainPage.xaml', [
    '<Page x:Class="App.Views.MainPage"',
    '      xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"',
    '      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">',
    '  <TextBlock Text="{x:Bind ViewModel.Title}" />',
    '</Page>',
  ].join('\n'));
  write(root, 'src/Views/MainPage.xaml.cs', [
    'namespace App.Views {',
    '  public partial class MainPage : Page {',
    '    public MainViewModel ViewModel { get; }',
    '  }',
    '}',
  ].join('\n'));
  write(root, 'src/ViewModels/MainViewModel.cs',
    'namespace App.ViewModels { public class MainViewModel { public string Title { get; set; } } }');
  write(root, 'src/Services/PartStore.cs',
    'namespace App.Services { public class PartStore { public void AddSingleton<T>() { } } }');
  write(root, 'src/App.csproj', '<Project Sdk="Microsoft.NET.Sdk" />');
  write(root, 'App.sln', 'Microsoft Visual Studio Solution File');
  return root;
};

describe('the inspection reports a folder without deciding anything', () => {
  it('reports this repository\'s structure and file counts as the compatibility walk does', async () => {
    const inspection = await inspectFolder(repositoryRoot, { grammarWasmPath });

    expect(inspection.readable).toBe(true);
    expect(inspection.problem).toBeUndefined();
    expect(inspection.structure?.csharpFiles).toBe(815);
    expect(inspection.structure?.xamlFiles).toBe(93);
    expect(inspection.structure?.projectFiles).toHaveLength(16);
    expect(inspection.structure?.solutionFiles).toHaveLength(1);
    expect(inspection.files).toEqual({ csharp: 815, xaml: 93, truncated: false });
    expect(inspection.evidence?.csharpFiles).toBe(815);
    expect(inspection.evidence?.xamlFiles).toBe(93);
  });

  it('reports the same ProfileEvidence the analysis carries for the same folder', async () => {
    const root = fixture();
    const inspection = await inspectFolder(root, { grammarWasmPath });
    const sources = collectSources(root, BUILT_IN_SELECTION);
    const model = await analyzeWorkspace({
      workspaceRoot: root,
      csharp: sources.csharp,
      xaml: sources.xaml,
      grammarWasmPath,
      includeExternalTypes: false,
      conventions: compileConventions(),
    });

    expect(inspection.evidence).toEqual(model.profileEvidence);
  });

  it('counts and names the declarations without giving any of them a layer', async () => {
    const root = fixture();
    const inspection = await inspectFolder(root, { grammarWasmPath });

    expect([...inspection.declarations!.keys()].sort()).toEqual([
      'App.Services.PartStore',
      'App.ViewModels.MainViewModel',
      'App.Views.MainPage',
    ]);
    for (const entry of inspection.declarations!.values()) {
      expect(entry).toEqual({ decl: expect.any(Object), file: expect.any(String) });
      expect(entry.decl).not.toHaveProperty('layer');
    }
  });

  it('reports the classes the markup states, as markup facts', async () => {
    const inspection = await inspectFolder(fixture(), { grammarWasmPath });

    expect(inspection.xamlClasses).toEqual([
      { name: 'App.Views.MainPage', file: 'src/Views/MainPage.xaml' },
    ]);
    // A class the markup names is still a declarationless fact: no layer rides on it here.
    expect(inspection.xamlClasses[0]).not.toHaveProperty('layer');
  });

  it('gives back only the facts and the evidence, so no layer, link or finding can ride along', async () => {
    const inspection = await inspectFolder(fixture(), { grammarWasmPath });

    expect(Object.keys(inspection).sort()).toEqual([
      'declarations', 'evidence', 'files', 'readable', 'structure', 'xamlClasses',
    ]);
    expect(inspection).not.toHaveProperty('nodes');
    expect(inspection).not.toHaveProperty('edges');
    expect(inspection).not.toHaveProperty('findings');
  });

  it('writes nothing into the folder it reads', async () => {
    const root = fixture();
    const before = readdirSync(root, { recursive: true }).map(String).sort();

    await inspectFolder(root, { grammarWasmPath });

    const after = readdirSync(root, { recursive: true }).map(String).sort();
    expect(after).toEqual(before);
  });

  it('reports a folder with no C# and no XAML as facts with an empty evidence set', async () => {
    const root = makeRoot('inspect-empty-');
    write(root, 'README.md', '# nothing to analyse');
    write(root, 'tools/Fallback.fsproj', '<Project />');

    const inspection = await inspectFolder(root, { grammarWasmPath });

    expect(inspection.readable).toBe(true);
    expect(inspection.structure?.csharpFiles).toBe(0);
    expect(inspection.structure?.xamlFiles).toBe(0);
    expect(inspection.structure?.projectFiles).toEqual(['tools/Fallback.fsproj']);
    expect(inspection.evidence).toEqual({
      nameSuffixes: [],
      namePrefixes: [],
      namespaceSegments: [],
      calls: [],
      baseTypes: [],
      methodAttributes: [],
      fieldAttributes: [],
      xamlRootTags: [],
      csharpFiles: 0,
      xamlFiles: 0,
    });
  });

  it('reports a folder it cannot read instead of throwing', async () => {
    const missing = join(tmpdir(), 'inspect-does-not-exist-023');

    const inspection = await inspectFolder(missing, { grammarWasmPath });

    expect(inspection.readable).toBe(false);
    expect(inspection.problem).toBe('the folder could not be listed');
    expect(inspection.evidence).toBeUndefined();
    expect(inspection.declarations).toBeUndefined();
    expect(inspection.structure).toBeUndefined();
  });

  it('needs no editor API, so the module imports no vscode', () => {
    const source = readFileSync(join(process.cwd(), 'src', 'graph', 'inspect.ts'), 'utf8');
    expect(source).not.toMatch(/from ['"]vscode['"]/);
    expect(source).not.toMatch(/require\(['"]vscode['"]\)/);
  });
});

describe('the derivation the analysis and the inspection share', () => {
  it('is exported from the inspection and used by the analysis, not reimplemented', () => {
    const analysis = readFileSync(join(process.cwd(), 'src', 'graph', 'analyze.ts'), 'utf8');
    expect(analysis).toContain("from './inspect'");
    expect(analysis).not.toContain('function deriveProfileEvidence');
    expect(analysis).not.toContain('function mergeDeclarations');
  });
});
