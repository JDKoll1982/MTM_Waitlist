import type { TypeDecl } from './csharp/parser';
import {
  builtInProfile,
  type GeneratedMembersRule,
  type LayerRule,
  type LinksSection,
  type ProfileLayer,
  type RepositoryProfile,
} from './profile';

/**
 * A repository's profile resolved over the built-in conventions.
 *
 * This is the only thing the analysis modules see: they ask it whether a name is a view model, which
 * calls create a registration, which bases are transparent. Resolving once per analysis run keeps a
 * thousand-file workspace from re-resolving the same profile a thousand times.
 */

export interface CompiledConventions {
  /** In precedence order: the first rule that matches decides. */
  layerRules: LayerRule[];
  /** Layer for a type no rule matches. */
  layerDefault: ProfileLayer;
  genericPairCalls: Set<string>;
  typeofPairCalls: Set<string>;
  dependencyPropertyCall: string;
  addCallPattern: RegExp;
  serviceLocator: Set<string>;
  dataContextCasts: Set<string>;
  entryPoints: Set<string>;
  transparentBases: Set<string>;
  generatedMembers: GeneratedMembersRule[];
  include: string[];
  exclude: string[];
}

function namespaceSegments(namespace: string): string[] {
  return (namespace || '').split('.').filter(Boolean);
}

/**
 * True when the namespace has a segment exactly equal to `name`.
 *
 * Matching whole segments matters: a substring test for `Models` also matches `ViewModels`, which
 * classified every view model in a `*.ViewModels` namespace as a plain model.
 */
export function hasNamespaceSegment(namespace: string, name: string): boolean {
  return namespaceSegments(namespace).includes(name);
}

/** What a rule is judged against: a declaration, or just the name a piece of code wrote. */
export interface LayerSubject {
  name: string;
  namespace?: string;
  kind?: TypeDecl['kind'];
}

function ruleMatches(rule: LayerRule, subject: LayerSubject): boolean {
  if (rule.kinds && !rule.kinds.includes(subject.kind ?? 'class')) {
    return false;
  }
  if (rule.nameSuffixes && !rule.nameSuffixes.some((suffix) => subject.name.endsWith(suffix))) {
    return false;
  }
  if (rule.namePrefixes && !rule.namePrefixes.some((prefix) => subject.name.startsWith(prefix))) {
    return false;
  }
  if (rule.namespaceSegments
    && !rule.namespaceSegments.some((segment) => hasNamespaceSegment(subject.namespace ?? '', segment))) {
    return false;
  }
  if (rule.unlessNameSuffixes && rule.unlessNameSuffixes.some((suffix) => subject.name.endsWith(suffix))) {
    return false;
  }
  return true;
}

/**
 * The layer the first matching rule assigns.
 *
 * Order is the precedence a profile declares, and the built-in order keeps the decisions the
 * classifier made before the rules were data.
 */
export function evaluateLayerRules(
  rules: LayerRule[],
  fallback: ProfileLayer,
  subject: LayerSubject,
): ProfileLayer {
  return rules.find((rule) => ruleMatches(rule, subject))?.layer ?? fallback;
}

/** True when the conventions place this declaration, or this written name, in the view-model layer. */
export function isViewModelType(conventions: CompiledConventions, subject: LayerSubject): boolean {
  return evaluateLayerRules(conventions.layerRules, conventions.layerDefault, subject) === 'viewmodel';
}

/** Resolves a profile over the built-in conventions. An absent profile is the built-in profile. */
export function compileConventions(profile?: RepositoryProfile): CompiledConventions {
  const defaults = builtInProfile();
  const layers = profile?.layers ?? defaults.layers ?? {};
  const links = profile?.links ?? {};
  const findings = profile?.findings ?? {};
  const files = profile?.files ?? {};

  const defaultLinks = defaults.links as Required<LinksSection>;
  const defaultFindings = defaults.findings ?? {};
  const defaultFiles = defaults.files ?? {};

  return {
    layerRules: layers.rules ?? defaults.layers?.rules ?? [],
    layerDefault: layers.default ?? defaults.layers?.default ?? 'model',
    genericPairCalls: new Set(links.genericPairCalls ?? defaultLinks.genericPairCalls),
    typeofPairCalls: new Set(links.typeofPairCalls ?? defaultLinks.typeofPairCalls),
    dependencyPropertyCall: links.dependencyPropertyCall ?? defaultLinks.dependencyPropertyCall,
    addCallPattern: new RegExp(links.addCallPattern ?? defaultLinks.addCallPattern),
    serviceLocator: new Set(links.serviceLocator ?? defaultLinks.serviceLocator),
    dataContextCasts: new Set(links.dataContextCasts ?? defaultLinks.dataContextCasts),
    entryPoints: new Set(findings.entryPoints ?? defaultFindings.entryPoints ?? []),
    transparentBases: new Set(findings.transparentBases ?? defaultFindings.transparentBases ?? []),
    generatedMembers: findings.generatedMembers ?? defaultFindings.generatedMembers ?? [],
    include: files.include ?? defaultFiles.include ?? [],
    exclude: files.exclude ?? defaultFiles.exclude ?? [],
  };
}
