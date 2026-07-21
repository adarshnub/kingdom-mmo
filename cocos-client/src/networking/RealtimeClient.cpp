#include "networking/RealtimeClient.h"

#include <sstream>
#include <utility>

namespace Kingdom
{
RealtimeClient::RealtimeClient() = default;

RealtimeClient::~RealtimeClient()
{
    close();
}

bool RealtimeClient::connect(const std::string& realtimeBaseUrl, const std::string& accessToken)
{
    close();
    _socket = std::make_unique<cocos2d::network::WebSocket>();
    const std::string url = realtimeBaseUrl + "?access_token=" + accessToken;
    const bool initialized = _socket->init(*this, url);
    if (!initialized)
    {
        _socket.reset();
        if (_statusCallback)
        {
            _statusCallback("Realtime initialization failed");
        }
    }
    return initialized;
}

void RealtimeClient::close()
{
    if (_socket)
    {
        _socket->close();
        _socket.reset();
    }
}

void RealtimeClient::setEventCallback(EventCallback callback) { _eventCallback = std::move(callback); }
void RealtimeClient::setStatusCallback(StatusCallback callback) { _statusCallback = std::move(callback); }

void RealtimeClient::onOpen(cocos2d::network::WebSocket*)
{
    if (_statusCallback)
    {
        _statusCallback("Realtime connected");
    }
}

void RealtimeClient::onMessage(cocos2d::network::WebSocket*, const cocos2d::network::WebSocket::Data& data)
{
    if (data.bytes == nullptr || data.len <= 0)
    {
        return;
    }

    const std::string json(data.bytes, data.bytes + data.len);
    const auto envelope = Contracts::parseRealtimeEnvelope(json);
    if (envelope.has_value() && _eventCallback)
    {
        _eventCallback(*envelope);
    }
}

void RealtimeClient::onClose(cocos2d::network::WebSocket*)
{
    if (_statusCallback)
    {
        _statusCallback("Realtime closed");
    }
}

void RealtimeClient::onError(cocos2d::network::WebSocket*, const cocos2d::network::WebSocket::ErrorCode&)
{
    if (_statusCallback)
    {
        _statusCallback("Realtime error");
    }
}
}
