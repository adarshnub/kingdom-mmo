# Local development

## Prerequisites

- Node.js 24 LTS
- Corepack and pnpm 10.14
- Docker Engine/Desktop with Compose v2
- Unity Hub and Unity 6.3 LTS for client development
- Optional: Terraform 1.9+ for infrastructure validation

## First run

```bash
corepack enable
corepack prepare pnpm@10.14.0 --activate
pnpm install
pnpm setup:local
pnpm dev
```

`setup:local` checks configuration, starts PostgreSQL/Redis/MinIO, waits for health checks, applies migrations, and seeds Kingdom 1. Re-running it is safe.

## Services

| Service           | Address                         | Credentials                                 |
| ----------------- | ------------------------------- | ------------------------------------------- |
| API               | http://localhost:3000           | Guest token from `/v1/auth/guest`           |
| Realtime          | ws://localhost:3001/v1/realtime | Bearer query token during local development |
| PostgreSQL        | localhost:5432                  | From `.env`                                 |
| Redis             | localhost:6379                  | Local network only                          |
| MinIO API/console | localhost:9000 / localhost:9001 | From `.env`                                 |
| Grafana           | localhost:3030                  | `admin` / `kingdom-local-only`              |
| pgAdmin           | localhost:5050                  | Tools profile only                          |

Start optional stacks with `pnpm local:observability` and `pnpm local:tools`.

## Safe database reset

`pnpm db:reset` refuses non-local hosts and databases not ending in `_local`. It drops application schemas, reruns migrations, and reseeds deterministic fixtures. Never use reset as a shared-environment migration strategy.

## Troubleshooting

- If ports conflict, change host-side ports in `.env` and Compose together.
- If a migration lock remains after an interrupted process, confirm no migration task is active before releasing it.
- If Node is older than 24, the setup script exits with the supported version instead of installing runtimes automatically.
- If Redis is cleared, restart the realtime gateway and worker; PostgreSQL reconstructs authoritative work.
