import { cp, mkdir } from 'node:fs/promises';
import { resolve } from 'node:path';

const packageRoot = process.cwd();
const outputRoot = resolve(packageRoot, 'dist');
await mkdir(outputRoot, { recursive: true });
await Promise.all([
  cp(resolve(packageRoot, 'migrations'), resolve(outputRoot, 'migrations'), { recursive: true }),
  cp(resolve(packageRoot, 'seeds'), resolve(outputRoot, 'seeds'), { recursive: true }),
]);
