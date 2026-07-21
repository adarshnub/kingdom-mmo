# API and realtime contracts

## Common rules

- Base HTTP path: `/v1`.
- JSON uses `snake_case` on the wire.
- IDs are UUID strings; new server IDs use UUIDv7.
- Timestamps are RFC 3339 UTC strings.
- Durations are integer milliseconds.
- Percentages and multipliers use integer basis points where 10,000 equals 100%.
- `bigint` database values are decimal strings on the wire.
- Mutating requests require `Authorization: Bearer <token>` and `Idempotency-Key: <UUID>` except guest bootstrap.

Success responses use `{ "data": ..., "meta": ... }`. Errors use `application/problem+json` with `type`, `title`, `status`, `code`, `detail`, `instance`, and `correlation_id`.

## Implemented vertical-slice endpoints

| Method | Path                     | Purpose                                             |
| ------ | ------------------------ | --------------------------------------------------- |
| `GET`  | `/health/live`           | Process liveness                                    |
| `GET`  | `/health/ready`          | PostgreSQL and dependency readiness                 |
| `POST` | `/v1/auth/guest`         | Create or restore a local guest account             |
| `GET`  | `/v1/bootstrap`          | Get authoritative player, city, queues, and marches |
| `POST` | `/v1/city/build/start`   | Start a building upgrade                            |
| `GET`  | `/v1/map/viewport`       | Read map objects within validated bounds            |
| `POST` | `/v1/map/march/dispatch` | Dispatch a march to a map object                    |

Later modules extend the same surface for research, troops, items, alliances, mail, reports, events, leaderboards, and commerce.

## Realtime envelope

```json
{
  "event_id": "0198a7c9-5e62-7f54-a22d-c40f2f82ed48",
  "type": "city.queue.completed.v1",
  "sequence": "1842",
  "occurred_at": "2026-07-21T08:30:00.000Z",
  "payload": {
    "queue_id": "0198a7c8-dbc8-7cb8-b9a7-f6cfa337668f"
  }
}
```

Clients acknowledge the latest sequence. If replay retention no longer includes that sequence, the server sends `system.resync_required.v1` and the client calls `/v1/bootstrap`.

## Compatibility

- Add optional fields without renaming or changing semantics.
- Introduce a new event type or endpoint version for breaking behavior.
- Keep generated C# code read-only and regenerate it in CI.
- Contract checks fail when OpenAPI or event schemas change without committed generated artifacts and documentation.
