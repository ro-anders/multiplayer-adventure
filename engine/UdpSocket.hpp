

#ifndef UdpSocket_hpp
#define UdpSocket_hpp

#include <stdio.h>

#include "Transport.hpp"

class sockaddr_in;

/**
 * Meant as a virtual interface to an OS-specific UDP socket.
 */
class UdpSocket {
public:
    
    UdpSocket();
    
    virtual ~UdpSocket();
    
    /**
     * Creates an OS specific socket address.  Does not affect this socket.
     * Caller is responsible for deleting the sockadd through deleteAddress()
     */
    virtual sockaddr_in* createAddress(Transport::Address source) = 0;
    
    /**
     * Delete a OS specific socket address.  Does not affect this socket.
     */
    virtual void deleteAddress(sockaddr_in* socketAddress) = 0;
    
    /**
     * Bind to the server socket.
     */
    virtual int bind(int port) = 0;
    
    /**
     * Whether or not this socket should block.
     */
    virtual void setBlocking(bool shouldBlock) = 0;

    /**
     * Pull data off the socket - non-blocking.  If connected to multiple machines, will
     * return data from either machine.
     */
    virtual int readData(char* buffer, int bufferLength) = 0;
    
    /**
     * Send data on the socket.
     * data - data to send
     * numBytes - number of bytes to send (does not assume data is null terminated)
     * recipient - the address to send it to
     */
    virtual int writeData(const char* data, int numBytes, sockaddr_in* recipient) = 0;
    
};

#endif /* UdpSocket_hpp */
