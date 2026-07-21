# Kingdom MMO

Kingdom MMO is an original, server-authoritative mobile 4X game for iOS and Android. This repository contains the Node.js/TypeScript server, Unity reference client, Cocos2d-x migration client, local infrastructure, contracts, documentation, and AWS deployment code.

The current implementation establishes the production foundation and the first server vertical slice: guest authentication, bootstrap snapshots, building queues, map viewports, march dispatch, deterministic combat, authoritative workers, idempotency, and realtime envelopes.

## Start locally

Prerequisites:

- Node.js 24 LTS with Corepack
- Docker with Compose v2
- Unity 6.3 LTS for reference client work
- Optional Cocos2d-x 3.x, CMake, and Visual Studio C++ tools for Cocos client work

```bash
corepack enable
corepack prepare pnpm@10.14.0 --activate
pnpm install
pnpm setup:local
pnpm dev
```

The API listens on `http://localhost:3000`, realtime WebSockets on `ws://localhost:3001/v1/realtime`, MinIO on `http://localhost:9001`, and optional observability tools can be started with `pnpm local:observability`.

Read [the documentation index](docs/README.md) before changing gameplay or architecture.

## Repository layout

```text
apps/server/             NestJS API, realtime, worker, migrations, and seeds
packages/contracts/      Shared HTTP and realtime contracts
packages/domain/         Deterministic domain rules
cocos-client/            Cocos2d-x C++/Lua migration client
game-client/             Unity 6.3 LTS reference client project
infra/                   Docker, observability, and Terraform
docs/                    Product, architecture, development, and operations docs
scripts/                 Cross-platform developer automation
```

## Quality gate

Run `pnpm verify` before opening a pull request. Integration tests require Docker.
