//
//  UdpTransport.hpp
//  MacAdventure
//

#ifndef UdpTransport_hpp
#define UdpTransport_hpp

#include <stdio.h>

#include "List.hpp"
#include "Transport.hpp"

class UdpSocket;

class UdpTransport: public Transport {
    
public:
    
    /**
     * Create a UdpTransport.
     */
    UdpTransport();
    
    ~UdpTransport();
    
    /**
     * Give the transport an implementation of a socket to connect and communicate with other clients.
     */
    void useSocket(UdpSocket* socket);
    
    /**
     * Often when testing we want to quickly launch two ends of a socket and let them
     * figure out who should be player one vs player two.
     * Call this method to make that happen.
     * Only works when running two clients on the same local machine.
     */
    void useDynamicPlayerSetup();
    
    void setInternalPort(int port);
    
    /**
     * Registers another player.  Players needed to be added in the order of their transport number.
     * If adding more than two will silently fail.
     */
    void addOtherPlayer(const Address & theirAdddr);
    
    /**
     * Registers another player. Players needed to be added in the order of their transport number.
     * If adding more than two will silently fail.
     * addresses - all the addresses the player may use
     * numAddresses - number of addresses in the addresses list
     */
    void addOtherPlayer(const Address * addresses, int numAddresses);
    
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
 
    /**
     * Deduce all the IPs that this machine is using.  Does not include localhost.
     */
    List<Address> determineThisMachineIPs();

    
private:
    
    static const char* NOT_YET_INITIATED;
    static const char* RECVD_NOTHING;
    static const char* RECVD_MESSAGE;
    static const char* RECVD_ACK;
    
    int myInternalPort;
    
    /** IP addresses that this machine's NIC is using. */
    char** internalIps;
    
    /** Number ips in internalIps list */
    int numInternalIps;
    
    /** Whether the socket has been opened or not. */
    bool socketBound;
    
    class Client {
    public:
        List<Address> possibleAddrs;
        List<struct sockaddr_in*> sockaddrs;
        Address address;
        Client();
        ~Client();
    };
    
    /** Array of the other machines addresses, one for each other machine. */
    Client* otherMachines;
    
    /** Whether comminicating with one or two other machines */
    int numOtherMachines;
    
    /**
     * All OS specific communication is encapsulated by the socket class.
     */
    UdpSocket* socket;
    
    /** An array of the states of the UDP connection (a state is a char*) */
    const char** states;
    
    /** A random number we use to determine test number */
    long randomNum;
    
    /**
     * Pull data off the socket - non-blocking
     * Put the address of the source of the data in from.
     */
    int readData(char* buffer, int bufferLength);
    
    int openSocket();

    /**
     * Send data on the socket.  If connected to multiple machines, will send
     * data to both machines.
     */
    int writeData(const char* data, int numBytes);
    

    /**
     * Send data on the socket.
     * data - data to send
     * numBytes - number of bytes to send (does not assume data is null terminated)
     * recipient - the index in the theirAddrs array of the address to send the data.  -1 will send to all addresses.
     */
    int writeData(const char* data, int numBytes, int recipient);

    /** Look for connection messages from other machines, and send a
     * connection message to each machine that isn't acknowledged yet */
    void punchHole();
    
    /**
     * Remove address and socket entries in this client that don't match the passed in one.
     * If the passed in one is not one of the machines addresses don't do anything.
     * clientNum - the player number of the client - used only in debugging and error reporting
     * otherMachine - the remote machine for which we have multiple addresses
     * from - the address we want to keep
     */
    void reduceClientToOneAddress(int clientNum, Client& otherMachine, Transport::Address& from);
    
    void compareNumbers(int myRandomNumber, char* theirMessage, int otherIndex);
    
    
};

#endif /* UdpTransport_hpp */
