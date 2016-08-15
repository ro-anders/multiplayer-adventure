
#include "GameSetup.hpp"

#include <iostream>
#include "json/json.h"
#include "json/json-forwards.h"
#include "Adventure.h"
#include "RestClient.hpp"
#include "Sys.hpp"
#include "UdpSocket.hpp"
#include "UdpTransport.hpp"

const int GameSetup::DEFAULT_GAME_LEVEL = 2;

GameSetup::GameParams::GameParams() :
shouldMute(false),
numberPlayers(0),
thisPlayer(0),
gameLevel(DEFAULT_GAME_LEVEL),
isScripting(false) {}

GameSetup::GameParams::GameParams(const GameParams& other) :
shouldMute(other.shouldMute),
numberPlayers(other.numberPlayers),
thisPlayer(other.thisPlayer),
gameLevel(other.gameLevel),
isScripting(other.isScripting) {}

GameSetup::GameParams& GameSetup::GameParams::operator=(const GameParams& other) {
    shouldMute = other.shouldMute;
    numberPlayers = other.numberPlayers;
    thisPlayer = other.thisPlayer;
    gameLevel = other.gameLevel;
    isScripting = other.isScripting;
    return *this;
}

bool GameSetup::GameParams::ok() {
    return (numberPlayers > 0);
}

GameSetup::GameSetup(RestClient& inClient, UdpTransport& inTransport) :
client(inClient),
xport(inTransport) {}

GameSetup::GameParams GameSetup::setup(int argc, char** argv) {
    GameParams newParams;

    bool isConnectTest = false;

    if ((argc >= 1) && (strcmp(argv[0], "test")==0)) {
        // Run a simple UDP socket test and exit.  Can either specify IPs and ports or let it dynamically use ports on localhost
        // MacAdventure test [<roleNumber(1 for receiver, 2 for sender)> <myport> <otherip>:<otherport>]
        isConnectTest = true;
        if (argc > 1) {
            newParams.thisPlayer = atoi(argv[1])-1;
            xport.setTransportNum(newParams.thisPlayer);
            int myPort = atoi(argv[2]);
            xport.setInternalPort(myPort);
            Transport::Address otheraddr = Transport::parseUrl(argv[3]);
            xport.addOtherPlayer(otheraddr);
        }
    } else if ((argc >= 1) && (strcmp(argv[0], "script")==0)) {
        newParams.isScripting = true;
        newParams.numberPlayers = 2;
        newParams.thisPlayer = (argc == 1 ? 0 : atoi(argv[1])-1);
        newParams.gameLevel = GAME_MODE_SCRIPTING;
    } else if ((argc >= 1) && (strcmp(argv[0], "broker")==0)){
        // A server will broker the game but still need some info that we parse from the command line.
        // MacAdventure broker <gameLevel>
        setupBrokeredGame(newParams, argc, argv);
        
    }else {
        // Other players' IP information will be specified on the command line.
        // MacAdventure <gameLevel> <thisPlayer(1-3)> <myinternalport> <theirip>:<theirport> [<thirdip>:<thirdport>]
        // or
        // MacAdventure [gameLevel]
        
        if (argc >= 1) {
            newParams.gameLevel = atoi(argv[0])-1;
        }
        
        newParams.numberPlayers = (argc <= 1 ? 2 : argc-2);
        Transport::Address addr1;
        if (argc > 1) {
            newParams.thisPlayer = atoi(argv[1])-1;
            xport.setTransportNum(newParams.thisPlayer);
            int myInternalPort = atoi(argv[2]);
            xport.setInternalPort(myInternalPort);
            addr1 = Transport::parseUrl(argv[3]);
            xport.addOtherPlayer(addr1);
            if (argc > 4) {
                Transport::Address addr2 = Transport::parseUrl(argv[4]);
                xport.addOtherPlayer(addr2);
            }
        }
        
        newParams.shouldMute = ((strcmp(addr1.ip(), "127.0.0.1")==0) && (newParams.thisPlayer > 0));
        
    }
    
    if (isConnectTest) {
        Transport::testTransport(xport);
    } else if (!newParams.isScripting) {
        xport.connect();
        while (!xport.isConnected()) {
            Sys::sleep(1000);
        }
        
        int setupNum = xport.getDynamicPlayerSetupNumber();
        if (setupNum != Transport::NOT_DYNAMIC_PLAYER_SETUP) {
            newParams.thisPlayer = setupNum;
        }
    }

    return newParams;
}

void GameSetup::setupBrokeredGame(GameSetup::GameParams& newParams, int argc, char** argv) {

    Transport::Address myAddress = determinePublicAddress();
    
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
    
    // TODO: Figure out game level
    newParams.gameLevel = GAME_MODE_2;
    
    xport.setExternalAddress(myAddress);
    xport.setTransportNum(newParams.thisPlayer);
    xport.addOtherPlayer(secondPlayer);
    newParams.numberPlayers = 2;
    // TODO: Broker third player
    Transport::Address thirdPlayer;
    if (thirdPlayer.isValid()) {
        xport.addOtherPlayer(thirdPlayer);
        newParams.numberPlayers = 3;
    }
}

/**
 * Contact the STUN server and it will tell you what IP and port your UDP packets
 * will look like they come from.
 */
Transport::Address GameSetup::determinePublicAddress() {
    
    Transport::Address publicAddress;
    
    // First need to pick which port this game will use for UDP communication.
    UdpSocket& socket = xport.reservePort();
    
    // Now send a packet on that port.
    Transport::Address stunServer("127.0.01", 8888);
    sockaddr_in* stunServerSockAddr = socket.createAddress(stunServer);
    printf("Sending message to STUN server\n");
    socket.writeData("Hello", 5, stunServerSockAddr);
    
    // Now listen on the socket, it should be non-blocking, and get the public IP and port
    char buffer[256];
    printf("Listening for STUN server message.\n");
    int numCharsRead = socket.readData(buffer, 256);
    if (numCharsRead > 0) {
        printf("Received \"%s\" from STUN server.", buffer);
        publicAddress = Transport::parseUrl(buffer);
    } else {
        printf("Error %d from STUN server.\n", numCharsRead);
    }
    socket.deleteAddress(stunServerSockAddr);
    
    return publicAddress;
}
