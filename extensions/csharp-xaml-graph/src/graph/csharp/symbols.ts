import type { TypeDecl } from './parser';

/** Index of every type declared in the workspace, with the name-based lookups resolution needs. */
export class TypeIndex {
  private readonly byFullName = new Map<string, TypeDecl>();

  private readonly bySimpleName = new Map<string, TypeDecl[]>();

  add(decl: TypeDecl): void {
    // A partial type is declared more than once; the first declaration is kept and the rest are merged
    // by the caller, so a duplicate full name here is expected and harmless.
    if (!this.byFullName.has(decl.fullName)) {
      this.byFullName.set(decl.fullName, decl);
    }
    const simple = decl.name;
    const list = this.bySimpleName.get(simple) ?? [];
    list.push(decl);
    this.bySimpleName.set(simple, list);
  }

  get(fullName: string): TypeDecl | undefined {
    return this.byFullName.get(fullName);
  }

  get size(): number {
    return this.byFullName.size;
  }

  all(): TypeDecl[] {
    return [...this.byFullName.values()];
  }

  /** Number of distinct declarations carrying a simple name; >1 means the name is ambiguous. */
  ambiguity(simpleName: string): number {
    const distinct = new Set((this.bySimpleName.get(simpleName) ?? []).map((decl) => decl.fullName));
    return distinct.size;
  }

  /**
   * Resolves a type reference as written in source to the declaration it means.
   *
   * Tried in order: the fully-qualified name, the enclosing namespace and each of its ancestors, every
   * `using` in the file, then the single workspace type with that simple name.
   */
  resolve(rawType: string, usings: readonly string[], namespace: string): TypeDecl | undefined {
    const name = (rawType ?? '').trim().replace(/^global::/, '');
    if (!name) {
      return undefined;
    }

    const exact = this.byFullName.get(name);
    if (exact) {
      return exact;
    }

    let scope = namespace;
    while (scope) {
      const candidate = this.byFullName.get(`${scope}.${name}`);
      if (candidate) {
        return candidate;
      }
      const cut = scope.lastIndexOf('.');
      scope = cut > 0 ? scope.slice(0, cut) : '';
    }

    for (const using of usings) {
      const candidate = this.byFullName.get(`${using}.${name}`);
      if (candidate) {
        return candidate;
      }
    }

    const simple = name.includes('.') ? name.slice(name.lastIndexOf('.') + 1) : name;
    const matches = this.bySimpleName.get(simple);
    if (matches && new Set(matches.map((decl) => decl.fullName)).size === 1) {
      return matches[0];
    }

    return undefined;
  }
}

/** Derives the compound-group bucket from a workspace-relative path. */
export function moduleOf(relativePath: string): string {
  const segments = relativePath.split('/').filter(Boolean);
  if (segments.length <= 1) {
    return '(root)';
  }
  return segments[0];
}
