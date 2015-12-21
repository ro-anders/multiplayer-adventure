//
//  Transport.hpp

#ifndef Transport_hpp
#define Transport_hpp

#include <stdio.h>

class Transport {
public:
    
    // Useful constants which are common among some of the derived classes.
    static const int ROLE_CLIENT;
    static const int ROLE_SERVER;
    static const int ROLE_UNSPECIFIED;
    static const int DEFAULT_PORT;
    
    
    virtual ~Transport() = 0;
    
    virtual void connect() = 0;
    
    virtual int sendPacket(const char* packetData) = 0;
    
    virtual int getPacket(char* buffer, int bufferLength) = 0;
    
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
    

    
protected:
    int connectNumber = 0;
    

};

#endif /* Transport_hpp */
