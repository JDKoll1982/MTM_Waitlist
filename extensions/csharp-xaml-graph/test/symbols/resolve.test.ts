import { beforeEach, describe, expect, it } from 'vitest';
import { candidatesForFile, candidatesForPosition, workspaceRelative } from '../../src/symbols/resolve';
import type { GraphModel } from '../../src/graph/model';
import { graphModel, graphNode } from '../support/model';
import {
  Location, Position, Range, SymbolKind, Uri, resetVscodeState, state,
} from '../support/vscode';

interface SymbolLike {
  name: string;
  kind: number;
  range: Range;
  children: SymbolLike[];
}

const symbol = (
  name: string,
  kind: number,
  startLine: number,
  endLine: number,
  children: SymbolLike[] = [],
): SymbolLike => ({
  name,
  kind,
  range: new Range(new Position(startLine, 0), new Position(endLine, 120)),
  children,
});

const document = (path: string, word?: string) => ({
  uri: Uri.file(path),
  getWordRangeAtPosition: () => (word ? new Range(new Position(0, 0), new Position(0, word.length)) : undefined),
  getText: () => word ?? '',
  positionAt: (offset: number) => new Position(0, offset),
});

const model: GraphModel = graphModel({
  nodes: [
    graphNode({ id: 'N.Views.SamplePage', label: 'SamplePage', kind: 'page', module: 'Module_A', location: { file: 'N/Views/SamplePage.cs', line: 10 } }),
    graphNode({ id: 'N.Views.Second', label: 'Second', kind: 'class', module: 'Module_A', location: { file: 'N/Views/SamplePage.cs', line: 40 } }),
    graphNode({ id: 'N.SampleViewModel', label: 'SampleViewModel', layer: 'viewmodel', module: 'Core', location: { file: 'N/SampleViewModel.cs', line: 5 } }),
    graphNode({ id: 'N.Markup', label: 'Markup', layer: 'view', module: 'Module_A', xamlFile: 'N/Views/Markup.xaml' }),
    graphNode({ id: 'N.Deeper.SampleViewModel', label: 'SampleViewModel', layer: 'viewmodel', module: 'Core', location: { file: 'N/Deeper.cs', line: 5 } }),
  ],
});

beforeEach(() => {
  for (const panel of [...state.panels]) {
    panel.dispose();
  }
  resetVscodeState();
  state.folders = [{ uri: Uri.file('/work'), name: 'work', index: 0 }];
});

describe('workspaceRelative', () => {
  it('strips the workspace folder and uses forward slashes', () => {
    expect(workspaceRelative(Uri.file('/work/N/Thing.cs'))).toBe('N/Thing.cs');
  });

  it('uses the first folder when the file is outside every folder', () => {
    expect(workspaceRelative(Uri.file('/elsewhere/N/Thing.cs'))).toBe('/elsewhere/N/Thing.cs');
  });

  it('falls back to the absolute path with forward slashes when no folder is open', () => {
    state.folders = [];
    expect(workspaceRelative(Uri.file('C:\\src\\N\\Thing.cs'))).toBe('C:/src/N/Thing.cs');
  });
});

describe('candidatesForFile', () => {
  it('returns nothing for a file the model does not place', () => {
    expect(candidatesForFile(Uri.file('/work/N/Unknown.cs'), model)).toEqual([]);
  });

  it('returns one candidate for a file that declares one type', () => {
    const [candidate] = candidatesForFile(Uri.file('/work/N/SampleViewModel.cs'), model);
    expect(candidate.label).toBe('SampleViewModel');
    expect(candidate.description).toBe('class · Core');
    expect(candidate.detail).toBe('SampleViewModel.cs · N/SampleViewModel.cs:5');
    expect(candidate.request).toEqual({
      title: 'SampleViewModel',
      nodeIds: ['N.SampleViewModel'],
      focus: 'N.SampleViewModel',
      member: undefined,
      depth: 1,
    });
  });

  it('offers the whole file first, then each type in source order', () => {
    const candidates = candidatesForFile(Uri.file('/work/N/Views/SamplePage.cs'), model);
    expect(candidates.map((candidate) => candidate.label)).toEqual([
      'Whole file — 2 types', 'SamplePage', 'Second',
    ]);
    expect(candidates[0].request.nodeIds).toEqual(['N.Views.SamplePage', 'N.Views.Second']);
    expect(candidates[0].request.focus).toBe('N.Views.SamplePage');
    expect(candidates[0].request.depth).toBe(1);
  });

  it('finds a view by the markup file it was declared in', () => {
    const [candidate] = candidatesForFile(Uri.file('/work/N/Views/Markup.xaml'), model);
    expect(candidate.request.nodeIds).toEqual(['N.Markup']);
    expect(candidate.detail).toBe('Markup.xaml · Module_A');
  });
});

describe('candidatesForPosition: file and symbol resolution', () => {
  it('delegates a XAML position to the file candidates', async () => {
    const candidates = await candidatesForPosition(
      document('/work/N/Views/Markup.xaml') as never, new Position(0, 0), model,
    );
    expect(candidates.map((candidate) => candidate.label)).toEqual(['Markup']);
  });

  it('uses the symbol tree to name the enclosing type and member', async () => {
    state.commandHandlers['vscode.executeDocumentSymbolProvider'] = (uri) => {
      expect((uri as Uri).path).toBe('/work/N/Views/SamplePage.cs');
      return [
        symbol('SamplePage', SymbolKind.Class, 9, 30, [
          symbol('Loaded', SymbolKind.Method, 12, 18),
        ]),
      ];
    };

    const candidates = await candidatesForPosition(
      document('/work/N/Views/SamplePage.cs', 'value') as never,
      new Position(14, 10),
      model,
    );

    const enclosing = candidates.find((candidate) => candidate.detail.startsWith('selected in the editor'));
    expect(enclosing?.label).toBe('SamplePage');
    expect(enclosing?.request).toMatchObject({ title: 'SamplePage.Loaded', member: 'Loaded', focus: 'N.Views.SamplePage' });
    expect(enclosing?.detail).toBe('selected in the editor — member Loaded · N/Views/SamplePage.cs:10');
  });

  it('ignores a symbol provider that throws', async () => {
    state.commandHandlers['vscode.executeDocumentSymbolProvider'] = () => {
      throw new Error('no symbols');
    };
    state.commandHandlers['vscode.executeDefinitionProvider'] = () => {
      throw new Error('no definitions');
    };

    const candidates = await candidatesForPosition(
      document('/work/N/SampleViewModel.cs', 'SampleViewModel') as never,
      new Position(0, 0),
      model,
    );

    expect(candidates.map((candidate) => candidate.label)).toEqual([
      'SampleViewModel', 'SampleViewModel',
    ]);
  });

  it('follows a definition link to the type it names', async () => {
    state.commandHandlers['vscode.executeDocumentSymbolProvider'] = (uri) => {
      const path = (uri as Uri).path;
      if (path === '/work/N/Views/SamplePage.cs') {
        return [symbol('SamplePage', SymbolKind.Class, 9, 30)];
      }
      return [symbol('SampleViewModel', SymbolKind.Class, 4, 20, [
        symbol('Refresh', SymbolKind.Method, 8, 10),
      ])];
    };
    state.commandHandlers['vscode.executeDefinitionProvider'] = () => [
      { targetUri: Uri.file('/work/N/SampleViewModel.cs'), targetRange: new Range(new Position(8, 0), new Position(8, 10)) },
    ];

    const candidates = await candidatesForPosition(
      document('/work/N/Views/SamplePage.cs', 'SampleViewModel') as never,
      new Position(14, 10),
      model,
    );

    const fromDefinition = candidates.find((candidate) => candidate.detail.startsWith('go to definition'));
    expect(fromDefinition?.request).toMatchObject({
      title: 'SampleViewModel.Refresh',
      nodeIds: ['N.SampleViewModel'],
      member: 'Refresh',
    });
  });

  it('accepts a plain location as well as a definition link', async () => {
    state.commandHandlers['vscode.executeDefinitionProvider'] = () => [
      new Location(Uri.file('/work/N/SampleViewModel.cs'), new Range(new Position(4, 0), new Position(4, 10))),
    ];

    const candidates = await candidatesForPosition(
      document('/work/N/Views/SamplePage.cs') as never,
      new Position(14, 10),
      model,
    );
    expect(candidates.some((candidate) => candidate.label === 'SampleViewModel')).toBe(true);
  });

  it('picks the nearest declaration above the target when the file has several types', async () => {
    state.commandHandlers['vscode.executeDefinitionProvider'] = () => [
      new Location(Uri.file('/work/N/Views/SamplePage.cs'), new Range(new Position(45, 0), new Position(45, 5))),
    ];

    const candidates = await candidatesForPosition(
      document('/work/N/Views/SamplePage.cs') as never,
      new Position(50, 0),
      model,
    );
    const fromDefinition = candidates.find((candidate) => candidate.detail.startsWith('go to definition'));
    expect(fromDefinition?.label).toBe('Second');
  });

  it('falls back to the first declaration when the target is above every type', async () => {
    state.commandHandlers['vscode.executeDefinitionProvider'] = () => [
      new Location(Uri.file('/work/N/Views/SamplePage.cs'), new Range(new Position(1, 0), new Position(1, 5))),
    ];

    const candidates = await candidatesForPosition(
      document('/work/N/Views/SamplePage.cs') as never,
      new Position(2, 0),
      model,
    );
    expect(candidates.some((candidate) => candidate.label === 'SamplePage')).toBe(true);
  });

  it('ignores a definition in a file the model does not place', async () => {
    state.commandHandlers['vscode.executeDefinitionProvider'] = () => [
      new Location(Uri.file('/work/N/Elsewhere.cs'), new Range(new Position(1, 0), new Position(1, 5))),
    ];

    const candidates = await candidatesForPosition(
      document('/work/N/Views/SamplePage.cs') as never,
      new Position(2, 0),
      model,
    );
    expect(candidates).toEqual([]);
  });
});

describe('candidatesForPosition: name matching', () => {
  it('matches a label exactly', async () => {
    const candidates = await candidatesForPosition(
      document('/work/x.cs', 'SamplePage') as never, new Position(0, 0), model,
    );
    expect(candidates.map((candidate) => candidate.label)).toEqual(['SamplePage']);
    expect(candidates[0].detail).toContain('name match');
  });

  it('matches a full name exactly', async () => {
    const candidates = await candidatesForPosition(
      document('/work/x.cs', 'N.SampleViewModel') as never, new Position(0, 0), model,
    );
    expect(candidates.map((candidate) => candidate.request.nodeIds)).toEqual([['N.SampleViewModel']]);
  });

  it('falls back to a case-insensitive label match when nothing matches exactly', async () => {
    const candidates = await candidatesForPosition(
      document('/work/x.cs', 'samplepage') as never, new Position(0, 0), model,
    );
    expect(candidates.map((candidate) => candidate.label)).toEqual(['SamplePage']);
  });

  it('returns nothing for a word the model does not carry', async () => {
    const candidates = await candidatesForPosition(
      document('/work/x.cs', 'Nothing') as never, new Position(0, 0), model,
    );
    expect(candidates).toEqual([]);
  });

  it('returns nothing when the cursor is on no word', async () => {
    const candidates = await candidatesForPosition(
      document('/work/x.cs') as never, new Position(0, 0), model,
    );
    expect(candidates).toEqual([]);
  });

  it('dedupes a type found twice and prefers the candidate that knows the member', async () => {
    state.commandHandlers['vscode.executeDocumentSymbolProvider'] = () => [
      symbol('SampleViewModel', SymbolKind.Class, 4, 30, [symbol('Refresh', SymbolKind.Method, 8, 10)]),
    ];

    const candidates = await candidatesForPosition(
      document('/work/N/SampleViewModel.cs', 'SampleViewModel') as never,
      new Position(9, 0),
      model,
    );

    // Two namespaces declare this name, so two candidates remain; the one in this file knows the member.
    expect(candidates).toHaveLength(2);
    const inThisFile = candidates.find((candidate) => candidate.request.nodeIds[0] === 'N.SampleViewModel');
    expect(inThisFile?.request.member).toBe('Refresh');
  });

  it('keeps the model’s order when two candidates share a label', async () => {
    const candidates = await candidatesForPosition(
      document('/work/x.cs', 'SampleViewModel') as never, new Position(0, 0), model,
    );
    expect(candidates.map((candidate) => candidate.label)).toEqual(['SampleViewModel', 'SampleViewModel']);
    expect(candidates.map((candidate) => candidate.request.nodeIds[0])).toEqual([
      'N.SampleViewModel', 'N.Deeper.SampleViewModel',
    ]);
  });
});

describe('candidatesForPosition: symbol and definition corners', () => {
  it('walks past a sibling symbol whose range does not contain the cursor', async () => {
    state.commandHandlers['vscode.executeDocumentSymbolProvider'] = () => [
      symbol('First', SymbolKind.Class, 1, 5),
      symbol('Second', SymbolKind.Class, 10, 30, [symbol('Run', SymbolKind.Method, 12, 14)]),
    ];

    const candidates = await candidatesForPosition(
      document('/work/N/Views/SamplePage.cs') as never,
      new Position(13, 0),
      model,
    );
    const enclosing = candidates.find((candidate) => candidate.detail.startsWith('selected in the editor'));
    expect(enclosing?.request.member).toBe('Run');
  });

  it('resolves a target outside every workspace folder against the file itself', async () => {
    state.folders = [];
    const absolute = graphModel({
      nodes: [graphNode({ id: 'N.SampleViewModel', label: 'SampleViewModel', location: { file: '/work/N/SampleViewModel.cs', line: 5 } })],
    });
    state.commandHandlers['vscode.executeDefinitionProvider'] = () => [
      new Location(Uri.file('/work/N/SampleViewModel.cs'), new Range(new Position(4, 0), new Position(4, 5))),
    ];

    const candidates = await candidatesForPosition(
      document('/work/N/Views/SamplePage.cs') as never,
      new Position(9, 0),
      absolute,
    );
    expect(candidates.some((candidate) => candidate.label === 'SampleViewModel')).toBe(true);
  });

  it('orders several types in one markup file, and a type with no source line first', async () => {
    const many = graphModel({
      nodes: [
        graphNode({ id: 'N.Second', label: 'Second', location: { file: 'N/Two.cs', line: 40 } }),
        graphNode({ id: 'N.First', label: 'First', location: { file: 'N/Two.cs', line: 10 } }),
        graphNode({ id: 'N.MarkupOnly', label: 'MarkupOnly', xamlFile: 'N/Two.cs' }),
      ],
    });
    const candidates = candidatesForFile(Uri.file('/work/N/Two.cs'), many);
    expect(candidates.map((candidate) => candidate.label)).toEqual([
      'Whole file — 3 types', 'MarkupOnly', 'First', 'Second',
    ]);
  });

  it('picks the nearest declaration above a target with no line of its own', async () => {
    const mixed = graphModel({
      nodes: [
        graphNode({ id: 'N.MarkupOnly', label: 'MarkupOnly', xamlFile: 'N/Two.cs' }),
        graphNode({ id: 'N.Later', label: 'Later', location: { file: 'N/Two.cs', line: 30 } }),
      ],
    });
    state.commandHandlers['vscode.executeDefinitionProvider'] = () => [
      new Location(Uri.file('/work/N/Two.cs'), new Range(new Position(20, 0), new Position(20, 5))),
    ];

    const candidates = await candidatesForPosition(
      document('/work/N/Two.cs') as never,
      new Position(20, 0),
      mixed,
    );
    const fromDefinition = candidates.find((candidate) => candidate.detail.startsWith('go to definition'));
    expect(fromDefinition?.label).toBe('MarkupOnly');
  });

  it('keeps the candidate that already knows the member when the same type is found again', async () => {
    state.commandHandlers['vscode.executeDocumentSymbolProvider'] = () => [
      symbol('SampleViewModel', SymbolKind.Class, 4, 30, [symbol('Refresh', SymbolKind.Method, 8, 10)]),
    ];
    // A model with one node carrying this name, so the symbol hit and the word hit are the same node.
    const single = graphModel({
      nodes: [graphNode({ id: 'N.SampleViewModel', label: 'SampleViewModel', location: { file: 'N/SampleViewModel.cs', line: 5 } })],
    });

    const candidates = await candidatesForPosition(
      document('/work/N/SampleViewModel.cs', 'SampleViewModel') as never,
      new Position(9, 0),
      single,
    );
    expect(candidates).toHaveLength(1);
    expect(candidates[0].request.member).toBe('Refresh');
  });
});
