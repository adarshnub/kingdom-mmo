#pragma once

#include "contracts/KingdomContracts.h"

#include <functional>
#include <string>

namespace Kingdom
{
class ApiClient final
{
public:
    using RawCallback = std::function<void(bool ok, const std::string& body)>;
    using AuthCallback = std::function<void(bool ok, Contracts::GuestAuthResponse response, std::string error)>;
    using BootstrapCallback = std::function<void(bool ok, Contracts::BootstrapResponse response, std::string error)>;
    using BuildCallback = std::function<void(bool ok, Contracts::StartBuildResponse response, std::string error)>;
    using SpeedUpCallback = std::function<void(bool ok, Contracts::SpeedUpBuildResponse response, std::string error)>;
    using ViewportCallback = std::function<void(bool ok, Contracts::ViewportResponse response, std::string error)>;

    explicit ApiClient(std::string baseUrl);

    void authenticateGuest(const std::string& installationId, const std::string& displayName, AuthCallback callback);
    void getBootstrap(const std::string& accessToken, BootstrapCallback callback);
    void startBuildingUpgrade(const std::string& accessToken, const std::string& buildingId, BuildCallback callback);
    void speedUpBuildingUpgrade(const std::string& accessToken, const std::string& buildingId, SpeedUpCallback callback);
    void getMapViewport(const std::string& accessToken, int minX, int maxX, int minY, int maxY, ViewportCallback callback);
    void dispatchMarch(const std::string& accessToken, const std::string& targetId, const std::string& marchType, RawCallback callback);

private:
    void get(const std::string& path, const std::string& accessToken, RawCallback callback);
    void post(const std::string& path, const std::string& body, const std::string& accessToken, bool idempotent, RawCallback callback);
    std::string createRequestId() const;

    std::string _baseUrl;
};
}
