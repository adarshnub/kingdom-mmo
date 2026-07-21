using System;
using System.Collections.Generic;
using System.Linq;
using Kingdom.Core;
using UnityEditor;
using UnityEngine;

namespace Kingdom.Editor
{
    public static class KingdomProjectSetup
    {
        private const string BackgroundDirectory = "Assets/_Game/Art/Backgrounds";
        private const string UiDirectory = "Assets/_Game/Art/UI";
        private const string GameConfigPath = "Assets/_Game/Config/GameConfig.asset";
        private const string ExpectedEditorVersion = "6000.3.7f1";

        private static readonly string[] RequiredScenePaths =
        {
            "Assets/_Game/Scenes/Boot.unity",
            "Assets/_Game/Scenes/Login.unity",
            "Assets/_Game/Scenes/Main.unity",
            "Assets/_Game/Scenes/City.unity",
            "Assets/_Game/Scenes/WorldMap.unity",
            "Assets/_Game/Scenes/BattleReplay.unity",
            "Assets/_Game/Scenes/Loading.unity"
        };

        private static readonly string[] RequiredBackgroundPaths =
        {
            $"{BackgroundDirectory}/capital-city-v2.png",
            $"{BackgroundDirectory}/frontier-marshal.png",
            $"{BackgroundDirectory}/world-frontier.png"
        };

        private static readonly string[] RequiredUiSpritePaths =
        {
            $"{UiDirectory}/Navigation/nav-city.png",
            $"{UiDirectory}/Navigation/nav-hero.png",
            $"{UiDirectory}/Navigation/nav-inventory.png",
            $"{UiDirectory}/Navigation/nav-mail.png",
            $"{UiDirectory}/Navigation/nav-alliance.png"
        };

        [MenuItem("Kingdom/Project Setup/Configure Portrait Mobile Settings")]
        public static void ConfigurePortraitMobileSettings()
        {
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.runInBackground = false;

            AssetDatabase.SaveAssets();
            Debug.Log("Kingdom player settings configured for portrait mobile play.");
        }

        [MenuItem("Kingdom/Project Setup/Configure Background Textures")]
        public static void ConfigureBackgroundTextures()
        {
            string[] assetPaths = AssetDatabase
                .FindAssets("t:Texture2D", new[] { BackgroundDirectory })
                .Select(AssetDatabase.GUIDToAssetPath)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();

            int updatedCount = 0;
            foreach (string assetPath in assetPaths)
            {
                if (ConfigureBackgroundTexture(assetPath))
                {
                    updatedCount++;
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"Kingdom background texture setup complete. Updated {updatedCount} of {assetPaths.Length} assets.");
        }

        [MenuItem("Kingdom/Project Setup/Configure UI Textures")]
        public static void ConfigureUiTextures()
        {
            string[] assetPaths = AssetDatabase
                .FindAssets("t:Texture2D", new[] { UiDirectory })
                .Select(AssetDatabase.GUIDToAssetPath)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();

            int updatedCount = 0;
            foreach (string assetPath in assetPaths)
            {
                if (ConfigureUiTexture(assetPath))
                    updatedCount++;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"Kingdom UI texture setup complete. Updated {updatedCount} of {assetPaths.Length} assets.");
        }

        [MenuItem("Kingdom/Project Setup/Validate Project")]
        public static void ValidateProjectMenu()
        {
            ValidateProject();
            Debug.Log("Kingdom project validation passed.");
        }

        // Public entry point for Unity's -executeMethod command-line option.
        public static void ConfigureAll()
        {
            ConfigurePortraitMobileSettings();
            ConfigureBackgroundTextures();
            ConfigureUiTextures();
            ValidateProject();
            Debug.Log("Kingdom automated project setup and validation completed.");
        }

        public static void ValidateProject()
        {
            var issues = new List<string>();

            if (!string.Equals(Application.unityVersion, ExpectedEditorVersion, StringComparison.Ordinal))
            {
                issues.Add($"Open the project with Unity {ExpectedEditorVersion}; current version is {Application.unityVersion}.");
            }

            if (PlayerSettings.defaultInterfaceOrientation != UIOrientation.Portrait)
            {
                issues.Add("PlayerSettings default orientation must be Portrait.");
            }

            if (AssetDatabase.LoadAssetAtPath<GameConfig>(GameConfigPath) == null)
            {
                issues.Add($"Missing GameConfig asset at {GameConfigPath}.");
            }
            else if (!AssetDatabase.GetDependencies(RequiredScenePaths[0], true).Contains(GameConfigPath, StringComparer.Ordinal))
            {
                issues.Add("Boot scene Bootstrapper is not linked to the required GameConfig asset.");
            }

            foreach (string scenePath in RequiredScenePaths)
            {
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) == null)
                {
                    issues.Add($"Missing required scene: {scenePath}.");
                }
            }

            string[] enabledBuildScenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();

            if (!enabledBuildScenes.SequenceEqual(RequiredScenePaths, StringComparer.Ordinal))
            {
                issues.Add("Enabled build scenes must match the required Kingdom scene order.");
            }

            foreach (string backgroundPath in RequiredBackgroundPaths)
            {
                if (AssetDatabase.LoadAssetAtPath<Texture2D>(backgroundPath) == null)
                {
                    issues.Add($"Missing required original background: {backgroundPath}.");
                    continue;
                }

                if (AssetImporter.GetAtPath(backgroundPath) is not TextureImporter importer)
                {
                    issues.Add($"Could not inspect texture importer for {backgroundPath}.");
                    continue;
                }

                TextureImporterPlatformSettings android = importer.GetPlatformTextureSettings("Android");
                if (importer.textureType != TextureImporterType.Sprite || importer.mipmapEnabled)
                {
                    issues.Add($"Background must be a non-mipmapped Sprite: {backgroundPath}.");
                }

                if (!android.overridden || android.format != TextureImporterFormat.ASTC_6x6)
                {
                    issues.Add($"Background must use the Android ASTC 6x6 override: {backgroundPath}.");
                }
            }

            foreach (string uiSpritePath in RequiredUiSpritePaths)
            {
                if (AssetDatabase.LoadAssetAtPath<Sprite>(uiSpritePath) == null)
                    issues.Add($"Missing or incorrectly imported original UI sprite: {uiSpritePath}.");
            }

            if (issues.Count > 0)
            {
                throw new InvalidOperationException(
                    "Kingdom project validation failed:\n- " + string.Join("\n- ", issues));
            }
        }

        private static bool ConfigureBackgroundTexture(string assetPath)
        {
            if (AssetImporter.GetAtPath(assetPath) is not TextureImporter importer)
            {
                return false;
            }

            bool changed = false;
            changed |= SetIfDifferent(importer.textureType, TextureImporterType.Sprite, value => importer.textureType = value);
            changed |= SetIfDifferent(importer.spriteImportMode, SpriteImportMode.Single, value => importer.spriteImportMode = value);
            changed |= SetIfDifferent(importer.mipmapEnabled, false, value => importer.mipmapEnabled = value);
            changed |= SetIfDifferent(importer.alphaIsTransparency, true, value => importer.alphaIsTransparency = value);
            changed |= SetIfDifferent(importer.sRGBTexture, true, value => importer.sRGBTexture = value);
            changed |= SetIfDifferent(importer.wrapMode, TextureWrapMode.Clamp, value => importer.wrapMode = value);
            changed |= SetIfDifferent(importer.filterMode, FilterMode.Bilinear, value => importer.filterMode = value);
            changed |= SetIfDifferent(
                importer.textureCompression,
                TextureImporterCompression.CompressedHQ,
                value => importer.textureCompression = value);
            changed |= SetIfDifferent(importer.maxTextureSize, 2048, value => importer.maxTextureSize = value);

            TextureImporterPlatformSettings android = importer.GetPlatformTextureSettings("Android");
            if (!android.overridden ||
                android.maxTextureSize != 2048 ||
                android.format != TextureImporterFormat.ASTC_6x6 ||
                android.compressionQuality != 50)
            {
                android.name = "Android";
                android.overridden = true;
                android.maxTextureSize = 2048;
                android.format = TextureImporterFormat.ASTC_6x6;
                android.compressionQuality = 50;
                importer.SetPlatformTextureSettings(android);
                changed = true;
            }

            if (changed)
            {
                importer.SaveAndReimport();
            }

            return changed;
        }

        private static bool ConfigureUiTexture(string assetPath)
        {
            if (AssetImporter.GetAtPath(assetPath) is not TextureImporter importer)
                return false;

            bool changed = false;
            changed |= SetIfDifferent(importer.textureType, TextureImporterType.Sprite, value => importer.textureType = value);
            changed |= SetIfDifferent(importer.spriteImportMode, SpriteImportMode.Single, value => importer.spriteImportMode = value);
            changed |= SetIfDifferent(importer.mipmapEnabled, false, value => importer.mipmapEnabled = value);
            changed |= SetIfDifferent(importer.sRGBTexture, true, value => importer.sRGBTexture = value);
            changed |= SetIfDifferent(importer.wrapMode, TextureWrapMode.Clamp, value => importer.wrapMode = value);
            changed |= SetIfDifferent(importer.filterMode, FilterMode.Bilinear, value => importer.filterMode = value);
            changed |= SetIfDifferent(
                importer.textureCompression,
                TextureImporterCompression.CompressedHQ,
                value => importer.textureCompression = value);
            changed |= SetIfDifferent(importer.maxTextureSize, 512, value => importer.maxTextureSize = value);

            TextureImporterPlatformSettings android = importer.GetPlatformTextureSettings("Android");
            if (!android.overridden ||
                android.maxTextureSize != 512 ||
                android.format != TextureImporterFormat.ASTC_6x6 ||
                android.compressionQuality != 50)
            {
                android.name = "Android";
                android.overridden = true;
                android.maxTextureSize = 512;
                android.format = TextureImporterFormat.ASTC_6x6;
                android.compressionQuality = 50;
                importer.SetPlatformTextureSettings(android);
                changed = true;
            }

            if (changed)
                importer.SaveAndReimport();

            return changed;
        }

        private static bool SetIfDifferent<T>(T current, T desired, Action<T> setter)
        {
            if (EqualityComparer<T>.Default.Equals(current, desired))
            {
                return false;
            }

            setter(desired);
            return true;
        }
    }
}
