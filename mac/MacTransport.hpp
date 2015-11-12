//
//  MacTransport.hpp
//  MacAdventure
//
//  Created by Robert Antonucci on 11/10/15.
//
//

#ifndef MacTransport_hpp
#define MacTransport_hpp

#include <stdio.h>
#include "Transport.hpp"

class MacTransport: public Transport {
    
public:
    
    MacTransport();
    
    ~MacTransport();
    
    void connect();
    
    int sendPacket(const char* packetData);
    
    int getPacket(char* buffer, int bufferLength);

private:
    
    int serverSocketFd; // Only used if this is the server-side of the socket
    
    int socketFd; // Used both if this is the client-side or the server-side of the socket
    
    int openServerSocket(int port);
    
    void openClientSocket(int port);
    
    void error(const char* message);
};

#endif /* MacTransport_hpp */
