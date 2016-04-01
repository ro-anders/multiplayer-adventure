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
    
    Address theirAddr;

    /**
     * Pull data off the socket - non-blocking
     */
    int readData(char* buffer, int bufferLength);
    

    virtual int openSocket() = 0;
    
    /**
     * Pull data off the socket - non-blocking
     */
    virtual int readData(char* buffer, int bufferLength, Address* from) = 0;

    
private:
    
    static const char* NOT_YET_INITIATED;
    static const char* RECVD_NOTHING;
    static const char* RECVD_MESSAGE;
    static const char* RECVD_ACK;

    /* The state of the UDP connection */
    const char* state;
    
    /** A random number we use to determine test number */
    long randomNum;
    
    void punchHole();
    
    void compareNumbers(int myRandomNumber, char* theirMessage);
    
    
};

#endif /* UdpTransport_hpp */
