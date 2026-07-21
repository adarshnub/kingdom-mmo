# Kingdom 1 product blueprint

## Purpose

Build an original persistent mobile 4X strategy MMO with one launch kingdom. Players develop one city, gather resources, train armies, join alliances, coordinate marches and rallies, fight monsters and other players, and contest a weekly throne event.

This document was derived from the supplied “Single Kingdom 4X MMO Blueprint Inspired by Publicly Observable Clash of Kings Systems.” Broken internal citation tokens and encoding artifacts were removed. All numbers below are design assumptions to validate through playtesting and telemetry.

## Originality requirement

The game may use established 4X genre patterns, but it must not copy another title’s protected art, names, characters, writing, sounds, UI composition, maps, balance tables, or proprietary implementation. Every production asset and content table must be original or licensed for this project.

## Launch audience and capacity

- iOS and Android players in India and Southeast Asia.
- English at launch, with every string externalized for Hindi and later locales.
- 5,000–20,000 registered players in Kingdom 1.
- 300–700 typical concurrent players and 1,000 during throne events.
- Mid-range devices run at 30 FPS; reference high-end devices target 60 FPS.

## Launch systems

| Area        | Kingdom 1 launch capability                                                   |
| ----------- | ----------------------------------------------------------------------------- |
| City        | One city, 16–20 building types, queues, resources, warehouse, wall, hospital  |
| Progression | Economy/military/defense/monster research, VIP convenience, quests            |
| Army        | Infantry, cavalry, archers, siege, tiers T1–T8, heroes and equipment          |
| World       | Streamed 1,200×1,200 map, cities, nodes, monsters, throne structures          |
| Marches     | Scout, attack, gather, reinforce, occupy, rally join, return                  |
| Alliance    | Membership, five roles, help, technology, gifts, chat, rallies                |
| Events      | Daily missions, gathering race, kill event, monster event, throne war         |
| Social      | Kingdom/alliance/private chat, mail, reports, moderation, blocking            |
| Commerce    | Starter packs, speedups, resource packs, monthly card, verified server-side   |
| Operations  | GM console, compensation, feature flags, live event configuration, audit logs |

## Core loop

1. Produce or gather resources.
2. Spend resources on buildings, research, troops, healing, and equipment.
3. Use increased power to access stronger map content and alliance warfare.
4. Complete missions and events for items and convenience boosts.
5. Coordinate alliance activity toward the weekly throne contest.

## Server authority

The client sends intent, never outcomes. It may request an upgrade, march, item use, or purchase claim. The server validates ownership, costs, timing, capacity, protection rules, and idempotency before committing a result.

## Deferred scope

Additional kingdoms, cross-kingdom warfare, instanced guild battles, PC/web clients, and multiple cities are not launch features. All kingdom-scoped records still carry `kingdom_id` so later expansion does not require a schema rewrite.

## Public references

- Apple App Review Guidelines: https://developer.apple.com/app-store/review/guidelines/
- Google Play Intellectual Property policy: https://support.google.com/googleplay/android-developer/answer/9888072
- Unity release support: https://unity.com/releases/unity-6/support
- Unity Addressables runtime content guidance: https://docs.unity3d.com/current/Manual/assets-managing-introduction.html
