//
//  JsTransport.cpp
//  MacAdventure
//

#include "JsTransport.hpp"


JsTransport::JsTransport() :
  Transport()
{}

JsTransport::JsTransport(int inPort) :
Transport(inPort)
{}

JsTransport::JsTransport(char* inIp, int inPort) :
Transport(inIp, inPort)
{}

JsTransport::~JsTransport() {
}

int JsTransport::sendPacket(const char* packetData) {
	return 0;
}

int JsTransport::getPacket(char* buffer, int bufferLength) {
	return 0;
}



int JsTransport::openServerSocket() {
    return 0;
}

int JsTransport::openClientSocket() {
    return 0;
}

int JsTransport::writeData(const char* data, int numBytes)
{
    return numBytes;
}

int JsTransport::readData(char *buffer, int bufferLength) {
    return 0;
}


void JsTransport::logError(const char *msg)
{
}


