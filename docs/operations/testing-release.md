# Testing and release gates

## Automated tests

- Unit/property: combat, resources, queue timing, loot, protection, teleports, and invariants.
- Integration: PostgreSQL transactions, Redis loss, idempotency, worker leases, outbox, and migrations.
- Contract: OpenAPI/event schema compatibility and generated Unity client compilation.
- Unity: state reducers, navigation, reconnect/resync, content failures, and memory-sensitive flows.
- Commerce: receipt replay, pending purchases, refunds, revocations, and exactly-once ledger entries.
- Security: dependency, secret, SAST, image, authorization, rate-limit, and staging DAST checks.

## Capacity gates

Before soft launch, pass a 24-hour soak and a throne-event load scenario with 1,000 connections, 50 viewport requests/second, 100 chat-message/second bursts, and 30 battle resolutions/second. Required SLOs are API p95 under 250 ms, viewport p95 under 400 ms, and due-job lag under five seconds.

## Release checklist

- No unresolved critical security, purchase, data-loss, or economy defects.
- Store sandbox purchase, refund, and account-recovery paths pass.
- Privacy export/delete and moderation workflows pass review.
- Android/iOS reference-device smoke tests and crash symbol upload pass.
- Database restore, worker recovery, Redis-loss, and deployment rollback drills pass.
- On-call owner, dashboards, alerts, incident channel, runbooks, and player support are active.
