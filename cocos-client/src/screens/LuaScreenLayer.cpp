#include "screens/LuaScreenLayer.h"

#include "app/SceneRouter.h"
#include "base/CCUserDefault.h"
#include "scripting/lua-bindings/manual/CCLuaEngine.h"
#include "state/GameSession.h"
#include "ui/CocosGUI.h"

extern "C"
{
#include "lua.h"
#include "lauxlib.h"
}

#include <algorithm>
#include <cmath>
#include <cstdint>
#include <ctime>
#include <functional>
#include <iomanip>
#include <limits>
#include <sstream>
#include <string>
#include <utility>

namespace Kingdom
{
namespace
{
struct CityBuildingDefinition
{
    std::string key;
    std::string type;
    std::string name;
    std::string role;
    std::string asset;
    std::string primaryAction;
    std::string secondaryAction;
    int requiredPalaceLevel = 1;
    cocos2d::Vec2 position;
    float width = 160.0f;
};

constexpr int MaximumBuildingLevel = 40;

const std::vector<CityBuildingDefinition>& cityBuildingDefinitions()
{
    static const std::vector<CityBuildingDefinition> definitions = {
        { "palace", "palace", "Town Hall", "City level gate and command hub", "art/city/keep.png", "upgrade", "buffs", 1, { 1200, 760 }, 500 },
        { "wall", "wall", "Harbor Wall", "City durability, traps, and defense state", "art/city/wall.png", "defense", "upgrade", 1, { 1200, 520 }, 520 },
        { "farm_1", "farm", "Farm", "Food production", "art/city/farm.png", "collect", "upgrade", 1, { 455, 595 }, 118 },
        { "farm_2", "farm", "Farm", "Food production", "art/city/farm.png", "collect", "upgrade", 1, { 405, 455 }, 115 },
        { "lumberyard_1", "lumberyard", "Lumberyard", "Wood production", "art/city/lumberyard.png", "collect", "upgrade", 1, { 1855, 590 }, 120 },
        { "lumberyard_2", "lumberyard", "Lumberyard", "Wood production", "art/city/lumberyard.png", "collect", "upgrade", 1, { 1985, 455 }, 115 },
        { "barracks", "barracks", "Barracks", "Infantry training", "art/city/barracks.png", "train", "upgrade", 1, { 725, 580 }, 140 },
        { "stable", "stable", "Elephant Stable", "Cavalry training", "art/city/stable.png", "train", "upgrade", 1, { 1675, 570 }, 140 },
        { "range", "range", "Range", "Archer training", "art/city/range.png", "train", "upgrade", 1, { 625, 390 }, 130 },
        { "workshop", "workshop", "Workshop", "Siege training", "art/city/workshop.png", "train", "upgrade", 1, { 1775, 390 }, 132 },
        { "drill_grounds", "drill_grounds", "Drill Grounds", "March size and army staging", "art/city/drill_grounds.png", "assign", "upgrade", 3, { 1200, 365 }, 160 },
        { "academy", "academy", "Academy", "Research economy, military, defense, and monsters", "art/city/academy.png", "research", "upgrade", 4, { 870, 465 }, 132 },
        { "hospital", "hospital", "Hospital", "Wounded recovery and heal queue", "art/city/hospital.png", "heal", "upgrade", 4, { 1530, 465 }, 132 },
        { "tavern", "tavern", "Tavern", "Hero recruitment and daily summon", "art/city/tavern.png", "recruit", "upgrade", 5, { 250, 760 }, 120 },
        { "embassy", "embassy", "Embassy", "Alliance help and reinforcement cap", "art/city/embassy.png", "help", "upgrade", 6, { 2150, 755 }, 122 },
        { "watchtower", "watchtower", "Watchtower", "Incoming march alerts and scout defense", "art/city/watchtower.png", "inspect", "upgrade", 7, { 520, 760 }, 120 },
        { "forge", "forge", "Forge", "Equipment crafting and refinement", "art/city/forge.png", "craft", "upgrade", 8, { 2090, 330 }, 122 },
        { "iron_mine", "iron_mine", "Iron Mine", "Iron production", "art/city/iron_mine.png", "collect", "upgrade", 9, { 255, 330 }, 122 },
        { "market", "market", "Market", "Resource trade and alliance logistics", "art/city/market.png", "trade", "upgrade", 10, { 960, 310 }, 126 },
        { "hall_of_war", "hall_of_war", "Hall of War", "Rally capacity and coordinated attacks", "art/city/hall_of_war.png", "rally", "upgrade", 12, { 1440, 310 }, 138 },
        { "silver_mine", "silver_mine", "Silver Mine", "Silver production", "art/city/silver_mine.png", "collect", "upgrade", 15, { 2005, 620 }, 120 },
        { "warehouse", "warehouse", "Warehouse", "Protected resource storage", "art/city/warehouse.png", "storage", "upgrade", 1, { 760, 310 }, 125 },
    };
    return definitions;
}

std::string formatLevel(int level)
{
    return "Lv " + std::to_string(std::max(0, level));
}

int upgradeTargetLevel(int currentLevel)
{
    return std::min(MaximumBuildingLevel, std::max(0, currentLevel) + 1);
}

std::uint64_t quotedUpgradeResourceCost(int currentLevel)
{
    const auto targetLevel = static_cast<std::uint64_t>(upgradeTargetLevel(currentLevel));
    return 1000ULL * targetLevel * targetLevel;
}

std::string formatQuantity(std::uint64_t value)
{
    const auto raw = std::to_string(value);
    std::string result;
    result.reserve(raw.size() + raw.size() / 3);
    for (std::size_t index = 0; index < raw.size(); ++index)
    {
        if (index > 0 && (raw.size() - index) % 3 == 0)
        {
            result.push_back(',');
        }
        result.push_back(raw[index]);
    }
    return result;
}

std::string formatCompactQuantity(std::uint64_t value)
{
    if (value >= 1000000000ULL)
    {
        std::ostringstream stream;
        stream << std::fixed << std::setprecision(value >= 10000000000ULL ? 0 : 1) << (static_cast<double>(value) / 1000000000.0) << "B";
        return stream.str();
    }
    if (value >= 1000000ULL)
    {
        std::ostringstream stream;
        stream << std::fixed << std::setprecision(value >= 10000000ULL ? 0 : 1) << (static_cast<double>(value) / 1000000.0) << "M";
        return stream.str();
    }
    if (value >= 10000ULL)
    {
        return std::to_string(value / 1000ULL) + "K";
    }
    return formatQuantity(value);
}

std::uint64_t resourceAmount(const std::string& value)
{
    try
    {
        return std::stoull(value);
    }
    catch (...)
    {
        return 0;
    }
}

std::string upgradeCostText(int currentLevel)
{
    const auto cost = formatQuantity(quotedUpgradeResourceCost(currentLevel));
    return "Cost: " + cost + " Food  |  " + cost + " Wood";
}

int visualTierForLevel(int level)
{
    return level <= 0 ? 0 : ((level - 1) % 5) + 1;
}

std::string visualTierAsset(int level)
{
    const int tier = visualTierForLevel(level);
    return tier == 0 ? "" : "art/city/states/level_tier_" + std::to_string(tier) + ".png";
}

int palaceArtTierForLevel(int level)
{
    return std::max(1, std::min(6, level));
}

std::string cityBuildingArtAsset(const CityBuildingDefinition& definition, int level)
{
    if (definition.key == "palace")
    {
        return "art/city/palace/palace_level_" + std::to_string(palaceArtTierForLevel(level)) + ".png";
    }
    return definition.asset;
}

float cityBuildingArtWidth(const CityBuildingDefinition& definition, int level)
{
    if (definition.key != "palace")
    {
        return definition.width;
    }

    switch (palaceArtTierForLevel(level))
    {
    case 1:
        return 430.0f;
    case 2:
        return 450.0f;
    case 3:
        return 470.0f;
    case 4:
        return 490.0f;
    case 5:
        return 510.0f;
    case 6:
    default:
        return 535.0f;
    }
}

std::string friendlyProblemTitle(const std::string& body)
{
    const auto problem = Contracts::parseProblemDetails(body);
    if (!problem.has_value())
    {
        return "Connection problem";
    }
    if (problem->code == "BUILD_QUEUE_OCCUPIED")
        return "Construction queue busy";
    if (problem->code == "INSUFFICIENT_RESOURCES")
        return "More resources needed";
    if (problem->code == "PALACE_LEVEL_REQUIRED")
        return "Palace level required";
    if (problem->code == "BUILDING_MAX_LEVEL")
        return "Maximum level reached";
    if (problem->code == "NO_ACTIVE_BUILD_QUEUE")
        return "No upgrade to speed up";
    if (problem->code == "INVALID_TOKEN")
        return "Sign in required";
    return problem->title.empty() ? "Action unavailable" : problem->title;
}

std::string friendlyProblemMessage(const std::string& body)
{
    const auto problem = Contracts::parseProblemDetails(body);
    if (!problem.has_value())
    {
        return "The kingdom server could not complete this request. Check that the local server is running, then try again.";
    }
    if (problem->code == "BUILD_QUEUE_OCCUPIED")
        return "Another building is already upgrading. Finish it or use Speed Up before starting a new upgrade.";
    if (problem->code == "INSUFFICIENT_RESOURCES")
        return "You need more Food and Wood for this upgrade. Gather resources, then try again.";
    if (problem->code == "PALACE_LEVEL_REQUIRED")
        return problem->detail + " Upgrade the Palace first.";
    if (problem->code == "BUILDING_MAX_LEVEL")
        return "This building is already at the current maximum level of 40.";
    if (problem->code == "NO_ACTIVE_BUILD_QUEUE")
        return "There is no active upgrade on this building to finish.";
    if (problem->code == "INVALID_TOKEN")
        return "Your session is no longer valid. Return to the login screen and enter the kingdom again.";
    return problem->detail.empty() ? "The kingdom server could not complete this request. Please try again." : problem->detail;
}

cocos2d::Color3B noticeColor(NoticeTone tone)
{
    switch (tone)
    {
    case NoticeTone::Success:
        return cocos2d::Color3B(132, 220, 128);
    case NoticeTone::Warning:
        return cocos2d::Color3B(242, 191, 91);
    case NoticeTone::Error:
        return cocos2d::Color3B(232, 122, 111);
    case NoticeTone::Info:
    default:
        return cocos2d::Color3B(144, 202, 230);
    }
}

std::string actionText(const std::string& action)
{
    if (action == "upgrade")
        return "Upgrade";
    if (action == "collect")
        return "Collect";
    if (action == "train")
        return "Train Troops";
    if (action == "research")
        return "Research";
    if (action == "heal")
        return "Heal Wounded";
    if (action == "defense")
        return "Defense";
    if (action == "buffs")
        return "City Buffs";
    if (action == "assign")
        return "Assign March";
    if (action == "recruit")
        return "Recruit";
    if (action == "help")
        return "Alliance Help";
    if (action == "inspect")
        return "Inspect";
    if (action == "craft")
        return "Craft";
    if (action == "trade")
        return "Trade";
    if (action == "rally")
        return "Rally";
    if (action == "storage")
        return "Storage";
    if (action == "items")
        return "Items";
    if (action == "hero")
        return "Hero";
    if (action == "alliance")
        return "Alliance";
    if (action == "mail")
        return "Mail";
    if (action == "events")
        return "Events";
    if (action == "offers")
        return "Offers";
    if (action == "mall")
        return "Mall";
    if (action == "season")
        return "Season";
    return action;
}

std::time_t parseIsoUtc(const std::string& value)
{
    if (value.size() < 19)
    {
        return 0;
    }

    std::tm time = {};
    std::istringstream input(value.substr(0, 19));
    input >> std::get_time(&time, "%Y-%m-%dT%H:%M:%S");
    if (input.fail())
    {
        return 0;
    }

#if defined(_WIN32)
    return _mkgmtime(&time);
#else
    return timegm(&time);
#endif
}

int secondsBetween(const std::string& start, const std::string& finish)
{
    const auto startTime = parseIsoUtc(start);
    const auto finishTime = parseIsoUtc(finish);
    if (startTime == 0 || finishTime == 0 || finishTime <= startTime)
    {
        return 0;
    }
    return static_cast<int>(finishTime - startTime);
}

int secondsRemaining(const std::string& finish)
{
    const auto finishTime = parseIsoUtc(finish);
    if (finishTime == 0)
    {
        return 0;
    }
    return std::max(0, static_cast<int>(finishTime - std::time(nullptr)));
}

std::string formatDuration(int totalSeconds)
{
    totalSeconds = std::max(0, totalSeconds);
    const int hours = totalSeconds / 3600;
    const int minutes = (totalSeconds % 3600) / 60;
    const int seconds = totalSeconds % 60;

    std::ostringstream out;
    if (hours > 0)
    {
        out << hours << "h " << minutes << "m";
    }
    else if (minutes > 0)
    {
        out << minutes << "m " << seconds << "s";
    }
    else
    {
        out << seconds << "s";
    }
    return out.str();
}

int quotedUpgradeSeconds(int currentLevel)
{
    const int targetLevel = upgradeTargetLevel(currentLevel);
    return 60 * targetLevel * targetLevel;
}

std::string readStringField(lua_State* state, const char* key)
{
    lua_getfield(state, -1, key);
    std::string value;
    if (lua_isstring(state, -1))
    {
        value = lua_tostring(state, -1);
    }
    lua_pop(state, 1);
    return value;
}

std::vector<std::string> readStringArrayField(lua_State* state, const char* key)
{
    std::vector<std::string> values;
    lua_getfield(state, -1, key);
    if (lua_istable(state, -1))
    {
#if LUA_VERSION_NUM >= 502
        const auto count = static_cast<int>(lua_rawlen(state, -1));
#else
        const auto count = static_cast<int>(lua_objlen(state, -1));
#endif
        for (int i = 1; i <= count; ++i)
        {
            lua_rawgeti(state, -1, i);
            if (lua_isstring(state, -1))
            {
                values.emplace_back(lua_tostring(state, -1));
            }
            lua_pop(state, 1);
        }
    }
    lua_pop(state, 1);
    return values;
}

cocos2d::Color4B backgroundFor(ScreenId screen)
{
    switch (screen)
    {
    case ScreenId::City:
        return cocos2d::Color4B(34, 50, 42, 255);
    case ScreenId::WorldMap:
        return cocos2d::Color4B(27, 48, 64, 255);
    case ScreenId::BattleReport:
        return cocos2d::Color4B(54, 38, 42, 255);
    default:
        return cocos2d::Color4B(30, 34, 40, 255);
    }
}
}

LuaScreenLayer* LuaScreenLayer::create(ScreenId screen)
{
    auto* layer = new LuaScreenLayer(screen);
    if (layer != nullptr && layer->init())
    {
        layer->autorelease();
        return layer;
    }
    delete layer;
    return nullptr;
}

LuaScreenLayer::LuaScreenLayer(ScreenId screen)
    : _screen(screen), _api(GameSession::getInstance().apiBaseUrl())
{
}

bool LuaScreenLayer::init()
{
    cocos2d::log("KingdomCocosClient: initializing screen %s", screenKey().c_str());
    if (!LayerColor::initWithColor(backgroundFor(_screen)))
    {
        cocos2d::log("KingdomCocosClient: LayerColor init failed");
        return false;
    }

    buildLayout(loadDescriptor());
    cocos2d::log("KingdomCocosClient: screen initialized");
    return true;
}

void LuaScreenLayer::onEnter()
{
    LayerColor::onEnter();
    if (_screen == ScreenId::Main)
    {
        connectRealtime();
    }
}

ScreenDescriptor LuaScreenLayer::loadDescriptor() const
{
    auto descriptor = defaultDescriptor();
    const auto fullPath = cocos2d::FileUtils::getInstance()->fullPathForFilename(scriptPath());
    if (fullPath.empty())
    {
        return descriptor;
    }

    auto* luaEngine = cocos2d::LuaEngine::getInstance();
    auto* stack = luaEngine->getLuaStack();
    lua_State* state = stack->getLuaState();
    const int top = lua_gettop(state);

    if (luaL_dofile(state, fullPath.c_str()) == 0 && lua_istable(state, -1))
    {
        descriptor.title = readStringField(state, "title");
        descriptor.subtitle = readStringField(state, "subtitle");
        descriptor.primaryAction = readStringField(state, "primary_action");
        descriptor.primaryTarget = readStringField(state, "primary_target");
        descriptor.secondaryAction = readStringField(state, "secondary_action");
        descriptor.secondaryTarget = readStringField(state, "secondary_target");
        descriptor.panels = readStringArrayField(state, "panels");
    }

    lua_settop(state, top);
    return descriptor;
}

ScreenDescriptor LuaScreenLayer::defaultDescriptor() const
{
    switch (_screen)
    {
    case ScreenId::Boot:
        return { "Kingdom MMO", "Cocos2d-x client boot", "Continue", "login", "", "", { "Server-authoritative mobile 4X prototype" } };
    case ScreenId::Login:
        return { "Guest Login", "Create or restore a local guest account", "Enter Kingdom", "auth", "", "", { "Uses /v1/auth/guest then /v1/bootstrap" } };
    case ScreenId::Main:
        return { "Command Center", "Choose a realm view", "City", "city", "World Map", "world", { "Realtime connects on this screen" } };
    case ScreenId::City:
        return { "Capital City", "Palace District", "World Map", "world", "Command", "main", {} };
    case ScreenId::WorldMap:
        return { "World Frontier", "Viewport data from /v1/map/viewport", "Refresh Viewport", "viewport", "City", "city", {} };
    case ScreenId::BattleReport:
        return { "Battle Report", "Replay viewer placeholder for committed combat reports", "Command Center", "main", "", "", { "Client visualizes reports; server owns outcomes" } };
    }
    return {};
}

std::string LuaScreenLayer::scriptPath() const
{
    return "scripts/screens/" + screenKey() + ".lua";
}

std::string LuaScreenLayer::screenKey() const
{
    switch (_screen)
    {
    case ScreenId::Boot:
        return "boot";
    case ScreenId::Login:
        return "login";
    case ScreenId::Main:
        return "main";
    case ScreenId::City:
        return "city";
    case ScreenId::WorldMap:
        return "world_map";
    case ScreenId::BattleReport:
        return "battle_report";
    }
    return "boot";
}

void LuaScreenLayer::buildLayout(const ScreenDescriptor& descriptor)
{
    const auto visible = cocos2d::Director::getInstance()->getVisibleSize();
    const auto origin = cocos2d::Director::getInstance()->getVisibleOrigin();
    const auto centerX = origin.x + visible.width * 0.5f;

    buildSceneArt();

    if (_screen == ScreenId::City)
    {
        buildCityHud(visible, origin);
        return;
    }

    const bool compactCityHeader = _screen == ScreenId::City;
    auto* title = cocos2d::Label::createWithSystemFont(descriptor.title, "Arial", compactCityHeader ? 28.0f : 42.0f);
    title->setAnchorPoint(compactCityHeader ? cocos2d::Vec2(0.0f, 0.5f) : cocos2d::Vec2(0.5f, 0.5f));
    title->setPosition(compactCityHeader
        ? cocos2d::Vec2(origin.x + 22.0f, origin.y + visible.height - 34.0f)
        : cocos2d::Vec2(centerX, origin.y + visible.height - 92.0f));
    title->enableOutline(cocos2d::Color4B(12, 18, 18, 190), 2);
    addChild(title, 100);

    auto* subtitle = cocos2d::Label::createWithSystemFont(descriptor.subtitle, "Arial", compactCityHeader ? 15.0f : 21.0f);
    subtitle->setAnchorPoint(compactCityHeader ? cocos2d::Vec2(0.0f, 0.5f) : cocos2d::Vec2(0.5f, 0.5f));
    subtitle->setColor(cocos2d::Color3B(210, 220, 220));
    subtitle->setPosition(compactCityHeader
        ? cocos2d::Vec2(origin.x + 24.0f, origin.y + visible.height - 64.0f)
        : cocos2d::Vec2(centerX, origin.y + visible.height - 140.0f));
    addChild(subtitle, 100);

    float panelY = origin.y + visible.height - (compactCityHeader ? 100.0f : 220.0f);
    for (const auto& panel : descriptor.panels)
    {
        auto* label = cocos2d::Label::createWithSystemFont(panel, "Arial", 18.0f);
        label->setAnchorPoint({ 0.0f, 0.5f });
        label->setColor(cocos2d::Color3B(235, 232, 214));
        label->enableOutline(cocos2d::Color4B(16, 22, 20, 180), 1);
        label->setPosition({ origin.x + 120, panelY });
        addChild(label, 100);
        panelY -= 34;
    }

    if (_screen == ScreenId::City)
    {
        auto* statusBackdrop = cocos2d::LayerColor::create(cocos2d::Color4B(14, 27, 29, 205), 460.0f, 30.0f);
        statusBackdrop->setIgnoreAnchorPointForPosition(false);
        statusBackdrop->setAnchorPoint({ 0.5f, 0.5f });
        statusBackdrop->setPosition({ centerX, origin.y + 96.0f });
        addChild(statusBackdrop, 99);
    }

    _statusLabel = cocos2d::Label::createWithSystemFont(GameSession::getInstance().status(), "Arial", 17.0f);
    _statusLabel->setColor(cocos2d::Color3B(176, 207, 228));
    _statusLabel->setPosition({ centerX, origin.y + 96 });
    addChild(_statusLabel, 100);

    addNavigationButton(descriptor.primaryAction, descriptor.primaryTarget, { centerX - 130, origin.y + 48 });
    addNavigationButton(descriptor.secondaryAction, descriptor.secondaryTarget, { centerX + 130, origin.y + 48 });
}

void LuaScreenLayer::buildCityHud(const cocos2d::Size& visible, const cocos2d::Vec2& origin)
{
    if (_cityHud != nullptr)
    {
        _cityHud->removeFromParent();
        _cityHud = nullptr;
    }

    _cityHud = cocos2d::Node::create();
    addChild(_cityHud, 180);

    const auto& bootstrap = GameSession::getInstance().bootstrap();
    const bool hasBootstrap = bootstrap.has_value();
    const auto resources = hasBootstrap ? bootstrap->city.resources : Contracts::Resources{ "1600000", "2400000", "1800000", "2100000", "1200000" };
    const std::string kingdomName = hasBootstrap && !bootstrap->player.displayName.empty() ? bootstrap->player.displayName : "Harbor Kingdom";
    const std::string powerText = hasBootstrap && !bootstrap->player.power.empty() ? bootstrap->player.power : "4,582,631";

    auto* profile = cocos2d::LayerColor::create(cocos2d::Color4B(18, 22, 23, 218), 320.0f, 96.0f);
    profile->setIgnoreAnchorPointForPosition(false);
    profile->setAnchorPoint({ 0.0f, 1.0f });
    profile->setPosition({ origin.x + 10.0f, origin.y + visible.height - 10.0f });
    _cityHud->addChild(profile, 2);

    auto* portrait = cocos2d::LayerColor::create(cocos2d::Color4B(51, 59, 60, 255), 82.0f, 82.0f);
    portrait->setIgnoreAnchorPointForPosition(false);
    portrait->setAnchorPoint({ 0.0f, 1.0f });
    portrait->setPosition({ 8.0f, 88.0f });
    profile->addChild(portrait, 2);

    auto* initials = cocos2d::Label::createWithSystemFont("HK", "Arial", 28);
    initials->setColor(cocos2d::Color3B(235, 223, 190));
    initials->setPosition({ 49.0f, 47.0f });
    profile->addChild(initials, 3);

    auto* levelBadge = cocos2d::Label::createWithSystemFont("26", "Arial", 16);
    levelBadge->setColor(cocos2d::Color3B(255, 220, 102));
    levelBadge->enableOutline(cocos2d::Color4B(28, 18, 8, 240), 2);
    levelBadge->setPosition({ 86.0f, 12.0f });
    profile->addChild(levelBadge, 4);

    auto* name = cocos2d::Label::createWithSystemFont(kingdomName, "Arial", 21);
    name->setAnchorPoint({ 0.0f, 0.5f });
    name->setColor(cocos2d::Color3B(250, 245, 224));
    name->setPosition({ 104.0f, 70.0f });
    profile->addChild(name, 3);

    auto* power = cocos2d::Label::createWithSystemFont("Power " + powerText, "Arial", 17);
    power->setAnchorPoint({ 0.0f, 0.5f });
    power->setColor(cocos2d::Color3B(255, 215, 105));
    power->setPosition({ 104.0f, 43.0f });
    profile->addChild(power, 3);

    auto* vip = cocos2d::Label::createWithSystemFont("VIP 5", "Arial", 17);
    vip->setAnchorPoint({ 0.0f, 0.5f });
    vip->setColor(cocos2d::Color3B(235, 200, 255));
    vip->setPosition({ 104.0f, 19.0f });
    profile->addChild(vip, 3);

    const float chipTop = origin.y + visible.height - 16.0f;
    const float chipWidth = std::max(92.0f, std::min(128.0f, visible.width * 0.095f));
    float chipX = origin.x + visible.width - 34.0f - chipWidth * 5.0f - 10.0f * 4.0f;
    addCityResourceChip(_cityHud, "Wood", formatCompactQuantity(resourceAmount(resources.wood)), { chipX, chipTop }, chipWidth);
    chipX += chipWidth + 10.0f;
    addCityResourceChip(_cityHud, "Food", formatCompactQuantity(resourceAmount(resources.food)), { chipX, chipTop }, chipWidth);
    chipX += chipWidth + 10.0f;
    addCityResourceChip(_cityHud, "Iron", formatCompactQuantity(resourceAmount(resources.iron)), { chipX, chipTop }, chipWidth);
    chipX += chipWidth + 10.0f;
    addCityResourceChip(_cityHud, "Silver", formatCompactQuantity(resourceAmount(resources.silver)), { chipX, chipTop }, chipWidth);
    chipX += chipWidth + 10.0f;
    addCityResourceChip(_cityHud, "Gold", formatCompactQuantity(resourceAmount(resources.gold)), { chipX, chipTop }, chipWidth);

    auto* clock = cocos2d::Label::createWithSystemFont("UTC 06/01 15:30", "Arial", 13);
    clock->setAnchorPoint({ 1.0f, 0.5f });
    clock->setColor(cocos2d::Color3B(225, 225, 220));
    clock->enableOutline(cocos2d::Color4B(0, 0, 0, 180), 1);
    clock->setPosition({ origin.x + visible.width - 20.0f, origin.y + visible.height - 70.0f });
    _cityHud->addChild(clock, 4);

    const float menuY = origin.y + visible.height - 122.0f;
    const float menuStartX = origin.x + visible.width - 300.0f;
    addCityMenuButton(_cityHud, "Events", "2", "events", { menuStartX, menuY }, 56.0f);
    addCityMenuButton(_cityHud, "Offers", "", "offers", { menuStartX + 76.0f, menuY }, 56.0f);
    addCityMenuButton(_cityHud, "Mall", "", "mall", { menuStartX + 152.0f, menuY }, 56.0f);
    addCityMenuButton(_cityHud, "Season", "", "season", { menuStartX + 228.0f, menuY }, 56.0f);

    const float railX = origin.x + visible.width - 42.0f;
    addCityMenuButton(_cityHud, "Build", "2/2", "build_queue", { railX, origin.y + visible.height - 248.0f }, 58.0f);
    addCityMenuButton(_cityHud, "Help", "1/1", "alliance", { railX, origin.y + visible.height - 346.0f }, 58.0f);
    addCityMenuButton(_cityHud, "March", "3/3", "world", { railX, origin.y + visible.height - 444.0f }, 58.0f);

    auto* questPanel = cocos2d::LayerColor::create(cocos2d::Color4B(16, 20, 20, 218), 342.0f, 84.0f);
    questPanel->setIgnoreAnchorPointForPosition(false);
    questPanel->setAnchorPoint({ 0.0f, 0.0f });
    questPanel->setPosition({ origin.x + 16.0f, origin.y + 92.0f });
    _cityHud->addChild(questPanel, 3);

    auto* chapter = cocos2d::Label::createWithSystemFont("Chapter 9", "Arial", 17);
    chapter->setAnchorPoint({ 0.0f, 0.5f });
    chapter->setColor(cocos2d::Color3B(255, 210, 105));
    chapter->setPosition({ 70.0f, 61.0f });
    questPanel->addChild(chapter, 2);

    auto* progress = cocos2d::Label::createWithSystemFont("(18/19)", "Arial", 14);
    progress->setAnchorPoint({ 1.0f, 0.5f });
    progress->setColor(cocos2d::Color3B(224, 224, 212));
    progress->setPosition({ 318.0f, 61.0f });
    questPanel->addChild(progress, 2);

    const int nextPalaceLevel = upgradeTargetLevel(palaceLevel());
    auto* quest = cocos2d::Label::createWithSystemFont("Upgrade Palace to Lv." + std::to_string(nextPalaceLevel), "Arial", 15);
    quest->setAnchorPoint({ 0.0f, 0.5f });
    quest->setColor(cocos2d::Color3B(238, 238, 220));
    quest->setPosition({ 70.0f, 29.0f });
    questPanel->addChild(quest, 2);

    auto* compass = cocos2d::DrawNode::create();
    compass->drawSolidCircle({ 0.0f, 0.0f }, 28.0f, 0.0f, 36, cocos2d::Color4F(0.20f, 0.17f, 0.12f, 0.95f));
    compass->drawTriangle({ 0.0f, 18.0f }, { -12.0f, -13.0f }, { 12.0f, -13.0f }, cocos2d::Color4F(0.78f, 0.68f, 0.48f, 1.0f));
    compass->setPosition({ 38.0f, 42.0f });
    questPanel->addChild(compass, 3);

    auto* chatPanel = cocos2d::LayerColor::create(cocos2d::Color4B(12, 15, 15, 208), 420.0f, 54.0f);
    chatPanel->setIgnoreAnchorPointForPosition(false);
    chatPanel->setAnchorPoint({ 0.0f, 0.0f });
    chatPanel->setPosition({ origin.x + 16.0f, origin.y + 28.0f });
    _cityHud->addChild(chatPanel, 3);

    auto* channel = cocos2d::Label::createWithSystemFont("Alliance", "Arial", 13);
    channel->setAnchorPoint({ 0.0f, 0.5f });
    channel->setColor(cocos2d::Color3B(245, 235, 204));
    channel->setPosition({ 64.0f, 35.0f });
    chatPanel->addChild(channel, 2);

    auto* chatOne = cocos2d::Label::createWithSystemFont("[ALL] Arjun: Great work on the event!", "Arial", 13);
    chatOne->setAnchorPoint({ 0.0f, 0.5f });
    chatOne->setColor(cocos2d::Color3B(224, 232, 225));
    chatOne->setPosition({ 132.0f, 35.0f });
    chatPanel->addChild(chatOne, 2);

    auto* chatTwo = cocos2d::Label::createWithSystemFont("[ALL] Meera: Let's keep growing together.", "Arial", 13);
    chatTwo->setAnchorPoint({ 0.0f, 0.5f });
    chatTwo->setColor(cocos2d::Color3B(224, 232, 225));
    chatTwo->setPosition({ 132.0f, 14.0f });
    chatPanel->addChild(chatTwo, 2);

    auto* chatIcon = cocos2d::DrawNode::create();
    chatIcon->drawSolidCircle({ 0.0f, 0.0f }, 25.0f, 0.0f, 36, cocos2d::Color4F(0.18f, 0.19f, 0.18f, 0.98f));
    chatIcon->drawTriangle({ -7.0f, -7.0f }, { 14.0f, 0.0f }, { -7.0f, 7.0f }, cocos2d::Color4F(0.74f, 0.61f, 0.38f, 1.0f));
    chatIcon->setPosition({ 28.0f, 27.0f });
    chatPanel->addChild(chatIcon, 3);

    const float navY = origin.y + 56.0f;
    const float navStartX = origin.x + visible.width - 570.0f;
    addCityMenuButton(_cityHud, "Items", "", "items", { navStartX, navY }, 64.0f);
    addCityMenuButton(_cityHud, "Hero", "", "hero", { navStartX + 120.0f, navY }, 64.0f);
    addCityMenuButton(_cityHud, "Alliance", "", "alliance", { navStartX + 240.0f, navY }, 64.0f);
    addCityMenuButton(_cityHud, "Mail", "6", "mail", { navStartX + 360.0f, navY }, 64.0f);
    addCityMenuButton(_cityHud, "World", "", "world", { origin.x + visible.width - 70.0f, navY + 2.0f }, 82.0f);

    auto* statusBackdrop = cocos2d::LayerColor::create(cocos2d::Color4B(12, 17, 18, 202), 420.0f, 28.0f);
    statusBackdrop->setIgnoreAnchorPointForPosition(false);
    statusBackdrop->setAnchorPoint({ 0.5f, 0.5f });
    statusBackdrop->setPosition({ origin.x + visible.width * 0.5f, origin.y + 24.0f });
    _cityHud->addChild(statusBackdrop, 2);

    _statusLabel = cocos2d::Label::createWithSystemFont(GameSession::getInstance().status(), "Arial", 14.0f);
    _statusLabel->setColor(cocos2d::Color3B(185, 216, 232));
    _statusLabel->setPosition(statusBackdrop->getPosition());
    _cityHud->addChild(_statusLabel, 4);
}

void LuaScreenLayer::addCityResourceChip(cocos2d::Node* parent, const std::string& label, const std::string& value, const cocos2d::Vec2& position, float width)
{
    auto* chip = cocos2d::LayerColor::create(cocos2d::Color4B(14, 18, 18, 222), width, 34.0f);
    chip->setIgnoreAnchorPointForPosition(false);
    chip->setAnchorPoint({ 0.0f, 1.0f });
    chip->setPosition(position);
    parent->addChild(chip, 4);

    auto* mark = cocos2d::DrawNode::create();
    mark->drawSolidCircle({ 0.0f, 0.0f }, 9.0f, 0.0f, 24, cocos2d::Color4F(0.66f, 0.49f, 0.27f, 1.0f));
    mark->setPosition({ 18.0f, 17.0f });
    chip->addChild(mark, 2);

    auto* initial = cocos2d::Label::createWithSystemFont(label.substr(0, 1), "Arial", 12);
    initial->setColor(cocos2d::Color3B(255, 239, 183));
    initial->setPosition({ 18.0f, 17.0f });
    chip->addChild(initial, 3);

    auto* amount = cocos2d::Label::createWithSystemFont(value, "Arial", 17);
    amount->setAnchorPoint({ 0.0f, 0.5f });
    amount->setColor(cocos2d::Color3B(246, 242, 224));
    amount->setPosition({ 34.0f, 17.0f });
    chip->addChild(amount, 3);
}

void LuaScreenLayer::addCityMenuButton(cocos2d::Node* parent, const std::string& title, const std::string& badge, const std::string& target, const cocos2d::Vec2& position, float size)
{
    auto* ring = cocos2d::DrawNode::create();
    ring->drawSolidCircle({ 0.0f, 0.0f }, size * 0.36f, 0.0f, 40, cocos2d::Color4F(0.10f, 0.11f, 0.10f, 0.94f));
    ring->drawCircle({ 0.0f, 0.0f }, size * 0.36f, 0.0f, 40, false, cocos2d::Color4F(0.62f, 0.50f, 0.31f, 0.95f));
    ring->setPosition(position);
    parent->addChild(ring, 5);

    auto* icon = cocos2d::Label::createWithSystemFont(title.substr(0, 1), "Arial", size >= 78.0f ? 26.0f : 22.0f);
    icon->setColor(cocos2d::Color3B(238, 221, 178));
    icon->enableOutline(cocos2d::Color4B(0, 0, 0, 200), 1);
    icon->setPosition(position);
    parent->addChild(icon, 6);

    auto* label = cocos2d::Label::createWithSystemFont(title, "Arial", size >= 78.0f ? 19.0f : 15.0f);
    label->setColor(cocos2d::Color3B(244, 239, 220));
    label->enableOutline(cocos2d::Color4B(0, 0, 0, 230), 2);
    label->setPosition({ position.x, position.y - size * 0.45f });
    parent->addChild(label, 6);

    if (!badge.empty())
    {
        auto* badgeBack = cocos2d::DrawNode::create();
        badgeBack->drawSolidCircle({ 0.0f, 0.0f }, badge.size() > 1 ? 16.0f : 12.0f, 0.0f, 24, cocos2d::Color4F(0.72f, 0.08f, 0.07f, 0.98f));
        badgeBack->setPosition({ position.x + size * 0.24f, position.y + size * 0.26f });
        parent->addChild(badgeBack, 7);

        auto* badgeText = cocos2d::Label::createWithSystemFont(badge, "Arial", badge.size() > 1 ? 12.0f : 13.0f);
        badgeText->setColor(cocos2d::Color3B::WHITE);
        badgeText->setPosition(badgeBack->getPosition());
        parent->addChild(badgeText, 8);
    }

    auto* button = cocos2d::ui::Button::create();
    button->setContentSize({ size + 24.0f, size + 28.0f });
    button->setPosition({ position.x, position.y - 8.0f });
    button->addClickEventListener([this, target](cocos2d::Ref*) {
        if (_noticeOverlay == nullptr)
        {
            handleAction(target);
        }
    });
    parent->addChild(button, 9);
}

void LuaScreenLayer::buildSceneArt()
{
    const auto visible = cocos2d::Director::getInstance()->getVisibleSize();
    const auto origin = cocos2d::Director::getInstance()->getVisibleOrigin();

    if (_screen == ScreenId::City)
    {
        buildCityArt(visible, origin);
    }
    else if (_screen == ScreenId::WorldMap)
    {
        buildWorldMapArt(visible, origin);
    }
    else if (_screen == ScreenId::BattleReport)
    {
        buildBattleReportArt(visible, origin);
    }
}

void LuaScreenLayer::buildCityArt(const cocos2d::Size& visible, const cocos2d::Vec2& origin)
{
    _cityWorldSize = cocos2d::Size(2400.0f, 1340.0f);
    _cityCanvas = cocos2d::Node::create();
    _cityCanvas->setContentSize(_cityWorldSize);
    addChild(_cityCanvas, 1);

    addArtSpriteTo(_cityCanvas, "art/city/harbor_city_backdrop.jpeg", { _cityWorldSize.width * 0.5f, _cityWorldSize.height * 0.5f }, _cityWorldSize.width, 1);

    _buildingHotspots.clear();
    const int currentPalaceLevel = palaceLevel();
    for (const auto& definition : cityBuildingDefinitions())
    {
        const auto* liveBuilding = findLiveBuilding(definition.key, definition.type);
        CityBuildingHotspot hotspot;
        hotspot.key = definition.key;
        hotspot.type = definition.type;
        hotspot.name = definition.name;
        hotspot.role = definition.role;
        hotspot.primaryAction = definition.primaryAction;
        hotspot.secondaryAction = definition.secondaryAction;
        hotspot.serverOwned = liveBuilding != nullptr;
        hotspot.backendId = liveBuilding != nullptr ? liveBuilding->id : "";
        hotspot.level = liveBuilding != nullptr ? liveBuilding->level : 0;
        hotspot.locked = currentPalaceLevel < definition.requiredPalaceLevel;
        hotspot.requirement = hotspot.locked ? "Requires Palace " + std::to_string(definition.requiredPalaceLevel) : "";
        const auto* activeQueue = liveBuilding != nullptr ? activeBuildQueueFor(liveBuilding->id) : nullptr;
        hotspot.upgrading = activeQueue != nullptr;
        if (activeQueue != nullptr)
        {
            hotspot.queueId = activeQueue->id;
            hotspot.startAt = activeQueue->startAt;
            hotspot.finishAt = activeQueue->finishAt;
        }

        const bool isPalace = definition.key == "palace";
        const float artWidth = cityBuildingArtWidth(definition, hotspot.level);
        auto* sprite = addArtSpriteTo(
            _cityCanvas,
            cityBuildingArtAsset(definition, hotspot.level),
            definition.position,
            artWidth,
            definition.position.y < 430.0f ? 15 : 10);
        if (sprite == nullptr)
        {
            continue;
        }

        // Every committed level gains a little physical presence. The tier
        // ornament below changes on each level and keeps the base art editable.
        const float levelScale = hotspot.serverOwned && hotspot.level > 0
            ? 1.0f + std::min(hotspot.level, 10) * (isPalace ? 0.007f : 0.014f)
            : 1.0f;
        sprite->setScale(sprite->getScale() * levelScale);

        if (hotspot.locked)
        {
            sprite->setOpacity(105);
            sprite->setColor(cocos2d::Color3B(145, 145, 145));
        }
        else if (!hotspot.serverOwned)
        {
            sprite->setOpacity(185);
        }

        if (hotspot.upgrading)
        {
            sprite->setColor(cocos2d::Color3B(255, 235, 170));
        }

        const auto spriteSize = sprite->getContentSize();
        const auto scaledWidth = spriteSize.width * sprite->getScaleX();
        const auto scaledHeight = spriteSize.height * sprite->getScaleY();
        hotspot.bounds = {
            definition.position.x - scaledWidth * 0.5f,
            definition.position.y - scaledHeight * 0.5f,
            scaledWidth,
            scaledHeight
        };

        const auto tierAsset = (!isPalace || hotspot.level > 6) ? visualTierAsset(hotspot.level) : "";
        if (!hotspot.locked && !tierAsset.empty())
        {
            auto* ornament = addArtSpriteTo(
                _cityCanvas,
                tierAsset,
                { definition.position.x, definition.position.y + scaledHeight * 0.30f },
                artWidth * 0.78f,
                31);
            if (ornament != nullptr)
            {
                ornament->setOpacity(hotspot.upgrading ? 170 : 235);
            }
        }

        if (hotspot.upgrading)
        {
            auto* ring = addArtSpriteTo(_cityCanvas, "art/city/states/upgrading_ring.png", definition.position, artWidth * 1.16f, 35);
            if (ring != nullptr)
            {
                ring->runAction(cocos2d::RepeatForever::create(cocos2d::RotateBy::create(2.0f, 360.0f)));
            }
            addArtSpriteTo(_cityCanvas, "art/city/states/upgrading_scaffold.png", definition.position, artWidth * 1.18f, 36);
        }
        else if (hotspot.locked)
        {
            addArtSpriteTo(_cityCanvas, "art/city/states/locked_overlay.png", definition.position, artWidth * 1.08f, 35);
        }
        else
        {
            const int glowZOrder = definition.position.y < 430.0f ? 14 : 9;
            auto* glow = addArtSpriteTo(_cityCanvas, "art/city/states/ready_glow.png", definition.position, artWidth * 1.08f, glowZOrder);
            if (glow != nullptr)
            {
                glow->setOpacity(130);
                glow->runAction(cocos2d::RepeatForever::create(cocos2d::Sequence::create(
                    cocos2d::FadeTo::create(0.7f, 60),
                    cocos2d::FadeTo::create(0.7f, 130),
                    nullptr)));
            }
        }

        const bool isMaxLevel = hotspot.level >= MaximumBuildingLevel;
        const float plaqueWidth = std::min(158.0f, std::max(104.0f, 62.0f + static_cast<float>(definition.name.size()) * 6.0f));
        const float plaqueY = definition.position.y + scaledHeight * 0.50f + 30.0f;
        auto* plaque = cocos2d::LayerColor::create(cocos2d::Color4B(14, 17, 18, 218), plaqueWidth, 44.0f);
        plaque->setIgnoreAnchorPointForPosition(false);
        plaque->setAnchorPoint({ 0.5f, 0.5f });
        plaque->setPosition({ definition.position.x, plaqueY });
        _cityCanvas->addChild(plaque, 43);

        auto* nameLabel = cocos2d::Label::createWithSystemFont(definition.name, "Arial", 15);
        nameLabel->setColor(cocos2d::Color3B(255, 248, 224));
        nameLabel->setPosition({ plaqueWidth * 0.5f, 29.0f });
        plaque->addChild(nameLabel, 2);

        const std::string levelText = hotspot.locked
            ? "Locked"
            : (hotspot.serverOwned ? (isMaxLevel ? "Lv." + std::to_string(hotspot.level) + " Max" : "Lv." + std::to_string(hotspot.level)) : "Plot");
        auto* levelLabel = cocos2d::Label::createWithSystemFont(levelText, "Arial", 13);
        levelLabel->setColor(hotspot.locked ? cocos2d::Color3B(220, 205, 176) : cocos2d::Color3B(205, 235, 178));
        levelLabel->setPosition({ plaqueWidth * 0.5f, 12.0f });
        plaque->addChild(levelLabel, 2);

        if (!hotspot.locked && !hotspot.upgrading && !isMaxLevel && hotspot.serverOwned)
        {
            auto* arrow = cocos2d::DrawNode::create();
            arrow->drawTriangle({ 0.0f, 7.0f }, { -5.0f, -4.0f }, { 5.0f, -4.0f }, cocos2d::Color4F(0.36f, 0.95f, 0.44f, 0.94f));
            arrow->setPosition({ 18.0f, 12.0f });
            plaque->addChild(arrow, 3);
        }

        if (hotspot.upgrading)
        {
            const int remaining = secondsRemaining(hotspot.finishAt);
            auto* timeLabel = cocos2d::Label::createWithSystemFont("Ends " + formatDuration(remaining), "Arial", 12);
            timeLabel->setColor(cocos2d::Color3B(255, 225, 110));
            timeLabel->enableOutline(cocos2d::Color4B(18, 22, 20, 210), 1);
            timeLabel->setPosition({ definition.position.x, plaqueY - 34.0f });
            _cityCanvas->addChild(timeLabel, 44);

            const int total = secondsBetween(hotspot.startAt, hotspot.finishAt);
            const float progress = total > 0 ? 1.0f - (static_cast<float>(remaining) / static_cast<float>(total)) : 0.0f;
            constexpr float barWidth = 104.0f;
            auto* barBack = cocos2d::DrawNode::create();
            barBack->drawSolidRect({ -barWidth * 0.5f, -4.0f }, { barWidth * 0.5f, 4.0f }, cocos2d::Color4F(0.08f, 0.10f, 0.10f, 0.82f));
            barBack->setPosition({ definition.position.x, plaqueY - 50.0f });
            _cityCanvas->addChild(barBack, 41);

            auto* barFill = cocos2d::DrawNode::create();
            const float fillWidth = std::max(8.0f, barWidth * std::max(0.0f, std::min(1.0f, progress)));
            barFill->drawSolidRect({ -barWidth * 0.5f, -4.0f }, { -barWidth * 0.5f + fillWidth, 4.0f }, cocos2d::Color4F(0.93f, 0.66f, 0.16f, 0.95f));
            barFill->setPosition(barBack->getPosition());
            _cityCanvas->addChild(barFill, 42);

            const auto startAt = hotspot.startAt;
            const auto finishAt = hotspot.finishAt;
            const auto queueId = hotspot.queueId;
            schedule([timeLabel, barFill, startAt, finishAt, barWidth](float) {
                const int updatedRemaining = secondsRemaining(finishAt);
                timeLabel->setString("Ends " + formatDuration(updatedRemaining));
                const int updatedTotal = secondsBetween(startAt, finishAt);
                const float updatedProgress = updatedTotal > 0 ? 1.0f - (static_cast<float>(updatedRemaining) / static_cast<float>(updatedTotal)) : 1.0f;
                const float updatedFillWidth = std::max(8.0f, barWidth * std::max(0.0f, std::min(1.0f, updatedProgress)));
                barFill->clear();
                barFill->drawSolidRect({ -barWidth * 0.5f, -4.0f }, { -barWidth * 0.5f + updatedFillWidth, 4.0f }, cocos2d::Color4F(0.93f, 0.66f, 0.16f, 0.95f));
            }, 1.0f, "city_queue_timer_" + queueId);
        }

        if (!hotspot.upgrading && !hotspot.locked && !isMaxLevel)
        {
            auto* indicator = cocos2d::DrawNode::create();
            indicator->drawSolidCircle({ 0.0f, 0.0f }, 8.0f, 0.0f, 24, cocos2d::Color4F(0.48f, 0.95f, 0.45f, 0.92f));
            indicator->setPosition({ definition.position.x + scaledWidth * 0.42f, definition.position.y + scaledHeight * 0.32f });
            indicator->runAction(cocos2d::RepeatForever::create(cocos2d::Sequence::create(
                cocos2d::ScaleTo::create(0.55f, 1.35f),
                cocos2d::ScaleTo::create(0.55f, 0.82f),
                nullptr)));
            _cityCanvas->addChild(indicator, 45);
        }

        _buildingHotspots.push_back(hotspot);
    }

    setCityCanvasPosition({
        origin.x + visible.width * 0.5f - 1200.0f,
        origin.y + visible.height * 0.50f - 690.0f
    });
    addCityInput();
}

void LuaScreenLayer::buildWorldMapArt(const cocos2d::Size& visible, const cocos2d::Vec2& origin)
{
    addArtSprite("art/world/world_backdrop.png", { origin.x + visible.width * 0.5f, origin.y + visible.height * 0.5f }, visible.width, 1);

    const float tileWidth = visible.width * 0.18f;
    for (int row = 0; row < 3; ++row)
    {
        for (int col = 0; col < 5; ++col)
        {
            const float x = origin.x + visible.width * (0.18f + col * 0.16f + (row % 2) * 0.08f);
            const float y = origin.y + visible.height * (0.23f + row * 0.15f);
            addArtSprite("art/world/tile_grass.png", { x, y }, tileWidth, 3);
        }
    }

    addArtSprite("art/world/castle_marker.png", { origin.x + visible.width * 0.48f, origin.y + visible.height * 0.44f }, visible.width * 0.11f, 8);
    addArtSprite("art/world/mine_marker.png", { origin.x + visible.width * 0.32f, origin.y + visible.height * 0.32f }, visible.width * 0.08f, 9);
    addArtSprite("art/world/forest_marker.png", { origin.x + visible.width * 0.66f, origin.y + visible.height * 0.31f }, visible.width * 0.09f, 9);
    addArtSprite("art/world/march_arrow.png", { origin.x + visible.width * 0.56f, origin.y + visible.height * 0.37f }, visible.width * 0.18f, 10);
}

void LuaScreenLayer::buildBattleReportArt(const cocos2d::Size& visible, const cocos2d::Vec2& origin)
{
    addArtSprite("art/world/world_backdrop.png", { origin.x + visible.width * 0.5f, origin.y + visible.height * 0.5f }, visible.width, 1);
    addArtSprite("art/world/castle_marker.png", { origin.x + visible.width * 0.42f, origin.y + visible.height * 0.36f }, visible.width * 0.10f, 8);
    addArtSprite("art/world/march_arrow.png", { origin.x + visible.width * 0.55f, origin.y + visible.height * 0.36f }, visible.width * 0.20f, 9);
    addArtSprite("art/world/mine_marker.png", { origin.x + visible.width * 0.68f, origin.y + visible.height * 0.36f }, visible.width * 0.08f, 8);
}

cocos2d::Sprite* LuaScreenLayer::addArtSprite(const std::string& path, const cocos2d::Vec2& position, float targetWidth, int zOrder)
{
    return addArtSpriteTo(this, path, position, targetWidth, zOrder);
}

cocos2d::Sprite* LuaScreenLayer::addArtSpriteTo(cocos2d::Node* parent, const std::string& path, const cocos2d::Vec2& position, float targetWidth, int zOrder)
{
    auto* sprite = cocos2d::Sprite::create(path);
    if (sprite == nullptr)
    {
        return nullptr;
    }

    const auto size = sprite->getContentSize();
    if (size.width > 0.0f)
    {
        sprite->setScale(targetWidth / size.width);
    }
    sprite->setPosition(position);
    parent->addChild(sprite, zOrder);
    return sprite;
}

void LuaScreenLayer::addNavigationButton(const std::string& text, const std::string& target, const cocos2d::Vec2& position)
{
    if (text.empty() || target.empty())
    {
        return;
    }

    auto* backing = cocos2d::LayerColor::create(cocos2d::Color4B(35, 83, 88, 240), 220.0f, 48.0f);
    backing->setIgnoreAnchorPointForPosition(false);
    backing->setAnchorPoint({ 0.5f, 0.5f });
    backing->setPosition(position);
    addChild(backing, 99);

    auto* button = cocos2d::ui::Button::create();
    button->setTitleText(text);
    button->setTitleFontSize(18);
    button->setTitleColor(cocos2d::Color3B(246, 241, 216));
    button->setContentSize({ 220, 48 });
    button->setPosition(position);
    button->addClickEventListener([this, target](cocos2d::Ref*) {
        if (_noticeOverlay == nullptr)
        {
            handleAction(target);
        }
    });
    addChild(button, 100);
}

void LuaScreenLayer::handleAction(const std::string& target)
{
    if (target == "login")
        SceneRouter::getInstance().openLogin();
    else if (target == "main")
        SceneRouter::getInstance().openMain();
    else if (target == "city")
        SceneRouter::getInstance().openCity();
    else if (target == "world")
        SceneRouter::getInstance().openWorldMap();
    else if (target == "battle_report")
        SceneRouter::getInstance().openBattleReport();
    else if (target == "auth")
        authenticateAndBootstrap();
    else if (target == "viewport")
        fetchWorldViewport();
    else if (target == "build")
        startFirstBuildingUpgrade();
    else if (target == "build_queue")
        showNotice("Construction queues", "Builders are shown on the right rail. Select a building in the city to start or finish an upgrade.", NoticeTone::Info);
    else if (target == "items" || target == "hero" || target == "alliance" || target == "mail" || target == "events" || target == "offers" || target == "mall" || target == "season")
        showNotice("Coming soon", actionText(target) + " will open here when that server action is connected.", NoticeTone::Info);
}

void LuaScreenLayer::authenticateAndBootstrap()
{
    auto& session = GameSession::getInstance();
    session.setStatus("Authenticating guest");
    refreshStatusText();

    _api.authenticateGuest(session.installationId(), "Frontier Lord",
        [this](bool ok, Contracts::GuestAuthResponse auth, std::string error) {
            auto& session = GameSession::getInstance();
            if (!ok)
            {
                session.setStatus("Guest auth failed: " + error);
                refreshStatusText();
                showNotice("Sign in failed", friendlyProblemMessage(error), NoticeTone::Error);
                return;
            }

            session.setAuth(std::move(auth));
            session.setStatus("Loading bootstrap");
            refreshStatusText();

            _api.getBootstrap(session.accessToken(),
                [this](bool bootstrapOk, Contracts::BootstrapResponse bootstrap, std::string bootstrapError) {
                    auto& current = GameSession::getInstance();
                    if (!bootstrapOk)
                    {
                        current.setStatus("Bootstrap failed: " + bootstrapError);
                        refreshStatusText();
                        showNotice("City could not load", friendlyProblemMessage(bootstrapError), NoticeTone::Error);
                        return;
                    }

                    current.setBootstrap(std::move(bootstrap));
                    current.setStatus("Bootstrap loaded");
                    SceneRouter::getInstance().openMain();
                });
        });
}

void LuaScreenLayer::fetchWorldViewport()
{
    auto& session = GameSession::getInstance();
    if (!session.hasAccessToken())
    {
        session.setStatus("Login required before map fetch");
        refreshStatusText();
        showNotice("Sign in required", "Enter the kingdom before opening the world map.", NoticeTone::Warning);
        return;
    }

    session.setStatus("Fetching viewport");
    refreshStatusText();
    _api.getMapViewport(session.accessToken(), 0, 20, 0, 20,
        [this](bool ok, Contracts::ViewportResponse response, std::string error) {
            std::ostringstream status;
            status << (ok ? "Viewport objects: " : "Viewport failed: ")
                   << (ok ? std::to_string(response.objects.size()) : error);
            GameSession::getInstance().setStatus(status.str());
            refreshStatusText();
            if (!ok)
            {
                showNotice("Map could not load", friendlyProblemMessage(error), NoticeTone::Error);
            }
        });
}

void LuaScreenLayer::startFirstBuildingUpgrade()
{
    auto& session = GameSession::getInstance();
    if (!session.hasAccessToken() || !session.bootstrap().has_value() || session.bootstrap()->city.buildings.empty())
    {
        session.setStatus("No bootstrap building available");
        refreshStatusText();
        showNotice("City data unavailable", "Enter the kingdom and wait for the city to finish loading before upgrading a building.", NoticeTone::Warning);
        return;
    }

    const auto buildingId = session.bootstrap()->city.buildings.front().id;
    startBuildingUpgrade(buildingId, "First Building");
}

void LuaScreenLayer::startBuildingUpgrade(const std::string& buildingId, const std::string& buildingName)
{
    auto& session = GameSession::getInstance();
    if (!session.hasAccessToken() || buildingId.empty())
    {
        session.setStatus("Login required before building upgrade");
        refreshStatusText();
        showNotice("Sign in required", "Enter the kingdom before starting an upgrade.", NoticeTone::Warning);
        return;
    }

    session.setStatus("Starting " + buildingName + " upgrade");
    refreshStatusText();
    showLoading("Starting upgrade");
    _api.startBuildingUpgrade(session.accessToken(), buildingId,
        [this, buildingId, buildingName](bool ok, Contracts::StartBuildResponse response, std::string error) {
            hideLoading();
            auto& session = GameSession::getInstance();
            if (ok)
            {
                const auto finishAt = response.queue.finishAt;
                session.applyBuildStarted(std::move(response.queue), std::move(response.resources));
                session.setStatus(buildingName + " upgrade queued");
                if (_screen == ScreenId::City)
                {
                    int currentLevel = 0;
                    if (session.bootstrap().has_value())
                    {
                        for (const auto& building : session.bootstrap()->city.buildings)
                        {
                            if (building.id == buildingId)
                            {
                                currentLevel = building.level;
                                break;
                            }
                        }
                    }
                    refreshCityPresentation();
                    showNotice(
                        "Upgrade started",
                        buildingName + " is now upgrading from level " + std::to_string(currentLevel) + " to level " +
                            std::to_string(upgradeTargetLevel(currentLevel)) + ". It will finish in " +
                            formatDuration(secondsRemaining(finishAt)) + ".",
                        NoticeTone::Success);
                }
                else
                {
                    SceneRouter::getInstance().openCity();
                }
                return;
            }

            session.setStatus("Build failed: " + error);
            refreshStatusText();
            showNotice(friendlyProblemTitle(error), friendlyProblemMessage(error), NoticeTone::Error);
        });
}

void LuaScreenLayer::speedUpBuildingUpgrade(const CityBuildingHotspot& hotspot)
{
    auto& session = GameSession::getInstance();
    if (!session.hasAccessToken() || !hotspot.serverOwned || hotspot.backendId.empty())
    {
        session.setStatus("Login required before speed-up");
        refreshStatusText();
        showNotice("Speed Up unavailable", "Enter the kingdom and select a live building upgrade before using Speed Up.", NoticeTone::Warning);
        return;
    }

    session.setStatus("Speeding up " + hotspot.name);
    refreshStatusText();
    showLoading("Completing upgrade");
    _api.speedUpBuildingUpgrade(session.accessToken(), hotspot.backendId,
        [this, name = hotspot.name](bool ok, Contracts::SpeedUpBuildResponse response, std::string error) {
            hideLoading();
            auto& session = GameSession::getInstance();
            if (ok)
            {
                const int completedLevel = response.building.level;
                session.applyBuildCompleted(std::move(response.queue), std::move(response.building));
                session.setStatus(name + " upgrade completed");
                if (_screen == ScreenId::City)
                {
                    refreshCityPresentation();
                    showNotice(
                        "Upgrade complete",
                        name + " is now level " + std::to_string(completedLevel) + ". Its visual tier has been updated.",
                        NoticeTone::Success);
                }
                else
                {
                    SceneRouter::getInstance().openCity();
                }
                return;
            }

            session.setStatus("Speed-up failed: " + error);
            refreshStatusText();
            showNotice(friendlyProblemTitle(error), friendlyProblemMessage(error), NoticeTone::Error);
        });
}

void LuaScreenLayer::addCityInput()
{
    if (_cityCanvas == nullptr || _cityInputAdded)
    {
        return;
    }

    auto* listener = cocos2d::EventListenerTouchOneByOne::create();
    listener->setSwallowTouches(false);
    listener->onTouchBegan = [this](cocos2d::Touch* touch, cocos2d::Event*) {
        if (_noticeOverlay != nullptr)
        {
            return false;
        }
        if (_actionPanel != nullptr && _actionPanel->getBoundingBox().containsPoint(touch->getLocation()))
        {
            return false;
        }

        _touchStartLocation = touch->getLocation();
        _canvasStartPosition = _cityCanvas->getPosition();
        _cityDragMoved = false;
        return true;
    };

    listener->onTouchMoved = [this](cocos2d::Touch* touch, cocos2d::Event*) {
        if (_cityCanvas == nullptr)
        {
            return;
        }

        const auto delta = touch->getLocation() - _touchStartLocation;
        if (std::abs(delta.x) > 5.0f || std::abs(delta.y) > 5.0f)
        {
            _cityDragMoved = true;
            dismissActionPanel();
        }
        setCityCanvasPosition(_canvasStartPosition + delta);
    };

    listener->onTouchEnded = [this](cocos2d::Touch* touch, cocos2d::Event*) {
        if (_cityCanvas == nullptr || _cityDragMoved)
        {
            return;
        }

        const auto localPoint = _cityCanvas->convertToNodeSpace(touch->getLocation());
        for (auto it = _buildingHotspots.rbegin(); it != _buildingHotspots.rend(); ++it)
        {
            if (it->bounds.containsPoint(localPoint))
            {
                showBuildingActions(*it);
                return;
            }
        }
        dismissActionPanel();
    };

    _eventDispatcher->addEventListenerWithSceneGraphPriority(listener, this);
    _cityInputAdded = true;
}

void LuaScreenLayer::setCityCanvasPosition(cocos2d::Vec2 position)
{
    if (_cityCanvas != nullptr)
    {
        _cityCanvas->setPosition(clampCityCanvasPosition(position));
    }
}

cocos2d::Vec2 LuaScreenLayer::clampCityCanvasPosition(cocos2d::Vec2 position) const
{
    const auto visible = cocos2d::Director::getInstance()->getVisibleSize();
    const auto origin = cocos2d::Director::getInstance()->getVisibleOrigin();

    if (_cityWorldSize.width <= visible.width)
    {
        position.x = origin.x + (visible.width - _cityWorldSize.width) * 0.5f;
    }
    else
    {
        const float minX = origin.x + visible.width - _cityWorldSize.width;
        const float maxX = origin.x;
        position.x = std::max(minX, std::min(maxX, position.x));
    }

    if (_cityWorldSize.height <= visible.height)
    {
        position.y = origin.y + (visible.height - _cityWorldSize.height) * 0.5f;
    }
    else
    {
        const float minY = origin.y + visible.height - _cityWorldSize.height;
        const float maxY = origin.y + 6.0f;
        position.y = std::max(minY, std::min(maxY, position.y));
    }

    return position;
}

void LuaScreenLayer::refreshCityPresentation()
{
    if (_screen != ScreenId::City)
    {
        return;
    }

    for (const auto& hotspot : _buildingHotspots)
    {
        if (!hotspot.queueId.empty())
        {
            unschedule("city_queue_timer_" + hotspot.queueId);
        }
    }
    dismissActionPanel();
    if (_cityCanvas != nullptr)
    {
        _cityCanvas->removeFromParent();
        _cityCanvas = nullptr;
    }
    _buildingHotspots.clear();

    const auto visible = cocos2d::Director::getInstance()->getVisibleSize();
    const auto origin = cocos2d::Director::getInstance()->getVisibleOrigin();
    buildCityArt(visible, origin);
    buildCityHud(visible, origin);
}

void LuaScreenLayer::showBuildingActions(const CityBuildingHotspot& hotspot)
{
    dismissActionPanel();

    const auto visible = cocos2d::Director::getInstance()->getVisibleSize();
    const auto origin = cocos2d::Director::getInstance()->getVisibleOrigin();
    constexpr float panelWidth = 410.0f;
    constexpr float panelHeight = 350.0f;
    constexpr float panelPadding = 22.0f;

    _actionPanel = cocos2d::LayerColor::create(cocos2d::Color4B(22, 29, 30, 238), panelWidth, panelHeight);
    _actionPanel->setIgnoreAnchorPointForPosition(false);
    _actionPanel->setAnchorPoint({ 1.0f, 0.0f });
    _actionPanel->setPosition({ origin.x + visible.width - 24.0f, origin.y + 92.0f });
    addChild(_actionPanel, 210);

    auto* title = cocos2d::Label::createWithSystemFont(hotspot.name, "Arial", 25);
    title->setAnchorPoint({ 0.0f, 0.5f });
    title->setColor(cocos2d::Color3B(255, 247, 216));
    title->setPosition({ 22.0f, panelHeight - 36.0f });
    _actionPanel->addChild(title, 2);

    auto* detail = cocos2d::Label::createWithSystemFont(hotspot.role, "Arial", 14);
    detail->setAnchorPoint({ 0.0f, 0.5f });
    detail->setColor(cocos2d::Color3B(205, 218, 205));
    detail->setDimensions(panelWidth - panelPadding * 2.0f, 34.0f);
    detail->setPosition({ panelPadding, panelHeight - 70.0f });
    _actionPanel->addChild(detail, 2);

    const bool isMaxLevel = hotspot.level >= MaximumBuildingLevel;
    const int targetLevel = upgradeTargetLevel(hotspot.level);
    auto* levelFlow = cocos2d::Label::createWithSystemFont(
        isMaxLevel
            ? "Level " + std::to_string(hotspot.level) + "  -  Maximum level"
            : "Level " + std::to_string(hotspot.level) + "   ->   Level " + std::to_string(targetLevel),
        "Arial",
        18);
    levelFlow->setAnchorPoint({ 0.0f, 0.5f });
    levelFlow->setColor(cocos2d::Color3B(255, 221, 126));
    levelFlow->setPosition({ panelPadding, panelHeight - 112.0f });
    _actionPanel->addChild(levelFlow, 2);

    const int quotedSeconds = hotspot.upgrading
        ? secondsRemaining(hotspot.finishAt)
        : quotedUpgradeSeconds(hotspot.level);
    const std::string stateText = hotspot.upgrading
        ? "Status: upgrading now"
        : (hotspot.locked ? "Locked: " + hotspot.requirement : (hotspot.serverOwned ? "Status: ready for an action" : "Status: city plot"));
    auto* state = cocos2d::Label::createWithSystemFont(stateText, "Arial", 15);
    state->setAnchorPoint({ 0.0f, 0.5f });
    state->setColor(hotspot.locked ? cocos2d::Color3B(236, 178, 126) : cocos2d::Color3B(178, 220, 178));
    state->setPosition({ panelPadding, panelHeight - 144.0f });
    _actionPanel->addChild(state, 2);

    auto* timing = cocos2d::Label::createWithSystemFont(
        hotspot.upgrading ? "Remaining: " + formatDuration(quotedSeconds) : (isMaxLevel ? "Upgrade time: complete" : "Upgrade time: " + formatDuration(quotedSeconds)),
        "Arial",
        15);
    timing->setAnchorPoint({ 0.0f, 0.5f });
    timing->setColor(cocos2d::Color3B(255, 225, 135));
    timing->setPosition({ panelPadding, panelHeight - 176.0f });
    _actionPanel->addChild(timing, 2);

    const std::string requirementText = hotspot.locked
        ? "Unlock requirement: " + hotspot.requirement
        : (isMaxLevel ? "No further upgrade is currently available." : upgradeCostText(hotspot.level));
    auto* requirement = cocos2d::Label::createWithSystemFont(requirementText, "Arial", 14);
    requirement->setAnchorPoint({ 0.0f, 0.5f });
    requirement->setColor(
        hotspot.locked ? cocos2d::Color3B(236, 178, 126)
            : (hasUpgradeResources(hotspot.level) ? cocos2d::Color3B(205, 218, 205) : cocos2d::Color3B(235, 142, 126)));
    requirement->setPosition({ panelPadding, panelHeight - 205.0f });
    _actionPanel->addChild(requirement, 2);

    if (hotspot.upgrading)
    {
        const int total = secondsBetween(hotspot.startAt, hotspot.finishAt);
        const float progress = total > 0 ? 1.0f - static_cast<float>(quotedSeconds) / static_cast<float>(total) : 0.0f;
        auto* barBack = cocos2d::DrawNode::create();
        const float barWidth = panelWidth - panelPadding * 2.0f;
        barBack->drawSolidRect({ 0.0f, 0.0f }, { barWidth, 10.0f }, cocos2d::Color4F(0.08f, 0.10f, 0.10f, 0.95f));
        barBack->setPosition({ panelPadding, panelHeight - 226.0f });
        _actionPanel->addChild(barBack, 2);

        auto* barFill = cocos2d::DrawNode::create();
        barFill->drawSolidRect({ 0.0f, 0.0f }, { barWidth * std::max(0.0f, std::min(1.0f, progress)), 10.0f }, cocos2d::Color4F(0.93f, 0.67f, 0.18f, 0.95f));
        barFill->setPosition(barBack->getPosition());
        _actionPanel->addChild(barFill, 3);
    }

    const float buttonWidth = 174.0f;
    auto addPanelButton = [this, hotspot, buttonWidth](const std::string& label, const std::string& action, const cocos2d::Vec2& position) {
        const bool isUpgrade = action == "upgrade";
        const bool isSpeedUp = action == "speedup";
        const cocos2d::Color4B backingColor = isUpgrade
            ? cocos2d::Color4B(126, 84, 39, 250)
            : (isSpeedUp && !hotspot.upgrading ? cocos2d::Color4B(66, 70, 69, 245) : cocos2d::Color4B(55, 91, 92, 245));
        auto* backing = cocos2d::LayerColor::create(backingColor, buttonWidth, 42.0f);
        backing->setIgnoreAnchorPointForPosition(false);
        backing->setAnchorPoint({ 0.5f, 0.5f });
        backing->setPosition(position);
        _actionPanel->addChild(backing, 1);

        auto* button = cocos2d::ui::Button::create();
        button->setTitleText(label);
        button->setTitleFontSize(15);
        button->setTitleColor(cocos2d::Color3B(248, 244, 218));
        button->setContentSize({ buttonWidth, 42.0f });
        button->setPosition(position);
        button->addClickEventListener([this, hotspot, action](cocos2d::Ref*) {
            performBuildingAction(hotspot, action);
        });
        _actionPanel->addChild(button, 2);

        if (isUpgrade && hotspot.level < MaximumBuildingLevel)
        {
            auto* arrow = cocos2d::DrawNode::create();
            const auto arrowColor = cocos2d::Color4F(1.0f, 0.91f, 0.55f, 1.0f);
            arrow->drawSegment({ 0.0f, -9.0f }, { 0.0f, 9.0f }, 2.5f, arrowColor);
            arrow->drawSegment({ -6.0f, 3.0f }, { 0.0f, 9.0f }, 2.5f, arrowColor);
            arrow->drawSegment({ 6.0f, 3.0f }, { 0.0f, 9.0f }, 2.5f, arrowColor);
            arrow->setPosition({ position.x - buttonWidth * 0.37f, position.y });
            _actionPanel->addChild(arrow, 3);
        }
    };

    const auto labelForAction = [targetLevel, isMaxLevel](const std::string& action) {
        if (action == "upgrade")
        {
            return isMaxLevel ? std::string("Maximum level") : "Upgrade to Lv " + std::to_string(targetLevel);
        }
        return actionText(action);
    };
    addPanelButton(labelForAction(hotspot.primaryAction), hotspot.primaryAction, { 111.0f, 86.0f });
    addPanelButton(labelForAction(hotspot.secondaryAction), hotspot.secondaryAction, { 299.0f, 86.0f });
    addPanelButton(hotspot.upgrading ? "Finish now" : "Speed Up", "speedup", { 111.0f, 32.0f });
    addPanelButton("Close", "close", { 299.0f, 32.0f });
}

void LuaScreenLayer::dismissActionPanel()
{
    if (_actionPanel != nullptr)
    {
        _actionPanel->removeFromParent();
        _actionPanel = nullptr;
    }
}

void LuaScreenLayer::performBuildingAction(const CityBuildingHotspot& hotspot, const std::string& action)
{
    auto& session = GameSession::getInstance();
    if (action == "close")
    {
        dismissActionPanel();
        return;
    }

    if (hotspot.locked)
    {
        session.setStatus(hotspot.name + " requires " + hotspot.requirement);
        refreshStatusText();
        dismissActionPanel();
        showNotice(
            "Building locked",
            hotspot.name + " cannot be used yet. " + hotspot.requirement + " before selecting an action here.",
            NoticeTone::Warning);
        return;
    }

    if (action == "upgrade")
    {
        if (hotspot.level >= MaximumBuildingLevel)
        {
            session.setStatus(hotspot.name + " is at the maximum level");
            refreshStatusText();
            dismissActionPanel();
            showNotice(
                "Maximum level reached",
                hotspot.name + " is already level " + std::to_string(MaximumBuildingLevel) + ". No further upgrade is currently available.",
                NoticeTone::Info);
            return;
        }

        if (hotspot.upgrading)
        {
            session.setStatus(hotspot.name + " is already upgrading");
            refreshStatusText();
            dismissActionPanel();
            showNotice(
                "Upgrade already underway",
                hotspot.name + " is currently upgrading. Select Finish now to complete this timer immediately.",
                NoticeTone::Warning);
            return;
        }

        if (!hotspot.serverOwned)
        {
            session.setStatus(hotspot.name + " is not ready to upgrade");
            refreshStatusText();
            dismissActionPanel();
            showNotice(
                "Building unavailable",
                hotspot.name + " is still being added to this city. Re-enter the kingdom to refresh the city layout, then try again.",
                NoticeTone::Warning);
            return;
        }

        if (!hasUpgradeResources(hotspot.level))
        {
            session.setStatus("More resources are needed for " + hotspot.name);
            refreshStatusText();
            dismissActionPanel();
            showNotice(
                "More resources needed",
                hotspot.name + " needs " + upgradeCostText(hotspot.level) + ". Gather Food and Wood, then try the upgrade again.",
                NoticeTone::Warning);
            return;
        }

        if (const auto* activeQueue = activeCityBuildQueue(); activeQueue != nullptr && activeQueue->refId != hotspot.backendId)
        {
            std::string activeBuildingName = "another building";
            for (const auto& candidate : _buildingHotspots)
            {
                if (candidate.backendId == activeQueue->refId)
                {
                    activeBuildingName = candidate.name;
                    break;
                }
            }
            session.setStatus("Construction queue is busy with " + activeBuildingName);
            refreshStatusText();
            dismissActionPanel();
            showNotice(
                "Construction queue busy",
                activeBuildingName + " is already upgrading. Finish that upgrade or use Finish now before starting " + hotspot.name + ".",
                NoticeTone::Warning);
            return;
        }

        dismissActionPanel();
        startBuildingUpgrade(hotspot.backendId, hotspot.name);
        return;
    }

    if (action == "speedup")
    {
        if (hotspot.upgrading)
        {
            dismissActionPanel();
            speedUpBuildingUpgrade(hotspot);
            return;
        }

        session.setStatus("No active timer on " + hotspot.name);
        refreshStatusText();
        dismissActionPanel();
        showNotice(
            "No upgrade to speed up",
            hotspot.name + " has no active upgrade timer. Start an upgrade first, then Finish now will be available here.",
            NoticeTone::Info);
        return;
    }

    const auto actionName = actionText(action);
    session.setStatus(actionName + " is not available yet for " + hotspot.name);
    refreshStatusText();
    dismissActionPanel();
    showNotice(
        actionName + " is not available yet",
        hotspot.name + " can be upgraded now. " + actionName + " will open here when its server action is connected.",
        NoticeTone::Info);
}

const Contracts::Building* LuaScreenLayer::findLiveBuilding(const std::string& key, const std::string& type) const
{
    const auto& bootstrap = GameSession::getInstance().bootstrap();
    if (!bootstrap.has_value())
    {
        return nullptr;
    }

    for (const auto& building : bootstrap->city.buildings)
    {
        if (building.slotKey == key || (key == type && building.buildingType == type))
        {
            return &building;
        }
    }

    return nullptr;
}

const Contracts::QueueJob* LuaScreenLayer::activeBuildQueueFor(const std::string& buildingId) const
{
    if (buildingId.empty())
    {
        return nullptr;
    }

    const auto& bootstrap = GameSession::getInstance().bootstrap();
    if (!bootstrap.has_value())
    {
        return nullptr;
    }

    for (const auto& queue : bootstrap->city.activeQueues)
    {
        if (queue.refId == buildingId && queue.jobType == "building_upgrade" && queue.status != "completed")
        {
            return &queue;
        }
    }

    return nullptr;
}

const Contracts::QueueJob* LuaScreenLayer::activeCityBuildQueue() const
{
    const auto& bootstrap = GameSession::getInstance().bootstrap();
    if (!bootstrap.has_value())
    {
        return nullptr;
    }

    for (const auto& queue : bootstrap->city.activeQueues)
    {
        if (queue.jobType == "building_upgrade" && (queue.status == "pending" || queue.status == "processing"))
        {
            return &queue;
        }
    }

    return nullptr;
}

bool LuaScreenLayer::isBuildingUpgrading(const std::string& buildingId) const
{
    return activeBuildQueueFor(buildingId) != nullptr;
}

bool LuaScreenLayer::hasUpgradeResources(int currentLevel) const
{
    const auto& bootstrap = GameSession::getInstance().bootstrap();
    if (!bootstrap.has_value())
    {
        return false;
    }

    const auto cost = quotedUpgradeResourceCost(currentLevel);
    const auto& resources = bootstrap->city.resources;
    return resourceAmount(resources.food) >= cost && resourceAmount(resources.wood) >= cost;
}

int LuaScreenLayer::palaceLevel() const
{
    const auto* palace = findLiveBuilding("palace", "palace");
    return palace != nullptr ? palace->level : 1;
}

void LuaScreenLayer::showLoading(const std::string& message)
{
    hideLoading();

    const auto visible = cocos2d::Director::getInstance()->getVisibleSize();
    const auto origin = cocos2d::Director::getInstance()->getVisibleOrigin();
    _loadingOverlay = cocos2d::LayerColor::create(cocos2d::Color4B(8, 11, 12, 140), visible.width, visible.height);
    _loadingOverlay->setPosition(origin);
    addChild(_loadingOverlay, 400);

    auto* panel = cocos2d::LayerColor::create(cocos2d::Color4B(24, 31, 32, 238), 300.0f, 132.0f);
    panel->setIgnoreAnchorPointForPosition(false);
    panel->setAnchorPoint({ 0.5f, 0.5f });
    panel->setPosition({ origin.x + visible.width * 0.5f, origin.y + visible.height * 0.5f });
    _loadingOverlay->addChild(panel, 1);

    auto* spinner = cocos2d::DrawNode::create();
    spinner->drawCircle({ 0.0f, 0.0f }, 28.0f, 0.0f, 42, false, cocos2d::Color4F(0.92f, 0.74f, 0.25f, 1.0f));
    spinner->drawSegment({ 0.0f, 0.0f }, { 28.0f, 0.0f }, 5.0f, cocos2d::Color4F(0.98f, 0.92f, 0.62f, 1.0f));
    spinner->setPosition({ 150.0f, 78.0f });
    spinner->runAction(cocos2d::RepeatForever::create(cocos2d::RotateBy::create(0.9f, 360.0f)));
    panel->addChild(spinner, 2);

    auto* label = cocos2d::Label::createWithSystemFont(message, "Arial", 17);
    label->setColor(cocos2d::Color3B(245, 239, 210));
    label->setPosition({ 150.0f, 34.0f });
    panel->addChild(label, 2);
}

void LuaScreenLayer::hideLoading()
{
    if (_loadingOverlay != nullptr)
    {
        _loadingOverlay->removeFromParent();
        _loadingOverlay = nullptr;
    }
}

void LuaScreenLayer::showNotice(const std::string& title, const std::string& message, NoticeTone tone)
{
    dismissNotice();

    const auto visible = cocos2d::Director::getInstance()->getVisibleSize();
    const auto origin = cocos2d::Director::getInstance()->getVisibleOrigin();
    const float panelWidth = std::min(460.0f, std::max(300.0f, visible.width - 48.0f));
    constexpr float panelHeight = 246.0f;

    _noticeOverlay = cocos2d::LayerColor::create(cocos2d::Color4B(7, 10, 12, 184), visible.width, visible.height);
    _noticeOverlay->setPosition(origin);
    addChild(_noticeOverlay, 500);

    auto* panel = cocos2d::LayerColor::create(cocos2d::Color4B(24, 31, 32, 250), panelWidth, panelHeight);
    panel->setIgnoreAnchorPointForPosition(false);
    panel->setAnchorPoint({ 0.5f, 0.5f });
    panel->setPosition({ origin.x + visible.width * 0.5f, origin.y + visible.height * 0.5f });
    _noticeOverlay->addChild(panel, 1);

    auto* accent = cocos2d::DrawNode::create();
    const auto color = noticeColor(tone);
    const auto accentColor = cocos2d::Color4F(
        static_cast<float>(color.r) / 255.0f,
        static_cast<float>(color.g) / 255.0f,
        static_cast<float>(color.b) / 255.0f,
        1.0f);
    accent->drawSolidCircle({ 0.0f, 0.0f }, 18.0f, 0.0f, 24, accentColor);
    accent->setPosition({ panelWidth * 0.5f, panelHeight - 34.0f });
    panel->addChild(accent, 2);

    auto* titleLabel = cocos2d::Label::createWithSystemFont(title, "Arial", 22);
    titleLabel->setColor(cocos2d::Color3B(250, 246, 221));
    titleLabel->setPosition({ panelWidth * 0.5f, panelHeight - 72.0f });
    panel->addChild(titleLabel, 2);

    auto* messageLabel = cocos2d::Label::createWithSystemFont(message, "Arial", 16);
    messageLabel->setAnchorPoint({ 0.5f, 0.5f });
    messageLabel->setColor(cocos2d::Color3B(211, 222, 211));
    messageLabel->setDimensions(panelWidth - 54.0f, 86.0f);
    messageLabel->setPosition({ panelWidth * 0.5f, 106.0f });
    panel->addChild(messageLabel, 2);

    auto* buttonBacking = cocos2d::LayerColor::create(cocos2d::Color4B(color.r, color.g, color.b, 235), 148.0f, 42.0f);
    buttonBacking->setIgnoreAnchorPointForPosition(false);
    buttonBacking->setAnchorPoint({ 0.5f, 0.5f });
    buttonBacking->setPosition({ panelWidth * 0.5f, 32.0f });
    panel->addChild(buttonBacking, 2);

    auto* button = cocos2d::ui::Button::create();
    button->setTitleText("Continue");
    button->setTitleFontSize(16);
    button->setTitleColor(cocos2d::Color3B(24, 31, 32));
    button->setContentSize({ 148.0f, 42.0f });
    button->setPosition({ panelWidth * 0.5f, 32.0f });
    button->addClickEventListener([this](cocos2d::Ref*) { dismissNotice(); });
    panel->addChild(button, 3);
}

void LuaScreenLayer::dismissNotice()
{
    if (_noticeOverlay != nullptr)
    {
        _noticeOverlay->removeFromParent();
        _noticeOverlay = nullptr;
    }
}

void LuaScreenLayer::connectRealtime()
{
    auto& session = GameSession::getInstance();
    if (!session.hasAccessToken())
    {
        return;
    }

    _realtime.setStatusCallback([this](const std::string& status) {
        GameSession::getInstance().setStatus(status);
        refreshStatusText();
    });
    _realtime.setEventCallback([this](const Contracts::RealtimeEnvelope& event) {
        GameSession::getInstance().setStatus("Realtime event: " + event.type);
        refreshStatusText();
    });
    _realtime.connect(session.realtimeBaseUrl(), session.accessToken());
}

void LuaScreenLayer::refreshStatusText()
{
    if (_statusLabel != nullptr)
    {
        _statusLabel->setString(GameSession::getInstance().status());
    }
}
}
