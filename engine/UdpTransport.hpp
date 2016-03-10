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
     * sleep - something to call sleep in a platform specific way
     */
    UdpTransport();
    
    /**
     * Connect to another game using UDP.
     * myExternalIp - the IP address my packets appear as
     * myExternaPort - the port my packets appear to come from
     * theirIp - the ip of the machine to connect to
     * theirPort - the port to connect to
     * sleep - something to call sleep in a platform specific way
     */
    UdpTransport(const char* myExternalIp, int myExternalPort,
                 const char* theirIp, int theirPort);
    
    ~UdpTransport();
    
    void connect();
    
protected:
    
    const char* myExternalIp;
    
    int myExternalPort;
    
    int myInternalPort;
    
    const char* theirIp;
    
    int theirPort;
    
    virtual int openSocket() = 0;
        
private:
    
    void punchHole();
    
    void compareNumbers(int myRandomNumber, char* theirMessage);
    
    
};

#endif /* UdpTransport_hpp */
