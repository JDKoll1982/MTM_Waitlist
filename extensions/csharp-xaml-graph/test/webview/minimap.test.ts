import { describe, expect, it } from 'vitest';
import {
  renderMinimap,
  type MinimapNode,
  type MinimapOptions,
} from '../../src/webview/client/minimap';

interface Circle {
  cx: number;
  cy: number;
  r: number;
  fill: string;
}

interface Rect {
  x: number;
  y: number;
  width: number;
  height: number;
  fill: string;
  stroke: string;
}

/** Reads one attribute out of a tag, so assertions do not depend on attribute order. */
const readAttribute = (tag: string, name: string): string => {
  const found = new RegExp(`\\b${name}="([^"]*)"`).exec(tag);
  return found ? found[1] : '';
};

const svgTag = (svg: string): string => /<svg\b[^>]*>/.exec(svg)?.[0] ?? '';

const circles = (svg: string): Circle[] => [...svg.matchAll(/<circle\b[^>]*>/g)].map((match) => ({
  cx: Number(readAttribute(match[0], 'cx')),
  cy: Number(readAttribute(match[0], 'cy')),
  r: Number(readAttribute(match[0], 'r')),
  fill: readAttribute(match[0], 'fill'),
}));

const rects = (svg: string): Rect[] => [...svg.matchAll(/<rect\b[^>]*>/g)].map((match) => ({
  x: Number(readAttribute(match[0], 'x')),
  y: Number(readAttribute(match[0], 'y')),
  width: Number(readAttribute(match[0], 'width')),
  height: Number(readAttribute(match[0], 'height')),
  fill: readAttribute(match[0], 'fill'),
  stroke: readAttribute(match[0], 'stroke'),
}));

const viewportRect = (svg: string): Rect | undefined => rects(svg).find((rect) => rect.stroke !== '');

const base = (overrides: Partial<MinimapOptions> = {}): MinimapOptions => ({
  nodes: [{ x: 0, y: 0, layer: 'view' }],
  bounds: { minX: 0, minY: 0, maxX: 100, maxY: 100 },
  width: 100,
  height: 100,
  colours: { view: '#4a9eff' },
  background: '#1e1e1e',
  ...overrides,
});

/** A row of nodes spread across the x axis, for the count-driven rules. */
const row = (count: number): MinimapNode[] => Array.from({ length: count }, (_unused, index) => ({
  x: index * 10,
  y: 0,
  layer: 'view',
}));

describe('renderMinimap: nothing to draw', () => {
  it('draws nothing for an empty node list', () => {
    expect(renderMinimap(base({ nodes: [] }))).toBe('');
  });

  it('draws nothing when the box has no usable size', () => {
    for (const bad of [0, -40, Number.NaN, Number.POSITIVE_INFINITY]) {
      expect(renderMinimap(base({ width: bad }))).toBe('');
      expect(renderMinimap(base({ height: bad }))).toBe('');
    }
  });

  it('draws nothing when the bounds have no extent on either axis', () => {
    // A zero-by-zero box describes a point, not a picture.
    expect(renderMinimap(base({ bounds: { minX: 5, minY: 5, maxX: 5, maxY: 5 } }))).toBe('');
  });
});

describe('renderMinimap: the svg wrapper', () => {
  it('is a self-describing image with the box as its viewport', () => {
    const tag = svgTag(renderMinimap(base({ width: 240, height: 160 })));

    expect(readAttribute(tag, 'viewBox')).toBe('0 0 240 160');
    expect(readAttribute(tag, 'width')).toBe('240');
    expect(readAttribute(tag, 'height')).toBe('160');
    expect(readAttribute(tag, 'preserveAspectRatio')).toBe('xMidYMid meet');
    expect(readAttribute(tag, 'role')).toBe('img');
    expect(readAttribute(tag, 'aria-label').toLowerCase()).toContain('overview');
  });

  it('paints the backdrop behind the dots', () => {
    const [backdrop] = rects(renderMinimap(base({ background: '#252526' })));

    expect(backdrop).toEqual({
      x: 0,
      y: 0,
      width: 100,
      height: 100,
      fill: '#252526',
      stroke: '',
    });
  });
});

describe('renderMinimap: scaling', () => {
  it('keeps every dot inside the box', () => {
    const svg = renderMinimap(base({
      nodes: [
        { x: -500, y: -300, layer: 'view' },
        { x: 900, y: 1200, layer: 'service' },
        { x: 0, y: 0, layer: 'viewmodel' },
      ],
      bounds: { minX: -500, minY: -300, maxX: 900, maxY: 1200 },
      width: 240,
      height: 160,
    }));
    const dots = circles(svg);

    expect(dots).toHaveLength(3);
    for (const dot of dots) {
      expect(dot.cx).toBeGreaterThanOrEqual(0);
      expect(dot.cx).toBeLessThanOrEqual(240);
      expect(dot.cy).toBeGreaterThanOrEqual(0);
      expect(dot.cy).toBeLessThanOrEqual(160);
    }
  });

  it('scales both axes by the same factor, so a graph lying on one axis stays on it', () => {
    const svg = renderMinimap(base({
      nodes: [{ x: 0, y: 50, layer: 'view' }, { x: 1000, y: 50, layer: 'view' }],
      bounds: { minX: 0, minY: 0, maxX: 1000, maxY: 100 },
    }));
    const [left, right] = circles(svg);

    expect(right.cy - left.cy).toBe(0);
    expect(right.cx - left.cx).toBeGreaterThan(0);
  });

  it('keeps the model aspect ratio rather than stretching to fill the box', () => {
    const svg = renderMinimap(base({
      nodes: [{ x: 0, y: 0, layer: 'view' }, { x: 200, y: 100, layer: 'view' }],
      bounds: { minX: 0, minY: 0, maxX: 200, maxY: 100 },
      width: 400,
      height: 400,
    }));
    const [start, end] = circles(svg);

    // The model is twice as wide as it is tall, so the drawing must be too.
    expect((end.cx - start.cx) / (end.cy - start.cy)).toBeCloseTo(2, 6);
  });

  it('centres the content in the box', () => {
    const svg = renderMinimap(base({
      nodes: [{ x: 0, y: 0, layer: 'view' }, { x: 200, y: 100, layer: 'view' }],
      bounds: { minX: 0, minY: 0, maxX: 200, maxY: 100 },
      width: 400,
      height: 400,
    }));
    const dots = circles(svg);

    // The wide axis fills the box, so its two dots must mirror about the centre line.
    expect(dots[0].cx + dots[1].cx).toBeCloseTo(400, 6);
    expect(dots[0].cy - 0).toBeGreaterThan(0);
    expect(400 - dots[1].cy).toBeCloseTo(dots[0].cy, 6);
  });

  it('keeps a long thin graph inside the box instead of overflowing it', () => {
    const svg = renderMinimap(base({
      nodes: [{ x: 0, y: 5, layer: 'view' }, { x: 5000, y: 5, layer: 'view' }],
      bounds: { minX: 0, minY: 0, maxX: 5000, maxY: 10 },
      width: 300,
      height: 200,
    }));
    const dots = circles(svg);

    for (const dot of dots) {
      expect(dot.cx).toBeLessThanOrEqual(300);
      expect(dot.cy).toBeLessThanOrEqual(200);
    }
    expect(Math.abs(dots[1].cy - dots[0].cy)).toBeLessThan(1);
  });

  it('handles bounds given in reverse without flipping or overflowing', () => {
    const svg = renderMinimap(base({
      nodes: [{ x: 0, y: 0, layer: 'view' }, { x: 100, y: 100, layer: 'view' }],
      bounds: { minX: 100, minY: 100, maxX: 0, maxY: 0 },
    }));
    const dots = circles(svg);

    expect(dots[0].cx).toBeLessThan(dots[1].cx);
    expect(dots[0].cy).toBeLessThan(dots[1].cy);
    expect(dots[1].cx).toBeLessThanOrEqual(100);
  });
});

describe('renderMinimap: dot radius', () => {
  it('shrinks as the graph grows and stays between 0.6 and 2.5 px', () => {
    const radiusFor = (count: number): number => {
      const nodes = row(count);
      const svg = renderMinimap(base({
        nodes,
        bounds: { minX: 0, minY: 0, maxX: count * 10, maxY: 100 },
      }));
      return circles(svg)[0].r;
    };

    const small = radiusFor(1);
    const medium = radiusFor(100);
    const large = radiusFor(1000);

    expect(small).toBeGreaterThan(medium);
    expect(medium).toBeGreaterThan(large);
    for (const radius of [small, medium, large]) {
      expect(radius).toBeGreaterThanOrEqual(0.6);
      expect(radius).toBeLessThanOrEqual(2.5);
    }
  });

  it('gives every dot the same radius', () => {
    const dots = circles(renderMinimap(base({
      nodes: row(40),
      bounds: { minX: 0, minY: 0, maxX: 400, maxY: 100 },
    })));

    expect(new Set(dots.map((dot) => dot.r)).size).toBe(1);
  });
});

describe('renderMinimap: the node cap', () => {
  it('draws every node when the graph is under the cap', () => {
    const nodes = row(4000);
    const svg = renderMinimap(base({
      nodes,
      bounds: { minX: 0, minY: 0, maxX: 40000, maxY: 100 },
    }));

    expect(circles(svg)).toHaveLength(4000);
  });

  it('samples down to 4000 dots when the graph is over the cap', () => {
    for (const count of [4001, 10000]) {
      const svg = renderMinimap(base({
        nodes: row(count),
        bounds: { minX: 0, minY: 0, maxX: count * 10, maxY: 100 },
      }));

      expect(circles(svg)).toHaveLength(4000);
    }
  });

  it('keeps the sampled dots spread across the picture', () => {
    const svg = renderMinimap(base({
      nodes: row(8000),
      bounds: { minX: 0, minY: 0, maxX: 80000, maxY: 100 },
    }));
    const dots = circles(svg);
    const spread = Math.max(...dots.map((dot) => dot.cx)) - Math.min(...dots.map((dot) => dot.cx));

    // A stride that collapsed to the first cluster would leave the right half empty.
    expect(spread).toBeGreaterThan(80);
  });
});

describe('renderMinimap: the viewport rectangle', () => {
  it('boxes the visible part of the picture', () => {
    const rect = viewportRect(renderMinimap(base({
      viewport: { x1: 25, y1: 25, x2: 75, y2: 75 },
    })));

    // The 100×100 model box maps to 94 px inside a 100 px box, so 25 → 26.5 and 75 → 73.5.
    expect(rect).toEqual({
      x: 26.5,
      y: 26.5,
      width: 47,
      height: 47,
      fill: 'none',
      stroke: '#f59e0b',
    });
  });

  it('is left off when no viewport is known', () => {
    expect(viewportRect(renderMinimap(base()))).toBeUndefined();
    expect(viewportRect(renderMinimap(base({ viewport: null })))).toBeUndefined();
    expect(rects(renderMinimap(base({ viewport: null })))).toHaveLength(1);
  });

  it('clamps a viewport that reaches past the bounds into the box', () => {
    const rect = viewportRect(renderMinimap(base({
      viewport: { x1: -10000, y1: -10000, x2: 100000, y2: 100000 },
    })));

    expect(rect).toEqual({
      x: 0,
      y: 0,
      width: 100,
      height: 100,
      fill: 'none',
      stroke: '#f59e0b',
    });
  });

  it('clips only the edge that overhangs', () => {
    const rect = viewportRect(renderMinimap(base({
      viewport: { x1: -50, y1: 50, x2: 50, y2: 60 },
    })));

    expect(rect?.x).toBe(0);
    expect(rect?.width).toBeCloseTo(50, 6);
    expect(rect?.height).toBeCloseTo(9.4, 6);
  });
});

describe('renderMinimap: colours', () => {
  it('fills each dot from its layer', () => {
    const dots = circles(renderMinimap(base({
      nodes: [{ x: 0, y: 0, layer: 'view' }, { x: 50, y: 50, layer: 'service' }],
      colours: { view: '#4a9eff', service: '#f0a030' },
    })));

    expect(dots.map((dot) => dot.fill)).toEqual(['#4a9eff', '#f0a030']);
  });

  it('falls back to neutral grey for a layer with no colour of its own', () => {
    const dots = circles(renderMinimap(base({
      nodes: [
        { x: 0, y: 0, layer: 'mystery' },
        { x: 50, y: 50, layer: 'blank' },
        { x: 20, y: 80, layer: 'constructor' },
      ],
      colours: { blank: '' },
    })));

    expect(dots.map((dot) => dot.fill)).toEqual(['#8a8a8a', '#8a8a8a', '#8a8a8a']);
  });

  it('escapes a colour that would otherwise break the attribute', () => {
    const svg = renderMinimap(base({ colours: { view: '#fff" onload="boom' } }));

    expect(circles(svg)[0].fill).toBe('#fff&quot; onload=&quot;boom');
    expect(svg).not.toContain('onload="boom');
  });
});

describe('renderMinimap: degenerate input', () => {
  it('never writes NaN, Infinity or undefined into the markup', () => {
    const svg = renderMinimap(base({
      nodes: [
        { x: Number.NaN, y: Number.NaN, layer: 'view' },
        { x: Number.POSITIVE_INFINITY, y: Number.NEGATIVE_INFINITY, layer: 'view' },
      ],
      bounds: { minX: Number.NaN, minY: Number.NaN, maxX: Number.NaN, maxY: Number.NaN },
      viewport: { x1: Number.NaN, y1: Number.NaN, x2: Number.POSITIVE_INFINITY, y2: 0 },
    }));

    expect(svg).not.toMatch(/NaN|Infinity|undefined/);
    expect(svg).not.toBe('');
    for (const dot of circles(svg)) {
      expect(Number.isFinite(dot.cx) && Number.isFinite(dot.cy) && Number.isFinite(dot.r)).toBe(true);
    }
    const rect = viewportRect(svg) as Rect;
    expect([rect.x, rect.y, rect.width, rect.height].every((value) => Number.isFinite(value))).toBe(true);
  });

  it('rounds coordinates to two decimal places', () => {
    const svg = renderMinimap(base({
      nodes: [{ x: 33.33333, y: 66.66666, layer: 'view' }],
      bounds: { minX: 0, minY: 0, maxX: 100, maxY: 100 },
    }));

    for (const value of svg.match(/(-?\d+\.\d+)/g) ?? []) {
      expect(value.split('.')[1].length).toBeLessThanOrEqual(2);
    }
  });
});

describe('renderMinimap: determinism', () => {
  it('produces byte-identical markup for identical input', () => {
    const options = base({
      nodes: [...row(50), { x: 12, y: 90, layer: 'service' }],
      bounds: { minX: 0, minY: 0, maxX: 500, maxY: 100 },
      viewport: { x1: 10, y1: 10, x2: 400, y2: 90 },
      colours: { view: '#4a9eff', service: '#f0a030' },
      width: 320,
      height: 180,
    });

    expect(renderMinimap(options)).toBe(renderMinimap(options));
  });
});
