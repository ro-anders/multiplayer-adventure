

#include "JSTransport.hpp"

JSTransport::JSTransport() {
}

JSTransport::~JSTransport() {
}

void JSTransport::connect() {
}

int JSTransport::sendPacket(const char* packetData) {
    return -1;
}

int JSTransport::getPacket(char* buffer, int bufferLength) {
    return 0;
}

int JSTransport::openServerSocket() {return -1;}
int JSTransport::openClientSocket() {return -1;}
int JSTransport::writeData(const char* data, int numBytes) {return -1;}
int JSTransport::readData(char* buffer, int bufferLength) {return -1;}

