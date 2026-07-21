import { Pool } from 'pg';
import { getEnvironment } from '../config/environment.js';
import { migrate, seed } from './sql-runner.js';

const databaseUrl = new URL(getEnvironment().DATABASE_URL);
const databaseName = databaseUrl.pathname.slice(1);
const localHosts = new Set(['localhost', '127.0.0.1', '::1']);

if (!localHosts.has(databaseUrl.hostname) || !databaseName.endsWith('_local')) {
  throw new Error(
    `Refusing to reset database ${databaseName} at ${databaseUrl.hostname}; only localhost *_local databases are allowed.`,
  );
}

const pool = new Pool({ connectionString: databaseUrl.toString(), max: 1 });
try {
  await pool.query('DROP SCHEMA public CASCADE');
  await pool.query('CREATE SCHEMA public');
  await pool.query('GRANT ALL ON SCHEMA public TO public');
} finally {
  await pool.end();
}

await migrate();
await seed();
