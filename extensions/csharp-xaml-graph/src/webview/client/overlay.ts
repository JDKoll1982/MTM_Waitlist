/**
 * Captions and region frames painted over the canvas.
 *
 * Cytoscape draws the graph itself, but it has no text-only element, and a compound parent node
 * would change the layout it is meant to annotate. So group captions, the unlinked-types tray and
 * module boundaries are absolutely-positioned DOM elements layered over the canvas and translated
 * by the canvas's own zoom and pan.
 *
 * The layer is repainted on every pan and zoom frame, so a paint must only touch what changed: the
 * node for an id is created once and then reused, and nothing is written to the DOM twice.
 */

/** A labelled region painted over the canvas. Coordinates are model coordinates. */
export interface OverlayFrame {
  /** Stable identity, so a repaint reuses the same DOM node instead of recreating it. */
  id: string;
  /** Caption shown at the top-left of the frame. Empty draws a border-only frame. */
  label: string;
  x: number;
  y: number;
  width: number;
  height: number;
  /** Visual treatment: a packed group, the unlinked-types tray, or a module boundary. */
  tone: 'group' | 'island' | 'module';
}

/** The canvas transform: model coordinates to rendered pixels. */
export interface OverlayTransform {
  zoom: number;
  panX: number;
  panY: number;
}

export interface OverlaySize {
  width: number;
  height: number;
}

export interface Overlay {
  /**
   * Draws exactly these frames. Frames outside the visible area are not drawn, and any DOM node
   * from a previous paint whose frame is gone is removed.
   */
  paint(frames: readonly OverlayFrame[], transform: OverlayTransform, size: OverlaySize): void;
  /** Removes every frame. */
  clear(): void;
  /** Ids currently drawn, in paint order — for tests and diagnostics. */
  visibleIds(): string[];
}

const LAYER_CLASS = 'overlay-layer';
const FRAME_CLASS = 'overlay-frame';
const LABEL_CLASS = 'overlay-label';

/**
 * Slack kept around the viewport while culling, in rendered pixels.
 *
 * A frame whose edge is just past the viewport is still worth drawing: without the margin the frame
 * would blink out while its own border is half visible, and a caption that straddles the edge would
 * vanish before the reader could read it.
 */
const CULL_MARGIN = 32;

/**
 * Back-to-front stacking per tone: a module boundary is the widest region and frames its groups, so
 * it is painted first and ends up behind them.
 */
const TONE_ORDER: Record<OverlayFrame['tone'], number> = { module: 0, group: 1, island: 2 };

interface RenderedBox {
  left: number;
  top: number;
  width: number;
  height: number;
}

interface PaintedFrame {
  el: HTMLDivElement;
  labelEl: HTMLSpanElement | undefined;
  tone: OverlayFrame['tone'];
  /** The label last written, so an unchanged caption is not rewritten on every pan frame. */
  text: string;
}

/**
 * The frame's rendered box, or nothing when the frame cannot be drawn.
 *
 * A degenerate frame must draw nothing rather than `NaNpx`: a single bad coordinate reaching the
 * style sheet would poison the whole layer, and the canvas transform arrives from a live user
 * gesture where a zero zoom happens on the first frame before the graph is fitted.
 */
function project(frame: OverlayFrame, transform: OverlayTransform): RenderedBox | undefined {
  const { x, y, width, height } = frame;
  if (![x, y, width, height].every((value) => Number.isFinite(value))) {
    return undefined;
  }
  if (width <= 0 || height <= 0) {
    return undefined;
  }

  const box: RenderedBox = {
    left: x * transform.zoom + transform.panX,
    top: y * transform.zoom + transform.panY,
    width: width * transform.zoom,
    height: height * transform.zoom,
  };

  // An extreme coordinate can overflow a finite zoom to Infinity, which is just as unusable as NaN.
  return [box.left, box.top, box.width, box.height].every((value) => Number.isFinite(value))
    ? box
    : undefined;
}

/** Whether the frame's own rendered box overlaps the viewport, allowing for the culling margin. */
function onScreen(box: RenderedBox, size: OverlaySize): boolean {
  return box.left + box.width >= -CULL_MARGIN
    && box.left <= size.width + CULL_MARGIN
    && box.top + box.height >= -CULL_MARGIN
    && box.top <= size.height + CULL_MARGIN;
}

/**
 * Whether the transform and viewport can produce pixels at all.
 *
 * Zero and negative zoom are the states cytoscape reports before the first fit and while the panel
 * is collapsed; drawing at either would stack every frame on the origin for a frame.
 */
function usableView(transform: OverlayTransform, size: OverlaySize): boolean {
  return transform.zoom > 0
    && Number.isFinite(transform.zoom)
    && Number.isFinite(transform.panX)
    && Number.isFinite(transform.panY)
    && Number.isFinite(size.width)
    && Number.isFinite(size.height)
    && size.width > 0
    && size.height > 0;
}

/**
 * Stacking order: tone, then the larger region, then id.
 *
 * Area orders the frames inside a tone so a region never covers a smaller one drawn over it, and
 * the id tie-break keeps two identically-sized frames in the same order on every paint — otherwise
 * the DOM would be reordered on every pan frame for no visible reason.
 */
function paintOrder(left: OverlayFrame, right: OverlayFrame): number {
  return TONE_ORDER[left.tone] - TONE_ORDER[right.tone]
    || right.width * right.height - left.width * left.height
    || left.id.localeCompare(right.id);
}

/**
 * Creates the overlay layer inside `host`.
 * The layer must not intercept pointer events, so the canvas underneath stays fully usable.
 */
export function createOverlay(host: HTMLElement): Overlay {
  const layer = document.createElement('div');
  layer.className = LAYER_CLASS;
  // Set through the style attribute as well as the class: the webview's CSP can serve the
  // stylesheet after this script runs, and an interactive layer would swallow every pan and click
  // in the meantime.
  layer.style.position = 'absolute';
  layer.style.left = '0';
  layer.style.top = '0';
  layer.style.right = '0';
  layer.style.bottom = '0';
  layer.style.pointerEvents = 'none';
  host.appendChild(layer);

  const nodes = new Map<string, PaintedFrame>();
  let order: string[] = [];

  const nodeFor = (frame: OverlayFrame): PaintedFrame => {
    const existing = nodes.get(frame.id);
    if (existing) {
      return existing;
    }
    const el = document.createElement('div');
    el.className = `${FRAME_CLASS} ${FRAME_CLASS}-${frame.tone}`;
    el.style.position = 'absolute';
    el.style.pointerEvents = 'none';
    const entry: PaintedFrame = { el, labelEl: undefined, tone: frame.tone, text: '' };
    nodes.set(frame.id, entry);
    return entry;
  };

  const apply = (frame: OverlayFrame, entry: PaintedFrame, box: RenderedBox): void => {
    if (entry.tone !== frame.tone) {
      entry.el.className = `${FRAME_CLASS} ${FRAME_CLASS}-${frame.tone}`;
      entry.tone = frame.tone;
    }

    entry.el.style.left = `${Math.round(box.left)}px`;
    entry.el.style.top = `${Math.round(box.top)}px`;
    entry.el.style.width = `${Math.round(box.width)}px`;
    entry.el.style.height = `${Math.round(box.height)}px`;

    if (entry.text === frame.label) {
      return;
    }
    entry.text = frame.label;

    if (frame.label === '') {
      entry.labelEl?.remove();
      entry.labelEl = undefined;
      return;
    }

    if (!entry.labelEl) {
      const span = document.createElement('span');
      span.className = LABEL_CLASS;
      span.style.pointerEvents = 'none';
      entry.el.appendChild(span);
      entry.labelEl = span;
    }
    // A type name can contain `<`; textContent cannot become markup, innerHTML can.
    entry.labelEl.textContent = frame.label;
  };

  const clear = (): void => {
    for (const entry of nodes.values()) {
      entry.el.remove();
    }
    nodes.clear();
    order = [];
  };

  const paint = (
    frames: readonly OverlayFrame[],
    transform: OverlayTransform,
    size: OverlaySize,
  ): void => {
    if (frames.length === 0 || !usableView(transform, size)) {
      clear();
      return;
    }

    const drawn = new Set<string>();
    const visible: { frame: OverlayFrame; box: RenderedBox }[] = [];

    // Sorting first makes the id de-duplication deterministic: a repeated id keeps the frame the
    // paint order puts on top, not whichever one happens to come first in the caller's array.
    for (const frame of [...frames].sort(paintOrder)) {
      if (drawn.has(frame.id)) {
        continue;
      }
      const box = project(frame, transform);
      if (!box || !onScreen(box, size)) {
        continue;
      }
      drawn.add(frame.id);
      visible.push({ frame, box });
    }

    for (const [id, entry] of [...nodes]) {
      if (!drawn.has(id)) {
        entry.el.remove();
        nodes.delete(id);
      }
    }

    order = [];
    for (const { frame, box } of visible) {
      const entry = nodeFor(frame);
      apply(frame, entry, box);
      // Appending an existing child moves it, so paint order is fixed without recreating nodes.
      layer.appendChild(entry.el);
      order.push(frame.id);
    }
  };

  return {
    paint,
    clear,
    visibleIds: (): string[] => [...order],
  };
}
