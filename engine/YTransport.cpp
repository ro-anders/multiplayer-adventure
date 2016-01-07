

#include "YTransport.hpp"

YTransport::YTransport(Transport* xport1, Transport* xport2) {
    numXports = 2;
    xports = new Transport*[numXports];    xports[0] = xport1;
    xports[1] = xport2;
    lastQueried = 0;
}

YTransport::~YTransport() {
    for(int ctr=0; ctr<numXports; ++ctr) {
        delete xports[ctr];
    }
    delete[] xports;
}

void YTransport::connect() {
    connectNumber = 0;
    for(int ctr=0; ctr<numXports; ++ctr) {
        xports[ctr]->connect();
        connectNumber += xports[ctr]->getConnectNumber(); // This doesn't really work for more than three players total.
    }
}

int YTransport::sendPacket(const char* packetData) {
    int bytesSent = 0;
    for(int ctr=0; ctr<numXports; ++ctr) {
        bytesSent = xports[ctr]->sendPacket(packetData);
    }
    return bytesSent;
}

int YTransport::getPacket(char* buffer, int bufferLength) {
    int bytesRead = 0;
    bool gotMessage = false;
    for(int ctr=0; !gotMessage && (ctr< numXports); ++ctr) {
        int nextIndex = (ctr + lastQueried + 1) % numXports;
        bytesRead = xports[nextIndex]->getPacket(buffer, bufferLength);
        if (bytesRead > 0) {
            gotMessage = true;
            lastQueried = nextIndex;
        }
    }
    return bytesRead;
}

// None of these methods are used, but to conform to Transport they have to be defined.
int YTransport::openServerSocket() {return -1;}
int YTransport::openClientSocket() {return -1;}
int YTransport::writeData(const char* data, int numBytes) {return -1;}
int YTransport::readData(char* buffer, int bufferLength) {return -1;}

