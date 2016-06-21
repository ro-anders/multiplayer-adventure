
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

StringMap::StringMap() :
numEntries(0),
spaceAllocated(16) {
    keys = new const char*[spaceAllocated];
    values = new const char*[spaceAllocated];
}

StringMap::~StringMap() {
    for(int ctr=0; ctr<numEntries; ++ctr) {
        delete[] keys[ctr];
        delete[] values[ctr];
    }
    delete[] keys;
    delete[] values;
}

const char* StringMap::get(const char* key) {
    int foundAt = findIndex(key);
    const char* found = (foundAt < 0 ? NULL : values[foundAt]);
    return found;
}

void StringMap::put(const char* key, const char* value) {
    int insertInSlot = findIndex(key);
    if (insertInSlot > 0) {
        delete[] values[insertInSlot];
        values[insertInSlot] = copyString(value);
    } else {
        if (spaceAllocated == numEntries) {
            allocateMoreSpace();
        }
        keys[numEntries] = copyString(key);
        values[numEntries] = copyString(value);
    }
}

void StringMap::remove(const char* key) {
    int insertInSlot = findIndex(key);
    if (insertInSlot > 0) {
        delete[] keys[insertInSlot];
        delete[] values[insertInSlot];
        // Shift everything down
        memcpy(keys+insertInSlot, keys+insertInSlot+1, numEntries-insertInSlot-1);
    }
}

int StringMap::findIndex(const char* key) {
    int found = -1;
    for(int ctr=0; (ctr<numEntries) && (found < 0); ++ctr) {
        if (strcmp(key, keys[ctr])==0) {
            found = ctr;
        }
    }
    return found;
}


int MacRestClient::makeCall(char* buffer, int bufferLength) {
    
    // Open the HTTP connnection to the server
    const char* serverName = "localhost";
    int port = 9000;
    // GET from Google
    //const char* message = "GET / HTTP/1.1\r\nHost: www.google.com\r\nConnection: close\r\n\r\n";
    // Get list of games
    //const char* message = "GET /games HTTP/1.1\r\nHost: localhost\r\nConnection: close\r\n\r\n";
    // Make a game request
    const char* message =
        "POST /game HTTP/1.1\r\n"
        "Host: localhost:9000\r\n"
        "User-Agent: curl/7.43.0\r\n"
        "Accept: */*\r\n"
        "Content-Length: 20\r\n"
        "Content-Type: application/x-www-form-urlencoded\r\n"
        "\r\n"
        "ip=2.2.2.2&port=1111";
    
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
    
    printf("Sending http request\n");

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
        // After the first read we don't block
        fcntl(socketFd, F_SETFL, O_NONBLOCK);
    }
    // Null terminate the response
    buffer[charsInBuffer] = '\0';
    
    std::cout << "Response = \"" << buffer << "\"" << std::endl;
    
    // Cleanup resources
    close(socketFd);

    return charsInBuffer;
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
    
    int responseSize = client.makeCall(buffer, BUFFER_LENGTH);
    if (responseSize < 0) {
        std::cout << "Error reading response.  code=" << responseSize << ", errno=" << errno << std::endl;
    } else {
        std::cout << buffer << std::endl;
    }
}