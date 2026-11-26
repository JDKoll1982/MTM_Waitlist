/**
 * The pure half of cross-repository validation: what a run measured, and how it compares with what
 * the checkout's code says it should have measured.
 *
 * The outcome is a list, never a count. Every failing item becomes one `Mismatch` naming the type and
 * what was expected of it, so a checkout below the bar can be read from the file rather than re-run.
 * Nothing here reads a file or knows about a checkout path, so a measurement carries no timestamp and
 * no absolute path and two equivalent runs measure identically.
 */

import type { FindingKind, GraphModel } from './model';
import type { ProfileLayer, ProfileProblem } from './profile';

/** One declared type the checkout has, as its own code says. */
export interface ExpectedType {
  /** The full name the graph uses as a node id. */
  name: string;
  /** Checkout-relative path the declaration was read from. */
  file: string;
  layer: ProfileLayer;
}

/** One view-to-view-model pair the checkout states. */
export interface ExpectedLink {
  view: string;
  viewModel: string;
}

/** One finding known to be true of the checkout. */
export interface ExpectedFinding {
  kind: FindingKind;
  /** The full names the finding is about, sorted, so a match does not depend on a title's wording. */
  types: string[];
}

/**
 * What one checkout's run is judged against.
 *
 * It is written by reading the checkout's code and committed before the first run against it, never
 * derived from the run it judges. It is exhaustive over the analysis scope: a type the run produces
 * and the record lacks is a mismatch, so an omission cannot be scored as accuracy.
 */
export interface ExpectedAnswerRecord {
  schemaVersion: 1;
  repository: string;
  /** One sentence saying how and when the record was read out of the code. */
  source: string;
  types: ExpectedType[];
  links: ExpectedLink[];
  findings: ExpectedFinding[];
}

/** What a run counted, sorted throughout and free of walk order. */
export interface Measurement {
  nodes: number;
  edges: number;
  findings: number;
  byLayer: Record<string, number>;
  byEdgeKind: Record<string, number>;
  byFindingKind: Record<string, number>;
  /** One [name, layer] pair per node, sorted. */
  types: [string, string][];
  /** One [view, viewModel] pair per view-to-view-model edge, sorted and de-duplicated. */
  links: [string, string][];
}

/** One failing item. This is the unit the outcome is reported in. */
export interface Mismatch {
  item: 'type' | 'link' | 'finding';
  /** The type the row is about: the type id for a type, the view for a link, the first named type for a finding. */
  name: string;
  /** What the record says: a layer, a view model, no finding, no link, or not in the record. */
  expected: string;
  /** What the run produced, or null when it produced nothing. */
  actual: string | null;
}

/** One condition's outcome, with every failing item it found. */
export interface ConditionReport {
  pass: boolean;
  /** One plain line for a condition that reports a single fact. Never a substitute for a mismatch list. */
  detail?: string;
  mismatches?: Mismatch[];
}

/** The five conditions the 100% bar is assessed on. */
export interface ValidationConditions {
  layers: ConditionReport;
  links: ConditionReport;
  findings: ConditionReport;
  import: ConditionReport;
  invariants: ConditionReport;
}

/** What `compareWithRecord` found, before the profile and the invariants are folded in. */
export interface ValidationReport {
  measurements: Measurement;
  layers: ConditionReport;
  links: ConditionReport;
  findings: ConditionReport;
}

/** The recorded result of validating one checkout, in the shape `contracts/validation-result.schema.json` uses. */
export interface ValidationResult {
  schemaVersion: 1;
  repository: string;
  profile: { sha256: string; problems: ProfileProblem[] };
  measurements: Measurement;
  conditions: ValidationConditions;
  verdict: 'pass' | 'fail';
}

/** One check of a reply, recorded so the first one can be told apart from a correction. */
export interface ImportAttempt {
  /** A hash of the reply's text, so two different replies are visibly different. */
  sha256: string;
  problems: ProfileProblem[];
}

/** Every recorded check of one checkout's replies. */
export interface ImportAttempts {
  schemaVersion: 1;
  repository: string;
  attempts: ImportAttempt[];
}

/**
 * Edge kinds that state a view-to-view-model relationship.
 *
 * A plain `reference` is a property or parameter typed as another type, and a `template` edge is a
 * data template's row model; neither claims the view binds that view model, so neither is a link.
 */
const NON_LINK_KINDS = new Set(['reference', 'template']);

/**
 * A code-point comparison, not a locale one.
 *
 * The measurement is committed and compared byte for byte, so its order cannot depend on the machine's
 * collation rules.
 */
function compareText(left: string, right: string): number {
  if (left === right) {
    return 0;
  }
  return left < right ? -1 : 1;
}

function countBy<T>(items: T[], key: (item: T) => string): Record<string, number> {
  const counts = new Map<string, number>();
  for (const item of items) {
    const label = key(item);
    counts.set(label, (counts.get(label) ?? 0) + 1);
  }
  return Object.fromEntries([...counts].sort(([left], [right]) => compareText(left, right)));
}

function sortedPairs(pairs: [string, string][]): [string, string][] {
  const seen = new Set<string>();
  const unique: [string, string][] = [];
  for (const pair of pairs) {
    const key = `${pair[0]}\u0000${pair[1]}`;
    if (!seen.has(key)) {
      seen.add(key);
      unique.push(pair);
    }
  }
  return unique.sort((left, right) => compareText(left[0], right[0]) || compareText(left[1], right[1]));
}

/** What one run counted. The model is the only input, so the same model always measures the same way. */
export function measure(model: GraphModel): Measurement {
  const byName = new Map(model.nodes.map((node) => [node.id, node.layer]));

  const links: [string, string][] = [];
  for (const edge of model.edges) {
    if (NON_LINK_KINDS.has(edge.kind)) {
      continue;
    }
    if (byName.get(edge.source) !== 'view' || byName.get(edge.target) !== 'viewmodel') {
      continue;
    }
    links.push([edge.source, edge.target]);
  }

  return {
    nodes: model.nodes.length,
    edges: model.edges.length,
    findings: model.findings.length,
    byLayer: countBy(model.nodes, (node) => node.layer),
    byEdgeKind: countBy(model.edges, (edge) => edge.kind),
    byFindingKind: countBy(model.findings, (finding) => finding.kind),
    types: model.nodes
      .map((node) => [node.id, node.layer] as [string, string])
      .sort((left, right) => compareText(left[0], right[0]) || compareText(left[1], right[1])),
    links: sortedPairs(links),
  };
}

/** The identity a finding is matched on: its kind and the sorted names it is about. */
function findingKey(kind: string, types: string[]): string {
  return `${kind}\u0000${[...types].sort().join(',')}`;
}

/** The first named type of a finding, which is the name a mismatch prints. */
function findingName(finding: { nodeIds: string[]; location?: { file: string } }): string {
  return finding.nodeIds[0] ?? finding.location?.file ?? 'unknown type';
}

function typeMismatches(
  runTypes: [string, string][],
  record: ExpectedAnswerRecord,
): Mismatch[] {
  const expected = new Map(record.types.map((type) => [type.name, type.layer]));
  const actual = new Map(runTypes);
  const mismatches: Mismatch[] = [];

  for (const [name, layer] of expected) {
    const produced = actual.get(name);
    if (produced === undefined) {
      mismatches.push({ item: 'type', name, expected: layer, actual: null });
      continue;
    }
    if (produced !== layer) {
      mismatches.push({ item: 'type', name, expected: layer, actual: produced });
    }
  }

  for (const [name, layer] of actual) {
    if (!expected.has(name)) {
      mismatches.push({ item: 'type', name, expected: 'not in the record', actual: layer });
    }
  }

  return mismatches;
}

function linkMismatches(runLinks: [string, string][], record: ExpectedAnswerRecord): Mismatch[] {
  const expected = new Set(record.links.map((link) => `${link.view}\u0000${link.viewModel}`));
  const actual = new Set(runLinks.map((link) => `${link[0]}\u0000${link[1]}`));
  const mismatches: Mismatch[] = [];

  for (const link of record.links) {
    const key = `${link.view}\u0000${link.viewModel}`;
    if (!actual.has(key)) {
      mismatches.push({ item: 'link', name: link.view, expected: link.viewModel, actual: null });
    }
  }
  for (const [view, viewModel] of runLinks) {
    if (!expected.has(`${view}\u0000${viewModel}`)) {
      mismatches.push({ item: 'link', name: view, expected: 'no link', actual: viewModel });
    }
  }

  return mismatches;
}

function findingMismatches(model: GraphModel, record: ExpectedAnswerRecord): Mismatch[] {
  const expected = new Map(
    record.findings.map((finding) => [findingKey(finding.kind, finding.types), finding]),
  );
  const actual = new Map(
    model.findings.map((finding) => [findingKey(finding.kind, finding.nodeIds), finding]),
  );
  const mismatches: Mismatch[] = [];

  for (const [key, finding] of expected) {
    if (!actual.has(key)) {
      mismatches.push({
        item: 'finding',
        name: [...finding.types].sort()[0] ?? 'unknown type',
        expected: finding.kind,
        actual: null,
      });
    }
  }
  for (const [key, finding] of actual) {
    if (!expected.has(key)) {
      mismatches.push({
        item: 'finding',
        name: findingName(finding),
        expected: 'no finding',
        actual: finding.kind,
      });
    }
  }

  return mismatches;
}

function condition(mismatches: Mismatch[]): ConditionReport {
  return mismatches.length === 0
    ? { pass: true, mismatches: [] }
    : { pass: false, mismatches };
}

/**
 * One mismatch per failing item, across types, links and findings.
 *
 * An empty array in the record is a claim of "none" and is checked like any other: a type the run
 * produces that the record does not list fails and names that type.
 */
export function compareWithRecord(model: GraphModel, record: ExpectedAnswerRecord): ValidationReport {
  const measurements = measure(model);
  return {
    measurements,
    layers: condition(typeMismatches(measurements.types, record)),
    links: condition(linkMismatches(measurements.links, record)),
    findings: condition(findingMismatches(model, record)),
  };
}

/** Folds one check of a reply into the attempts record, keeping the order they happened in. */
export function recordAttempt(
  existing: ImportAttempts | undefined,
  repository: string,
  sha256: string,
  problems: ProfileProblem[],
): ImportAttempts {
  return {
    schemaVersion: 1,
    repository,
    attempts: [...(existing?.attempts ?? []), { sha256, problems }],
  };
}

/**
 * True only when there is exactly one recorded attempt and it carried no problem.
 *
 * A first reply carrying a problem is recorded rather than quietly retried, and deleting the attempts
 * file starts a fresh first attempt — which is visible in git.
 */
export function firstAttemptClean(attempts: ImportAttempts | undefined): boolean {
  return attempts?.attempts.length === 1 && attempts.attempts[0].problems.length === 0;
}

/** The import condition, which needs the attempts record rather than the model. */
export function importCondition(attempts: ImportAttempts | undefined): ConditionReport {
  const count = attempts?.attempts.length ?? 0;
  if (count === 0) {
    return { pass: false, detail: 'no reply has been checked yet' };
  }
  const first = attempts!.attempts[0].problems.length;
  return {
    pass: count === 1 && first === 0,
    detail: count === 1
      ? `1 attempt, ${first} ${first === 1 ? 'problem' : 'problems'}`
      : `${count} attempts; only a single clean first reply passes`,
  };
}

/** A recorded result, with the verdict derived so a partial success cannot be written. */
export function buildResult(
  repository: string,
  profile: { sha256: string; problems: ProfileProblem[] },
  report: ValidationReport,
  importReport: ConditionReport,
  invariants: ConditionReport,
): ValidationResult {
  const conditions: ValidationConditions = {
    layers: report.layers,
    links: report.links,
    findings: report.findings,
    import: importReport,
    invariants,
  };
  const verdict = Object.values(conditions).every((entry) => entry.pass) ? 'pass' : 'fail';
  return {
    schemaVersion: 1,
    repository,
    profile,
    measurements: report.measurements,
    conditions,
    verdict,
  };
}

/** The layers an expected type may name, so a record's reader sees the four names it accepts. */
export const RECORDED_LAYERS: ProfileLayer[] = ['view', 'viewmodel', 'service', 'model'];
