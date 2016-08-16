
#include "RestClient.hpp"

#include <string.h>

const char* RestClient::BROKER_SERVER = "localhost";

const int RestClient::STUN_PORT = 8888;
const int RestClient::REST_PORT = 9000;

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


int RestClient::get(const char* path, char* responseBuffer, int bufferLength) {
    char message[2000];
    sprintf(message, "GET %s HTTP/1.1\r\n"
            "Host: %s:%d\r\n"
            "Accept: */*\r\n"
            "\r\n", path, BROKER_SERVER, REST_PORT);
    
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
            "%s", path, BROKER_SERVER, REST_PORT, contentLength, content);
    
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


