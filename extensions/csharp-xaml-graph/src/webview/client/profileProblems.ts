import { profileProblemNote } from '../../graph/profile';
import { compatibilityNote } from '../../graph/compatibility';

/**
 * The repository block above the findings.
 *
 * Two kinds of note land here and nowhere else. A *profile problem* is a key the reader could not use;
 * a *compatibility note* is a structure this repository has that the analysis cannot handle. Both are
 * about how the repository was read rather than about the code, so they are met before the cleanup
 * list, and the block returns an empty string when there is nothing to say so the pane never shows an
 * empty box.
 */

/** One note as the pane shows it: the structure it is about, and the sentence. */
interface RepositoryNote {
  path: string;
  message: string;
}

/** The pane renders names taken from a file a person wrote, so every value is escaped. */
function escapeText(value: string): string {
  return value
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;');
}

function notesByKind(warnings: string[]): RepositoryNote[] {
  const notes: RepositoryNote[] = [];
  for (const warning of warnings) {
    const problem = profileProblemNote(warning);
    if (problem) {
      notes.push(problem);
      continue;
    }
    const compatibility = compatibilityNote(warning);
    if (compatibility) {
      notes.push({ path: compatibility.structure, message: compatibility.message });
    }
  }
  return notes;
}

/**
 * One block, one row per note of either kind, escaped; an empty string when there is nothing to say.
 *
 * The structure sits in the same muted path tone a profile problem's key uses, because it plays the
 * same part in the sentence: the thing the note is about.
 */
export function renderRepositoryNotes(warnings: string[]): string {
  const notes = notesByKind(warnings);
  if (notes.length === 0) {
    return '';
  }

  const rows = notes.map((note) => '<li class="profile-problem">'
    + (note.path ? `<code class="profile-problem-path">${escapeText(note.path)}</code>` : '')
    + `<span class="profile-problem-text">${escapeText(note.message)}</span>`
    + '</li>').join('');

  return '<section class="profile-problems" aria-label="Repository notes">'
    + `<h3>This repository (${notes.length} ${notes.length === 1 ? 'note' : 'notes'})</h3>`
    + `<ul class="profile-problem-list">${rows}</ul>`
    + '</section>';
}
