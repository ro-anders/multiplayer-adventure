
#ifndef PosixUdpSocket_hpp
#define PosixUdpSocket_hpp

#include <stdio.h>

#include <netinet/in.h>
#include <stdio.h>
#include "UdpSocket.hpp"

class PosixUdpSocket: public UdpSocket {
    
public:
    
    
    /**
     * Used only for in testing when running two games on one machine.  Attempts to listen first on the
     * default port and, if that is taken by the other game, on the default port + 1.
     */
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
     * Send data on the socket.
     * data - data to send
     * numBytes - number of bytes to send (does not assume data is null terminated)
     * recipient - the address to send it to
     */
    int writeData(const char* data, int numBytes, sockaddr_in* recipient);
    
    int readData(char* buffer, int bufferLength);
    
    
private:
    
    int socketFd; // UDP Socket to send and receive
    
    struct sockaddr_in* remaddrs; // Destinations we are sending to
    
    struct sockaddr_in  sender; // The sender of a message we just received

};


#endif /* PosixUdpSocket_hpp */
