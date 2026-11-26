/**
 * The per-repository answer to "can this repository be analysed at all".
 *
 * It is decided from structure alone — counts and file names from one path walk, no file opened
 * and no convention gathered — so it can be issued before the first profile question and it can
 * never be a paraphrase of what the analysis later found. The verdict reads and writes nothing in
 * the repository it describes.
 *
 * It is pure: nothing here imports `vscode` or `node:fs`.
 */

/** The number of files the analysis reads before it stops. The walk and the analysis share it. */
export const ANALYSIS_FILE_CAP = 20000;

/** What one path walk saw. Counts are files under the built-in selection. */
export interface StructureFacts {
  /** Files ending `.cs`. */
  csharpFiles: number;
  /** Files ending `.xaml`. */
  xamlFiles: number;
  /** Repository-relative paths ending `.csproj`, `.fsproj`, `.vbproj` or `.vcxproj`, sorted. */
  projectFiles: string[];
  /** Repository-relative paths ending `.sln`, sorted. */
  solutionFiles: string[];
  /** True when the walk stopped at the cap, so the two counts are a floor rather than a total. */
  truncated: boolean;
}

/** Which observation failed. */
export type IncompatibilityCode =
  | 'no-csharp'
  | 'no-xaml'
  | 'over-analysis-cap'
  | 'unsupported-project'
  | 'unreadable-structure';

/**
 * One thing the extension cannot handle, named as a structure rather than as a general failure.
 *
 * `evidence` is never empty: a reason with no evidence is an opinion. It carries counts and up to
 * three repository-relative paths, and no absolute path, machine name or timestamp, so a recorded
 * verdict stays portable.
 */
export interface IncompatibilityReason {
  code: IncompatibilityCode;
  /** The thing that cannot be handled, in the reader's words: `no C# files`, or `src/Other.fsproj`. */
  structure: string;
  /** One plain sentence naming the counts where the counts are the evidence. */
  message: string;
  evidence: string[];
}

export type CompatibilityVerdictKind = 'compatible' | 'incompatible' | 'unknown';

/** The verdict, in the shape `contracts/compatibility.schema.json` records. */
export interface CompatibilityVerdict {
  schemaVersion: 1;
  /** The slug of the repository the verdict is about. */
  repository: string;
  verdict: CompatibilityVerdictKind;
  reasons: IncompatibilityReason[];
  facts: StructureFacts;
  limits: { analysisFileCap: number };
}

/** The facts of a folder the walk could not list at all. */
export const UNREADABLE_FACTS: StructureFacts = {
  csharpFiles: 0,
  xamlFiles: 0,
  projectFiles: [],
  solutionFiles: [],
  truncated: false,
};

/**
 * The slug an identifier rule gives a repository name: lower case, every run of characters outside
 * `a-z0-9` replaced by one `-`, leading and trailing `-` removed.
 *
 * One rule serves the verdict, the request folder and the validation records, so the three always
 * agree on what a checkout is called.
 */
export function slugFor(name: string): string {
  return name
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-+|-+$/g, '');
}

/** Project kinds the analysis does not read at all. A `.csproj` is not one of them. */
const UNSUPPORTED_EXTENSIONS = ['.fsproj', '.vbproj', '.vcxproj'];

/** At most three paths are named, so a reason stays readable and the schema's cap is honoured. */
const MAX_NAMED_PATHS = 3;

function plural(count: number, one: string, many: string): string {
  return `${count} ${count === 1 ? one : many}`;
}

function noCsharp(facts: StructureFacts): IncompatibilityReason {
  return {
    code: 'no-csharp',
    structure: 'no C# files',
    message: `The analysis reads C# and XAML, and found no C# files in the files it reads, so there are no `
      + `types to draw (${plural(facts.xamlFiles, 'XAML file', 'XAML files')} read).`,
    evidence: [`0 C# files`, plural(facts.xamlFiles, 'XAML file', 'XAML files')],
  };
}

function noXaml(facts: StructureFacts): IncompatibilityReason {
  return {
    code: 'no-xaml',
    structure: 'no XAML files',
    message: `The analysis reads C# and XAML, and found no XAML files in the files it reads, so no view-to-view-model `
      + `binding can be drawn (${plural(facts.csharpFiles, 'C# file', 'C# files')} read).`,
    evidence: [`0 XAML files`, plural(facts.csharpFiles, 'C# file', 'C# files')],
  };
}

function overCap(facts: StructureFacts, cap: number): IncompatibilityReason {
  const total = facts.csharpFiles + facts.xamlFiles;
  return {
    code: 'over-analysis-cap',
    structure: 'more files than the analysis reads',
    message: `The analysis stops at ${cap} files, and this repository has ${total} C# and XAML files in the `
      + 'files it reads, so it could not be read in full.',
    evidence: [
      `${total} C# and XAML files`,
      `the analysis reads at most ${cap}`,
      ...(facts.truncated ? ['the walk stopped at the cap, so the count is a floor'] : []),
    ],
  };
}

function unsupportedProject(path: string, total: number, shown: number): IncompatibilityReason {
  const extension = UNSUPPORTED_EXTENSIONS.find((known) => path.toLowerCase().endsWith(known)) ?? 'project';
  return {
    code: 'unsupported-project',
    structure: path,
    message: `The analysis reads .csproj projects and C# sources, and this repository is built as a ${extension} `
      + `project (${path}), which it does not read.`,
    evidence: [
      `1 ${extension} project`,
      ...(total > shown ? [`${total} unsupported project files in total`] : []),
    ],
  };
}

function unreadableStructure(): IncompatibilityReason {
  return {
    code: 'unreadable-structure',
    structure: 'the repository folder',
    message: 'The repository folder could not be listed, so its languages and project kinds were not checked.',
    evidence: ['the folder could not be listed'],
  };
}

/**
 * One reason per failed observation, in a stable order, so the person is told all of what is wrong
 * rather than the first thing found.
 *
 * `facts` is undefined when the walk could not list the folder at all, which is a third answer rather
 * than a soft yes: profile work then proceeds as it does today, with one note saying the structure was
 * not checked.
 */
export function assessCompatibility(
  facts: StructureFacts | undefined,
  cap: number,
  repository = '',
): CompatibilityVerdict {
  const limits = { analysisFileCap: cap };

  if (!facts) {
    return {
      schemaVersion: 1,
      repository,
      verdict: 'unknown',
      reasons: [unreadableStructure()],
      facts: UNREADABLE_FACTS,
      limits,
    };
  }

  const reasons: IncompatibilityReason[] = [];
  if (facts.csharpFiles === 0) {
    reasons.push(noCsharp(facts));
  }
  if (facts.xamlFiles === 0) {
    reasons.push(noXaml(facts));
  }
  if (facts.truncated || facts.csharpFiles + facts.xamlFiles >= cap) {
    reasons.push(overCap(facts, cap));
  }

  // A `.csproj` or a `.sln` is recorded as a fact and never decides the verdict, so only the project
  // kinds the analysis does not read at all are named here.
  const unsupported = facts.projectFiles
    .filter((path) => UNSUPPORTED_EXTENSIONS.some((known) => path.toLowerCase().endsWith(known)));
  const named = unsupported.slice(0, MAX_NAMED_PATHS);
  named.forEach((path) => reasons.push(unsupportedProject(path, unsupported.length, named.length)));

  return {
    schemaVersion: 1,
    repository,
    verdict: reasons.length > 0 ? 'incompatible' : 'compatible',
    reasons,
    facts,
    limits,
  };
}

/* ------------------------------------------------------- how a reason reaches the pane */

/** How a compatibility reason reads in the model's warning list. */
export const NOT_ANALYSED = 'not analysed';

/**
 * The one format the host writes and the page reads, so a reason the analysis could not act on is
 * shown above the findings without the model growing a second field for it.
 */
export function compatibilityWarningText(reason: IncompatibilityReason): string {
  return `${NOT_ANALYSED} › ${reason.structure}: ${reason.message}`;
}

/** The note a model warning carries, or undefined when the warning is about something else. */
export function compatibilityNote(warning: string): { structure: string; message: string } | undefined {
  const prefix = `${NOT_ANALYSED} › `;
  if (!warning.startsWith(prefix)) {
    return undefined;
  }
  const body = warning.slice(prefix.length);
  const split = body.indexOf(': ');
  return split < 0
    ? { structure: body, message: '' }
    : { structure: body.slice(0, split), message: body.slice(split + 2) };
}

/* --------------------------------------------------------------- the request file */

/** One change the extension needs, so a reason is never a dead end. */
export interface RequiredChange {
  /** What must change in the extension, in one line. */
  title: string;
  /** The reason code this change answers. */
  because: IncompatibilityCode;
  /** One sentence saying what the extension would do differently. */
  change: string;
  /** The repository's own facts. Never empty. */
  evidence: string[];
}

/**
 * Every reason code mapped to at least one change.
 *
 * A refusal that names a problem and asks for nothing is a dead end, so the mapping is total: a new
 * code cannot be added without saying what would fix it.
 */
export const CHANGES_FOR_REASON: Record<IncompatibilityCode, Omit<RequiredChange, 'evidence'>[]> = {
  'no-csharp': [
    {
      title: 'Read a repository that declares no C#.',
      because: 'no-csharp',
      change: 'The extension would accept a repository whose types are not C# and place them on the graph, '
        + 'instead of stopping because the C# file count is zero.',
    },
    {
      title: 'Say which languages the analysis reads, before asking about conventions.',
      because: 'no-csharp',
      change: 'The setup prompt would name the languages the analysis understands, so a repository built in '
        + 'another language is told what would be needed rather than asked questions it cannot answer.',
    },
  ],
  'no-xaml': [
    {
      title: 'Read a repository that declares no XAML.',
      because: 'no-xaml',
      change: 'The extension would draw the C# dependency graph for a repository with no XAML, and treat the '
        + 'missing view layer as absent rather than as a reason to refuse the whole repository.',
    },
  ],
  'over-analysis-cap': [
    {
      title: 'Analyse a repository larger than the current file cap.',
      because: 'over-analysis-cap',
      change: `The extension would analyse past ${ANALYSIS_FILE_CAP} files, or say which part of the repository `
        + 'it stopped before, instead of refusing a repository it could not read in full.',
    },
  ],
  'unsupported-project': [
    {
      title: 'Read the project kinds the analysis refuses.',
      because: 'unsupported-project',
      change: 'The extension would read .fsproj, .vbproj and .vcxproj projects, or accept a repository built as '
        + 'one without a C# project beside it.',
    },
  ],
  'unreadable-structure': [
    {
      title: 'List a repository the process cannot read.',
      because: 'unreadable-structure',
      change: 'The extension would read the structure of a folder opened over a remote or virtual file system, '
        + 'rather than reporting that its structure could not be checked.',
    },
  ],
};

/** The changes one reason requires, each carrying that reason's evidence. */
export function changesFor(reason: IncompatibilityReason): RequiredChange[] {
  return CHANGES_FOR_REASON[reason.code].map((change) => ({ ...change, evidence: [...reason.evidence] }));
}

/** Why this repository could not be profiled, in one sentence. */
function summary(verdict: CompatibilityVerdict): string {
  const count = verdict.reasons.length;
  return `${verdict.repository} could not be profiled: the extension found `
    + `${plural(count, 'structure', 'structures')} it cannot handle, before any convention was asked.`;
}

/**
 * The request file, built from the verdict alone.
 *
 * Every reason is one section in the verdict's order, opening with the sentence the person was shown,
 * so the file and the dialog cannot say different things. Every change is a subsection carrying at
 * least one evidence line from the requesting repository.
 */
export function buildUpdateRequest(verdict: CompatibilityVerdict): string {
  const lines = [
    `# Extension change request: ${verdict.repository}`,
    '',
    'Run this with `/speckit-companion-specify`.',
    '',
    summary(verdict),
  ];

  for (const reason of verdict.reasons) {
    lines.push('', `## ${reason.message}`, '');
    lines.push(`Structure: ${reason.structure}`);
    lines.push(`Evidence: ${reason.evidence.slice(0, MAX_NAMED_PATHS).join(', ')}`);

    for (const change of changesFor(reason)) {
      lines.push('', `### ${change.title}`, '', change.change, '', 'Evidence:');
      for (const item of change.evidence) {
        lines.push(`- ${item}`);
      }
    }
  }

  lines.push(
    '',
    '## What was not changed',
    '',
    `Nothing in ${verdict.repository} was created or changed. This file is a request against the extension.`,
  );

  return `${lines.join('\n')}\n`;
}
