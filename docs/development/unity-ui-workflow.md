# Unity UI Development Workflow

The Unity client is an existing project under `game-client`; do not create a new project from a Hub template. The repository is pinned to Unity `6000.3.7f1` (Unity 6.3 LTS).

## Open the project manually

1. Start Docker Desktop, then run `pnpm dev` from the repository root when client/server integration is needed.
2. In Unity Hub, choose **Projects > Add > Add project from disk**.
3. Select the `game-client` directory, not the repository root.
4. Open it with Unity `6000.3.7f1` and wait for package import and script compilation to finish.
5. Open **Window > General > Console** and resolve red compilation errors before generating content.
6. Run **Kingdom > Project Setup > Create Core Scenes**.
7. Run **Kingdom > Project Setup > Configure Portrait Mobile Settings**.
8. Run **Kingdom > Project Setup > Configure Background Textures**.
9. Run **Kingdom > Project Setup > Validate Project**.

Validation expects the seven core scenes in build order, the original background assets, and `Assets/_Game/Config/GameConfig.asset`. UI generation should create the config asset and assign it to the Boot scene's `Bootstrapper`.

## Automated scene generation and setup

Close the Unity Editor before using batch mode. In PowerShell from the repository root:

```powershell
$unityEditor = 'C:\Program Files\Unity\Hub\Editor\6000.3.7f1\Editor\Unity.exe'
$projectPath = (Resolve-Path '.\game-client').Path
$artifactsPath = (New-Item -ItemType Directory -Force '.\artifacts').FullName

$generateScenes = Start-Process -FilePath $unityEditor -WindowStyle Hidden -Wait -PassThru -ArgumentList @(
  '-batchmode',
  '-nographics',
  '-quit',
  '-projectPath', $projectPath,
  '-executeMethod', 'Kingdom.Editor.CreateCoreScenes.Create',
  '-logFile', (Join-Path $artifactsPath 'unity-generate-scenes.log')
)
if ($generateScenes.ExitCode -ne 0) {
  throw "Unity scene generation failed with exit code $($generateScenes.ExitCode)."
}

$configureProject = Start-Process -FilePath $unityEditor -WindowStyle Hidden -Wait -PassThru -ArgumentList @(
  '-batchmode',
  '-nographics',
  '-quit',
  '-projectPath', $projectPath,
  '-executeMethod', 'Kingdom.Editor.KingdomProjectSetup.ConfigureAll',
  '-logFile', (Join-Path $artifactsPath 'unity-project-setup.log')
)
if ($configureProject.ExitCode -ne 0) {
  throw "Unity project setup failed with exit code $($configureProject.ExitCode)."
}
```

The setup command is idempotent. It enforces portrait orientation, imports background art as single non-mipmapped sprites, applies Android ASTC 6x6 compression, and validates required project content. A non-zero Unity exit code or `Kingdom project validation failed` in the log means generation is incomplete.

If the Unity installation is elsewhere, replace `$unityEditor` with the path shown in Unity Hub under **Installs**. Create the `artifacts` directory before running these commands if it does not already exist.

## Run EditMode tests in batch mode

```powershell
$unityEditor = 'C:\Program Files\Unity\Hub\Editor\6000.3.7f1\Editor\Unity.exe'
$projectPath = (Resolve-Path '.\game-client').Path
$artifactsPath = (New-Item -ItemType Directory -Force '.\artifacts').FullName

$testRun = Start-Process -FilePath $unityEditor -WindowStyle Hidden -Wait -PassThru -ArgumentList @(
  '-batchmode',
  '-nographics',
  '-projectPath', $projectPath,
  '-runTests',
  '-testPlatform', 'EditMode',
  '-testResults', (Join-Path $artifactsPath 'unity-editmode-results.xml'),
  '-logFile', (Join-Path $artifactsPath 'unity-editmode.log')
)
if ($testRun.ExitCode -ne 0) {
  throw "Unity EditMode tests failed with exit code $($testRun.ExitCode)."
}
```

The first tests cover server-clock synchronization, local UTC behavior, API base-URL normalization, and configuration timeout limits. Add EditMode tests for pure state and view-model logic; use PlayMode tests for scene navigation, animation, input, and lifecycle behavior.

## Run PlayMode UI smoke tests

```powershell
$unityEditor = 'C:\Program Files\Unity\Hub\Editor\6000.3.7f1\Editor\Unity.exe'
$projectPath = (Resolve-Path '.\game-client').Path
$artifactsPath = (New-Item -ItemType Directory -Force '.\artifacts').FullName

$testRun = Start-Process -FilePath $unityEditor -WindowStyle Hidden -Wait -PassThru -ArgumentList @(
  '-batchmode',
  '-projectPath', $projectPath,
  '-runTests',
  '-testPlatform', 'PlayMode',
  '-testResults', (Join-Path $artifactsPath 'unity-playmode-results.xml'),
  '-logFile', (Join-Path $artifactsPath 'unity-playmode.log')
)
if ($testRun.ExitCode -ne 0) {
  throw "Unity PlayMode tests failed with exit code $($testRun.ExitCode)."
}
```

The smoke suite instantiates login, the navigation shell, all prototype destinations, loading, and battle-report views. Device testing is still required for safe-area behavior, touch feel, memory, and real Android networking.

## Safe iteration rules

- Keep authoritative game state and balance on the server; Unity sends player intent and renders server results.
- Put display-only constants and local environment URLs in ScriptableObjects.
- Use TextMeshPro for text, Addressables for remotely delivered content, and assembly definitions for each feature boundary.
- Do not edit generated scene YAML by hand. Change editor-generation code and regenerate the scenes.
- Do not store access or refresh tokens in `PlayerPrefs`; use platform-secure storage when persistent sessions are added.
- Test the UI at multiple portrait aspect ratios and keep interactive controls inside the device safe area.
