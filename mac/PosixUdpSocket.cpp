

#include "PosixUdpSocket.hpp"

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
#include "Transport.hpp"

PosixUdpSocket::PosixUdpSocket() :
UdpSocket() {
    memset((char *) &sender, 0, sizeof(sender));
    
    // At construction we don't know how manyremote machines there will be, so we just make
    // space for two.
    remaddrs = new sockaddr_in[2];
    for(int ctr=0; ctr<2; ++ctr) {
        memset((char *) &remaddrs[ctr], 0, sizeof(sender));
    }
}

PosixUdpSocket::~PosixUdpSocket() {
    if (socketFd > 0) {
        close(socketFd);
    }
    delete[] remaddrs;
}

/**
 * Creates an OS specific socket address.
 */
sockaddr_in* PosixUdpSocket::createAddress(Transport::Address address, bool dnsLookup) {
    
    sockaddr_in* socketAddr = new sockaddr_in();
    
    // Zero out the memory slot before filling it.
    memset((char *) socketAddr, 0, sizeof(sockaddr_in));
    
    socketAddr->sin_family = AF_INET;
    if (dnsLookup) {
        hostent* hp = gethostbyname(address.ip());
        bcopy((char *)hp->h_addr,
              (char *)&socketAddr->sin_addr.s_addr,
              hp->h_length);
    } else {
        socketAddr->sin_addr.s_addr = inet_addr(address.ip());
    }
    socketAddr->sin_port = htons(address.port());
    return socketAddr;
}

/**
 * Delete a OS specific socket address.
 */
void PosixUdpSocket::deleteAddress(sockaddr_in* socketAddress) {
    delete socketAddress;
}

/**
 * Bind to the server socket.
 */
int PosixUdpSocket::bind(int myInternalPort) {
    // Create the server socket and bind to it
    socketFd = socket(AF_INET, SOCK_DGRAM, 0);
    if (socketFd < 0) {
        Logger::logError("ERROR opening socket");
        return Transport::TPT_ERROR;
    }
    struct sockaddr_in serv_addr;
    memset((char *) &serv_addr, 0, sizeof(serv_addr));
    serv_addr.sin_family = AF_INET;
    serv_addr.sin_addr.s_addr = htonl(INADDR_ANY);
    serv_addr.sin_port = htons(myInternalPort);
    Logger::log() << "Opening socket on port " << ntohs(serv_addr.sin_port) << Logger::EOM;
    if (::bind(socketFd, (struct sockaddr *) &serv_addr, sizeof(serv_addr)) < 0) {
        // Assume it is because another process is listening and we should instead launch the client
        return Transport::TPT_BUSY;
    }
    
    return Transport::TPT_OK;
}

void PosixUdpSocket::setBlocking(bool isBlocking) {
    if (isBlocking) {
        // TODO: Don't know how to do this
    } else {
        fcntl(socketFd, F_SETFL, O_NONBLOCK);
    }
}

/**
 * Send data on the socket.
 * data - data to send
 * numBytes - number of bytes to send (does not assume data is null terminated)
 * recipient - the address to send it to
 */
int PosixUdpSocket::writeData(const char* data, int numBytes, sockaddr_in* recipient)
{
    int numSent = sendto(socketFd, data, numBytes, 0, (struct sockaddr *)recipient, sizeof(sockaddr_in));
    return numSent;
}

int PosixUdpSocket::readData(char *buffer, int bufferLength) {
    // Receive the next packet
    int n = recvfrom(socketFd, buffer, bufferLength, 0, NULL, NULL);
    return n;
}