# Authoritative gameplay rules

## Numeric model

Gameplay uses integers and fixed-point basis points. Do not use JavaScript floating-point operations for a persisted economy or combat result. Clamp intermediate values to documented bounds and serialize 64-bit quantities as decimal strings.

## Building queues

1. Lock the city and target building.
2. Reject if the queue is occupied, prerequisites fail, or resources are insufficient.
3. Deduct resource costs and append one economy-ledger record per resource.
4. Create a queue job with server-derived `start_at` and `finish_at`.
5. Store the idempotent response and an outbox event in the same transaction.
6. A worker completes due work and increments the building exactly once.

Initial vertical-slice duration uses `60 seconds × target level²`. Initial food and wood cost uses `1,000 × target level²`. These are test values, not final balance.

## Marches

The server validates ownership, available troops, march slots, target legality, protection rules, and coordinate bounds. Distance uses integer grid coordinates. Initial travel duration is `max(1 second, Manhattan distance × 1 second)` for deterministic tests; final design replaces it with fixed-point Euclidean distance and troop-speed modifiers.

March states are `outbound`, `resolving`, `returning`, `completed`, or `cancelled`. A worker owns every state transition. Troops cannot appear in two marches because dispatch locks and deducts their stationed stack in one transaction.

## Combat

The vertical slice uses a deterministic seeded linear-congruential generator and integer attack/defense values. Production combat evolves through versioned rules; existing reports keep the rule version and seed so they remain replayable.

Required invariants:

- Casualties never exceed committed troops.
- Resource and troop quantities never become negative.
- The same rule version, seed, and inputs yield byte-equivalent results.
- A battle report and all casualty/loot ledger changes commit atomically.
- Clients never provide effective stats, randomness, casualties, rewards, or victory state.

## Economy ledger

Every grant and spend records player, resource/item key, signed delta, reason, reference type/id, command correlation, and creation time. Corrections use compensating entries; production ledger rows are not edited or deleted.
