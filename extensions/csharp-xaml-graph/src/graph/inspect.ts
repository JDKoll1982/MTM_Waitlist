/**
 * The inspection: one reading of a folder, reported without an editor and without deciding anything.
 *
 * A profile author, the setup prompt and an expected-answer record all need the same things from a
 * repository — its structure, the files that are read, its types, its namespaces and its naming
 * habits. This module derives them once. The prompt quotes them, `--inspect` prints them and
 * `--expected` writes them down, so the three cannot describe one repository three different ways.
 *
 * It reports and does not judge: no layer is decided, no link is drawn and no finding is computed
 * here. Those are the analysis's answers, and an inspection that guessed at them would be the
 * purpose-written second reading FR-030 rules out.
 *
 * It needs no editor. The walk and the read are here, in `src/structure.ts`'s sibling boundary, and
 * nothing in this file imports `vscode`.
 */

import { readdirSync, readFileSync } from 'node:fs';
import { join } from 'node:path';

import type { StructureFacts } from './compatibility';
import { ANALYSIS_FILE_CAP } from './compatibility';
import { compileConventions, type CompiledConventions } from './conventions';
import type { CSharpFile, TypeDecl } from './csharp/parser';
import { initCSharpGrammar, parseCSharpFile } from './csharp/parser';
import { BUILT_IN_SELECTION, prunesDirectory, selectsFile, type FileSelection } from './files';
import type { ProfileEvidence } from './model';
import type { XamlFile } from './xaml/parser';
import { parseXamlFile } from './xaml/parser';
import { gatherStructure } from '../structure';

/** One source file the walk read, ready for the parser. */
export interface SourceText {
  /** Path relative to the folder inspected, with forward slashes. */
  path: string;
  text: string;
}

/** A declared type and the file its first declaration was read from. */
export interface MergedDeclaration {
  decl: TypeDecl;
  file: string;
}

/** What the inspection needs from its caller. */
export interface InspectOptions {
  /** The C# grammar. `npm run compile` copies it beside the bundles, under `media/`. */
  grammarWasmPath: string;
  /** Files to read. The built-in selection when absent. */
  selection?: FileSelection;
  /**
   * Conventions to parse with. The built-in ones when absent. Only the service-locator call names
   * reach the parse, so a profile that declares its own locator is read as it declares it.
   */
  conventions?: CompiledConventions;
}

/** What one inspection found. Nothing here is a judgement about the repository. */
export interface Inspection {
  /** Facts from one listing walk, absent when the folder could not be listed at all. */
  structure?: StructureFacts;
  /** Files the parse pass actually read, after the selection, and whether the cap stopped it. */
  files: { csharp: number; xaml: number; truncated: boolean };
  /** What a profile author needs. Absent when the folder could not be read. */
  evidence?: ProfileEvidence;
  /**
   * Every declared type, merged by full name, with the file it was read from. Absent when the folder
   * could not be read. It carries no layer, no link and no finding: those are the analysis's answers.
   */
  declarations?: Map<string, MergedDeclaration>;
  /**
   * The `x:Class` each XAML file states, and the file that states it, sorted by name. It is a fact
   * about the markup, not a layer decision: whether a class is a view is the analysis's answer.
   */
  xamlClasses: { name: string; file: string }[];
  /** True when the folder could be listed and read, so `structure` and `evidence` are present. */
  readable: boolean;
  /** One plain sentence when it could not be read, so a caller reports rather than throws. */
  problem?: string;
}

/**
 * Every file a run reads, collected by the matcher the host uses.
 *
 * A directory no included file can sit under is pruned rather than walked, and the walk stops at the
 * same cap the analysis stops at, so the count a run measures is the count this walk read.
 */
export function collectSources(
  root: string,
  selection: FileSelection,
): { csharp: SourceText[]; xaml: SourceText[]; truncated: boolean } {
  const csharp: SourceText[] = [];
  const xaml: SourceText[] = [];
  let truncated = false;
  const pending: { absolute: string; relative: string }[] = [{ absolute: root, relative: '' }];

  while (pending.length > 0) {
    const current = pending.pop()!;
    let entries;
    try {
      entries = readdirSync(current.absolute, { withFileTypes: true });
    } catch {
      continue;
    }

    for (const entry of entries) {
      const relative = current.relative ? `${current.relative}/${entry.name}` : entry.name;
      if (entry.isDirectory()) {
        if (!prunesDirectory(selection, relative)) {
          pending.push({ absolute: join(current.absolute, entry.name), relative });
        }
        continue;
      }
      if (!selectsFile(selection, relative)) {
        continue;
      }
      const lower = entry.name.toLowerCase();
      const target = lower.endsWith('.cs') ? csharp : lower.endsWith('.xaml') ? xaml : undefined;
      if (!target) {
        continue;
      }
      try {
        target.push({ path: relative, text: readFileSync(join(current.absolute, entry.name), 'utf8') });
      } catch {
        // A file this process may not read is left out rather than failing the whole run.
      }
      if (csharp.length + xaml.length >= ANALYSIS_FILE_CAP) {
        truncated = true;
        pending.length = 0;
        break;
      }
    }
  }

  return { csharp, xaml, truncated };
}

/**
 * Stamps the origin file onto every record a declaration produced.
 *
 * A partial type merges records from several files, and each record's line number is only meaningful
 * against the file it was parsed from. Without this, an evidence location for a member declared in a
 * later partial names the first partial's file and points at the wrong line.
 */
function stampOrigin(decl: TypeDecl, file: string): void {
  const mark = (record: { file?: string }): void => {
    if (!record.file) {
      record.file = file;
    }
  };
  decl.constructors.forEach(mark);
  decl.properties.forEach(mark);
  decl.fields.forEach(mark);
  decl.methods.forEach(mark);
  decl.assignments.forEach(mark);
  decl.calls.forEach(mark);
  decl.creations.forEach(mark);
  decl.mentions.forEach(mark);
}

/** Merges every declaration of a partial type into one record. */
export function mergeDeclarations(files: CSharpFile[]): Map<string, MergedDeclaration> {
  const merged = new Map<string, MergedDeclaration>();

  for (const file of files) {
    for (const decl of file.types) {
      stampOrigin(decl, file.path);

      const existing = merged.get(decl.fullName);
      if (!existing) {
        merged.set(decl.fullName, {
          decl: {
            ...decl,
            constructors: [...decl.constructors],
            properties: [...decl.properties],
            fields: [...decl.fields],
            methods: [...decl.methods],
            assignments: [...decl.assignments],
            calls: [...decl.calls],
            creations: [...decl.creations],
            mentions: [...decl.mentions],
            typeofRefs: [...decl.typeofRefs],
            bases: [...decl.bases],
          },
          file: file.path,
        });
        continue;
      }

      // Every member list has to be merged. Dropping one here silently loses that whole kind of
      // evidence for the rest of the type — which is what made commands declared in a second partial
      // read as dead bindings.
      const target = existing.decl;
      target.constructors.push(...decl.constructors);
      target.properties.push(...decl.properties);
      target.fields.push(...decl.fields);
      target.methods.push(...decl.methods);
      target.assignments.push(...decl.assignments);
      target.calls.push(...decl.calls);
      target.creations.push(...decl.creations);
      target.mentions.push(...decl.mentions);
      target.typeofRefs.push(...decl.typeofRefs);
      for (const base of decl.bases) {
        if (!target.bases.includes(base)) {
          target.bases.push(base);
        }
      }
      target.isPartial = target.isPartial || decl.isPartial;
      target.isAbstract = target.isAbstract || decl.isAbstract;
    }
  }

  return merged;
}

/**
 * Counts and examples drawn from the parsed declarations, for the profile prompts.
 *
 * Nothing here reads a file again: the parse has already seen every type, namespace and call site,
 * which is exactly the evidence a question needs to be answerable from the repository. Each list is
 * capped and sorted so the same repository always produces the same prompt.
 *
 * `entries` is the merged declaration map, so this is the same derivation `analyzeWorkspace` carries
 * on its model rather than a second one.
 */
export function deriveProfileEvidence(
  entries: Map<string, MergedDeclaration>,
  xamlFiles: XamlFile[],
  csharpFiles: number,
  xamlCount: number,
): ProfileEvidence {
  const CAP = 20;
  const EXAMPLES = 2;

  /** The trailing PascalCase word: `SignInViewModel` → `ViewModel`, `DunnageHandler` → `Handler`. */
  const suffixOf = (name: string): string => {
    const parts = name.replace(/^I(?=[A-Z])/, '').match(/[A-Z][a-z0-9]*/g);
    return parts && parts.length > 1 ? parts[parts.length - 1] : name;
  };

  const tally = (keys: (name: string) => string[]): { key: string; examples: string[]; count: number }[] => {
    const byKey = new Map<string, { examples: string[]; count: number }>();
    for (const entry of entries.values()) {
      for (const key of keys(entry.decl.name)) {
        const seen = byKey.get(key) ?? { examples: [], count: 0 };
        seen.count += 1;
        if (seen.examples.length < EXAMPLES && !seen.examples.includes(entry.decl.name)) {
          seen.examples.push(entry.decl.name);
        }
        byKey.set(key, seen);
      }
    }
    return [...byKey]
      .map(([key, value]) => ({ key, examples: value.examples, count: value.count }))
      .filter((entry) => entry.count >= 2)
      .sort((left, right) => right.count - left.count || left.key.localeCompare(right.key))
      .slice(0, CAP);
  };

  const suffixes = tally((name) => [suffixOf(name)]);
  const prefixes = tally((name) => {
    const match = /^[A-Za-z]+_/.exec(name);
    return match ? [match[0]] : [];
  });

  const segmentCounts = new Map<string, number>();
  for (const entry of entries.values()) {
    for (const segment of entry.decl.namespace.split('.').filter(Boolean)) {
      segmentCounts.set(segment, (segmentCounts.get(segment) ?? 0) + 1);
    }
  }
  const segments = [...segmentCounts]
    .map(([segment, types]) => ({ segment, types }))
    .sort((left, right) => right.types - left.types || left.segment.localeCompare(right.segment))
    .slice(0, CAP);

  const callSites = new Map<string, { name: string; typeArguments: number; example: string; count: number }>();
  for (const entry of entries.values()) {
    for (const call of entry.decl.calls) {
      const typeArguments = call.typeArgs.length > 0 ? call.typeArgs.length : call.typeOfArgs.length;
      if (typeArguments === 0) {
        continue;
      }
      const key = `${call.method}|${typeArguments}`;
      const seen = callSites.get(key);
      if (seen) {
        seen.count += 1;
        continue;
      }
      callSites.set(key, {
        name: call.method,
        typeArguments,
        example: `${call.file ?? entry.file}:${call.line}`,
        count: 1,
      });
    }
  }
  const calls = [...callSites.values()]
    .sort((left, right) => right.count - left.count || left.name.localeCompare(right.name))
    .slice(0, CAP)
    .map(({ name, typeArguments, example }) => ({ name, typeArguments, example }));

  const baseTypes = new Set<string>();
  const methodAttributes = new Set<string>();
  const fieldAttributes = new Set<string>();
  for (const entry of entries.values()) {
    for (const base of entry.decl.bases) {
      const simple = base.includes('<') ? base.slice(0, base.indexOf('<')) : base;
      const tail = simple.slice(simple.lastIndexOf('.') + 1).trim();
      if (tail) {
        baseTypes.add(tail);
      }
    }
    for (const method of entry.decl.methods) {
      for (const attribute of method.attributes) {
        methodAttributes.add(attribute);
      }
    }
    for (const field of entry.decl.fields) {
      for (const attribute of field.attributes) {
        fieldAttributes.add(attribute);
      }
    }
  }

  const rootTags = new Map<string, number>();
  for (const xaml of xamlFiles) {
    rootTags.set(xaml.rootTag, (rootTags.get(xaml.rootTag) ?? 0) + 1);
  }

  return {
    nameSuffixes: suffixes.map((entry) => ({ name: entry.key, examples: entry.examples })),
    namePrefixes: prefixes.map((entry) => ({ name: entry.key, examples: entry.examples })),
    namespaceSegments: segments,
    calls,
    baseTypes: [...baseTypes].sort((left, right) => left.localeCompare(right)),
    methodAttributes: [...methodAttributes].sort((left, right) => left.localeCompare(right)),
    fieldAttributes: [...fieldAttributes].sort((left, right) => left.localeCompare(right)),
    xamlRootTags: [...rootTags]
      .map(([tag, files]) => ({ tag, files }))
      .sort((left, right) => right.files - left.files || left.tag.localeCompare(right.tag)),
    csharpFiles,
    xamlFiles: xamlCount,
  };
}

const NO_FILES = { csharp: 0, xaml: 0, truncated: false };

/**
 * Inspects one folder: its structure, the files the selection reads, and the evidence a profile
 * author needs.
 *
 * A folder that cannot be listed is reported rather than thrown, because the caller's answer is a
 * sentence and an exit code, not a stack trace. A folder with no C# and no XAML is not a failure: its
 * facts come back and its evidence set is empty, which is what lets an empty repository be described.
 */
export async function inspectFolder(root: string, options: InspectOptions): Promise<Inspection> {
  const selection = options.selection ?? BUILT_IN_SELECTION;
  const conventions = options.conventions ?? compileConventions();
  const structure = gatherStructure(root);
  if (!structure) {
    return { files: NO_FILES, readable: false, problem: 'the folder could not be listed', xamlClasses: [] };
  }

  const sources = collectSources(root, selection);
  await initCSharpGrammar(options.grammarWasmPath);

  const csharpFiles: CSharpFile[] = [];
  for (const file of sources.csharp) {
    try {
      csharpFiles.push(parseCSharpFile(file.path, file.text, conventions.serviceLocator));
    } catch {
      // One file the parser cannot take is left out, exactly as the analysis leaves it out.
    }
  }
  const xamlFiles: XamlFile[] = [];
  for (const file of sources.xaml) {
    try {
      xamlFiles.push(parseXamlFile(file.path, file.text));
    } catch {
      // Same: a file that will not parse is not a reason to abandon the inspection.
    }
  }

  const merged = mergeDeclarations(csharpFiles);

  return {
    structure,
    files: { csharp: sources.csharp.length, xaml: sources.xaml.length, truncated: sources.truncated },
    evidence: deriveProfileEvidence(merged, xamlFiles, sources.csharp.length, sources.xaml.length),
    declarations: merged,
    xamlClasses: xamlFiles
      .filter((file) => file.className !== null)
      .map((file) => ({ name: file.className as string, file: file.path }))
      .sort((left, right) => (left.name === right.name ? 0 : left.name < right.name ? -1 : 1)),
    readable: true,
  };
}
