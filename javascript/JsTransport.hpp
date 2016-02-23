//
//  JsTransport.hpp
//  MacAdventure
//

#ifndef JsTransport_hpp
#define JsTransport_hpp

#include <stdio.h>
#include "../engine/Transport.hpp"

class JsTransport: public Transport {
    
public:
    
    /**
     * Create a socket to this machine on the default port.  First try to open
     * a server socket, but if the port is already busy open up a client socket.
     * Useful for testing.
     */
    JsTransport();

    /**
     * Create a server socket.
     * port - the port to listen on.  If 0, will listen on the default port.
     */
    JsTransport(int port);

    /**
     * Connect a socket to another machine.
     * ip - the ip of the machine to connect to
     * port - the port to connect to.  If 0, will listen on the default port.
     */
    JsTransport(char* ip, int port);

    int sendPacket(const char* packetData);
    
    int getPacket(char* buffer, int bufferLength);


    ~JsTransport();
                    
protected:

    int openServerSocket();
    
    int openClientSocket();

    int writeData(const char* data, int numBytes);
    
    int readData(char* buffer, int bufferLength);

    void logError(const char* message);

private:
    
};

#endif /* JsTransport_hpp */
