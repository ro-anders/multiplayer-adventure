//
//  WinUdpTransport.cpp
//

#include "WinUdpTransport.h"

// Socket includes
#include <fcntl.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <sys/types.h>
// End socket includes

#include "..\engine\Sys.hpp"

WinUdpTransport::WinUdpTransport() :
  UdpTransport(),
  socket(INVALID_SOCKET)
{
    setup();
}

WinUdpTransport::WinUdpTransport(const Address& inMyExternalAddr, const Address& inTheirAddr) :
UdpTransport(inMyExternalAddr, inTheirAddr),
socket(INVALID_SOCKET)
{
    setup();
}

WinUdpTransport::WinUdpTransport(const Address& inMyExternalAddr, int transportNum,
	const Address& otherAddr1, const Address& otherAddr2) :
UdpTransport(inMyExternalAddr, transportNum, otherAddr1, otherAddr2),
socket(INVALID_SOCKET)
{
	setup();
}
WinUdpTransport::~WinUdpTransport() {
	if (socket != INVALID_SOCKET) {
		closesocket(socket);
	}
	WSACleanup();
}

void WinUdpTransport::setup() {
	remaddrs = new sockaddr_in[numOtherMachines];
	for (int ctr = 0; ctr<numOtherMachines; ++ctr) {
		memset((char *)&remaddrs[ctr], 0, sizeof(sender));
	}
	memset((char *)&sender, 0, sizeof(sender));
}

int WinUdpTransport::openSocket() {
	char errorMessage[2000];

	for (int ctr = 0; ctr < numOtherMachines; ++ctr) {
		remaddrs[ctr].sin_family = AF_INET;
		remaddrs[ctr].sin_addr.S_un.S_addr = inet_addr(theirAddrs[ctr].ip());
		remaddrs[ctr].sin_port = htons(theirAddrs[ctr].port());
		printf("Initialized = %s:%d.\n", inet_ntoa(remaddrs[ctr].sin_addr), ntohs(remaddrs[ctr].sin_port));
	}

	// Initialize Winsock
	WSADATA wsaData;
	int iResult = WSAStartup(MAKEWORD(2, 2), &wsaData);
	if (iResult != 0) {
		sprintf(errorMessage, "WSAStartup failed with error: %d\n", iResult);
		Sys::log(errorMessage);
		return Transport::TPT_ERROR;
	}

	if ((socket = ::socket(AF_INET, SOCK_DGRAM, 0)) == INVALID_SOCKET)
	{
		sprintf(errorMessage, "Could not create socket : %d", WSAGetLastError());
		Sys::log(errorMessage);
		return Transport::TPT_ERROR;
	}

    struct sockaddr_in serv_addr;
    memset((char *) &serv_addr, 0, sizeof(serv_addr));
    serv_addr.sin_family = AF_INET;
    serv_addr.sin_addr.s_addr = htonl(INADDR_ANY);
    serv_addr.sin_port = htons(myInternalPort);
    printf("Opening socket on port %d\n", ntohs(serv_addr.sin_port));
    if (bind(socket, (struct sockaddr *) &serv_addr, sizeof(serv_addr)) == SOCKET_ERROR) {
        // Assume it is because another process is listening and we should instead launch the client
        return Transport::TPT_BUSY;
    }

	// Set the socket to non-blocking
	u_long nbflag = 1;
	iResult = ioctlsocket(socket, FIONBIO, &nbflag);
	if (iResult != 0) {
		sprintf(errorMessage, "ioctlsocket failed with error: %d\n", WSAGetLastError());
		Sys::log(errorMessage);
		WSACleanup();
		return Transport::TPT_ERROR;
	}
	
	return Transport::TPT_OK;
}

int WinUdpTransport::writeData(const char* data, int numBytes, int recipient)
{
	int numSent = 0; // If sending to multiple machines, we just return what one send reported.
	for (int ctr = 0; ctr < numOtherMachines; ++ctr) {
		if ((recipient < 0) || (ctr == recipient)) {
			numSent = sendto(socket, data, numBytes, 0, (struct sockaddr *)&remaddrs[ctr], sizeof(remaddrs[ctr]));
		}
	}
    return numSent;
}

int WinUdpTransport::readData(char *buffer, int bufferLength) {
    int n = recvfrom(socket, buffer, bufferLength, 0, NULL, NULL);
    return n;
}


void WinUdpTransport::testSockets(const char* myExternalIp, int myExternalPort, const char* theirExternalIp, int theirExternalPort)
{
    WinUdpTransport t;
    Transport::testTransport(t);
}
