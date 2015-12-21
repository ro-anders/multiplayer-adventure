//
//  Transport.cpp

#include "Transport.hpp"

#include <stdlib.h>


const int Transport::ROLE_CLIENT = 1;
const int Transport::ROLE_SERVER = 0;
const int Transport::ROLE_UNSPECIFIED = -1;
const int Transport::DEFAULT_PORT = 5678;

Transport::~Transport() {}

/**
 * Parse an socket address of the form 127.0.0.1:5678.
 * Port may be omitted, in which case the outPort is not modified.
 * TODO: This does weird things with the socket address.  It modifies it and requires is not
 * be deleted.
 */
void Transport::parseUrl(char* socketAddr, char** outIp, int* outPort) {
    *outIp = socketAddr;
    // TODO: Isn't there a find() defined somewhere?
    int colonIndex = -1;
    for(int ctr=0; (colonIndex == -1) && (socketAddr[ctr] != '\0'); ++ctr) {
        if (socketAddr[ctr] == ':') {
            colonIndex = ctr;
        }
    }
    if (colonIndex > 0) {
        *outPort = atoi(socketAddr+colonIndex);
        socketAddr[colonIndex] = '\0';
    }
}

