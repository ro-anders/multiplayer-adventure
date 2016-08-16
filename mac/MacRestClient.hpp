
#ifndef MacRestClient_hpp
#define MacRestClient_hpp

#include <stdio.h>
#include "RestClient.hpp"


class MacRestClient: public RestClient {
public:
    
    static void test();
    static void mimicServer();
    
protected:
    
    int request(const char* path, const char* message, char* responseBuffer, int bufferLength);

};

#endif /* MacRestClient_hpp */
