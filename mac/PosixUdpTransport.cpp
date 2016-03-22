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

PosixUdpTransport::PosixUdpTransport() :
  UdpTransport()
{
    setup();
}

PosixUdpTransport::PosixUdpTransport(const char* inMyExternalIp, int inMyExternalPort,
                                     const char* inTheirIp, int inTheirPort) :
UdpTransport(inMyExternalIp, inMyExternalPort, inTheirIp, inTheirPort)
{
    setup();
}

PosixUdpTransport::~PosixUdpTransport() {
    if (socketFd > 0) {
        close(socketFd);
    }
}

void PosixUdpTransport::setup() {
    memset((char *) &remaddr, 0, sizeof(remaddr));
}

int PosixUdpTransport::openSocket() {
 
    // TODO: Fix this
    // ip is an ip, not a hostname, but don't know how to convert a
    // string ip to a server address format, so calling gethost - ugh
    // Should be remaddr.sin_addr.S_un.S_addr = inet_addr(theirIp)  - or something like that
    hostent* hp = gethostbyname(theirIp);
    bcopy((char *)hp->h_addr,
          (char *)&remaddr.sin_addr.s_addr,
          hp->h_length);
    remaddr.sin_family = AF_INET;
    remaddr.sin_port = htons(theirPort);
    printf("Initialized = %s:%d.\n", inet_ntoa(remaddr.sin_addr), ntohs(remaddr.sin_port));

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

int PosixUdpTransport::writeData(const char* data, int numBytes)
{
    printf("Sending message to %s:%d: %s.\n", inet_ntoa(remaddr.sin_addr), ntohs(remaddr.sin_port), data);
    int n = sendto(socketFd, data, numBytes, 0, (struct sockaddr *)&remaddr, sizeof(remaddr));
    return n;
}

int PosixUdpTransport::readData(char *buffer, int bufferLength) {
    int n = recvfrom(socketFd, buffer, bufferLength, 0, NULL, NULL);
    if (n > 0) {
        printf("Received message: %s.\n", buffer);
    }
    return n;
}


void PosixUdpTransport::testSockets(const char* myExternalIp, int myExternalPort, const char* theirExternalIp, int theirExternalPort)
{
    PosixUdpTransport t;
    Transport::testTransport(t);
}
