//
//  MacUdpTransport.hpp
//  MacAdventure
//

#ifndef MacUdpTransport_hpp
#define MacUdpTransport_hpp

#include <netinet/in.h>
#include <stdio.h>
#include "Transport.hpp"

class MacUdpTransport: public Transport {
    
public:
    
    /**
     * Used only for in testing when running two games on one machine.  Attempts to listen first on the
     * default port and, if that is taken by the other game, on the default port + 1.
     */
    MacUdpTransport();

    /**
     * Connect to another game using UDP.
     * myExternalIp - the IP address my packets appear as
     * myExternaPort - the port my packets appear to come from
     * theirIp - the ip of the machine to connect to
     * theirPort - the port to connect to
     */
    MacUdpTransport(const char* myExternalIp, int myExternalPort, const char* ip, int port);

    ~MacUdpTransport();
    
    void connect();
    
    static void testSockets(const char* myExternalIp, int myExternalPort, const char* theirExternalIp, int theirExternalPort);
    
protected:

    int openServerSocket();
    
    int openClientSocket();

    int writeData(const char* data, int numBytes);
    
    int readData(char* buffer, int bufferLength);

private:
    
    const char* myExternalIp;
    
    int myExternalPort;
    
    int myInternalPort;
    
    int socketFd; // UDP Socket to send and receive
    
    struct sockaddr_in remaddr;
    
    // Code common to all consturctors.
    void setup();
    
    /**
     * We don't have a client/server relationship, so there is just openSocket().
     */
    int openSocket();
    
    void punchHole();
    
    void compareNumbers(int myRandomNumber, char* theirMessage);


};

#endif /* MacUdpTransport_hpp */
