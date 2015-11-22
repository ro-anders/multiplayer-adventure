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
    
    static void testSockets();

private:
    
    const char PACKET_DELIMETER; // Character used to signify end of packet.
    
    int serverSocketFd; // Only used if this is the server-side of the socket
    
    int socketFd; // Used both if this is the client-side or the server-side of the socket
    
    char* streamBuffer; // Buffer to store data until end of packet is reached.
    
    int streamBufferSize; // Size of stream buffer
    
    int charsInStreamBuffer; // Number of characters read into stream buffer
    
    int openServerSocket(int port);
    
    void openClientSocket(int port);
    
    void error(const char* message);
};

#endif /* MacTransport_hpp */
