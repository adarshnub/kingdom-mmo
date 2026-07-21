using System;
using System.Collections;
using System.Collections.Generic;
using Kingdom.Boot;
using Kingdom.Contracts;
using Kingdom.Core;
using Kingdom.Networking;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Kingdom.UI
{
    public sealed class KingdomShellController : MonoBehaviour
    {
        [SerializeField] private UiTheme theme;
        [SerializeField] private Sprite cityBackdrop;
        [SerializeField] private Sprite worldBackdrop;
        [SerializeField] private Sprite heroPortrait;
        [SerializeField] private Sprite cityNavigationIcon;
        [SerializeField] private Sprite heroNavigationIcon;
        [SerializeField] private Sprite inventoryNavigationIcon;
        [SerializeField] private Sprite mailNavigationIcon;
        [SerializeField] private Sprite allianceNavigationIcon;

        private readonly Dictionary<KingdomScreenId, ButtonView> navigation = new();
        private UiTheme resolvedTheme;
        private RectTransform contentRoot;
        private TMP_Text playerText;
        private TMP_Text powerText;
        private TMP_Text statusText;
        private TMP_Text[] resourceTexts;
        private GameSession session;
        private KingdomScreenId currentScreen;
        private RectTransform cityDetailPanel;
        private bool upgradeSubmitting;

        private static readonly CitySlotDefinition[] CitySlots =
        {
            new("palace", "palace", new Vector2(0.5f, 0.68f), "Central keep"),
            new("farm_1", "farm", new Vector2(0.27f, 0.45f), "Farmstead"),
            new("barracks", "barracks", new Vector2(0.72f, 0.4f), "Barracks"),
            new("wall", "wall", new Vector2(0.5f, 0.33f), "Wall foundation"),
            new("warehouse", "warehouse", new Vector2(0.35f, 0.23f), "Warehouse plot"),
            new("hospital", "hospital", new Vector2(0.66f, 0.23f), "Hospital plot"),
            new("academy", "academy", new Vector2(0.29f, 0.68f), "Academy plot"),
            new("range", "range", new Vector2(0.72f, 0.63f), "Range plot"),
        };

        public void ConfigureArt(
            Sprite city,
            Sprite world,
            Sprite hero,
            UiTheme configuredTheme,
            Sprite cityIcon = null,
            Sprite heroIcon = null,
            Sprite inventoryIcon = null,
            Sprite mailIcon = null,
            Sprite allianceIcon = null)
        {
            cityBackdrop = city;
            worldBackdrop = world;
            heroPortrait = hero;
            theme = configuredTheme;
            cityNavigationIcon = cityIcon;
            heroNavigationIcon = heroIcon;
            inventoryNavigationIcon = inventoryIcon;
            mailNavigationIcon = mailIcon;
            allianceNavigationIcon = allianceIcon;
        }

        private void Start()
        {
            resolvedTheme = UiTheme.Resolve(theme);
            try
            {
                session = Bootstrapper.Resolve<GameSession>();
                session.Changed += RefreshHud;
            }
            catch (InvalidOperationException)
            {
                session = null;
            }

            BuildShell();
            OpenScreen(KingdomScreenId.City);
            RefreshHud();
        }

        private void OnDestroy()
        {
            if (session != null)
                session.Changed -= RefreshHud;
        }

        public void OpenScreen(KingdomScreenId screen)
        {
            currentScreen = screen;
            ScreenUiBuilder.DestroyChildren(contentRoot);

            switch (screen)
            {
                case KingdomScreenId.World:
                    BuildWorldScreen();
                    break;
                case KingdomScreenId.Alliance:
                    BuildAllianceScreen();
                    break;
                case KingdomScreenId.Mail:
                    BuildMailScreen();
                    break;
                case KingdomScreenId.More:
                    BuildMoreScreen();
                    break;
                case KingdomScreenId.Hero:
                    BuildHeroScreen();
                    break;
                case KingdomScreenId.Inventory:
                    BuildInventoryScreen();
                    break;
                case KingdomScreenId.Training:
                    BuildTrainingScreen();
                    break;
                case KingdomScreenId.Territory:
                    BuildTerritoryScreen();
                    break;
                default:
                    BuildCityScreen();
                    break;
            }

            RefreshNavigation();
        }

        private void BuildShell()
        {
            CanvasView canvas = UiFactory.CreateCanvas("KingdomCanvas", transform, resolvedTheme);
            UiFactory.CreatePanel(canvas.Canvas.transform, "RootBackdrop", resolvedTheme.Colors.Background);
            canvas.SafeArea.SetAsLastSibling();

            BuildTopHud(canvas.SafeArea);

            contentRoot = UiFactory.CreatePanel(
                canvas.SafeArea,
                "ScreenContent",
                resolvedTheme.Colors.Background,
                new Vector2(0f, 0.155f),
                new Vector2(1f, 0.86f),
                Vector4.zero);

            RectTransform status = UiFactory.CreatePanel(
                canvas.SafeArea,
                "StatusStrip",
                resolvedTheme.Colors.Overlay,
                new Vector2(0f, 0.11f),
                new Vector2(1f, 0.155f),
                Vector4.zero);
            statusText = UiFactory.CreateText(
                status,
                "Status",
                "KINGDOM 1  •  The frontier beacon is stable.",
                resolvedTheme.Typography.Caption,
                resolvedTheme.Colors.TextPrimary,
                TextAlignmentOptions.MidlineLeft);
            UiFactory.SetStretch(statusText.rectTransform, 32f, 4f);

            BuildBottomNavigation(canvas.SafeArea);
        }

        private void BuildTopHud(RectTransform safeArea)
        {
            RectTransform hud = UiFactory.CreatePanel(
                safeArea,
                "TopHud",
                resolvedTheme.Colors.Overlay,
                new Vector2(0f, 0.86f),
                Vector2.one,
                Vector4.zero);

            RectTransform identity = UiFactory.CreatePanel(
                hud,
                "Identity",
                Color.clear,
                new Vector2(0f, 0.5f),
                Vector2.one,
                new Vector4(24f, 8f, 24f, 6f));
            ScreenUiBuilder.AddHorizontalLayout(identity, 16f, null, TextAnchor.MiddleCenter);

            Image avatar = UiFactory.CreateImage(identity, "RulerPortrait", Color.white);
            avatar.sprite = heroNavigationIcon;
            avatar.preserveAspect = true;
            ScreenUiBuilder.SetLayout(avatar, 96f, 96f, 0f, 0f);
            playerText = UiFactory.CreateText(
                identity,
                "Player",
                "Frontier Ruler\nVIP 1",
                resolvedTheme.Typography.Title,
                resolvedTheme.Colors.TextPrimary,
                TextAlignmentOptions.MidlineLeft,
                FontStyles.Bold);
            ScreenUiBuilder.SetLayout(playerText, 80f, -1f, 2f);
            powerText = UiFactory.CreateText(
                identity,
                "Power",
                "POWER 0",
                resolvedTheme.Typography.Caption,
                resolvedTheme.Colors.GoldBright,
                TextAlignmentOptions.MidlineRight,
                FontStyles.Bold);
            ScreenUiBuilder.SetLayout(powerText, 80f, 260f, 0f);

            RectTransform resources = UiFactory.CreatePanel(
                hud,
                "Resources",
                resolvedTheme.Colors.Surface,
                Vector2.zero,
                new Vector2(1f, 0.5f),
                new Vector4(16f, 8f, 16f, 4f));
            ScreenUiBuilder.AddHorizontalLayout(resources, 8f, null, TextAnchor.MiddleCenter);

            string[] codes = { "FD", "WD", "IR", "SV", "GD" };
            resourceTexts = new TMP_Text[codes.Length];
            for (int index = 0; index < codes.Length; index++)
            {
                RectTransform chip = UiFactory.CreatePanel(resources, $"Resource_{codes[index]}", resolvedTheme.Colors.SurfaceRaised);
                ScreenUiBuilder.SetLayout(chip, 64f, -1f, 1f);
                TMP_Text label = UiFactory.CreateText(
                    chip,
                    "Value",
                    $"{codes[index]} 0",
                    24f,
                    resolvedTheme.Colors.TextPrimary,
                    TextAlignmentOptions.Center,
                    FontStyles.Bold);
                UiFactory.SetStretch(label.rectTransform, 4f, 2f);
                resourceTexts[index] = label;
            }
        }

        private void BuildBottomNavigation(RectTransform safeArea)
        {
            RectTransform bar = UiFactory.CreatePanel(
                safeArea,
                "BottomNavigation",
                resolvedTheme.Colors.Surface,
                Vector2.zero,
                new Vector2(1f, 0.11f),
                Vector4.zero);
            ScreenUiBuilder.AddHorizontalLayout(
                bar,
                8f,
                new RectOffset(8, 8, 6, 6),
                TextAnchor.MiddleCenter);

            CreateNavigationButton(bar, KingdomScreenId.City, "CITY", cityNavigationIcon);
            CreateNavigationButton(bar, KingdomScreenId.Hero, "HERO", heroNavigationIcon);
            CreateNavigationButton(bar, KingdomScreenId.Inventory, "ITEMS", inventoryNavigationIcon);
            CreateNavigationButton(bar, KingdomScreenId.Mail, "MAIL", mailNavigationIcon, "4");
            CreateNavigationButton(bar, KingdomScreenId.Alliance, "ALLIANCE", allianceNavigationIcon);
        }

        private void CreateNavigationButton(
            Transform parent,
            KingdomScreenId id,
            string label,
            Sprite icon,
            string notification = null)
        {
            ButtonView button = UiFactory.CreateIconButton(parent, $"Nav_{id}", label, icon, resolvedTheme);
            ScreenUiBuilder.SetLayout(button.Root, 180f, -1f, 1f);
            button.Label.fontSize = 22f;
            button.Button.onClick.AddListener(() => OpenScreen(id));
            if (!string.IsNullOrWhiteSpace(notification))
            {
                RectTransform badge = ScreenUiBuilder.CreateBadge(
                    button.Root,
                    notification,
                    resolvedTheme,
                    resolvedTheme.Colors.Danger,
                    46f);
                AnchorBox(badge, new Vector2(0.82f, 0.82f), new Vector2(46f, 46f));
            }
            navigation[id] = button;
        }

        private void RefreshNavigation()
        {
            KingdomScreenId selected = currentScreen switch
            {
                KingdomScreenId.World => KingdomScreenId.City,
                KingdomScreenId.Training => KingdomScreenId.City,
                KingdomScreenId.More => KingdomScreenId.City,
                KingdomScreenId.Territory => KingdomScreenId.Alliance,
                _ => currentScreen,
            };

            foreach (KeyValuePair<KingdomScreenId, ButtonView> entry in navigation)
            {
                bool active = entry.Key == selected;
                entry.Value.Background.color = active
                    ? resolvedTheme.Colors.Gold
                    : resolvedTheme.Colors.SurfaceRaised;
                entry.Value.Label.color = active
                    ? resolvedTheme.Colors.TextOnLight
                    : resolvedTheme.Colors.TextPrimary;
            }
        }

        private void RefreshHud()
        {
            if (playerText == null)
                return;

            BootstrapData bootstrap = session?.Bootstrap;
            if (bootstrap?.player == null || bootstrap.city?.resources == null)
            {
                playerText.text = "Frontier Ruler\nVIP 1";
                powerText.text = "POWER 0";
                string[] fallback = { "FD 100K", "WD 100K", "IR 10K", "SV 1K", "GD 500" };
                for (int index = 0; index < resourceTexts.Length; index++)
                    resourceTexts[index].text = fallback[index];
                return;
            }

            playerText.text = $"{bootstrap.player.display_name}\nVIP 1";
            powerText.text = $"POWER {ScreenUiBuilder.CompactNumber(bootstrap.player.power)}";
            BootstrapResources resources = bootstrap.city.resources;
            string[] values =
            {
                $"FD {ScreenUiBuilder.CompactNumber(resources.food)}",
                $"WD {ScreenUiBuilder.CompactNumber(resources.wood)}",
                $"IR {ScreenUiBuilder.CompactNumber(resources.iron)}",
                $"SV {ScreenUiBuilder.CompactNumber(resources.silver)}",
                $"GD {ScreenUiBuilder.CompactNumber(resources.gold)}",
            };
            for (int index = 0; index < resourceTexts.Length; index++)
                resourceTexts[index].text = values[index];
        }

        private void BuildCityScreen()
        {
            ScreenUiBuilder.CreateBackground(contentRoot, "CapitalCity", cityBackdrop, resolvedTheme.Colors.Background);
            UiFactory.CreatePanel(contentRoot, "CityReadability", new Color(0.02f, 0.05f, 0.07f, 0.06f));
            cityDetailPanel = null;

            RectTransform cityGrid = UiFactory.CreatePanel(
                contentRoot,
                "CityBuildGrid",
                Color.clear,
                new Vector2(0.02f, 0.17f),
                new Vector2(0.98f, 0.96f),
                Vector4.zero);
            CreateCityGridGuide(cityGrid);

            Dictionary<string, BootstrapBuilding> buildingsBySlot = CreateBuildingsBySlot(GetCityBuildings());
            foreach (CitySlotDefinition slot in CitySlots)
            {
                if (buildingsBySlot.TryGetValue(slot.SlotKey, out BootstrapBuilding building))
                {
                    CreateCityMarker(cityGrid, slot, building);
                }
                else
                {
                    CreateLockedCitySlot(cityGrid, slot);
                }
            }

            RectTransform events = UiFactory.CreatePanel(
                contentRoot,
                "EventRail",
                Color.clear,
                new Vector2(0.77f, 0.54f),
                new Vector2(0.985f, 0.96f),
                Vector4.zero);
            ScreenUiBuilder.AddVerticalLayout(events, 12f);
            CreateEventAction(events, inventoryNavigationIcon, "BUILD", "02:14", () => SetStatus("One construction queue is active."));
            CreateEventAction(events, allianceNavigationIcon, "PACT", "4H", () => SetStatus("Beacon Trials end in four hours."));
            CreateEventAction(events, mailNavigationIcon, "REWARD", "NEW", () => SetStatus("A frontier reward is ready."));

            ButtonView realm = UiFactory.CreateIconButton(
                contentRoot,
                "RealmMap",
                "REALM",
                cityNavigationIcon,
                resolvedTheme);
            AnchorBox(realm.Root, new Vector2(0.105f, 0.19f), new Vector2(150f, 150f));
            realm.Label.fontSize = 20f;
            realm.Button.onClick.AddListener(() => OpenScreen(KingdomScreenId.World));

            RectTransform quest = UiFactory.CreatePanel(
                contentRoot,
                "QuestCard",
                resolvedTheme.Colors.Overlay,
                new Vector2(0.19f, 0.025f),
                new Vector2(0.96f, 0.145f),
                Vector4.zero);
            TMP_Text questTitle = UiFactory.CreateText(
                quest,
                "Quest",
                "FRONTIER TASK\nTap a building for stats. Drag tiles to test the city grid.",
                resolvedTheme.Typography.Caption,
                resolvedTheme.Colors.TextPrimary,
                TextAlignmentOptions.MidlineLeft,
                FontStyles.Bold);
            UiFactory.SetStretch(questTitle.rectTransform, 28f, 8f);
            SetStatus("Castle grounds ready. Select a building to inspect upgrades.");
        }

        private void BuildWorldScreen()
        {
            ScreenUiBuilder.CreateBackground(contentRoot, "WorldFrontier", worldBackdrop, resolvedTheme.Colors.Background);
            UiFactory.CreatePanel(contentRoot, "MapReadability", new Color(0.01f, 0.04f, 0.05f, 0.08f));

            CreateMapMarker(contentRoot, new Vector2(0.31f, 0.64f), "Watchtower  •  Lv 4", () => SetStatus("Watchtower scouted. Travel: 01:48."));
            CreateMapMarker(contentRoot, new Vector2(0.68f, 0.53f), "Moonsteel Quarry  •  Lv 7", () => SetStatus("Quarry selected. 12.4K moonsteel available."));
            CreateMapMarker(contentRoot, new Vector2(0.47f, 0.29f), "Ashen Sentinel  •  Lv 9", () => SetStatus("Sentinel selected. Recommended power: 24K."));

            RectTransform coordinates = UiFactory.CreatePanel(
                contentRoot,
                "Coordinates",
                resolvedTheme.Colors.Overlay,
                new Vector2(0.27f, 0.025f),
                new Vector2(0.73f, 0.115f),
                Vector4.zero);
            int x = session?.Bootstrap?.city?.x ?? 600;
            int y = session?.Bootstrap?.city?.y ?? 600;
            TMP_Text coordinateText = UiFactory.CreateText(
                coordinates,
                "Value",
                $"KINGDOM 1    X {x}    Y {y}",
                resolvedTheme.Typography.Caption,
                resolvedTheme.Colors.TextPrimary,
                TextAlignmentOptions.Center,
                FontStyles.Bold);
            UiFactory.SetStretch(coordinateText.rectTransform, 12f, 4f);
        }

        private void BuildHeroScreen()
        {
            ScreenUiBuilder.CreateBackground(contentRoot, "HeroPortrait", heroPortrait, resolvedTheme.Colors.Background);
            UiFactory.CreatePanel(contentRoot, "HeroScrim", new Color(0.01f, 0.03f, 0.05f, 0.28f));
            CreateScreenTitle("Marshal Aster", "FRONTIER COMMANDER  •  LEVEL 25", true);

            string[] leftSlots = { "WEAPON  18", "ARMOR  16", "BOOTS  12" };
            string[] rightSlots = { "HELM  15", "RELIC  10", "RING  8" };
            CreateEquipmentColumn(new Vector2(0.025f, 0.33f), new Vector2(0.28f, 0.72f), leftSlots);
            CreateEquipmentColumn(new Vector2(0.72f, 0.33f), new Vector2(0.975f, 0.72f), rightSlots);

            RectTransform progress = UiFactory.CreatePanel(
                contentRoot,
                "HeroProgress",
                resolvedTheme.Colors.Overlay,
                new Vector2(0.06f, 0.03f),
                new Vector2(0.94f, 0.24f),
                Vector4.zero);
            ScreenUiBuilder.AddVerticalLayout(progress, 14f, new RectOffset(24, 24, 20, 20));
            ProgressBarView experience = UiFactory.CreateProgressBar(progress, "Experience", "EXPERIENCE  31,448 / 38,200", 0.82f, resolvedTheme);
            ScreenUiBuilder.SetLayout(experience.Root, 68f);
            ProgressBarView command = UiFactory.CreateProgressBar(progress, "Command", "COMMAND  72 / 100", 0.72f, resolvedTheme);
            command.Fill.color = resolvedTheme.Colors.RoyalBlue;
            ScreenUiBuilder.SetLayout(command.Root, 68f);
        }

        private void BuildInventoryScreen()
        {
            UiFactory.CreatePanel(contentRoot, "InventoryBackdrop", resolvedTheme.Colors.Background);
            CreateScreenTitle("Inventory", "ORIGINAL ITEMS  •  64 / 120 SLOTS", false);

            RectTransform tabs = UiFactory.CreatePanel(
                contentRoot,
                "Tabs",
                resolvedTheme.Colors.Surface,
                new Vector2(0.03f, 0.78f),
                new Vector2(0.97f, 0.88f),
                Vector4.zero);
            ScreenUiBuilder.AddHorizontalLayout(tabs, 8f, new RectOffset(8, 8, 8, 8));
            foreach (string tab in new[] { "ALL", "RESOURCE", "SPEED", "COMBAT" })
            {
                string selectedTab = tab;
                ButtonView button = UiFactory.CreateButton(tabs, $"Tab_{tab}", tab, resolvedTheme, tab == "ALL" ? ButtonTone.Primary : ButtonTone.Quiet);
                button.Label.fontSize = 22f;
                button.Button.onClick.AddListener(() => SetStatus($"Inventory filter: {selectedTab}."));
            }

            ScrollAreaView scroll = UiFactory.CreateScrollArea(contentRoot, "InventoryGrid", resolvedTheme);
            UiFactory.SetAnchors(scroll.Root, new Vector2(0.02f, 0.02f), new Vector2(0.98f, 0.77f), Vector4.zero);
            for (int start = 0; start < PrototypeContent.Inventory.Length; start += 3)
            {
                RectTransform row = UiFactory.CreatePanel(scroll.Content, $"Row_{start / 3}", Color.clear);
                ScreenUiBuilder.SetLayout(row, 236f);
                ScreenUiBuilder.AddHorizontalLayout(row, 14f);
                for (int column = 0; column < 3; column++)
                {
                    int index = start + column;
                    if (index < PrototypeContent.Inventory.Length)
                        CreateInventoryTile(row, PrototypeContent.Inventory[index]);
                    else
                        ScreenUiBuilder.SetLayout(UiFactory.CreatePanel(row, "Empty", Color.clear), 220f, -1f, 1f);
                }
            }
        }

        private void BuildMailScreen()
        {
            UiFactory.CreatePanel(contentRoot, "MailBackdrop", resolvedTheme.Colors.Parchment);
            CreateScreenTitle("Dispatches", "MAIL, REPORTS, AND KINGDOM NOTICES", false, true);
            ScrollAreaView scroll = UiFactory.CreateScrollArea(contentRoot, "MailList", resolvedTheme);
            UiFactory.SetAnchors(scroll.Root, new Vector2(0.02f, 0.02f), new Vector2(0.98f, 0.87f), Vector4.zero);

            foreach (MailEntry entry in PrototypeContent.Mail)
            {
                MailEntry selected = entry;
                RectTransform row = CreateListRow(
                    scroll.Content,
                    entry.Code,
                    entry.Title,
                    entry.Detail,
                    entry.Unread > 0 ? entry.Unread.ToString() : string.Empty,
                    resolvedTheme.Colors.SurfaceRaised,
                    () => SetStatus($"Opened {selected.Title.ToLowerInvariant()}."));
                ScreenUiBuilder.SetLayout(row, 126f);
            }
        }

        private void BuildAllianceScreen()
        {
            UiFactory.CreatePanel(contentRoot, "AllianceBackdrop", resolvedTheme.Colors.Background);
            CreateScreenTitle("Alliance", "COOPERATE, RALLY, AND CONTROL TERRITORY", false);

            RectTransform summary = UiFactory.CreatePanel(
                contentRoot,
                "AllianceSummary",
                resolvedTheme.Colors.SurfaceRaised,
                new Vector2(0.04f, 0.67f),
                new Vector2(0.96f, 0.86f),
                Vector4.zero);
            ScreenUiBuilder.CreateBadge(summary, "AVR", resolvedTheme, resolvedTheme.Colors.RoyalBlue, 108f);
            RectTransform badge = summary.GetChild(summary.childCount - 1) as RectTransform;
            AnchorBox(badge, new Vector2(0.12f, 0.5f), new Vector2(108f, 108f));
            TMP_Text info = UiFactory.CreateText(
                summary,
                "Info",
                "AVERON PACT\nLeader  Mira Vale    •    38 / 50 members\nPower  8.4M    •    English",
                resolvedTheme.Typography.Caption,
                resolvedTheme.Colors.TextPrimary,
                TextAlignmentOptions.MidlineLeft,
                FontStyles.Bold);
            UiFactory.SetAnchors(info.rectTransform, new Vector2(0.24f, 0.08f), new Vector2(0.96f, 0.92f), Vector4.zero);

            ScrollAreaView scroll = UiFactory.CreateScrollArea(contentRoot, "AllianceContent", resolvedTheme);
            UiFactory.SetAnchors(scroll.Root, new Vector2(0.02f, 0.02f), new Vector2(0.98f, 0.65f), Vector4.zero);
            foreach (string action in PrototypeContent.AllianceActions)
            {
                string selected = action;
                RectTransform row = CreateListRow(
                    scroll.Content,
                    selected.Substring(0, 2).ToUpperInvariant(),
                    selected,
                    AllianceDetail(selected),
                    ">",
                    resolvedTheme.Colors.SurfaceRaised,
                    () =>
                    {
                        if (selected == "Territory")
                            OpenScreen(KingdomScreenId.Territory);
                        else
                            SetStatus($"Alliance section: {selected}.");
                    });
                ScreenUiBuilder.SetLayout(row, 118f);
            }

            TMP_Text recommended = UiFactory.CreateText(
                scroll.Content,
                "RecommendedTitle",
                "RECOMMENDED ALLIANCES",
                resolvedTheme.Typography.Caption,
                resolvedTheme.Colors.GoldBright,
                TextAlignmentOptions.MidlineLeft,
                FontStyles.Bold);
            ScreenUiBuilder.SetLayout(recommended, 72f);
            foreach (AllianceEntry entry in PrototypeContent.Alliances)
            {
                AllianceEntry selected = entry;
                RectTransform row = CreateListRow(
                    scroll.Content,
                    entry.Tag,
                    $"({entry.Tag}) {entry.Name}",
                    $"Leader {entry.Leader}  •  {entry.Members}/{entry.Capacity}  •  {entry.Language}",
                    entry.Power,
                    resolvedTheme.Colors.Surface,
                    () => SetStatus($"Viewing {selected.Name}. Application requires confirmation."));
                ScreenUiBuilder.SetLayout(row, 126f);
            }
        }

        private void BuildMoreScreen()
        {
            UiFactory.CreatePanel(contentRoot, "MoreBackdrop", resolvedTheme.Colors.Background);
            CreateScreenTitle("Command Hall", "HERO, ITEMS, TROOPS, AND ACCOUNT", false);
            ScrollAreaView scroll = UiFactory.CreateScrollArea(contentRoot, "CommandMenu", resolvedTheme);
            UiFactory.SetAnchors(scroll.Root, new Vector2(0.03f, 0.03f), new Vector2(0.97f, 0.86f), Vector4.zero);

            CreateMenuDestination(scroll.Content, "HR", "Hero and equipment", "Commander levels, equipment, and skills", KingdomScreenId.Hero);
            CreateMenuDestination(scroll.Content, "IT", "Inventory", "Resources, boosts, rewards, and crafting", KingdomScreenId.Inventory);
            CreateMenuDestination(scroll.Content, "TR", "Train troops", "Choose a unit rank and queue training", KingdomScreenId.Training);
            CreateListRow(scroll.Content, "EV", "Events", "Beacon Trials and throne schedules", ">", resolvedTheme.Colors.SurfaceRaised, () => SetStatus("Events are server-configured."));
            CreateListRow(scroll.Content, "ST", "Settings", "Audio, notifications, accessibility, and account", ">", resolvedTheme.Colors.SurfaceRaised, () => SetStatus("Settings prototype selected."));
        }

        private void BuildTrainingScreen()
        {
            ScreenUiBuilder.CreateBackground(contentRoot, "TrainingBackdrop", cityBackdrop, resolvedTheme.Colors.Background);
            UiFactory.CreatePanel(contentRoot, "TrainingScrim", new Color(0.02f, 0.04f, 0.05f, 0.66f));
            CreateScreenTitle("Dawn Sentinels", "BARRACKS  •  UNIT TRAINING", true);

            RectTransform ranks = UiFactory.CreatePanel(
                contentRoot,
                "Ranks",
                Color.clear,
                new Vector2(0.05f, 0.39f),
                new Vector2(0.95f, 0.75f),
                Vector4.zero);
            ScreenUiBuilder.AddVerticalLayout(ranks, 12f);
            foreach (string rank in PrototypeContent.UnitRanks)
            {
                string selected = rank;
                ButtonView button = UiFactory.CreateButton(ranks, $"Rank_{rank[0]}", rank, resolvedTheme, ButtonTone.Quiet);
                button.Button.onClick.AddListener(() => SetStatus($"Selected {selected}."));
            }

            RectTransform order = UiFactory.CreatePanel(
                contentRoot,
                "TrainingOrder",
                resolvedTheme.Colors.Overlay,
                new Vector2(0.05f, 0.04f),
                new Vector2(0.95f, 0.35f),
                Vector4.zero);
            ScreenUiBuilder.AddVerticalLayout(order, 16f, new RectOffset(28, 28, 24, 24));
            TMP_Text cost = UiFactory.CreateText(
                order,
                "Cost",
                "TRAIN 770    •    FOOD 52.3K    •    WOOD 139.3K    •    TIME 15:22",
                resolvedTheme.Typography.Caption,
                resolvedTheme.Colors.TextPrimary,
                TextAlignmentOptions.Center,
                FontStyles.Bold);
            ScreenUiBuilder.SetLayout(cost, 70f);
            ProgressBarView amount = UiFactory.CreateProgressBar(order, "Amount", "UNIT CAPACITY  770 / 1000", 0.77f, resolvedTheme);
            amount.Fill.color = resolvedTheme.Colors.Gold;
            ScreenUiBuilder.SetLayout(amount.Root, 72f);
            ButtonView train = UiFactory.CreateButton(order, "Train", "START TRAINING", resolvedTheme);
            train.Button.onClick.AddListener(() => SetStatus("Training request prepared. The server will validate cost and queue capacity."));
            ButtonView back = UiFactory.CreateButton(order, "Back", "BACK TO COMMAND HALL", resolvedTheme, ButtonTone.Quiet);
            back.Button.onClick.AddListener(() => OpenScreen(KingdomScreenId.More));
        }

        private void BuildTerritoryScreen()
        {
            ScreenUiBuilder.CreateBackground(contentRoot, "TerritoryMap", worldBackdrop, resolvedTheme.Colors.Background);
            UiFactory.CreatePanel(contentRoot, "TerritoryWash", new Color(0.08f, 0.07f, 0.04f, 0.48f));
            CreateScreenTitle("Alliance Territory", "AVERON PACT  •  WARD ACTIVE 4D 09H", true);

            Image zone = UiFactory.CreateImage(contentRoot, "ControlledZone", new Color(0.08f, 0.52f, 0.48f, 0.34f));
            AnchorBox(zone.rectTransform, new Vector2(0.5f, 0.49f), new Vector2(650f, 650f));
            zone.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            zone.raycastTarget = false;

            CreateTerritoryNode(new Vector2(0.5f, 0.53f), "AVR  •  Citadel", resolvedTheme.Colors.Gold);
            CreateTerritoryNode(new Vector2(0.31f, 0.63f), "North Beacon", resolvedTheme.Colors.RoyalBlue);
            CreateTerritoryNode(new Vector2(0.69f, 0.63f), "Tide Gate", resolvedTheme.Colors.RoyalBlue);
            CreateTerritoryNode(new Vector2(0.5f, 0.34f), "Southwatch", resolvedTheme.Colors.RoyalBlue);

            RectTransform legend = UiFactory.CreatePanel(
                contentRoot,
                "TerritoryLegend",
                resolvedTheme.Colors.Overlay,
                new Vector2(0.08f, 0.025f),
                new Vector2(0.92f, 0.14f),
                Vector4.zero);
            TMP_Text legendText = UiFactory.CreateText(
                legend,
                "Legend",
                "CONTROL  4 STRUCTURES    •    INFLUENCE  68%    •    NEXT UPKEEP  09:12",
                resolvedTheme.Typography.Caption,
                resolvedTheme.Colors.TextPrimary,
                TextAlignmentOptions.Top,
                FontStyles.Bold);
            UiFactory.SetAnchors(legendText.rectTransform, new Vector2(0.03f, 0.42f), new Vector2(0.97f, 0.94f), Vector4.zero);
            ButtonView back = UiFactory.CreateButton(legend, "Back", "BACK TO ALLIANCE", resolvedTheme, ButtonTone.Quiet);
            UiFactory.SetAnchors(back.Root, new Vector2(0.25f, 0.03f), new Vector2(0.75f, 0.42f), Vector4.zero);
            back.Button.onClick.AddListener(() => OpenScreen(KingdomScreenId.Alliance));
        }

        private void CreateTerritoryNode(Vector2 position, string label, Color color)
        {
            ButtonView node = UiFactory.CreateButton(contentRoot, $"Territory_{label}", label, resolvedTheme, ButtonTone.Quiet);
            node.Background.color = color;
            node.Label.fontSize = 21f;
            AnchorBox(node.Root, position, new Vector2(260f, 88f));
            node.Button.onClick.AddListener(() => SetStatus($"Territory structure selected: {label}."));
        }

        private void CreateEquipmentColumn(Vector2 min, Vector2 max, IEnumerable<string> slots)
        {
            RectTransform column = UiFactory.CreatePanel(contentRoot, "EquipmentColumn", Color.clear, min, max, Vector4.zero);
            ScreenUiBuilder.AddVerticalLayout(column, 14f);
            foreach (string slot in slots)
            {
                string selected = slot;
                ButtonView button = UiFactory.CreateButton(column, $"Slot_{slot}", slot, resolvedTheme, ButtonTone.Quiet);
                button.Label.fontSize = 22f;
                button.Button.onClick.AddListener(() => SetStatus($"Equipment slot: {selected}."));
            }
        }

        private void CreateInventoryTile(Transform parent, InventoryEntry entry)
        {
            Color rarity = entry.Rarity switch
            {
                >= 5 => resolvedTheme.Colors.Gold,
                4 => resolvedTheme.Colors.RoyalBlue,
                3 => new Color(0.42f, 0.29f, 0.58f, 1f),
                _ => resolvedTheme.Colors.SurfaceRaised,
            };
            ButtonView tile = UiFactory.CreateButton(parent, $"Item_{entry.Code}", string.Empty, resolvedTheme, ButtonTone.Quiet);
            tile.Background.color = rarity;
            ScreenUiBuilder.SetLayout(tile.Root, 220f, -1f, 1f);
            ScreenUiBuilder.CreateBadge(tile.Root, entry.Code, resolvedTheme, resolvedTheme.Colors.Overlay, 78f);
            RectTransform badge = tile.Root.GetChild(tile.Root.childCount - 1) as RectTransform;
            AnchorBox(badge, new Vector2(0.5f, 0.68f), new Vector2(78f, 78f));
            tile.Label.text = $"{entry.Name}\n{entry.Category}  •  x{entry.Quantity}";
            tile.Label.fontSize = 22f;
            tile.Label.alignment = TextAlignmentOptions.Bottom;
            UiFactory.SetAnchors(tile.Label.rectTransform, Vector2.zero, Vector2.one, new Vector4(10f, 10f, 10f, 92f));
            tile.Button.onClick.AddListener(() => SetStatus($"{entry.Name}: {entry.Quantity} available."));
        }

        private RectTransform CreateListRow(
            Transform parent,
            string code,
            string title,
            string detail,
            string trailing,
            Color color,
            Action onClick)
        {
            ButtonView button = UiFactory.CreateButton(parent, $"Row_{code}_{title}", string.Empty, resolvedTheme, ButtonTone.Quiet);
            button.Background.color = color;
            button.Label.gameObject.SetActive(false);
            ScreenUiBuilder.SetLayout(button.Root, 120f);

            ScreenUiBuilder.CreateBadge(button.Root, code, resolvedTheme, resolvedTheme.Colors.RoyalBlue, 76f);
            RectTransform badge = button.Root.GetChild(button.Root.childCount - 1) as RectTransform;
            AnchorBox(badge, new Vector2(0.07f, 0.5f), new Vector2(76f, 76f));

            TMP_Text titleText = UiFactory.CreateText(
                button.Root,
                "Title",
                title,
                resolvedTheme.Typography.Body,
                resolvedTheme.Colors.TextPrimary,
                TextAlignmentOptions.BottomLeft,
                FontStyles.Bold);
            UiFactory.SetAnchors(titleText.rectTransform, new Vector2(0.15f, 0.43f), new Vector2(0.79f, 0.94f), Vector4.zero);
            TMP_Text detailText = UiFactory.CreateText(
                button.Root,
                "Detail",
                detail,
                resolvedTheme.Typography.Caption,
                resolvedTheme.Colors.TextSecondary,
                TextAlignmentOptions.TopLeft);
            UiFactory.SetAnchors(detailText.rectTransform, new Vector2(0.15f, 0.06f), new Vector2(0.82f, 0.5f), Vector4.zero);
            TMP_Text trailingText = UiFactory.CreateText(
                button.Root,
                "Trailing",
                trailing,
                resolvedTheme.Typography.Caption,
                resolvedTheme.Colors.GoldBright,
                TextAlignmentOptions.Center,
                FontStyles.Bold);
            UiFactory.SetAnchors(trailingText.rectTransform, new Vector2(0.82f, 0.08f), new Vector2(0.98f, 0.92f), Vector4.zero);
            button.Button.onClick.AddListener(() => onClick?.Invoke());
            return button.Root;
        }

        private void CreateMenuDestination(Transform parent, string code, string title, string detail, KingdomScreenId destination)
        {
            RectTransform row = CreateListRow(
                parent,
                code,
                title,
                detail,
                ">",
                resolvedTheme.Colors.SurfaceRaised,
                () => OpenScreen(destination));
            ScreenUiBuilder.SetLayout(row, 132f);
        }

        private void CreateCompactAction(Transform parent, string code, string detail, Action action)
        {
            ButtonView button = UiFactory.CreateButton(parent, $"Action_{code}", $"{code}\n{detail}", resolvedTheme, ButtonTone.Quiet);
            button.Label.fontSize = 21f;
            button.Button.onClick.AddListener(() => action?.Invoke());
        }

        private void CreateEventAction(Transform parent, Sprite icon, string label, string detail, Action action)
        {
            ButtonView button = UiFactory.CreateIconButton(
                parent,
                $"Event_{label}",
                $"{label}  {detail}",
                icon,
                resolvedTheme);
            ScreenUiBuilder.SetLayout(button.Root, 142f);
            button.Label.fontSize = 18f;
            button.Button.onClick.AddListener(() => action?.Invoke());
        }

        private void CreateCityMarker(Vector2 position, string label, int level, Action action)
        {
            ButtonView marker = UiFactory.CreateButton(
                contentRoot,
                $"CityMarker_{label}",
                $"{label}  •  {level}",
                resolvedTheme,
                ButtonTone.Quiet);
            marker.Background.color = new Color(0.03f, 0.06f, 0.07f, 0.82f);
            marker.Label.fontSize = 20f;
            AnchorBox(marker.Root, position, new Vector2(230f, 68f));
            marker.Button.onClick.AddListener(() => action?.Invoke());
        }

        private BootstrapBuilding[] GetCityBuildings()
        {
            BootstrapBuilding[] buildings = session?.Bootstrap?.city?.buildings;
            if (buildings != null && buildings.Length > 0)
                return buildings;

            return new[]
            {
                new BootstrapBuilding { id = string.Empty, slot_key = "palace", building_type = "palace", level = 1 },
                new BootstrapBuilding { id = string.Empty, slot_key = "farm_1", building_type = "farm", level = 1 },
                new BootstrapBuilding { id = string.Empty, slot_key = "barracks", building_type = "barracks", level = 1 },
            };
        }

        private static Dictionary<string, BootstrapBuilding> CreateBuildingsBySlot(BootstrapBuilding[] buildings)
        {
            Dictionary<string, BootstrapBuilding> bySlot = new(StringComparer.OrdinalIgnoreCase);
            foreach (BootstrapBuilding building in buildings)
            {
                if (!string.IsNullOrWhiteSpace(building.slot_key))
                    bySlot[building.slot_key] = building;
            }

            return bySlot;
        }

        private void CreateCityGridGuide(RectTransform cityGrid)
        {
            foreach (CitySlotDefinition slot in CitySlots)
            {
                Image cell = UiFactory.CreateImage(cityGrid, $"GridCell_{slot.SlotKey}", new Color(0.92f, 0.78f, 0.48f, 0.18f));
                cell.raycastTarget = false;
                AnchorBox(cell.rectTransform, slot.Position, new Vector2(116f, 82f));
                cell.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            }
        }

        private void CreateLockedCitySlot(RectTransform cityGrid, CitySlotDefinition slot)
        {
            ButtonView locked = UiFactory.CreateButton(
                cityGrid,
                $"LockedSlot_{slot.SlotKey}",
                $"{slot.Label}\nLOCKED",
                resolvedTheme,
                ButtonTone.Quiet);
            locked.Background.color = new Color(0.02f, 0.04f, 0.05f, 0.52f);
            locked.Label.color = resolvedTheme.Colors.TextSecondary;
            locked.Label.fontSize = 17f;
            AnchorBox(locked.Root, slot.Position, new Vector2(190f, 66f));
            locked.Button.onClick.AddListener(() => SetStatus($"{slot.Label} unlocks through castle progression and quests."));
        }

        private void CreateCityMarker(RectTransform cityGrid, CitySlotDefinition slot, BootstrapBuilding building)
        {
            string displayName = DisplayBuildingName(building.building_type);
            ButtonView marker = UiFactory.CreateButton(
                cityGrid,
                $"CityMarker_{slot.SlotKey}",
                $"{displayName}\nLv {building.level}",
                resolvedTheme,
                ButtonTone.Quiet);
            marker.Background.color = BuildingColor(building.building_type);
            marker.Label.fontSize = 20f;
            AnchorBox(marker.Root, slot.Position, new Vector2(218f, 82f));
            marker.Button.onClick.AddListener(() => ShowBuildingDetails(building, slot));

            CityBuildingTile tile = marker.Root.gameObject.AddComponent<CityBuildingTile>();
            tile.Configure(
                cityGrid,
                CitySnapPoints(),
                slot.Position,
                snappedPosition => SetStatus($"{displayName} moved to {FindSlotLabel(snappedPosition)}. Layout saving still needs a server endpoint."));
        }

        private void ShowBuildingDetails(BootstrapBuilding building, CitySlotDefinition slot)
        {
            if (cityDetailPanel != null)
                Destroy(cityDetailPanel.gameObject);

            string displayName = DisplayBuildingName(building.building_type);
            BuildingUpgradePreview upgrade = QuoteBuildingUpgrade(building.level);
            BootstrapQueueJob activeQueue = ActiveBuildingQueue();
            bool queueBusy = activeQueue != null;
            bool currentBuildingQueued = queueBusy && activeQueue.ref_id == building.id;
            bool canAfford = HasEnoughResources(upgrade);
            bool canStartUpgrade =
                !upgradeSubmitting &&
                !queueBusy &&
                canAfford &&
                !string.IsNullOrWhiteSpace(building.id);

            cityDetailPanel = UiFactory.CreatePanel(
                contentRoot,
                "BuildingDetails",
                resolvedTheme.Colors.Overlay,
                new Vector2(0.04f, 0.02f),
                new Vector2(0.96f, 0.35f),
                Vector4.zero);
            ScreenUiBuilder.AddVerticalLayout(cityDetailPanel, 10f, new RectOffset(24, 24, 16, 16));

            TMP_Text title = UiFactory.CreateText(
                cityDetailPanel,
                "Title",
                $"{displayName}  â€¢  Level {building.level}",
                resolvedTheme.Typography.Title,
                resolvedTheme.Colors.TextPrimary,
                TextAlignmentOptions.Left,
                FontStyles.Bold);
            ScreenUiBuilder.SetLayout(title, 52f);

            TMP_Text detail = UiFactory.CreateText(
                cityDetailPanel,
                "Detail",
                $"{BuildingRole(building.building_type)}\n{BuildingStat(building.building_type, building.level)}\nNext: {BuildingStat(building.building_type, upgrade.TargetLevel)}",
                resolvedTheme.Typography.Caption,
                resolvedTheme.Colors.TextPrimary,
                TextAlignmentOptions.TopLeft);
            ScreenUiBuilder.SetLayout(detail, 112f);

            string queueText = currentBuildingQueued
                ? $"UPGRADING NOW  â€¢  finishes in {QueueRemaining(activeQueue)}"
                : queueBusy
                    ? $"BUILDER BUSY  â€¢  finishes in {QueueRemaining(activeQueue)}"
                    : $"UPGRADE Lv {upgrade.TargetLevel}  â€¢  FD {FormatResourceAmount(upgrade.FoodCost)}  â€¢  WD {FormatResourceAmount(upgrade.WoodCost)}  â€¢  {FormatDuration(upgrade.DurationSeconds)}";
            if (!canAfford && !queueBusy)
                queueText += "  â€¢  NEED MORE RESOURCES";

            TMP_Text cost = UiFactory.CreateText(
                cityDetailPanel,
                "UpgradeCost",
                queueText,
                resolvedTheme.Typography.Caption,
                resolvedTheme.Colors.GoldBright,
                TextAlignmentOptions.Center,
                FontStyles.Bold);
            ScreenUiBuilder.SetLayout(cost, 44f);

            RectTransform actions = UiFactory.CreatePanel(cityDetailPanel, "Actions", Color.clear);
            ScreenUiBuilder.SetLayout(actions, 76f);
            ScreenUiBuilder.AddHorizontalLayout(actions, 10f);

            ButtonView upgradeButton = UiFactory.CreateButton(actions, "Upgrade", $"UPGRADE Lv {upgrade.TargetLevel}", resolvedTheme);
            upgradeButton.Button.interactable = canStartUpgrade;
            upgradeButton.Button.onClick.AddListener(() => BeginBuildingUpgrade(building));

            ButtonView secondary = UiFactory.CreateButton(
                actions,
                "Secondary",
                building.building_type == "barracks" ? "TRAIN TROOPS" : "MOVE MODE",
                resolvedTheme,
                ButtonTone.Secondary);
            secondary.Button.onClick.AddListener(() =>
            {
                if (building.building_type == "barracks")
                    OpenScreen(KingdomScreenId.Training);
                else
                    SetStatus($"Drag {displayName} between highlighted slots. Server persistence comes next.");
            });

            ButtonView close = UiFactory.CreateButton(actions, "Close", "CLOSE", resolvedTheme, ButtonTone.Quiet);
            close.Button.onClick.AddListener(() =>
            {
                if (cityDetailPanel != null)
                    Destroy(cityDetailPanel.gameObject);
                cityDetailPanel = null;
            });

            SetStatus($"{displayName} selected on {slot.Label}.");
        }

        private void BeginBuildingUpgrade(BootstrapBuilding building)
        {
            if (upgradeSubmitting)
                return;

            if (string.IsNullOrWhiteSpace(building.id))
            {
                SetStatus("Start from the Boot scene and login before sending upgrade commands.");
                return;
            }

            ApiClient apiClient;
            GameSession liveSession;
            try
            {
                apiClient = Bootstrapper.Resolve<ApiClient>();
                liveSession = Bootstrapper.Resolve<GameSession>();
            }
            catch (InvalidOperationException)
            {
                SetStatus("Game services are not ready. Open the Boot scene first.");
                return;
            }

            upgradeSubmitting = true;
            SetStatus($"Starting {DisplayBuildingName(building.building_type)} upgrade...");
            StartCoroutine(StartBuildingUpgradeFlow(apiClient, liveSession, building));
        }

        private IEnumerator StartBuildingUpgradeFlow(
            ApiClient apiClient,
            GameSession liveSession,
            BootstrapBuilding building)
        {
            string startError = null;
            yield return apiClient.StartBuildingUpgrade(
                building.id,
                _ => { },
                error => startError = error);

            if (!string.IsNullOrWhiteSpace(startError))
            {
                upgradeSubmitting = false;
                SetStatus(startError);
                yield break;
            }

            string refreshError = null;
            yield return apiClient.GetBootstrap(
                bootstrap => liveSession.ApplyBootstrap(bootstrap),
                error => refreshError = error);

            upgradeSubmitting = false;
            if (!string.IsNullOrWhiteSpace(refreshError))
            {
                SetStatus($"Upgrade queued, but refresh failed: {refreshError}");
                RefreshHud();
                yield break;
            }

            OpenScreen(KingdomScreenId.City);
            SetStatus($"{DisplayBuildingName(building.building_type)} upgrade queued. Server worker will complete it.");
        }

        private BootstrapQueueJob ActiveBuildingQueue()
        {
            BootstrapQueueJob[] queues = session?.Bootstrap?.city?.active_queues;
            if (queues == null)
                return null;

            foreach (BootstrapQueueJob queue in queues)
            {
                if (queue.job_type == "building_upgrade" && (queue.status == "pending" || queue.status == "processing"))
                    return queue;
            }

            return null;
        }

        private bool HasEnoughResources(BuildingUpgradePreview upgrade)
        {
            BootstrapResources resources = session?.Bootstrap?.city?.resources;
            if (resources == null)
                return false;

            return long.TryParse(resources.food, out long food) &&
                   long.TryParse(resources.wood, out long wood) &&
                   food >= upgrade.FoodCost &&
                   wood >= upgrade.WoodCost;
        }

        private Color BuildingColor(string buildingType)
        {
            return buildingType switch
            {
                "palace" => new Color(0.52f, 0.34f, 0.11f, 0.92f),
                "farm" => new Color(0.13f, 0.34f, 0.18f, 0.88f),
                "barracks" => new Color(0.36f, 0.12f, 0.1f, 0.9f),
                _ => resolvedTheme.Colors.SurfaceRaised,
            };
        }

        private static BuildingUpgradePreview QuoteBuildingUpgrade(int currentLevel)
        {
            int targetLevel = Mathf.Clamp(currentLevel + 1, 1, 40);
            long levelSquared = (long)targetLevel * targetLevel;
            return new BuildingUpgradePreview(
                targetLevel,
                1_000L * levelSquared,
                1_000L * levelSquared,
                60 * targetLevel * targetLevel);
        }

        private static string DisplayBuildingName(string buildingType)
        {
            return buildingType switch
            {
                "palace" => "Castle Keep",
                "farm" => "Farm",
                "barracks" => "Barracks",
                "wall" => "Wall",
                "warehouse" => "Warehouse",
                "hospital" => "Hospital",
                "academy" => "Academy",
                "range" => "Range",
                _ => TitleCase(buildingType),
            };
        }

        private static string BuildingRole(string buildingType)
        {
            return buildingType switch
            {
                "palace" => "Primary progression gate. Higher levels unlock plots, queues, army tiers, and world systems.",
                "farm" => "Resource production building. Farms generate food for construction, upkeep, and troop training.",
                "barracks" => "Army production building. Barracks train infantry and unlock stronger troop tiers over time.",
                _ => "City structure. Upgrades increase power and unlock stronger kingdom systems.",
            };
        }

        private static string BuildingStat(string buildingType, int level)
        {
            return buildingType switch
            {
                "palace" => $"Power +{level * 120}  â€¢  city unlock tier {Mathf.Max(1, level)}",
                "farm" => $"Food production {FormatResourceAmount(level * 900L)} / hour  â€¢  storage buffer {FormatResourceAmount(level * 2_500L)}",
                "barracks" => $"Training capacity {FormatResourceAmount(level * 250L)}  â€¢  infantry tier T{Mathf.Clamp((level + 3) / 4, 1, 10)}",
                _ => $"Power +{level * 80}",
            };
        }

        private static Vector2[] CitySnapPoints()
        {
            Vector2[] points = new Vector2[CitySlots.Length];
            for (int index = 0; index < CitySlots.Length; index++)
                points[index] = CitySlots[index].Position;
            return points;
        }

        private static string FindSlotLabel(Vector2 position)
        {
            CitySlotDefinition closest = CitySlots[0];
            float closestDistance = Vector2.SqrMagnitude(position - closest.Position);
            for (int index = 1; index < CitySlots.Length; index++)
            {
                float distance = Vector2.SqrMagnitude(position - CitySlots[index].Position);
                if (distance >= closestDistance)
                    continue;

                closest = CitySlots[index];
                closestDistance = distance;
            }

            return closest.Label;
        }

        private static string QueueRemaining(BootstrapQueueJob queue)
        {
            if (queue == null || !DateTime.TryParse(queue.finish_at, out DateTime finishAt))
                return "soon";

            TimeSpan remaining = finishAt.ToUniversalTime() - DateTime.UtcNow;
            if (remaining <= TimeSpan.Zero)
                return "soon";

            return FormatDuration(Mathf.CeilToInt((float)remaining.TotalSeconds));
        }

        private static string FormatDuration(int seconds)
        {
            if (seconds >= 3600)
                return $"{seconds / 3600}h {(seconds % 3600) / 60:00}m";
            return $"{seconds / 60:00}:{seconds % 60:00}";
        }

        private static string FormatResourceAmount(long value)
        {
            if (value >= 1_000_000_000L)
                return $"{value / 1_000_000_000f:0.#}B";
            if (value >= 1_000_000L)
                return $"{value / 1_000_000f:0.#}M";
            if (value >= 1_000L)
                return $"{value / 1_000f:0.#}K";
            return value.ToString();
        }

        private void CreateMapMarker(Transform parent, Vector2 position, string label, Action action)
        {
            ButtonView marker = UiFactory.CreateButton(parent, $"Marker_{label}", label, resolvedTheme, ButtonTone.Secondary);
            marker.Label.fontSize = 23f;
            AnchorBox(marker.Root, position, new Vector2(300f, 92f));
            marker.Button.onClick.AddListener(() => action?.Invoke());
        }

        private void CreateScreenTitle(string title, string subtitle, bool overArtwork, bool darkText = false)
        {
            Color background = darkText
                ? resolvedTheme.Colors.ParchmentDark
                : overArtwork
                    ? resolvedTheme.Colors.Overlay
                    : resolvedTheme.Colors.Surface;
            Color textColor = darkText ? resolvedTheme.Colors.TextOnLight : resolvedTheme.Colors.TextPrimary;
            RectTransform header = UiFactory.CreatePanel(
                contentRoot,
                "ScreenHeader",
                background,
                new Vector2(0f, 0.88f),
                Vector2.one,
                Vector4.zero);
            TMP_Text heading = UiFactory.CreateText(
                header,
                "Heading",
                title,
                resolvedTheme.Typography.Title,
                textColor,
                TextAlignmentOptions.BottomLeft,
                FontStyles.Bold);
            UiFactory.SetAnchors(heading.rectTransform, new Vector2(0.04f, 0.39f), new Vector2(0.96f, 0.96f), Vector4.zero);
            TMP_Text caption = UiFactory.CreateText(
                header,
                "Caption",
                subtitle,
                resolvedTheme.Typography.Caption,
                darkText ? resolvedTheme.Colors.TextOnLight : resolvedTheme.Colors.GoldBright,
                TextAlignmentOptions.TopLeft,
                FontStyles.Bold);
            UiFactory.SetAnchors(caption.rectTransform, new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.42f), Vector4.zero);
        }

        private void SetStatus(string value)
        {
            if (statusText != null)
                statusText.text = $"Kingdom 1 • {value}";
        }

        private static void AnchorBox(RectTransform rect, Vector2 normalizedCenter, Vector2 size)
        {
            rect.anchorMin = normalizedCenter;
            rect.anchorMax = normalizedCenter;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;
        }

        private static string TitleCase(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "Building";
            string normalized = value.Replace('_', ' ');
            return char.ToUpperInvariant(normalized[0]) + normalized.Substring(1);
        }

        private static string AllianceDetail(string action)
        {
            return action switch
            {
                "Territory" => "View structures, borders, and control points",
                "Rallies" => "Coordinate attacks and reinforcements",
                "Alliance help" => "Reduce ally construction and research timers",
                "Missions" => "Complete shared weekly objectives",
                "Research" => "Contribute resources to permanent bonuses",
                _ => "Roles, activity, invitations, and moderation",
            };
        }
    }

    internal readonly struct CitySlotDefinition
    {
        public CitySlotDefinition(string slotKey, string buildingType, Vector2 position, string label)
        {
            SlotKey = slotKey;
            BuildingType = buildingType;
            Position = position;
            Label = label;
        }

        public string SlotKey { get; }
        public string BuildingType { get; }
        public Vector2 Position { get; }
        public string Label { get; }
    }

    internal readonly struct BuildingUpgradePreview
    {
        public BuildingUpgradePreview(int targetLevel, long foodCost, long woodCost, int durationSeconds)
        {
            TargetLevel = targetLevel;
            FoodCost = foodCost;
            WoodCost = woodCost;
            DurationSeconds = durationSeconds;
        }

        public int TargetLevel { get; }
        public long FoodCost { get; }
        public long WoodCost { get; }
        public int DurationSeconds { get; }
    }
}
