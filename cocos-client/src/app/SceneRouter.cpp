#include "app/SceneRouter.h"

#include "cocos2d.h"
#include "screens/KingdomSceneFactory.h"

namespace Kingdom
{
SceneRouter& SceneRouter::getInstance()
{
    static SceneRouter router;
    return router;
}

void SceneRouter::openBoot() { open(ScreenId::Boot); }
void SceneRouter::openLogin() { open(ScreenId::Login); }
void SceneRouter::openMain() { open(ScreenId::Main); }
void SceneRouter::openCity() { open(ScreenId::City); }
void SceneRouter::openWorldMap() { open(ScreenId::WorldMap); }
void SceneRouter::openBattleReport() { open(ScreenId::BattleReport); }

void SceneRouter::open(ScreenId screen)
{
    auto scene = KingdomSceneFactory::create(screen);
    auto* director = cocos2d::Director::getInstance();
    if (director->getRunningScene() == nullptr)
    {
        director->runWithScene(scene);
        return;
    }

    director->replaceScene(cocos2d::TransitionFade::create(0.18f, scene));
}
}
