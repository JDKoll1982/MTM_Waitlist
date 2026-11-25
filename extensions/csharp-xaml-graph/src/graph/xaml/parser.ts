import { Parser } from 'htmlparser2';

export interface XamlBinding {
  /** Raw binding expression, e.g. `ViewModel.StatusMessage` or `Path=Status`. */
  expression: string;
  /**
   * Which markup extension produced it. `x:Bind` is compile-checked against the page class;
   * `Binding`/`TemplateBinding` are the WPF, MAUI and UWP forms, which resolve against a
   * `DataContext` at run time and therefore cannot be checked the same way.
   */
  dialect: 'x:Bind' | 'Binding' | 'TemplateBinding';
  /** Leading identifier of the expression, or null when the expression does not start with one. */
  root: string | null;
  /** Member segments after the root, e.g. `["StatusMessage"]`. */
  path: string[];
  line: number;
  /** True when the binding sits inside a DataTemplate, i.e. it targets the item, not the page. */
  inTemplate: boolean;
  /**
   * The innermost `x:DataType` in scope when the binding is inside a template, in the form it was
   * written (`models:PermissionCell`). Null outside a template, or inside one with no `x:DataType`.
   */
  templateDataType: string | null;
  /** The template's declared type with its `using:` prefix resolved, when one could be resolved. */
  templateTypeNamespace: string | null;
}

export interface XamlDataType {
  value: string;
  line: number;
}

export interface XamlFile {
  path: string;
  className: string | null;
  rootTag: string;
  /** Every `x:Name` declared in the file. */
  elementNames: string[];
  /**
   * Local names of every element the document instantiates, e.g. `ReadStatusIndicator` from
   * `<mock:ReadStatusIndicator />`. A UserControl is used through markup like this, so a view that
   * appears here in another file is genuinely used even though no C# names it.
   */
  elementTags: string[];
  dataTypes: XamlDataType[];
  bindings: XamlBinding[];
  /** Prefix declared on any element, mapped to the CLR namespace from its `using:` URI. */
  xmlnsUsing: Record<string, string>;
  /** Attribute values that name a code-behind member, e.g. `Click="OnSave"`. */
  handlerNames: string[];
}

const ROOT_IDENTIFIER = /^\s*([A-Za-z_][A-Za-z0-9_]*)\s*(?:\.|\(|,|\s|$)/;
const MEMBER_SEGMENT = /^[A-Za-z_][A-Za-z0-9_]*/;

/** Extracts the leading identifier from a binding expression. */
export function bindingRoot(expression: string): string | null {
  if (!expression || expression.trimStart().startsWith('{')) {
    return null;
  }
  const match = ROOT_IDENTIFIER.exec(expression);
  return match ? match[1] : null;
}

/** Splits the member segments that follow the root: `ViewModel.Status.Text` → ["Status","Text"]. */
export function bindingPath(expression: string, root: string | null): string[] {
  if (!root) {
    return [];
  }
  const rest = expression.trim().slice(root.length);
  const path: string[] = [];
  let index = 0;

  while (index < rest.length) {
    if (rest[index] === '.') {
      index += 1;
      continue;
    }
    if (rest[index] === '(' || rest[index] === ',' || rest[index] === ' ') {
      break;
    }
    const match = MEMBER_SEGMENT.exec(rest.slice(index));
    if (!match) {
      break;
    }
    path.push(match[0]);
    index += match[0].length;
  }

  return path;
}

/** Takes the expression before the first comma that is not inside brackets or parentheses. */
function firstArgument(raw: string): string {
  let depth = 0;
  for (let index = 0; index < raw.length; index += 1) {
    const character = raw[index];
    if (character === '(' || character === '[' || character === '{') {
      depth += 1;
    } else if (character === ')' || character === ']' || character === '}') {
      depth -= 1;
    } else if (character === ',' && depth === 0) {
      return raw.slice(0, index).trim();
    }
  }
  return raw.trim();
}

/**
 * Pulls every binding markup extension out of an attribute value.
 *
 * A binding is not an attribute of its own — it is the value of whichever property it feeds
 * (`Text="{x:Bind ViewModel.Status}"`), so the attribute loop has to look inside values. Both the
 * compile-checked `x:Bind` form and the run-time `Binding` / `TemplateBinding` forms are read, because
 * a WPF or MAUI repository would otherwise report zero bindings and every view would look unbound.
 */
export function extractBindingExtensions(value: string): { expression: string; dialect: XamlBinding['dialect'] }[] {
  const found: { expression: string; dialect: XamlBinding['dialect'] }[] = [];
  let index = 0;

  while (index < value.length) {
    const start = value.indexOf('{', index);
    if (start < 0) {
      break;
    }

    const slice = value.slice(start);
    const head = /^\{\s*(x:Bind|TemplateBinding|Binding)\b/i.exec(slice);
    if (!head) {
      index = start + 1;
      continue;
    }

    let depth = 1;
    let cursor = start + head[0].length;
    while (cursor < value.length && depth > 0) {
      const character = value[cursor];
      if (character === '{') {
        depth += 1;
      } else if (character === '}') {
        depth -= 1;
      }
      cursor += 1;
    }

    const body = value.slice(start + head[0].length, depth === 0 ? cursor - 1 : value.length);
    const keyword = head[1].toLowerCase();
    const dialect: XamlBinding['dialect'] = keyword === 'x:bind'
      ? 'x:Bind'
      : keyword === 'templatebinding' ? 'TemplateBinding' : 'Binding';

    // `{Binding Path=Status, Mode=OneWay}` names its path explicitly; `{Binding Status}` is shorthand.
    const pathMatch = /(?:^|,)\s*Path\s*=\s*([^,}]+)/i.exec(body);
    const expression = dialect === 'x:Bind'
      ? firstArgument(body)
      : (pathMatch ? pathMatch[1].trim() : firstArgument(body));

    found.push({ expression, dialect });
    index = cursor;
  }

  return found;
}

/** Expressions only, in source order, for callers that do not care which dialect produced them. */
export function extractBindings(value: string): string[] {
  return extractBindingExtensions(value).map((entry) => entry.expression);
}

/** Resolves an `x:DataType` value through the file's `using:` prefixes. */
function resolveDataType(value: string, file: XamlFile): string | null {
  const trimmed = value.trim();
  if (!trimmed) {
    return null;
  }
  const separator = trimmed.indexOf(':');
  if (separator <= 0) {
    return trimmed;
  }
  const prefix = trimmed.slice(0, separator);
  const local = trimmed.slice(separator + 1);
  const namespace = file.xmlnsUsing[prefix];
  return namespace ? `${namespace}.${local}` : local;
}

/** Parses one XAML file into the facts the graph needs, with exact 1-based line numbers. */
export function parseXamlFile(relativePath: string, source: string): XamlFile {
  const lineStarts: number[] = [0];
  for (let index = 0; index < source.length; index += 1) {
    if (source.charCodeAt(index) === 10) {
      lineStarts.push(index + 1);
    }
  }

  const lineOf = (offset: number): number => {
    let low = 0;
    let high = lineStarts.length - 1;
    while (low < high) {
      const mid = (low + high + 1) >> 1;
      if (lineStarts[mid] <= offset) {
        low = mid;
      } else {
        high = mid - 1;
      }
    }
    return low + 1;
  };

  const file: XamlFile = {
    path: relativePath,
    className: null,
    rootTag: '',
    elementNames: [],
    elementTags: [],
    dataTypes: [],
    bindings: [],
    xmlnsUsing: {},
    handlerNames: [],
  };

  const stack: { name: string; isTemplate: boolean; dataType: string | null }[] = [];
  let templateDepth = 0;
  let sawRoot = false;

  /** The innermost `x:DataType` declared by an enclosing template, in source form. */
  const currentTemplateDataType = (): string | null => {
    for (let index = stack.length - 1; index >= 0; index -= 1) {
      if (stack[index].isTemplate && stack[index].dataType) {
        return stack[index].dataType;
      }
    }
    return null;
  };

  const parser = new Parser(
    {
      onopentag(name, attributes) {
            const elementLine = lineOf(parser.startIndex);

            // An attribute on a multi-line element sits on its own line, and a finding that points at the
            // element's first line sends the reader to the wrong place. Locate the attribute within the
            // open tag once and reuse it.
            const attributeLines = new Map<string, number>();
            const attributeLine = (attributeName: string): number => {
              const cached = attributeLines.get(attributeName);
              if (cached !== undefined) {
                return cached;
              }
              const close = source.indexOf('>', parser.startIndex);
              const end = close > 0 ? Math.min(source.length, close + 1) : Math.min(source.length, parser.startIndex + 4000);
              const tagSlice = source.slice(parser.startIndex, end);
              const escaped = attributeName.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
              const match = new RegExp(`(?:^|\\s)${escaped}\\s*=`).exec(tagSlice);
              const found = match ? lineOf(parser.startIndex + match.index) : elementLine;
              attributeLines.set(attributeName, found);
              return found;
            };

        if (!sawRoot) {
          file.rootTag = name;
          sawRoot = true;
        }

        const isTemplate = name === 'DataTemplate' || name === 'ItemsPanelTemplate';

        // A property element (`Page.Resources`, `Grid.RowDefinitions`) is not an instantiated type;
        // only a plain element name is. Dot-bearing names would otherwise pollute the tag list.
        if (!name.includes('.')) {
          const local = name.includes(':') ? name.slice(name.indexOf(':') + 1) : name;
          file.elementTags.push(local);
        }
        // The template's own x:DataType scopes its children, so it goes on the stack before the
        // attribute loop reads any binding on this element.
        stack.push({
          name,
          isTemplate,
          dataType: typeof attributes['x:DataType'] === 'string' ? attributes['x:DataType'].trim() : null,
        });
        if (isTemplate) {
          templateDepth += 1;
        }

        for (const [attributeName, value] of Object.entries(attributes)) {
          if (attributeName.startsWith('xmlns:')) {
            const prefix = attributeName.slice('xmlns:'.length);
            const usingMatch = /^using:\s*(.+)$/.exec(value);
            if (usingMatch) {
              file.xmlnsUsing[prefix] = usingMatch[1].trim();
            }
            continue;
          }

          switch (attributeName) {
            case 'x:Class':
              if (!file.className) {
                file.className = value.trim();
              }
              break;
            case 'x:Name':
              if (value.trim()) {
                file.elementNames.push(value.trim());
              }
              break;
            case 'x:DataType':
              file.dataTypes.push({ value: value.trim(), line: attributeLine(attributeName) });
              break;
            default:
              // An event handler is a code-behind member named by the markup.
              if (/^(Click|Tapped|DoubleTapped|SelectionChanged|TextChanged|Checked|Unchecked|Loaded|Unloaded|NavigationCompleted|ItemClick|PointerPressed|Holding|LostFocus|GotFocus|KeyDown|PointerEntered|PointerExited)$/.test(attributeName)) {
                const handler = value.trim();
                if (/^[A-Za-z_][A-Za-z0-9_]*$/.test(handler)) {
                  file.handlerNames.push(handler);
                }
              }
              break;
          }

          // A binding lives inside the value of the property it feeds, not as its own attribute.
          for (const entry of extractBindingExtensions(value)) {
            const compiled = entry.dialect === 'x:Bind';
            // Only `x:Bind` names its root in the markup: it binds against the page class. The other
            // dialects bind against a run-time DataContext this extension does not model, so their
            // root stays null and they are never judged as dead bindings.
            const root = compiled ? bindingRoot(entry.expression) : null;
            const scope = templateDepth > 0 ? currentTemplateDataType() : null;
            file.bindings.push({
              expression: entry.expression,
              dialect: entry.dialect,
              root,
              path: compiled
                ? bindingPath(entry.expression, root)
                : entry.expression.split('.').map((segment) => segment.trim()).filter(Boolean),
              line: attributeLine(attributeName),
              inTemplate: templateDepth > 0,
              templateDataType: scope,
              templateTypeNamespace: scope ? resolveDataType(scope, file) : null,
            });
          }
        }
      },
      onclosetag() {
        const closed = stack.pop();
        if (closed?.isTemplate) {
          templateDepth = Math.max(0, templateDepth - 1);
        }
      },
    },
    { xmlMode: true, lowerCaseAttributeNames: false, lowerCaseTags: false },
  );

  parser.write(source);
  parser.end();

  return file;
}
