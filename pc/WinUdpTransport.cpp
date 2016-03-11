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

WinUdpTransport::WinUdpTransport(const char* inMyExternalIp, int inMyExternalPort,
                                     const char* inTheirIp, int inTheirPort) :
UdpTransport(inMyExternalIp, inMyExternalPort, inTheirIp, inTheirPort),
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
    memset((char *) &remaddr, 0, sizeof(remaddr));
    printf("Uninitialized = %s:%d.\n", inet_ntoa(remaddr.sin_addr), ntohs(remaddr.sin_port));
}

int WinUdpTransport::openSocket() {
	char errorMessage[2000];

	remaddr.sin_family = AF_INET;
	remaddr.sin_addr.S_un.S_addr = inet_addr(theirIp);
	remaddr.sin_port = htons(theirPort);
    printf("Initialized = %s:%d.\n", inet_ntoa(remaddr.sin_addr), ntohs(remaddr.sin_port));

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

int WinUdpTransport::writeData(const char* data, int numBytes)
{
    printf("Sending message to %s:%d: %s.\n", inet_ntoa(remaddr.sin_addr), ntohs(remaddr.sin_port), data);
    int n = sendto(socket, data, numBytes, 0, (struct sockaddr *)&remaddr, sizeof(remaddr));
    return n;
}

int WinUdpTransport::readData(char *buffer, int bufferLength) {
    printf("Checking message from %s:%d\n", inet_ntoa(remaddr.sin_addr), ntohs(remaddr.sin_port));
    int n = recvfrom(socket, buffer, bufferLength, 0, NULL, NULL);
    if (n > 0) {
        printf("Received message: %s.\n", buffer);
    }
    return n;
}


void WinUdpTransport::testSockets(const char* myExternalIp, int myExternalPort, const char* theirExternalIp, int theirExternalPort)
{
    WinUdpTransport t;
    Transport::testTransport(t);
}
