#include "contracts/KingdomContracts.h"

#include "json/document.h"
#include "json/stringbuffer.h"
#include "json/writer.h"

namespace Kingdom::Contracts
{
namespace
{
std::string getString(const rapidjson::Value& value, const char* key)
{
    if (!value.IsObject() || !value.HasMember(key) || !value[key].IsString())
    {
        return {};
    }
    return value[key].GetString();
}

int getInt(const rapidjson::Value& value, const char* key, int fallback = 0)
{
    if (!value.IsObject() || !value.HasMember(key) || !value[key].IsInt())
    {
        return fallback;
    }
    return value[key].GetInt();
}

const rapidjson::Value* getData(const rapidjson::Document& doc)
{
    if (!doc.IsObject())
    {
        return nullptr;
    }
    if (doc.HasMember("data") && doc["data"].IsObject())
    {
        return &doc["data"];
    }
    return &doc;
}

Resources parseResources(const rapidjson::Value& value)
{
    return {
        getString(value, "food"),
        getString(value, "wood"),
        getString(value, "iron"),
        getString(value, "silver"),
        getString(value, "gold"),
    };
}

QueueJob parseQueueJob(const rapidjson::Value& value)
{
    return {
        getString(value, "id"),
        getString(value, "job_type"),
        getString(value, "ref_id"),
        getString(value, "start_at"),
        getString(value, "finish_at"),
        getString(value, "status"),
    };
}

Building parseBuilding(const rapidjson::Value& value)
{
    return {
        getString(value, "id"),
        getString(value, "slot_key"),
        getString(value, "building_type"),
        getInt(value, "level"),
    };
}

MarchSnapshot parseMarch(const rapidjson::Value& value)
{
    return {
        getString(value, "id"),
        getString(value, "march_type"),
        getString(value, "status"),
        getString(value, "arrive_at"),
    };
}

std::string writeJson(rapidjson::Document& document)
{
    rapidjson::StringBuffer buffer;
    rapidjson::Writer<rapidjson::StringBuffer> writer(buffer);
    document.Accept(writer);
    return buffer.GetString();
}
}

std::optional<GuestAuthResponse> parseGuestAuthResponse(const std::string& json)
{
    rapidjson::Document doc;
    doc.Parse(json.c_str());
    const auto* data = getData(doc);
    if (doc.HasParseError() || data == nullptr)
    {
        return std::nullopt;
    }

    return GuestAuthResponse{
        getString(*data, "account_id"),
        getString(*data, "player_id"),
        getString(*data, "access_token"),
        getString(*data, "refresh_token"),
        getString(*data, "access_token_expires_at"),
    };
}

std::optional<BootstrapResponse> parseBootstrapResponse(const std::string& json)
{
    rapidjson::Document doc;
    doc.Parse(json.c_str());
    const auto* data = getData(doc);
    if (doc.HasParseError() || data == nullptr)
    {
        return std::nullopt;
    }

    BootstrapResponse response;
    response.serverTime = getString(*data, "server_time");

    if (data->HasMember("player") && (*data)["player"].IsObject())
    {
        const auto& player = (*data)["player"];
        response.player = {
            getString(player, "id"),
            getString(player, "kingdom_id"),
            getString(player, "display_name"),
            getString(player, "power"),
        };
    }

    if (data->HasMember("city") && (*data)["city"].IsObject())
    {
        const auto& city = (*data)["city"];
        response.city.id = getString(city, "id");
        response.city.x = getInt(city, "x");
        response.city.y = getInt(city, "y");
        if (city.HasMember("resources") && city["resources"].IsObject())
        {
            response.city.resources = parseResources(city["resources"]);
        }
        if (city.HasMember("buildings") && city["buildings"].IsArray())
        {
            for (const auto& building : city["buildings"].GetArray())
            {
                response.city.buildings.push_back(parseBuilding(building));
            }
        }
        if (city.HasMember("active_queues") && city["active_queues"].IsArray())
        {
            for (const auto& queue : city["active_queues"].GetArray())
            {
                response.city.activeQueues.push_back(parseQueueJob(queue));
            }
        }
    }

    if (data->HasMember("active_marches") && (*data)["active_marches"].IsArray())
    {
        for (const auto& march : (*data)["active_marches"].GetArray())
        {
            response.activeMarches.push_back(parseMarch(march));
        }
    }

    return response;
}

std::optional<StartBuildResponse> parseStartBuildResponse(const std::string& json)
{
    rapidjson::Document doc;
    doc.Parse(json.c_str());
    const auto* data = getData(doc);
    if (doc.HasParseError() || data == nullptr)
    {
        return std::nullopt;
    }

    StartBuildResponse response;
    if (data->HasMember("queue") && (*data)["queue"].IsObject())
    {
        response.queue = parseQueueJob((*data)["queue"]);
    }
    if (data->HasMember("resources") && (*data)["resources"].IsObject())
    {
        response.resources = parseResources((*data)["resources"]);
    }
    return response;
}

std::optional<SpeedUpBuildResponse> parseSpeedUpBuildResponse(const std::string& json)
{
    rapidjson::Document doc;
    doc.Parse(json.c_str());
    const auto* data = getData(doc);
    if (doc.HasParseError() || data == nullptr)
    {
        return std::nullopt;
    }

    SpeedUpBuildResponse response;
    if (data->HasMember("queue") && (*data)["queue"].IsObject())
    {
        response.queue = parseQueueJob((*data)["queue"]);
    }
    if (data->HasMember("building") && (*data)["building"].IsObject())
    {
        response.building = parseBuilding((*data)["building"]);
    }
    return response;
}

std::optional<ProblemDetails> parseProblemDetails(const std::string& json)
{
    rapidjson::Document doc;
    doc.Parse(json.c_str());
    if (doc.HasParseError() || !doc.IsObject())
    {
        return std::nullopt;
    }

    ProblemDetails problem{
        getInt(doc, "status"),
        getString(doc, "code"),
        getString(doc, "title"),
        getString(doc, "detail"),
        getString(doc, "correlation_id"),
    };
    if (problem.status == 0 && problem.code.empty() && problem.detail.empty())
    {
        return std::nullopt;
    }
    return problem;
}

std::optional<ViewportResponse> parseViewportResponse(const std::string& json)
{
    rapidjson::Document doc;
    doc.Parse(json.c_str());
    const auto* data = getData(doc);
    if (doc.HasParseError() || data == nullptr)
    {
        return std::nullopt;
    }

    ViewportResponse response;
    const rapidjson::Value* source = data;
    if (data->HasMember("objects") && (*data)["objects"].IsArray())
    {
        source = &(*data)["objects"];
    }

    if (source->IsArray())
    {
        for (const auto& object : source->GetArray())
        {
            response.objects.push_back({
                getString(object, "id"),
                getString(object, "object_type"),
                getInt(object, "x"),
                getInt(object, "y"),
                getInt(object, "level", 1),
            });
        }
    }

    return response;
}

std::optional<RealtimeEnvelope> parseRealtimeEnvelope(const std::string& json)
{
    rapidjson::Document doc;
    doc.Parse(json.c_str());
    if (doc.HasParseError() || !doc.IsObject())
    {
        return std::nullopt;
    }

    std::string payloadJson = "{}";
    if (doc.HasMember("payload"))
    {
        rapidjson::StringBuffer buffer;
        rapidjson::Writer<rapidjson::StringBuffer> writer(buffer);
        doc["payload"].Accept(writer);
        payloadJson = buffer.GetString();
    }

    return RealtimeEnvelope{
        getString(doc, "event_id"),
        getString(doc, "type"),
        getString(doc, "sequence"),
        getString(doc, "occurred_at"),
        payloadJson,
    };
}

std::string createGuestAuthRequestJson(const std::string& installationId, const std::string& displayName)
{
    rapidjson::Document doc;
    doc.SetObject();
    auto& allocator = doc.GetAllocator();
    doc.AddMember("installation_id", rapidjson::Value(installationId.c_str(), allocator), allocator);
    if (!displayName.empty())
    {
        doc.AddMember("display_name", rapidjson::Value(displayName.c_str(), allocator), allocator);
    }
    return writeJson(doc);
}

std::string createStartBuildRequestJson(const std::string& buildingId)
{
    rapidjson::Document doc;
    doc.SetObject();
    auto& allocator = doc.GetAllocator();
    doc.AddMember("building_id", rapidjson::Value(buildingId.c_str(), allocator), allocator);
    return writeJson(doc);
}

std::string createSpeedUpBuildRequestJson(const std::string& buildingId)
{
    return createStartBuildRequestJson(buildingId);
}

std::string createDispatchMarchRequestJson(const std::string& targetId, const std::string& marchType)
{
    rapidjson::Document doc;
    doc.SetObject();
    auto& allocator = doc.GetAllocator();
    doc.AddMember("target_id", rapidjson::Value(targetId.c_str(), allocator), allocator);
    doc.AddMember("march_type", rapidjson::Value(marchType.c_str(), allocator), allocator);

    rapidjson::Value troops(rapidjson::kArrayType);
    rapidjson::Value starter(rapidjson::kObjectType);
    starter.AddMember("troop_type", "infantry", allocator);
    starter.AddMember("tier", 1, allocator);
    starter.AddMember("count", "1", allocator);
    troops.PushBack(starter, allocator);
    doc.AddMember("troops", troops, allocator);

    return writeJson(doc);
}
}
