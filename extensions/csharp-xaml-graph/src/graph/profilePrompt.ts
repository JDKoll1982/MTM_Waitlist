import type { ProfileEvidence } from './model';
import { PROFILE_FILE_NAME, formatProfile, type RepositoryProfile } from './profile';
import { renderContract } from './profileContract';

/**
 * The two prompts the extension hands to VS Code's chat.
 *
 * Both are built from the repository's own evidence — the types, namespaces and calls the analysis
 * already saw — so a question can be answered by looking at code the developer recognises. Every
 * question comes before any proposed answer: the point of the conversation is that the developer
 * tells the extension what its conventions are, not that a model guesses and is corrected.
 */

/** At most two example names per signal, so a question stays a line rather than a list. */
const EXAMPLES = 2;

function orNone(line: string): string {
  return line.length > 0 ? line : 'none seen in this repository';
}

function quoteNames(items: { name: string; examples: string[] }[]): string {
  return orNone(items
    .map((item) => `${item.name} (${item.examples.slice(0, EXAMPLES).join(', ') || 'no example'})`)
    .join(', '));
}

function quoteSegments(items: { segment: string; types: number }[]): string {
  return orNone(items.map((item) => `${item.segment} (${item.types} ${item.types === 1 ? 'type' : 'types'})`).join(', '));
}

function quoteCalls(items: { name: string; typeArguments: number; example: string }[]): string {
  return orNone(items
    .map((item) => `${item.name} with ${item.typeArguments} `
      + `${item.typeArguments === 1 ? 'type argument' : 'type arguments'} (${item.example})`)
    .join(', '));
}

function quotePlain(items: string[]): string {
  return orNone(items.join(', '));
}

/**
 * What to say instead of a prompt when there is nothing to profile.
 *
 * A folder with no C# or XAML has no conventions to record, and writing an empty profile would leave
 * a file that looks authoritative and says nothing.
 */
export function nothingToProfile(folderName: string): string {
  return `${folderName} has no C# or XAML files in scope, so there are no conventions to record. `
    + 'Nothing was written. Add the files you want analysed, or widen csharpXamlGraph.include, and run '
    + 'the setup again.';
}

/** The shape the reply should take. It is a format, not an answer, so it comes after the questions. */
const REPLY_SHAPE = `${JSON.stringify({
  version: 1,
  repository: '<the folder name>',
  layers: {
    rules: [
      { layer: 'service', nameSuffixes: ['Handler'], namespaceSegments: ['Services'] },
      { layer: 'viewmodel', nameSuffixes: ['Screen'] },
    ],
    default: 'model',
  },
  links: {
    genericPairCalls: ['Configure'],
    typeofPairCalls: ['RegisterRoute'],
    dependencyPropertyCall: 'Register',
    addCallPattern: '^Add[A-Z]',
    serviceLocator: ['GetService'],
    dataContextCasts: ['is', 'as'],
  },
  findings: {
    entryPoints: ['App', 'Program'],
    transparentBases: ['Page', 'UserControl'],
    generatedMembers: [{ attribute: 'RelayCommand', on: 'method', form: 'commandSuffix' }],
  },
  files: { include: ['**/*.cs', '**/*.xaml'], exclude: ['**/obj/**'] },
}, null, 2)}`;

/**
 * The whole format, printed in both prompts before anything is asked.
 *
 * One call serves both prompts so the create and the refresh conversation cannot describe the format
 * differently, and so no accepted key is ever left for the assistant to infer (FR-023, FR-024).
 */
function contractBlock(): string[] {
  return [
    'Every key the profile accepts is described below: what it means, the shape of its value, whether',
    'it is required, and what the extension does when it is absent. Do not write a key this list does',
    'not name, and do not rename or re-nest one of these.',
    '',
    renderContract(),
  ];
}

/**
 * The setup prompt for a repository that has no profile yet.
 *
 * One question per part of the profile — what marks a service, what marks a view model, how a type
 * gets registered, what starts the application, which files matter — each quoting the evidence the
 * analysis found, followed by the shape the answer should arrive in.
 */
export function buildSetupPrompt(evidence: ProfileEvidence, folderName: string): string {
  if (evidence.csharpFiles + evidence.xamlFiles === 0) {
    return nothingToProfile(folderName);
  }

  return [
    `You are writing the conventions profile for the workspace folder "${folderName}".`,
    '',
    `The file ${PROFILE_FILE_NAME} goes at the root of that folder and is committed with the code. It`,
    'records the C#/XAML conventions this repository follows, so the dependency graph places types and',
    'draws registration links by what this repository actually does rather than by a built-in guess.',
    '',
    ...contractBlock(),
    '',
    'Answer the questions below before proposing anything. Each quotes what the analysis already saw',
    'in this repository, so answer from that evidence. If a question cannot be answered from the code',
    'you can see, say so and leave that part out: an absent key keeps the built-in convention.',
    '',
    `Questions about "${folderName}" (${evidence.csharpFiles} C# files and ${evidence.xamlFiles} XAML files read)`,
    '',
    '1. What marks a service, store or other collaborator?',
    `   Type names ending in: ${quoteNames(evidence.nameSuffixes)}`,
    `   Type names starting with: ${quoteNames(evidence.namePrefixes)}`,
    '   Which of these endings and prefixes mean "a collaborator", and which mean "plain data"?',
    '',
    '2. What marks a view model, and where do view models live?',
    `   Namespaces seen: ${quoteSegments(evidence.namespaceSegments)}`,
    '   A namespace on its own is a weak signal: row models and navigation payloads often sit beside',
    '   the view models that own them. Which namespaces hold view models, and what endings mark data',
    '   that must stay a model even there?',
    '',
    '3. How is a type registered or resolved?',
    `   Calls seen: ${quoteCalls(evidence.calls)}`,
    '   Which of these calls register a type with the container, which pair a type with what it',
    '   resolves, and which read a service out of the container at run time?',
    '',
    '4. Which types start the application, and which base types add nothing to bind to?',
    `   Base types declared here: ${quotePlain(evidence.baseTypes)}`,
    `   Attributes on methods: ${quotePlain(evidence.methodAttributes)}`,
    `   Attributes on fields: ${quotePlain(evidence.fieldAttributes)}`,
    '   Which base types are framework types that declare no application members, and which',
    '   attributes make the compiler generate a member a binding may name?',
    '',
    '5. Which files should the analysis read?',
    '   State any folder that holds generated, vendored or sample code which should stay out.',
    `   XAML root elements seen: ${quotePlain(evidence.xamlRootTags.map((tag) => `${tag.tag} (${tag.files})`))}`,
    '',
    'Reply with one JSON object only, in this shape. Leave out every key you are not sure about:',
    '',
    '```json',
    REPLY_SHAPE,
    '```',
  ].join('\n');
}

/**
 * The refresh prompt for a repository that already has a profile.
 *
 * It carries the profile as written, names the parts of the format it does not cover, and asks for
 * conventions the code has gained since. Additions only: a declaration the profile already holds is
 * kept, because a refresh that dropped one would quietly change how the graph reads the repository.
 */
export function buildRefreshPrompt(
  evidence: ProfileEvidence,
  profile: RepositoryProfile,
  folderName: string,
): string {
  const covered: string[] = [];
  if (profile.layers?.rules?.length) {
    covered.push(`layer rules (${profile.layers.rules.length})`);
  }
  if (profile.links) {
    covered.push('link detection');
  }
  if (profile.findings) {
    covered.push('findings rules');
  }
  if (profile.files) {
    covered.push('file selection');
  }

  const uncovered: string[] = [];
  if (!profile.layers?.rules?.length) {
    uncovered.push('which name endings and namespaces place a type in a layer');
  }
  if (!profile.links) {
    uncovered.push('which calls register or resolve a type');
  }
  if (!profile.findings) {
    uncovered.push('which types are entry points, which bases are transparent, and which members are '
      + 'generated');
  }
  if (!profile.files) {
    uncovered.push('which files are read');
  }

  const lines = [
    `The workspace folder "${folderName}" already has a conventions profile at ${PROFILE_FILE_NAME}.`,
    '',
    'It currently declares:',
    ...(covered.length > 0 ? covered.map((entry) => `- ${entry}`) : ['- nothing yet']),
    '',
    'Here is the profile as it stands:',
    '',
    '```json',
    formatProfile(profile).trimEnd(),
    '```',
  ];

  if (uncovered.length > 0) {
    lines.push(
      '',
      'It says nothing about:',
      ...uncovered.map((entry) => `- ${entry}`),
    );
  }

  lines.push(
    '',
    'Before you answer, here is the whole format, so every key you use is one it accepts:',
    '',
    ...contractBlock(),
    '',
    'Answer from the evidence below before proposing anything:',
    `- Calls seen now: ${quoteCalls(evidence.calls)}`,
    `- Type names ending in: ${quoteNames(evidence.nameSuffixes)}`,
    `- Namespaces seen: ${quoteSegments(evidence.namespaceSegments)}`,
    `- Base types declared here: ${quotePlain(evidence.baseTypes)}`,
    '',
    'Has the repository gained a convention since this profile was written? Look for registration',
    'calls, type-name endings, namespace segments and attributes that no declaration above covers,',
    'and propose only the additions. Every declaration already in the file is kept as written, so a',
    'key you leave out stays exactly as it is.',
    '',
    'Reply with the additions as one JSON object in the same shape as the file above. Include only',
    'the keys that change or are new.',
  );

  return lines.join('\n');
}
