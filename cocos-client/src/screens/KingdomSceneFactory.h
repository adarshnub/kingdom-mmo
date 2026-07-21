#pragma once

#include "app/SceneRouter.h"
#include "cocos2d.h"

namespace Kingdom
{
class KingdomSceneFactory final
{
public:
    static cocos2d::Scene* create(ScreenId screen);
};
}
