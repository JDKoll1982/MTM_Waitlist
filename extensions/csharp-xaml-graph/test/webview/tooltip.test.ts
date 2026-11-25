import { describe, expect, it } from 'vitest';
import {
  EDGE_COLOUR,
  renderEdgeTooltip,
  renderTooltip,
  summariseEdge,
  summariseNode,
  type NodeSummaryInput,
} from '../../src/webview/client/tooltip';
import { graphEdge, finding } from '../support/model';

const long = 'x'.repeat(400);

describe('summariseEdge', () => {
  it('carries the label, swatch colour and confidence wording', () => {
    const summary = summariseEdge({
      kind: 'injects',
      sourceLabel: 'Page',
      sourceFullName: 'N.Page',
      targetLabel: 'ViewModel',
      targetFullName: 'N.ViewModel',
      confidence: 'high',
      member: 'vm',
      file: 'a.cs',
      line: 4,
    });

    expect(summary).toEqual({
      kind: 'injects',
      kindLabel: 'constructor injection',
      colour: EDGE_COLOUR.injects,
      sourceLabel: 'Page',
      targetLabel: 'ViewModel',
      sourceFullName: 'N.Page',
      targetFullName: 'N.ViewModel',
      confidence: 'high',
      confidenceLabel: 'read straight from the source',
      member: 'vm',
      file: 'a.cs',
      line: 4,
    });
  });

  it('translates each confidence into what it means for the reader', () => {
    const base = {
      kind: 'binds' as const,
      sourceLabel: 'a', sourceFullName: 'A', targetLabel: 'b', targetFullName: 'B',
    };
    expect(summariseEdge({ ...base, confidence: 'medium' }).confidenceLabel).toBe('inferred from a reference');
    expect(summariseEdge({ ...base, confidence: 'low' }).confidenceLabel).toBe('a weak hint — worth checking');
  });

  it('falls back to the raw values for a kind or confidence it does not know', () => {
    const summary = summariseEdge({
      kind: 'mystery' as never,
      sourceLabel: 'a', sourceFullName: 'A', targetLabel: 'b', targetFullName: 'B',
      confidence: 'unknown' as never,
    });
    expect(summary.kindLabel).toBe('mystery');
    expect(summary.colour).toBe('#6b6b6b');
    expect(summary.confidenceLabel).toBe('unknown');
  });
});

describe('renderEdgeTooltip', () => {
  const summary = summariseEdge({
    kind: 'field',
    sourceLabel: 'Page',
    sourceFullName: 'N.Page',
    targetLabel: 'ViewModel',
    targetFullName: 'N.ViewModel',
    confidence: 'high',
    member: '_vm',
    file: 'N/Page.cs',
    line: 42,
  });

  it('states both types, the kind and the evidence line', () => {
    const html = renderEdgeTooltip(summary);
    expect(html).toContain('<strong>Page</strong>');
    expect(html).toContain('<strong>ViewModel</strong>');
    expect(html).toContain('field-backed');
    expect(html).toContain('N/Page.cs:42');
    expect(html).toContain('_vm');
    expect(html).toContain('Confidence: high — read straight from the source');
    expect(html).toContain('Double-click the line to open the code that creates it');
    expect(html).toContain(`background:${EDGE_COLOUR.field}`);
  });

  it('omits the evidence and member rows when there are none', () => {
    const bare = summariseEdge({
      kind: 'reference',
      sourceLabel: 'a', sourceFullName: 'A', targetLabel: 'b', targetFullName: 'B',
      confidence: 'low',
    });
    const html = renderEdgeTooltip(bare);
    expect(html).not.toContain('tip-counts');
    expect(html).not.toContain('tip-meta"><span class="muted">_');
  });

  it('defaults the evidence line to the first line of the file', () => {
    const partial = { ...summary, line: undefined };
    expect(renderEdgeTooltip(partial)).toContain('N/Page.cs:1');
  });

  it('escapes markup in every value it prints', () => {
    const dangerous = { ...summary, sourceLabel: '<img onerror="x">', member: '<b>', file: 'a&b.cs' };
    const html = renderEdgeTooltip(dangerous);
    expect(html).not.toContain('<img');
    expect(html).toContain('&lt;img');
    expect(html).toContain('&lt;b&gt;');
    expect(html).toContain('a&amp;b.cs');
  });
});

describe('summariseNode', () => {
  const edges = [
    graphEdge({ id: 'e1', source: 'N.Type', target: 'N.Other', kind: 'reference' }),
    graphEdge({ id: 'e2', source: 'N.Type', target: 'N.Other2', kind: 'reference' }),
    graphEdge({ id: 'e3', source: 'N.Caller', target: 'N.Type', kind: 'injects' }),
  ];

  const input = (partial: Partial<NodeSummaryInput> = {}): NodeSummaryInput => ({
    id: 'N.Type',
    label: 'Type',
    fullName: 'N.Type',
    layer: 'viewmodel',
    kind: 'class',
    module: 'Module_A',
    edges,
    findings: [],
    ...partial,
  });

  it('counts the edges in each direction and their total', () => {
    const summary = summariseNode(input());
    expect(summary.out).toBe(2);
    expect(summary.incoming).toBe(1);
    expect(summary.drawn).toBe(3);
  });

  it('chips the layer, kind and module, plus registration and the source line when known', () => {
    const summary = summariseNode(input({ diRegistered: true, file: 'N/Type.cs', line: 9 }));
    expect(summary.chips).toEqual([
      { text: 'viewmodel', cls: 'layer-viewmodel' },
      { text: 'class', cls: 'muted' },
      { text: 'Module_A', cls: 'muted' },
      { text: 'DI registered', cls: 'muted' },
      { text: 'N/Type.cs:9', cls: 'muted' },
    ]);
  });

  it('defaults the source line to one', () => {
    const summary = summariseNode(input({ file: 'N/Type.cs' }));
    expect(summary.chips.at(-1)).toEqual({ text: 'N/Type.cs:1', cls: 'muted' });
  });

  it('orders the issues by severity', () => {
    const summary = summariseNode(input({
      findings: [
        finding({ kind: 'unused-member', title: 'low one', severity: 'low' }),
        finding({ kind: 'dead-binding', title: 'high one', severity: 'high' }),
        finding({ kind: 'unused-type', title: 'medium one', severity: 'medium' }),
      ],
    }));
    expect(summary.issues.map((issue) => issue.severity)).toEqual(['high', 'medium', 'low']);
    expect(summary.issues[0].kind).toBe('Dead binding');
  });

  it('truncates a long detail and falls back to the title when the detail is empty', () => {
    const summary = summariseNode(input({
      findings: [finding({ kind: 'cycle', title: 'short title', detail: long, severity: 'high' })],
    }));
    expect(summary.issues[0].detail.length).toBe(180);
    expect(summary.issues[0].detail.endsWith('…')).toBe(true);

    const fallback = summariseNode(input({
      findings: [finding({ kind: 'cycle', title: 'only a title', detail: '', severity: 'high' })],
    }));
    expect(fallback.issues[0].detail).toBe('only a title');
  });
});

describe('renderTooltip', () => {
  const base = {
    label: 'Type',
    fullName: 'N.Type',
    chips: [{ text: 'viewmodel', cls: 'layer-viewmodel' }],
    out: 1,
    incoming: 2,
    drawn: 3,
    issues: [],
  };

  it('says plainly when nothing is wrong', () => {
    const html = renderTooltip(base);
    expect(html).toContain('No cleanup findings.');
    expect(html).toContain('1 dependency out · 2 used by');
    expect(html).toContain('Right-click to unpack its shape');
  });

  it('pluralises the dependency count', () => {
    expect(renderTooltip({ ...base, out: 2 })).toContain('2 dependencies out');
    expect(renderTooltip({ ...base, out: 0 })).toContain('0 dependencies out');
  });

  it('lists at most four issues and counts the rest', () => {
    const issues = Array.from({ length: 6 }, (_, index) => ({
      kind: 'Cycle',
      severity: 'high' as const,
      detail: `issue ${index}`,
      title: 't',
    }));
    const html = renderTooltip({ ...base, issues });
    expect(html).toContain('issue 0');
    expect(html).toContain('issue 3');
    expect(html).not.toContain('issue 4');
    expect(html).toContain('and 2 more in Problems');
  });

  it('labels each severity in words', () => {
    const html = renderTooltip({
      ...base,
      issues: [
        { kind: 'Cycle', severity: 'high', detail: 'a', title: 'a' },
        { kind: 'Cycle', severity: 'medium', detail: 'b', title: 'b' },
        { kind: 'Cycle', severity: 'low', detail: 'c', title: 'c' },
      ],
    });
    expect(html).toContain('must fix');
    expect(html).toContain('review');
    expect(html).toContain('note');
  });

  it('escapes markup in the name and the issues', () => {
    const html = renderTooltip({
      ...base,
      label: '<script>',
      issues: [{ kind: '<b>', severity: 'high', detail: '<i>', title: 'x' }],
    });
    expect(html).not.toContain('<script>');
    expect(html).toContain('&lt;script&gt;');
    expect(html).toContain('&lt;i&gt;');
  });
});
