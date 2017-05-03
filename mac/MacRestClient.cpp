
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

#include "Logger.hpp"
#include "Sys.hpp"

int MacRestClient::request(const char* path, const char* message, char* responseBuffer, int bufferLength) {
    
    // Open the HTTP connnection to the server
    
    Logger::log("Opening http socket\n");
    int n;
    struct sockaddr_in serv_addr;
    struct hostent *server;
    
    int socketFd = socket(AF_INET, SOCK_STREAM, 0);
    if (socketFd < 0) {
        Logger::logError("ERROR opening http socket");
        return -1;
    }
    server = gethostbyname(BROKER_SERVER);
    if (server == NULL) {
        Logger::logError("ERROR, no such http host\n");
        return -2;
    }
    bzero((char *) &serv_addr, sizeof(serv_addr));
    serv_addr.sin_family = AF_INET;
    bcopy((char *)server->h_addr,
          (char *)&serv_addr.sin_addr.s_addr,
          server->h_length);
    serv_addr.sin_port = htons(REST_PORT);
    n = ::connect(socketFd,(struct sockaddr *) &serv_addr,sizeof(serv_addr));
    if (n < 0) {
        Logger::logError("ERROR making http connecting");
        return -3;
    }
    
    Logger::log() << "Sending http request:\n" << message << Logger::EOM;

    // Send the HTTP request
    int messageLen = strlen(message);
    n = write(socketFd, message, messageLen);
    if (n < 0) {
        Logger::logError("ERROR sending http request");
        return -4;
    } else if (n<messageLen) {
        // TODO: Is this a fatal error?
        Logger::logError("ERROR http request only partially sent");
    }
    
    Logger::log("Reading http response\n");

    // Read the HTTP response
    int charsInBuffer = 0;
    int charsRead = 0;
    bool keepGoing = true;
    while (keepGoing) {
        charsRead = read( socketFd, responseBuffer+charsInBuffer, bufferLength-charsInBuffer);
        // TODO: Sites like Google return negative number when you've read all the data off the socket, but Play just returns 0.
        // Figure this out.
        if (charsRead <= 0) {
            keepGoing = false;
            if (charsInBuffer == 0) {
                Logger::logError("Error reading http response");
                return -5;
            }
        } else {
            charsInBuffer += charsRead;
            if (charsInBuffer >= bufferLength) {
                Logger::logError("ERROR http response too big.  Truncated.");
                Logger::logError(responseBuffer);
                keepGoing = false;
            }
        }
        // After the first read we don't block
        fcntl(socketFd, F_SETFL, O_NONBLOCK);
    }
    // Null terminate the response
    responseBuffer[charsInBuffer] = '\0';
    
    std::cout << "Response = \"" << responseBuffer << "\"" << std::endl;
    
    // Cleanup resources
    close(socketFd);
    
    charsInBuffer = stripOffHeaders(responseBuffer, charsInBuffer);

    std::cout << "Response Body = \"" << responseBuffer << "\"" << std::endl;

    return charsInBuffer;
}

void MacRestClient::mimicServer() {
    int port = 9000;
    Logger::log("Opening server socket\n");
    
    int serverSocketFd = socket(AF_INET, SOCK_STREAM, 0);
    if (serverSocketFd < 0) {
        Logger::logError("ERROR opening socket");
        return;
    }
    
    struct sockaddr_in serv_addr;
    memset((char *) &serv_addr, 0, sizeof(serv_addr));
    serv_addr.sin_family = AF_INET;
    serv_addr.sin_addr.s_addr = htonl(INADDR_ANY);
    serv_addr.sin_port = htons(port);
    if (bind(serverSocketFd, (struct sockaddr *) &serv_addr,
             sizeof(serv_addr)) < 0) {
        Logger::logError("ERROR port already in use");
        return;
    }
    
    listen(serverSocketFd,5);
    
    struct sockaddr_in cli_addr;
    socklen_t clilen = sizeof(cli_addr);
    int socketFd = accept(serverSocketFd,
                      (struct sockaddr *) &cli_addr,
                      &clilen);
    if (socketFd < 0) {
        Logger::logError("ERROR on accept");
        return;
    }

    int BUFFER_LENGTH = 1000;
    char buffer[BUFFER_LENGTH];
    bool done = false;
    std::cout << '"';
    while (!done) {
        int n = read(socketFd, buffer, BUFFER_LENGTH);
        for(int ctr=0; ctr<n; ++ctr) {
            std::cout << buffer[ctr];
        }
    }
    std::cout << '"' << std::endl;

}

void MacRestClient::test() {
    MacRestClient client;
    int BUFFER_LENGTH = 10000;
    char buffer[BUFFER_LENGTH];
    
    int responseSize = client.get("/task", buffer, BUFFER_LENGTH);
    if (responseSize < 0) {
        std::cout << "Error reading response.  code=" << responseSize << ", errno=" << errno << std::endl;
    } else {
        std::cout << buffer << std::endl;
    }
}
