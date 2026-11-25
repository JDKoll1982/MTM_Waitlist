import { describe, expect, it } from 'vitest';
import {
  DEFAULT_GRAPH_SETTINGS,
  UNPACKED_LAYOUTS,
  UNPACKED_LAYOUT_DESCRIPTION,
  UNPACKED_LAYOUT_LABEL,
  toUnpackedLayout,
} from '../../src/graph/settings';

describe('unpacked layout setting', () => {
  it('offers exactly the six layouts the manifest contributes', () => {
    expect([...UNPACKED_LAYOUTS]).toEqual(['layered-tb', 'layered-lr', 'force', 'radial', 'grid', 'circle']);
  });

  it('defaults to layered-tb, which is Layered Down', () => {
    expect(DEFAULT_GRAPH_SETTINGS.unpackedLayout).toBe('layered-tb');
    expect(UNPACKED_LAYOUT_LABEL[DEFAULT_GRAPH_SETTINGS.unpackedLayout]).toBe('Layered Down');
  });

  it('labels and describes every layout', () => {
    for (const layout of UNPACKED_LAYOUTS) {
      expect(UNPACKED_LAYOUT_LABEL[layout].length).toBeGreaterThan(0);
      expect(UNPACKED_LAYOUT_DESCRIPTION[layout].length).toBeGreaterThan(0);
    }
  });

  it('keeps a known layout as it is', () => {
    for (const layout of UNPACKED_LAYOUTS) {
      expect(toUnpackedLayout(layout)).toBe(layout);
    }
  });

  it('falls back to Layered Down for anything unknown, not to an arbitrary layout', () => {
    for (const value of ['', 'Layered Down', 'dagre', null, undefined, 3, {}, ['grid']]) {
      expect(toUnpackedLayout(value)).toBe('layered-tb');
    }
  });
});
