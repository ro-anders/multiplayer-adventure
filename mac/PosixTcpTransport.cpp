//
//  PosixTcpTransport.cpp
//  MacAdventure
//

#include "PosixTcpTransport.hpp"

// Socket includes
#include <fcntl.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <unistd.h>
#include <sys/types.h>
#include <sys/socket.h>
#include <netinet/in.h>
#include <netdb.h>
// End socket includes

#include "Logger.hpp"
#include "MacSleep.hpp"

PosixTcpTransport::PosixTcpTransport() :
  TcpTransport()
{}

PosixTcpTransport::PosixTcpTransport(int inPort) :
TcpTransport(inPort)
{}

PosixTcpTransport::PosixTcpTransport(char* inIp, int inPort) :
TcpTransport(inIp, inPort)
{}

PosixTcpTransport::~PosixTcpTransport() {
    if (serverSocketFd > 0) {
        close(serverSocketFd);
    }
    
    if (socketFd > 0) {
        close(socketFd);
    }
}

int PosixTcpTransport::openServerSocket() {
    printf("Opening server socket\n");
    
    serverSocketFd = socket(AF_INET, SOCK_STREAM, 0);
    if (serverSocketFd < 0) {
        logger->error("ERROR opening socket");
        return Transport::TPT_ERROR;
    }

    struct sockaddr_in serv_addr;
    memset((char *) &serv_addr, 0, sizeof(serv_addr));
    serv_addr.sin_family = AF_INET;
    serv_addr.sin_addr.s_addr = htonl(INADDR_ANY);
    serv_addr.sin_port = htons(port);
    if (bind(serverSocketFd, (struct sockaddr *) &serv_addr,
             sizeof(serv_addr)) < 0) {
        // Assume it is because another process is listening and we should instead launch the client
        return Transport::TPT_BUSY;
    }
    
    listen(serverSocketFd,5);
    
    struct sockaddr_in cli_addr;
    socklen_t clilen = sizeof(cli_addr);
    socketFd = accept(serverSocketFd,
                       (struct sockaddr *) &cli_addr,
                       &clilen);
    if (socketFd < 0) {
        logger->error("ERROR on accept");
        return Transport::TPT_ERROR;
    }
    fcntl(socketFd, F_SETFL, O_NONBLOCK);

    return Transport::TPT_OK;

    
}

int PosixTcpTransport::openClientSocket() {
    printf("Opening client socket\n");
    int n;
    struct sockaddr_in serv_addr;
    struct hostent *server;
    
    socketFd = socket(AF_INET, SOCK_STREAM, 0);
    if (socketFd < 0) {
        logger->error("ERROR opening socket");
        return Transport::TPT_ERROR;
    }
    server = gethostbyname(ip);
    if (server == NULL) {
        logger->error("ERROR, no such host\n");
        return Transport::TPT_ERROR;
    }
    bzero((char *) &serv_addr, sizeof(serv_addr));
    serv_addr.sin_family = AF_INET;
    bcopy((char *)server->h_addr,
          (char *)&serv_addr.sin_addr.s_addr,
          server->h_length);
    serv_addr.sin_port = htons(port);
    n = ::connect(socketFd,(struct sockaddr *) &serv_addr,sizeof(serv_addr));
    if (n < 0) {
        logger->error("ERROR connecting");
        return Transport::TPT_ERROR;
    }
    fcntl(socketFd, F_SETFL, O_NONBLOCK);
    
    return Transport::TPT_OK;
}

int PosixTcpTransport::writeData(const char* data, int numBytes)
{
    int n = write(socketFd,data,numBytes);
    return n;
}

int PosixTcpTransport::readData(char *buffer, int bufferLength) {
    int n = read(socketFd, buffer, bufferLength);
    return n;
}


void PosixTcpTransport::testSockets() {
    PosixTcpTransport t;
    MacSleep sleep;
    Transport::testTransport(t, sleep);
}

