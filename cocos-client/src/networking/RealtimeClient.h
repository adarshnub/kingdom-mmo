#pragma once

#include "contracts/KingdomContracts.h"
#include "network/WebSocket.h"

#include <functional>
#include <memory>
#include <string>

namespace Kingdom
{
class RealtimeClient final : public cocos2d::network::WebSocket::Delegate
{
public:
    using EventCallback = std::function<void(const Contracts::RealtimeEnvelope&)>;
    using StatusCallback = std::function<void(const std::string&)>;

    RealtimeClient();
    ~RealtimeClient() override;

    bool connect(const std::string& realtimeBaseUrl, const std::string& accessToken);
    void close();
    void setEventCallback(EventCallback callback);
    void setStatusCallback(StatusCallback callback);

    void onOpen(cocos2d::network::WebSocket* socket) override;
    void onMessage(cocos2d::network::WebSocket* socket, const cocos2d::network::WebSocket::Data& data) override;
    void onClose(cocos2d::network::WebSocket* socket) override;
    void onError(cocos2d::network::WebSocket* socket, const cocos2d::network::WebSocket::ErrorCode& error) override;

private:
    std::unique_ptr<cocos2d::network::WebSocket> _socket;
    EventCallback _eventCallback;
    StatusCallback _statusCallback;
};
}
