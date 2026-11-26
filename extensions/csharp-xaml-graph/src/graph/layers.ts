import type { NodeLayer } from './model';
import type { TypeDecl } from './csharp/parser';
import { evaluateLayerRules, isViewModelType, type CompiledConventions } from './conventions';

/**
 * Classifies a declared C# type into the layer its node is coloured and filtered by.
 *
 * The decision order is data now: the built-in rules reproduce exactly what this module used to
 * hardcode — interfaces judged first, so an `IResettableViewModel` interface stays a model rather
 * than being tinted like a page's view model; a view-model name beating the namespace it sits in; a
 * `ViewModels` namespace counting only for a class whose name does not say "I am data". A
 * repository's profile replaces that order with its own.
 */
export function classifyType(decl: TypeDecl, conventions: CompiledConventions): NodeLayer {
  return evaluateLayerRules(conventions.layerRules, conventions.layerDefault, decl);
}

/**
 * True when the naming rules alone call this a view model.
 *
 * Used where only the written name is known — the declared type of a property, a type argument —
 * because there is no namespace or declaration kind to judge such a name by.
 */
export function isViewModelName(name: string, conventions: CompiledConventions): boolean {
  return isViewModelType(conventions, { name });
}

/** Layers that a view-model property or constructor parameter edge may target. */
export function isViewModelLike(
  decl: TypeDecl | undefined,
  fallbackName: string,
  conventions: CompiledConventions,
): boolean {
  return isViewModelType(conventions, decl ?? { name: fallbackName });
}
