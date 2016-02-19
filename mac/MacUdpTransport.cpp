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
#include <netinet/in.h>
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

MacUdpTransport::~MacUdpTransport() {
    if (serverSocketFd > 0) {
        close(serverSocketFd);
    }
    
    if (socketFd > 0) {
        close(socketFd);
    }
}

int MacUdpTransport::openServerSocket() {
    printf("Opening server socket\n");
    
    serverSocketFd = socket(AF_INET, SOCK_DGRAM, 0);
    if (serverSocketFd < 0) {
        logError("ERROR opening socket");
        return ERROR;
    }
    struct sockaddr_in serv_addr;
    memset((char *) &serv_addr, 0, sizeof(serv_addr));
    serv_addr.sin_family = AF_INET;
    serv_addr.sin_addr.s_addr = htonl(INADDR_ANY);
    serv_addr.sin_port = htons(port);
    if (bind(serverSocketFd, (struct sockaddr *) &serv_addr, sizeof(serv_addr)) < 0) {
        // Assume it is because another process is listening and we should instead launch the client
        return BUSY;
    }
    
    // Since this is UDP, we need to receive a message from the client before we can send a message back,
    // so the client will send us a handshake message.
    struct sockaddr_in remaddr;
    socklen_t addrlen = sizeof(remaddr);            /* length of addresses */
    int BUFSIZE = 128;
    char buffer[BUFSIZE];
    int recvlen = recvfrom(serverSocketFd, buffer, BUFSIZE, 0, (struct sockaddr *)&remaddr, &addrlen);
    if (recvlen < 0) {
        logError("ERROR finalizing socket");
        return ERROR;
    }
    
    // TODO: Set to non-blocking receive (if we can do that in a UDP socket)
    
    return OK;
}

int MacUdpTransport::openClientSocket() {
    printf("Opening client socket\n");
    int n;
    struct sockaddr_in serv_addr;
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
    memset((char *) &serv_addr, 0, sizeof(serv_addr));
    serv_addr.sin_family = AF_INET;
    serv_addr.sin_port = htons(port);
    memcpy((void *)&serv_addr.sin_addr, server->h_addr_list[0], server->h_length);
    
    // Being UDP we need to send a sample message to the serverbefore the server can respond.
    const char* initializer = "AOKOKOK";
    if (sendto(socketFd, initializer, strlen(initializer), 0, (struct sockaddr *)&serv_addr, sizeof(serv_addr)) < 0) {
        logError("ERROR finalizing socket");
        return ERROR;
    }
    
    // Still need to set non-blocking receive
    return OK;
}

int MacUdpTransport::writeData(const char* data, int numBytes)
{
    int n = sendto(socketFd, data, numBytes, 0, (struct sockaddr *)&serv_addr, sizeof(serv_addr));
    return n;
}

int MacUdpTransport::readData(char *buffer, int bufferLength) {
    int n = recvfrom(serverSocketFd, buffer, bufferLength, 0, (struct sockaddr *)&remaddr, &addrlen);
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
                sleep(5);
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

