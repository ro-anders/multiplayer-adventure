//
//  UdpTransport.hpp
//  MacAdventure
//

#ifndef UdpTransport_hpp
#define UdpTransport_hpp

#include <stdio.h>

#include "Transport.hpp"

class UdpSocket;

class UdpTransport: public Transport {
    
public:
    
    /**
     * Create a UdpTransport.
     * socket - an uninitialized socket to handle the passing of UDP packets
     * isTest - if running this in a development environment for testing something we want the transport
     * to figure out how to talk to another test instance running on the same local host with no other
     * information.  Otherwise, more information needs to be dictated before the transport can connect.
     */
    UdpTransport(UdpSocket* socket, bool useDynamicSetup);
    
    ~UdpTransport();
    
    /**
     * The ip address to tell other machines to use to talk to this machine.
     */
    void setExternalAddress(const Address& myExternalAddr);
    
    void setInternalPort(int port);
    
    /**
     * Registers another player.  Players needed to be added in the order of their transport number.
     * If adding more than two will silently fail.
     */
    void addOtherPlayer(const Address & theirAdddr);
    
    /**
     * This will bind to a port even though it is not yet setup to receive messages from other games.
     * It is used mostly to determine before setup what our port will look like to other machines on the internet.
     */
    UdpSocket& reservePort();
    
    void connect();
    
    /**
     * We override isConnected() to not only check to see if we have connected but also send appropriate
     * messages to the other machine.  If we never check to see if we are connected we will never connect.
     */
    bool isConnected();
    
    int getPacket(char* buffer, int bufferLength);
    
    
protected:
    
    Address myExternalAddr;
    
    int myInternalPort;
    
    /** Whether the socket has been opened or not. */
    bool socketBound;
    
    /** Array of the other machines addresses, one for each other machine. */
    Address* theirAddrs;
    
    /** Whether comminicating with one or two other machines */
    int numOtherMachines;
    
    /**
     * Pull data off the socket - non-blocking
     */
    int readData(char* buffer, int bufferLength);

    /**
     * Send data on the socket.  If connected to multiple machines, will send
     * data to both machines.
     */
    int writeData(const char* data, int numBytes);
    

    int openSocket();
        
    /**
     * Send data on the socket.
     * data - data to send
     * numBytes - number of bytes to send (does not assume data is null terminated)
     * recipient - the index in the theirAddrs array of the address to send the data.  -1 will send to all addresses.
     */
    int writeData(const char* data, int numBytes, int recipient);

    
private:
    
    static const char* NOT_YET_INITIATED;
    static const char* RECVD_NOTHING;
    static const char* RECVD_MESSAGE;
    static const char* RECVD_ACK;
    
    /**
     * All OS specific communication is encapsulated by the socket class.
     */
    UdpSocket* socket;
    
    /** An array of the states of the UDP connection (a state is a char*) */
    const char** states;
    
    /** A random number we use to determine test number */
    long randomNum;
    
    /** Destinations we are sending to */
    struct sockaddr_in** remaddrs;

    
    /** Look for connection messages from other machines, and send a 
     * connection message to each machine that isn't acknowledged yet */
    void punchHole();
    
    void compareNumbers(int myRandomNumber, char* theirMessage, int otherIndex);
    
    
};

#endif /* UdpTransport_hpp */
