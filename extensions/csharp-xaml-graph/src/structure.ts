/**
 * The two things the compatibility gate needs from the file system: one walk of a folder's tree, and
 * one writer for the request file.
 *
 * The walk lists and never opens: a verdict has to be cheap enough to issue before every profile
 * question, and reading a file to decide whether the folder is readable at all would be the analysis
 * the gate exists to precede. It shares the matcher with the host, so the same files are counted that
 * the analysis would read.
 */

import { mkdirSync, readdirSync, writeFileSync } from 'node:fs';
import { join } from 'node:path';
import { ANALYSIS_FILE_CAP, type StructureFacts } from './graph/compatibility';
import { BUILT_IN_SELECTION, prunesDirectory, selectsFile } from './graph/files';

/** Project files the verdict records, whether or not the analysis reads the project kind. */
const PROJECT_EXTENSIONS = ['.csproj', '.fsproj', '.vbproj', '.vcxproj'];

/** Where a request file goes, relative to the extension root. */
export const UPDATE_PROMPT_FOLDER = 'update_prompts';

function extensionOf(name: string): string {
  const dot = name.lastIndexOf('.');
  return dot < 0 ? '' : name.slice(dot).toLowerCase();
}

function codePointOrder(left: string, right: string): number {
  if (left === right) {
    return 0;
  }
  return left < right ? -1 : 1;
}

/**
 * Lists a folder's tree once and counts what the verdict is decided from.
 *
 * Returns undefined when the root cannot be listed at all — a remote window, a folder this process may
 * not read — which the verdict reports as `unknown` rather than as a refusal. `truncated` is set when
 * the cap stopped the walk, so the two counts are read as a floor.
 */
export function gatherStructure(root: string, cap = ANALYSIS_FILE_CAP): StructureFacts | undefined {
  const facts: StructureFacts = {
    csharpFiles: 0,
    xamlFiles: 0,
    projectFiles: [],
    solutionFiles: [],
    truncated: false,
  };

  const pending: { absolute: string; relative: string }[] = [{ absolute: root, relative: '' }];
  try {
    // Priced before the loop so an unreadable root fails here rather than mid-walk.
    readdirSync(root);
  } catch {
    return undefined;
  }

  while (pending.length > 0) {
    const current = pending.pop()!;
    let entries;
    try {
      entries = readdirSync(current.absolute, { withFileTypes: true });
    } catch {
      // One unreadable subfolder does not hide the rest of the tree; it is simply not counted.
      continue;
    }

    for (const entry of entries) {
      const relative = current.relative ? `${current.relative}/${entry.name}` : entry.name;
      if (entry.isDirectory()) {
        if (!prunesDirectory(BUILT_IN_SELECTION, relative)) {
          pending.push({ absolute: join(current.absolute, entry.name), relative });
        }
        continue;
      }

      const extension = extensionOf(entry.name);
      if (PROJECT_EXTENSIONS.includes(extension)) {
        facts.projectFiles.push(relative);
        continue;
      }
      if (extension === '.sln') {
        facts.solutionFiles.push(relative);
        continue;
      }
      if (!selectsFile(BUILT_IN_SELECTION, relative)) {
        continue;
      }
      if (extension === '.cs') {
        facts.csharpFiles += 1;
      } else if (extension === '.xaml') {
        facts.xamlFiles += 1;
      }
      if (facts.csharpFiles + facts.xamlFiles >= cap) {
        facts.truncated = true;
        pending.length = 0;
        break;
      }
    }
  }

  // A code-point sort, so the recorded list does not depend on the machine's collation rules.
  facts.projectFiles.sort(codePointOrder);
  facts.solutionFiles.sort(codePointOrder);
  return facts;}

/** What happened to a request file. A failure is reported rather than thrown, so a caller can fall back. */
export type WriteRequestResult =
  | { ok: true; path: string }
  | { ok: false; path: string; error: string };

/**
 * Writes `update_prompts/<slug>/request.md` under the extension root it is given.
 *
 * The folder carries the identity and the file inside it is `request.md`, so answering yes twice
 * leaves one current request rather than a stack.
 */
export function writeUpdateRequest(
  extensionRoot: string,
  slug: string,
  text: string,
): WriteRequestResult {
  const relative = `${UPDATE_PROMPT_FOLDER}/${slug}/request.md`;
  try {
    mkdirSync(join(extensionRoot, UPDATE_PROMPT_FOLDER, slug), { recursive: true });
    writeFileSync(join(extensionRoot, UPDATE_PROMPT_FOLDER, slug, 'request.md'), text, 'utf8');
    return { ok: true, path: relative };
  } catch (error) {
    return { ok: false, path: relative, error: (error as Error).message };
  }
}
