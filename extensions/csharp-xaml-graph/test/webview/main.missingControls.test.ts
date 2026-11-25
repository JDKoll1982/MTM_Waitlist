/**
 * The panel when its own markup is missing a checkbox.
 *
 * `wireControls` looks every layer and edge-kind checkbox up by id, so a markup change that drops
 * one must not stop the rest of the toolbar from being wired. The real page always carries them —
 * the extension's own selftest asserts that — so this is the only way the guards are reachable.
 */
import { beforeAll, describe, expect, it } from 'vitest';
import { renderHtml } from '../../src/webview/html';
import { graphEdge, graphModel, graphNode } from '../support/model';
import { Uri } from '../support/vscode';

const posted: Record<string, unknown>[] = [];
const el = <T extends HTMLElement>(id: string): T => document.getElementById(id) as T;
const tick = (ms = 0): Promise<void> => new Promise((resolve) => setTimeout(resolve, ms));

const model = graphModel({
  nodes: [
    graphNode({ id: 'N.A', label: 'A', layer: 'view', module: 'M' }),
    graphNode({ id: 'N.B', label: 'B', layer: 'view', module: 'M' }),
  ],
  edges: [graphEdge({ id: 'e1', source: 'N.A', target: 'N.B', kind: 'binds' })],
});

beforeAll(async () => {
  const html = renderHtml({
    webview: { cspSource: 'test', asWebviewUri: (uri: Uri) => uri } as never,
    scriptUri: Uri.file('main.js'),
    styleUri: Uri.file('style.css'),
    nonce: 'n',
  });
  document.body.innerHTML = new DOMParser().parseFromString(html, 'text/html').body.innerHTML;

  document.getElementById('layer-service')?.remove();
  document.getElementById('edge-binds')?.remove();

  (window as unknown as { acquireVsCodeApi: () => unknown }).acquireVsCodeApi = () => ({
    postMessage: (message: Record<string, unknown>) => posted.push(message),
    getState: () => undefined,
    setState: () => undefined,
  });
  await import('../../src/webview/client/main');
  await tick(10);
});

describe('the toolbar with a missing checkbox', () => {
  it('still wires the controls that are present', async () => {
    const container = el<HTMLDivElement>('cy');
    container.style.padding = '0px';
    Object.defineProperty(container, 'clientWidth', { value: 800, configurable: true });
    Object.defineProperty(container, 'clientHeight', { value: 600, configurable: true });

    const layout = el<HTMLSelectElement>('layout');
    layout.value = 'layered-tb';
    layout.dispatchEvent(new Event('change'));

    window.dispatchEvent(new MessageEvent('message', { data: { type: 'model', model } }));
    await tick(20);

    expect(el('stats').textContent).toContain('of 2 nodes');

    const present = el<HTMLInputElement>('layer-view');
    present.checked = false;
    present.dispatchEvent(new Event('change'));
    expect(el('stats').textContent).toContain('0 of 2 nodes');
  });
});


