
#ifndef YTransport_hpp
#define YTransport_hpp

#include "Transport.hpp"

#include <stdio.h>

/**
 * This takes a two tranports to two different clients and provides an interface to both through one transport.
 */
class YTransport: public Transport {
public:
    /**
     * Contructs a single facade that serves both transports.
     * YTransport assumes responsibility for deleting the transports.
     */
    YTransport(Transport* xport1, Transport* xport2);
    
    ~YTransport();
    
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
    Transport** xports;
    
    int numXports;
    
    int lastQueried;
    
};
#endif /* YTransport_hpp */
