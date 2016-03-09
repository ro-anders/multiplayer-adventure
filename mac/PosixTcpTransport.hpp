//
//  PosixTcpTransport.hpp
//  MacAdventure
//

#ifndef PosixTcpTransport_hpp
#define PosixTcpTransport_hpp

#include <stdio.h>
#include "TcpTransport.hpp"

class PosixTcpTransport: public TcpTransport {
    
public:
    
    /**
     * Create a socket to this machine on the default port.  First try to open
     * a server socket, but if the port is already busy open up a client socket.
     * Useful for testing.
     */
    PosixTcpTransport();

    /**
     * Create a server socket.
     * port - the port to listen on.  If 0, will listen on the default port.
     */
    PosixTcpTransport(int port);

    /**
     * Connect a socket to another machine.
     * ip - the ip of the machine to connect to
     * port - the port to connect to.  If 0, will listen on the default port.
     */
    PosixTcpTransport(char* ip, int port);

    ~PosixTcpTransport();
                
    static void testSockets();
    
protected:

    int openServerSocket();
    
    int openClientSocket();

    int writeData(const char* data, int numBytes);
    
    int readData(char* buffer, int bufferLength);

private:
    
    int serverSocketFd; // Only used if this is the server-side of the socket
    
    int socketFd; // Used both if this is the client-side or the server-side of the socket
    
};

#endif /* PosixTcpTransport_hpp */
