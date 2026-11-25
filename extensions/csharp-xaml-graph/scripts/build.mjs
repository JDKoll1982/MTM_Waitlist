import * as esbuild from 'esbuild';
import { copyFileSync, mkdirSync, existsSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const here = dirname(fileURLToPath(import.meta.url));
const root = join(here, '..');
const watch = process.argv.includes('--watch');

/** @type {import('esbuild').BuildOptions} */
const extension = {
  entryPoints: [join(root, 'src/extension.ts')],
  outfile: join(root, 'out/extension.js'),
  bundle: true,
  platform: 'node',
  format: 'cjs',
  target: 'node20',
  sourcemap: true,
  // web-tree-sitter loads its own .wasm next to its package directory, so it must stay on disk
  // rather than be inlined into the bundle.
  external: ['vscode', 'web-tree-sitter'],
  logLevel: 'info',
};

/** @type {import('esbuild').BuildOptions} */
const selftest = {
  entryPoints: [join(root, 'src/selftest.ts')],
  outfile: join(root, 'out/selftest.js'),
  bundle: true,
  platform: 'node',
  format: 'cjs',
  target: 'node20',
  sourcemap: true,
  external: ['web-tree-sitter'],
  logLevel: 'info',
};

/** @type {import('esbuild').BuildOptions} */
const webview = {
  entryPoints: [join(root, 'src/webview/client/main.ts')],
  outfile: join(root, 'out/webview/main.js'),
  bundle: true,
  platform: 'browser',
  format: 'iife',
  target: 'es2022',
  sourcemap: true,
  logLevel: 'info',
};

function copyAssets() {
  mkdirSync(join(root, 'out/webview'), { recursive: true });
  mkdirSync(join(root, 'media'), { recursive: true });
  copyFileSync(join(root, 'src/webview/client/style.css'), join(root, 'out/webview/style.css'));

  const grammar = join(root, 'node_modules/tree-sitter-c-sharp/tree-sitter-c_sharp.wasm');
  if (!existsSync(grammar)) {
    throw new Error(`C# grammar not found at ${grammar}. Run npm install first.`);
  }
  copyFileSync(grammar, join(root, 'media/tree-sitter-c_sharp.wasm'));
}

copyAssets();

if (watch) {
  for (const options of [extension, selftest, webview]) {
    const ctx = await esbuild.context(options);
    await ctx.watch();
  }
  console.log('watching...');
} else {
  await Promise.all([esbuild.build(extension), esbuild.build(selftest), esbuild.build(webview)]);
}
