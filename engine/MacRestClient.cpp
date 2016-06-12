
#include "MacRestClient.hpp"

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
#include <iostream>

#include "Sys.hpp"

int MacRestClient::makeCall(char* buffer, int bufferLength) {
    
    // Open the HTTP connnection to the server
    const char* serverName = "localhost";
    int port = 9000;
    //const char* message = "GET / HTTP/1.1\r\nHost: www.google.com\r\nConnection: close\r\n\r\n";
    const char* message = "GET /games HTTP/1.1\r\nHost: localhost\r\nConnection: close\r\n\r\n";

    printf("Opening http socket\n");
    int n;
    struct sockaddr_in serv_addr;
    struct hostent *server;
    
    int socketFd = socket(AF_INET, SOCK_STREAM, 0);
    if (socketFd < 0) {
        Sys::log("ERROR opening http socket");
        return -1;
    }
    server = gethostbyname(serverName);
    if (server == NULL) {
        Sys::log("ERROR, no such http host\n");
        return -2;
    }
    bzero((char *) &serv_addr, sizeof(serv_addr));
    serv_addr.sin_family = AF_INET;
    bcopy((char *)server->h_addr,
          (char *)&serv_addr.sin_addr.s_addr,
          server->h_length);
    serv_addr.sin_port = htons(port);
    n = ::connect(socketFd,(struct sockaddr *) &serv_addr,sizeof(serv_addr));
    if (n < 0) {
        Sys::log("ERROR making http connecting");
        return -3;
    }
    
    // Send the HTTP request
    int messageLen = strlen(message);
    n = write(socketFd, message, messageLen);
    if (n < 0) {
        Sys::log("ERROR sending http request");
        return -4;
    } else if (n<messageLen) {
        // TODO: Is this a fatal error?
        Sys::log("ERROR http request only partially sent");
    }

    // Read the HTTP response
    int charsInBuffer = 0;
    int charsRead = 0;
    bool keepGoing = true;
    while (keepGoing) {
        charsRead = read( socketFd, buffer+charsInBuffer, bufferLength-charsInBuffer);
        // TODO: Sites like Google return negative number when you've read all the data off the socket, but Play just returns 0.
        // Figure this out.
        if (charsRead <= 0) {
            keepGoing = false;
            if (charsInBuffer == 0) {
                Sys::log("Error reading http response");
                return -5;
            }
        } else {
            charsInBuffer += charsRead;
            if (charsInBuffer >= bufferLength) {
                Sys::log("ERROR http response too big.  Truncated.");
                Sys::log(buffer);
                keepGoing = false;
            }
        }
    }
    // Null terminate the response
    buffer[charsInBuffer] = '\0';
    
    // Cleanup resources
    close(socketFd);

    return charsInBuffer;
}

void MacRestClient::test() {
    MacRestClient client;
    int BUFFER_LENGTH = 10000;
    char buffer[BUFFER_LENGTH];
    
    int responseSize = client.makeCall(buffer, BUFFER_LENGTH);
    if (responseSize < 0) {
        std::cout << "Error reading response.  code=" << responseSize << ", errno=" << errno << std::endl;
    } else {
        std::cout << buffer << std::endl;
    }
}