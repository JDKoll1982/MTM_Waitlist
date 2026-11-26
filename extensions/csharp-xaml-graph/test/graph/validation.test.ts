import { describe, expect, it } from 'vitest';
import {
  buildResult,
  compareWithRecord,
  firstAttemptClean,
  importCondition,
  measure,
  recordAttempt,
  type ExpectedAnswerRecord,
  type ImportAttempts,
} from '../../src/graph/validation';
import { finding, graphEdge, graphModel, graphNode } from '../support/model';

/**
 * A small but complete model: a view bound to a view model, one service, one finding.
 *
 * It is written by hand rather than produced by the analysis so a mismatch can be caused by editing
 * exactly one thing.
 */
function model() {
  return graphModel({
    nodes: [
      graphNode({ id: 'App.HomePage', layer: 'view', kind: 'page' }),
      graphNode({ id: 'App.HomeViewModel', layer: 'viewmodel' }),
      graphNode({ id: 'App.OrderService', layer: 'service' }),
      graphNode({ id: 'App.OrderRow', layer: 'model' }),
    ],
    edges: [
      graphEdge({ source: 'App.HomePage', target: 'App.HomeViewModel', kind: 'binds' }),
      graphEdge({ source: 'App.HomeViewModel', target: 'App.OrderService', kind: 'injects' }),
      graphEdge({ source: 'App.HomeViewModel', target: 'App.OrderRow', kind: 'reference' }),
    ],
    findings: [
      finding({ kind: 'unused-type', title: 'App.OrderRow is never used', nodeIds: ['App.OrderRow'] }),
    ],
  });
}

const record = (partial: Partial<ExpectedAnswerRecord> = {}): ExpectedAnswerRecord => ({
  schemaVersion: 1,
  repository: 'app',
  source: 'read from the code',
  types: [
    { name: 'App.HomePage', file: 'App/HomePage.cs', layer: 'view' },
    { name: 'App.HomeViewModel', file: 'App/HomeViewModel.cs', layer: 'viewmodel' },
    { name: 'App.OrderService', file: 'App/OrderService.cs', layer: 'service' },
    { name: 'App.OrderRow', file: 'App/OrderRow.cs', layer: 'model' },
  ],
  links: [{ view: 'App.HomePage', viewModel: 'App.HomeViewModel' }],
  findings: [{ kind: 'unused-type', types: ['App.OrderRow'] }],
  ...partial,
});

describe('measure', () => {
  it('counts the three totals and each label', () => {
    const measured = measure(model());

    expect(measured.nodes).toBe(4);
    expect(measured.edges).toBe(3);
    expect(measured.findings).toBe(1);
    expect(measured.byLayer).toEqual({ model: 1, service: 1, view: 1, viewmodel: 1 });
    expect(measured.byEdgeKind).toEqual({ binds: 1, injects: 1, reference: 1 });
    expect(measured.byFindingKind).toEqual({ 'unused-type': 1 });
  });

  it('states view-to-view-model pairs only, ignoring a plain reference', () => {
    const measured = measure(model());
    expect(measured.links).toEqual([['App.HomePage', 'App.HomeViewModel']]);
  });

  it('sorts types and links, and de-duplicates a repeated edge', () => {
    const twice = graphModel({
      nodes: [
        graphNode({ id: 'B.View', layer: 'view' }),
        graphNode({ id: 'B.VM', layer: 'viewmodel' }),
        graphNode({ id: 'A.Model', layer: 'model' }),
      ],
      edges: [
        graphEdge({ source: 'B.View', target: 'B.VM', kind: 'binds' }),
        graphEdge({ source: 'B.View', target: 'B.VM', kind: 'field' }),
      ],
    });

    const measured = measure(twice);
    expect(measured.types).toEqual([['A.Model', 'model'], ['B.VM', 'viewmodel'], ['B.View', 'view']]);
    expect(measured.links).toEqual([['B.View', 'B.VM']]);
  });

  it('carries no timestamp, no duration and no absolute path', () => {
    const measured = JSON.stringify(measure(model()));
    expect(measured).not.toMatch(/\d{4}-\d{2}-\d{2}/);
    expect(measured).not.toContain(process.cwd());
    expect(measured).not.toContain('generatedAt');
    expect(measured).not.toContain('elapsedMs');
  });

  it('measures two equivalent runs identically', () => {
    expect(JSON.stringify(measure(model()))).toBe(JSON.stringify(measure(model())));
  });
});

describe('compareWithRecord', () => {
  it('passes every condition when the run matches the record', () => {
    const report = compareWithRecord(model(), record());

    expect(report.layers).toEqual({ pass: true, mismatches: [] });
    expect(report.links).toEqual({ pass: true, mismatches: [] });
    expect(report.findings).toEqual({ pass: true, mismatches: [] });
  });

  it('names an unexpected type', () => {
    const report = compareWithRecord(model(), record({ types: record().types.filter((type) => type.name !== 'App.OrderRow') }));

    expect(report.layers.pass).toBe(false);
    expect(report.layers.mismatches).toEqual([
      { item: 'type', name: 'App.OrderRow', expected: 'not in the record', actual: 'model' },
    ]);
  });

  it('names a recorded type the run does not produce', () => {
    const report = compareWithRecord(model(), record({
      types: [...record().types, { name: 'App.Missing', file: 'App/Missing.cs', layer: 'service' }],
    }));

    expect(report.layers.mismatches).toEqual([
      { item: 'type', name: 'App.Missing', expected: 'service', actual: null },
    ]);
  });

  it('names a type the run places in another layer', () => {
    const report = compareWithRecord(model(), record({
      types: record().types.map((type) => (type.name === 'App.OrderRow' ? { ...type, layer: 'viewmodel' } : type)),
    }));

    expect(report.layers.mismatches).toEqual([
      { item: 'type', name: 'App.OrderRow', expected: 'viewmodel', actual: 'model' },
    ]);
  });

  it('names a missing link and an invented one', () => {
    const missing = compareWithRecord(model(), record({
      links: [
        { view: 'App.HomePage', viewModel: 'App.HomeViewModel' },
        { view: 'App.OtherPage', viewModel: 'App.HomeViewModel' },
      ],
    }));
    expect(missing.links.mismatches).toEqual([
      { item: 'link', name: 'App.OtherPage', expected: 'App.HomeViewModel', actual: null },
    ]);

    const invented = compareWithRecord(model(), record({ links: [] }));
    expect(invented.links.mismatches).toEqual([
      { item: 'link', name: 'App.HomePage', expected: 'no link', actual: 'App.HomeViewModel' },
    ]);
  });

  it('names a finding expected and absent, and one present and unexpected', () => {
    const absent = compareWithRecord(model(), record({
      findings: [{ kind: 'cycle', types: ['App.HomeViewModel', 'App.OrderService'] }],
    }));
    expect(absent.findings.mismatches).toEqual([
      { item: 'finding', name: 'App.HomeViewModel', expected: 'cycle', actual: null },
      { item: 'finding', name: 'App.OrderRow', expected: 'no finding', actual: 'unused-type' },
    ]);

    const unexpected = compareWithRecord(model(), record({ findings: [] }));
    expect(unexpected.findings.mismatches).toEqual([
      { item: 'finding', name: 'App.OrderRow', expected: 'no finding', actual: 'unused-type' },
    ]);
  });

  it('checks an empty array in the record as a claim of none', () => {
    const empty = compareWithRecord(graphModel(), record({ types: [], links: [], findings: [] }));
    expect(empty.layers.pass).toBe(true);
    expect(empty.links.pass).toBe(true);
    expect(empty.findings.pass).toBe(true);

    const claim = compareWithRecord(model(), record({ types: [], links: [], findings: [] }));
    expect(claim.layers.mismatches).toHaveLength(4);
    expect(claim.links.mismatches).toHaveLength(1);
    expect(claim.findings.mismatches).toHaveLength(1);
  });
});

describe('the recorded result', () => {
  const passing = { sha256: 'a'.repeat(64), problems: [] };
  const clean: ImportAttempts = { schemaVersion: 1, repository: 'app', attempts: [{ sha256: 'b'.repeat(64), problems: [] }] };

  it('passes only when all five conditions pass', () => {
    const report = compareWithRecord(model(), record());
    const result = buildResult('app', passing, report, importCondition(clean), { pass: true, detail: 'ALL CHECKS PASSED' });

    expect(result.verdict).toBe('pass');
    expect(Object.keys(result.conditions)).toEqual(['layers', 'links', 'findings', 'import', 'invariants']);
  });

  it('is a failing verdict when one mismatch stands, never a partial success', () => {
    const report = compareWithRecord(model(), record({ links: [] }));
    const result = buildResult('app', passing, report, importCondition(clean), { pass: true });

    expect(result.verdict).toBe('fail');
    expect(result.conditions.links.mismatches).toEqual([
      { item: 'link', name: 'App.HomePage', expected: 'no link', actual: 'App.HomeViewModel' },
    ]);
  });

  it('is a failing verdict when the invariants fail', () => {
    const report = compareWithRecord(model(), record());
    const result = buildResult('app', passing, report, importCondition(clean), { pass: false, detail: '1 CHECK(S) FAILED' });
    expect(result.verdict).toBe('fail');
  });
});

describe('the import condition', () => {
  const problem = { path: 'layers.rules[0].layer', message: 'missing', severity: 'warning' as const };

  it('is false with no attempts file, so a run cannot pass on an unchecked reply', () => {
    expect(firstAttemptClean(undefined)).toBe(false);
    expect(importCondition(undefined)).toEqual({ pass: false, detail: 'no reply has been checked yet' });
  });

  it('is false when the one attempt carried a problem, which is recorded rather than retried', () => {
    const attempts = recordAttempt(undefined, 'app', 'c'.repeat(64), [problem]);
    expect(firstAttemptClean(attempts)).toBe(false);
    expect(importCondition(attempts).detail).toBe('1 attempt, 1 problem');
  });

  it('is true with exactly one clean attempt', () => {
    const attempts = recordAttempt(undefined, 'app', 'd'.repeat(64), []);
    expect(firstAttemptClean(attempts)).toBe(true);
    expect(importCondition(attempts)).toEqual({ pass: true, detail: '1 attempt, 0 problems' });
  });

  it('is false with two attempts even when both are clean', () => {
    const once = recordAttempt(undefined, 'app', 'e'.repeat(64), []);
    const twice = recordAttempt(once, 'app', 'f'.repeat(64), []);
    expect(firstAttemptClean(twice)).toBe(false);
    expect(twice.attempts.map((attempt) => attempt.sha256)).toEqual(['e'.repeat(64), 'f'.repeat(64)]);
    expect(importCondition(twice).detail).toContain('2 attempts');
  });
});
