import type { NodeLayer } from './model';
import type { TypeDecl } from './csharp/parser';

const SERVICE_NAME_SUFFIXES = [
  'Service', 'Store', 'Provider', 'Reader', 'Writer', 'Catalog', 'Resolver',
  'Policy', 'Tracker', 'Hasher', 'Server', 'Broker', 'Gate', 'Factory', 'Client',
  'Database', 'Repository', 'Cache', 'Host', 'Pipeline', 'Notifier', 'Prompt',
];

const MODEL_NAMESPACE_HINTS = ['.Models', '.Options', '.Permissions', '.Converters', '.Helpers', '.Selectors'];

/** Classifies a declared C# type into the layer its node is coloured and filtered by. */
export function classifyType(decl: TypeDecl): NodeLayer {
  const name = decl.name;
  const namespace = decl.namespace || '';

  if (name.endsWith('ViewModel')) {
    return 'viewmodel';
  }

  if (decl.kind === 'enum') {
    return 'model';
  }

  if (decl.kind === 'interface') {
    if (namespace.includes('.Contracts') || namespace.includes('.Services')) {
      return 'service';
    }
    return 'model';
  }

  if (namespace.includes('.Services') || namespace.endsWith('.Services')) {
    return 'service';
  }

  if (SERVICE_NAME_SUFFIXES.some((suffix) => name.endsWith(suffix))) {
    return 'service';
  }

  if (MODEL_NAMESPACE_HINTS.some((hint) => namespace.includes(hint))) {
    return 'model';
  }

  return 'model';
}

/** Layers that a view-model property or constructor parameter edge may target. */
export function isViewModelLike(decl: TypeDecl | undefined, fallbackName: string): boolean {
  if (decl) {
    return decl.name.endsWith('ViewModel');
  }
  return fallbackName.endsWith('ViewModel');
}
