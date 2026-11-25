import { createRequire } from 'node:module';

/* eslint-disable @typescript-eslint/no-explicit-any */

const requireFromHere = createRequire(__filename);
// web-tree-sitter is kept out of the bundle because it locates its own runtime .wasm on disk.
const TreeSitter = requireFromHere('web-tree-sitter') as any;

export type Visibility = 'public' | 'protected' | 'internal' | 'private';

export interface ParamDecl {
  type: string;
  name: string;
}

export interface ConstructorDecl {
  params: ParamDecl[];
  line: number;
  /** The file this record was parsed from. A partial type merges records from several files. */
  file?: string;
}

export interface PropertyDecl {
  name: string;
  type: string;
  /** The declared type exactly as written, generics included. */
  rawType: string;
  line: number;
  isObservable: boolean;
  isPublic: boolean;
  visibility: Visibility;
  /** Attribute names, without the trailing `Attribute`, e.g. ["RelayCommand"], ["JsonIgnore"]. */
  attributes: string[];
  /** The file this record was parsed from. */
  file?: string;
}

export interface FieldDecl {
  name: string;
  type: string;
  rawType: string;
  line: number;
  isReadonly: boolean;
  visibility: Visibility;
  attributes: string[];
  /** The file this record was parsed from. */
  file?: string;
}

export interface MethodDecl {
  name: string;
  returnType: string;
  rawReturnType: string;
  rawParamTypes: string[];
  line: number;
  visibility: Visibility;
  attributes: string[];
  isOverride: boolean;
  isStatic: boolean;
  isAsync: boolean;
  isPartial: boolean;
  /** True when the first parameter carries `this`; the method is called on a receiver, not through its type. */
  isExtension: boolean;
  /** The file this record was parsed from. */
  file?: string;
}

export type AssignedValue =
  | { kind: 'service-locator'; typeName: string }
  | { kind: 'variable'; name: string }
  | { kind: 'new'; typeName: string }
  | { kind: 'other'; text: string };

export interface AssignmentSite {
  target: string;
  value: AssignedValue;
  line: number;
  /** The file this record was parsed from. */
  file?: string;
}

export interface CallSite {
  receiver: string;
  method: string;
  typeArgs: string[];
  line: number;
  /** The file this record was parsed from. */
  file?: string;
}

export interface TypeMention {
  /** The type name as written, generics stripped. */
  name: string;
  /** The construct that mentioned it, for the edge label: 'local', 'cast', 'typeof', 'attribute', … */
  via: string;
  line: number;
  /** The file this record was parsed from. */
  file?: string;
}

export interface TypeDecl {
  name: string;
  namespace: string;
  fullName: string;
  kind: 'class' | 'record' | 'struct' | 'interface' | 'enum';
  line: number;
  isPartial: boolean;
  isAbstract: boolean;
  bases: string[];
  constructors: ConstructorDecl[];
  properties: PropertyDecl[];
  fields: FieldDecl[];
  methods: MethodDecl[];
  assignments: AssignmentSite[];
  calls: CallSite[];
  creations: { typeName: string; line: number; file?: string }[];
  /** Types named in a `typeof(X)` expression; a runtime reference the graph cannot draw as an edge. */
  typeofRefs: string[];
  /** Every type named anywhere in this declaration, with the construct that named it. */
  mentions: TypeMention[];
}

export interface CSharpFile {
  path: string;
  usings: string[];
  types: TypeDecl[];
  hasSyntaxError: boolean;
}

let parser: any = null;

/** Loads the C# grammar once per extension host process. */
export async function initCSharpGrammar(wasmPath: string): Promise<void> {
  if (parser) {
    return;
  }

  await TreeSitter.Parser.init({
    locateFile: () => requireFromHere.resolve('web-tree-sitter/web-tree-sitter.wasm'),
  });

  const language = await TreeSitter.Language.load(wasmPath);
  parser = new TreeSitter.Parser();
  parser.setLanguage(language);
}

function lineOf(node: any): number {
  return node?.startPosition ? node.startPosition.row + 1 : 1;
}

function named(node: any): any[] {
  return node?.namedChildren ? node.namedChildren.filter(Boolean) : [];
}

function text(node: any): string {
  return node ? String(node.text) : '';
}

/** Strips generics, nullability, array bounds and the `global::` prefix so a name can be resolved. */
export function normalizeTypeText(raw: string): string {
  let value = (raw ?? '').trim().replace(/^global::/, '');
  const angle = value.indexOf('<');
  if (angle > 0) {
    value = value.slice(0, angle);
  }
  return value.replace(/\?+$/, '').replace(/\s*\[[^\]]*\]/g, '').trim();
}

function genericArgumentTexts(genericName: any): string[] {
  const list = genericName?.childForFieldName?.('type_arguments')
    ?? named(genericName).find((child: any) => child.type === 'type_argument_list');
  if (!list) {
    return [];
  }
  return named(list).map((child: any) => text(child).trim()).filter(Boolean);
}

function isTypeDeclaration(nodeType: string): boolean {
  return nodeType === 'class_declaration'
    || nodeType === 'record_declaration'
    || nodeType === 'struct_declaration'
    || nodeType === 'interface_declaration'
    || nodeType === 'enum_declaration';
}

function kindOf(nodeType: string): TypeDecl['kind'] {
  switch (nodeType) {
    case 'record_declaration': return 'record';
    case 'struct_declaration': return 'struct';
    case 'interface_declaration': return 'interface';
    case 'enum_declaration': return 'enum';
    default: return 'class';
  }
}

function hasModifier(node: any, keyword: string): boolean {
  return named(node).some((child: any) => child.type === 'modifier' && text(child).trim() === keyword);
}

/** Attribute names on a member, with the `Attribute` suffix and any namespace stripped. */
function attributeNames(node: any): string[] {
  const names: string[] = [];
  for (const child of named(node)) {
    if (child.type !== 'attribute_list') {
      continue;
    }
    for (const attribute of named(child)) {
      if (attribute.type !== 'attribute') {
        continue;
      }
      const nameNode = attribute.childForFieldName?.('name') ?? named(attribute)[0];
      const raw = text(nameNode).trim();
      if (!raw) {
        continue;
      }
      const simple = raw.slice(raw.lastIndexOf('.') + 1).replace(/Attribute$/, '');
      if (simple) {
        names.push(simple);
      }
    }
  }
  return names;
}

function visibilityOf(node: any, kind: 'class' | 'interface'): Visibility {
  if (hasModifier(node, 'public')) {
    return 'public';
  }
  if (hasModifier(node, 'protected')) {
    return 'protected';
  }
  if (hasModifier(node, 'internal')) {
    return 'internal';
  }
  if (hasModifier(node, 'private')) {
    return 'private';
  }
  // A class member with no modifier is private; an interface member is implicitly public.
  return kind === 'interface' ? 'public' : 'private';
}

function walk(node: any, visit: (node: any) => void): void {
  if (!node) {
    return;
  }
  visit(node);
  for (const child of named(node)) {
    walk(child, visit);
  }
}

function describeValue(right: any): AssignedValue | undefined {
  if (!right) {
    return undefined;
  }

  if (right.type === 'invocation_expression') {
    const fn = right.childForFieldName?.('function') ?? named(right)[0];
    if (!fn) {
      return undefined;
    }

    if (fn.type === 'member_access_expression') {
      const receiver = text(fn.childForFieldName?.('expression') ?? named(fn)[0]).trim();
      const nameNode = fn.childForFieldName?.('name') ?? named(fn)[named(fn).length - 1];
      if (nameNode?.type === 'generic_name') {
        const method = text(nameNode.childForFieldName?.('name') ?? named(nameNode)[0]).trim();
        const args = genericArgumentTexts(nameNode);
        if (method === 'GetService' || method === 'GetRequiredService') {
          return { kind: 'service-locator', typeName: normalizeTypeText(args[0] ?? '') };
        }
      }
      return { kind: 'other', text: `${receiver}.${text(nameNode)}` };
    }

    if (fn.type === 'generic_name') {
      const method = text(fn.childForFieldName?.('name') ?? named(fn)[0]).trim();
      const args = genericArgumentTexts(fn);
      if (method === 'GetService' || method === 'GetRequiredService') {
        return { kind: 'service-locator', typeName: normalizeTypeText(args[0] ?? '') };
      }
    }

    return { kind: 'other', text: text(right) };
  }

  if (right.type === 'object_creation_expression') {
    const typeNode = right.childForFieldName?.('type') ?? named(right)[0];
    return { kind: 'new', typeName: normalizeTypeText(text(typeNode)) };
  }

  if (right.type === 'identifier') {
    return { kind: 'variable', name: text(right) };
  }

  // `viewModel ?? throw new ArgumentNullException(nameof(viewModel))`
  if (right.type === 'binary_expression' || right.type === 'throw_expression') {
    const first = named(right).find((child: any) => child.type === 'identifier');
    if (first) {
      return { kind: 'variable', name: text(first) };
    }
  }

  return { kind: 'other', text: text(right) };
}

function collectCalls(body: any): CallSite[] {
  const calls: CallSite[] = [];
  walk(body, (current: any) => {
    if (current.type !== 'invocation_expression') {
      return;
    }
    const fn = current.childForFieldName?.('function') ?? named(current)[0];
    if (!fn || fn.type !== 'member_access_expression') {
      return;
    }
    const receiver = text(fn.childForFieldName?.('expression') ?? named(fn)[0]).trim();
    const nameNode = fn.childForFieldName?.('name') ?? named(fn)[named(fn).length - 1];
    if (nameNode?.type === 'generic_name') {
      calls.push({
        receiver,
        method: text(nameNode.childForFieldName?.('name') ?? named(nameNode)[0]).trim(),
        typeArgs: genericArgumentTexts(nameNode),
        line: lineOf(current),
      });
    }
  });
  return calls;
}

function collectAssignments(body: any): AssignmentSite[] {
  const sites: AssignmentSite[] = [];
  walk(body, (current: any) => {
    if (current.type !== 'assignment_expression') {
      return;
    }
    const left = current.childForFieldName?.('left') ?? named(current)[0];
    const right = current.childForFieldName?.('right') ?? named(current)[1];
    if (!left || left.type !== 'identifier') {
      return;
    }
    const value = describeValue(right);
    if (value) {
      sites.push({ target: text(left).trim(), value, line: lineOf(current) });
    }
  });
  return sites;
}

function collectCreations(body: any): { typeName: string; line: number }[] {
  const creations: { typeName: string; line: number }[] = [];
  walk(body, (current: any) => {
    if (current.type !== 'object_creation_expression') {
      return;
    }
    const typeNode = current.childForFieldName?.('type') ?? named(current)[0];
    const typeName = normalizeTypeText(text(typeNode));
    if (typeName) {
      creations.push({ typeName, line: lineOf(current) });
    }
  });
  return creations;
}

function collectTypeofRefs(body: any): string[] {
  const refs = new Set<string>();
  walk(body, (current: any) => {
    if (current.type !== 'typeof_expression') {
      return;
    }
    const typeNode = named(current).find((child: any) => child.type !== 'typeof');
    const name = normalizeTypeText(text(typeNode ?? named(current)[0]));
    if (name) {
      refs.add(name);
    }
  });
  return [...refs];
}

/** Names a type-expression node can carry a type in, per construct. */
const TYPE_FIELD_BY_NODE: Record<string, string> = {
  variable_declaration: 'type',
  cast_expression: 'type',
  object_creation_expression: 'type',
  array_creation_expression: 'type',
  event_declaration: 'type',
  property_declaration: 'type',
  field_declaration: 'type',
  parameter: 'type',
  type_pattern: 'type',
  declaration_expression: 'type',
};

const VIA_BY_NODE: Record<string, string> = {
  variable_declaration: 'local',
  cast_expression: 'cast',
  object_creation_expression: 'new',
  array_creation_expression: 'new array',
  event_declaration: 'event',
  property_declaration: 'property',
  field_declaration: 'field',
  parameter: 'parameter',
  type_pattern: 'pattern',
  declaration_expression: 'out declaration',
};

/**
 * Collects every type the type's body mentions, and why.
 *
 * A class dependency graph that only reads declarations misses most real coupling: a method that
 * casts to another type, compares with `is`, registers an attribute, declares an event or touches
 * another type's statics all create a dependency. Those are exactly the references that make
 * structural cycles appear, so they are gathered here rather than inferred.
 */
function collectTypeMentions(node: any): TypeMention[] {
  const mentions: TypeMention[] = [];
  const push = (raw: string, via: string, line: number): void => {
    const name = normalizeTypeText(raw);
    if (name && /^[A-Za-z_]/.test(name)) {
      mentions.push({ name, via, line });
    }
  };

  walk(node, (current: any) => {
    const field = TYPE_FIELD_BY_NODE[current.type];
    if (field) {
      const typeNode = current.childForFieldName?.(field)
        ?? named(current).find((child: any) => child.type === 'type');
      if (typeNode) {
        push(text(typeNode), VIA_BY_NODE[current.type] ?? current.type, lineOf(current));
      }
    }

    // `x as T`
    if (current.type === 'as_expression') {
      const right = current.childForFieldName?.('right') ?? named(current)[1];
      if (right) {
        push(text(right), 'as', lineOf(current));
      }
    }

    // `x is T` and `x is T t`. This grammar wraps both in an `is_pattern_expression` whose pattern
    // carries the type as its first child — a `declaration_pattern` for the typed form and a
    // `constant_pattern` for a plain test. Reading only `is_expression` missed every one of them.
    if (current.type === 'is_expression' || current.type === 'is_pattern_expression') {
      const pattern = named(current).find((child: any) => child.type.endsWith('_pattern'));
      const typeNode = pattern ? named(pattern)[0] : (current.childForFieldName?.('right') ?? named(current)[1]);
      const isNameNode = typeNode && ['identifier', 'qualified_name', 'generic_name', 'predefined_type',
        'nullable_type', 'array_type'].includes(typeNode.type);
      if (typeNode && isNameNode) {
        push(text(typeNode), 'is', lineOf(current));
      }
    }

    // `typeof(T)` — recorded, and also a mention so it keeps T alive in the graph.
    if (current.type === 'typeof_expression') {
      const typeNode = named(current).find((child: any) => child.type !== 'typeof');
      if (typeNode) {
        push(text(typeNode), 'typeof', lineOf(current));
      }
    }

    // `[SomeAttribute]` — the attribute type is a real dependency.
    if (current.type === 'attribute') {
      const nameNode = current.childForFieldName?.('name') ?? named(current)[0];
      const raw = text(nameNode).trim();
      if (raw) {
        const simple = raw.slice(raw.lastIndexOf('.') + 1).replace(/Attribute$/, '');
        push(simple, 'attribute', lineOf(current));
      }
    }

    // `where T : SomeBase` — generic constraints bind the declaration to another type.
    if (current.type === 'type_parameter_constraint') {
      push(text(current), 'constraint', lineOf(current));
    }

    // `Other.Thing(...)` — a static call or member touch. Only an upper-case receiver can be a type.
    if (current.type === 'member_access_expression') {
      const receiver = current.childForFieldName?.('expression') ?? named(current)[0];
      const raw = text(receiver).trim();
      if (receiver?.type === 'identifier' && /^[A-Z][A-Za-z0-9_]*$/.test(raw)) {
        push(raw, 'static access', lineOf(current));
      }
    }

    // `catch (SomeException)` — the caught type is a dependency too.
    if (current.type === 'catch_declaration') {
      const typeNode = current.childForFieldName?.('type');
      if (typeNode) {
        push(text(typeNode), 'catch', lineOf(current));
      }
    }
  });

  return mentions;
}

function readParameters(parameterList: any): ParamDecl[] {
  if (!parameterList) {
    return [];
  }
  return named(parameterList)
    .filter((child: any) => child.type === 'parameter')
    .map((param: any) => {
      const positional = named(param).filter(
        (child: any) => child.type !== 'attribute_list' && child.type !== 'modifier');
      return {
        type: normalizeTypeText(text(param.childForFieldName?.('type') ?? positional[0])),
        name: text(param.childForFieldName?.('name') ?? positional[1]).trim(),
      };
    })
    .filter((param: ParamDecl) => param.type.length > 0 && param.name.length > 0);
}

/** True when the first parameter of a method carries `this`, making it an extension method. */
function isExtensionMethod(parameterList: any): boolean {
  const first = named(parameterList ?? {}).find((child: any) => child.type === 'parameter');
  if (!first) {
    return false;
  }
  return named(first).some((child: any) => child.type === 'modifier' && text(child).trim() === 'this');
}

/**
 * A positional record's parameter list declares public init-only properties.
 *
 * `record PartPictureChangeDisplay(string Title, string Detail)` gives `Title` and `Detail` as
 * bindable members. Without this, every `x:Bind Title` inside a template over such a record would
 * read as a dead binding.
 */
function readRecordParameters(body: any, child: any, decl: TypeDecl): void {
  if (child.type !== 'record_declaration' && child.type !== 'record_struct_declaration') {
    return;
  }
  const parameterList = child.childForFieldName?.('parameters')
    ?? named(child).find((inner: any) => inner.type === 'parameter_list');
  for (const param of readParameters(parameterList)) {
    if (decl.properties.some((property) => property.name === param.name)) {
      continue;
    }
    decl.properties.push({
      name: param.name,
      type: param.type,
      rawType: param.type,
      line: decl.line,
      isObservable: false,
      isPublic: true,
      visibility: 'public',
      attributes: [],
    });
  }
}

function readMembers(body: any, decl: TypeDecl): void {
  if (!body) {
    return;
  }

  for (const member of named(body)) {
    switch (member.type) {
      case 'constructor_declaration':
        decl.constructors.push({
          params: readParameters(
            member.childForFieldName?.('parameters')
              ?? named(member).find((child: any) => child.type === 'parameter_list')),
          line: lineOf(member),
        });
        break;

      case 'property_declaration': {
        const positional = named(member).filter((child: any) =>
          child.type !== 'modifier'
          && child.type !== 'attribute_list'
          && child.type !== 'accessor_list'
          && child.type !== 'equals_value_clause'
          && child.type !== 'arrow_expression_clause');
        const nameNode = member.childForFieldName?.('name') ?? positional[1];
        const typeNode = member.childForFieldName?.('type') ?? positional[0];
        decl.properties.push({
          name: text(nameNode).trim(),
          type: normalizeTypeText(text(typeNode)),
          rawType: text(typeNode).trim(),
          line: lineOf(member),
          isObservable: attributeNames(member).includes('ObservableProperty'),
          isPublic: hasModifier(member, 'public'),
          visibility: visibilityOf(member, decl.kind === 'interface' ? 'interface' : 'class'),
          attributes: attributeNames(member),
        });
        break;
      }

      case 'field_declaration': {
        const positional = named(member).filter((child: any) =>
          child.type !== 'modifier'
          && child.type !== 'attribute_list'
          && child.type !== 'equals_value_clause');
        // The grammar keeps a field's type inside its `variable_declaration`, not on the
        // `field_declaration` itself, so reading a `type` field here yields the whole
        // declaration text — which then resolves to nothing and silently drops every
        // field-typed reference.
        const variablesNode = positional.find((child: any) => child.type === 'variable_declaration');
        const typeNode = variablesNode?.childForFieldName?.('type')
          ?? (variablesNode ? named(variablesNode)[0] : undefined);
        const declarators = variablesNode
          ? named(variablesNode).filter((child: any) => child.type === 'variable_declarator')
          : [];
        for (const declarator of declarators) {
          decl.fields.push({
            name: text(declarator.childForFieldName?.('name') ?? named(declarator)[0]).trim(),
            type: normalizeTypeText(text(typeNode)),
            rawType: text(typeNode).trim(),
            line: lineOf(member),
            isReadonly: hasModifier(member, 'readonly'),
            visibility: visibilityOf(member, 'class'),
            attributes: attributeNames(member),
          });
        }
        break;
      }

      case 'method_declaration': {
        const nameNode = member.childForFieldName?.('name')
          ?? named(member).find((child: any) => child.type === 'identifier');
        // A method's return type is the `returns` field; `type` is used by properties.
        const typeNode = member.childForFieldName?.('type')
          ?? member.childForFieldName?.('returns');
        const parameterList = member.childForFieldName?.('parameters')
          ?? named(member).find((child: any) => child.type === 'parameter_list');
        decl.methods.push({
          name: text(nameNode).trim(),
          returnType: normalizeTypeText(text(typeNode)),
          rawReturnType: text(typeNode).trim(),
          rawParamTypes: readParameters(parameterList).map((parameter) => parameter.type),
          line: lineOf(member),
          visibility: visibilityOf(member, decl.kind === 'interface' ? 'interface' : 'class'),
          attributes: attributeNames(member),
          isOverride: hasModifier(member, 'override'),
          isStatic: hasModifier(member, 'static'),
          isAsync: hasModifier(member, 'async'),
          isPartial: hasModifier(member, 'partial'),
          isExtension: isExtensionMethod(parameterList),
        });
        break;
      }

      default:
        break;
    }
  }

  decl.assignments.push(...collectAssignments(body));
  decl.calls.push(...collectCalls(body));
  decl.creations.push(...collectCreations(body));
  decl.typeofRefs.push(...collectTypeofRefs(body));
  decl.mentions.push(...collectTypeMentions(body));
}

/**
 * Walks a C# syntax tree collecting type declarations.
 *
 * `prefix` is the enclosing namespace plus any outer type names, so a nested type resolves to
 * `Namespace.Outer.Inner` rather than a re-ordered join of separate pieces. A file-scoped namespace
 * applies to its following siblings, not to its own (empty) body, so the scope is carried in a local.
 */
function visitChildren(children: any[], file: CSharpFile, prefix: string): void {
  let scope = prefix;

  for (const child of children) {
    if (!child) {
      continue;
    }

    if (child.type === 'file_scoped_namespace_declaration') {
      const name = text(child.childForFieldName?.('name') ?? named(child)[0]).trim();
      if (name) {
        scope = scope ? `${scope}.${name}` : name;
      }
      continue;
    }

    if (child.type === 'namespace_declaration') {
      const name = text(child.childForFieldName?.('name') ?? named(child)[0]).trim();
      const combined = scope && name ? `${scope}.${name}` : (name || scope);
      const body = child.childForFieldName?.('body')
        ?? named(child).find((inner: any) => inner.type === 'declaration_list');
      visitChildren(named(body), file, combined);
      continue;
    }

    if (child.type === 'using_directive') {
      // `using Alias = Some.Thing;` names the alias in a `name` field; the namespace is the
      // qualified name beside it. Without this check the alias itself is recorded as a
      // namespace and every `Alias.Thing` reference then resolves through it.
      const alias = child.childForFieldName?.('alias') ?? child.childForFieldName?.('name');
      const target = named(child).find(
        (inner: any) => inner.type === 'qualified_name' || inner.type === 'identifier');
      if (target && !alias) {
        file.usings.push(text(target).trim());
      }
      continue;
    }

    if (isTypeDeclaration(child.type)) {
      const nameNode = child.childForFieldName?.('name')
        ?? named(child).find((inner: any) => inner.type === 'identifier');
      const name = text(nameNode).trim();
      if (!name) {
        continue;
      }

      const baseListNode = child.childForFieldName?.('bases')
        ?? named(child).find((inner: any) => inner.type === 'base_list');
      const bases = baseListNode
        ? named(baseListNode)
          .map((base: any) => normalizeTypeText(text(base).replace(/\(.*$/, '')))
          .filter(Boolean)
        : [];

      const decl: TypeDecl = {
        name,
        namespace: scope,
        fullName: scope ? `${scope}.${name}` : name,
        kind: kindOf(child.type),
        line: lineOf(child),
        isPartial: hasModifier(child, 'partial'),
        isAbstract: hasModifier(child, 'abstract'),
        bases,
        constructors: [],
        properties: [],
        fields: [],
        methods: [],
        assignments: [],
        calls: [],
        creations: [],
        typeofRefs: [],
        mentions: [],
      };

      const body = child.childForFieldName?.('body')
        ?? named(child).find((inner: any) => inner.type === 'declaration_list');

      readMembers(body, decl);
      readRecordParameters(body, child, decl);

      // A type's own attributes and generic constraints sit beside its body, not inside it, so the
      // member walk never sees them. `[Serializable]`, a routing attribute, a JSON converter or a
      // `where T : SomeBase` constraint is a real dependency that would otherwise vanish.
      for (const outside of named(child).filter((inner: any) =>
        inner.type === 'attribute_list' || inner.type === 'type_parameter_constraints_clause')) {
        decl.mentions.push(...collectTypeMentions(outside));
      }

      file.types.push(decl);

      const nested = named(body).filter((inner: any) => isTypeDeclaration(inner.type));
      if (nested.length > 0) {
        visitChildren(nested, file, decl.fullName);
      }
      continue;
    }

    // Anything not handled above (compilation unit, preprocessor branches) is descended into.
    visitChildren(named(child), file, scope);
  }
}

/** Parses one C# file into the declarations and call sites the graph needs. */
export function parseCSharpFile(relativePath: string, source: string): CSharpFile {
  if (!parser) {
    throw new Error('C# grammar is not loaded; call initCSharpGrammar first.');
  }

  const tree = parser.parse(source);
  const file: CSharpFile = {
    path: relativePath,
    usings: [],
    types: [],
    hasSyntaxError: tree.rootNode.hasError === true,
  };
  visitChildren(named(tree.rootNode), file, '');
  tree.delete();
  return file;
}

