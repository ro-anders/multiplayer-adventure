//
//  MacUdpTransport.cpp
//  MacAdventure
//

#include "MacUdpTransport.hpp"

// Socket includes
#include <fcntl.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <unistd.h>
#include <sys/types.h>
#include <sys/socket.h>
#include <netdb.h>
// End socket includes


MacUdpTransport::MacUdpTransport() :
  Transport()
{}

MacUdpTransport::MacUdpTransport(int inPort) :
Transport(inPort)
{}

MacUdpTransport::MacUdpTransport(char* inIp, int inPort) :
Transport(inIp, inPort)
{}

void MacUdpTransport::setup() {
    memset((char *) &remaddr, 0, sizeof(remaddr));
}

MacUdpTransport::~MacUdpTransport() {
    if (socketFd > 0) {
        close(socketFd);
    }
}

int MacUdpTransport::openServerSocket() {
    printf("Opening server socket\n");
    
    socketFd = socket(AF_INET, SOCK_DGRAM, 0);
    if (socketFd < 0) {
        logError("ERROR opening socket");
        return ERROR;
    }
    struct sockaddr_in serv_addr;
    memset((char *) &serv_addr, 0, sizeof(serv_addr));
    serv_addr.sin_family = AF_INET;
    serv_addr.sin_addr.s_addr = htonl(INADDR_ANY);
    serv_addr.sin_port = htons(port);
    if (bind(socketFd, (struct sockaddr *) &serv_addr, sizeof(serv_addr)) < 0) {
        // Assume it is because another process is listening and we should instead launch the client
        return BUSY;
    }
    
    printf("Server opened socket.  Waiting for init message.\n");
    
    // Since this is UDP, we need to receive a message from the client before we can send a message back,
    // so the client will send us a handshake message.
    struct sockaddr_in remaddr;
    socklen_t addrlen = sizeof(remaddr);            /* length of addresses */
    int BUFSIZE = 128;
    char buffer[BUFSIZE];
    int recvlen = recvfrom(socketFd, buffer, BUFSIZE, 0, (struct sockaddr *)&remaddr, &addrlen);
    if (recvlen < 0) {
        logError("ERROR finalizing socket");
        return ERROR;
    }
    
    buffer[recvlen] = '\0';
    printf("Received init message: %s.\n", buffer);
    

    // Set to non-blocking receive
    fcntl(socketFd, F_SETFL, O_NONBLOCK);
    
    return OK;
}

int MacUdpTransport::openClientSocket() {
    printf("Opening client socket\n");
    struct hostent *server;
    const char* serverAddress = (ip == UNSPECIFIED ? "localhost" : ip);
    
    socketFd = socket(AF_INET, SOCK_DGRAM, 0);
    if (socketFd < 0) {
        logError("ERROR opening socket");
        return ERROR;
    }
    server = gethostbyname(serverAddress);
    if (server == NULL) {
        logError("ERROR, no such host\n");
        return ERROR;
    }
    socklen_t addrlen = sizeof(remaddr);
    memset((char *) &remaddr, 0, addrlen);
    remaddr.sin_family = AF_INET;
    remaddr.sin_port = htons(port);
    memcpy((void *)&remaddr.sin_addr, server->h_addr_list[0], server->h_length);
    
    printf("Client opened socket.  Sending init message.\n");
    
    // Being UDP we need to send a sample message to the server before the server can respond.
    const char* initializer = "AOKOKOK";
    if (sendto(socketFd, initializer, strlen(initializer), 0, (struct sockaddr *)&remaddr, addrlen) < 0) {
        logError("ERROR finalizing socket");
        return ERROR;
    }
    
    printf("Sent init  message.\n");
    
    // Set to non-blocking receive
    fcntl(socketFd, F_SETFL, O_NONBLOCK);

    return OK;
}

int MacUdpTransport::writeData(const char* data, int numBytes)
{
    printf("%s sending message: %s.\n", (connectNumber ? "Server" : "Client"), data);
    int n = sendto(socketFd, data, numBytes, 0, (struct sockaddr *)&remaddr, sizeof(remaddr));
    return n;
}

int MacUdpTransport::readData(char *buffer, int bufferLength) {
    socklen_t addrlen = sizeof(remaddr);
    int n = recvfrom(socketFd, buffer, bufferLength, 0, (struct sockaddr *)&remaddr, &addrlen);
    if (n > 0) {
        printf("%s received message: %s.\n", (connectNumber ? "Server" : "Client"), buffer);
    }
    return n;
}


void MacUdpTransport::logError(const char *msg)
{
    perror(msg);
}

void MacUdpTransport::testSockets() {
    int NUM_MESSAGES = 10;
    Transport* t = new MacUdpTransport();
    t->connect();
    if (t->getConnectNumber() == 1) {
        for(int ctr=0; ctr<NUM_MESSAGES; ++ctr) {
            char message[256];
            sprintf(message, "Message %d\n\0", (ctr+1));
            int charsSent = t->sendPacket(message);
            if (charsSent <= 0) {
                perror("Error sending packet");
            }
            if (ctr == (NUM_MESSAGES/2)) {
                printf("Pausing\n");
                sleep(30);
            }
        }
    } else {
        // We wait a second for the sender to send some stuff
        sleep(2);
        for(int ctr=0;ctr<NUM_MESSAGES;++ctr) {
            char buffer[256];
            int charsReceived = t->getPacket(buffer, 256);
            if (charsReceived < 0) {
                perror("Error receiving packet");
            } else if (charsReceived == 0) {
                printf("Received no data.\n");
            }
            if (ctr == (NUM_MESSAGES/2)) {
                printf("Client is pausing, but we should not.  Should receive no data after this.\n");
            }
        }
    }
    // Pause
    printf("Hit Return to exit");
    char tmpBuffer[256];
    fgets(tmpBuffer,255,stdin);
    printf("Exiting");
    delete t;
    exit(0);
    
}

