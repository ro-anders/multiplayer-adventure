//
//  TcpTransport.hpp

#ifndef TcpTransport_hpp
#define TcpTransport_hpp

#include <stdio.h>

#include "Transport.hpp"

class Logger;

class TcpTransport: public Transport {
public:
    
    /**
     * Create a socket to this machine on the default port.  First try to open
     * a server socket, but if the port is already busy open up a client socket.
     * Useful for testing.
     */
    TcpTransport();
    
    /**
     * Create a server socket.
     * port - the port to listen on.  If 0, will listen on the default port.
     */
    TcpTransport(int port);
    
    /**
     * Connect a socket to another machine.
     * ip - the ip of the machine to connect to
     * port - the port to connect to.  If 0, will listen on the default port.
     */
    TcpTransport(const char* ip, int port);
    
    virtual ~TcpTransport() = 0;
    
    void connect();
        
protected:
    
    /**
     * The IP of the socket address that we are connecting to, or null if this is to be a server socket, or
     * UNSPECIFIED in the testing case that two games are run on the same machiine.
     */
    const char* ip;
    
    /** The port that we are connecting to (or, if this is a server socket, the port to listen on) */
    int port;
    
    /**
     * Open a server socket.
     */
    virtual int openServerSocket() = 0;
    
    /**
     * Open a client socket.
     */
    virtual int openClientSocket() = 0;
    
    /**
     * Send data on the socket.
     */
    virtual int writeData(const char* data, int numBytes) = 0;
    
    /**
     * Pull data off the socket - non-blocking
     */
    virtual int readData(char* buffer, int bufferLength) = 0;
    
private:

};

#endif /* TcpTransport_hpp */
