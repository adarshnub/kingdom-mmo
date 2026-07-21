# Kingdom MMO documentation

Start with the product blueprint, then read the system document for the area you plan to change.

## Product

- [Kingdom 1 blueprint](product/kingdom-1-blueprint.md): launch vision, original feature set, boundaries, and assumptions.
- [City builder progression plan](product/city-builder-progression-plan.md): castle-grid interaction target and implementation gaps.
- [Delivery roadmap](product/roadmap.md): staged 48-week delivery and exit criteria.

## Engineering

- [Architecture](engineering/architecture.md): deployables, data flow, authority, and module boundaries.
- [API contracts](engineering/api-contracts.md): REST, realtime, common types, errors, and idempotency.
- [Gameplay rules](engineering/gameplay-rules.md): deterministic combat, timers, economy, and invariants.
- [Unity client](engineering/unity-client.md): scenes, assemblies, networking, content, and performance.

## Development and operations

- [Local development](development/local-setup.md): prerequisites, commands, services, and troubleshooting.
- [AWS operations](operations/aws-deployment.md): environments, Terraform, deployment, backup, and rollback.
- [Testing and release](operations/testing-release.md): automated gates, load targets, and launch checklist.

## Architecture decisions

Architecture decision records live in [`decisions/`](decisions/README.md). Update an ADR when a foundational choice changes.

## Supplied reference

- [Original supplied blueprint, cleaned](reference/supplied-blueprint.md): the complete input document retained for traceability. Product and engineering documents above remain authoritative when wording differs.
