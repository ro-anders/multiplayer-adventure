

#ifndef RestClient_hpp
#define RestClient_hpp

#include <stdio.h>

#include "Transport.hpp"


class RestClient {
public:
    
    RestClient();
    
    void setAddress(const Transport::Address& address);
    
    int get(const char* path, char* responseBuffer, int bufferLength);

    int post(const char* path, const char* content, char* responseBuffer, int bufferLength);
    
    /** The name of the REST server that will broker the game. Also doubles as a STUN server (well technically not STUN
     but serves the same role as one). */
    static const char* DEFAULT_BROKER_SERVER;
    
    /** The port to make REST calls to */
    static const int REST_PORT;
    
    /** The port to send UDP messages to to identify public ip */
    static const int STUN_PORT;
    
protected:
    
    Transport::Address brokerAddress;
    
    int stripOffHeaders(char* buffer, int charsInBuffer);
    
    virtual int request(const char* path, const char* message, char* responseBuffer, int bufferLength) = 0;


    
};

#endif /* RestClient_hpp */

