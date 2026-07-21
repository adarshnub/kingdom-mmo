cmake_minimum_required(VERSION 3.10)
project(KingdomCocosClient)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

if(NOT DEFINED COCOS2DX_ROOT)
  if(DEFINED ENV{COCOS2DX_ROOT})
    set(COCOS2DX_ROOT "$ENV{COCOS2DX_ROOT}")
  else()
    message(FATAL_ERROR "Set COCOS2DX_ROOT to a Cocos2d-x 3.x checkout before configuring.")
  endif()
endif()

set(KINGDOM_SOURCES
  src/platform/win32/main.cpp
  src/app/AppDelegate.cpp
  src/app/SceneRouter.cpp
  src/contracts/KingdomContracts.cpp
  src/networking/ApiClient.cpp
  src/networking/RealtimeClient.cpp
  src/screens/LuaScreenLayer.cpp
  src/screens/KingdomSceneFactory.cpp
  src/state/GameSession.cpp
)

add_executable(KingdomCocosClient ${KINGDOM_SOURCES})

target_include_directories(KingdomCocosClient PRIVATE
  src
  ${COCOS2DX_ROOT}/cocos
  ${COCOS2DX_ROOT}/cocos/platform
  ${COCOS2DX_ROOT}/cocos/editor-support
  ${COCOS2DX_ROOT}/external
  ${COCOS2DX_ROOT}/external/lua/lua
  ${COCOS2DX_ROOT}/external/lua/luajit/include
  ${COCOS2DX_ROOT}/external/lua/tolua
)

if(EXISTS "${COCOS2DX_ROOT}/CMakeLists.txt")
  set(BUILD_TESTS OFF CACHE BOOL "Disable Cocos sample projects" FORCE)
  add_subdirectory(${COCOS2DX_ROOT} ${CMAKE_BINARY_DIR}/cocos2d)
elseif(EXISTS "${COCOS2DX_ROOT}/cocos/CMakeLists.txt")
  add_subdirectory(${COCOS2DX_ROOT}/cocos ${CMAKE_BINARY_DIR}/cocos2d)
else()
  message(FATAL_ERROR "COCOS2DX_ROOT does not look like a Cocos2d-x source checkout: ${COCOS2DX_ROOT}")
endif()
if(WIN32 AND TARGET cocos2d)
  target_compile_definitions(cocos2d PRIVATE GWL_WNDPROC=GWLP_WNDPROC GWL_USERDATA=GWLP_USERDATA)
endif()
target_link_libraries(KingdomCocosClient cocos2d external)
if(TARGET luacocos2d)
  target_link_libraries(KingdomCocosClient luacocos2d)
endif()
setup_cocos_app_config(KingdomCocosClient)

if(WIN32)
  cocos_copy_target_dll(KingdomCocosClient)
endif()

if(WIN32)
  set(APP_RES_DIR "$<TARGET_FILE_DIR:KingdomCocosClient>/Resources")
  add_custom_command(TARGET KingdomCocosClient POST_BUILD
    COMMAND ${CMAKE_COMMAND} -E copy_directory "${CMAKE_CURRENT_SOURCE_DIR}/Resources" "${APP_RES_DIR}"
    COMMENT "copy Resources directory into target output"
  )
endif()

set_target_properties(KingdomCocosClient PROPERTIES
  VS_DEBUGGER_WORKING_DIRECTORY "${CMAKE_CURRENT_SOURCE_DIR}"
)
