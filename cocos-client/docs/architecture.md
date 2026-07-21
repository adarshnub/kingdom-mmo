# Cocos client architecture

## Purpose

The Cocos client replaces the Unity client surface while preserving the existing
server-authoritative backend and contract model. It intentionally avoids copying
another game's protected assets, UI composition, writing, names, or balance.

## Runtime layers

- C++ core: app lifecycle, scene routing, HTTP, WebSocket, persistence, DTOs,
  platform bridges, and performance-critical rendering systems.
- Lua presentation: screen descriptors, event UI, tutorial flow, and live-ops
  presentation that can change without touching core client code.
- Existing backend: authoritative validation for resources, queues, map actions,
  marches, combat, and realtime events.

## Scene flow

`Boot` opens `Login`. Login calls guest auth and then bootstrap. The `Main` shell
connects realtime and routes to City, World Map, and Battle Report screens. City
and World Map currently implement the same smoke endpoints as the Unity vertical
slice.

## Networking

- HTTP base URL: `http://localhost:3000`
- Realtime URL: `ws://localhost:3001/v1/realtime?access_token=<token>`
- JSON remains `snake_case` on the wire.
- Mutating commands use a fresh `Idempotency-Key` UUID.
- Realtime payloads are parsed as envelopes and then reduced into local read
  models once feature-specific event handlers exist.

## Content and performance

Use sprite sheets, compressed textures, pooled map markers, pooled list rows, and
explicit scene-owned lifetime. Do not add allocation-heavy work to frame updates.
Lua is for presentation flow and live UI behavior, not authoritative simulation.
