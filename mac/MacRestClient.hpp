
#ifndef MacRestClient_hpp
#define MacRestClient_hpp

#include <stdio.h>

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

class MacRestClient {
public:
    int makeCall(char* buffer, int bufferLength);
    
    static void test();
};

#endif /* MacRestClient_hpp */
