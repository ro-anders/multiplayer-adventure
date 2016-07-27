

#ifndef UdpSocket_hpp
#define UdpSocket_hpp

#include <stdio.h>


/**
 * Meant as a virtual interface to an OS-specific UDP socket.
 */
class UdpSocket {
public:
    
    UdpSocket();
    
    virtual ~UdpSocket();
    
    /**
     * Pull data off the socket - non-blocking.  If connected to multiple machines, will
     * return data from either machine.
     */
    virtual int readData(char* buffer, int bufferLength) = 0;
    
    /**
     * Send data on the socket.  If connected to multiple machines, will send
     * data to both machines.
     */
    virtual int writeData(const char* data, int numBytes);

};

#endif /* UdpSocket_hpp */
