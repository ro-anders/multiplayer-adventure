//
//  UdpTransport.cpp
//

#include "UdpTransport.hpp"

// Socket includes
#ifdef _WIN32
#include <winsock2.h>
#else
#include <arpa/inet.h>
#endif

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

UdpTransport::UdpTransport() :
  Transport(),
  myExternalIp("127.0.0.1"),
  myExternalPort(Transport::DEFAULT_PORT+1),
  myInternalPort(Transport::DEFAULT_PORT+1)
{
    setup();
}

UdpTransport::UdpTransport(const char* inMyExternalIp, int inMyExternalPort, const char* inIp, int inPort) :
Transport(inIp, inPort),
myExternalIp(inMyExternalIp),
myExternalPort(inMyExternalPort),
myInternalPort(inMyExternalPort)
{
    setup();
}

void UdpTransport::setup() {
    memset((char *) &remaddr, 0, sizeof(remaddr));
    printf("Uninitialized = %s:%d.\n", inet_ntoa(remaddr.sin_addr), ntohs(remaddr.sin_port));
    
    // ip is an ip, not a hostname, but don't know how to convert a
    // string ip to a server address format, so calling gethost - ugh
    hostent* hp = gethostbyname(ip);
    bcopy((char *)hp->h_addr,
          (char *)&remaddr.sin_addr.s_addr,
          hp->h_length);
    remaddr.sin_port = htons(port);
    printf("Initialized = %s:%d.\n", inet_ntoa(remaddr.sin_addr), ntohs(remaddr.sin_port));
    
}

UdpTransport::~UdpTransport() {
    if (socketFd > 0) {
        close(socketFd);
    }
}

void UdpTransport::connect() {
    if (ip == UNSPECIFIED) {
        // TODO
//        ip = "127.0.0.1";
//        int busy = openSocket();
//        if (busy == Transport::BUSY) {
//            port = DEFAULT_PORT+1;
//            --myInternalPort;
//            myExternalPort
//            openSocket();
//        }
    } else {
        openSocket();
    }

    printf("Bound to socket.  Initiating handshake.\n");

    punchHole();
}

int UdpTransport::openSocket() {
    
    socketFd = socket(AF_INET, SOCK_DGRAM, 0);
    if (socketFd < 0) {
        logger->error("ERROR opening socket");
        return ERROR;
    }
    struct sockaddr_in serv_addr;
    memset((char *) &serv_addr, 0, sizeof(serv_addr));
    serv_addr.sin_family = AF_INET;
    serv_addr.sin_addr.s_addr = htonl(INADDR_ANY);
    serv_addr.sin_port = htons(myInternalPort);
    printf("Opening socket on port %d\n", ntohs(serv_addr.sin_port));
    if (bind(socketFd, (struct sockaddr *) &serv_addr, sizeof(serv_addr)) < 0) {
        // Assume it is because another process is listening and we should instead launch the client
        return Transport::BUSY;
    }

    // Set to non-blocking receive
    fcntl(socketFd, F_SETFL, O_NONBLOCK);

    return Transport::OK;
}

void UdpTransport::punchHole() {

    // We need a big random integer.
    long randomNum = random() % 1000000;
    
    // Since this is UDP and NATs may be involved, our messages may be blocked until the other game sends
    // packets to us.  So we must enter a loop constantly sending a message and checking if we are getting their
    // messages.
    
    const char* RECVD_NOTHING = "UA";
    const char* RECVD_MESSAGE = "UB";
    const char* RECVD_ACK = "UC";
    
    char sendBuffer[16] = "";
    const int READ_BUFFER_LENGTH = 200;
    char recvBuffer[READ_BUFFER_LENGTH];
    
    const char* state = RECVD_NOTHING;
    while (state != RECVD_ACK) {
        printf("Checking for messages.\n");
        // See what messages we have received.
        int bytes = getPacket(recvBuffer, READ_BUFFER_LENGTH);
        while (bytes > 0) {
            printf("Got message: %s.\n", recvBuffer);
            if (bytes != 8) {
                logger->error("Read packet of unexpected length.");
                printf("Message=%s.  Bytes read=%d.\n", recvBuffer, bytes);
            }
            if (state == RECVD_NOTHING) {
                state = RECVD_MESSAGE;
            }
            if (recvBuffer[1] != RECVD_NOTHING[1]) {
                // They have received our message, and now we have received their's.  So ACK.
                state = RECVD_ACK;
                compareNumbers(randomNum, recvBuffer);
            }
                
            // Check for more messages.
            printf("Checking for init messages.\n");
            bytes = getPacket(recvBuffer, READ_BUFFER_LENGTH);
        }
        
        printf("Sending init message.\n");
        // Now send a packet
        sprintf(sendBuffer, "%s%ld", state, randomNum);
        sendPacket(sendBuffer);
        
        // Now wait a second.
        sleep(1);
    }
}

void UdpTransport::compareNumbers(int myRandomNumber, char* theirMessage) {
    // Whoever has the lowest number is given connect number 0.  In the one in a
    // million chance that they picked the same number, use alphabetically order of
    // the IP and port.

    long theirRandomNumber;
    // Pull out the number and figure out the connect number.
    char temp1, temp2;
    int parsed = sscanf(theirMessage, "%c%c%ld", &temp1, &temp2, &theirRandomNumber);
    if (parsed < 3) {
        logger->error("Parsed packet of unexpected length.");
        printf("Message=%s.  Number=%ld.  Parsed=%d.\n", theirMessage, theirRandomNumber, parsed);
    }
    
    if (myRandomNumber < theirRandomNumber) {
        connectNumber = 0;
    } else if (theirRandomNumber < myRandomNumber) {
        connectNumber = 1;
    } else {
        int ipCmp = strcmp(myExternalIp, ip);
        if (ipCmp < 0) {
            connectNumber = 0;
        } else if (ipCmp > 0) {
            connectNumber = 1;
        } else {
            // If IP's are equal then ports can't be equal.
            connectNumber = (myExternalPort < port ? 0 : 1);
        }
    }
    
    
}

int UdpTransport::writeData(const char* data, int numBytes)
{
    printf("%s sending message to %s:%d: %s.\n", (connectNumber ? "Server" : "Client"),
           inet_ntoa(remaddr.sin_addr), ntohs(remaddr.sin_port), data);
    int n = sendto(socketFd, data, numBytes, 0, (struct sockaddr *)&remaddr, sizeof(remaddr));
    return n;
}

int UdpTransport::readData(char *buffer, int bufferLength) {
    socklen_t addrlen = sizeof(remaddr);
    printf("%s checking message from %s:%d\n", (connectNumber ? "Server" : "Client"), inet_ntoa(remaddr.sin_addr), ntohs(remaddr.sin_port));
    int n = recvfrom(socketFd, buffer, bufferLength, 0, (struct sockaddr *)&remaddr, &addrlen);
    if (n > 0) {
        printf("%s received message: %s.\n", (connectNumber ? "Server" : "Client"), buffer);
    }
    return n;
}


void UdpTransport::testSockets(const char* myExternalIp, int myExternalPort, const char* theirExternalIp, int theirExternalPort) {
    int NUM_MESSAGES = 10;
    
    
    Transport* t = new UdpTransport(myExternalIp, myExternalPort, theirExternalIp, theirExternalPort);
    
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

int UdpTransport::openClientSocket() {
    // UDP does not have server vs client sockets.  Just peer-to-peer sockets.
    // THis method must be defined but should never be called.
    return Transport::ERROR;
}

int UdpTransport::openServerSocket() {
    // UDP does not have server vs client sockets.  Just peer-to-peer sockets.
    // THis method must be defined but should never be called.
    return Transport::ERROR;
}

