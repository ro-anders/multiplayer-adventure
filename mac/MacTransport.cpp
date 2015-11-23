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
    PACKET_DELIMETER('\0')
{
    streamBufferSize = 1024;
    streamBuffer = new char[streamBufferSize]; // TODO: Make this more dynamic.
    charsInStreamBuffer = 0;
}

MacTransport::~MacTransport() {
    // TODO: Call super-class's destructor
    delete [] streamBuffer;
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
    connectNumber = (busy ? 1 : 0);
}

int MacTransport::sendPacket(const char* packetData) {
    char delimStr[1];
    delimStr[0] = PACKET_DELIMETER;
    int n = write(socketFd,packetData,strlen(packetData));
    if (n < 0) {
        error("ERROR writing to socket");
    } else {
        int n2 = write(socketFd, delimStr, 1);
        if (n2 < 0) {
            error("ERROR writing to socket");
        } else {
            printf("Sent \"%s\"\n", packetData);
        }
    }
    return n;
}

int MacTransport::getPacket(char* buffer, int bufferLength) {
    int hitError = 0;
    int ranOutOfData = 0;
    int delimeterIndex = -1;
    int startOfNewData = 0;
    while ((delimeterIndex < 0) && !hitError && !ranOutOfData) {
        
        // Search through the new data for a delimeter.
        for(int ctr=startOfNewData; (delimeterIndex < 0) && (ctr<charsInStreamBuffer); ++ctr) {
            delimeterIndex = (streamBuffer[ctr] == PACKET_DELIMETER ? ctr : -1);
        }
        
        // Detect if we've run out of buffer.
        if ((delimeterIndex < 0) && (charsInStreamBuffer >= streamBufferSize)) {
            printf("ERROR reading from socket.  Packet too big for buffer.  Truncating.");
            streamBuffer[streamBufferSize-1] = '\0';
            delimeterIndex = streamBufferSize-1;
        }

        if (delimeterIndex < 0) {
            // If we don't have delimeter, pull more data off the socket
            startOfNewData = charsInStreamBuffer;
            int charsToRead = streamBufferSize-charsInStreamBuffer;
            int n = read(socketFd, streamBuffer+charsInStreamBuffer, charsToRead);
            if (n == -1) {
                // Socket has no more data.  Return no data even if we have some sitting in the stream buffer.
                ranOutOfData = 1;
            }
            else if (n < 0) {
                hitError = n;
            } else {
                charsInStreamBuffer += n;
            }
        }
    }
    
    int charsInPacket = 0;
    if (hitError) {
        error("ERROR reading from socket");
    } else if (ranOutOfData) {
        charsInPacket = 0;
        buffer[0] = '\0';
    } else {
        // Copy the data into the passed in buffer.
        charsInPacket = delimeterIndex; // We don't copy the delimeter
        if (delimeterIndex >= bufferLength) {
            printf("ERROR reading from socket.  Packet too big for buffer.  Truncating.");
            charsInPacket = bufferLength-1;
        }
        memcpy(buffer, streamBuffer, charsInPacket * sizeof(char));
        buffer[charsInPacket] = '\0';

        // Remove the characters from the stream buffer
        memmove(streamBuffer, streamBuffer+delimeterIndex+1, (charsInStreamBuffer-delimeterIndex-1)*sizeof(char));
        charsInStreamBuffer = charsInStreamBuffer-delimeterIndex-1;
        printf("Received message: \"%s\"\n",buffer);
    }
    
    return (hitError ? hitError : charsInPacket);
}

int MacTransport::openServerSocket(int portno) {
    printf("Opening server socket\n");
    
    socklen_t clilen;
    struct sockaddr_in serv_addr, cli_addr;
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
    fcntl(socketFd, F_SETFL, O_NONBLOCK);

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
    n = ::connect(socketFd,(struct sockaddr *) &serv_addr,sizeof(serv_addr));
    if (n < 0) {
        error("ERROR connecting");
    }
    fcntl(socketFd, F_SETFL, O_NONBLOCK);
}


void MacTransport::error(const char *msg)
{
    perror(msg);
    exit(1);
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

