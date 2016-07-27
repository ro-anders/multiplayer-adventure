
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
    
    virtual ~PosixUdpSocket();
    
    
    /**
     * Send data on the socket.
     * data - data to send
     * numBytes - number of bytes to send (does not assume data is null terminated)
     * recipient - the index in the theirAddrs array of the address to send the data.  -1 will send to all addresses.
     */
    int writeData(const char* data, int numBytes, int recipient);
    
    int readData(char* buffer, int bufferLength);
    
    
private:
    
    int socketFd; // UDP Socket to send and receive
    
    struct sockaddr_in* remaddrs; // Destinations we are sending to
    
    struct sockaddr_in  sender; // The sender of a message we just received

};


#endif /* PosixUdpSocket_hpp */
