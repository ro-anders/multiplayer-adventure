//
//  PosixUdpTransport.cpp
//  MacAdventure
//

#include "PosixUdpTransport.hpp"

// Socket includes
#include <arpa/inet.h>
#include <fcntl.h>
#include <netdb.h>
#include <netinet/in.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <sys/types.h>
#include <sys/socket.h>
#include <unistd.h>
// End socket includes

#include "Sys.hpp"

PosixUdpTransport::PosixUdpTransport(bool isTest) :
UdpTransport(isTest)
{
    setup();
}

PosixUdpTransport::PosixUdpTransport() :
UdpTransport()
{
    setup();
}

PosixUdpTransport::PosixUdpTransport(const Address& inMyExternalAddr,  const Address& inTheirAddr) :
UdpTransport(inMyExternalAddr, inTheirAddr)
{
    setup();
}

PosixUdpTransport::PosixUdpTransport(const Address& inMyExternalAddr,  int transportNum,
                                     const Address& otherAddr1, const Address& otherAddr2) :
UdpTransport(inMyExternalAddr, transportNum, otherAddr1, otherAddr2)
{
    setup();
}

PosixUdpTransport::~PosixUdpTransport() {
    if (socketFd > 0) {
        close(socketFd);
    }
    delete[] remaddrs;
}

void PosixUdpTransport::setup() {
    // Prepare (zero out) the blocks holding internet address information.
    
    memset((char *) &sender, 0, sizeof(sender));

    // At construction we don't know how manyremote machines there will be, so we just make
    // space for two.
    remaddrs = new sockaddr_in[2];
    for(int ctr=0; ctr<2; ++ctr) {
        memset((char *) &remaddrs[ctr], 0, sizeof(sender));
    }
}

int PosixUdpTransport::openSocket() {
 
    // Create the structures that represent the end-points for the other machines
    for(int ctr=0; ctr<numOtherMachines; ++ctr) {
        // TODO: Fix this
        // ip is an ip, not a hostname, but don't know how to convert a
        // string ip to a server address format, so calling gethost - ugh
        // Should be remaddr.sin_addr.S_un.S_addr = inet_addr(theirIp)  - or something like that
        hostent* hp = gethostbyname(theirAddrs[ctr].ip());
        bcopy((char *)hp->h_addr,
              (char *)&remaddrs[ctr].sin_addr.s_addr,
              hp->h_length);
        remaddrs[ctr].sin_family = AF_INET;
        remaddrs[ctr].sin_port = htons(theirAddrs[ctr].port());
        printf("Initialized = %s:%d.\n", inet_ntoa(remaddrs[ctr].sin_addr), ntohs(remaddrs[ctr].sin_port));
    }

    // Create the server socket and bind to it
    socketFd = socket(AF_INET, SOCK_DGRAM, 0);
    if (socketFd < 0) {
        Sys::log("ERROR opening socket");
        return TPT_ERROR;
    }
    struct sockaddr_in serv_addr;
    memset((char *) &serv_addr, 0, sizeof(serv_addr));
    serv_addr.sin_family = AF_INET;
    serv_addr.sin_addr.s_addr = htonl(INADDR_ANY);
    serv_addr.sin_port = htons(myInternalPort);
    printf("Opening socket on port %d\n", ntohs(serv_addr.sin_port));
    if (bind(socketFd, (struct sockaddr *) &serv_addr, sizeof(serv_addr)) < 0) {
        // Assume it is because another process is listening and we should instead launch the client
        return Transport::TPT_BUSY;
    }

    // Set to non-blocking receive
    fcntl(socketFd, F_SETFL, O_NONBLOCK);

    return Transport::TPT_OK;
}

int PosixUdpTransport::writeData(const char* data, int numBytes, int recipient)
{
    int numSent = 0; // If sending to multiple machines, we just return what one send reported.
    for(int ctr=0; ctr<numOtherMachines; ++ctr) {
        if ((recipient < 0) || (ctr == recipient)) {
            numSent = sendto(socketFd, data, numBytes, 0, (struct sockaddr *)&remaddrs[ctr], sizeof(remaddrs[ctr]));
        }
    }
    return numSent;
}

int PosixUdpTransport::readData(char *buffer, int bufferLength) {
    // Receive the next packet
    int n = recvfrom(socketFd, buffer, bufferLength, 0, NULL, NULL);
    return n;
}


void PosixUdpTransport::testSockets(const char* myExternalIp, int myExternalPort, const char* theirExternalIp, int theirExternalPort)
{
    PosixUdpTransport t;
    Transport::testTransport(t);
}
