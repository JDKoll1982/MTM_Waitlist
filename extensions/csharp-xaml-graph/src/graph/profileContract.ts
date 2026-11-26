/**
 * The profile format, described once.
 *
 * Two things read this one table. `contractKeys` is what the parser accepts, so a key no row
 * describes cannot be accepted by accident; `renderContract` is what both prompts print before they
 * ask anything, so a key the parser accepts is always a key the assistant was told about. The two
 * cannot drift apart, which is the whole of FR-024.
 *
 * It is pure: nothing here imports `vscode` or `node:fs`, so the format is exercised by the test
 * suite and printed by the headless validator without an extension host.
 */

/** The part of the file that holds a row. One row belongs to exactly one section. */
export type SectionName =
  | 'root'
  | 'layers'
  | 'layers.rules[]'
  | 'links'
  | 'findings'
  | 'findings.generatedMembers[]'
  | 'files';

/** The four layers a profile can place a type in, and the closed set `layers.default` accepts. */
export const PROFILE_LAYER_VALUES = ['view', 'viewmodel', 'service', 'model'] as const;

/** The declaration kinds a layer rule can restrict itself to. */
export const DECL_KIND_VALUES = ['class', 'interface', 'enum', 'record', 'struct'] as const;

/** Which declaration a generated-member attribute sits on. */
export const GENERATED_MEMBER_ON_VALUES = ['method', 'field'] as const;

/** How a generated member is named from the declaration that carries the attribute. */
export const GENERATED_MEMBER_FORM_VALUES = ['commandSuffix', 'pascalCaseWithoutPrefix'] as const;

/**
 * One key the profile accepts.
 *
 * `path` is the dotted place the key sits, in the same form a problem names it, so
 * `layers.rules[].layer` is the row for the `layer` key inside a rule. `fields` carries the keys of a
 * nested object or of the items of a list, which is how one table reaches every depth.
 */
export interface ProfileField {
  /** Where the key sits, dotted, with `[]` where a list of objects intervenes. */
  path: string;
  /** The key's exact spelling. It is the string a reply must contain. */
  key: string;
  /** The part of the file that holds it, so a prompt renders one group at a time. */
  section: SectionName;
  /** The value in words. */
  shape: string;
  /** True for `version` alone: every other key may be left out. */
  required: boolean;
  /** One sentence: what the key declares. */
  meaning: string;
  /** One sentence: what the extension does when the key is absent. */
  absent: string;
  /** The closed set of values, when the shape is a choice. */
  values?: readonly string[];
  /** The keys of a nested object, or of the items of a list. */
  fields?: ProfileField[];
}

const ABSENT_VERSION = 'A profile without it cannot be used, and the built-in conventions apply.';

/** Every key, in the order a person reads the file. */
export const PROFILE_CONTRACT: ProfileField[] = [
  {
    path: 'version',
    key: 'version',
    section: 'root',
    shape: 'the number 1',
    required: true,
    meaning: 'States which version of the profile format this file uses.',
    absent: ABSENT_VERSION,
  },
  {
    path: 'repository',
    key: 'repository',
    section: 'root',
    shape: 'text',
    required: false,
    meaning: 'Names the repository this profile describes. It is read by people and never used to match a folder.',
    absent: 'The profile describes whichever folder holds it.',
  },
  {
    path: 'layers',
    key: 'layers',
    section: 'root',
    shape: 'a section holding the rules that place a type in a layer',
    required: false,
    meaning: 'Places types in the four layers the graph colours by.',
    absent: 'The built-in layer rules and the built-in default layer apply.',
    fields: [
      {
        path: 'layers.rules',
        key: 'rules',
        section: 'layers',
        shape: 'a list of layer rules',
        required: false,
        meaning: 'Each rule places a type in a layer when every condition it states holds. The first rule that matches decides, so the order is the precedence.',
        absent: 'The built-in layer rules apply.',
        fields: [
          {
            path: 'layers.rules[].layer',
            key: 'layer',
            section: 'layers.rules[]',
            shape: 'one of view, viewmodel, service, model',
            required: false,
            values: PROFILE_LAYER_VALUES,
            meaning: 'The layer this rule assigns when it matches.',
            absent: 'A rule with no layer states nothing, so it is skipped.',
          },
          {
            path: 'layers.rules[].kinds',
            key: 'kinds',
            section: 'layers.rules[]',
            shape: 'a list of text, each one of class, interface, enum, record, struct',
            required: false,
            values: DECL_KIND_VALUES,
            meaning: 'Restricts the rule to these declaration kinds.',
            absent: 'The rule applies to a declaration of any kind.',
          },
          {
            path: 'layers.rules[].nameSuffixes',
            key: 'nameSuffixes',
            section: 'layers.rules[]',
            shape: 'a list of text',
            required: false,
            meaning: 'Matches when the type name ends with one of these.',
            absent: 'The rule does not test the end of the name.',
          },
          {
            path: 'layers.rules[].namePrefixes',
            key: 'namePrefixes',
            section: 'layers.rules[]',
            shape: 'a list of text',
            required: false,
            meaning: 'Matches when the type name starts with one of these.',
            absent: 'The rule does not test the start of the name.',
          },
          {
            path: 'layers.rules[].namespaceSegments',
            key: 'namespaceSegments',
            section: 'layers.rules[]',
            shape: 'a list of text',
            required: false,
            meaning: 'Matches when the namespace has a segment exactly equal to one of these. A substring test would make Models match ViewModels, so only whole segments count.',
            absent: 'The rule does not test the namespace.',
          },
          {
            path: 'layers.rules[].unlessNameSuffixes',
            key: 'unlessNameSuffixes',
            section: 'layers.rules[]',
            shape: 'a list of text',
            required: false,
            meaning: 'Stops the rule applying when the type name ends with one of these, which is how a row model beside the view models stays a model.',
            absent: 'Nothing stops the rule once its other conditions hold.',
          },
        ],
      },
      {
        path: 'layers.default',
        key: 'default',
        section: 'layers',
        shape: 'one of view, viewmodel, service, model',
        required: false,
        values: PROFILE_LAYER_VALUES,
        meaning: 'The layer for a type no rule matches.',
        absent: 'A type no rule matches is placed in the model layer.',
      },
    ],
  },
  {
    path: 'links',
    key: 'links',
    section: 'root',
    shape: 'a section holding the calls that draw a dependency',
    required: false,
    meaning: 'Recognises how this repository registers, resolves and binds types.',
    absent: 'The built-in link detection applies.',
    fields: [
      {
        path: 'links.genericPairCalls',
        key: 'genericPairCalls',
        section: 'links',
        shape: 'a list of text',
        required: false,
        meaning: 'Method names called as x.Call<TA, TB>(): draws a resolves edge from the second type to the first and registers both.',
        absent: 'The built-in name Configure applies.',
      },
      {
        path: 'links.typeofPairCalls',
        key: 'typeofPairCalls',
        section: 'links',
        shape: 'a list of text',
        required: false,
        meaning: 'Method names called as x.Call(typeof(TA), typeof(TB)): draws a resolves edge and registers the first type.',
        absent: 'The built-in name RegisterRoute applies.',
      },
      {
        path: 'links.dependencyPropertyCall',
        key: 'dependencyPropertyCall',
        section: 'links',
        shape: 'text',
        required: false,
        meaning: 'A method name called as x.Call(nameof(P), typeof(TValue), typeof(TOwner)): draws a getservice edge when the value type is a view model.',
        absent: 'The built-in name Register applies.',
      },
      {
        path: 'links.addCallPattern',
        key: 'addCallPattern',
        section: 'links',
        shape: 'a regular expression',
        required: false,
        meaning: 'A registration method name matches this pattern. One type argument registers that type; two pair an implementation with the interface it serves.',
        absent: 'The built-in pattern ^Add[A-Z] applies.',
      },
      {
        path: 'links.serviceLocator',
        key: 'serviceLocator',
        section: 'links',
        shape: 'a list of text',
        required: false,
        meaning: 'Method names that read a service from the container rather than being a plain call.',
        absent: 'The built-in names GetService and GetRequiredService apply.',
      },
      {
        path: 'links.dataContextCasts',
        key: 'dataContextCasts',
        section: 'links',
        shape: 'a list of text',
        required: false,
        meaning: 'Cast keywords that, applied to the data context with an interface type, draw a binding edge.',
        absent: 'The built-in keywords is and as apply.',
      },
    ],
  },
  {
    path: 'findings',
    key: 'findings',
    section: 'root',
    shape: 'a section holding the rules that change what the cleanup report counts',
    required: false,
    meaning: 'Adjusts the cleanup findings to what this repository actually does.',
    absent: 'The built-in findings rules apply.',
    fields: [
      {
        path: 'findings.entryPoints',
        key: 'entryPoints',
        section: 'findings',
        shape: 'a list of text',
        required: false,
        meaning: 'Type names that are never reported unused.',
        absent: 'The built-in entry-point names apply.',
      },
      {
        path: 'findings.transparentBases',
        key: 'transparentBases',
        section: 'findings',
        shape: 'a list of text',
        required: false,
        meaning: 'Base type names that add no bindable members, so a binding to a member they do not declare is genuinely missing.',
        absent: 'The built-in transparent base names apply.',
      },
      {
        path: 'findings.generatedMembers',
        key: 'generatedMembers',
        section: 'findings',
        shape: 'a list of generated-member rules',
        required: false,
        meaning: 'Members the compiler generates from an attribute, so a binding to one is not a dead binding.',
        absent: 'The built-in generated-member rules apply.',
        fields: [
          {
            path: 'findings.generatedMembers[].attribute',
            key: 'attribute',
            section: 'findings.generatedMembers[]',
            shape: 'text',
            required: false,
            meaning: 'The attribute name without brackets, for example RelayCommand.',
            absent: 'A rule with no attribute states nothing, so it is skipped.',
          },
          {
            path: 'findings.generatedMembers[].on',
            key: 'on',
            section: 'findings.generatedMembers[]',
            shape: 'one of method, field',
            required: false,
            values: GENERATED_MEMBER_ON_VALUES,
            meaning: 'Which declaration carries the attribute.',
            absent: 'A rule without it states nothing, so it is skipped.',
          },
          {
            path: 'findings.generatedMembers[].form',
            key: 'form',
            section: 'findings.generatedMembers[]',
            shape: 'one of commandSuffix, pascalCaseWithoutPrefix',
            required: false,
            values: GENERATED_MEMBER_FORM_VALUES,
            meaning: 'How the generated member is named. commandSuffix yields NameCommand, and StemCommand for a name ending in Async. pascalCaseWithoutPrefix strips leading underscores and upper-cases the first letter.',
            absent: 'A rule without it states nothing, so it is skipped.',
          },
        ],
      },
    ],
  },
  {
    path: 'files',
    key: 'files',
    section: 'root',
    shape: 'a section deciding which files are read',
    required: false,
    meaning: 'Decides which files the analysis reads.',
    absent: 'The built-in file selection applies.',
    fields: [
      {
        path: 'files.include',
        key: 'include',
        section: 'files',
        shape: 'a list of text',
        required: false,
        meaning: 'Glob patterns, relative to the workspace folder, describing the files to read.',
        absent: 'The built-in include patterns apply.',
      },
      {
        path: 'files.exclude',
        key: 'exclude',
        section: 'files',
        shape: 'a list of text',
        required: false,
        meaning: 'Glob patterns excluded from the read.',
        absent: 'The built-in exclude patterns apply.',
      },
    ],
  },
];

/** The rows of `fields`, and the rows below them, so every depth is reachable from one list. */
function rowsOf(fields: ProfileField[]): ProfileField[] {
  return fields.flatMap((field) => [field, ...rowsOf(field.fields ?? [])]);
}

/** The keys accepted in one section, taken from the rows that declare that section. */
export function contractKeys(section: SectionName): string[] {
  return rowsOf(PROFILE_CONTRACT)
    .filter((field) => field.section === section)
    .map((field) => field.key);
}

/** The heading that introduces each section, in the order the prompt renders them. */
const SECTION_TITLE: Record<SectionName, string> = {
  root: 'At the top of the file',
  layers: 'In layers, which places types in a layer',
  'layers.rules[]': 'Inside each entry of layers.rules',
  links: 'In links, which recognises how a type is registered, resolved or bound',
  findings: 'In findings, which changes what the cleanup report counts',
  'findings.generatedMembers[]': 'Inside each entry of findings.generatedMembers',
  files: 'In files, which decides which files are read',
};

const SECTION_ORDER: SectionName[] = [
  'root', 'layers', 'layers.rules[]', 'links', 'findings', 'findings.generatedMembers[]', 'files',
];

/**
 * Every row as prose, grouped by the part of the file that holds it.
 *
 * A prompt prints this before its first question, so an assistant is never asked to infer a key's
 * name, letter case or nesting from habit.
 */
export function renderContract(): string {
  const lines = [
    'The profile format is closed: these are all the keys it accepts.',
    'An absent key keeps the built-in convention described under it.',
    'A key that is not listed here is not part of the format, and is reported rather than used.',
  ];

  for (const section of SECTION_ORDER) {
    const rows = rowsOf(PROFILE_CONTRACT).filter((field) => field.section === section);
    if (rows.length === 0) {
      continue;
    }
    lines.push('', `${SECTION_TITLE[section]}:`);
    for (const row of rows) {
      lines.push(`- ${row.path} (${row.shape}, ${row.required ? 'required' : 'optional'})`);
      lines.push(`  ${row.meaning}`);
      if (row.values) {
        lines.push(`  One of: ${row.values.join(', ')}.`);
      }
      lines.push(`  If absent: ${row.absent}`);
    }
  }

  return lines.join('\n');
}
