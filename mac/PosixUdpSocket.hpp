
#ifndef PosixUdpSocket_hpp
#define PosixUdpSocket_hpp

#include <netinet/in.h>
#include <stdio.h>
#include "UdpSocket.hpp"

class PosixUdpSocket: public UdpSocket {
    
public:
    
    
    PosixUdpSocket();
    
    ~PosixUdpSocket();
    
    
    /**
     * Creates an OS specific socket address.
     */
    sockaddr_in* createAddress(Transport::Address source, bool dnsLookup=false);
    
    /**
     * Delete a OS specific socket address.
     */
    void deleteAddress(sockaddr_in* socketAddress);
    
    /**
     * Bind to the server socket.
     */
    int bind(int port);
    
    /**
     * Whether or not this socket should block.
     */
    void setBlocking(bool shouldBlock);
    
    /**
     * How long the read should listen for data before giving up.  In seconds.
     */
    void setTimeout(int seconds);

    /**
     * Send data on the socket.
     * data - data to send
     * numBytes - number of bytes to send (does not assume data is null terminated)
     * recipient - the address to send it to
     */
    int writeData(const char* data, int numBytes, sockaddr_in* recipient);
    
    
    int readData(char* buffer, int bufferLength, Transport::Address* source);
    
    /**
     * Return a list of all IP4 addresses that this machine is using.
     */
    List<Transport::Address> getLocalIps();

    
    
private:
    
    int socketFd; // UDP Socket to send and receive
    
    struct sockaddr_in* remaddrs; // Destinations we are sending to
    
    struct sockaddr_in  sender; // The sender of a message we just received

};


#endif /* PosixUdpSocket_hpp */
