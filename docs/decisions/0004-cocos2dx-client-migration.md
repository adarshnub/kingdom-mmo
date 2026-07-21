# 0004: Cocos2d-x client migration

## Status

Accepted

## Context

The repository started with a Unity client and a server-authoritative TypeScript
backend. The client technology is being migrated toward a Cocos2d-x stack that is
closer to older mobile 4X production patterns while preserving backend authority.

## Decision

Add a Cocos2d-x 3.x client under `cocos-client/` using C++ for core runtime
systems and Lua for presentation scripts. Keep the Unity client as a temporary
reference until Cocos reaches feature parity.

The backend, OpenAPI contracts, database, worker, realtime gateway, and admin web
app remain unchanged.

## Consequences

- Client parity work can proceed without destabilizing the server.
- C++ DTOs must stay aligned with `packages/contracts`.
- Windows desktop is the first local smoke target; Android follows after the
  toolchain is available.
- The project keeps its originality requirement and does not clone protected
  expression from another title.
