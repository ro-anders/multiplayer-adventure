

#ifndef RestClient_hpp
#define RestClient_hpp

#include <stdio.h>

#endif /* RestClient_hpp */

/**
 * A real simple map of strings to strings until we figure out if we need a thrid-party library
 * to handle REST communication
 */
class StringMap {
public:
    
    StringMap();
    
    ~StringMap();
    
    const char* get(const char* key);
    
    void put(const char* key, const char* value);
    
    void remove(const char* key);
    
private:
    int numEntries;
    
    int spaceAllocated;
    
    const char** keys;
    const char** values;
    
    int findIndex(const char* key);
    
    void allocateMoreSpace() {}
    
    const char* copyString(const char* source) {return NULL;}
    
};

class RestClient {
public:
    
    int get(const char* path, char* responseBuffer, int bufferLength);

    int post(const char* path, const char* content, char* responseBuffer, int bufferLength);
    
    /** The name of the REST server that will broker the game. Also doubles as a STUN server (well technically not STUN
     but serves the same role as one). */
    static const char* BROKER_SERVER;
    
    /** The port to make REST calls to */
    static const int REST_PORT;
    
    /** The port to send UDP messages to to identify public ip */
    static const int STUN_PORT;
    
protected:
    
    int stripOffHeaders(char* buffer, int charsInBuffer);
    
    virtual int request(const char* path, const char* message, char* responseBuffer, int bufferLength) = 0;


    
};
