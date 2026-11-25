/**
 * A request to show the graph around something the reader right-clicked.
 *
 * The panel's model is type-level, so a member selection arrives here as the type that declares it
 * plus the member name; the webview highlights the edges that carry that member.
 */
export interface GraphRequest {
  /** What the reader asked for, shown in the focus bar. */
  title: string;
  /** Types the graph is restricted to. Empty means the whole workspace. */
  nodeIds: string[];
  /** The type to centre and select. */
  focus?: string;
  /** Member (method, property, field) whose dependency lines are highlighted. */
  member?: string;
  /** How many hops of partners to keep around the scope. */
  depth?: number;
}
