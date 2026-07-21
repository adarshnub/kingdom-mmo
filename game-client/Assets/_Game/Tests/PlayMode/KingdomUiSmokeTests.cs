using System.Collections;
using System.Linq;
using Kingdom.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Kingdom.Tests
{
    public sealed class KingdomUiSmokeTests
    {
        [UnityTest]
        public IEnumerator LoginScreenBuildsGuestEntryControls()
        {
            GameObject root = new GameObject("LoginTestRoot");
            root.AddComponent<LoginScreenController>();

            yield return null;

            Canvas canvas = root.GetComponentInChildren<Canvas>(true);
            Assert.That(canvas, Is.Not.Null);
            Assert.That(root.GetComponentsInChildren<TMP_InputField>(true), Has.Length.EqualTo(1));
            Assert.That(root.GetComponentsInChildren<Button>(true).Length, Is.GreaterThanOrEqualTo(1));
            Assert.That(
                root.GetComponentsInChildren<TMP_Text>(true).Any(text => text.text == "KINGDOM ONE"),
                Is.True);

            Transform safeArea = canvas.transform.Find("SafeArea");
            Transform background = canvas.transform.Find("FrontierMarshal");
            Transform scrim = canvas.transform.Find("ArtScrim");
            Assert.That(safeArea, Is.Not.Null);
            Assert.That(background, Is.Not.Null);
            Assert.That(scrim, Is.Not.Null);
            Assert.That(safeArea.GetSiblingIndex(), Is.GreaterThan(background.GetSiblingIndex()));
            Assert.That(safeArea.GetSiblingIndex(), Is.GreaterThan(scrim.GetSiblingIndex()));

            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ShellBuildsEveryPrototypeDestination()
        {
            GameObject root = new GameObject("ShellTestRoot");
            KingdomShellController shell = root.AddComponent<KingdomShellController>();

            yield return null;

            Assert.That(root.GetComponentInChildren<Canvas>(true), Is.Not.Null);
            Assert.That(root.GetComponentsInChildren<Button>(true).Length, Is.GreaterThanOrEqualTo(5));
            Assert.That(root.GetComponentsInChildren<CityBuildingTile>(true).Length, Is.GreaterThanOrEqualTo(3));
            Assert.That(
                root.GetComponentsInChildren<TMP_Text>(true).Any(text => text.text.Contains("Castle Keep")),
                Is.True);

            KingdomScreenId[] destinations =
            {
                KingdomScreenId.City,
                KingdomScreenId.World,
                KingdomScreenId.Alliance,
                KingdomScreenId.Mail,
                KingdomScreenId.More,
                KingdomScreenId.Hero,
                KingdomScreenId.Inventory,
                KingdomScreenId.Training,
                KingdomScreenId.Territory,
            };

            foreach (KingdomScreenId destination in destinations)
            {
                shell.OpenScreen(destination);
                yield return null;
                Assert.That(
                    root.GetComponentsInChildren<TMP_Text>(true).Length,
                    Is.GreaterThan(5),
                    $"{destination} should render meaningful text content.");
            }

            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator LoadingAndBattleReportScreensBuildWithoutSceneAssets()
        {
            GameObject loadingRoot = new GameObject("LoadingTestRoot");
            loadingRoot.AddComponent<LoadingScreenController>();
            GameObject reportRoot = new GameObject("ReportTestRoot");
            reportRoot.AddComponent<BattleReportScreenController>();

            yield return null;

            Assert.That(loadingRoot.GetComponentInChildren<Canvas>(true), Is.Not.Null);
            Assert.That(reportRoot.GetComponentInChildren<Canvas>(true), Is.Not.Null);
            Assert.That(
                reportRoot.GetComponentsInChildren<TMP_Text>(true).Any(text => text.text == "BATTLE REPORT"),
                Is.True);

            Object.Destroy(loadingRoot);
            Object.Destroy(reportRoot);
            yield return null;
        }
    }
}
