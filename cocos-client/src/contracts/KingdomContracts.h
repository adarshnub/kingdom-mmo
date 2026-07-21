#pragma once

#include <cstdint>
#include <optional>
#include <string>
#include <vector>

namespace Kingdom::Contracts
{
struct Resources
{
    std::string food;
    std::string wood;
    std::string iron;
    std::string silver;
    std::string gold;
};

struct QueueJob
{
    std::string id;
    std::string jobType;
    std::string refId;
    std::string startAt;
    std::string finishAt;
    std::string status;
};

struct Building
{
    std::string id;
    std::string slotKey;
    std::string buildingType;
    int level = 0;
};

struct PlayerSnapshot
{
    std::string id;
    std::string kingdomId;
    std::string displayName;
    std::string power;
};

struct CitySnapshot
{
    std::string id;
    int x = 0;
    int y = 0;
    Resources resources;
    std::vector<Building> buildings;
    std::vector<QueueJob> activeQueues;
};

struct MarchSnapshot
{
    std::string id;
    std::string marchType;
    std::string status;
    std::string arriveAt;
};

struct BootstrapResponse
{
    std::string serverTime;
    PlayerSnapshot player;
    CitySnapshot city;
    std::vector<MarchSnapshot> activeMarches;
};

struct GuestAuthResponse
{
    std::string accountId;
    std::string playerId;
    std::string accessToken;
    std::string refreshToken;
    std::string accessTokenExpiresAt;
};

struct MapObject
{
    std::string id;
    std::string objectType;
    int x = 0;
    int y = 0;
    int level = 1;
};

struct ViewportResponse
{
    std::vector<MapObject> objects;
};

struct StartBuildResponse
{
    QueueJob queue;
    Resources resources;
};

struct SpeedUpBuildResponse
{
    QueueJob queue;
    Building building;
};

struct RealtimeEnvelope
{
    std::string eventId;
    std::string type;
    std::string sequence;
    std::string occurredAt;
    std::string payloadJson;
};

struct ProblemDetails
{
    int status = 0;
    std::string code;
    std::string title;
    std::string detail;
    std::string correlationId;
};

std::optional<GuestAuthResponse> parseGuestAuthResponse(const std::string& json);
std::optional<BootstrapResponse> parseBootstrapResponse(const std::string& json);
std::optional<StartBuildResponse> parseStartBuildResponse(const std::string& json);
std::optional<SpeedUpBuildResponse> parseSpeedUpBuildResponse(const std::string& json);
std::optional<ProblemDetails> parseProblemDetails(const std::string& json);
std::optional<ViewportResponse> parseViewportResponse(const std::string& json);
std::optional<RealtimeEnvelope> parseRealtimeEnvelope(const std::string& json);
std::string createGuestAuthRequestJson(const std::string& installationId, const std::string& displayName);
std::string createStartBuildRequestJson(const std::string& buildingId);
std::string createSpeedUpBuildRequestJson(const std::string& buildingId);
std::string createDispatchMarchRequestJson(const std::string& targetId, const std::string& marchType);
}
