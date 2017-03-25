
#include "GameSetup.hpp"

#include <iostream>
#include "json/json.h"
#include "json/json-forwards.h"
#include "Adventure.h"
#include "Logger.hpp"
#include "RestClient.hpp"
#include "Sys.hpp"
#include "UdpSocket.hpp"
#include "UdpTransport.hpp"

const int GameSetup::DEFAULT_GAME_LEVEL = GAME_MODE_1;

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
    checkExpirationDate();
    
    GameParams newParams;
    bool isConnectTest = false;

    if ((argc >= 1) && (strcmp(argv[0], "test")==0)) {
        // Run a simple UDP socket test and exit.  Can either specify IPs and ports or let it dynamically use ports on localhost
        // H2HAdventure test [<roleNumber(1 for receiver, 2 for sender)> <myport> <otherip>:<otherport>]
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
        newParams.numberPlayers = 3;
        newParams.thisPlayer = (argc == 1 ? 0 : atoi(argv[1])-1);
        newParams.gameLevel = GAME_MODE_SCRIPTING;
    } else if ((argc >= 1) && (strcmp(argv[0], "broker")==0)){
        // A server will broker the game but still need some info that we parse from the command line.
        // H2HAdventure broker <gameLevel (1-3,4)> <desiredPlayers (2-3)> [stunserver:stunport]
        setupBrokeredGame(newParams, argc, argv);
        
    } else if ((argc >= 1) && (strcmp(argv[0], "debug")==0)){
        setupSelfGame(newParams, argc, argv);
    } else if ((argc >= 1) && (strcmp(argv[0], "p2p")==0)){
        // Other players' IP information will be specified on the command line.
        // H2HAdventure <gameLevel(1-3,4)> <thisPlayer(1-3)> <myinternalport> <theirip>:<theirport> [<thirdip>:<thirdport>]
        setupP2PGame(newParams, argc, argv);
    }else {
        // If they don't specify then do one of two things.
        // If no arguments at all, launch a brokered game requesting 2 players and game 2.
        // If it has arguments, treat as a p2p request.
        if (argc == 0) {
            const char* mode = "broker";
            const char* game = "2";
            const char* players = "2";
            char* fakeArgv[]={(char*)mode, (char*)game, (char*)players};
            setupBrokeredGame(newParams, 3, fakeArgv);
        } else {
            const char* mode = "p2p";
            char* fakeArgv[] = {(char*)mode, argv[0], argv[1], argv[2], argv[3], (argc>4?argv[4]:NULL)};
            setupP2PGame(newParams, argc+1, fakeArgv);
        }
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
            newParams.shouldMute = (setupNum == 0);
        }
    }

    return newParams;
}

void GameSetup::setupBrokeredGame(GameSetup::GameParams& newParams, int argc, char** argv) {

    // A server will broker the game but still need some info that we parse from the command line.
    // H2HAdventure broker <gameLevel (1-3,4)> <desiredPlayers (2-3)> [stunserver:stunport]

    newParams.gameLevel = atoi(argv[1])-1;
    int desiredPlayers = (atoi(argv[2]) <= 2 ? 2 : 3);
    int sessionId = Sys::random() * 10000000;

    Transport::Address stunServer(client.BROKER_SERVER, client.STUN_PORT);
    if (argc > 3) {
        stunServer = Transport::parseUrl(argv[3]);
    }
    Transport::Address myAddress = determinePublicAddress(stunServer);
    
    Json::Value responseJson;
    // Connect to the client and register a game request.
    char requestContent[200];
    sprintf(requestContent, "{\"ip1\": \"%s\",\"port1\": %d, \"sessionId\": %d, \"gameToPlay\": %d, \"desiredPlayers\": %d}",
            myAddress.ip(), myAddress.port(), sessionId, newParams.gameLevel, desiredPlayers);
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
    
    // Expecting response of the form:
    // {"gameParams":{
    //   "numPlayers":2, "thisPlayer":1, "gameToPlay":0,
    //   "otherPlayers":[
    //    {"ip1":"127.0.0.1", "port1":5678, "sessionId":"1471662312", "gameToPlay":0, "desiredPlayers":2},
    //    {"ip1":"127.0.0.1", "port1":5679, "sessionId":"1471662320", "gameToPlay":0, "desiredPlayers":2}
    //   ]}
    // }
    Json::Value gameParams = responseJson["gameParams"];
    newParams.numberPlayers = gameParams["numPlayers"].asInt();
    newParams.thisPlayer = gameParams["thisPlayer"].asInt();
    newParams.gameLevel = gameParams["gameToPlay"].asInt();
    Json::Value otherPlayers = gameParams["otherPlayers"];
    int numOtherPlayers = otherPlayers.size();
    for(int ctr=0; ctr<numOtherPlayers; ++ctr) {
        Json::Value otherPlayer = otherPlayers[ctr];
        const char* ip = otherPlayer["ip1"].asCString();
        int port = otherPlayer["port1"].asInt();
        Transport::Address otherPlayerAddr(ip, port);
        std::cout << "Adding player " << otherPlayerAddr.ip() << ":" << otherPlayerAddr.port() << std::endl;
        xport.addOtherPlayer(otherPlayerAddr);
    }
    
    xport.setExternalAddress(myAddress);
    xport.setTransportNum(newParams.thisPlayer);
}

void GameSetup::setupP2PGame(GameSetup::GameParams& newParams, int argc, char** argv) {
    // Other players' IP information will be specified on the command line.
    // H2HAdventure p2p <gameLevel(1-3,4)> <thisPlayer(1-3)> <myinternalport> <theirip>:<theirport> [<thirdip>:<thirdport>]

    newParams.gameLevel = atoi(argv[1])-1;
    newParams.numberPlayers = argc-3;
    Transport::Address addr1;
    newParams.thisPlayer = atoi(argv[2])-1;
    xport.setTransportNum(newParams.thisPlayer);
    int myInternalPort = atoi(argv[3]);
    xport.setInternalPort(myInternalPort);
    addr1 = Transport::parseUrl(argv[4]);
    xport.addOtherPlayer(addr1);
    if (argc > 5) {
        Transport::Address addr2 = Transport::parseUrl(argv[5]);
        xport.addOtherPlayer(addr2);
    }
    
    newParams.shouldMute = ((strcmp(addr1.ip(), "127.0.0.1")==0) && (newParams.thisPlayer > 0));

}

void GameSetup::setupSelfGame(GameSetup::GameParams& newParams, int argc, char** argv) {
    // Used for quick testing.  Run two instances in this mode on the same machine and they will
    // coordinate between themselves what ports to use and which is which player.  Only works when
    // both instances are running on the same machine.
    // H2HAdventure debug [gameLevel(1-3,4)]

    if (argc > 1) {
        newParams.gameLevel = atoi(argv[1])-1;
    }
    newParams.numberPlayers = 2;
    // shouldMute will be set after games coordinate which is which.
}


/**
 * Contact the STUN server and it will tell you what IP and port your UDP packets
 * will look like they come from.
 */
Transport::Address GameSetup::determinePublicAddress(Transport::Address stunServer) {

    Transport::Address publicAddress;
    
    // First need to pick which port this game will use for UDP communication.
    UdpSocket& socket = xport.reservePort();
    
    // Now send a packet on that port.
    sockaddr_in* stunServerSockAddr = socket.createAddress(stunServer, true);
    Logger::log("Sending message to STUN server");
    socket.writeData("Hello", 5, stunServerSockAddr);
    
    // Now listen on the socket, it should be non-blocking, and get the public IP and port
    char buffer[256];
    Logger::log("Listening for STUN server message.");
    int numCharsRead = socket.readData(buffer, 256);
    if (numCharsRead > 0) {
		// Throw a null on the end to terminate the string
		buffer[numCharsRead] = '\0';
        Logger::log() << "Received \"" << buffer << "\" from STUN server." << Logger::EOM;
        publicAddress = Transport::parseUrl(buffer);
    } else {
        Logger::logError() << "Error " << numCharsRead << " from STUN server." << Logger::EOM;
    }
    socket.deleteAddress(stunServerSockAddr);
    
    return publicAddress;
}

void GameSetup::checkExpirationDate() {
    
    const long EXPIRATION_DATE = 20170430;
    long time = Sys::today();
    if ((EXPIRATION_DATE > 0) && (time > EXPIRATION_DATE)) {
        Logger::logError("Beta Release has expired.");
        exit(-1);
    }
        
}
