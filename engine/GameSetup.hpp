

#ifndef GameSetup_hpp
#define GameSetup_hpp

#include <stdio.h>

#include "Transport.hpp"

class RestClient;
struct sockaddr_in;
class UdpSocket;
class UdpTransport;

/**
 * Encapsulates collecting game setup information (from command line and GUI) and going through the process
 * of connecting multiple players across the internet and starting the game.
 */
class GameSetup {
public:
    
    /** If no game level is specified (usually only in dev and test cases) what game level to use */
    static const int DEFAULT_GAME_LEVEL;
    
    /**
     * A holder for all the info needed to setup a game.
     * Mostly the addresses of the other players but also
     * which level, which player this player is, what
     * the difficulty switches start out at
     */
    class GameParams {
    public:
        
        const char* playerName() {return privatePlayerName;}
        bool shouldMute; // Whether other games are running on the same machine (usually only for dev and testing) and this game should mute itself
        int numberPlayers; // How many players are in the game
        int thisPlayer; // Which player this game is playing.
        int gameLevel; // Which game to play (refer to GAME_MODE_ enum)
        bool noTransport; // Whether this is a real game with other players or a degenerate case that only requires one player (e.g. executing a script)
        
        GameParams();
        GameParams(const GameParams& other);
        GameParams& operator=(const GameParams&);
        bool ok();
        void setPlayerName(const char* newPlayerName);
        
    private:
        char* privatePlayerName;
    };
    
    /**
     * Reads command line arguments and sets up game.
     * On some OS's argv includes the executable name as the first argument, but on others it does not.
     * This assumes it DOES NOT, so if OS does, call setup(argc-1, argv+1).
     */
    GameSetup(RestClient& client, UdpTransport& transport);
    
    /**
     * Reads command line arguments.
     * On some OS's argv includes the executable name as the first argument, but on others it does not.
     * This assumes it DOES NOT, so if OS does, call setup(argc-1, argv+1).
     */
    void setCommandLineArgs(int argc, char** argv);
    
    void setPlayerName(const char* playerName);
    
    void setGameLevel(int level);
    
    void setNumberPlayers(int numPlayers);
    
    /**
     * This checks to see if the game is ready to play and, if not, executes the next step in the setup process.
     * Will occassionally generate status messages (e.g. "first player joined, waiting for second") that it will
     * send to the platform to display.
     */
    void checkSetup();
    
    /**
     * True if the game has been all setup and is ready to play.  False if still being setup.
     */
    bool isGameSetup();
    
    /**
     * Get the parameters for the setup game.  If the game is not yet setup may have incomplete
     * information.
     */
    GameParams getSetup();
    
private:
    
    /** Milliseconds to wait for communications with broker before timing out. */
    static const int STUN_TIMEOUT; // How long to wait for broker to respond with public ip (in ms).
    static const int BROKER_PERIOD; // Ms between requesting from btoker status of game setup
    static const int UDP_HANDSHAKE_PERIOD; // Ms between sending/checking UDP packets with initial connection handshake
    
    Transport::Address stunServer;
    Transport::Address broker;
    Transport::Address publicAddress;
    UdpSocket* stunServerSocket;
    sockaddr_in* stunServerSockAddr;
    int brokerSessionId;
    char brokerRequestContent[2000];
    
    /** The parameters of the setup game. */
    GameParams newParams;
    
    /** The cuurent state in the state machine of setting up a game. */
    int setupState;
    static const int SETUP_INIT;
    static const int SETUP_REQUEST_PUBLIC_IP;
    static const int SETUP_WAITING_FOR_PUBLIC_IP;
    static const int SETUP_MAKE_BROKER_REQUEST;
    static const int SETUP_WAITING_FOR_BROKERING;
    static const int SETUP_PAUSE_BEFORE_CONNECTING;
    static const int SETUP_INIT_CONNECT_WITH_PLAYERS;
    static const int SETUP_CONNECTING_WITH_PLAYERS;
    static const int SETUP_CONNECTED;
    static const int SETUP_FAILED;

    /** Whether we need to determin our public IP address.*/
    bool needPublicIp;
    
    /** Whether we need to get other player information from a broker */
    bool isBrokeredGame;
    
    /** Whether to run a simple UDP connection test and exit */
    bool isConnectTest;
    
    /** If there is a timeout in play (e.g. we've just sent a UDP to the broker
     * and are waiting for a UDP response) this is when the timeout started. */
    long timeoutStart;
    
    void askForPublicAddress();
    
    Transport::Address checkForPublicAddress();
    
    void craftBrokerRequest(Transport::Address);
    
    bool pollBroker();

    void setupP2PGame(GameSetup::GameParams& newParams, int argc, char** argv);
    
    bool hasExpired();

    RestClient& client;
    
    UdpTransport& xport;
    
    /** Send a UDP packet out on the designated port in attempt to keep firewall/NAT from reclaiming port. */
    void keepPortOpen();

	/** The game has successfully been setup and will start.  Clean up any resources from game setup. */
	void doSetupConnected();

};

#endif /* GameSetup_hpp */
