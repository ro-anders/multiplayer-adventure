
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
const int GameSetup::BROKER_TIMEOUT = 10000; // In milliseconds
const int GameSetup::UDP_HANDSHAKE_PERIOD = 1000; // In milliseconds

const int GameSetup::SETUP_INIT = 0;
const int GameSetup::SETUP_WAITING_FOR_PUBLIC_IP = 1;
const int GameSetup::SETUP_MAKE_BROKER_REQUEST = 2;
const int GameSetup::SETUP_INIT_CONNECT_WITH_PLAYERS = 3;
const int GameSetup::SETUP_CONNECTING_WITH_PLAYERS = 4;
const int GameSetup::SETUP_CONNECTED = 5;


GameSetup::GameParams::GameParams() :
shouldMute(false),
numberPlayers(0),
thisPlayer(0),
gameLevel(DEFAULT_GAME_LEVEL),
noTransport(false) {}

GameSetup::GameParams::GameParams(const GameParams& other) :
shouldMute(other.shouldMute),
numberPlayers(other.numberPlayers),
thisPlayer(other.thisPlayer),
gameLevel(other.gameLevel),
noTransport(other.noTransport) {}

GameSetup::GameParams& GameSetup::GameParams::operator=(const GameParams& other) {
    shouldMute = other.shouldMute;
    numberPlayers = other.numberPlayers;
    thisPlayer = other.thisPlayer;
    gameLevel = other.gameLevel;
    noTransport = other.noTransport;
    return *this;
}

bool GameSetup::GameParams::ok() {
    return (numberPlayers > 0);
}

GameSetup::GameSetup(RestClient& inClient, UdpTransport& inTransport) :
client(inClient),
xport(inTransport),
stunServer(RestClient::BROKER_SERVER, RestClient::STUN_PORT),
broker(RestClient::BROKER_SERVER, RestClient::REST_PORT),
setupState(SETUP_INIT),
needPublicIp(false),
isBrokeredGame(false),
isConnectTest(false),
timeoutStart(0),
stunServerSocket(NULL),
stunServerSockAddr(NULL) {}

/**
 * Reads command line arguments.
 * On some OS's argv includes the executable name as the first argument, but on others it does not.
 * This assumes it DOES NOT, so if OS does, call setup(argc-1, argv+1).
 */
void GameSetup::setCommandLineArgs(int argc, char** argv) {
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
        newParams.noTransport = true;
        newParams.numberPlayers = 3;
        newParams.thisPlayer = (argc == 1 ? 0 : atoi(argv[1])-1);
        newParams.gameLevel = GAME_MODE_SCRIPTING;
    } else if ((argc >= 1) && (strcmp(argv[0], "single") == 0)) {
        // Used for debugging with a single client and a faux second player.
        // H2HAdventure single [gameLevel(1-3,4)]
        newParams.gameLevel = (argc > 1 ? atoi(argv[1])-1 : DEFAULT_GAME_LEVEL);
        newParams.noTransport = true;
        newParams.numberPlayers = 2;
        newParams.thisPlayer = 0;
    } else if ((argc >= 1) && (strcmp(argv[0], "broker")==0)){
        // A server will broker the game but still need some info that we parse from the command line.
        // H2HAdventure broker <gameLevel (1-3,4)> <desiredPlayers (2-3)> [stunserver:stunport]
        needPublicIp = true;
        isBrokeredGame = true;
        newParams.gameLevel = atoi(argv[1])-1;
        newParams.numberPlayers = (atoi(argv[2]) <= 2 ? 2 : 3);
        if (argc > 3) {
            broker = Transport::parseUrl(argv[3]);
            stunServer = Transport::Address(broker.ip(), RestClient::STUN_PORT);
        }
    } else if ((argc >= 1) && (strcmp(argv[0], "dev")==0)){
        // Used only for development.  Assumes a second dev instance is being started on the same machine and will
        // dynamically decide which port to use and which is player 1 vs player 2.
        xport.useDynamicPlayerSetup();
        newParams.gameLevel = (argc >= 2 ? atoi(argv[1])-1 : DEFAULT_GAME_LEVEL);
        newParams.numberPlayers = 2;
    } else if ((argc >= 1) && (strcmp(argv[0], "p2p")==0)){
        // Other players' IP information will be specified on the command line.
        // H2HAdventure <gameLevel(1-3,4)> <thisPlayer(1-3)> <myinternalport> <theirip>:<theirport> [<thirdip>:<thirdport>]
        setupP2PGame(newParams, argc, argv);
    }else {
        // Brokered with key information coming from the GUI
        needPublicIp = true;
        isBrokeredGame = true;
        // TODOX: How do we get info from GUI?
        //newParams.gameLevel = atoi(argv[1])-1;
        //newParams.numberPlayers = (atoi(argv[2]) <= 2 ? 2 : 3);
    }
}

void GameSetup::setGameLevel(int level) {
    newParams.gameLevel = level;
}

void GameSetup::setNumberPlayers(int numPlayers) {
    newParams.numberPlayers = numPlayers;
}

/**
 * This checks to see if the game is ready to play and, if not, executes the next step in the setup process.
 * Will occassionally generate status messages (e.g. "first player joined, waiting for second").  If this status
 * message changes, will return true.  If there is no change in status, will return false.
 */
bool GameSetup::checkSetup() {
    long currentTime;
    bool statusChange = false;
    switch (setupState) {
        case SETUP_INIT: {
            checkExpirationDate();
            if (needPublicIp) {
                timeoutStart = Sys::runTime();
                askForPublicAddress();
                setupState = SETUP_WAITING_FOR_PUBLIC_IP;
            } else if (isBrokeredGame) {
                // TODOX: Do I really need to check for this here?
            } else {
                setupState = SETUP_INIT_CONNECT_WITH_PLAYERS;
            }
            break;
        }
        case SETUP_WAITING_FOR_PUBLIC_IP: {
            currentTime = Sys::runTime();
            if (currentTime-timeoutStart > BROKER_TIMEOUT) {
                throw BrokerUnreachableException();
            }
            Transport::Address publicAddress = checkForPublicAddress();
            if (publicAddress.isValid()) {
                craftBrokerRequest(publicAddress);
                setupState = SETUP_MAKE_BROKER_REQUEST;
            }
            break;
        }
        case SETUP_MAKE_BROKER_REQUEST: {
            // TODOX
            break;
        }
        case SETUP_INIT_CONNECT_WITH_PLAYERS: {
            if (!newParams.noTransport) {
                xport.connect();
                setupState = SETUP_CONNECTING_WITH_PLAYERS;
                timeoutStart = Sys::runTime();
            } else {
                setupState = SETUP_CONNECTED;
            }
            break;
        }
        case SETUP_CONNECTING_WITH_PLAYERS: {
            currentTime = Sys::runTime();
            if (currentTime-timeoutStart > UDP_HANDSHAKE_PERIOD) {
                bool nowConnected = xport.isConnected();
                if (nowConnected) {
                    int setupNum = xport.getDynamicPlayerSetupNumber();
                    if (setupNum != Transport::NOT_DYNAMIC_PLAYER_SETUP) {
                        newParams.thisPlayer = setupNum;
                        newParams.shouldMute = (setupNum == 1);
                    }
                    setupState = SETUP_CONNECTED;
                }
                timeoutStart = currentTime;
            }
            break;
        }
    }
    
    return statusChange;
}

void GameSetup::askForPublicAddress() {
    // First need to pick which port this game will use for UDP communication.
    stunServerSocket = &xport.reservePort();
    
    // Now send a packet on that port.
    stunServerSockAddr = stunServerSocket->createAddress(stunServer, true);
    Logger::log("Sending message to STUN server");
    stunServerSocket->writeData("Hello", 5, stunServerSockAddr);
    
    stunServerSocket->setBlocking(false);
    
}

/**
 * True if the game has been all setup and is ready to play.  False if still being setup.
 */
bool GameSetup::isGameSetup() {
    // TODOX: Implement
    return setupState == SETUP_CONNECTED;
}

/**
 * A message indicating the current status of setting up the game.
 */
const char* GameSetup::getStatus() {
    // TODOX: Implement
    return "NaN";
}

/**
 * Get the parameters for the setup game.  If the game is not completely setup, this may
 * return incomplete data.
 */
GameSetup::GameParams GameSetup::getSetup() {
      return newParams;
}

void GameSetup::craftBrokerRequest(Transport::Address) {
    // TODOX: Implement
}


// TODOX: Get rid of this method.  It should be broken up into other methods
void GameSetup::setupBrokeredGame(int argc, char** argv) {

    

	List<Transport::Address> privateAddresses = xport.determineThisMachineIPs();
	Transport::Address publicAddress = determinePublicAddress(stunServer);
    
    int sessionId = Sys::random() * 10000000;
    Json::Value responseJson;
    // Connect to the client and register a game request.
    char requestContent[2000];
    sprintf(requestContent, "{\"addrs\":[{\"ip\": \"%s\",\"port\": %d}", publicAddress.ip(), publicAddress.port());
    for(int ctr=0; ctr<privateAddresses.size(); ++ctr) {
        sprintf(requestContent+strlen(requestContent), ",{\"ip\": \"%s\",\"port\": %d}",
                privateAddresses.get(ctr).ip(), privateAddresses.get(ctr).port());
    }
    sprintf(requestContent+strlen(requestContent), "], \"sessionId\": %d, \"gameToPlay\": %d, \"desiredPlayers\": %d}",
            sessionId, newParams.gameLevel, newParams.numberPlayers);
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
    //   "otherPlayers":[{
    //     "addrs":[{
    //         "ip":"5.5.5.5",
    //         "port":5678
    //       },{
    //         "ip":"127.0.0.1",
    //         "port":5678
    //       }],
    //     "sessionId":"1471662312",
    //     "gameToPlay":0,
    //     "desiredPlayers":2
    //    },{
    //     "addrs":[{
    //         "ip":"6.6.6.6",
    //         "port":5678
    //       }],
    //     "sessionId":"1471662320",
    //     "gameToPlay":0,
    //     "desiredPlayers":2
    //   }]}
    // }
    Json::Value gameParams = responseJson["gameParams"];
    newParams.numberPlayers = gameParams["numPlayers"].asInt();
    newParams.thisPlayer = gameParams["thisPlayer"].asInt();
    newParams.gameLevel = gameParams["gameToPlay"].asInt();
    Json::Value otherPlayers = gameParams["otherPlayers"];
    int numOtherPlayers = otherPlayers.size();
    for(int plyrCtr=0; plyrCtr<numOtherPlayers; ++plyrCtr) {
        // Going to guess there aren't more than 10 addresses
        Transport::Address addresses[10];
        Json::Value otherPlayer = otherPlayers[plyrCtr];
        Json::Value playerAddrs = otherPlayer["addrs"];
        int numAddresses = playerAddrs.size();
        for (int addrCtr=0; addrCtr<numAddresses; ++addrCtr) {
            Json::Value nextAddress = playerAddrs[addrCtr];
            const char* ip = nextAddress["ip"].asCString();
            int port = nextAddress["port"].asInt();
            addresses[addrCtr] = Transport::Address(ip, port);
        }
        // TODOX: Will exception if no address.  In general need better validation and error response
        std::cout << "Adding player " << addresses[0].ip() << ":" << addresses[0].port() << std::endl;
        xport.addOtherPlayer(addresses, numAddresses);
    }
    
    xport.setTransportNum(newParams.thisPlayer);
}

// TODOX: Get rid of this method.  It should be broken up into other methods.
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

// TODOX: Get rid of this method.  It should be broken up into other methods.
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
    
    // Now listen on the socket and get the public IP and port
    socket.setTimeout(15); // Listen for 2 minutes
    char buffer[256];
    Logger::log("Listening for STUN server message.");
    int numCharsRead = socket.readData(buffer, 256);
    if (numCharsRead > 0) {
        // Throw a null on the end to terminate the string
        buffer[numCharsRead] = '\0';
        Logger::log() << "Received \"" << buffer << "\" from STUN server." << Logger::EOM;
        publicAddress = Transport::parseUrl(buffer);
        if (!publicAddress.isValid()) {
            Logger::logError() << "Could not parse IP from STUN server message: " << buffer << Logger::EOM;
            throw std::runtime_error("Could not determine public address.");
        }
    } else {
        Logger::logError() << "Error: Received " << numCharsRead << " from STUN server." << Logger::EOM;
        throw BrokerException();
    }
    socket.deleteAddress(stunServerSockAddr);
    
    return publicAddress;
}

/**
 * A request has been made to the STUN server for the public IP address.
 * Listen for and process the response.
 */
Transport::Address GameSetup::checkForPublicAddress() {
    
    Transport::Address publicAddress;
    // Listen on the socket and get the public IP and port
    char buffer[256];
    Logger::log("Checking for STUN server message.");
    int numCharsRead = stunServerSocket->readData(buffer, 256);
    if (numCharsRead > 0) {
        // Throw a null on the end to terminate the string
        buffer[numCharsRead] = '\0';
        Logger::log() << "Received \"" << buffer << "\" from STUN server." << Logger::EOM;
        publicAddress = Transport::parseUrl(buffer);
        stunServerSocket->deleteAddress(stunServerSockAddr);
        if (!publicAddress.isValid()) {
            Logger::logError() << "Could not parse IP from STUN server message: " << buffer << Logger::EOM;
            throw std::runtime_error("Could not determine public address.");
        }
    }
    
    return publicAddress;
}

void GameSetup::checkExpirationDate() {
    
    const long EXPIRATION_DATE = 20170630;
    long time = Sys::today();
    if ((EXPIRATION_DATE > 0) && (time > EXPIRATION_DATE)) {
        throw std::runtime_error("Beta Release has expired.");
    }
        
}
