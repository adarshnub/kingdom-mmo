# Contributing

## Workflow

1. Read the relevant document in `docs/` and update the contract before changing public behavior.
2. Use a short-lived branch named `feature/<topic>`, `fix/<topic>`, or `docs/<topic>`.
3. Keep controllers thin and put authoritative rules in domain or application services.
4. Add a migration for every database change. Never edit a migration that has reached a shared environment.
5. Add tests for the happy path, invalid input, authorization, idempotent retry, and concurrency when applicable.
6. Run `pnpm verify` and document operational impact in the pull request.

## Definition of done

A change is done when code, tests, contracts, migrations, telemetry, documentation, and rollback implications agree. Client code sends intent only; no client-calculated reward, cost, timer, or battle outcome is authoritative.

## Review ownership

- Gameplay/economy changes require game-design and backend review.
- Public contracts require backend and Unity review.
- Terraform, secrets, IAM, and production migrations require infrastructure review.
- Store, privacy, moderation, or player-data changes require product and compliance review.
