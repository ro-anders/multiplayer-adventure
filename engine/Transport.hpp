//
//  Transport.hpp

#ifndef Transport_hpp
#define Transport_hpp

#include <stdio.h>

class Logger;

class Transport {
public:
    
    static const int DEFAULT_PORT;
    
    /**
     * Create a socket to this machine on the default port.  First try to open
     * a server socket, but if the port is already busy open up a client socket.
     * Useful for testing.
     */
    Transport();
    
    /**
     * Create a server socket.
     * port - the port to listen on.  If 0, will listen on the default port.
     */
    Transport(int port);
    
    /**
     * Connect a socket to another machine.
     * ip - the ip of the machine to connect to
     * port - the port to connect to.  If 0, will listen on the default port.
     */
    Transport(char* ip, int port);
    
    virtual ~Transport() = 0;
    
    virtual void connect();
    
    /**
     * Send a packet to a client.
     * Returns the number of bytes sent.
     */
    virtual int sendPacket(const char* packetData);
    
    /**
     * Polls the client for a message.  If found fills the buffer with the message
     * and returns the number of bytes in the message.  If no message, returns 0 and
     * leaves the buffer untouched.
     */
    virtual int getPacket(char* buffer, int bufferLength);
    
    /**
     * Often when testing we want to quickly launch two ends of a socket and let them
     * figure out which should be the server and which the client.  In that case this 
     * can let you know how it worked out.  The first one to connect
     * will return 0.  The second one will return 1.
     */
    int getConnectNumber() {
        return connectNumber;
    }
    
    /**
     * Parse an socket address of the form 127.0.0.1:5678 into an ip/address and a port.
     * TODO: This does weird things with the input string.  It modifies it and requires is not
     * be deleted.
     */
    static void parseUrl(char* socketAddress, char** outIp, int* outPort);
    
    static void setLogger(Logger* logger);
    
    
protected:
    /** A constant used for the IP when you don't know if this is going to be a server or client socket. */
    static char* UNSPECIFIED;
    
    /** Character used to signify the end of the packet */
    static const char* PACKET_DELIMETER;
    
    /** The order in which this socket connected.  0 for first (server socket), 1 for second (client socket) */
    int connectNumber;
    
    /**
     * The IP of the socket address that we are connecting to, or null if this is to be a server socket, or
     * UNSPECIFIED if this may be a server socket listening on localhost or this may be a client socket connecting to 
     * localhost and which it is depends on who connects first.
     */
    const char* ip;
    
    /** The port of the socket address. */
    int port;
    
    /** Buffer to store data until end of packet is reached. */
    char* streamBuffer;
    
    /** Size of stream buffer */
    int streamBufferSize;
    
    /** Number of characters read into stream buffer */
    int charsInStreamBuffer;
    
    static Logger* logger;
    
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
	/**
	* Setup buffers.
	* Code common to all three constructors.
	*/
	virtual void setup();

};

#endif /* Transport_hpp */
