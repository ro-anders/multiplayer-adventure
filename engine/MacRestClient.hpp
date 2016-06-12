
#ifndef MacRestClient_hpp
#define MacRestClient_hpp

#include <stdio.h>

class MacRestClient {
public:
    int makeCall(char* buffer, int bufferLength);
    
    static void test();
};

#endif /* MacRestClient_hpp */
