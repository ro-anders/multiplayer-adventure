#include <string.h>
#include "sys.h"
#include "Logger.hpp"
#include "UdpSocket.hpp"

UdpSocket::UdpSocket() {}

UdpSocket::~UdpSocket() {}

int UdpSocket::readData(char* buffer, int bufferLength) {
    // return readData(buffer, bufferLength, NULL);
    // VERBOSE LOGGING
    static Transport::Address tmpAddress;
    int charsRead = readData(buffer, bufferLength, &tmpAddress);
    static char message[2000];
    if (charsRead > 0) {
        ::strncpy(message, buffer, charsRead);
        message[charsRead] = '\0';
        Logger::log() << "Read \"" << message << "\" from " << tmpAddress.ip() << ":" << tmpAddress.port() << Logger::EOM;
    }
    return charsRead;
}

int UdpSocket::readData(char* buffer, int bufferLength, Transport::Address& from) {
    // return readData(buffer, bufferLength, &from);
    // VERBOSE LOGGING
    int charsRead =  readData(buffer, bufferLength, &from);
    static char message[2000];
    if (charsRead > 0) {
        ::strncpy(message, buffer, charsRead);
        message[charsRead] = '\0';
        Logger::log() << "Read \"" << message << "\" from " << from.ip() << ":" << from.port() << Logger::EOM;
    }
    return charsRead;
}
