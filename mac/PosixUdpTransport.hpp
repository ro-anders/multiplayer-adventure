//
//  PosixUdpTransport.hpp
//  MacAdventure
//

#ifndef PosixUdpTransport_hpp
#define PosixUdpTransport_hpp

#include <netinet/in.h>
#include <stdio.h>
#include "UdpTransport.hpp"

class PosixUdpTransport: public UdpTransport {
    
public:
    
    /**
     * Used only for in testing when running two games on one machine.  Attempts to listen first on the
     * default port and, if that is taken by the other game, on the default port + 1.
     */
    PosixUdpTransport();

    /**
     * Connect to another game using UDP.
     * myExternalAddr - the IP address and port my packets appear to come from
     * theirAddr - the ip and port of the machine to connect to
     */
    PosixUdpTransport(const Address& myExternalAddr, const Address& theirAddr);

    ~PosixUdpTransport();
    
    static void testSockets(const char* myExternalIp, int myExternalPort, const char* theirExternalIp, int theirExternalPort);
    
protected:

    int openSocket();
    
    int writeData(const char* data, int numBytes);
    
    int readData(char* buffer, int bufferLength);

private:
        
    int socketFd; // UDP Socket to send and receive
    
    struct sockaddr_in remaddr; // Destination we are sending to
    
    // Code common to all consturctors.
    void setup();
    
};

#endif /* PosixUdpTransport_hpp */
