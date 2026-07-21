#include "app/AppDelegate.h"

#include "app/SceneRouter.h"
#include "platform/CCFileUtils.h"
#include "base/CCUserDefault.h"
#include "scripting/lua-bindings/manual/CCLuaEngine.h"

namespace Kingdom
{
AppDelegate::AppDelegate() = default;

bool AppDelegate::applicationDidFinishLaunching()
{
    cocos2d::log("KingdomCocosClient: applicationDidFinishLaunching");
    auto* director = cocos2d::Director::getInstance();
    auto* glview = director->getOpenGLView();

    if (glview == nullptr)
    {
        glview = cocos2d::GLViewImpl::createWithRect(
            "Kingdom MMO - Cocos Client",
            cocos2d::Rect(0, 0, 1280, 720));
        director->setOpenGLView(glview);
    }

    glview->setDesignResolutionSize(1280, 720, ResolutionPolicy::SHOW_ALL);
    director->setAnimationInterval(1.0f / 60.0f);
    director->setDisplayStats(false);

    auto* fileUtils = cocos2d::FileUtils::getInstance();
    fileUtils->addSearchPath("Resources");

    auto* luaEngine = cocos2d::LuaEngine::getInstance();
    cocos2d::ScriptEngineManager::getInstance()->setScriptEngine(luaEngine);

    cocos2d::log("KingdomCocosClient: opening boot scene");
    SceneRouter::getInstance().openBoot();
    return true;
}

void AppDelegate::applicationDidEnterBackground()
{
    cocos2d::Director::getInstance()->stopAnimation();
    cocos2d::UserDefault::getInstance()->flush();
}

void AppDelegate::applicationWillEnterForeground()
{
    cocos2d::Director::getInstance()->startAnimation();
}
}
