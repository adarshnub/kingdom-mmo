import { readFile, readdir } from 'node:fs/promises';
import { resolve } from 'node:path';
import { Pool, type PoolClient } from 'pg';
import { getEnvironment } from '../config/environment.js';

const migrationLockKey = 4_901_004_001;

async function sqlFiles(directory: string): Promise<string[]> {
  return (await readdir(directory))
    .filter((file) => file.endsWith('.sql'))
    .sort((left, right) => left.localeCompare(right));
}

async function ensureMigrationTable(client: PoolClient): Promise<void> {
  await client.query(`
    CREATE TABLE IF NOT EXISTS schema_migrations (
      name text PRIMARY KEY,
      applied_at timestamptz NOT NULL DEFAULT clock_timestamp()
    )
  `);
}

export async function migrate(): Promise<void> {
  const pool = new Pool({ connectionString: getEnvironment().DATABASE_URL, max: 1 });
  const client = await pool.connect();
  const directory = resolve(import.meta.dirname, '../../migrations');
  try {
    await client.query('SELECT pg_advisory_lock($1)', [migrationLockKey]);
    await ensureMigrationTable(client);
    const applied = new Set(
      (await client.query<{ name: string }>('SELECT name FROM schema_migrations')).rows.map(
        (row) => row.name,
      ),
    );
    for (const file of await sqlFiles(directory)) {
      if (applied.has(file)) continue;
      const sql = await readFile(resolve(directory, file), 'utf8');
      await client.query('BEGIN');
      try {
        await client.query(sql);
        await client.query('INSERT INTO schema_migrations (name) VALUES ($1)', [file]);
        await client.query('COMMIT');
        console.log(`Applied migration ${file}`);
      } catch (error) {
        await client.query('ROLLBACK');
        throw error;
      }
    }
  } finally {
    await client.query('SELECT pg_advisory_unlock($1)', [migrationLockKey]).catch(() => undefined);
    client.release();
    await pool.end();
  }
}

export async function seed(): Promise<void> {
  const pool = new Pool({ connectionString: getEnvironment().DATABASE_URL, max: 1 });
  const directory = resolve(import.meta.dirname, '../../seeds');
  try {
    for (const file of await sqlFiles(directory)) {
      await pool.query(await readFile(resolve(directory, file), 'utf8'));
      console.log(`Applied seed ${file}`);
    }
  } finally {
    await pool.end();
  }
}
