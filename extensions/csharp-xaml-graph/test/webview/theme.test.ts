import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { readTheme } from '../../src/webview/client/theme';

/** What a webview that declares no custom properties must resolve to, exactly. */
const DARK_PALETTE = {
  label: '#e8e8e8',
  labelOutline: '#0d0d0d',
  nodeBorder: '#141414',
  edge: '#6b6b6b',
  edgeMuted: '#8a8a8a',
  highlight: '#ffffff',
  underlay: '#4a9eff',
  background: '#1e1e1e',
  light: false,
};

/** A reader over a fixed set of declarations that records every property it was asked for. */
const reader = (properties: Record<string, string> = {}) => {
  const asked: string[] = [];
  const read = (property: string): string => {
    asked.push(property);
    return properties[property] ?? '';
  };
  return { asked, read };
};

describe('readTheme: fallbacks', () => {
  it('returns the dark palette when no property is declared', () => {
    expect(readTheme(reader().read)).toEqual(DARK_PALETTE);
  });

  it('treats a whitespace-only value as absent', () => {
    const read = (): string => '   ';
    expect(readTheme(read)).toEqual(DARK_PALETTE);
  });

  it('falls back when a declared colour cannot be parsed', () => {
    const read = (): string => 'not-a-colour';
    expect(readTheme(read)).toEqual(DARK_PALETTE);
  });

  it('falls back when the document declares a malformed rgb()', () => {
    const read = (): string => 'rgb(1, 2)';
    expect(readTheme(read)).toEqual(DARK_PALETTE);
  });

  it('falls back when a hex has the wrong number of digits', () => {
    const read = (): string => '#ffff';
    expect(readTheme(read)).toEqual(DARK_PALETTE);
  });

  it('falls back when a channel is not a number', () => {
    const read = (): string => 'rgb(red, 0, 0)';
    expect(readTheme(read)).toEqual(DARK_PALETTE);
  });

  it('falls back when rgb() carries too many parts', () => {
    const read = (): string => 'rgba(1, 2, 3, 4, 5)';
    expect(readTheme(read)).toEqual(DARK_PALETTE);
  });
});

describe('readTheme: light and dark backgrounds', () => {
  it('reports a light background as light and inverts the label pair', () => {
    const theme = readTheme(reader({ '--vscode-editor-background': '#ffffff' }).read);
    expect(theme.light).toBe(true);
    expect(theme.label).toBe('#0d0d0d');
    expect(theme.labelOutline).toBe('#ffffff');
  });

  it('keeps a theme foreground that is already dark on a light background', () => {
    const theme = readTheme(reader({
      '--vscode-editor-background': '#ffffff',
      '--vscode-editor-foreground': '#111111',
    }).read);
    expect(theme.light).toBe(true);
    expect(theme.label).toBe('#111111');
    expect(theme.labelOutline).toBe('#ffffff');
  });

  it('reports a dark background as dark and keeps the pale label', () => {
    const theme = readTheme(reader({ '--vscode-editor-background': '#202020' }).read);
    expect(theme.light).toBe(false);
    expect(theme.label).toBe(DARK_PALETTE.label);
    expect(theme.labelOutline).toBe(DARK_PALETTE.labelOutline);
  });

  it('derives the label pair from a tinted light background', () => {
    const theme = readTheme(reader({ '--vscode-editor-background': '#fdf6e3' }).read);
    expect(theme.light).toBe(true);
    expect(theme.label).toBe('#0d0c0b');
    expect(theme.labelOutline).toBe('#fffefc');
  });

  it('treats pure black as a dark background', () => {
    const theme = readTheme(reader({ '--vscode-editor-background': '#000000' }).read);
    expect(theme.light).toBe(false);
    expect(theme.label).toBe(DARK_PALETTE.label);
  });
});

describe('readTheme: property sources', () => {
  it('asks for every documented property, in order', () => {
    const { asked, read } = reader();
    readTheme(read);
    expect(asked).toEqual([
      '--vscode-editor-background',
      '--vscode-editor-foreground',
      '--vscode-focusBorder',
      '--vscode-editorWidget-border',
      '--vscode-panel-border',
      '--vscode-descriptionForeground',
    ]);
  });

  it('stops asking once the widget border is declared', () => {
    const { asked, read } = reader({ '--vscode-editorWidget-border': '#454545' });
    readTheme(read);
    expect(asked).not.toContain('--vscode-panel-border');
  });

  it('prefers the widget border over the panel border for the node and edge colour', () => {
    const theme = readTheme(reader({
      '--vscode-editorWidget-border': '#454545',
      '--vscode-panel-border': '#333333',
    }).read);
    expect(theme.nodeBorder).toBe('#454545');
    expect(theme.edge).toBe('#454545');
  });

  it('uses the panel border for the node and edge colour when the widget border is absent', () => {
    const theme = readTheme(reader({ '--vscode-panel-border': '#333333' }).read);
    expect(theme.nodeBorder).toBe('#333333');
    expect(theme.edge).toBe('#333333');
  });

  it('resolves the highlight from the focus border', () => {
    const theme = readTheme(reader({ '--vscode-focusBorder': '#0078d4' }).read);
    expect(theme.highlight).toBe('#0078d4');
  });

  it('resolves muted lines from the description foreground', () => {
    const theme = readTheme(reader({ '--vscode-descriptionForeground': '#9d9d9d' }).read);
    expect(theme.edgeMuted).toBe('#9d9d9d');
  });

  it('resolves the label from the editor foreground on a dark background', () => {
    const theme = readTheme(reader({
      '--vscode-editor-background': '#1e1e1e',
      '--vscode-editor-foreground': '#cccccc',
    }).read);
    expect(theme.label).toBe('#cccccc');
  });

  it('trims a padded property value', () => {
    const theme = readTheme(reader({ '--vscode-focusBorder': '  #ff0000  ' }).read);
    expect(theme.highlight).toBe('#ff0000');
  });
});

describe('readTheme: colour forms', () => {
  it('accepts #rgb', () => {
    const theme = readTheme(reader({
      '--vscode-editor-background': '#fff',
      '--vscode-focusBorder': '#f00',
    }).read);
    expect(theme.light).toBe(true);
    expect(theme.background).toBe('#fff');
    expect(theme.highlight).toBe('#f00');
  });

  it('accepts #rrggbb', () => {
    const theme = readTheme(reader({ '--vscode-editor-background': '#1e1e1e' }).read);
    expect(theme.light).toBe(false);
    expect(theme.background).toBe('#1e1e1e');
  });

  it('accepts rgb()', () => {
    const theme = readTheme(reader({ '--vscode-editor-background': 'rgb(255, 255, 255)' }).read);
    expect(theme.light).toBe(true);
    expect(theme.background).toBe('rgb(255, 255, 255)');
  });

  it('accepts rgba()', () => {
    const theme = readTheme(reader({ '--vscode-editor-background': 'rgba(250, 250, 250, 0.8)' }).read);
    expect(theme.light).toBe(true);
    expect(theme.background).toBe('rgba(250, 250, 250, 0.8)');
  });

  it('accepts a functional colour with space-separated channels', () => {
    const theme = readTheme(reader({ '--vscode-editor-background': 'rgb(20 20 20)' }).read);
    expect(theme.light).toBe(false);
  });

  it('clamps a channel above 255', () => {
    const theme = readTheme(reader({ '--vscode-editor-background': 'rgb(300, 300, 300)' }).read);
    expect(theme.light).toBe(true);
  });
});

describe('readTheme: the default reader', () => {
  beforeEach(() => {
    document.body.removeAttribute('style');
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('works with no reader in jsdom and returns the complete palette', () => {
    expect(readTheme()).toEqual(DARK_PALETTE);
  });

  it('reads a custom property from the body when one is set', () => {
    document.body.style.setProperty('--vscode-editor-background', '#ffffff');
    const theme = readTheme();
    expect(theme.light).toBe(true);
    expect(theme.background).toBe('#ffffff');
  });

  it('falls back when the environment has no computed style', () => {
    vi.stubGlobal('getComputedStyle', undefined);
    expect(readTheme()).toEqual(DARK_PALETTE);
  });

  it('falls back when there is no document at all', () => {
    vi.stubGlobal('document', undefined);
    expect(readTheme()).toEqual(DARK_PALETTE);
  });
});
