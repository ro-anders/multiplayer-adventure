
#include "GameSetup.hpp"

#include <iostream>
#include "json/json.h"
#include "json/json-forwards.h"
#include "RestClient.hpp"
#include "Sys.hpp"

GameSetup::GameParams::GameParams() :
thisPlayerAddress(),
secondPlayerAddress(),
thirdPlayerAddress() {}

GameSetup::GameParams::GameParams(const GameParams& other) :
thisPlayerAddress(other.thisPlayerAddress),
secondPlayerAddress(other.secondPlayerAddress),
thirdPlayerAddress(other.thirdPlayerAddress) {}

GameSetup::GameParams& GameSetup::GameParams::operator=(const GameParams& other) {
    this->thisPlayerAddress = other.thisPlayerAddress;
    this->secondPlayerAddress = other.secondPlayerAddress;
    this->thirdPlayerAddress = other.thirdPlayerAddress;
    return *this;
}

GameSetup::GameParams GameSetup::setup(RestClient& client, Transport::Address myAddress) {
    GameParams newParams;
    
    Json::Value responseJson;
    // Connect to the client and register a game request.
    char requestContent[200];
    sprintf(requestContent, "ip=%s&port=%d", myAddress.ip(), myAddress.port());
    char response[1000];
    bool gotResponse = false;
    
    while (!gotResponse) {
        client.post("/game", requestContent, response, 1000);
        
        std::stringstream strm(response);
        strm >> responseJson;
        
        gotResponse = !responseJson.empty();
        if (!gotResponse) {
            std::cout << "Waiting for second player." << std::endl;
            Sys::sleep(10000);
        }
    }
    
    Json::Value gameParams = responseJson["gameParams"];
    Json::Value otherPlayer = gameParams["otherPlayer"];
    const char* ip = otherPlayer["ip"].asCString();
    int port = otherPlayer["port"].asInt();
    
    Transport::Address secondPlayer(ip, port);
    std::cout << "Got second player " << secondPlayer.ip() << ":" << secondPlayer.port() << std::endl;
    newParams.secondPlayerAddress = secondPlayer;
    
    return newParams;
}

bool GameSetup::GameParams::ok() {
    return secondPlayerAddress.isValid();
}
