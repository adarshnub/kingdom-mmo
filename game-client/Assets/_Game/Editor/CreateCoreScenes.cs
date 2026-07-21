using System.IO;
using System.Linq;
using Kingdom.Boot;
using Kingdom.Core;
using Kingdom.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Kingdom.Editor
{
    public static class CreateCoreScenes
    {
        private const string SceneDirectory = "Assets/_Game/Scenes";
        private const string ConfigDirectory = "Assets/_Game/Config";
        private const string GameConfigPath = ConfigDirectory + "/GameConfig.asset";
        private const string UiThemePath = ConfigDirectory + "/UiTheme.asset";
        private const string CityArtPath = "Assets/_Game/Art/Backgrounds/capital-city-v2.png";
        private const string WorldArtPath = "Assets/_Game/Art/Backgrounds/world-frontier.png";
        private const string HeroArtPath = "Assets/_Game/Art/Backgrounds/frontier-marshal.png";
        private const string CityIconPath = "Assets/_Game/Art/UI/Navigation/nav-city.png";
        private const string HeroIconPath = "Assets/_Game/Art/UI/Navigation/nav-hero.png";
        private const string InventoryIconPath = "Assets/_Game/Art/UI/Navigation/nav-inventory.png";
        private const string MailIconPath = "Assets/_Game/Art/UI/Navigation/nav-mail.png";
        private const string AllianceIconPath = "Assets/_Game/Art/UI/Navigation/nav-alliance.png";

        private static readonly string[] SceneNames =
        {
            "Boot", "Login", "Main", "City", "WorldMap", "BattleReplay", "Loading"
        };

        [MenuItem("Kingdom/Project Setup/Create Core Scenes")]
        public static void Create()
        {
            Directory.CreateDirectory(SceneDirectory);
            Directory.CreateDirectory(ConfigDirectory);
            AssetDatabase.Refresh();

            GameConfig gameConfig = LoadOrCreateAsset<GameConfig>(GameConfigPath);
            UiTheme uiTheme = LoadOrCreateAsset<UiTheme>(UiThemePath);
            KingdomProjectSetup.ConfigurePortraitMobileSettings();
            KingdomProjectSetup.ConfigureBackgroundTextures();
            KingdomProjectSetup.ConfigureUiTextures();

            Sprite cityArt = AssetDatabase.LoadAssetAtPath<Sprite>(CityArtPath);
            Sprite worldArt = AssetDatabase.LoadAssetAtPath<Sprite>(WorldArtPath);
            Sprite heroArt = AssetDatabase.LoadAssetAtPath<Sprite>(HeroArtPath);
            Sprite cityIcon = AssetDatabase.LoadAssetAtPath<Sprite>(CityIconPath);
            Sprite heroIcon = AssetDatabase.LoadAssetAtPath<Sprite>(HeroIconPath);
            Sprite inventoryIcon = AssetDatabase.LoadAssetAtPath<Sprite>(InventoryIconPath);
            Sprite mailIcon = AssetDatabase.LoadAssetAtPath<Sprite>(MailIconPath);
            Sprite allianceIcon = AssetDatabase.LoadAssetAtPath<Sprite>(AllianceIconPath);

            foreach (string sceneName in SceneNames)
            {
                string path = $"{SceneDirectory}/{sceneName}.unity";
                var scene = File.Exists(path)
                    ? EditorSceneManager.OpenScene(path, OpenSceneMode.Single)
                    : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

                ConfigureScene(
                    sceneName,
                    gameConfig,
                    uiTheme,
                    cityArt,
                    worldArt,
                    heroArt,
                    cityIcon,
                    heroIcon,
                    inventoryIcon,
                    mailIcon,
                    allianceIcon);
                EditorSceneManager.SaveScene(scene, path);
            }

            EditorBuildSettings.scenes = SceneNames
                .Select(name => new EditorBuildSettingsScene($"{SceneDirectory}/{name}.unity", true))
                .ToArray();
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene($"{SceneDirectory}/Boot.unity", OpenSceneMode.Single);
            Debug.Log("Kingdom core scenes, local configuration, artwork, and build settings are ready.");
        }

        private static void ConfigureScene(
            string sceneName,
            GameConfig gameConfig,
            UiTheme uiTheme,
            Sprite cityArt,
            Sprite worldArt,
            Sprite heroArt,
            Sprite cityIcon,
            Sprite heroIcon,
            Sprite inventoryIcon,
            Sprite mailIcon,
            Sprite allianceIcon)
        {
            if (sceneName != "Boot")
                EnsureUiCamera(uiTheme);

            switch (sceneName)
            {
                case "Boot":
                    Bootstrapper bootstrapper = FindOrCreateComponent<Bootstrapper>("Bootstrapper");
                    bootstrapper.Configure(gameConfig);
                    EditorUtility.SetDirty(bootstrapper);
                    break;
                case "Login":
                    EnsureEventSystem();
                    LoginScreenController login = FindOrCreateComponent<LoginScreenController>("LoginScreen");
                    login.Configure(heroArt, uiTheme);
                    EditorUtility.SetDirty(login);
                    break;
                case "Main":
                    EnsureEventSystem();
                    KingdomShellController shell = FindOrCreateComponent<KingdomShellController>("KingdomShell");
                    shell.ConfigureArt(
                        cityArt,
                        worldArt,
                        heroArt,
                        uiTheme,
                        cityIcon,
                        heroIcon,
                        inventoryIcon,
                        mailIcon,
                        allianceIcon);
                    EditorUtility.SetDirty(shell);
                    break;
                case "BattleReplay":
                    EnsureEventSystem();
                    BattleReportScreenController report = FindOrCreateComponent<BattleReportScreenController>("BattleReportScreen");
                    report.Configure(worldArt, uiTheme);
                    EditorUtility.SetDirty(report);
                    break;
                case "Loading":
                    LoadingScreenController loading = FindOrCreateComponent<LoadingScreenController>("LoadingScreen");
                    loading.Configure(heroArt, uiTheme);
                    EditorUtility.SetDirty(loading);
                    break;
            }
        }

        private static T LoadOrCreateAsset<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
                return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static T FindOrCreateComponent<T>(string objectName) where T : Component
        {
            GameObject target = GameObject.Find(objectName);
            if (target == null)
                target = new GameObject(objectName);
            T component = target.GetComponent<T>();
            return component != null ? component : target.AddComponent<T>();
        }

        private static void EnsureEventSystem()
        {
            EventSystem eventSystem = Object.FindFirstObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                GameObject eventObject = new GameObject("EventSystem");
                eventSystem = eventObject.AddComponent<EventSystem>();
            }

            if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
                eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            StandaloneInputModule legacy = eventSystem.GetComponent<StandaloneInputModule>();
            if (legacy != null)
                Object.DestroyImmediate(legacy);
        }

        private static void EnsureUiCamera(UiTheme theme)
        {
            Camera camera = Object.FindFirstObjectByType<Camera>();
            if (camera == null)
            {
                GameObject cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                cameraObject.transform.position = new Vector3(0f, 0f, -10f);
                camera = cameraObject.AddComponent<Camera>();
            }

            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = UiTheme.Resolve(theme).Colors.Background;
            camera.cullingMask = 0;
            camera.orthographic = true;
            camera.depth = -100f;
            camera.allowHDR = false;
            camera.allowMSAA = false;
            EditorUtility.SetDirty(camera);
        }

    }
}
