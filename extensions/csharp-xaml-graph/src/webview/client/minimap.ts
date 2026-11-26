/** One point in the overview. Coordinates are model coordinates. */
export interface MinimapNode {
  x: number;
  y: number;
  /** Node layer, used to pick the dot colour. */
  layer: string;
}

/** Model-coordinate bounds of everything drawn. */
export interface MinimapBounds {
  minX: number;
  minY: number;
  maxX: number;
  maxY: number;
}

/** Model-coordinate rectangle of the visible viewport. */
export interface MinimapViewport {
  x1: number;
  y1: number;
  x2: number;
  y2: number;
}

export interface MinimapOptions {
  nodes: readonly MinimapNode[];
  bounds: MinimapBounds;
  /** Omitted when the canvas is not mounted yet. */
  viewport?: MinimapViewport | null;
  /** Pixel size of the overview box. */
  width: number;
  height: number;
  /** Layer name to fill colour. Unknown layers fall back to a neutral grey. */
  colours: Readonly<Record<string, string>>;
  /** Backdrop so the overview reads as a picture rather than floating dots. */
  background: string;
}

/**
 * Breathing room kept inside the box. A dot at the extreme corner of the bounds would otherwise be
 * drawn half outside the picture, and the overview would look cropped rather than framed.
 */
const INSET = 3;

/**
 * The overview draws one circle per node. Past this many, the dots overlap into a grey smear and the
 * extra elements only cost layout time, so the list is sampled down to this count.
 */
const MAX_DOTS = 4000;

/**
 * Circle radius bounds, in pixels. Below the floor a dot disappears at 100% zoom; above the ceiling a
 * small graph reads as a handful of blobs instead of a chart.
 */
const MIN_RADIUS = 0.6;
const MAX_RADIUS = 2.5;

/**
 * Numerator of the radius rule. The area a dot covers is what the eye reads, so the radius tracks
 * `1 / sqrt(count)`: four times the nodes halves the radius, and total ink stays roughly constant.
 */
const RADIUS_NUMERATOR = 7;

/**
 * Upper bound on the model-to-pixel factor. A graph whose extent is a fraction of a model pixel would
 * otherwise be blown up until two coincident nodes sat at opposite corners of the box, which
 * overstates how far apart they are. Lowering the scale never overflows the box, so a ceiling is safe.
 * There is deliberately no floor: a floor would draw a wide graph wider than the box.
 */
const MAX_SCALE = 1000;

const FALLBACK_COLOUR = '#8a8a8a';

/** Amber reads against both the dark and the light theme backgrounds the panel is opened in. */
const VIEWPORT_STROKE = '#f59e0b';

const ARIA_LABEL = 'Graph overview: the whole dependency graph, with the visible area boxed';

const isPositiveFinite = (value: number): boolean => Number.isFinite(value) && value > 0;

/** Coordinates are rounded so the markup stays short, and two identical inputs stay byte-identical. */
const round2 = (value: number): string => String(Math.round(value * 100) / 100);

/** `NaN`, `Infinity` and `undefined` must never reach the markup: they make an SVG attribute invalid. */
const num = (value: number): string => round2(Number.isFinite(value) ? value : 0);

const clampToBox = (value: number, size: number): number => Math.min(size, Math.max(0, value));

const escapeAttribute = (value: unknown): string => String(value)
  .replace(/&/g, '&amp;')
  .replace(/</g, '&lt;')
  .replace(/>/g, '&gt;')
  .replace(/"/g, '&quot;')
  .replace(/'/g, '&#39;');

/**
 * Colour for a layer. `hasOwn` rather than a plain lookup because a layer named `constructor` would
 * otherwise pick up a function from the prototype chain and be written into the markup.
 */
function colourFor(colours: Readonly<Record<string, string>>, layer: string): string {
  const found = Object.hasOwn(colours, layer) ? colours[layer] : undefined;
  return typeof found === 'string' && found.length > 0 ? found : FALLBACK_COLOUR;
}

/**
 * Evenly spaced sample of at most `MAX_DOTS` nodes. Taking every nth node keeps the sampled points
 * spread across the whole picture, so the overview still shows the graph's shape.
 */
function sampleNodes(nodes: readonly MinimapNode[]): readonly MinimapNode[] {
  if (nodes.length <= MAX_DOTS) {
    return nodes;
  }
  const stride = nodes.length / MAX_DOTS;
  const sampled: MinimapNode[] = [];
  for (let index = 0; index < MAX_DOTS; index += 1) {
    sampled.push(nodes[Math.floor(index * stride)] as MinimapNode);
  }
  return sampled;
}

/**
 * One factor for both axes, so the overview is a faithful miniature instead of a stretched one.
 * An axis with no extent cannot constrain the factor, so it drops out of the minimum.
 */
function fitScale(contentWidth: number, contentHeight: number, usableWidth: number, usableHeight: number): number {
  const fits = [
    contentWidth > 0 ? usableWidth / contentWidth : Number.POSITIVE_INFINITY,
    contentHeight > 0 ? usableHeight / contentHeight : Number.POSITIVE_INFINITY,
  ];
  return Math.min(MAX_SCALE, ...fits);
}

/**
 * Renders the whole graph as a small SVG picture with a box over the visible part.
 * Returns an empty string when there is nothing worth drawing.
 */
export function renderMinimap(options: MinimapOptions): string {
  const { nodes, bounds, viewport, width, height, colours, background } = options;

  if (nodes.length === 0 || !isPositiveFinite(width) || !isPositiveFinite(height)) {
    return '';
  }

  // Reversed bounds would flip the picture; normalising keeps the transform below linear.
  const minX = Math.min(bounds.minX, bounds.maxX);
  const minY = Math.min(bounds.minY, bounds.maxY);
  const contentWidth = Math.abs(bounds.maxX - bounds.minX);
  const contentHeight = Math.abs(bounds.maxY - bounds.minY);

  /* A box with no extent on either axis describes a point, not a picture. Cytoscape reports a node's
     own width and height, so this only happens when there is nothing measurable to draw. */
  if (contentWidth === 0 && contentHeight === 0) {
    return '';
  }

  const sample = sampleNodes(nodes);
  const usableWidth = Math.max(1, width - INSET * 2);
  const usableHeight = Math.max(1, height - INSET * 2);
  const scale = fitScale(contentWidth, contentHeight, usableWidth, usableHeight);
  // Centring the scaled extent leaves the unused slack split evenly on both sides of the box.
  const offsetX = (width - contentWidth * scale) / 2 - minX * scale;
  const offsetY = (height - contentHeight * scale) / 2 - minY * scale;

  const radius = Math.min(MAX_RADIUS, Math.max(MIN_RADIUS, RADIUS_NUMERATOR / Math.sqrt(sample.length)));

  const dots = sample
    .map((node) => `<circle cx="${num(node.x * scale + offsetX)}" cy="${num(node.y * scale + offsetY)}"`
      + ` r="${num(radius)}" fill="${escapeAttribute(colourFor(colours, node.layer))}"/>`)
    .join('');

  let viewportMarkup = '';
  if (viewport) {
    // A viewport can sit partly outside the drawn elements when the reader pans past the edge; the
    // box clips it so the marker never leaves the picture.
    const nearX = clampToBox(viewport.x1 * scale + offsetX, width);
    const farX = clampToBox(viewport.x2 * scale + offsetX, width);
    const nearY = clampToBox(viewport.y1 * scale + offsetY, height);
    const farY = clampToBox(viewport.y2 * scale + offsetY, height);
    viewportMarkup = `<rect x="${num(Math.min(nearX, farX))}" y="${num(Math.min(nearY, farY))}"`
      + ` width="${num(Math.abs(farX - nearX))}" height="${num(Math.abs(farY - nearY))}"`
      + ` fill="none" stroke="${VIEWPORT_STROKE}" stroke-width="1" vector-effect="non-scaling-stroke"/>`;
  }

  const size = `viewBox="0 0 ${num(width)} ${num(height)}" width="${num(width)}" height="${num(height)}"`;

  return `<svg xmlns="http://www.w3.org/2000/svg" ${size}`
    + ` preserveAspectRatio="xMidYMid meet" role="img" aria-label="${ARIA_LABEL}">`
    + `<rect x="0" y="0" width="${num(width)}" height="${num(height)}" fill="${escapeAttribute(background)}"/>`
    + dots
    + viewportMarkup
    + '</svg>';
}
