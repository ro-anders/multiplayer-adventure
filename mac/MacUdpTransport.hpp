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
     * Create a socket to this machine on the default port.  First try to open
     * a server socket, but if the port is already busy open up a client socket.
     * Useful for testing.
     */
    MacUdpTransport();

    /**
     * Create a server socket.
     * port - the port to listen on.  If 0, will listen on the default port.
     */
    MacUdpTransport(int port);

    /**
     * Connect a socket to another machine.
     * ip - the ip of the machine to connect to
     * port - the port to connect to.  If 0, will listen on the default port.
     */
    MacUdpTransport(char* ip, int port);

    ~MacUdpTransport();
                
    static void testSockets();
    
protected:

    int openServerSocket();
    
    int openClientSocket();

    int writeData(const char* data, int numBytes);
    
    int readData(char* buffer, int bufferLength);

    void logError(const char* message);

private:
    
    int socketFd; // UDP Socket to send and receive
    
    struct sockaddr_in remaddr;
    
    // Code common to all consturctors.
    void setup();
    
};

#endif /* MacUdpTransport_hpp */
