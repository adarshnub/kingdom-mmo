#pragma once

#include "app/SceneRouter.h"
#include "networking/ApiClient.h"
#include "networking/RealtimeClient.h"

#include "cocos2d.h"

#include <string>
#include <vector>

namespace Kingdom
{
struct ScreenDescriptor
{
    std::string title;
    std::string subtitle;
    std::string primaryAction;
    std::string primaryTarget;
    std::string secondaryAction;
    std::string secondaryTarget;
    std::vector<std::string> panels;
};

enum class NoticeTone
{
    Info,
    Success,
    Warning,
    Error,
};

class LuaScreenLayer final : public cocos2d::LayerColor
{
public:
    static LuaScreenLayer* create(ScreenId screen);

    explicit LuaScreenLayer(ScreenId screen);
    bool init() override;
    void onEnter() override;

private:
    struct CityBuildingHotspot
    {
        std::string key;
        std::string type;
        std::string name;
        std::string role;
        std::string primaryAction;
        std::string secondaryAction;
        std::string requirement;
        std::string backendId;
        std::string queueId;
        std::string startAt;
        std::string finishAt;
        cocos2d::Rect bounds;
        int level = 0;
        bool serverOwned = false;
        bool locked = false;
        bool upgrading = false;
    };

    ScreenDescriptor loadDescriptor() const;
    ScreenDescriptor defaultDescriptor() const;
    std::string scriptPath() const;
    std::string screenKey() const;

    void buildLayout(const ScreenDescriptor& descriptor);
    void buildSceneArt();
    void buildCityHud(const cocos2d::Size& visible, const cocos2d::Vec2& origin);
    void addCityResourceChip(cocos2d::Node* parent, const std::string& label, const std::string& value, const cocos2d::Vec2& position, float width);
    void addCityMenuButton(cocos2d::Node* parent, const std::string& title, const std::string& badge, const std::string& target, const cocos2d::Vec2& position, float size);
    void buildCityArt(const cocos2d::Size& visible, const cocos2d::Vec2& origin);
    void buildWorldMapArt(const cocos2d::Size& visible, const cocos2d::Vec2& origin);
    void buildBattleReportArt(const cocos2d::Size& visible, const cocos2d::Vec2& origin);
    cocos2d::Sprite* addArtSprite(const std::string& path, const cocos2d::Vec2& position, float targetWidth, int zOrder);
    cocos2d::Sprite* addArtSpriteTo(cocos2d::Node* parent, const std::string& path, const cocos2d::Vec2& position, float targetWidth, int zOrder);
    void addNavigationButton(const std::string& text, const std::string& target, const cocos2d::Vec2& position);
    void handleAction(const std::string& target);
    void authenticateAndBootstrap();
    void fetchWorldViewport();
    void startFirstBuildingUpgrade();
    void startBuildingUpgrade(const std::string& buildingId, const std::string& buildingName);
    void speedUpBuildingUpgrade(const CityBuildingHotspot& hotspot);
    void connectRealtime();
    void refreshStatusText();
    void addCityInput();
    void setCityCanvasPosition(cocos2d::Vec2 position);
    cocos2d::Vec2 clampCityCanvasPosition(cocos2d::Vec2 position) const;
    void refreshCityPresentation();
    void showBuildingActions(const CityBuildingHotspot& hotspot);
    void dismissActionPanel();
    void performBuildingAction(const CityBuildingHotspot& hotspot, const std::string& action);
    const Contracts::Building* findLiveBuilding(const std::string& key, const std::string& type) const;
    const Contracts::QueueJob* activeBuildQueueFor(const std::string& buildingId) const;
    const Contracts::QueueJob* activeCityBuildQueue() const;
    bool isBuildingUpgrading(const std::string& buildingId) const;
    bool hasUpgradeResources(int currentLevel) const;
    int palaceLevel() const;
    void showLoading(const std::string& message);
    void hideLoading();
    void showNotice(const std::string& title, const std::string& message, NoticeTone tone);
    void dismissNotice();

    ScreenId _screen;
    ApiClient _api;
    RealtimeClient _realtime;
    cocos2d::Label* _statusLabel = nullptr;
    cocos2d::LayerColor* _loadingOverlay = nullptr;
    cocos2d::LayerColor* _noticeOverlay = nullptr;
    cocos2d::Node* _cityHud = nullptr;
    cocos2d::Node* _cityCanvas = nullptr;
    cocos2d::LayerColor* _actionPanel = nullptr;
    cocos2d::Size _cityWorldSize;
    cocos2d::Vec2 _touchStartLocation;
    cocos2d::Vec2 _canvasStartPosition;
    std::vector<CityBuildingHotspot> _buildingHotspots;
    bool _cityDragMoved = false;
    bool _cityInputAdded = false;
};
}
