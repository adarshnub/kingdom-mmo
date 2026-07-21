# ADR-0002: PostgreSQL authority

Status: Accepted

PostgreSQL is the source of truth for all player state, timers, purchases, event progress, and economy changes. Redis holds rebuildable cache, locks, rate limits, and fan-out state only. Workers recover due work from PostgreSQL after any restart or Redis loss.
