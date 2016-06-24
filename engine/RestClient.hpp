

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
    
    virtual int post(const char* path, const char* content, char* responseBuffer, int bufferLength) = 0;
};
