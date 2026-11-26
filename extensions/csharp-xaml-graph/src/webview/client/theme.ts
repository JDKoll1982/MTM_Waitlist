/** Colours the canvas needs, resolved from the webview's CSS custom properties. */
export interface CanvasTheme {
  /** Node label text. */
  label: string;
  /** Halo drawn behind a label so it stays readable over a node. */
  labelOutline: string;
  /** Default node border. */
  nodeBorder: string;
  /** Default edge line and arrow. */
  edge: string;
  /** Low-emphasis edges and structural lines. */
  edgeMuted: string;
  /** Focus and hover strokes. */
  highlight: string;
  /** Tint behind a highlighted region. */
  underlay: string;
  /** Canvas background, used by the overview so it matches the panel. */
  background: string;
  /** True when the resolved background reads as light. */
  light: boolean;
}

type Read = (property: string) => string;

interface Rgb {
  readonly r: number;
  readonly g: number;
  readonly b: number;
}

/** A colour as the theme declared it, plus the channels the light/dark decision is made from. */
interface Resolved {
  value: string;
  rgb: Rgb;
}

const BLACK: Rgb = { r: 0, g: 0, b: 0 };
const WHITE: Rgb = { r: 255, g: 255, b: 255 };

/**
 * The palette the canvas drew with before it followed the theme, kept as channels so the light-theme
 * derivations work from the same numbers. Every fallback comes from here: a webview that declares no
 * custom properties — a theme switch mid-redraw, a panel that has not attached yet, the test
 * harness — has to look exactly as it did before.
 */
const DARK = {
  background: { r: 0x1e, g: 0x1e, b: 0x1e },
  label: { r: 0xe8, g: 0xe8, b: 0xe8 },
  labelOutline: { r: 0x0d, g: 0x0d, b: 0x0d },
  nodeBorder: { r: 0x14, g: 0x14, b: 0x14 },
  edge: { r: 0x6b, g: 0x6b, b: 0x6b },
  edgeMuted: { r: 0x8a, g: 0x8a, b: 0x8a },
  highlight: { r: 0xff, g: 0xff, b: 0xff },
  underlay: { r: 0x4a, g: 0x9e, b: 0xff },
};

/** Where each canvas colour comes from. The border chain is ordered: the first declared value wins. */
const SOURCE = {
  background: ['--vscode-editor-background'],
  label: ['--vscode-editor-foreground'],
  highlight: ['--vscode-focusBorder'],
  border: ['--vscode-editorWidget-border', '--vscode-panel-border'],
  muted: ['--vscode-descriptionForeground'],
} as const;

/** The threshold either side of which a background is called light or dark. */
const LIGHT_THRESHOLD = 0.5;

function clamp(value: number): number {
  return Math.min(255, Math.max(0, value));
}

/**
 * Parses the colour forms a VS Code theme actually declares: `#rgb`, `#rrggbb`, `rgb(...)` and
 * `rgba(...)`. Anything else returns null so the caller can fall back — a theme value the canvas
 * cannot reason about must not become a stroke it cannot draw.
 */
function parseColour(value: string): Rgb | null {
  const text = value.trim().toLowerCase();

  const hex = /^#([0-9a-f]{3}|[0-9a-f]{6})$/.exec(text);
  if (hex) {
    const digits = hex[1].length === 3
      ? [...hex[1]].map((digit) => digit + digit).join('')
      : hex[1];
    return {
      r: parseInt(digits.slice(0, 2), 16),
      g: parseInt(digits.slice(2, 4), 16),
      b: parseInt(digits.slice(4, 6), 16),
    };
  }

  const functional = /^rgba?\(([^()]*)\)$/.exec(text);
  if (!functional) {
    return null;
  }
  const parts = functional[1].split(/[\s,/]+/).filter((part) => part.length > 0);
  if (parts.length < 3 || parts.length > 4) {
    return null;
  }
  const channels: number[] = [];
  for (const part of parts.slice(0, 3)) {
    const channel = Number(part);
    if (!Number.isFinite(channel)) {
      return null;
    }
    channels.push(clamp(Math.round(channel)));
  }
  return { r: channels[0], g: channels[1], b: channels[2] };
}

/**
 * WCAG relative luminance of an sRGB colour, used to decide whether the canvas is on a light or a
 * dark theme. Alpha is ignored: a theme colour is opaque, and an overlay's alpha says nothing about
 * the background it will be drawn on.
 */
function luminance(colour: Rgb): number {
  const channel = (value: number): number => {
    const fraction = value / 255;
    return fraction <= 0.03928 ? fraction / 12.92 : ((fraction + 0.055) / 1.055) ** 2.4;
  };
  return 0.2126 * channel(colour.r) + 0.7152 * channel(colour.g) + 0.0722 * channel(colour.b);
}

function toHex(colour: Rgb): string {
  const pair = (value: number): string => clamp(Math.round(value)).toString(16).padStart(2, '0');
  return `#${pair(colour.r)}${pair(colour.g)}${pair(colour.b)}`;
}

/** Blends `from` towards `to` by `weight`, 0 keeping `from` and 1 returning `to`. */
function blend(from: Rgb, to: Rgb, weight: number): string {
  const mix = (a: number, b: number): number => a + (b - a) * weight;
  return toHex({ r: mix(from.r, to.r), g: mix(from.g, to.g), b: mix(from.b, to.b) });
}

/** The first colour `properties` declares, or null when none of them declares one. */
function declared(read: Read, properties: readonly string[]): Resolved | null {
  for (const property of properties) {
    const value = read(property).trim();
    const rgb = parseColour(value);
    if (rgb) {
      return { value, rgb };
    }
  }
  return null;
}

function solid(rgb: Rgb): Resolved {
  return { value: toHex(rgb), rgb };
}

/**
 * The label colour on a light background: the theme's own foreground when it is dark enough to read,
 * otherwise a near-black tint of that background. Deriving the second case keeps a custom light theme's
 * hue instead of painting a hardcoded black over it.
 */
function lightLabel(background: Rgb, foreground: Resolved): string {
  return luminance(foreground.rgb) <= LIGHT_THRESHOLD ? foreground.value : blend(background, BLACK, 0.95);
}

/**
 * Reads one custom property from the document body, where VS Code injects the active theme.
 *
 * Every step is guarded because the canvas has to survive a webview with no document at all (a unit
 * test, a panel that has not attached yet). An absent body or an absent `getComputedStyle` reads as
 * "nothing declared", which sends every value to the dark palette.
 */
function bodyProperty(property: string): string {
  if (typeof document === 'undefined' || !document.body) {
    return '';
  }
  const style = typeof getComputedStyle === 'function' ? getComputedStyle(document.body) : null;
  return style ? style.getPropertyValue(property) : '';
}

/**
 * Resolves the palette from a style declaration.
 * `read` defaults to the document's computed body style when available.
 *
 * A blank property, or one whose value is not a colour this module parses, counts as absent and
 * takes the dark fallback. On a light background the text pair is inverted — a deeply dark label and
 * a near-white halo — because a pale label on a pale canvas is unreadable in the other direction.
 * The accent colours are left alone: the theme already declares them legible against its own
 * background.
 */
export function readTheme(read: Read = bodyProperty): CanvasTheme {
  const background = declared(read, SOURCE.background) ?? solid(DARK.background);
  const foreground = declared(read, SOURCE.label) ?? solid(DARK.label);
  const highlight = declared(read, SOURCE.highlight) ?? solid(DARK.highlight);
  const border = declared(read, SOURCE.border);
  const muted = declared(read, SOURCE.muted) ?? solid(DARK.edgeMuted);
  const light = luminance(background.rgb) > LIGHT_THRESHOLD;

  return {
    background: background.value,
    light,
    label: light ? lightLabel(background.rgb, foreground) : foreground.value,
    labelOutline: light ? blend(background.rgb, WHITE, 0.9) : toHex(DARK.labelOutline),
    nodeBorder: (border ?? solid(DARK.nodeBorder)).value,
    edge: (border ?? solid(DARK.edge)).value,
    edgeMuted: muted.value,
    highlight: highlight.value,
    underlay: toHex(DARK.underlay),
  };
}
