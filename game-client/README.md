# Kingdom Unity client

Open this directory with Unity `6000.3.7f1` (Unity 6.3 LTS). The generated scenes and local configuration are checked in; use **Kingdom > Project Setup > Create Core Scenes** only when regenerating them after editor-code changes.

## Run the current vertical slice

1. From the repository root, start the local backend with `pnpm dev`.
2. Open `Assets/_Game/Scenes/Boot.unity`.
3. Press Play.
4. Enter a ruler name and select **Begin Journey**.

Guest authentication and `/v1/bootstrap` are live. The server supplies player identity, city coordinates, resources, buildings, queues, and marches. The Unity prototype then exposes City, World, Alliance, Mail, Hero, Inventory, Training, Territory, Loading, and Battle Report views.

The City view renders the starter castle grid from authoritative building records. A new account begins with a level-1 castle keep, farm, and barracks; tapping a building opens stats, next-level preview, costs, queue state, and an upgrade action backed by `/v1/city/build/start`. Drag snapping is currently client-side only until a server layout endpoint is added.

The interface uses original project artwork and a reusable portrait-safe uGUI/TextMeshPro foundation. It is intentionally client-intent-only: UI and art never calculate authoritative economy, combat, entitlement, or timer outcomes.

See `docs/development/unity-ui-workflow.md` for generation, validation, and batch-test commands.
