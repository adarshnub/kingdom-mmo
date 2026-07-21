#include "screens/KingdomSceneFactory.h"

#include "screens/LuaScreenLayer.h"

namespace Kingdom
{
cocos2d::Scene* KingdomSceneFactory::create(ScreenId screen)
{
    auto* scene = cocos2d::Scene::create();
    auto* layer = LuaScreenLayer::create(screen);
    if (layer != nullptr)
    {
        scene->addChild(layer);
    }
    else
    {
        auto* fallback = cocos2d::LayerColor::create(cocos2d::Color4B(30, 34, 40, 255));
        auto* label = cocos2d::Label::createWithSystemFont("Scene failed to initialize", "Arial", 28);
        label->setPosition({ 640.0f, 360.0f });
        fallback->addChild(label);
        scene->addChild(fallback);
    }
    return scene;
}
}
