//
//  TcpTransport.cpp

#include "TcpTransport.hpp"

#include <stdlib.h>
#include <string.h>

TcpTransport::TcpTransport() :
Transport(true),
port(DEFAULT_PORT),
ip(LOCALHOST_IP)
{}

TcpTransport::TcpTransport(int inPort) :
Transport(false),
port(inPort == 0 ? DEFAULT_PORT : inPort),
ip(NULL)
{}

TcpTransport::TcpTransport(const char* inIp, int inPort) :
Transport(false),
port(inPort == 0 ? DEFAULT_PORT : inPort),
ip(inIp)
{}



TcpTransport::~TcpTransport()
{
}

void TcpTransport::connect() {
    if (getTestSetupNumber() == NOT_YET_DETERMINED) {
        // Try to bind to a port.  If it's busy, assume the other program has bound and try to connect to it.
        int busy = openServerSocket();
        if (busy == TPT_BUSY) {
            openClientSocket();
        }
        setTestSetupNumber(busy ? 1 : 0);
    } else if (ip == NULL) {
        openServerSocket();
    } else {
        openClientSocket();
    }
    connected = true;
}


