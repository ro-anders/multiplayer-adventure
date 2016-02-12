
#ifndef JSTransport_hpp
#define JSTransport_hpp

#include "../engine/Transport.hpp"

#include <stdio.h>

class JSTransport: public Transport {
public:
    JSTransport();
    
    ~JSTransport();
    
    void connect();
    
    int sendPacket(const char* packetData);
    
    int getPacket(char* buffer, int bufferLength);
    
protected:
    
    // None of these methods are used, but to conform to Transport they have to be defined.
    int openServerSocket();
    int openClientSocket();
    int writeData(const char* data, int numBytes);
    int readData(char* buffer, int bufferLength);
    
    
private:
    
};
#endif /* JSTransport_hpp */
