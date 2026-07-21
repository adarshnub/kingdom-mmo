# City Builder Progression Plan

## Reference model

Use public 4X/castle-builder patterns as genre references, not as a clone target. Public Clash of Kings store listings describe a player starting from a small town, then growing through walls, resource production, armies, heroes, research, alliances, and real-time wars. Fan-maintained references also show the expected split between inner-city buildings such as castle, barracks, depot, walls, academy-style buildings, and field buildings such as farms, sawmills, mines, hospitals, and military tents.

Production must keep the existing originality rule: no copied art, names, layouts, balance tables, UI composition, audio, writing, or proprietary behavior.

## Current repository state

- New guest accounts already receive an authoritative city with level-1 `palace`, `farm_1`, and `barracks` records, starter resources, starter shield, and T1 infantry in `apps/server/src/auth/auth.service.ts`.
- `/v1/bootstrap` already returns player identity, city coordinates, resources, buildings, active queues, and active marches.
- `/v1/city/build/start` already validates ownership, one active build queue, food/wood costs, idempotency, resource ledger entries, and queue creation.
- The Unity city screen now renders the starting buildings on a city grid, supports client-side drag snapping, opens a building detail sheet, displays current stats, next-level stats, upgrade costs, queue state, and can start the authoritative upgrade command.

## Target city interaction

- First login shows only the low-level castle core plus level-1 starter economy and military buildings.
- The city view is a grid/plot system with empty, locked, buildable, and occupied slots.
- Tapping a building opens a sheet with level, role, current stats, next stats, requirements, resource costs, time, queue state, and primary actions.
- Dragging a building should snap to valid plots and eventually persist through a server-owned layout endpoint.
- Building, upgrading, training, research, healing, and item acceleration remain server-authoritative.

## Missing production work

1. Add server-owned city slot coordinates, unlock rules, and layout persistence.
2. Add content tables for building definitions, levels, prerequisites, effects, and buildability.
3. Add build-new-building, train-troops, collect-production, and speed-up endpoints.
4. Replace client preview formulas with server-provided upgrade quotes.
5. Add realtime queue countdown updates and completion refresh behavior.
6. Add mobile pan/zoom, occupancy validation, invalid-placement feedback, and richer original building art.
