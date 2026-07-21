#include "app/AppDelegate.h"

namespace
{
int runApplication()
{
    Kingdom::AppDelegate app;
    return cocos2d::Application::getInstance()->run();
}
} // namespace

#if defined(_WIN32)
#include <windows.h>

int WINAPI WinMain(HINSTANCE, HINSTANCE, LPSTR, int)
{
    return runApplication();
}
#endif

int main(int, char**)
{
    return runApplication();
}
