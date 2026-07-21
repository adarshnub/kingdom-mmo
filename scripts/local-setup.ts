import { copyFile, access } from 'node:fs/promises';
import { connect } from 'node:net';
import { spawnSync } from 'node:child_process';
import process from 'node:process';

const minimumNodeMajor = 24;

function run(command: string, args: string[]): void {
  const result = spawnSync(command, args, {
    stdio: 'inherit',
    shell: process.platform === 'win32',
  });

  if (result.status !== 0) {
    throw new Error(
      `${command} ${args.join(' ')} failed with exit code ${result.status ?? 'unknown'}`,
    );
  }
}

async function fileExists(path: string): Promise<boolean> {
  try {
    await access(path);
    return true;
  } catch {
    return false;
  }
}

async function waitForPort(host: string, port: number, timeoutMs: number): Promise<void> {
  const deadline = Date.now() + timeoutMs;

  while (Date.now() < deadline) {
    const ready = await new Promise<boolean>((resolve) => {
      const socket = connect({ host, port });
      socket.once('connect', () => {
        socket.destroy();
        resolve(true);
      });
      socket.once('error', () => resolve(false));
      socket.setTimeout(1_000, () => {
        socket.destroy();
        resolve(false);
      });
    });

    if (ready) return;
    await new Promise((resolve) => setTimeout(resolve, 1_000));
  }

  throw new Error(`Timed out waiting for ${host}:${port}`);
}

async function main(): Promise<void> {
  const nodeMajor = Number.parseInt(process.versions.node.split('.')[0] ?? '0', 10);
  if (nodeMajor < minimumNodeMajor) {
    throw new Error(
      `Node.js ${minimumNodeMajor} LTS is required; found ${process.versions.node}. Use .nvmrc or .node-version.`,
    );
  }

  if (!(await fileExists('.env'))) {
    await copyFile('.env.example', '.env');
    console.log('Created .env from .env.example');
  }

  run('docker', ['compose', 'up', '-d', 'postgres', 'redis', 'minio', 'minio-init']);
  await Promise.all([
    waitForPort('127.0.0.1', 5432, 60_000),
    waitForPort('127.0.0.1', 6379, 60_000),
    waitForPort('127.0.0.1', 9000, 60_000),
  ]);

  run('pnpm', ['db:migrate']);
  run('pnpm', ['db:seed']);
  console.log('Kingdom 1 local environment is ready. Run pnpm dev.');
}

main().catch((error: unknown) => {
  console.error(error instanceof Error ? error.message : error);
  process.exitCode = 1;
});
