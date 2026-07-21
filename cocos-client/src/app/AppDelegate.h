#pragma once

#include "cocos2d.h"

namespace Kingdom
{
class AppDelegate final : private cocos2d::Application
{
public:
    AppDelegate();
    ~AppDelegate() override = default;

    bool applicationDidFinishLaunching() override;
    void applicationDidEnterBackground() override;
    void applicationWillEnterForeground() override;
};
}
