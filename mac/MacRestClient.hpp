
#ifndef MacRestClient_hpp
#define MacRestClient_hpp

#include <stdio.h>
#include "RestClient.hpp"


class MacRestClient: public RestClient {
public:
    int post(const char* path, const char* content, char* responseBuffer, int bufferLength);

    int get(const char* path, char* responseBuffer, int bufferLength);

    static void test();
    static void mimicServer();
    
private:
    int stripOffHeaders(char* buffer, int charsInBuffer);
};

#endif /* MacRestClient_hpp */
