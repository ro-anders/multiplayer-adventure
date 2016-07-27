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
    UdpTransport(UdpSocket* socket, bool isTest);
    
    /**
     * Used only for in testing when running two games on one machine.  Attempts to listen first on the
     * default port and, if that is taken by the other game, on the default port + 1.
     * socket - an uninitialized socket to handle the passing of UDP packets
     */
    UdpTransport(UdpSocket* socket);
    
    /**
     * Connect to another game using UDP.
     * socket - an uninitialized socket to handle the passing of UDP packets
     * myExternalAddr - the IP address and port my packets appear to come from
     * theirIp - the ip and port of the machine to connect to
     */
    UdpTransport(UdpSocket* socket, const Address& myExternalAddrconst, const Address & theirAddr);
    
    /**
     * Connect to two other games using UDP.
     * socket - an uninitialized socket to handle the passing of UDP packets
     * myExternalAddr - the IP address and port my packets appear to come from
     * transportNum - the three machines have an order in which they are declared.  This is this machine's placement in that order.
     * other1 - the ip and port of the first machine to connect to
     * other2 - the ip and port of the second machine to connect to
     */
    UdpTransport(UdpSocket* socket, const Address& myExternalAddrconst, int transportNum, const Address & other1, const Address& other2);
    
    ~UdpTransport();
    
    /**
     * Set the number this transport uses to identify itself to other transports.
     * This number is vestigial when there are only two machines in the game but is used when there are three.
     */
    void setTransportNum(int transportNum);
    
    /**
     * Registers another player.  Players needed to be added in the order of their transport number.
     * If adding more than two will silently fail.
     */
    void addOtherPlayer(const Address & theirAdddr);
    
    /**
     * This will bind to a port even though it is not yet setup to receive messages from other games.
     * It is used mostly to determine before setup what our port will look like to other machines on the internet.
     */
    int reservePort();
    
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
    

    virtual int openSocket() = 0;
        
    /**
     * Send data on the socket.
     * data - data to send
     * numBytes - number of bytes to send (does not assume data is null terminated)
     * recipient - the index in the theirAddrs array of the address to send the data.  -1 will send to all addresses.
     */
    virtual int writeData(const char* data, int numBytes, int recipient) = 0;

    
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
    
    /** 0, 1, or 2.  The machines in the game are specified with an ordering consistent across the three games.
     * This is this machine's place in that ordering, though in just a two player game it is not needed and
     * will always be 0. */
    int transportNum;
    
    /** A random number we use to determine test number */
    long randomNum;
    
    /** Look for connection messages from other machines, and send a 
     * connection message to each machine that isn't acknowledged yet */
    void punchHole();
    
    void compareNumbers(int myRandomNumber, char* theirMessage, int otherIndex);
    
    
};

#endif /* UdpTransport_hpp */
