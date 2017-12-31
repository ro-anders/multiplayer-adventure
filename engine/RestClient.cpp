#include "sys.h"
#include "RestClient.hpp"

#include <string.h>

const char* RestClient::DEFAULT_BROKER_SERVER = "roserver.ddns.net";

const int RestClient::STUN_PORT = 8888;
const int RestClient::REST_PORT = 8080;

RestClient::RestClient() :
brokerAddress(Transport::Address(DEFAULT_BROKER_SERVER, REST_PORT))
{}

void RestClient::setAddress(const Transport::Address& address) {
    brokerAddress = address;
}

int RestClient::get(const char* path, char* responseBuffer, int bufferLength) {
    char message[2000];
    sprintf(message, "GET %s HTTP/1.1\r\n"
            "Host: %s:%d\r\n"
            "Accept: */*\r\n"
            "\r\n", path, brokerAddress.ip(), brokerAddress.port());
    
    int charsInResponse = request(path, message, responseBuffer, bufferLength);
    
    return charsInResponse;
}

int RestClient::post(const char* path, const char* content, char* responseBuffer, int bufferLength) {
    char message[2000];
    int contentLength = strlen(content);
    sprintf(message, "POST %s HTTP/1.1\r\n"
            "Host: %s:%d\r\n"
            "Accept: application/json\r\n"
            "Content-Type: application/json\r\n"
            "Content-Length: %d\r\n"
            "\r\n"
            "%s", path, brokerAddress.ip(), brokerAddress.port(), contentLength, content);
    
    int charsInResponse = request(path, message, responseBuffer, bufferLength);
    
    return charsInResponse;
}


int RestClient::stripOffHeaders(char* buffer, int charsInBuffer) {
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


