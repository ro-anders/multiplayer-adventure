
#include "sys.h"
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
const int GameSetup::STUN_TIMEOUT = 30000; // In milliseconds
const int GameSetup::BROKER_PERIOD = 10000; // In milliseconds
const int GameSetup::UDP_HANDSHAKE_PERIOD = 1000; // In milliseconds

const int GameSetup::SETUP_INIT = 0;
const int GameSetup::SETUP_REQUEST_PUBLIC_IP = 1;
const int GameSetup::SETUP_WAITING_FOR_PUBLIC_IP = 2;
const int GameSetup::SETUP_MAKE_BROKER_REQUEST = 3;
const int GameSetup::SETUP_WAITING_FOR_BROKERING = 4;
const int GameSetup::SETUP_PAUSE_BEFORE_CONNECTING = 5;
const int GameSetup::SETUP_INIT_CONNECT_WITH_PLAYERS = 6;
const int GameSetup::SETUP_CONNECTING_WITH_PLAYERS = 7;
const int GameSetup::SETUP_CONNECTED = 8;
const int GameSetup::SETUP_FAILED = 9;



GameSetup::GameParams::GameParams() :
shouldMute(false),
numberPlayers(0),
thisPlayer(0),
gameLevel(DEFAULT_GAME_LEVEL),
noTransport(false),
privatePlayerName(NULL) {
    setPlayerName("");
}

GameSetup::GameParams::GameParams(const GameParams& other) :
shouldMute(other.shouldMute),
numberPlayers(other.numberPlayers),
thisPlayer(other.thisPlayer),
gameLevel(other.gameLevel),
noTransport(other.noTransport),
privatePlayerName(NULL) {
    setPlayerName(other.privatePlayerName);
}

GameSetup::GameParams& GameSetup::GameParams::operator=(const GameParams& other) {
    setPlayerName(other.privatePlayerName);
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

void GameSetup::GameParams::setPlayerName(const char* newName) {
    if (privatePlayerName != NULL) {
        delete[] privatePlayerName;
    }
    privatePlayerName = new char[strlen(newName)+1];
    strcpy(privatePlayerName, newName);
}

GameSetup::GameSetup(RestClient& inClient, UdpTransport& inTransport) :
client(inClient),
xport(inTransport),
stunServer(RestClient::DEFAULT_BROKER_SERVER, RestClient::STUN_PORT),
broker(RestClient::DEFAULT_BROKER_SERVER, RestClient::REST_PORT),
publicAddress(),
setupState(SETUP_INIT),
needPublicIp(false),
isBrokeredGame(false),
isConnectTest(false),
timeoutStart(0),
brokerSessionId((int)(Sys::random() * 10000000)),
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
        // H2HAdventure single
        newParams.noTransport = true;
        newParams.numberPlayers = 2;
        newParams.thisPlayer = 0;
    } else if ((argc >= 1) && (strcmp(argv[0], "broker")==0)){
        // A server will broker the game but still need some info that we parse from the command line.
        // H2HAdventure broker [stunserver:stunport]
        needPublicIp = true;
        isBrokeredGame = true;
        if (argc > 1) {
            broker = Transport::parseUrl(argv[1]);
            if (broker.port() == 0) {
                broker = Transport::Address(broker.ip(), RestClient::REST_PORT);
            }
            stunServer = Transport::Address(broker.ip(), RestClient::STUN_PORT);
            client.setAddress(broker);
        }
    } else if ((argc >= 1) && (strcmp(argv[0], "dev")==0)){
        // Used only for development.  Assumes a second dev instance is being started on the same machine and will
        // dynamically decide which port to use and which is player 1 vs player 2.
        xport.useDynamicPlayerSetup();
        newParams.numberPlayers = 2;
    } else if ((argc >= 1) && (strcmp(argv[0], "p2p")==0)){
        // Other players' IP information will be specified on the command line.
        // H2HAdventure p2p <gameLevel(1-3,4)> <thisPlayer(1-3)> <myinternalport> <theirip>:<theirport> [<thirdip>:<thirdport>]
        setupP2PGame(newParams, argc, argv);
    }else {
        // Brokered with key information coming from the GUI
        needPublicIp = true;
        isBrokeredGame = true;
    }
}

void GameSetup::setPlayerName(const char* playerName) {
    newParams.setPlayerName(playerName);
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
void GameSetup::checkSetup() {
    long currentTime;
    switch (setupState) {
        case SETUP_INIT: {
            if (hasExpired()) {
                Platform_DisplayStatus("Beta versions only work for a limited time.\nThis version has expired.", -1);
                setupState = SETUP_FAILED;
            } else if (needPublicIp) {
                setupState = SETUP_REQUEST_PUBLIC_IP;
            } else if (isBrokeredGame) {
                setupState = SETUP_MAKE_BROKER_REQUEST;
            } else {
                timeoutStart = Sys::runTime() + 3000; // TODOX: Remove this line
                setupState = SETUP_PAUSE_BEFORE_CONNECTING; // TODOX: SETUP_INIT_CONNECT_WITH_PLAYERS;
            }
            break;
        }
        case SETUP_REQUEST_PUBLIC_IP: {
            Platform_DisplayStatus("Contacting game broker", -1);
            askForPublicAddress();
            setupState = SETUP_WAITING_FOR_PUBLIC_IP;
            timeoutStart = Sys::runTime();
            break;
        }
        case SETUP_WAITING_FOR_PUBLIC_IP: {
            currentTime = Sys::runTime();
            if (currentTime-timeoutStart > STUN_TIMEOUT) {
                Platform_DisplayStatus("Failed to connect with game broker.\nBroker may be down or you may be behind a firewall.", -1);
                setupState = SETUP_FAILED;
            } else {
                publicAddress = checkForPublicAddress();
                if (publicAddress.isValid()) {
                    setupState = SETUP_MAKE_BROKER_REQUEST;
                }
            }
            break;
        }
        case SETUP_MAKE_BROKER_REQUEST: {
            Platform_DisplayStatus("Waiting for other players.", -1);
            craftBrokerRequest(publicAddress);
            setupState = SETUP_WAITING_FOR_BROKERING;
            timeoutStart = 0;
            break;
        }
        case SETUP_WAITING_FOR_BROKERING: {
            currentTime = Sys::runTime();
            if (currentTime-timeoutStart > BROKER_PERIOD) {
                keepPortOpen();
                bool nowConnected = pollBroker();
                if (nowConnected) {
                    timeoutStart = Sys::runTime() + 10000; // Usingg 'timeoutStart' as a timeout end.  Bad.
                    setupState = SETUP_PAUSE_BEFORE_CONNECTING;
                } else {
                    timeoutStart = Sys::runTime();
                }
            }
            break;
        }
        case SETUP_PAUSE_BEFORE_CONNECTING: {
            int timeLeft = timeoutStart - Sys::runTime();
            if (timeLeft <= 0) {
                setupState = SETUP_INIT_CONNECT_WITH_PLAYERS;
            }
            break;
        }
        case SETUP_INIT_CONNECT_WITH_PLAYERS: {
            Platform_DisplayStatus("Initiating peer-to-peer connections", -1);
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
        case SETUP_CONNECTED: case SETUP_FAILED: {
            // Do nothing
            break;
        }
    }
    if (setupState == SETUP_CONNECTED) {
        Platform_DisplayStatus("", 0);
    }
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

void GameSetup::keepPortOpen() {
    if (stunServerSockAddr != NULL) {
        stunServerSocket->writeData("Ping", 5, stunServerSockAddr);
    }
}

/**
 * True if the game has been all setup and is ready to play.  False if still being setup.
 */
bool GameSetup::isGameSetup() {
    return setupState == SETUP_CONNECTED;
}

/**
 * Get the parameters for the setup game.  If the game is not completely setup, this may
 * return incomplete data.
 */
GameSetup::GameParams GameSetup::getSetup() {
      return newParams;
}

void GameSetup::craftBrokerRequest(Transport::Address) {
    List<Transport::Address> privateAddresses = xport.determineThisMachineIPs();
    if (!publicAddress.isValid() && (privateAddresses.size()==0)) {
        throw std::runtime_error("No known IP addresses to publish.");
    }
    Transport::Address firstAddress = publicAddress;
    int secondAddress = 0;
    if (!publicAddress.isValid()) {
        firstAddress = privateAddresses.get(0);
        secondAddress = 1;
    }
    
    // Connect to the client and register a game request.
    sprintf(brokerRequestContent, "{\"playerName\":\"%s\",\"addrs\":[{\"ip\": \"%s\",\"port\": %d}", newParams.playerName(),
            publicAddress.ip(), publicAddress.port());
    for(int ctr=0; ctr<privateAddresses.size(); ++ctr) {
        sprintf(brokerRequestContent+strlen(brokerRequestContent), ",{\"ip\": \"%s\",\"port\": %d}",
                privateAddresses.get(ctr).ip(), privateAddresses.get(ctr).port());
    }
    sprintf(brokerRequestContent+strlen(brokerRequestContent), "], \"sessionId\": %d, \"gameToPlay\": %d, \"desiredPlayers\": %d}",
            brokerSessionId, newParams.gameLevel, newParams.numberPlayers);
}


bool GameSetup::pollBroker() {
    // TODOX: How do we report the broker bexoming unreachable
    char response[10000];
    bool gameSetup = false;
    Json::Value responseJson;
    
    client.post("/game", brokerRequestContent, response, 10000);
    
    std::stringstream strm(response);
    strm >> responseJson;
    
    if (responseJson.empty()) {
        // TODOX: Not sure if this is how broker becoming unreachable manifests
        // Do something intelligent.
        Platform_DisplayStatus("The game broker has stopped responding for some unknown reason.\nWill continue trying.", -1);
    } else {
        gameSetup = (responseJson["gameToPlay"].asInt() >= 0);
        
        // Expecting response of the form
        // {
        //   "gameToPlay": 1,
        //   "numPlayers": 2,
        //   "thisPlayer": 0,
        //   "requests": [
        //     {
        //       "addrs": [
        //         {
        //           "ip": "127.0.0.1",
        //           "port": 9999
        //         }
        //       ],
        //       "sessionId": "2425783",
        //       "gameToPlay": 1,
        //       "desiredPlayers": 2
        //     },
        //     {
        //       "addrs": [
        //         {
        //           "ip": "127.0.0.1",
        //           "port": 8888
        //         }
        //       ],
        //       "sessionId": "2425783",
        //       "gameToPlay": 1,
        //       "desiredPlayers": 2
        //     }
        //   ]
        // }
        //
        // Where "gameToPlay" will be -1 if the game is not full yet.
        
        if (gameSetup) {
            // Read in the game info
            newParams.gameLevel = responseJson["gameToPlay"].asInt();
            newParams.numberPlayers = responseJson["numPlayers"].asInt();
            newParams.thisPlayer = responseJson["thisPlayer"].asInt();
            Json::Value requests = responseJson["requests"];
            int numRequests = requests.size();
            char player1[256];
            player1[0] = '\0';
            char player2[256];
            player2[0] = '\0';
            char message[1024];
            for(int plyrCtr=0; plyrCtr<numRequests; ++plyrCtr) {
                // Going to guess there aren't more than 10 addresses
                Transport::Address addresses[10];
                Json::Value otherPlayer = requests[plyrCtr];
                const char* playerName = otherPlayer["playerName"].asCString();
                Json::Value playerAddrs = otherPlayer["addrs"];
                int numAddresses = playerAddrs.size();
                for (int addrCtr=0; addrCtr<numAddresses; ++addrCtr) {
                    Json::Value nextAddress = playerAddrs[addrCtr];
                    const char* ip = nextAddress["ip"].asCString();
                    int port = nextAddress["port"].asInt();
                    addresses[addrCtr] = Transport::Address(ip, port);
                }
                
                if (plyrCtr != newParams.thisPlayer) {
                    char* nameDest = (player1[0] == '\0' ? player1 : player2);
                    strcpy(nameDest, playerName);
                    // TODOX: Will exception if no address.  In general need better validation and error response
                    std::cout << "Adding player " << playerName << " at " << addresses[0].ip() << ":" << addresses[0].port() << std::endl;
                    xport.addOtherPlayer(addresses, numAddresses);
                }
            }
            xport.setTransportNum(newParams.thisPlayer);
            if (player2[0] == '\0') {
                sprintf(message, "Playing game %d against %s.\nStarting momentarily.", newParams.gameLevel+1, player1);
            } else {
                sprintf(message, "Playing game %d against %s and %s.\nStarting momentarily.", newParams.gameLevel+1, player1, player2);
            }
            Platform_DisplayStatus(message, -1);
        } else {
            // Read just the names of the players to give a status message.
            Json::Value requests = responseJson["requests"];
            int numberJoined = requests.size();
            if (numberJoined <= 1) {
                Platform_DisplayStatus("Waiting for other players.", -1);
            } else {
                char msg[256];
                int thisPlayerSlot = responseJson["thisPlayer"].asInt();
                Json::Value otherPlayer = requests[1-thisPlayerSlot];
                const char* otherPlayerName = otherPlayer["playerName"].asCString();

                sprintf(msg, "%s has joined the game.  Waiting for third player.", otherPlayerName);
                Platform_DisplayStatus(msg, -1);
            }
        }

    }

    return gameSetup;
}

// TODOX: Get rid of this method.  It should be broken up into other methods.
void GameSetup::setupP2PGame(GameSetup::GameParams& newParams, int argc, char** argv) {
    // Other players' IP information will be specified on the command line.
    // H2HAdventure p2p <gameLevel(1-3,4)> <thisPlayer(1-3)> <myinternalport> <theirip>:<theirport> [<thirdip>:<thirdport>]
    
    newParams.gameLevel = atoi(argv[1])-1;
    newParams.numberPlayers = argc-3;
    newParams.thisPlayer = atoi(argv[2])-1;
    xport.setTransportNum(newParams.thisPlayer);
    int myInternalPort = atoi(argv[3]);
    xport.setInternalPort(myInternalPort);
    Transport::Address addr1 = Transport::parseUrl(argv[4]);
    xport.addOtherPlayer(addr1);
    if (argc > 5) {
        Transport::Address addr2 = Transport::parseUrl(argv[5]);
        xport.addOtherPlayer(addr2);
    }
    
    newParams.shouldMute = ((strcmp(addr1.ip(), "127.0.0.1")==0) && (newParams.thisPlayer > 0));
    
}


/**
 * A request has been made to the STUN server for the public IP address.
 * Listen for and process the response.
 */
Transport::Address GameSetup::checkForPublicAddress() {
    
    Transport::Address publicAddress;
    // Listen on the socket and get the public IP and port
    char buffer[256];
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

bool GameSetup::hasExpired() {
    
    const long EXPIRATION_DATE = 20171231;
    long time = Sys::today();
    return ((EXPIRATION_DATE > 0) && (time > EXPIRATION_DATE));
    
}
