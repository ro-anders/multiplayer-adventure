//
//  UdpTransport.hpp
//  MacAdventure
//

#ifndef UdpTransport_hpp
#define UdpTransport_hpp

#include <stdio.h>

#include "Transport.hpp"

class UdpTransport: public Transport {
    
public:
    
    /**
     * Used only for in testing when running two games on one machine.  Attempts to listen first on the
     * default port and, if that is taken by the other game, on the default port + 1.
     */
    UdpTransport();
    
    /**
     * Connect to another game using UDP.
     * myExternalAddr - the IP address and port my packets appear to come from
     * theirIp - the ip and port of the machine to connect to
     */
    UdpTransport(const Address& myExternalAddrconst, const Address & theirAddr);
    
    /**
     * Connect to two other games using UDP.
     * myExternalAddr - the IP address and port my packets appear to come from
     * other1 - the ip and port of the first machine to connect to
     * other2 - the ip and port of the second machine to connect to
     */
    UdpTransport(const Address& myExternalAddrconst, const Address & other1, const Address& other2);
    
    ~UdpTransport();
    
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
    const int numOtherMachines;
    
    /**
     * Pull data off the socket - non-blocking.  If connected to multiple machines, will
     * return data from either machine.
     */
    int readData(char* buffer, int bufferLength);
    
    /**
     * Send data on the socket.  If connected to multiple machines, will send
     * data to both machines.
     */
    int writeData(const char* data, int numBytes);
    

    virtual int openSocket() = 0;
    
    /**
     * Pull data off the socket - non-blocking.
     * buffer - buffer to put read data into
     * bufferLength - maximum number of characters to read in
     * from - address field to fill in with who sent the message.  If null, no 
     *        sender information will be returned.
     */
    virtual int readData(char* buffer, int bufferLength, Address* from) = 0;
    
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
    
    /** An array of the states of the UDP connection (a state is a char*) */
    const char** states;
    
    /** A random number we use to determine test number */
    long randomNum;
    
    /** Look for connection messages from other machines, and send a 
     * connection message to each machine that isn't acknowledged yet */
    void punchHole();
    
    void compareNumbers(int myRandomNumber, char* theirMessage, int otherIndex);
    
    
};

#endif /* UdpTransport_hpp */
