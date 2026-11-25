/**
 * jsdom gaps cytoscape and the webview rely on.
 *
 * These are environment repairs, not test doubles for the extension's own code: a real
 * `HTMLCanvasElement` needs a 2D context cytoscape can draw into, a real panel element needs a
 * measured size, and `ResizeObserver` exists in every browser the webview runs in.
 */
import { afterAll } from 'vitest';

interface Context2DLike {
  canvas: HTMLCanvasElement | null;
  [key: string | symbol]: unknown;
}

function makeContext2D(): Context2DLike {
  const noop = (): void => undefined;
  const base: Record<string, unknown> = {
    canvas: null,
    measureText: (value: unknown) => ({ width: String(value ?? '').length * 6 }),
    createLinearGradient: () => ({ addColorStop: noop }),
    createPattern: () => ({}),
    createRadialGradient: () => ({ addColorStop: noop }),
    getImageData: () => ({ data: new Uint8ClampedArray(4) }),
    createImageData: () => ({ data: new Uint8ClampedArray(4) }),
  };
  const noops = [
    'putImageData', 'drawImage', 'setLineDash', 'setTransform', 'resetTransform', 'save', 'restore',
    'translate', 'scale', 'rotate', 'clearRect', 'fillRect', 'strokeRect', 'beginPath', 'closePath',
    'moveTo', 'lineTo', 'arc', 'arcTo', 'bezierCurveTo', 'quadraticCurveTo', 'rect', 'clip', 'fill',
    'stroke', 'fillText', 'strokeText',
  ];
  return new Proxy(base as Context2DLike, {
    get(target, property) {
      if (property in target) {
        return target[property];
      }
      if (typeof property === 'string' && noops.includes(property)) {
        return noop;
      }
      return undefined;
    },
    set(target, property, value) {
      target[property] = value;
      return true;
    },
  });
}

const sharedContext = makeContext2D();

HTMLCanvasElement.prototype.getContext = function getContext(this: HTMLCanvasElement, type: string) {
  if (type !== '2d') {
    return null;
  }
  sharedContext.canvas = this;
  return sharedContext as unknown as CanvasRenderingContext2D;
} as typeof HTMLCanvasElement.prototype.getContext;

class ResizeObserverStub {
  observe(): void {}

  unobserve(): void {}

  disconnect(): void {}
}

(globalThis as unknown as { ResizeObserver: unknown }).ResizeObserver = ResizeObserverStub;

if (!('matchMedia' in window)) {
  Object.defineProperty(window, 'matchMedia', {
    configurable: true,
    value: (query: string) => ({
      matches: false,
      media: query,
      onchange: null,
      addListener: () => undefined,
      removeListener: () => undefined,
      addEventListener: () => undefined,
      removeEventListener: () => undefined,
      dispatchEvent: () => false,
    }),
  });
}

/**
 * The panel schedules work on short timers — a zoom settles its labels after 90ms, a hidden details
 * panel resizes the canvas after 60ms. Draining them once the file's tests finish keeps that work
 * inside the environment that owns it instead of surfacing as an unhandled error mid-suite.
 */
afterAll(async () => {
  await new Promise((resolve) => setTimeout(resolve, 250));
});
