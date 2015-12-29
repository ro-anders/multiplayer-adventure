//
//  MacTransport.cpp
//  MacAdventure
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


MacTransport::MacTransport() :
  Transport()
{}

MacTransport::MacTransport(int inPort) :
Transport(inPort)
{}

MacTransport::MacTransport(char* inIp, int inPort) :
Transport(inIp, inPort)
{}

MacTransport::~MacTransport() {
    if (serverSocketFd > 0) {
        close(serverSocketFd);
    }
    
    if (socketFd > 0) {
        close(socketFd);
    }
}

int MacTransport::sendPacket(const char* packetData) {
    char delimStr[1];
    delimStr[0] = PACKET_DELIMETER;
    int n = write(socketFd,packetData,strlen(packetData));
    if (n < 0) {
        logError("ERROR writing to socket");
    } else {
        int n2 = write(socketFd, delimStr, 1);
        if (n2 < 0) {
            logError("ERROR writing to socket");
        } else {
            printf("Sent \"%s\"\n", packetData);
        }
    }
    return n;
}

int MacTransport::openServerSocket() {
    printf("Opening server socket\n");
    
    socklen_t clilen;
    struct sockaddr_in serv_addr, cli_addr;
    serverSocketFd = socket(AF_INET, SOCK_STREAM, 0);
    if (serverSocketFd < 0)
        logError("ERROR opening socket");
    bzero((char *) &serv_addr, sizeof(serv_addr));
    serv_addr.sin_family = AF_INET;
    serv_addr.sin_addr.s_addr = INADDR_ANY;
    serv_addr.sin_port = htons(port);
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
        logError("ERROR on accept");
    fcntl(socketFd, F_SETFL, O_NONBLOCK);

    return 0;

    
}

int MacTransport::openClientSocket() {
    printf("Opening client socket\n");
    int n;
    struct sockaddr_in serv_addr;
    struct hostent *server;
    const char* serverAddress = (ip == UNSPECIFIED ? "localhost" : ip);
    
    socketFd = socket(AF_INET, SOCK_STREAM, 0);
    if (socketFd < 0)
        logError("ERROR opening socket");
    server = gethostbyname(serverAddress);
    if (server == NULL) {
        fprintf(stderr,"ERROR, no such host\n");
        exit(0);
    }
    bzero((char *) &serv_addr, sizeof(serv_addr));
    serv_addr.sin_family = AF_INET;
    bcopy((char *)server->h_addr,
          (char *)&serv_addr.sin_addr.s_addr,
          server->h_length);
    serv_addr.sin_port = htons(port);
    n = ::connect(socketFd,(struct sockaddr *) &serv_addr,sizeof(serv_addr));
    if (n < 0) {
        logError("ERROR connecting");
    }
    fcntl(socketFd, F_SETFL, O_NONBLOCK);
    
    return 0;
}

int MacTransport::sendData(const char* data)
{
    // TODO: Implement
    return -1;
}

int MacTransport::readData(char *buffer, int bufferLength) {
    int n = read(socketFd, buffer, bufferLength);
    return n;
}


void MacTransport::logError(const char *msg)
{
    perror(msg);
}

void MacTransport::testSockets() {
    int NUM_MESSAGES = 10;
    Transport* t = new MacTransport();
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

