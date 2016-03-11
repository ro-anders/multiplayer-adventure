//
//  PosixUdpTransport.hpp
//  MacAdventure
//

#ifndef PosixUdpTransport_hpp
#define PosixUdpTransport_hpp

#include <stdio.h>
#include <winsock2.h>
#include "..\engine\UdpTransport.hpp"

class WinUdpTransport: public UdpTransport {
    
public:
    
    /**
     * Used only for in testing when running two games on one machine.  Attempts to listen first on the
     * default port and, if that is taken by the other game, on the default port + 1.
     */
    WinUdpTransport();

    /**
     * Connect to another game using UDP.
     * myExternalIp - the IP address my packets appear as
     * myExternaPort - the port my packets appear to come from
     * theirIp - the ip of the machine to connect to
     * theirPort - the port to connect to
     */
    WinUdpTransport(const char* myExternalIp, int myExternalPort,
                      const char* theirIp, int theirPort);

    ~WinUdpTransport();
    
    static void testSockets(const char* myExternalIp, int myExternalPort, const char* theirExternalIp, int theirExternalPort);
    
protected:

    int openSocket();
    
    int writeData(const char* data, int numBytes);
    
    int readData(char* buffer, int bufferLength);

private:
        
    SOCKET socket; // UDP Socket to send and receive
    
    struct sockaddr_in remaddr; // Destination we are sending to
    
    // Code common to all consturctors.
    void setup();
};

#endif /* PosixUdpTransport_hpp */
