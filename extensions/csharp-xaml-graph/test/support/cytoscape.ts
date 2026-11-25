import cytoscape, { type Core, type ElementDefinition } from 'cytoscape';
import dagre from 'cytoscape-dagre';

let registered = false;

/** Registers the dagre layout once; `main.ts` also registers it, so a second call is tolerated. */
export function registerDagre(): void {
  if (registered) {
    return;
  }
  try {
    cytoscape.use(dagre);
  } catch {
    // Already registered by another module instance in this process.
  }
  registered = true;
}

/**
 * A real cytoscape core in jsdom.
 *
 * The panel element needs a measured size and a zero padding: cytoscape computes its viewport as
 * `clientWidth - padding`, and jsdom reports an empty padding, which parses as NaN.
 */
export function createCore(elements: ElementDefinition[] = []): Core {
  registerDagre();

  const container = document.createElement('div');
  container.style.padding = '0px';
  Object.defineProperty(container, 'clientWidth', { value: 1000, configurable: true });
  Object.defineProperty(container, 'clientHeight', { value: 700, configurable: true });
  document.body.appendChild(container);

  return cytoscape({
    container,
    elements,
    layout: { name: 'preset' },
    style: [
      { selector: 'node', style: { width: 40, height: 20, label: 'data(label)' } },
    ],
  });
}

export const nodeData = (id: string, extra: Record<string, unknown> = {}): ElementDefinition => ({
  group: 'nodes',
  data: { id, label: id.split('.').pop() ?? id, ...extra },
});

export const edgeData = (source: string, target: string, id = `${source}->${target}`): ElementDefinition => ({
  group: 'edges',
  data: { id, source, target },
});
