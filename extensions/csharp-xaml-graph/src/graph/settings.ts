/**
 * The layout used inside a shape the reader unpacked.
 *
 * Packing a shape means compressing it into a tile, so unpacking it must mean something different
 * from packing it. A chain of types reads top to bottom — each type above the ones it depends on —
 * which is why Layered Down is the default.
 */
export const UNPACKED_LAYOUTS = ['layered-tb', 'layered-lr', 'force', 'radial', 'grid', 'circle'] as const;

export type UnpackedLayout = (typeof UNPACKED_LAYOUTS)[number];

export const UNPACKED_LAYOUT_LABEL: Record<UnpackedLayout, string> = {
  'layered-tb': 'Layered Down',
  'layered-lr': 'Layered Right',
  force: 'Force',
  radial: 'Radial',
  grid: 'Grid',
  circle: 'Circle',
};

export const UNPACKED_LAYOUT_DESCRIPTION: Record<UnpackedLayout, string> = {
  'layered-tb':
    'Each type sits above the types it depends on. Reads as a call chain and fits a narrow band.',
  'layered-lr':
    'Each type sits to the left of the types it depends on. Wide and short, which suits a short chain.',
  force: 'Nodes push apart until the springs settle. Good for a group with no clear direction.',
  radial: 'Layered types sit near the middle and models further out.',
  grid: 'An even grid in declaration order. No dependency shape at all.',
  circle: 'One ring. Only legible for a small group.',
};

export interface GraphSettings {
  /** Layout used inside an unpacked shape. */
  unpackedLayout: UnpackedLayout;
}

export const DEFAULT_GRAPH_SETTINGS: GraphSettings = {
  unpackedLayout: 'layered-tb',
};

/** Accepts whatever the configuration holds and falls back to the default for anything unknown. */
export function toUnpackedLayout(value: unknown): UnpackedLayout {
  return UNPACKED_LAYOUTS.includes(value as UnpackedLayout)
    ? (value as UnpackedLayout)
    : DEFAULT_GRAPH_SETTINGS.unpackedLayout;
}
