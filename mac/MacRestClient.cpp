
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

int MacRestClient::get(const char* path, char* responseBuffer, int bufferLength) {
    // Open the HTTP connnection to the server
    const char* serverName = "localhost";
    int port = 8080;
    
    char message[2000];
    sprintf(message, "GET %s HTTP/1.1\r\n"
            "Host: %s:%d\r\n"
            "Accept: */*\r\n"
            "\r\n", path, serverName, port);
    
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
    
    printf("Sending http request:\n%s\n", message);
    
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
    
    printf("Reading http response\n");
    
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
                Sys::log("Error reading http response");
                return -5;
            }
        } else {
            charsInBuffer += charsRead;
            if (charsInBuffer >= bufferLength) {
                Sys::log("ERROR http response too big.  Truncated.");
                Sys::log(responseBuffer);
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

int MacRestClient::post(const char* path, const char* content, char* responseBuffer, int bufferLength) {
    
    // Open the HTTP connnection to the server
    const char* serverName = "localhost";
    int port = 9000;
    
    char message[2000];
    int contentLength = strlen(content);
    sprintf(message, "POST %s HTTP/1.1\r\n"
        "Host: %s:%d\r\n"
        "Accept: application/json\r\n"
        "Content-Type: application/json\r\n"
        "Content-Length: %d\r\n"
        "\r\n"
        "%s", path, serverName, port, contentLength, content);
    
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
    
    printf("Sending http request:\n%s\n", message);

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
    
    printf("Reading http response\n");

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
                Sys::log("Error reading http response");
                return -5;
            }
        } else {
            charsInBuffer += charsRead;
            if (charsInBuffer >= bufferLength) {
                Sys::log("ERROR http response too big.  Truncated.");
                Sys::log(responseBuffer);
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

int MacRestClient::stripOffHeaders(char* buffer, int charsInBuffer) {
    const char* DELIMETER = "\r\n\r\n";
    int DELIMETER_LENGTH = 4;
    char* found = strstr(buffer, DELIMETER);
    if ((found == NULL) || (found+DELIMETER_LENGTH+1 >= buffer + charsInBuffer)) {
        // No body.
        buffer[0] = '\0';
        return 0;
    } else {
        int charsInBody = charsInBuffer-(found-buffer)-DELIMETER_LENGTH;
        strcpy(buffer, found+DELIMETER_LENGTH);
        return charsInBody;
    }
}

void MacRestClient::mimicServer() {
    int port = 9000;
    printf("Opening server socket\n");
    
    int serverSocketFd = socket(AF_INET, SOCK_STREAM, 0);
    if (serverSocketFd < 0) {
        Sys::log("ERROR opening socket");
        return;
    }
    
    struct sockaddr_in serv_addr;
    memset((char *) &serv_addr, 0, sizeof(serv_addr));
    serv_addr.sin_family = AF_INET;
    serv_addr.sin_addr.s_addr = htonl(INADDR_ANY);
    serv_addr.sin_port = htons(port);
    if (bind(serverSocketFd, (struct sockaddr *) &serv_addr,
             sizeof(serv_addr)) < 0) {
        Sys::log("ERROR port already in use");
        return;
    }
    
    listen(serverSocketFd,5);
    
    struct sockaddr_in cli_addr;
    socklen_t clilen = sizeof(cli_addr);
    int socketFd = accept(serverSocketFd,
                      (struct sockaddr *) &cli_addr,
                      &clilen);
    if (socketFd < 0) {
        Sys::log("ERROR on accept");
        return;
    }

    int BUFFER_LENGTH = 1000;
    char buffer[BUFFER_LENGTH];
    bool done = false;
    std::cout << '"';
    while (!done) {
        int n = read(socketFd, buffer, BUFFER_LENGTH);
        for(int ctr=0; ctr<n; ++ctr) {
            if (true) {
                std::cout << buffer[ctr];
            } else {
                if (buffer[ctr] == ' ') {
                    std::cout << '-';
                }
                else if (((buffer[ctr]<'a') || (buffer[ctr]>'z')) &&
                         ((buffer[ctr]<'A') || (buffer[ctr]>'Z'))&&
                         ((buffer[ctr]<'0') || (buffer[ctr]>'9'))) {
                    std::cout << '<' << (int)buffer[ctr] << '>';
                } else {
                    std::cout << buffer[ctr];
                }
            }
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