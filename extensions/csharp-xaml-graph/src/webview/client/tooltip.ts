import type { EdgeKind, Finding, GraphEdge } from '../../graph/model';
import { EDGE_KIND_LABEL, FINDING_LABEL } from '../../graph/model';

/** Swatch colour per edge kind, matching the canvas style so the tooltip agrees with the line. */
export const EDGE_COLOUR: Record<EdgeKind, string> = {
  resolves: '#2fbf71',
  getservice: '#4a9eff',
  binds: '#38bdf8',
  injects: '#f0a030',
  field: '#4a9eff',
  constructs: '#c084fc',
  composes: '#c084fc',
  implements: '#8a8a8a',
  inherits: '#8a8a8a',
  reference: '#6b6b6b',
  template: '#555555',
};

/** What each confidence means in the reader's terms, not in the analyzer's. */
const CONFIDENCE_LABEL: Record<GraphEdge['confidence'], string> = {
  high: 'read straight from the source',
  medium: 'inferred from a reference',
  low: 'a weak hint — worth checking',
};

/** Everything the hover tooltip states about one edge, computed without touching the DOM. */
export interface EdgeSummary {
  kind: EdgeKind;
  kindLabel: string;
  colour: string;
  sourceLabel: string;
  targetLabel: string;
  sourceFullName: string;
  targetFullName: string;
  confidence: GraphEdge['confidence'];
  confidenceLabel: string;
  /** The member the edge came from, e.g. `new SplashPage` or the property name. */
  member?: string;
  file?: string;
  line?: number;
}

export interface EdgeSummaryInput {
  kind: EdgeKind;
  sourceLabel: string;
  sourceFullName: string;
  targetLabel: string;
  targetFullName: string;
  confidence: GraphEdge['confidence'];
  member?: string;
  file?: string;
  line?: number;
}

export function summariseEdge(input: EdgeSummaryInput): EdgeSummary {
  return {
    kind: input.kind,
    kindLabel: EDGE_KIND_LABEL[input.kind] ?? input.kind,
    colour: EDGE_COLOUR[input.kind] ?? '#6b6b6b',
    sourceLabel: input.sourceLabel,
    targetLabel: input.targetLabel,
    sourceFullName: input.sourceFullName,
    targetFullName: input.targetFullName,
    confidence: input.confidence,
    confidenceLabel: CONFIDENCE_LABEL[input.confidence] ?? input.confidence,
    member: input.member,
    file: input.file,
    line: input.line,
  };
}

/**
 * The line's tooltip: which two types it joins, what the dependency is, how sure the analyzer is, and
 * the exact line of source that produced it. A line without that evidence is not worth drawing.
 */
export function renderEdgeTooltip(summary: EdgeSummary): string {
  const source = `<strong>${escapeHtml(summary.sourceLabel)}</strong>`;
  const target = `<strong>${escapeHtml(summary.targetLabel)}</strong>`;
  const evidence = summary.file
    ? `<div class="tip-counts">${escapeHtml(summary.file)}:${escapeHtml(summary.line ?? 1)}</div>`
    : '';

  const member = summary.member
    ? `<div class="tip-meta"><span class="muted">${escapeHtml(summary.member)}</span></div>`
    : '';

  return `<div class="tip-edge-head">`
    + `<span class="tip-swatch" style="background:${escapeHtml(summary.colour)}"></span>`
    + `<span class="tip-edge-kind">${escapeHtml(summary.kindLabel)}</span>`
    + `</div>`
    + `<div class="tip-edge-pair">${source} <span class="muted">→</span> ${target}</div>`
    + `<div class="tip-full">${escapeHtml(summary.sourceFullName)} → ${escapeHtml(summary.targetFullName)}</div>`
    + member
    + evidence
    + `<div class="tip-meta"><span class="muted">Confidence: ${escapeHtml(summary.confidence)} — `
    + `${escapeHtml(summary.confidenceLabel)}</span></div>`
    + '<div class="tip-hint muted">Double-click the line to open the code that creates it</div>';
}

/** Everything the hover tooltip states about one node, computed without touching the DOM. */
export interface NodeSummary {
  label: string;
  fullName: string;
  /** Small labels under the name: layer, kind, module, and any state worth calling out. */
  chips: { text: string; cls: string }[];
  /** How many edges leave, arrive and are drawn for this node. */
  out: number;
  incoming: number;
  drawn: number;
  issues: { kind: string; severity: Finding['severity']; detail: string; title: string }[];
}

export interface NodeSummaryInput {
  id: string;
  label: string;
  fullName: string;
  layer: string;
  kind: string;
  module: string;
  file?: string;
  line?: number;
  diRegistered?: boolean;
  inCycle?: boolean;
  /** Every edge in the model; the summary filters to this node. */
  edges: readonly GraphEdge[];
  /** Findings that point at this node. */
  findings: readonly Finding[];
}

const SEVERITY_LABEL: Record<Finding['severity'], string> = {
  high: 'must fix',
  medium: 'review',
  low: 'note',
};

const escapeHtml = (value: unknown): string => String(value)
  .replace(/&/g, '&amp;')
  .replace(/</g, '&lt;')
  .replace(/>/g, '&gt;')
  .replace(/"/g, '&quot;');

const truncate = (text: string, limit: number): string =>
  text.length <= limit ? text : `${text.slice(0, limit - 1).trimEnd()}…`;

/** Issues are ordered by severity, so the tooltip leads with the one that matters. */
const SEVERITY_ORDER: Record<Finding['severity'], number> = { high: 0, medium: 1, low: 2 };

export function summariseNode(input: NodeSummaryInput): NodeSummary {
  const out = input.edges.filter((edge) => edge.source === input.id).length;
  const incoming = input.edges.filter((edge) => edge.target === input.id).length;

  const chips: { text: string; cls: string }[] = [
    { text: input.layer, cls: `layer-${input.layer}` },
    { text: input.kind, cls: 'muted' },
    { text: input.module, cls: 'muted' },
  ];
  if (input.diRegistered) {
    chips.push({ text: 'DI registered', cls: 'muted' });
  }
  if (input.file) {
    chips.push({ text: `${input.file}:${input.line ?? 1}`, cls: 'muted' });
  }

  return {
    label: input.label,
    fullName: input.fullName,
    chips,
    out,
    incoming,
    drawn: out + incoming,
    issues: [...input.findings]
      .sort((left, right) => SEVERITY_ORDER[left.severity] - SEVERITY_ORDER[right.severity])
      .map((finding) => ({
        kind: FINDING_LABEL[finding.kind],
        severity: finding.severity,
        detail: truncate(finding.detail || finding.title, 180),
        title: finding.title,
      })),
  };
}

/** The tooltip body. Hover must state the issues, not hide them behind the Problems tab. */
export function renderTooltip(summary: NodeSummary): string {
  const meta = summary.chips
    .map((chip) => `<span class="${chip.cls}">${escapeHtml(chip.text)}</span>`)
    .join('');

  const counts = `${summary.out} dependenc${summary.out === 1 ? 'y' : 'ies'} out · `
    + `${summary.incoming} used by`;

  const issues = summary.issues.length === 0
    ? '<div class="tip-ok">No cleanup findings.</div>'
    : `<div class="tip-issues">${summary.issues.slice(0, 4).map((issue) => `
        <div class="tip-issue severity-${issue.severity}">
          <span class="kind">${escapeHtml(issue.kind)}</span>
          <span>${escapeHtml(issue.detail)}
            <span class="muted">${escapeHtml(SEVERITY_LABEL[issue.severity])}</span></span>
        </div>`).join('')}${
        summary.issues.length > 4
          ? `<div class="muted">and ${summary.issues.length - 4} more in Problems</div>`
          : ''}
      </div>`;

  return `<div class="tip-head"><strong>${escapeHtml(summary.label)}</strong></div>`
    + `<div class="tip-full">${escapeHtml(summary.fullName)}</div>`
    + `<div class="tip-meta">${meta}</div>`
    + `<div class="tip-counts">${counts}</div>`
    + issues
    + '<div class="tip-hint muted">Right-click to unpack its shape · double-click to open the source</div>';
}
