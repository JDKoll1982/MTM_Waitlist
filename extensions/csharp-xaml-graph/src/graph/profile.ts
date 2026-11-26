import type { NodeLayer } from './model';
import { BUILT_IN_SELECTION } from './files';
import {
  DECL_KIND_VALUES,
  GENERATED_MEMBER_FORM_VALUES,
  GENERATED_MEMBER_ON_VALUES,
  PROFILE_LAYER_VALUES,
  contractKeys,
} from './profileContract';

/**
 * The conventions one workspace folder declares, and the built-in conventions that stand in for
 * anything it does not declare.
 *
 * The shape is `contracts/profile.schema.json` in the feature that introduced it: every section and
 * every key inside it is optional, an absent key keeps the built-in value, and a present array
 * replaces the built-in array rather than extending it. Nothing here imports `vscode`, so the format
 * is exercised by the test suite without an extension host.
 */

/** The name of the file, at the workspace folder root, that a repository commits. */
export const PROFILE_FILE_NAME = 'csharp-xaml-graph.profile.json';

/** The four layers a profile can place a type in. `external` is for referenced framework types. */
export type ProfileLayer = Exclude<NodeLayer, 'external'>;

const PROFILE_LAYERS: ProfileLayer[] = [...PROFILE_LAYER_VALUES];

/** Declaration kinds a layer rule can restrict itself to. */
export type DeclKind = 'class' | 'record' | 'struct' | 'interface' | 'enum';

const DECL_KINDS: DeclKind[] = [...DECL_KIND_VALUES];

export interface LayerRule {
  layer: ProfileLayer;
  kinds?: DeclKind[];
  nameSuffixes?: string[];
  namePrefixes?: string[];
  namespaceSegments?: string[];
  /** The rule does not apply when the name ends with one of these. */
  unlessNameSuffixes?: string[];
}

export interface LayersSection {
  /** In precedence order: the first rule that matches decides. */
  rules?: LayerRule[];
  default?: ProfileLayer;
}

export interface LinksSection {
  /** `x.Call<TA, TB>()` — draws a `resolves` edge and registers both types. */
  genericPairCalls?: string[];
  /** `x.Call(typeof(TA), typeof(TB))` — draws a `resolves` edge and registers the first type. */
  typeofPairCalls?: string[];
  /** `x.Call(nameof(P), typeof(TValue), typeof(TOwner))` — draws a `getservice` edge. */
  dependencyPropertyCall?: string;
  /** A method name matching this registers its type arguments. */
  addCallPattern?: string;
  /** Container lookups rather than plain calls. */
  serviceLocator?: string[];
  /** Cast keywords that bind the data context to an interface. */
  dataContextCasts?: string[];
}

export type GeneratedMemberForm = 'commandSuffix' | 'pascalCaseWithoutPrefix';

export type GeneratedMemberOn = 'method' | 'field';

export interface GeneratedMembersRule {
  /** Attribute name without brackets, for example `RelayCommand`. */
  attribute: string;
  on: GeneratedMemberOn;
  form: GeneratedMemberForm;
}

export interface FindingsSection {
  /** Type names never reported unused. */
  entryPoints?: string[];
  /** Base types that add no bindable members. */
  transparentBases?: string[];
  generatedMembers?: GeneratedMembersRule[];
}

export interface FilesSection {
  include?: string[];
  exclude?: string[];
}

export interface RepositoryProfile {
  version: 1;
  repository?: string;
  layers?: LayersSection;
  links?: LinksSection;
  findings?: FindingsSection;
  files?: FilesSection;
}

export type ProfileSeverity = 'warning' | 'error';

/**
 * One thing the reader could not use. `error` means the whole file was unusable and the built-in
 * conventions applied; `warning` means one declaration was skipped and the rest still applies.
 */
export interface ProfileProblem {
  /** Where in the profile, in dotted form with array indices: `layers.rules[2].kinds`. */
  path: string;
  /** One plain sentence: what was expected and what was found. */
  message: string;
  severity: ProfileSeverity;
}

export interface ParseProfileResult {
  /** Undefined when the file was absent, blank, or unusable as a whole. */
  profile?: RepositoryProfile;
  problems: ProfileProblem[];
}

/* -------------------------------------------------------------- built-in conventions */

const SERVICE_NAME_SUFFIXES = [
  'Service', 'Store', 'Provider', 'Reader', 'Writer', 'Catalog', 'Resolver',
  'Policy', 'Tracker', 'Hasher', 'Server', 'Broker', 'Gate', 'Factory', 'Client',
  'Database', 'Repository', 'Cache', 'Host', 'Pipeline', 'Notifier', 'Prompt',
];

const SERVICE_NAME_PREFIXES = ['Service_', 'Dao_', 'Repository_', 'Store_', 'Provider_', 'Client_'];

/*
 * The decision order `classifyType` used to carry in code, written out as rules. Each condition kind
 * is its own rule because a rule matches when EVERY condition it states holds, and the original
 * checks combined a name signal with a namespace signal as alternatives.
 */
const BUILT_IN_RULES: LayerRule[] = [
  { layer: 'service', kinds: ['interface'], nameSuffixes: SERVICE_NAME_SUFFIXES },
  { layer: 'service', kinds: ['interface'], namePrefixes: SERVICE_NAME_PREFIXES },
  { layer: 'service', kinds: ['interface'], namespaceSegments: ['Services'] },
  { layer: 'service', kinds: ['interface'], namespaceSegments: ['Contracts'] },
  { layer: 'model', kinds: ['interface'] },
  { layer: 'viewmodel', nameSuffixes: ['ViewModel'] },
  { layer: 'viewmodel', namePrefixes: ['ViewModel_', 'VM_'] },
  {
    layer: 'viewmodel',
    kinds: ['class'],
    namespaceSegments: ['ViewModels'],
    // Row models, records and navigation payloads live beside the view models that own them, so a
    // type whose name says "I am data" stays a model even in a `ViewModels` folder.
    unlessNameSuffixes: [
      'Data', 'Card', 'Row', 'Item', 'Option', 'Credentials', 'EventArgs', 'Args',
      'Outcome', 'Display', 'Result', 'Entry', 'Dto', 'Payload', 'Model',
    ],
  },
  { layer: 'model', kinds: ['enum'] },
  { layer: 'service', namespaceSegments: ['Services'] },
  { layer: 'service', nameSuffixes: SERVICE_NAME_SUFFIXES },
  { layer: 'service', namePrefixes: SERVICE_NAME_PREFIXES },
];

const BUILT_IN_LINKS: Required<LinksSection> = {
  genericPairCalls: ['Configure'],
  typeofPairCalls: ['RegisterRoute'],
  dependencyPropertyCall: 'Register',
  addCallPattern: '^Add[A-Z]',
  serviceLocator: ['GetService', 'GetRequiredService'],
  dataContextCasts: ['is', 'as'],
};

/**
 * Base types known to add no bindable members, so a missing member is genuinely missing.
 *
 * Framework bases matter here: a WinUI page derives from `Page`, a WPF page from `Page` or
 * `UserControl`, a MAUI page from `ContentPage`. None declares application members, and none is
 * declared in the workspace, so without this list every view would look like it had an unresolvable
 * base and no dead binding could ever be reported.
 */
const BUILT_IN_TRANSPARENT_BASES = [
  'ObservableObject', 'ObservableRecipient', 'ObservableValidator', 'ObservableMessage',
  'BindableBase', 'ViewModelBase',
  'Page', 'UserControl', 'Window', 'WindowEx', 'Application', 'ContentPage', 'ContentView',
  'ContentDialog', 'Form', 'Control', 'FrameworkElement', 'Component', 'UserControlBase',
];

const BUILT_IN_GENERATED_MEMBERS: GeneratedMembersRule[] = [
  { attribute: 'RelayCommand', on: 'method', form: 'commandSuffix' },
  { attribute: 'ObservableProperty', on: 'field', form: 'pascalCaseWithoutPrefix' },
];

const BUILT_IN_INCLUDE = BUILT_IN_SELECTION.include;

const BUILT_IN_EXCLUDE = BUILT_IN_SELECTION.exclude;

/**
 * The conventions the extension applies when a repository declares nothing.
 *
 * It is a complete profile rather than a set of loose constants so one code path serves both the
 * profiled and the unprofiled run: the graph a repository gets with no profile is the graph it got
 * before profiles existed.
 */
export function builtInProfile(): RepositoryProfile {
  return {
    version: 1,
    layers: { rules: BUILT_IN_RULES, default: 'model' },
    links: { ...BUILT_IN_LINKS },
    findings: {
      entryPoints: ['App', 'Program', 'Startup', 'Main'],
      transparentBases: BUILT_IN_TRANSPARENT_BASES,
      generatedMembers: BUILT_IN_GENERATED_MEMBERS,
    },
    files: { include: BUILT_IN_INCLUDE, exclude: BUILT_IN_EXCLUDE },
  };
}

/* ---------------------------------------------------------------------- parsing */

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value);
}

/** The keys the format names, so anything else can be reported rather than silently carried. */
function reportUnknownKeys(
  record: Record<string, unknown>,
  known: string[],
  path: string,
  problems: ProfileProblem[],
): void {
  for (const key of Object.keys(record)) {
    if (!known.includes(key)) {
      problems.push({
        path: path ? `${path}.${key}` : key,
        message: `\`${key}\` is not part of the profile format, so it was ignored.`,
        severity: 'warning',
      });
    }
  }
}

/**
 * Reads a list of strings, dropping anything that is not one.
 *
 * A repeated entry is applied once. A declared pattern that matches nothing in the repository is
 * kept: it describes a convention the code has not grown into yet.
 */
function readStringArray(value: unknown, path: string, problems: ProfileProblem[]): string[] | undefined {
  if (value === undefined) {
    return undefined;
  }
  if (!Array.isArray(value)) {
    problems.push({
      path,
      message: `Expected a list of strings, found ${describe(value)}. This part was skipped.`,
      severity: 'warning',
    });
    return undefined;
  }
  const names: string[] = [];
  value.forEach((entry, index) => {
    if (typeof entry === 'string' && entry.trim().length > 0) {
      const trimmed = entry.trim();
      if (!names.includes(trimmed)) {
        names.push(trimmed);
      }
      return;
    }
    problems.push({
      path: `${path}[${index}]`,
      message: `Expected a string, found ${describe(entry)}. This entry was skipped.`,
      severity: 'warning',
    });
  });
  return names;
}

function describe(value: unknown): string {
  if (value === null) {
    return 'null';
  }
  if (Array.isArray(value)) {
    return 'a list';
  }
  return typeof value === 'object' ? 'an object' : `a ${typeof value}`;
}

function readOneOf<T extends string>(
  value: unknown,
  allowed: T[],
  path: string,
  problems: ProfileProblem[],
): T | undefined {
  if (value === undefined) {
    return undefined;
  }
  if (typeof value === 'string' && (allowed as string[]).includes(value)) {
    return value as T;
  }
  problems.push({
    path,
    message: `Expected one of ${allowed.join(', ')}, found ${describe(value)}. This part was skipped.`,
    severity: 'warning',
  });
  return undefined;
}

function readString(value: unknown, path: string, problems: ProfileProblem[]): string | undefined {
  if (value === undefined) {
    return undefined;
  }
  if (typeof value === 'string' && value.trim().length > 0) {
    return value.trim();
  }
  problems.push({
    path,
    message: `Expected text, found ${describe(value)}. This part was skipped.`,
    severity: 'warning',
  });
  return undefined;
}

/** Folds the declared keys of one rule into a rule, or refuses the rule when `layer` is unusable. */
function readLayerRule(value: unknown, path: string, problems: ProfileProblem[]): LayerRule | undefined {
  if (!isRecord(value)) {
    problems.push({
      path,
      message: `Expected a layer rule, found ${describe(value)}. This rule was skipped.`,
      severity: 'warning',
    });
    return undefined;
  }
  reportUnknownKeys(
    value,
    contractKeys('layers.rules[]'),
    path,
    problems,
  );
  const layer = readOneOf(value.layer, PROFILE_LAYERS, `${path}.layer`, problems);
  if (!layer) {
    if (value.layer === undefined) {
      problems.push({
        path: `${path}.layer`,
        message: 'Every layer rule states the layer it assigns. This rule was skipped.',
        severity: 'warning',
      });
    }
    return undefined;
  }

  const rule: LayerRule = { layer };
  const kinds = readStringArray(value.kinds, `${path}.kinds`, problems);
  if (kinds) {
    rule.kinds = kinds.filter((kind): kind is DeclKind => (DECL_KINDS as string[]).includes(kind));
    for (const kind of kinds) {
      if (!(DECL_KINDS as string[]).includes(kind)) {
        problems.push({
          path: `${path}.kinds`,
          message: `\`${kind}\` is not a declaration kind. It was ignored.`,
          severity: 'warning',
        });
      }
    }
  }
  const suffixes = readStringArray(value.nameSuffixes, `${path}.nameSuffixes`, problems);
  if (suffixes) {
    rule.nameSuffixes = suffixes;
  }
  const prefixes = readStringArray(value.namePrefixes, `${path}.namePrefixes`, problems);
  if (prefixes) {
    rule.namePrefixes = prefixes;
  }
  const segments = readStringArray(value.namespaceSegments, `${path}.namespaceSegments`, problems);
  if (segments) {
    rule.namespaceSegments = segments;
  }
  const unless = readStringArray(value.unlessNameSuffixes, `${path}.unlessNameSuffixes`, problems);
  if (unless) {
    rule.unlessNameSuffixes = unless;
  }
  return rule;
}

function readLayersSection(value: unknown, problems: ProfileProblem[]): LayersSection | undefined {
  if (value === undefined) {
    return undefined;
  }
  if (!isRecord(value)) {
    problems.push({
      path: 'layers',
      message: `Expected a section describing layer classification, found ${describe(value)}. `
        + 'The built-in layer rules applied instead.',
      severity: 'warning',
    });
    return undefined;
  }
  reportUnknownKeys(value, contractKeys('layers'), 'layers', problems);

  const section: LayersSection = {};
  if (value.rules !== undefined) {
    if (!Array.isArray(value.rules)) {
      problems.push({
        path: 'layers.rules',
        message: `Expected a list of layer rules, found ${describe(value.rules)}. `
          + 'The built-in layer rules applied instead.',
        severity: 'warning',
      });
    } else {
      const rules: LayerRule[] = [];
      value.rules.forEach((entry, index) => {
        const rule = readLayerRule(entry, `layers.rules[${index}]`, problems);
        if (rule) {
          rules.push(rule);
        }
      });
      section.rules = rules;
    }
  }
  const fallback = readOneOf(value.default, PROFILE_LAYERS, 'layers.default', problems);
  if (fallback) {
    section.default = fallback;
  }
  return section;
}

function readLinksSection(value: unknown, problems: ProfileProblem[]): LinksSection | undefined {
  if (value === undefined) {
    return undefined;
  }
  if (!isRecord(value)) {
    problems.push({
      path: 'links',
      message: `Expected a section describing link detection, found ${describe(value)}. `
        + 'The built-in link rules applied instead.',
      severity: 'warning',
    });
    return undefined;
  }
  reportUnknownKeys(value, contractKeys('links'), 'links', problems);

  const section: LinksSection = {};
  const generic = readStringArray(value.genericPairCalls, 'links.genericPairCalls', problems);
  if (generic) {
    section.genericPairCalls = generic;
  }
  const typeofCalls = readStringArray(value.typeofPairCalls, 'links.typeofPairCalls', problems);
  if (typeofCalls) {
    section.typeofPairCalls = typeofCalls;
  }
  const dependencyCall = readString(value.dependencyPropertyCall, 'links.dependencyPropertyCall', problems);
  if (dependencyCall) {
    section.dependencyPropertyCall = dependencyCall;
  }
  const addPattern = readString(value.addCallPattern, 'links.addCallPattern', problems);
  if (addPattern) {
    if (isRegExp(addPattern)) {
      section.addCallPattern = addPattern;
    } else {
      problems.push({
        path: 'links.addCallPattern',
        message: `\`${addPattern}\` is not a usable regular expression. The built-in pattern applied instead.`,
        severity: 'warning',
      });
    }
  }
  const locator = readStringArray(value.serviceLocator, 'links.serviceLocator', problems);
  if (locator) {
    section.serviceLocator = locator;
  }
  const casts = readStringArray(value.dataContextCasts, 'links.dataContextCasts', problems);
  if (casts) {
    section.dataContextCasts = casts;
  }
  return section;
}

function isRegExp(pattern: string): boolean {
  try {
    // Constructing it is the test: a pattern that throws here would throw on every analysed call.
    new RegExp(pattern);
    return true;
  } catch {
    return false;
  }
}

/** Two rules that declare the same thing, so a repeated declaration can be applied once. */
function sameGeneratedMember(left: GeneratedMembersRule, right: GeneratedMembersRule): boolean {
  return left.attribute.toLowerCase() === right.attribute.toLowerCase()
    && left.on === right.on
    && left.form === right.form;
}

function readGeneratedMembers(
  value: unknown,
  problems: ProfileProblem[],
): GeneratedMembersRule[] | undefined {
  if (value === undefined) {
    return undefined;
  }
  if (!Array.isArray(value)) {
    problems.push({
      path: 'findings.generatedMembers',
      message: `Expected a list of generated-member rules, found ${describe(value)}. `
        + 'The built-in rules applied instead.',
      severity: 'warning',
    });
    return undefined;
  }

  const rules: GeneratedMembersRule[] = [];
  value.forEach((entry, index) => {
    const path = `findings.generatedMembers[${index}]`;
    if (!isRecord(entry)) {
      problems.push({
        path,
        message: `Expected a rule naming an attribute, found ${describe(entry)}. This rule was skipped.`,
        severity: 'warning',
      });
      return;
    }
    reportUnknownKeys(entry, contractKeys('findings.generatedMembers[]'), path, problems);
    const attribute = readString(entry.attribute, `${path}.attribute`, problems);
    const on = readOneOf(entry.on, [...GENERATED_MEMBER_ON_VALUES], `${path}.on`, problems);
    const form = readOneOf(
      entry.form,
      [...GENERATED_MEMBER_FORM_VALUES],
      `${path}.form`,
      problems,
    );
    if (!attribute || !on || !form) {
      if (attribute && (entry.on === undefined || entry.form === undefined)) {
        problems.push({
          path,
          message: 'A generated-member rule states the attribute, whether it sits on a method or a '
            + 'field, and how the member is named. This rule was skipped.',
          severity: 'warning',
        });
      }
      return;
    }
    const rule: GeneratedMembersRule = { attribute, on, form };
    if (!rules.some((known) => sameGeneratedMember(known, rule))) {
      rules.push(rule);
    }
  });
  return rules;
}

function readFindingsSection(value: unknown, problems: ProfileProblem[]): FindingsSection | undefined {
  if (value === undefined) {
    return undefined;
  }
  if (!isRecord(value)) {
    problems.push({
      path: 'findings',
      message: `Expected a section describing findings rules, found ${describe(value)}. `
        + 'The built-in findings rules applied instead.',
      severity: 'warning',
    });
    return undefined;
  }
  reportUnknownKeys(value, contractKeys('findings'), 'findings', problems);

  const section: FindingsSection = {};
  const entryPoints = readStringArray(value.entryPoints, 'findings.entryPoints', problems);
  if (entryPoints) {
    section.entryPoints = entryPoints;
  }
  const transparent = readStringArray(value.transparentBases, 'findings.transparentBases', problems);
  if (transparent) {
    section.transparentBases = transparent;
  }
  const generated = readGeneratedMembers(value.generatedMembers, problems);
  if (generated) {
    section.generatedMembers = generated;
  }
  return section;
}

function readFilesSection(value: unknown, problems: ProfileProblem[]): FilesSection | undefined {
  if (value === undefined) {
    return undefined;
  }
  if (!isRecord(value)) {
    problems.push({
      path: 'files',
      message: `Expected a section describing which files are read, found ${describe(value)}. `
        + 'The built-in file selection applied instead.',
      severity: 'warning',
    });
    return undefined;
  }
  reportUnknownKeys(value, contractKeys('files'), 'files', problems);

  const section: FilesSection = {};
  const include = readStringArray(value.include, 'files.include', problems);
  if (include) {
    section.include = include;
  }
  const exclude = readStringArray(value.exclude, 'files.exclude', problems);
  if (exclude) {
    section.exclude = exclude;
  }
  return section;
}

/**
 * Reads the text of a profile file.
 *
 * A blank file is absent, not broken: it reads as "no profile" with a message that says so. Anything
 * that cannot be used at all comes back as an `error` problem naming the part, and anything that is
 * merely unrecognised comes back as a `warning` while the rest of the file still applies.
 */
export function parseProfile(text: string): ParseProfileResult {
  if (text.trim().length === 0) {
    return {
      problems: [{
        path: PROFILE_FILE_NAME,
        message: 'The profile file is empty, so the built-in conventions apply.',
        severity: 'warning',
      }],
    };
  }

  let raw: unknown;
  try {
    raw = JSON.parse(text);
  } catch (error) {
    return {
      problems: [{
        path: PROFILE_FILE_NAME,
        message: `The profile could not be read as JSON: ${(error as Error).message}`,
        severity: 'error',
      }],
    };
  }

  if (!isRecord(raw)) {
    return {
      problems: [{
        path: PROFILE_FILE_NAME,
        message: `A profile is a JSON object, not ${describe(raw)}. The built-in conventions apply.`,
        severity: 'error',
      }],
    };
  }

  if (raw.version !== 1) {
    return {
      problems: [{
        path: 'version',
        message: raw.version === undefined
          ? 'A profile states `"version": 1`. This file states none, so the built-in conventions apply.'
          : `A profile states \`"version": 1\`. This file states ${describe(raw.version)}, so the built-in conventions apply.`,
        severity: 'error',
      }],
    };
  }

  const problems: ProfileProblem[] = [];
  reportUnknownKeys(raw, contractKeys('root'), '', problems);

  const profile: RepositoryProfile = { version: 1 };
  const repository = readString(raw.repository, 'repository', problems);
  if (repository) {
    profile.repository = repository;
  }
  const layers = readLayersSection(raw.layers, problems);
  if (layers) {
    profile.layers = layers;
  }
  const links = readLinksSection(raw.links, problems);
  if (links) {
    profile.links = links;
  }
  const findings = readFindingsSection(raw.findings, problems);
  if (findings) {
    profile.findings = findings;
  }
  const files = readFilesSection(raw.files, problems);
  if (files) {
    profile.files = files;
  }

  return { profile, problems };
}

/* ------------------------------------------------------------------------ merging */

/** A list union that keeps the order the existing declaration was written in. */
function unionNames(existing: string[] | undefined, incoming: string[] | undefined): string[] | undefined {
  const merged = [...(existing ?? [])];
  for (const name of incoming ?? []) {
    if (!merged.includes(name)) {
      merged.push(name);
    }
  }
  return merged.length > 0 ? merged : undefined;
}

function unionRules(existing: LayerRule[] | undefined, incoming: LayerRule[] | undefined): LayerRule[] | undefined {
  const merged = [...(existing ?? [])];
  for (const rule of incoming ?? []) {
    const key = JSON.stringify(rule);
    if (!merged.some((known) => JSON.stringify(known) === key)) {
      merged.push(rule);
    }
  }
  return merged.length > 0 ? merged : undefined;
}

/**
 * Folds a newly imported reply into the file the repository already holds.
 *
 * Every declaration the existing profile makes survives, and a declaration the reply adds appears
 * once. Where both state one value — the default layer, the dependency-property call — the existing
 * value wins, so importing twice never silently contradicts what a person wrote.
 */
export function mergeProfile(existing: RepositoryProfile, incoming: RepositoryProfile): RepositoryProfile {
  const merged: RepositoryProfile = { version: 1 };

  const repository = existing.repository ?? incoming.repository;
  if (repository) {
    merged.repository = repository;
  }

  const rules = unionRules(existing.layers?.rules, incoming.layers?.rules);
  const fallback = existing.layers?.default ?? incoming.layers?.default;
  if (rules || fallback) {
    merged.layers = { ...(rules ? { rules } : {}), ...(fallback ? { default: fallback } : {}) };
  }

  const links: LinksSection = {};
  const generic = unionNames(existing.links?.genericPairCalls, incoming.links?.genericPairCalls);
  if (generic) {
    links.genericPairCalls = generic;
  }
  const typeofCalls = unionNames(existing.links?.typeofPairCalls, incoming.links?.typeofPairCalls);
  if (typeofCalls) {
    links.typeofPairCalls = typeofCalls;
  }
  const dependencyCall = existing.links?.dependencyPropertyCall ?? incoming.links?.dependencyPropertyCall;
  if (dependencyCall) {
    links.dependencyPropertyCall = dependencyCall;
  }
  const addPattern = existing.links?.addCallPattern ?? incoming.links?.addCallPattern;
  if (addPattern) {
    links.addCallPattern = addPattern;
  }
  const locator = unionNames(existing.links?.serviceLocator, incoming.links?.serviceLocator);
  if (locator) {
    links.serviceLocator = locator;
  }
  const casts = unionNames(existing.links?.dataContextCasts, incoming.links?.dataContextCasts);
  if (casts) {
    links.dataContextCasts = casts;
  }
  if (Object.keys(links).length > 0) {
    merged.links = links;
  }

  const findings: FindingsSection = {};
  const entryPoints = unionNames(existing.findings?.entryPoints, incoming.findings?.entryPoints);
  if (entryPoints) {
    findings.entryPoints = entryPoints;
  }
  const transparent = unionNames(existing.findings?.transparentBases, incoming.findings?.transparentBases);
  if (transparent) {
    findings.transparentBases = transparent;
  }
  const generated = [...(existing.findings?.generatedMembers ?? [])];
  for (const rule of incoming.findings?.generatedMembers ?? []) {
    if (!generated.some((known) => sameGeneratedMember(known, rule))) {
      generated.push(rule);
    }
  }
  if (generated.length > 0) {
    findings.generatedMembers = generated;
  }
  if (Object.keys(findings).length > 0) {
    merged.findings = findings;
  }

  const files: FilesSection = {};
  const include = unionNames(existing.files?.include, incoming.files?.include);
  if (include) {
    files.include = include;
  }
  const exclude = unionNames(existing.files?.exclude, incoming.files?.exclude);
  if (exclude) {
    files.exclude = exclude;
  }
  if (Object.keys(files).length > 0) {
    merged.files = files;
  }

  return merged;
}

/**
 * True when a profile records nothing a reader could use: no layer rule, no link, no findings rule,
 * no file selection. A section that survived parsing but lost every entry reads as nothing.
 */
export function recordsNothing(profile: RepositoryProfile): boolean {
  const layers = profile.layers;
  const links = profile.links;
  const findings = profile.findings;
  const files = profile.files;
  return (!layers || (layers.rules ?? []).length === 0 && !layers.default)
    && (!links || Object.keys(links).length === 0)
    && (!findings || Object.keys(findings).length === 0)
    && (!files || Object.keys(files).length === 0);
}

/* ------------------------------------------------------------- reporting a problem */

/** One problem as the graph shows it: where in the file, and what went wrong. */
export interface ProfileProblemNote {
  path: string;
  message: string;
}

/**
 * How one problem reads in the model's warning list.
 *
 * The host and the page share this one format, so a problem the analysis found can be shown above
 * the findings without the model growing a second field for it.
 */
export function profileWarningText(problem: ProfileProblem): string {
  return problem.path && problem.path !== PROFILE_FILE_NAME
    ? `${PROFILE_FILE_NAME} › ${problem.path}: ${problem.message}`
    : `${PROFILE_FILE_NAME}: ${problem.message}`;
}

/** The problem a model warning carries, or undefined when the warning is about something else. */
export function profileProblemNote(warning: string): ProfileProblemNote | undefined {
  if (!warning.startsWith(PROFILE_FILE_NAME)) {
    return undefined;
  }
  const rest = warning.slice(PROFILE_FILE_NAME.length);
  if (rest.startsWith(' › ')) {
    const body = rest.slice(' › '.length);
    const split = body.indexOf(': ');
    return split < 0
      ? { path: body, message: '' }
      : { path: body.slice(0, split), message: body.slice(split + 2) };
  }
  return { path: '', message: rest.replace(/^: /, '') };
}

/* ---------------------------------------------------------------------- formatting */
/**
 * Writes a profile as plain two-space JSON with the keys in the order a person reads them.
 *
 * A profile is committed and reviewed like code, so the same profile must always produce the same
 * bytes: a key order derived from whatever object happened to be built would show a diff every time
 * the import order changed.
 */
export function formatProfile(profile: RepositoryProfile): string {
  const out: Record<string, unknown> = { version: profile.version };
  if (profile.repository) {
    out.repository = profile.repository;
  }

  const rules = profile.layers?.rules ?? [];
  if (rules.length > 0 || profile.layers?.default) {
    out.layers = {
      ...(rules.length > 0 ? { rules: rules.map(orderedRule) } : {}),
      ...(profile.layers?.default ? { default: profile.layers.default } : {}),
    };
  }

  const links = profile.links;
  if (links && Object.keys(links).length > 0) {
    out.links = {
      ...(links.genericPairCalls ? { genericPairCalls: links.genericPairCalls } : {}),
      ...(links.typeofPairCalls ? { typeofPairCalls: links.typeofPairCalls } : {}),
      ...(links.dependencyPropertyCall ? { dependencyPropertyCall: links.dependencyPropertyCall } : {}),
      ...(links.addCallPattern ? { addCallPattern: links.addCallPattern } : {}),
      ...(links.serviceLocator ? { serviceLocator: links.serviceLocator } : {}),
      ...(links.dataContextCasts ? { dataContextCasts: links.dataContextCasts } : {}),
    };
  }

  const findings = profile.findings;
  if (findings && Object.keys(findings).length > 0) {
    out.findings = {
      ...(findings.entryPoints ? { entryPoints: findings.entryPoints } : {}),
      ...(findings.transparentBases ? { transparentBases: findings.transparentBases } : {}),
      ...(findings.generatedMembers
        ? {
          generatedMembers: findings.generatedMembers.map((rule) => ({
            attribute: rule.attribute,
            on: rule.on,
            form: rule.form,
          })),
        }
        : {}),
    };
  }

  const files = profile.files;
  if (files && Object.keys(files).length > 0) {
    out.files = {
      ...(files.include ? { include: files.include } : {}),
      ...(files.exclude ? { exclude: files.exclude } : {}),
    };
  }

  return `${JSON.stringify(out, null, 2)}\n`;
}

function orderedRule(rule: LayerRule): Record<string, unknown> {
  return {
    layer: rule.layer,
    ...(rule.kinds ? { kinds: rule.kinds } : {}),
    ...(rule.nameSuffixes ? { nameSuffixes: rule.nameSuffixes } : {}),
    ...(rule.namePrefixes ? { namePrefixes: rule.namePrefixes } : {}),
    ...(rule.namespaceSegments ? { namespaceSegments: rule.namespaceSegments } : {}),
    ...(rule.unlessNameSuffixes ? { unlessNameSuffixes: rule.unlessNameSuffixes } : {}),
  };
}
