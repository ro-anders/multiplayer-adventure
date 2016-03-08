//
//  MacTransport.hpp
//  MacAdventure
//

#ifndef MacTransport_hpp
#define MacTransport_hpp

#include <stdio.h>
#include "Transport.hpp"

class MacTransport: public Transport {
    
public:
    
    /**
     * Create a socket to this machine on the default port.  First try to open
     * a server socket, but if the port is already busy open up a client socket.
     * Useful for testing.
     */
    MacTransport();

    /**
     * Create a server socket.
     * port - the port to listen on.  If 0, will listen on the default port.
     */
    MacTransport(int port);

    /**
     * Connect a socket to another machine.
     * ip - the ip of the machine to connect to
     * port - the port to connect to.  If 0, will listen on the default port.
     */
    MacTransport(char* ip, int port);

    ~MacTransport();
                
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

#endif /* MacTransport_hpp */
