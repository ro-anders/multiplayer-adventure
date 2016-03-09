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

#include "Logger.hpp"
#include "MacSleep.hpp"


PosixUdpTransport::PosixUdpTransport(Sleep* sleep) :
  UdpTransport(sleep)
{
    setup();
}

PosixUdpTransport::PosixUdpTransport(const char* inMyExternalIp, int inMyExternalPort,
                                     const char* inTheirIp, int inTheirPort, Sleep* sleep) :
UdpTransport(inMyExternalIp, inMyExternalPort, inTheirIp, inTheirPort, sleep)
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
    printf("Uninitialized = %s:%d.\n", inet_ntoa(remaddr.sin_addr), ntohs(remaddr.sin_port));
    
    // ip is an ip, not a hostname, but don't know how to convert a
    // string ip to a server address format, so calling gethost - ugh
    hostent* hp = gethostbyname(theirIp);
    bcopy((char *)hp->h_addr,
          (char *)&remaddr.sin_addr.s_addr,
          hp->h_length);
    remaddr.sin_port = htons(theirPort);
    printf("Initialized = %s:%d.\n", inet_ntoa(remaddr.sin_addr), ntohs(remaddr.sin_port));
    
}

int PosixUdpTransport::openSocket() {
    
    socketFd = socket(AF_INET, SOCK_DGRAM, 0);
    if (socketFd < 0) {
        logger->error("ERROR opening socket");
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
    socklen_t addrlen = sizeof(remaddr);
    printf("Checking message from %s:%d\n", inet_ntoa(remaddr.sin_addr), ntohs(remaddr.sin_port));
    int n = recvfrom(socketFd, buffer, bufferLength, 0, (struct sockaddr *)&remaddr, &addrlen);
    if (n > 0) {
        printf("Received message: %s.\n", buffer);
    }
    return n;
}


void PosixUdpTransport::testSockets(const char* myExternalIp, int myExternalPort, const char* theirExternalIp, int theirExternalPort)
{
    MacSleep* sleep = new MacSleep();
    PosixUdpTransport t(sleep);
    Transport::testTransport(t, *sleep);
}
