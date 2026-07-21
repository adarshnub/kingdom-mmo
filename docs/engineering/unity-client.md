# Unity client architecture

## Project baseline

Use Unity 6.3 LTS with URP, Addressables, Input System, Localization, TextMeshPro, and Unity Test Framework. Keep runtime code behind assembly definitions to reduce compile time and enforce boundaries.

## Assemblies

- `Kingdom.Core`: boot lifecycle, configuration, clocks, logging, and reusable utilities.
- `Kingdom.Contracts`: generated HTTP DTOs and maintained realtime envelopes.
- `Kingdom.Networking`: authenticated HTTP, WebSocket reconnect, sequence tracking, and retry policy.
- `Kingdom.Domain`: local read models and state reducers; never authoritative simulation.
- `Kingdom.Presentation`: view models and navigation state.
- `Kingdom.UI`: screens, reusable components, popups, localization, and accessibility.
- `Kingdom.Tests`: EditMode and PlayMode tests.

## Scene flow

`Boot` initializes services, loads configuration, checks client compatibility, restores a session, and opens `Login` or `Main`. City and World Map load additively beneath the Main shell. Battle Replay visualizes a server report and never calculates a new outcome.

## Networking

- HTTP commands use a fresh UUID idempotency key retained until a terminal response.
- Retry only network failures and documented retryable status codes.
- Realtime reconnect uses exponential backoff with jitter and the last acknowledged sequence.
- `system.resync_required.v1` clears transient projections and fetches `/v1/bootstrap`.
- Tokens stay in platform-secure storage, never PlayerPrefs.

## Assets and performance

Use direct references for boot-critical assets and Addressables for map objects, event art, heroes, audio, and seasonal content. Pool marches, markers, effects, and list rows. Never use scene searches, component lookup, allocation-heavy LINQ, or string concatenation in `Update`.

Performance budgets:

- 30 FPS minimum on a selected 4 GB Android reference device; 60 FPS target on high-end devices.
- No increasing managed memory after ten city/map transitions.
- Map viewport rendering and asset loading must not block the main thread.
- Remote content failure shows a retryable error and retains the last valid catalog.
