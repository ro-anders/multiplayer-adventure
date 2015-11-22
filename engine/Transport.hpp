//
//  Transport.hpp
//  MacAdventure
//
//  Created by Robert Antonucci on 11/10/15.
//
//

#ifndef Transport_hpp
#define Transport_hpp

#include <stdio.h>

class Transport {
public:
    virtual void connect() = 0;
    
    virtual int sendPacket(const char* packetData) = 0;
    
    virtual int getPacket(char* buffer, int bufferLength) = 0;
    
    int getConnectNumber() {
        return connectNumber;
    }
    
    
protected:
    int connectNumber = 0;

};

#endif /* Transport_hpp */
