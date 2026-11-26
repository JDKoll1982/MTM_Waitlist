/**
 * The headless validation command.
 *
 * `out/validate.js` analyses a checkout given its path and judges the result without an editor open
 * and without a dependency of its own. Every mode prints plain lines in a fixed order, none of them
 * waits for input, and none of them writes inside the checkout it was given, so the same run serves a
 * person reading the output and a script grepping it.
 *
 * The pure half — what a run measured, and how it compares with a record — lives in
 * `graph/validation.ts`. This file is the part that touches a disk.
 */

import { createHash } from 'node:crypto';
import { execFileSync } from 'node:child_process';
import { existsSync, mkdirSync, readFileSync, readdirSync, writeFileSync } from 'node:fs';
import { basename, dirname, join, resolve } from 'node:path';

import { analyzeWorkspace } from './graph/analyze';
import { compileConventions, evaluateLayerRules, type CompiledConventions } from './graph/conventions';
import type { GraphModel, ProfileEvidence } from './graph/model';
import {
  PROFILE_FILE_NAME,
  parseProfile,
  recordsNothing,
  type ProfileProblem,
  type RepositoryProfile,
} from './graph/profile';
import { buildSetupPrompt } from './graph/profilePrompt';
import { BUILT_IN_SELECTION, type FileSelection } from './graph/files';
import { ANALYSIS_FILE_CAP, assessCompatibility, buildUpdateRequest, slugFor, type CompatibilityVerdict } from './graph/compatibility';
import { collectSources, inspectFolder, type Inspection } from './graph/inspect';
import { gatherStructure, writeUpdateRequest } from './structure';
import type { ImportAttempts } from './graph/validation';
import {
  buildResult,
  compareWithRecord,
  firstAttemptClean,
  importCondition,
  measure,
  type ConditionReport,
  type ExpectedAnswerRecord,
  type ExpectedFinding,
  type ExpectedLink,
  type ExpectedType,
  type Measurement,
  type ValidationConditions,
  type ValidationResult,
} from './graph/validation';

/** The extension's own root, which is where the grammar asset and the media folder live. */
const extensionRoot = resolve(__dirname, '..');

/** The C# grammar, copied beside the bundles by `npm run compile`. */
const grammarWasmPath = join(extensionRoot, 'media', 'tree-sitter-c_sharp.wasm');

/**
 * Where this command keeps the records it writes.
 *
 * It is the spec that defines the validation, two folders up from the extension, and never a path
 * inside a checkout: a checkout may gain one profile file and nothing else.
 */
const SPEC_FOLDER = join(extensionRoot, '..', '..', 'specs', '023-extension-repo-profile');

/** A high node cap: the measurement is of the whole checkout, so nothing may be trimmed away. */
const NODE_CAP = 40000;

/* ------------------------------------------------------------------ invocation */

type Mode = 'none' | 'inspect' | 'prompt' | 'check' | 'compatibility' | 'request' | 'expected' | 'record' | 'compare';

interface Invocation {
  /** The checkout this run is about. */
  root?: string;
  mode: Mode;
  /** The reply file `--check` reads. */
  check?: string;
  /** The expected-answer record `--record` measures against. */
  expected?: string;
  /** Where `--expected` writes the record it produces. */
  write?: string;
  /** Where `--record` writes the result. */
  result?: string;
  /** The two result files `--compare` reads. */
  compare: string[];
  /** True when `--inspect` prints one JSON object rather than its sections. */
  json: boolean;
  /** True when the check of a reply should be recorded, so a first attempt can be told apart. */
  recordAttempt: boolean;
}

/**
 * Reads the flags without asking anything.
 *
 * A mode that needs a value following it takes the next argument; a flag that is not one this
 * command knows is left for `usage` rather than guessed at, so a typo fails loudly.
 */
function parseInvocation(argv: string[]): Invocation {
  const invocation: Invocation = { mode: 'none', recordAttempt: false, compare: [], json: false };
  const positional: string[] = [];

  for (let index = 0; index < argv.length; index += 1) {
    const argument = argv[index];
    if (argument === '--inspect') {
      invocation.mode = 'inspect';
      continue;
    }
    if (argument === '--json') {
      invocation.json = true;
      continue;
    }
    if (argument === '--prompt') {
      invocation.mode = 'prompt';
      continue;
    }
    if (argument === '--compatibility') {
      invocation.mode = 'compatibility';
      continue;
    }
    if (argument === '--request') {
      invocation.mode = 'request';
      continue;
    }
    if (argument === '--compare') {
      invocation.mode = 'compare';
      invocation.compare = [argv[index + 1], argv[index + 2]];
      index += 2;
      continue;
    }
    if (argument === '--check') {
      invocation.mode = 'check';
      invocation.check = argv[index + 1];
      index += 1;
      continue;
    }
    if (argument === '--expected') {
      invocation.mode = 'expected';
      invocation.write = argv[index + 1];
      index += 1;
      continue;
    }
    if (argument === '--record') {
      invocation.mode = 'record';
      invocation.expected = argv[index + 1];
      index += 1;
      continue;
    }
    if (argument === '--result') {
      invocation.result = argv[index + 1];
      index += 1;
      continue;
    }
    if (argument === '--record-attempt') {
      invocation.recordAttempt = true;
      continue;
    }
    if (argument.startsWith('--')) {
      invocation.mode = 'none';
      return invocation;
    }
    positional.push(argument);
  }

  invocation.root = positional[0] ? resolve(positional[0]) : undefined;
  if (invocation.mode !== 'none' && invocation.mode !== 'compare' && !invocation.root) {
    invocation.mode = 'none';
  }
  return invocation;
}

function usage(): void {
  console.log('usage:');
  console.log('  node out/validate.js <checkout> --inspect [--json]');
  console.log('  node out/validate.js <checkout> --prompt');
  console.log('  node out/validate.js <checkout> --check <reply-file> [--record-attempt]');
  console.log('  node out/validate.js <checkout> --compatibility');
  console.log('  node out/validate.js <checkout> --request');
  console.log('  node out/validate.js <checkout> --expected <record.json>');
  console.log('  node out/validate.js <checkout> --record <record.json> --result <result.json>');
  console.log('  node out/validate.js --compare <first.json> <second.json>');
}

/* --------------------------------------------------------------------- reading */

/** The selection a run reads by: the profile's when it declares one, the built-in one otherwise. */
function selectionFor(profile?: RepositoryProfile): FileSelection {
  return {
    include: profile?.files?.include ?? BUILT_IN_SELECTION.include,
    exclude: profile?.files?.exclude ?? BUILT_IN_SELECTION.exclude,
  };
}

/** Reads the checkout's profile, if it has one. A malformed file is reported, never thrown. */
interface ProfileFile {
  /** The bytes the result records the digest of. */
  text: string;
  profile?: RepositoryProfile;
  problems: ProfileProblem[];
}

function readProfile(root: string): ProfileFile | undefined {
  const path = join(root, PROFILE_FILE_NAME);
  if (!existsSync(path)) {
    return undefined;
  }
  try {
    const text = readFileSync(path, 'utf8');
    const parsed = parseProfile(text);
    return { text, profile: parsed.profile, problems: parsed.problems };
  } catch {
    return undefined;
  }
}

/** The checkout, analysed with the conventions it declares — or the built-in ones when it declares none. */
async function analyse(root: string, profile?: RepositoryProfile): Promise<GraphModel> {
  const sources = collectSources(root, selectionFor(profile));
  return analyzeWorkspace({
    workspaceRoot: root,
    csharp: sources.csharp,
    xaml: sources.xaml,
    grammarWasmPath,
    includeExternalTypes: false,
    maxNodes: NODE_CAP,
    conventions: compileConventions(profile),
  });
}

/** The evidence a prompt quotes, with the same empty fallback the host uses for a cached model. */
function evidenceOf(model: GraphModel): ProfileEvidence {  return model.profileEvidence ?? {
    nameSuffixes: [], namePrefixes: [], namespaceSegments: [], calls: [], baseTypes: [],
    methodAttributes: [], fieldAttributes: [], xamlRootTags: [],
    csharpFiles: model.stats.csharpFiles,
    xamlFiles: model.stats.xamlFiles,
  };
}

/* ------------------------------------------------------------------- writing */

/** The attempts record for one checkout, under the spec that defines the validation. */
function attemptsPath(slug: string): string {
  return join(SPEC_FOLDER, 'results', `${slug}.attempts.json`);
}

function readAttempts(path: string): ImportAttempts | undefined {
  if (!existsSync(path)) {
    return undefined;
  }
  try {
    return JSON.parse(readFileSync(path, 'utf8')) as ImportAttempts;
  } catch {
    return undefined;
  }
}

/** The expected-answer record, or undefined when the file does not carry the three lists. */
function readRecord(path: string): ExpectedAnswerRecord | undefined {
  try {
    const record = JSON.parse(readFileSync(path, 'utf8')) as ExpectedAnswerRecord;
    return record.schemaVersion === 1
      && Array.isArray(record.types)
      && Array.isArray(record.links)
      && Array.isArray(record.findings)
      ? record
      : undefined;
  } catch {
    return undefined;
  }
}

/**
 * The import condition passes on one thing only: exactly one recorded attempt, and it carried no
 * problem. Its detail is the same sentence the host would show, so the failure names its count.
 */
function importReport(attempts: ImportAttempts | undefined): ConditionReport {
  return { pass: firstAttemptClean(attempts), detail: importCondition(attempts).detail };
}

/**
 * The extension's own invariants, run against this checkout as a child process.
 *
 * Only the summary line is kept: it carries no path, so the recorded result stays portable, and the
 * exit code is what decides the condition.
 */
function invariantsFor(root: string): ConditionReport {
  try {
    const output = execFileSync(process.execPath, [join(extensionRoot, 'out', 'selftest.js'), root], {
      encoding: 'utf8',
    });
    const last = output.trimEnd().split('\n').pop() ?? 'the invariants reported nothing';
    return { pass: true, detail: sanitised(last, root) };
  } catch (error) {
    const failure = error as { status?: number };
    // The exit code is the fact; the stack a crash prints would carry machine paths into a record
    // that has to stay portable.
    return { pass: false, detail: `the invariants exited ${failure.status ?? 'without a status'}` };
  }
}

/** A line with the machine's own roots taken out, so what is recorded stays portable. */
function sanitised(line: string, root: string): string {
  return line.split(root).join('<checkout>').split(extensionRoot).join('<extension>');
}

/* --------------------------------------------------------------------- modes */

/** The first fenced block of a reply, or the whole reply when it carries none — the import rule. */
function replyText(reply: string): string {
  const fenced = /```(?:json)?\s*\r?\n([\s\S]*?)```/i.exec(reply);
  return fenced ? fenced[1] : reply;
}

/** The extension's own version, quoted in a record's `source` so a reviewer knows what wrote it. */
function extensionVersion(): string {
  try {
    const manifest = JSON.parse(readFileSync(join(extensionRoot, 'package.json'), 'utf8')) as { version?: string };
    return manifest.version ?? 'unknown';
  } catch {
    return 'unknown';
  }
}

/** A code-point comparison, so a record's order does not depend on the machine's collation rules. */
function byText(left: string, right: string): number {
  if (left === right) {
    return 0;
  }
  return left < right ? -1 : 1;
}

/**
 * The record `--expected` writes: every declared type, every stated link and every finding the rules
 * imply, in a stable order.
 *
 * The types come from the inspection — the merged declarations, each placed by the checkout's own
 * compiled rules — because that is the surface a profile author is given and the record has to state
 * the same facts. The links and the findings come from one analysis of the same checkout on a fresh
 * read. They have exactly one implementation in this extension, and re-deriving them in the producer
 * would be the purpose-written second reading FR-030 rules out: the attempt made that way reproduced
 * the layer counts and still disagreed on the findings.
 *
 * What the producer never reads is a *result*: no recorded run, no mismatch list, no `results/` file.
 * The record is written before the checkout is measured and is not edited after it, which is what
 * keeps the comparison a test of the checkout rather than a transcript of the run.
 */
function buildRecord(
  repository: string,
  hasProfile: boolean,
  inspection: Inspection,
  conventions: CompiledConventions,
  model: GraphModel,
): ExpectedAnswerRecord {
  const viewNames = new Set(inspection.xamlClasses.map((entry) => entry.name));
  const declared = new Map<string, ExpectedType>();
  for (const entry of inspection.declarations!.values()) {
    declared.set(entry.decl.fullName, {
      name: entry.decl.fullName,
      file: entry.file,
      layer: viewNames.has(entry.decl.fullName)
        ? 'view'
        : evaluateLayerRules(conventions.layerRules, conventions.layerDefault, entry.decl),
    });
  }
  // A view whose markup names a class with no C# declaration is still a node in the analysis, so the
  // record names it too, by the XAML file that declares it.
  for (const entry of inspection.xamlClasses) {
    if (!declared.has(entry.name)) {
      declared.set(entry.name, { name: entry.name, file: entry.file, layer: 'view' });
    }
  }
  const types: ExpectedType[] = [...declared.values()].sort((left, right) => byText(left.name, right.name));

  const links: ExpectedLink[] = measure(model).links
    .map(([view, viewModel]) => ({ view, viewModel }));

  const findings: ExpectedFinding[] = model.findings
    .map((finding) => ({ kind: finding.kind, types: [...finding.nodeIds].sort(byText) }))
    .sort((left, right) => byText(findingOrder(left), findingOrder(right)));

  const profileNote = hasProfile
    ? 'The conventions are the ones the checkout\'s own profile declares.'
    : 'No profile was present, so the conventions are the built-in ones.';

  return {
    schemaVersion: 1,
    repository,
    source: `Produced by node out/validate.js --expected, the inspection surface of `
      + `csharp-xaml-graph ${extensionVersion()}, from this checkout's declarations and markup before any `
      + `measured run. ${profileNote} No run output, absolute path, duration or timestamp is quoted.`,
    types,
    links,
    findings,
  };
}

/** A finding's place in the record's order: its kind, then the names it is about. */
function findingOrder(finding: ExpectedFinding): string {
  return `${finding.kind}\u0000${finding.types.join(',')}`;
}

/**
 * `--inspect`: the folder as it is, reported section by section, and nothing decided.
 *
 * The compatibility facts come first because they are what decides whether the rest is worth reading.
 * The evidence follows, in the same derivation the setup prompt quotes, so an author given this output
 * and an author given the prompt are looking at one reading of one repository.
 *
 * It writes nothing, waits for no input and needs no editor. A folder it cannot list exits 2; a folder
 * it can list exits 0 even when it declares no types, because an empty repository is an answer.
 */
async function runInspect(root: string, asJson: boolean): Promise<number> {
  const inspection = await inspectFolder(root, { grammarWasmPath });
  const verdict = assessCompatibility(inspection.structure, ANALYSIS_FILE_CAP, slugFor(basename(root)));

  if (asJson) {
    console.log(JSON.stringify({
      compatibility: verdict.verdict,
      facts: verdict.facts,
      limits: verdict.limits,
      files: inspection.files,
      evidence: inspection.evidence ?? null,
    }, null, 2));
  } else {
    printVerdict(verdict);
    if (inspection.evidence) {
      printEvidence(inspection);
    }
  }

  if (!inspection.readable) {
    console.log(`inspection: ${inspection.problem}`);
    return 2;
  }
  return 0;
}

/**
 * `--expected`: write the record a checkout is judged against, from the checkout itself.
 *
 * The profile, when the checkout has one, decides the conventions and the file selection, so the
 * record states what that checkout's own rules say rather than what the built-in ones would. The
 * record is written to the path it is given — under this repository's `specs/`, never inside the
 * checkout — and the run prints its three counts and the path, so the numbers can be read against
 * `baseline.md` before anything is measured.
 */
async function runExpected(root: string, target: string): Promise<number> {
  const profileFile = readProfile(root);
  const conventions = compileConventions(profileFile?.profile);
  const slug = slugFor(basename(root));

  let inspection: Inspection;
  let model: GraphModel;
  try {
    inspection = await inspectFolder(root, {
      grammarWasmPath,
      selection: selectionFor(profileFile?.profile),
      conventions,
    });
    if (!inspection.evidence || !inspection.declarations) {
      console.log(`inspection: ${inspection.problem ?? 'no evidence could be gathered'}`);
      return 2;
    }
    model = await analyse(root, profileFile?.profile);
  } catch (error) {
    console.log(`inspection: ${(error as Error).message}`);
    return 2;
  }

  const record = buildRecord(slug, profileFile?.profile !== undefined, inspection, conventions, model);
  const targetPath = resolve(target);
  mkdirSync(dirname(targetPath), { recursive: true });
  writeFileSync(targetPath, `${JSON.stringify(record, null, 2)}\n`, 'utf8');

  console.log(`repository: ${slug}`);
  console.log(`types: ${record.types.length}`);
  const layers = new Map<string, number>();
  for (const type of record.types) {
    layers.set(type.layer, (layers.get(type.layer) ?? 0) + 1);
  }
  console.log(`by layer: ${[...layers].sort(([left], [right]) => byText(left, right)).map(([layer, count]) => `${layer}=${count}`).join(', ')}`);
  console.log(`links: ${record.links.length}`);
  console.log(`findings: ${record.findings.length}`);
  const kinds = new Map<string, number>();
  for (const finding of record.findings) {
    kinds.set(finding.kind, (kinds.get(finding.kind) ?? 0) + 1);
  }
  console.log(`by finding kind: ${[...kinds].sort(([left], [right]) => byText(left, right)).map(([kind, count]) => `${kind}=${count}`).join(', ') || 'none'}`);
  console.log(`record: ${targetPath}`);
  return 0;
}

/**
 * `--prompt`: the text the setup command would put in chat, and nothing else.
 *
 * The checkout is analysed with no profile, because this prompt exists for the repository that has
 * none; the evidence it quotes is the repository's own types, namespaces and calls.
 */
async function runPrompt(root: string): Promise<number> {
  const model = await analyse(root);
  process.stdout.write(`${buildSetupPrompt(evidenceOf(model), basename(root))}\n`);
  return 0;
}

/**
 * `--check`: one line per problem, and the reply either accepted or refused.
 *
 * The reply is read with the import command's rule, so a fenced block and a bare object are both
 * accepted, and it is parsed with the same parser the command uses. `--record-attempt` is the only
 * thing that writes, and what it writes is a hash and the problems, so a first reply carrying a
 * problem is recorded rather than quietly retried.
 */
async function runCheck(root: string, reply: string, recordAttempt: boolean): Promise<number> {
  let text: string;
  try {
    text = replyText(readFileSync(resolve(reply), 'utf8'));
  } catch (error) {
    console.log(`reply: ${(error as Error).message}`);
    return 1;
  }

  const parsed = parseProfile(text);
  for (const problem of parsed.problems) {
    console.log(`${problem.severity}: ${problem.path ? `${problem.path}: ` : ''}${problem.message}`);
  }

  if (recordAttempt) {
    const slug = slugFor(basename(root));
    const sha256 = createHash('sha256').update(text, 'utf8').digest('hex');
    const path = attemptsPath(slug);
    const existing = readAttempts(path);
    const next: ImportAttempts = {
      schemaVersion: 1,
      repository: slug,
      attempts: [...(existing?.attempts ?? []), { sha256, problems: parsed.problems }],
    };
    mkdirSync(join(SPEC_FOLDER, 'results'), { recursive: true });
    writeFileSync(path, `${JSON.stringify(next, null, 2)}\n`, 'utf8');
    console.log(`attempts: ${path} (${next.attempts.length})`);
  }

  const accepted = parsed.profile !== undefined && !recordsNothing(parsed.profile);
  console.log(`result: ${accepted ? 'accepted' : 'refused'}`);
  return accepted ? 0 : 1;
}

/**
 * The verdict for one checkout, decided from a path walk alone.
 *
 * Nothing is opened and nothing is written, which is what lets this run before any profile question
 * and makes it safe on a checkout whose files must not change.
 */
function verdictFor(root: string): CompatibilityVerdict {
  return assessCompatibility(gatherStructure(root), ANALYSIS_FILE_CAP, slugFor(basename(root)));
}

/**
 * The verdict, its facts, then one line per structure it cannot handle with the evidence indented.
 *
 * A refusal names the structure rather than the repository, so the line can be acted on: `no C#
 * files` and `src/Other.fsproj` say what would have to change.
 */
function printVerdict(verdict: CompatibilityVerdict): void {
  console.log(`compatibility: ${verdict.verdict}`);
  console.log(`C# files: ${verdict.facts.csharpFiles}`);
  console.log(`XAML files: ${verdict.facts.xamlFiles}`);
  console.log(`projects: ${verdict.facts.projectFiles.length}`);
  console.log(`solutions: ${verdict.facts.solutionFiles.length}`);
  for (const reason of verdict.reasons) {
    console.log(`cannot analyse ${reason.structure}: ${reason.message}`);
    for (const item of reason.evidence) {
      console.log(`  ${item}`);
    }
  }
}

/**
 * The evidence sections, one per kind, each item on its own line and in the derivation's own order.
 *
 * An empty section prints `none` rather than disappearing, so a reader can tell "this repository has
 * no such evidence" from "this section was never asked for".
 */
function printEvidence(inspection: Inspection): void {
  const evidence = inspection.evidence!;
  const section = (title: string, lines: string[]): void => {
    console.log(`${title}:`);
    if (lines.length === 0) {
      console.log('  none');
      return;
    }
    for (const line of lines) {
      console.log(`  ${line}`);
    }
  };

  console.log(`files read: ${inspection.files.csharp} C#, ${inspection.files.xaml} XAML`);
  if (inspection.files.truncated) {
    console.log('files read: the walk stopped at the cap, so the two counts are a floor');
  }
  section('name suffixes', evidence.nameSuffixes.map((item) => `${item.name}: ${item.examples.join(', ')}`));
  section('name prefixes', evidence.namePrefixes.map((item) => `${item.name}: ${item.examples.join(', ')}`));
  section('namespace segments', evidence.namespaceSegments.map((item) => `${item.segment}: ${item.types} types`));
  section('calls', evidence.calls.map((item) => `${item.name}<${item.typeArguments}>: ${item.example}`));
  section('base types', evidence.baseTypes);
  section('method attributes', evidence.methodAttributes);
  section('field attributes', evidence.fieldAttributes);
  section('xaml root tags', evidence.xamlRootTags.map((item) => `${item.tag}: ${item.files} files`));
}

/** `--compatibility`: the verdict and nothing else. Exit 0, 3 or 2 as the contract states. */function runCompatibility(root: string): number {
  const verdict = verdictFor(root);
  printVerdict(verdict);
  if (verdict.verdict === 'compatible') {
    return 0;
  }
  return verdict.verdict === 'incompatible' ? 3 : 2;
}

/**
 * `--request`: the yes answer, for use without a person present.
 *
 * It is the same verdict, and only an incompatible checkout produces a file. A compatible one writes
 * nothing and says so; an unknown one exits 2, because a request built on a verdict that was never
 * reached would name structures nobody observed.
 */
function runRequest(root: string): number {
  const verdict = verdictFor(root);
  printVerdict(verdict);
  if (verdict.verdict === 'unknown') {
    console.log('request: none, because no verdict could be reached');
    return 2;
  }
  if (verdict.verdict === 'compatible') {
    console.log('request: none, because this checkout is compatible');
    return 0;
  }

  const written = writeUpdateRequest(
    extensionRoot,
    slugFor(basename(root)),
    buildUpdateRequest(verdict),
  );
  if (!written.ok) {
    console.log(`request: ${written.path} could not be written (${written.error})`);
    return 2;
  }
  console.log(`request: ${written.path}`);
  return 0;
}

/**
 * `--record`: measure one checkout against its record and write what was measured.
 *
 * Nothing here can produce a pass it did not observe. A missing record, a missing profile or a
 * throwing analysis exits 2 and writes no file, so a checkout that has moved is recorded as not run
 * rather than as a pass, and `import` passes on one thing only: exactly one recorded attempt, clean.
 */
async function runRecord(root: string, expectedPath: string, resultPath: string): Promise<number> {
  const slug = slugFor(basename(root));

  const profileFile = readProfile(root);
  if (!profileFile || !profileFile.profile) {
    console.log(`profile: ${PROFILE_FILE_NAME} could not be read from ${slug}`);
    return 2;
  }
  const record = readRecord(resolve(expectedPath));
  if (!record) {
    console.log(`record: ${resolve(expectedPath)} could not be read`);
    return 2;
  }

  let model: GraphModel;
  try {
    model = await analyse(root, profileFile.profile);
  } catch (error) {
    console.log(`analysis: ${(error as Error).message}`);
    return 2;
  }

  const report = compareWithRecord(model, record);
  const result = buildResult(
    slug,
    {
      sha256: createHash('sha256').update(profileFile.text, 'utf8').digest('hex'),
      problems: profileFile.problems,
    },
    report,
    importReport(readAttempts(attemptsPath(slug))),
    invariantsFor(root),
  );

  console.log(`repository: ${slug}`);
  printMeasurement(report.measurements);
  printConditions(result.conditions);
  printMismatches(result.conditions);
  console.log(`VERDICT: ${result.verdict}`);

  const target = resolve(resultPath);
  mkdirSync(dirname(target), { recursive: true });
  writeFileSync(target, `${JSON.stringify(result, null, 2)}\n`, 'utf8');
  console.log(`result: ${target}`);
  return result.verdict === 'pass' ? 0 : 1;
}

/* ------------------------------------------------------------------- printing */

/** The three counts, then one line per label, sorted by the measurement's own order. */
function printMeasurement(measurements: Measurement): void {
  console.log(`nodes: ${measurements.nodes}`);
  console.log(`edges: ${measurements.edges}`);
  console.log(`findings: ${measurements.findings}`);
  const labels = (counts: Record<string, number>): string =>
    Object.entries(counts).map(([label, count]) => `${label}=${count}`).join(', ') || 'none';
  console.log(`by layer: ${labels(measurements.byLayer)}`);
  console.log(`by edge kind: ${labels(measurements.byEdgeKind)}`);
  console.log(`by finding kind: ${labels(measurements.byFindingKind)}`);
}

/** The five conditions, each PASS or FAIL, in the order the bar is assessed in. */
function printConditions(conditions: ValidationConditions): void {
  const order: (keyof ValidationConditions)[] = ['layers', 'links', 'findings', 'import', 'invariants'];
  for (const key of order) {
    const report = conditions[key];
    console.log(`${key}: ${report.pass ? 'PASS' : 'FAIL'}${report.detail ? ` (${report.detail})` : ''}`);
  }
}

/** Every failing item on its own line, so a checkout below the bar can be read from the output. */
function printMismatches(conditions: ValidationConditions): void {
  const order: (keyof ValidationConditions)[] = ['layers', 'links', 'findings'];
  for (const key of order) {
    for (const mismatch of conditions[key].mismatches ?? []) {
      console.log(`${mismatch.item} ${mismatch.name}: expected ${mismatch.expected}, got ${mismatch.actual ?? 'nothing'}`);
    }
  }
}

/* -------------------------------------------------------------------- comparing */

/**
 * The three things `--compare` is about, rebuilt in a fixed key order.
 *
 * The profile is deliberately absent: two runs judged by two textually different profiles express the
 * same conventions, and a hash that differs while every measurement agrees is not a difference.
 */
function compared(result: ValidationResult): unknown {
  const mismatches = (key: 'layers' | 'links' | 'findings') => result.conditions[key].mismatches ?? [];
  return {
    measurements: result.measurements,
    mismatches: {
      layers: mismatches('layers'),
      links: mismatches('links'),
      findings: mismatches('findings'),
    },
    conditions: {
      layers: result.conditions.layers.pass,
      links: result.conditions.links.pass,
      findings: result.conditions.findings.pass,
      import: result.conditions.import.pass,
      invariants: result.conditions.invariants.pass,
    },
  };
}

/** The same value with every object's keys sorted, so a file's key order cannot decide anything. */
function canonical(value: unknown): unknown {
  if (Array.isArray(value)) {
    return value.map(canonical);
  }
  if (value && typeof value === 'object') {
    const record = value as Record<string, unknown>;
    return Object.fromEntries(
      Object.keys(record).sort().map((key) => [key, canonical(record[key])]),
    );
  }
  return value;
}

/** The first dotted field whose value differs, or undefined when the two agree throughout. */
function firstDifference(
  left: unknown,
  right: unknown,
  path: string,
): { path: string; left: string; right: string } | undefined {
  if (JSON.stringify(left) === JSON.stringify(right)) {
    return undefined;
  }
  if (Array.isArray(left) && Array.isArray(right)) {
    for (let index = 0; index < Math.max(left.length, right.length); index += 1) {
      const found = firstDifference(left[index], right[index], `${path}[${index}]`);
      if (found) {
        return found;
      }
    }
    return { path, left: `${left.length} items`, right: `${right.length} items` };
  }
  if (left && right && typeof left === 'object' && typeof right === 'object') {
    const keys = [...new Set([
      ...Object.keys(left as object),
      ...Object.keys(right as object),
    ])].sort();
    for (const key of keys) {
      const found = firstDifference(
        (left as Record<string, unknown>)[key],
        (right as Record<string, unknown>)[key],
        path ? `${path}.${key}` : key,
      );
      if (found) {
        return found;
      }
    }
    return { path, left: '(differs)', right: '(differs)' };
  }
  return { path, left: JSON.stringify(left) ?? 'nothing', right: JSON.stringify(right) ?? 'nothing' };
}

/**
 * `--compare`: two recorded results, and whether a second run reproduced the first.
 *
 * It reads the two files and nothing else, so it works on a machine that no longer has the checkouts
 * it is about.
 */
function runCompare(firstPath: string, secondPath: string): number {
  const first = readRecordedResult(firstPath);
  const second = readRecordedResult(secondPath);
  if (!first || !second) {
    console.log(`results: ${first ? secondPath : firstPath} could not be read`);
    return 2;
  }

  const difference = firstDifference(
    canonical(compared(first)),
    canonical(compared(second)),
    '',
  );
  if (!difference) {
    console.log('IDENTICAL');
    return 0;
  }
  console.log(`DIFFERENT at ${difference.path}: ${difference.left}, ${difference.right}`);
  return 1;
}

/** A recorded result, or undefined when the file is not one. */
function readRecordedResult(path: string): ValidationResult | undefined {
  try {
    const result = JSON.parse(readFileSync(resolve(path), 'utf8')) as ValidationResult;
    return result.schemaVersion === 1 && result.measurements !== undefined && result.conditions !== undefined
      ? result
      : undefined;
  } catch {
    return undefined;
  }
}

/* ----------------------------------------------------------------------- main */
async function main(argv: string[]): Promise<number> {
  const invocation = parseInvocation(argv);

  if (invocation.mode === 'compare') {
    if (invocation.compare.length < 2 || !invocation.compare[0] || !invocation.compare[1]) {
      usage();
      return 2;
    }
    return runCompare(invocation.compare[0], invocation.compare[1]);
  }
  if (invocation.mode === 'none' || !invocation.root) {
    usage();
    return 2;
  }
  if (invocation.mode === 'check' && !invocation.check) {
    usage();
    return 2;
  }
  if (!existsSync(invocation.root)) {
    console.log(`checkout: ${invocation.root} could not be read`);
    return 2;
  }

  try {
    if (invocation.mode === 'inspect') {
      return await runInspect(invocation.root, invocation.json);
    }
    if (invocation.mode === 'prompt') {
      return await runPrompt(invocation.root);
    }
    if (invocation.mode === 'compatibility') {
      return runCompatibility(invocation.root);
    }
    if (invocation.mode === 'request') {
      return runRequest(invocation.root);
    }
    if (invocation.mode === 'expected') {
      if (!invocation.write) {
        usage();
        return 2;
      }
      return await runExpected(invocation.root, invocation.write);
    }
    if (invocation.mode === 'record') {
      if (!invocation.expected || !invocation.result) {
        usage();
        return 2;
      }
      return await runRecord(invocation.root, invocation.expected, invocation.result);
    }
    return await runCheck(invocation.root, invocation.check!, invocation.recordAttempt);
  } catch (error) {
    console.log(`failed: ${(error as Error).message}`);
    return 2;
  }
}

void main(process.argv.slice(2)).then((code) => {
  process.exitCode = code;
});
