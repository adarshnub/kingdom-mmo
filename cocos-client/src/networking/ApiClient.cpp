#include "networking/ApiClient.h"

#include "network/HttpClient.h"

#include <iomanip>
#include <random>
#include <sstream>
#include <utility>

namespace Kingdom
{
namespace
{
std::string readBody(cocos2d::network::HttpResponse* response)
{
    if (response == nullptr || response->getResponseData() == nullptr)
    {
        return {};
    }

    const auto* data = response->getResponseData();
    return std::string(data->begin(), data->end());
}

bool isSuccess(cocos2d::network::HttpResponse* response)
{
    if (response == nullptr || !response->isSucceed())
    {
        return false;
    }
    const long status = response->getResponseCode();
    return status >= 200 && status < 300;
}
}

ApiClient::ApiClient(std::string baseUrl) : _baseUrl(std::move(baseUrl)) {}

void ApiClient::authenticateGuest(const std::string& installationId, const std::string& displayName, AuthCallback callback)
{
    post("/v1/auth/guest", Contracts::createGuestAuthRequestJson(installationId, displayName), "", false,
        [callback = std::move(callback)](bool ok, const std::string& body) {
            const auto parsed = Contracts::parseGuestAuthResponse(body);
            callback(ok && parsed.has_value(), parsed.value_or(Contracts::GuestAuthResponse{}), ok ? "Invalid auth response" : body);
        });
}

void ApiClient::getBootstrap(const std::string& accessToken, BootstrapCallback callback)
{
    get("/v1/bootstrap", accessToken,
        [callback = std::move(callback)](bool ok, const std::string& body) {
            const auto parsed = Contracts::parseBootstrapResponse(body);
            callback(ok && parsed.has_value(), parsed.value_or(Contracts::BootstrapResponse{}), ok ? "Invalid bootstrap response" : body);
        });
}

void ApiClient::startBuildingUpgrade(const std::string& accessToken, const std::string& buildingId, BuildCallback callback)
{
    post("/v1/city/build/start", Contracts::createStartBuildRequestJson(buildingId), accessToken, true,
        [callback = std::move(callback)](bool ok, const std::string& body) {
            const auto parsed = Contracts::parseStartBuildResponse(body);
            callback(ok && parsed.has_value(), parsed.value_or(Contracts::StartBuildResponse{}), ok ? "Invalid build response" : body);
        });
}

void ApiClient::speedUpBuildingUpgrade(const std::string& accessToken, const std::string& buildingId, SpeedUpCallback callback)
{
    post("/v1/city/build/speed-up", Contracts::createSpeedUpBuildRequestJson(buildingId), accessToken, true,
        [callback = std::move(callback)](bool ok, const std::string& body) {
            const auto parsed = Contracts::parseSpeedUpBuildResponse(body);
            callback(ok && parsed.has_value(), parsed.value_or(Contracts::SpeedUpBuildResponse{}), ok ? "Invalid speed-up response" : body);
        });
}

void ApiClient::getMapViewport(const std::string& accessToken, int minX, int maxX, int minY, int maxY, ViewportCallback callback)
{
    std::ostringstream query;
    query << "/v1/map/viewport?min_x=" << minX << "&max_x=" << maxX << "&min_y=" << minY << "&max_y=" << maxY;
    get(query.str(), accessToken,
        [callback = std::move(callback)](bool ok, const std::string& body) {
            const auto parsed = Contracts::parseViewportResponse(body);
            callback(ok && parsed.has_value(), parsed.value_or(Contracts::ViewportResponse{}), ok ? "Invalid viewport response" : body);
        });
}

void ApiClient::dispatchMarch(const std::string& accessToken, const std::string& targetId, const std::string& marchType, RawCallback callback)
{
    post("/v1/map/march/dispatch", Contracts::createDispatchMarchRequestJson(targetId, marchType), accessToken, true, std::move(callback));
}

void ApiClient::get(const std::string& path, const std::string& accessToken, RawCallback callback)
{
    auto* request = new cocos2d::network::HttpRequest();
    request->setUrl((_baseUrl + path).c_str());
    request->setRequestType(cocos2d::network::HttpRequest::Type::GET);
    if (!accessToken.empty())
    {
        request->setHeaders({ "Authorization: Bearer " + accessToken });
    }
    request->setResponseCallback([callback = std::move(callback)](cocos2d::network::HttpClient*, cocos2d::network::HttpResponse* response) {
        callback(isSuccess(response), readBody(response));
    });
    cocos2d::network::HttpClient::getInstance()->send(request);
    request->release();
}

void ApiClient::post(const std::string& path, const std::string& body, const std::string& accessToken, bool idempotent, RawCallback callback)
{
    auto* request = new cocos2d::network::HttpRequest();
    request->setUrl((_baseUrl + path).c_str());
    request->setRequestType(cocos2d::network::HttpRequest::Type::POST);
    request->setRequestData(body.c_str(), body.size());

    std::vector<std::string> headers = { "Content-Type: application/json" };
    if (!accessToken.empty())
    {
        headers.push_back("Authorization: Bearer " + accessToken);
    }
    if (idempotent)
    {
        headers.push_back("Idempotency-Key: " + createRequestId());
    }
    request->setHeaders(headers);

    request->setResponseCallback([callback = std::move(callback)](cocos2d::network::HttpClient*, cocos2d::network::HttpResponse* response) {
        callback(isSuccess(response), readBody(response));
    });
    cocos2d::network::HttpClient::getInstance()->send(request);
    request->release();
}

std::string ApiClient::createRequestId() const
{
    std::random_device device;
    std::mt19937_64 rng(device());
    std::uniform_int_distribution<unsigned long long> distribution;
    const auto a = distribution(rng);
    const auto b = distribution(rng);

    std::ostringstream out;
    out << std::hex << std::setfill('0')
        << std::setw(8) << ((a >> 32) & 0xffffffff) << "-"
        << std::setw(4) << ((a >> 16) & 0xffff) << "-"
        << std::setw(4) << ((a & 0x0fff) | 0x7000) << "-"
        << std::setw(4) << (((b >> 48) & 0x3fff) | 0x8000) << "-"
        << std::setw(12) << (b & 0xffffffffffff);
    return out.str();
}
}
