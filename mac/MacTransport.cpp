//
//  MacTransport.cpp
//  MacAdventure
//
//  Created by Robert Antonucci on 11/10/15.
//
//

#include "MacTransport.hpp"

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


MacTransport::MacTransport() {
    
}

MacTransport::~MacTransport() {
    if (serverSocketFd > 0) {
        close(serverSocketFd);
    }
    
    if (socketFd > 0) {
        close(socketFd);
    }
}

void MacTransport::connect() {
    // Try to bind to a port.  If it's busy, assume the other program has bound and try to connect to it.
    int port = 5678;
    int busy = openServerSocket(port);
    if (busy) {
        openClientSocket(port);
    }
    connectNumber = (busy ? 2 : 1);
}

int MacTransport::sendPacket(const char* packetData) {
    int n = write(socketFd,packetData,strlen(packetData));
    if (n < 0)
        error("ERROR writing to socket");

}

int MacTransport::getPacket(char* buffer, int bufferLength) {
    bzero(buffer,bufferLength);
    int n = read(socketFd, buffer, bufferLength-1);
    if (n < 0) error("ERROR reading from socket");
    printf("Here is the message: %s\n",buffer);

}

int MacTransport::openServerSocket(int portno) {
    printf("Opening server socket\n");
    
    socklen_t clilen;
    char buffer[256];
    struct sockaddr_in serv_addr, cli_addr;
    int n;
    serverSocketFd = socket(AF_INET, SOCK_STREAM, 0);
    if (serverSocketFd < 0)
        error("ERROR opening socket");
    bzero((char *) &serv_addr, sizeof(serv_addr));
    serv_addr.sin_family = AF_INET;
    serv_addr.sin_addr.s_addr = INADDR_ANY;
    serv_addr.sin_port = htons(portno);
    if (bind(serverSocketFd, (struct sockaddr *) &serv_addr,
             sizeof(serv_addr)) < 0) {
        // Assume it is because another process is listening and we should instead launch the client
        return 1;
    }
    listen(serverSocketFd,5);
    clilen = sizeof(cli_addr);
    socketFd = accept(serverSocketFd,
                       (struct sockaddr *) &cli_addr,
                       &clilen);
    if (socketFd < 0)
        error("ERROR on accept");
    //    fcntl(socketFd, F_SETFL, O_NONBLOCK);

    return 0;

    
}

void MacTransport::openClientSocket(int portno) {
    printf("Opening client socket\n");
    int n;
    struct sockaddr_in serv_addr;
    struct hostent *server;
    
    char buffer[256];
    socketFd = socket(AF_INET, SOCK_STREAM, 0);
    if (socketFd < 0)
        error("ERROR opening socket");
    //    fcntl(socketFd, F_SETFL, O_NONBLOCK);
    server = gethostbyname("localhost");
    if (server == NULL) {
        fprintf(stderr,"ERROR, no such host\n");
        exit(0);
    }
    bzero((char *) &serv_addr, sizeof(serv_addr));
    serv_addr.sin_family = AF_INET;
    bcopy((char *)server->h_addr,
          (char *)&serv_addr.sin_addr.s_addr,
          server->h_length);
    serv_addr.sin_port = htons(portno);
    if (::connect(socketFd,(struct sockaddr *) &serv_addr,sizeof(serv_addr)) < 0)
        error("ERROR connecting");
}


void MacTransport::error(const char *msg)
{
    perror(msg);
    exit(1);
}

