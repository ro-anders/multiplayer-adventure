
#include "WinUdpSocket.h"

// Socket includes
#include <fcntl.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <sys/types.h>
// End socket includes

#include "..\engine\Sys.hpp"
#include "..\engine\Transport.hpp"

WinUdpSocket::WinUdpSocket() :
	UdpSocket() {
	memset((char *)&sender, 0, sizeof(sender));

	// At construction we don't know how manyremote machines there will be, so we just make
	// space for two.
	remaddrs = new sockaddr_in[2];
	for (int ctr = 0; ctr<2; ++ctr) {
		memset((char *)&remaddrs[ctr], 0, sizeof(sender));
	}
}

WinUdpSocket::~WinUdpSocket() {
	if (socketFd != INVALID_SOCKET) {
		closesocket(socketFd);
	}
	WSACleanup();

	delete[] remaddrs;
}

/**
* Creates an OS specific socket address.
*/
sockaddr_in* WinUdpSocket::createAddress(Transport::Address address) {

	sockaddr_in* socketAddr = new sockaddr_in();

	// Zero out the memory slot before filling it.
	memset((char *)socketAddr, 0, sizeof(sockaddr_in));

	socketAddr->sin_family = AF_INET;
	socketAddr->sin_addr.S_un.S_addr = inet_addr(address.ip());
	socketAddr->sin_port = htons(address.port());
	printf("Initialized = %s:%d.\n", inet_ntoa(socketAddr->sin_addr), ntohs(socketAddr->sin_port));

	return socketAddr;
}

/**
* Delete a OS specific socket address.
*/
void WinUdpSocket::deleteAddress(sockaddr_in* socketAddress) {
	delete socketAddress;
}

/**
* Bind to the server socket.
*/
int WinUdpSocket::bind(int myInternalPort) {

	// Initialize Winsock
	WSADATA wsaData;
	int iResult = WSAStartup(MAKEWORD(2, 2), &wsaData);
	if (iResult != 0) {
		char errorMessage[2000];
		sprintf(errorMessage, "WSAStartup failed with error: %d\n", iResult);
		Sys::log(errorMessage);
		return Transport::TPT_ERROR;
	}

	// Create the server socket and bind to it
	if ((socketFd = ::socket(AF_INET, SOCK_DGRAM, 0)) == INVALID_SOCKET)
	{
		char errorMessage[2000];
		sprintf(errorMessage, "Could not create socket : %d", WSAGetLastError());
		Sys::log(errorMessage);
		return Transport::TPT_ERROR;
	}

	struct sockaddr_in serv_addr;
	memset((char *)&serv_addr, 0, sizeof(serv_addr));
	serv_addr.sin_family = AF_INET;
	serv_addr.sin_addr.s_addr = htonl(INADDR_ANY);
	serv_addr.sin_port = htons(myInternalPort);
	printf("Opening socket on port %d\n", ntohs(serv_addr.sin_port));
	if (::bind(socketFd, (struct sockaddr *) &serv_addr, sizeof(serv_addr)) == SOCKET_ERROR) {
		// Assume it is because another process is listening and we should instead launch the client
		return Transport::TPT_BUSY;
	}

	return Transport::TPT_OK;
}

void WinUdpSocket::setBlocking(bool isBlocking) {
	if (isBlocking) {
		// TODO: Don't know how to do this
	}
	else {
		u_long nbflag = 1;
		int iResult = ioctlsocket(socketFd, FIONBIO, &nbflag);
		if (iResult != 0) {
			char errorMessage[2000];
			sprintf(errorMessage, "ioctlsocket failed with error: %d\n", WSAGetLastError());
			Sys::log(errorMessage);
		}
	}
}

/**
* Send data on the socket.
* data - data to send
* numBytes - number of bytes to send (does not assume data is null terminated)
* recipient - the address to send it to
*/
int WinUdpSocket::writeData(const char* data, int numBytes, sockaddr_in* recipient)
{
	printf("Sending packet to other machine on port %d\n", ntohs(recipient->sin_port));
	int numSent = sendto(socketFd, data, numBytes, 0, (struct sockaddr *)recipient, sizeof(sockaddr_in));
	return numSent;
}

int WinUdpSocket::readData(char *buffer, int bufferLength) {
	// Receive the next packet
	int n = recvfrom(socketFd, buffer, bufferLength, 0, NULL, NULL);
	return n;
}
