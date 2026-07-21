# Kingdom Cocos Client

This is the Cocos2d-x migration client for Kingdom MMO. It sits beside the Unity
client until it reaches parity.

## Runtime target

- Cocos2d-x 3.x
- C++ for lifecycle, networking, state, persistence, and platform glue
- Lua for screen descriptors, live-ops UI flow, and tutorial/presentation script
- Windows desktop first, Android second

The client remains server-authoritative: it sends player intent to the existing
TypeScript backend and renders committed state from HTTP and realtime messages.

## Local prerequisites

The current repository does not vendor Cocos2d-x. Install these locally and point
the project at them:

- Visual Studio 2022 Build Tools with Desktop C++
- CMake
- Cocos2d-x 3.x
- Python 2.7 only if your legacy Cocos CLI requires it

Set `COCOS2DX_ROOT` to the Cocos2d-x checkout root before configuring:

```powershell
$env:COCOS2DX_ROOT="C:\dev\cocos2d-x-3.17.2"
cmake -S cocos-client -B cocos-client/build/win32-x86 -G "Visual Studio 17 2022" -A Win32
cmake --build cocos-client/build/win32-x86 --config Debug
```

Start the backend separately with the existing workflow:

```powershell
pnpm setup:local
pnpm dev
```

## Windows development launch

From the repository root, use PowerShell:

```powershell
.\cocos-client\scripts\run-dev.ps1 -Restart
```

The script locates Visual Studio's bundled CMake when `cmake` is not on your
shell PATH, builds the Debug target, and starts the executable from its DLL and
Resources directory. Use `-NoBuild` to start the last successful build.

## Implemented migration surface

- Boot, login, main shell, city, world map, and battle report scene descriptors
- C++ app delegate and scene router
- C++ contract DTOs mirroring the current OpenAPI vertical slice
- HTTP client wrapper for guest auth, bootstrap, start build, viewport, and march dispatch
- WebSocket wrapper for `/v1/realtime?access_token=...`

All art, UI copy, names, and balance remain original to this project.
