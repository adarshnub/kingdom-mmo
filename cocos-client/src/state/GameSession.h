#pragma once

#include "contracts/KingdomContracts.h"

#include <optional>
#include <string>

namespace Kingdom
{
class GameSession final
{
public:
    static GameSession& getInstance();

    const std::string& apiBaseUrl() const;
    const std::string& realtimeBaseUrl() const;
    const std::string& installationId();

    void setAuth(Contracts::GuestAuthResponse auth);
    const std::optional<Contracts::GuestAuthResponse>& auth() const;
    bool hasAccessToken() const;
    std::string accessToken() const;

    void setBootstrap(Contracts::BootstrapResponse bootstrap);
    const std::optional<Contracts::BootstrapResponse>& bootstrap() const;
    void applyBuildStarted(Contracts::QueueJob queue, Contracts::Resources resources);
    void applyBuildCompleted(Contracts::QueueJob queue, Contracts::Building building);

    void setStatus(std::string status);
    const std::string& status() const;

private:
    GameSession() = default;
    std::string createInstallationId() const;

    std::string _apiBaseUrl = "http://localhost:3000";
    std::string _realtimeBaseUrl = "ws://localhost:3001/v1/realtime";
    std::string _installationId;
    std::string _status = "Starting";
    std::optional<Contracts::GuestAuthResponse> _auth;
    std::optional<Contracts::BootstrapResponse> _bootstrap;
};
}
