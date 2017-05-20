#include "sys.h"
#include "UdpSocket.hpp"

UdpSocket::UdpSocket() {}

UdpSocket::~UdpSocket() {}

int UdpSocket::readData(char* buffer, int bufferLength) {
    return readData(buffer, bufferLength, NULL);
}

int UdpSocket::readData(char* buffer, int bufferLength, Transport::Address& from) {
    return readData(buffer, bufferLength, &from);
}
