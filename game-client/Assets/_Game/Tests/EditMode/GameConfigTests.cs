using Kingdom.Core;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Kingdom.Tests
{
    public sealed class GameConfigTests
    {
        [TestCase("http://localhost:3000/", "http://localhost:3000")]
        [TestCase("https://api.kingdom.example///", "https://api.kingdom.example")]
        [TestCase("https://api.kingdom.example", "https://api.kingdom.example")]
        public void ApiBaseUrl_RemovesTrailingSlashes(string configuredUrl, string expectedUrl)
        {
            GameConfig config = ScriptableObject.CreateInstance<GameConfig>();
            try
            {
                var serializedConfig = new SerializedObject(config);
                serializedConfig.FindProperty("apiBaseUrl").stringValue = configuredUrl;
                serializedConfig.ApplyModifiedPropertiesWithoutUndo();

                Assert.That(config.ApiBaseUrl, Is.EqualTo(expectedUrl));
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [TestCase(1, 5)]
        [TestCase(30, 30)]
        [TestCase(120, 60)]
        public void RequestTimeoutSeconds_ClampsToSupportedRange(int configuredTimeout, int expectedTimeout)
        {
            GameConfig config = ScriptableObject.CreateInstance<GameConfig>();
            try
            {
                var serializedConfig = new SerializedObject(config);
                serializedConfig.FindProperty("requestTimeoutSeconds").intValue = configuredTimeout;
                serializedConfig.ApplyModifiedPropertiesWithoutUndo();

                Assert.That(config.RequestTimeoutSeconds, Is.EqualTo(expectedTimeout));
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }
    }
}
