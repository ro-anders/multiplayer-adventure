
#include "GameSetup.hpp"

#include <iostream>
#include "json/json.h"
#include "json/json-forwards.h"
#include "RestClient.hpp"
#include "Sys.hpp"
#include "UdpTransport.hpp"

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

bool GameSetup::GameParams::ok() {
    return secondPlayerAddress.isValid();
}

GameSetup::GameSetup(RestClient& inClient, UdpTransport& inTransport) :
client(inClient),
transport(inTransport) {}

GameSetup::GameParams GameSetup::setup(int argc, char** argv) {
    GameParams newParams;

    Transport::Address myAddress = Transport::parseUrl(argv[3]);

    Json::Value responseJson;
    // Connect to the client and register a game request.
    char requestContent[200];
    sprintf(requestContent, "{\"ip1\": \"%s\",\"port1\": %d}", myAddress.ip(), myAddress.port());
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
    newParams.numberPlayers = gameParams["numPlayers"].asInt();
    newParams.thisPlayer = gameParams["thisPlayer"].asInt();
    Json::Value otherPlayer = gameParams["otherPlayer"];
    const char* ip = otherPlayer["ip1"].asCString();
    int port = otherPlayer["port1"].asInt();
    
    Transport::Address secondPlayer(ip, port);
    std::cout << "Got second player " << secondPlayer.ip() << ":" << secondPlayer.port() << std::endl;
    newParams.secondPlayerAddress = secondPlayer;
    
    return newParams;
}

/**
 * Contact the UDP server and it will tell you what IP and port your UDP packets 
 * will look like they come from.
 */
Transport::Address GameSetup::determinePublicAddress() {
    // First need to pick which port this game will use for UDP communication.
    transport.reservePort();
    
    // Now send a packet on that port.
    
}
