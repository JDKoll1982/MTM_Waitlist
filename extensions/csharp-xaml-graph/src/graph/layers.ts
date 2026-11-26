import type { NodeLayer } from './model';
import type { TypeDecl } from './csharp/parser';

/** Name endings that mark a type as a service, store or other collaborator. */
const SERVICE_NAME_SUFFIXES = [
  'Service', 'Store', 'Provider', 'Reader', 'Writer', 'Catalog', 'Resolver',
  'Policy', 'Tracker', 'Hasher', 'Server', 'Broker', 'Gate', 'Factory', 'Client',
  'Database', 'Repository', 'Cache', 'Host', 'Pipeline', 'Notifier', 'Prompt',
];

/**
 * Name prefixes that mark a service in the conventions this extension has to read.
 *
 * `Dao_Thing` and `Service_Thing` state a role as plainly as `ThingService` does, and a repository
 * that uses prefixes would otherwise classify every one of them as a plain model.
 */
const SERVICE_NAME_PREFIXES = ['Service_', 'Dao_', 'Repository_', 'Store_', 'Provider_', 'Client_'];

/** Name prefixes that mark a view model where the suffix convention is not used. */
const VIEWMODEL_NAME_PREFIXES = ['ViewModel_', 'VM_'];

function namespaceSegments(namespace: string): string[] {
  return (namespace || '').split('.').filter(Boolean);
}

/**
 * True when the namespace has a segment exactly equal to `name`.
 *
 * Matching whole segments matters: a substring test for `Models` also matches `ViewModels`, which
 * classified every view model in a `*.ViewModels` namespace as a plain model.
 */
function hasSegment(namespace: string, name: string): boolean {
  return namespaceSegments(namespace).includes(name);
}

export function isViewModelName(name: string): boolean {
  return name.endsWith('ViewModel') || VIEWMODEL_NAME_PREFIXES.some((prefix) => name.startsWith(prefix));
}

function isServiceName(name: string): boolean {
  return SERVICE_NAME_SUFFIXES.some((suffix) => name.endsWith(suffix))
    || SERVICE_NAME_PREFIXES.some((prefix) => name.startsWith(prefix));
}

/**
 * Classifies a declared C# type into the layer its node is coloured and filtered by.
 *
 * Several conventions are recognised because no two repositories agree on one: the `*ViewModel`
 * suffix and the `ViewModel_*` prefix, a `ViewModels` namespace segment, and service roles by suffix
 * or by prefix. Interfaces are judged first, so an `IResettableViewModel` interface stays a model
 * rather than being tinted like a page's view model.
 */
/**
 * Name endings that mark a type as data rather than behaviour.
 *
 * A `ViewModels` namespace is a weak signal on its own: this codebase — and most others — keeps its
 * row models, records and navigation payloads in the same folder as the view models that own them.
 * Trusting the namespace alone classified 21 non-view-models as view models, which then read as
 * "view models no view reaches" and competed for the same colour. A type that ends with one of these
 * is data even when it sits beside the view models.
 */
const DATA_NAME_SUFFIXES = [
  'Data', 'Card', 'Row', 'Item', 'Option', 'Credentials', 'EventArgs', 'Args',
  'Outcome', 'Display', 'Result', 'Entry', 'Dto', 'Payload', 'Model',
];

function looksLikeData(name: string): boolean {
  return DATA_NAME_SUFFIXES.some((suffix) => name.endsWith(suffix));
}

export function classifyType(decl: TypeDecl): NodeLayer {
  const name = decl.name;
  const namespace = decl.namespace || '';

  if (decl.kind === 'interface') {
    if (isServiceName(name) || hasSegment(namespace, 'Services') || hasSegment(namespace, 'Contracts')) {
      return 'service';
    }
    return 'model';
  }

  // A view-model naming convention is the strongest signal a type carries, so it wins outright —
  // `ViewModel_DatabaseDeployment` is a view model wherever it lives.
  if (isViewModelName(name)) {
    return 'viewmodel';
  }

  // The namespace is a weaker signal, so it only counts for a class whose name does not say "I am
  // data". A record in a `ViewModels` folder is a row type, and an enum there is an enum.
  if (decl.kind === 'class' && hasSegment(namespace, 'ViewModels') && !looksLikeData(name)) {
    return 'viewmodel';
  }

  if (decl.kind === 'enum') {
    return 'model';
  }

  if (hasSegment(namespace, 'Services') || isServiceName(name)) {
    return 'service';
  }

  return 'model';
}

/** Layers that a view-model property or constructor parameter edge may target. */
export function isViewModelLike(decl: TypeDecl | undefined, fallbackName: string): boolean {
  if (decl) {
    return isViewModelName(decl.name);
  }
  return isViewModelName(fallbackName);
}
