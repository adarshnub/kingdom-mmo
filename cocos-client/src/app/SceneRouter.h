#pragma once

#include <string>

namespace Kingdom
{
enum class ScreenId
{
    Boot,
    Login,
    Main,
    City,
    WorldMap,
    BattleReport
};

class SceneRouter final
{
public:
    static SceneRouter& getInstance();

    void openBoot();
    void openLogin();
    void openMain();
    void openCity();
    void openWorldMap();
    void openBattleReport();
    void open(ScreenId screen);

private:
    SceneRouter() = default;
};
}
