import { afterEach, describe, expect, it } from 'vitest';
import {
  createOverlay,
  type Overlay,
  type OverlayFrame,
  type OverlaySize,
  type OverlayTransform,
} from '../../src/webview/client/overlay';

let host: HTMLElement | undefined;

const mount = (): { host: HTMLElement; layer: HTMLElement; overlay: Overlay } => {
  const element = document.createElement('div');
  host = element;
  document.body.appendChild(element);
  const overlay = createOverlay(element);
  return { host: element, layer: element.querySelector('.overlay-layer') as HTMLElement, overlay };
};

afterEach(() => {
  host?.remove();
  host = undefined;
});

const frame = (partial: Partial<OverlayFrame> = {}): OverlayFrame => ({
  id: 'frame',
  label: '',
  x: 0,
  y: 0,
  width: 100,
  height: 50,
  tone: 'group',
  ...partial,
});

const view = (partial: Partial<OverlayTransform> = {}): OverlayTransform => ({
  zoom: 1,
  panX: 0,
  panY: 0,
  ...partial,
});

const size = (partial: Partial<OverlaySize> = {}): OverlaySize => ({
  width: 800,
  height: 600,
  ...partial,
});

const frames = (layer: HTMLElement): HTMLElement[] =>
  [...layer.querySelectorAll<HTMLElement>('.overlay-frame')];

describe('createOverlay', () => {
  it('adds a layer that cannot intercept pointer events', () => {
    const { host: element, layer } = mount();

    expect(layer.parentElement).toBe(element);
    expect(layer.className).toBe('overlay-layer');
    expect(layer.style.position).toBe('absolute');
    // Also inline, because the stylesheet can arrive after the script under the webview's CSP.
    expect(layer.style.pointerEvents).toBe('none');
  });
});

describe('paint', () => {
  it('places a frame at the pixel offset the transform implies', () => {
    const { layer, overlay } = mount();
    overlay.paint(
      [frame({ id: 'a', x: 10, y: 20, width: 100, height: 50 })],
      view({ zoom: 2, panX: 5, panY: 7 }),
      size(),
    );

    const [element] = frames(layer);
    expect(element.style.left).toBe('25px');
    expect(element.style.top).toBe('47px');
    expect(element.style.width).toBe('200px');
    expect(element.style.height).toBe('100px');
  });

  it('rounds every edge to whole pixels', () => {
    const { layer, overlay } = mount();
    overlay.paint(
      [frame({ x: 0.4, y: 0.6, width: 10.5, height: 9.4 })],
      view({ zoom: 1.1, panX: 0.35, panY: -0.4 }),
      size(),
    );

    const [element] = frames(layer);
    expect(element.style.left).toBe('1px');
    expect(element.style.top).toBe('0px');
    expect(element.style.width).toBe('12px');
    expect(element.style.height).toBe('10px');
  });

  it('classes each frame with its tone', () => {
    const { layer, overlay } = mount();
    overlay.paint(
      [
        frame({ id: 'a', tone: 'module' }),
        frame({ id: 'b', tone: 'group', x: 200 }),
        frame({ id: 'c', tone: 'island', x: 400 }),
      ],
      view(),
      size(),
    );

    expect(frames(layer).map((element) => element.className)).toEqual([
      'overlay-frame overlay-frame-module',
      'overlay-frame overlay-frame-group',
      'overlay-frame overlay-frame-island',
    ]);
  });

  it('writes the label as text, so markup in a type name stays literal', () => {
    const { layer, overlay } = mount();
    overlay.paint(
      [frame({ label: '<script>alert(1)</script>' })],
      view(),
      size(),
    );

    const span = layer.querySelector('.overlay-label') as HTMLElement;
    expect(span.tagName).toBe('SPAN');
    expect(span.textContent).toBe('<script>alert(1)</script>');
    expect(layer.querySelector('script')).toBeNull();
    expect(span.innerHTML).toBe('&lt;script&gt;alert(1)&lt;/script&gt;');
  });

  it('draws a border-only frame when the label is empty', () => {
    const { layer, overlay } = mount();
    overlay.paint([frame({ label: '' })], view(), size());

    expect(layer.querySelector('.overlay-label')).toBeNull();
  });

  it('reuses the node for an id it has already drawn', () => {
    const { layer, overlay } = mount();

    overlay.paint([frame({ id: 'a', label: 'Types' })], view(), size());
    const first = frames(layer)[0];

    overlay.paint([frame({ id: 'a', label: 'Types', x: 40 })], view({ zoom: 1.5 }), size());
    const second = frames(layer)[0];

    expect(second).toBe(first);
    expect(frames(layer)).toHaveLength(1);
    expect(second.style.left).toBe('60px');
  });

  it('updates a label in place and drops the span when the label empties', () => {
    const { layer, overlay } = mount();

    overlay.paint([frame({ label: 'First' })], view(), size());
    const span = layer.querySelector('.overlay-label') as HTMLElement;

    overlay.paint([frame({ label: 'Second' })], view(), size());
    expect(layer.querySelector('.overlay-label')).toBe(span);
    expect(span.textContent).toBe('Second');

    overlay.paint([frame({ label: '' })], view(), size());
    expect(layer.querySelector('.overlay-label')).toBeNull();
  });

  it('restyles a frame whose tone changed', () => {
    const { layer, overlay } = mount();

    overlay.paint([frame({ tone: 'group' })], view(), size());
    overlay.paint([frame({ tone: 'module' })], view(), size());

    expect(frames(layer)[0].className).toBe('overlay-frame overlay-frame-module');
  });

  it('removes the node for a frame that is no longer painted', () => {
    const { layer, overlay } = mount();

    overlay.paint([frame({ id: 'a' }), frame({ id: 'b', x: 200 })], view(), size());
    expect(overlay.visibleIds()).toEqual(['a', 'b']);

    overlay.paint([frame({ id: 'b', x: 200 })], view(), size());
    expect(overlay.visibleIds()).toEqual(['b']);
    expect(frames(layer)).toHaveLength(1);
    expect(frames(layer)[0].style.left).toBe('200px');
  });

  it('culls a frame entirely outside the viewport', () => {
    const { layer, overlay } = mount();

    overlay.paint(
      [
        frame({ id: 'right', x: 1000 }),
        frame({ id: 'left', x: -500 }),
        frame({ id: 'below', y: 700 }),
        frame({ id: 'above', y: -300 }),
        frame({ id: 'inside', x: 100 }),
      ],
      view(),
      size(),
    );

    expect(overlay.visibleIds()).toEqual(['inside']);
    expect(frames(layer)).toHaveLength(1);
  });

  it('keeps a frame that straddles the edge inside the culling margin', () => {
    const { layer, overlay } = mount();

    overlay.paint(
      [
        // Its box ends 16px left of the viewport, inside the 32px margin.
        frame({ id: 'straddles', x: -116, width: 100 }),
        // Its box starts 16px past the right edge, inside the margin.
        frame({ id: 'past', x: 816, width: 100, height: 20 }),
      ],
      view(),
      size(),
    );

    expect(overlay.visibleIds()).toEqual(['straddles', 'past']);
  });

  it('culls a frame once it is beyond the margin', () => {
    const { layer, overlay } = mount();

    overlay.paint(
      [
        // Ends 40px left of the viewport — past the 32px margin.
        frame({ id: 'straddles', x: -140, width: 100 }),
        // Starts 40px past the right edge — past the margin.
        frame({ id: 'past', x: 840, width: 100, height: 20 }),
      ],
      view(),
      size(),
    );

    expect(overlay.visibleIds()).toEqual([]);
    expect(frames(layer)).toHaveLength(0);
  });

  it('draws nothing for a degenerate transform', () => {
    const { layer, overlay } = mount();

    for (const transform of [
      view({ zoom: 0 }),
      view({ zoom: -1 }),
      view({ zoom: Number.NaN }),
      view({ zoom: Number.POSITIVE_INFINITY }),
      view({ panX: Number.NaN }),
      view({ panY: Number.NEGATIVE_INFINITY }),
    ]) {
      overlay.paint([frame()], transform, size());
      expect(overlay.visibleIds()).toEqual([]);
      expect(frames(layer)).toHaveLength(0);
    }
  });

  it('draws nothing for a degenerate viewport', () => {
    const { layer, overlay } = mount();

    for (const viewport of [
      size({ width: 0 }),
      size({ height: -10 }),
      size({ width: Number.NaN }),
      size({ height: Number.POSITIVE_INFINITY }),
    ]) {
      overlay.paint([frame()], view(), viewport);
      expect(overlay.visibleIds()).toEqual([]);
      expect(frames(layer)).toHaveLength(0);
    }
  });

  it('skips a frame with a zero, negative or non-finite box rather than writing NaNpx', () => {
    const { layer, overlay } = mount();

    overlay.paint(
      [
        frame({ id: 'zero-width', width: 0 }),
        frame({ id: 'negative-height', height: -50 }),
        frame({ id: 'nan-x', x: Number.NaN }),
        frame({ id: 'infinite-y', y: Number.POSITIVE_INFINITY }),
        // A finite coordinate still overflows once the zoom multiplies it.
        frame({ id: 'overflow-x', x: Number.MAX_VALUE }),
        frame({ id: 'sound', x: 10 }),
      ],
      view({ zoom: 10 }),
      size(),
    );

    expect(overlay.visibleIds()).toEqual(['sound']);
    expect(layer.innerHTML).not.toContain('NaN');
    expect(layer.innerHTML).not.toContain('Infinity');
  });

  it('clears the layer on an empty paint', () => {
    const { layer, overlay } = mount();

    overlay.paint([frame()], view(), size());
    expect(frames(layer)).toHaveLength(1);

    overlay.paint([], view(), size());
    expect(overlay.visibleIds()).toEqual([]);
    expect(layer.querySelectorAll('.overlay-frame')).toHaveLength(0);
  });

  it('keeps one node per id when the input repeats an id', () => {
    const { layer, overlay } = mount();

    overlay.paint(
      [
        frame({ id: 'dup', label: 'small' }),
        frame({ id: 'dup', label: 'large', width: 400, height: 300 }),
      ],
      view(),
      size(),
    );

    expect(overlay.visibleIds()).toEqual(['dup']);
    expect(frames(layer)).toHaveLength(1);
    // The larger frame sorts first, so it is the one that survives.
    expect((layer.querySelector('.overlay-label') as HTMLElement).textContent).toBe('large');
  });
});

describe('paint order', () => {
  it('puts modules behind groups, then larger regions first, then by id', () => {
    const { layer, overlay } = mount();
    const spread = [
      frame({ id: 'g-small', tone: 'group', width: 100, height: 100 }),
      frame({ id: 'module', tone: 'module', x: 0, width: 400, height: 300 }),
      frame({ id: 'island', tone: 'island', x: 500, width: 10, height: 10 }),
      frame({ id: 'g-big', tone: 'group', x: 200, width: 200, height: 200 }),
    ];

    overlay.paint(spread, view(), size());
    const expected = ['module', 'g-big', 'g-small', 'island'];

    expect(overlay.visibleIds()).toEqual(expected);
    expect(frames(layer).map((element) => element.className)).toEqual([
      'overlay-frame overlay-frame-module',
      'overlay-frame overlay-frame-group',
      'overlay-frame overlay-frame-group',
      'overlay-frame overlay-frame-island',
    ]);
  });

  it('gives the same order for the same frames supplied in a different order', () => {
    const { layer, overlay } = mount();
    const spread = [
      frame({ id: 'a', tone: 'group', width: 100, height: 100 }),
      frame({ id: 'b', tone: 'group', x: 120, width: 100, height: 100 }),
      frame({ id: 'c', tone: 'module', x: 240, width: 200, height: 200 }),
    ];

    overlay.paint(spread, view(), size());
    const forward = overlay.visibleIds();

    overlay.paint([...spread].reverse(), view(), size());
    const backward = overlay.visibleIds();

    // Equal areas fall back to the id, so the order does not depend on the caller's array.
    expect(forward).toEqual(['c', 'a', 'b']);
    expect(backward).toEqual(forward);
  });
});

describe('clear', () => {
  it('removes every frame and forgets them', () => {
    const { layer, overlay } = mount();

    overlay.paint([frame({ id: 'a' }), frame({ id: 'b', x: 200 })], view(), size());
    const first = frames(layer)[0];

    overlay.clear();
    expect(overlay.visibleIds()).toEqual([]);
    expect(layer.querySelectorAll('.overlay-frame')).toHaveLength(0);

    // A paint after clear starts clean rather than reusing a node it claims to have forgotten.
    overlay.paint([frame({ id: 'a' })], view(), size());
    expect(frames(layer)[0]).not.toBe(first);
  });
});

describe('visibleIds', () => {
  it('reports the ids it drew, in paint order', () => {
    const { layer, overlay } = mount();

    expect(overlay.visibleIds()).toEqual([]);

    overlay.paint(
      [frame({ id: 'b', x: 200 }), frame({ id: 'a' })],
      view(),
      size(),
    );

    expect(overlay.visibleIds()).toEqual(['a', 'b']);
  });

  it('hands back a copy, so a caller cannot corrupt the layer', () => {
    const { layer, overlay } = mount();

    overlay.paint([frame({ id: 'a' })], view(), size());
    overlay.visibleIds().push('ghost');

    expect(overlay.visibleIds()).toEqual(['a']);
  });
});
