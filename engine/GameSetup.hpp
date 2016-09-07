

#ifndef GameSetup_hpp
#define GameSetup_hpp

#include <stdio.h>

#include "Transport.hpp"

class RestClient;
class UdpTransport;

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
        
        bool shouldMute; // Whether other games are running on the same machine (usually only for dev and testing) and this game should mute itself
        int numberPlayers; // How many players are in the game
        int thisPlayer; // Which player this game is playing.
        int gameLevel; // Which game to play (refer to GAME_MODE_ enum)
        bool isScripting; // Whether this is a real game or just executing/authoring a script
        
        GameParams();
        GameParams(const GameParams& other);
        GameParams& operator=(const GameParams&);
        bool ok();
    };
    
    GameSetup(RestClient& client, UdpTransport& transport);
    
    /**
     * Reads command line arguments and sets up game.
     * On some OS's argv includes the executable name as the first argument, but on others it does not.
     * This assumes it DOES NOT, so if OS does, call setup(argc-1, argv+1).
     */
    GameParams setup(int argc, char** argv);
    
private:

    void setupBrokeredGame(GameSetup::GameParams& newParams, int argc, char** argv);
    
    void checkExpirationDate();

    RestClient& client;
    
    UdpTransport& xport;
    
    Transport::Address determinePublicAddress(Transport::Address stunServer);
};

#endif /* GameSetup_hpp */
