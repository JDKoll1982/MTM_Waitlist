import * as path from 'node:path';
import { fileURLToPath } from 'node:url';
import { defineConfig } from 'vitest/config';

const here = path.dirname(fileURLToPath(import.meta.url));

export default defineConfig({
  // `graph/csharp/parser.ts` resolves its own WASM dependency through `createRequire(__filename)`.
  // Under Vite the module is ESM, so the filename is supplied here rather than by Node.
  define: {
    __filename: JSON.stringify(path.join(here, 'src', 'graph', 'csharp', 'parser.ts')),
  },
  resolve: {
    alias: {
      vscode: path.join(here, 'test', 'support', 'vscode.ts'),
    },
  },
  test: {
    environment: 'jsdom',
    globals: false,
    include: ['test/**/*.test.ts'],
    setupFiles: [path.join(here, 'test', 'setup', 'environment.ts')],
    // The C# grammar is loaded once per process; a single fork keeps that cost paid once.
    pool: 'forks',
    poolOptions: { forks: { singleFork: true } },
    testTimeout: 30000,
    hookTimeout: 30000,
    coverage: {
      provider: 'v8',
      reporter: ['text', 'json-summary', 'html'],
      reportsDirectory: 'coverage',
      include: ['src/**/*.ts'],
      // The selftest harness is itself a test script; it is exercised by `npm run selftest`.
      // `request.ts` declares one interface and has no executable code at all.
      exclude: ['src/selftest.ts', 'src/graph/request.ts', 'src/**/*.d.ts'],
      all: true,
      clean: true,
      // Regression guard, set just under what the suite reaches today. Raise them as gaps close.
      thresholds: {
        lines: 98,
        statements: 98,
        branches: 93,
        functions: 100,
      },
    },
  },
});
