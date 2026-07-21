# Cocos client architecture

## Project baseline

Use Cocos2d-x 3.x with C++ core modules and Lua presentation scripts. The Cocos
client is added beside the Unity reference client until it reaches parity.

The game remains original. Do not copy another game's protected art, names,
writing, UI composition, sounds, maps, balance tables, or proprietary behavior.

## Client modules

- App: lifecycle, resolution policy, Lua engine registration, and scene routing.
- Contracts: C++ DTOs and JSON parsers mirroring `packages/contracts`.
- Networking: authenticated HTTP, WebSocket realtime, idempotency keys, and error
  propagation.
- State: local session, auth tokens, bootstrap snapshot, and read-model cache.
- Screens: Cocos scene/layer rendering driven by Lua descriptors.
- Lua scripts: screen content, tutorial/live-ops presentation flow, and lightweight
  UI behavior.

## Scene flow

`Boot` opens `Login`. Login calls `/v1/auth/guest`, stores the access token in
session memory, then fetches `/v1/bootstrap`. `Main` connects realtime and routes
to `City`, `WorldMap`, and `BattleReport`.

City and World Map send only intent commands and render server responses. Battle
Report visualizes committed reports and must not calculate authoritative combat.

## Networking

- Local API: `http://localhost:3000`
- Local realtime: `ws://localhost:3001/v1/realtime`
- Mutating requests include a fresh `Idempotency-Key`.
- Realtime reconnect and sequence replay should follow the existing API contract
  before production mobile release.
- Tokens must move to platform-secure storage before shipping; the first desktop
  milestone keeps them in process memory.

## Performance

Pool map markers, march indicators, transient effects, and list rows. Use sprite
sheets and compressed textures for mobile assets. Keep Lua out of frame-critical
loops unless profiling proves it is safe on the Android reference device.
