#include "state/GameSession.h"

#include "base/CCUserDefault.h"

#include <iomanip>
#include <algorithm>
#include <random>
#include <sstream>
#include <utility>

namespace Kingdom
{
namespace
{
constexpr const char* InstallationIdKey = "kingdom.installation_id";
}

GameSession& GameSession::getInstance()
{
    static GameSession session;
    return session;
}

const std::string& GameSession::apiBaseUrl() const { return _apiBaseUrl; }
const std::string& GameSession::realtimeBaseUrl() const { return _realtimeBaseUrl; }

const std::string& GameSession::installationId()
{
    if (!_installationId.empty())
    {
        return _installationId;
    }

    auto* defaults = cocos2d::UserDefault::getInstance();
    _installationId = defaults->getStringForKey(InstallationIdKey, "");
    if (_installationId.empty())
    {
        _installationId = createInstallationId();
        defaults->setStringForKey(InstallationIdKey, _installationId);
        defaults->flush();
    }
    return _installationId;
}

void GameSession::setAuth(Contracts::GuestAuthResponse auth) { _auth = std::move(auth); }
const std::optional<Contracts::GuestAuthResponse>& GameSession::auth() const { return _auth; }
bool GameSession::hasAccessToken() const { return _auth.has_value() && !_auth->accessToken.empty(); }
std::string GameSession::accessToken() const { return hasAccessToken() ? _auth->accessToken : ""; }

void GameSession::setBootstrap(Contracts::BootstrapResponse bootstrap) { _bootstrap = std::move(bootstrap); }
const std::optional<Contracts::BootstrapResponse>& GameSession::bootstrap() const { return _bootstrap; }

void GameSession::applyBuildStarted(Contracts::QueueJob queue, Contracts::Resources resources)
{
    if (!_bootstrap.has_value())
    {
        return;
    }

    _bootstrap->city.resources = std::move(resources);
    for (auto& existing : _bootstrap->city.activeQueues)
    {
        if (existing.id == queue.id || (!queue.refId.empty() && existing.refId == queue.refId))
        {
            existing = std::move(queue);
            return;
        }
    }
    _bootstrap->city.activeQueues.push_back(std::move(queue));
}

void GameSession::applyBuildCompleted(Contracts::QueueJob queue, Contracts::Building building)
{
    if (!_bootstrap.has_value())
    {
        return;
    }

    for (auto& existing : _bootstrap->city.buildings)
    {
        if (existing.id == building.id)
        {
            existing = std::move(building);
            break;
        }
    }

    _bootstrap->city.activeQueues.erase(
        std::remove_if(
            _bootstrap->city.activeQueues.begin(),
            _bootstrap->city.activeQueues.end(),
            [&queue](const Contracts::QueueJob& existing) {
                return existing.id == queue.id || (!queue.refId.empty() && existing.refId == queue.refId);
            }),
        _bootstrap->city.activeQueues.end());
}

void GameSession::setStatus(std::string status) { _status = std::move(status); }
const std::string& GameSession::status() const { return _status; }

std::string GameSession::createInstallationId() const
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
        << std::setw(4) << ((a & 0x0fff) | 0x4000) << "-"
        << std::setw(4) << (((b >> 48) & 0x3fff) | 0x8000) << "-"
        << std::setw(12) << (b & 0xffffffffffff);
    return out.str();
}
}
