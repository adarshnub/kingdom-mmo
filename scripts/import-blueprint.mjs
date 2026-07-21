import { mkdir, readFile, writeFile } from 'node:fs/promises';
import { dirname, resolve } from 'node:path';

const sourcePath = process.argv.slice(2).find((argument) => argument !== '--');
if (!sourcePath) {
  throw new Error('Usage: pnpm docs:import-blueprint -- <path-to-blueprint.txt>');
}

const outputPath = resolve('docs/reference/supplied-blueprint.md');
const source = await readFile(resolve(sourcePath), 'utf8');
const cleaned = source
  .replace(/\s*cite.*?/gu, '')
  .replaceAll('\r\n', '\n')
  .trim();
const notice = `> Imported from the user-supplied blueprint for traceability. Broken internal citation tokens were removed. All figures, formulas, schedules, and balance tables are design assumptions; the maintained documents under \`docs/product\` and \`docs/engineering\` are authoritative.\n\n`;

await mkdir(dirname(outputPath), { recursive: true });
await writeFile(outputPath, `${notice}${cleaned}\n`, 'utf8');
console.log(`Imported ${cleaned.split('\n').length} blueprint lines to ${outputPath}.`);
