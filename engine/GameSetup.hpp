

#ifndef GameSetup_hpp
#define GameSetup_hpp

#include <stdio.h>

#include "Transport.hpp"

class RestClient;

class GameSetup {
public:
    
    /**
     * A holder for all the info needed to setup a game.
     * Mostly the addresses of the other players but also
     * which level, which player this player is, what
     * the difficulty switches start out at
     */
    class GameParams {
    public:
        int numberPlayers;
        int thisPlayer;
        Transport::Address thisPlayerAddress;
        Transport::Address secondPlayerAddress;
        Transport::Address thirdPlayerAddress;
        
        GameParams();
        GameParams(const GameParams& other);
        GameParams& operator=(const GameParams&);
        bool ok();
    };
    
    GameParams setup(RestClient& client, Transport::Address myAddress);
};

#endif /* GameSetup_hpp */
