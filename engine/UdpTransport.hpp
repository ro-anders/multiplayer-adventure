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
    
protected:
    
    Address myExternalAddr;
    
    int myInternalPort;
    
    Address theirAddr;
    
    virtual int openSocket() = 0;
        
private:
    
    void punchHole();
    
    void compareNumbers(int myRandomNumber, char* theirMessage);
    
    
};

#endif /* UdpTransport_hpp */
