

#ifndef UdpSocket_hpp
#define UdpSocket_hpp

#include <stdio.h>

#include "List.hpp"
#include "Transport.hpp"

struct sockaddr_in;

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
     * @source an internet address.  If server is a dns name, dnsLookup must be 
     * set to true.
     */
    virtual sockaddr_in* createAddress(Transport::Address source, bool dnsLookup=false) = 0;
    
    
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
    int readData(char* buffer, int bufferLength);
    
    /**
     * Pull data off the socket - non-blocking.  If connected to multiple machines, will
     * return data from either machine.
     * Will put the address of the source into from field.
     */
    int readData(char* buffer, int bufferLength, Transport::Address& from);
    
    /**
     * Send data on the socket.
     * data - data to send
     * numBytes - number of bytes to send (does not assume data is null terminated)
     * recipient - the address to send it to
     */
    virtual int writeData(const char* data, int numBytes, sockaddr_in* recipient) = 0;
    
    /**
     * Return a list of all IP4 addresses that this machine is using.
     */
    virtual List<Transport::Address> getLocalIps() = 0;
  
protected:

    /**
     * Pull data off the socket - non-blocking.  If connected to multiple machines, will
     * return data from either machine.
     * If a from address is passed in, will put the address of the source into that field.
     */
    virtual int readData(char* buffer, int bufferLength, Transport::Address* from) = 0;
    
};

#endif /* UdpSocket_hpp */
