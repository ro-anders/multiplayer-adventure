//
//  PosixUdpTransport.hpp
//  MacAdventure
//

#ifndef PosixUdpTransport_hpp
#define PosixUdpTransport_hpp

#include <netinet/in.h>
#include <stdio.h>
#include "UdpTransport.hpp"

class PosixUdpTransport: public UdpTransport {
    
public:
    
    /**
     * Create a UdpTransport.
     * isTest - if running this in a development environment for testing something we want the transport
     * to figure out how to talk to another test instance running on the same local host with no other
     * information.  Otherwise, more information needs to be dictated before the transport can connect.
     */
    PosixUdpTransport(bool isTest);
    
    /**
     * Used only for in testing when running two games on one machine.  Attempts to listen first on the
     * default port and, if that is taken by the other game, on the default port + 1.
     */
    PosixUdpTransport();
    
    /**
     * Connect to another game using UDP.
     * myExternalAddr - the IP address and port my packets appear to come from
     * theirAddr - the ip and port of the machine to connect to
     */
    PosixUdpTransport(const Address& myExternalAddr, const Address& theirAddr);
    
    /**
     * Connect to two other games using UDP.
     * myExternalAddr - the IP address and port my packets appear to come from
     * transportNum - the three machines have an order in which they are declared.  This is this machine's placement in that order.
     * otherAddr1 - the ip and port of the first machine to connect to (order is important)
     * otherAddr2 - the ip and port of the second machine to connect to (order is important)
     */
    PosixUdpTransport(const Address& myExternalAddr, int transportNum, const Address& otherAddr1, const Address& otherAddr2);
    
    ~PosixUdpTransport();
    
    static void testSockets(const char* myExternalIp, int myExternalPort, const char* theirExternalIp, int theirExternalPort);
    
protected:

    int openSocket();
    
    /**
     * Send data on the socket.
     * data - data to send
     * numBytes - number of bytes to send (does not assume data is null terminated)
     * recipient - the index in the theirAddrs array of the address to send the data.  -1 will send to all addresses.
     */
    int writeData(const char* data, int numBytes, int recipient);
    
private:
        
    int socketFd; // UDP Socket to send and receive
    
    struct sockaddr_in* remaddrs; // Destinations we are sending to
    
    struct sockaddr_in  sender; // The sender of a message we just received
    
    // Code common to all consturctors.
    void setup();
    
};

#endif /* PosixUdpTransport_hpp */
