
#define  _WINSOCK_DEPRECATED_NO_WARNINGS

#include "WinUdpSocket.h"

// Socket includes
#include <fcntl.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <sys/types.h>

#include <iphlpapi.h>
#include <ws2tcpip.h>
#pragma comment(lib, "ws2_32.lib")
#pragma comment(lib, "iphlpapi.lib")


// End socket includes

#include "..\engine\Logger.hpp"
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
sockaddr_in* WinUdpSocket::createAddress(Transport::Address address, bool dnsLookup) {

	sockaddr_in* socketAddr = new sockaddr_in();

	// Zero out the memory slot before filling it.
	memset((char *)socketAddr, 0, sizeof(sockaddr_in));

	socketAddr->sin_family = AF_INET;
	if (dnsLookup) {
		// TODOX: DNS Lookup
		socketAddr->sin_addr.S_un.S_addr = inet_addr(address.ip());
	}
	else {
		socketAddr->sin_addr.S_un.S_addr = inet_addr(address.ip());
	}
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
		Logger::logError() << "WSAStartup failed with error: " << iResult << Logger::EOM;
		return Transport::TPT_ERROR;
	}

	// Create the server socket and bind to it
	if ((socketFd = ::socket(AF_INET, SOCK_DGRAM, 0)) == INVALID_SOCKET)
	{
		Logger::logError() << "Could not create socket: " << WSAGetLastError() << Logger::EOM;
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
			Logger::logError() << "ioctlsocket failed with error: " << WSAGetLastError() << Logger::EOM;
		}
	}
}

void WinUdpSocket::setTimeout(int seconds) {
	// TODOX: Implement
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

//int PosixUdpSocket::readData(char *buffer, int bufferLength) {
int WinUdpSocket::readData(char *buffer, int bufferLength, Transport::Address* source) {
	int n;

	// Receive the next packet
	if (source == NULL) {
		n = recvfrom(socketFd, buffer, bufferLength, 0, NULL, NULL);
	}
	else {
		struct sockaddr_in source_addr;
		int source_addr_len = sizeof(source_addr);
		memset((char *)&source_addr, 0, sizeof(source_addr));
		n = recvfrom(socketFd, buffer, bufferLength, 0, (struct sockaddr*)&source_addr, &source_addr_len);
		const char* ip = inet_ntoa(source_addr.sin_addr);
		*source = Transport::Address(ip, ntohs(source_addr.sin_port));
	}

	return n;
}

/**
* Return a list of all IP4 addresses that this machine is using.
*/
List<Transport::Address> WinUdpSocket::getLocalIps() {
	WSAData d;
	WSAStartup(MAKEWORD(2, 2), &d);

	DWORD rv, size;
	PIP_ADAPTER_ADDRESSES adapter_addresses, aa;
	PIP_ADAPTER_UNICAST_ADDRESS ua;

	rv = GetAdaptersAddresses(AF_UNSPEC, GAA_FLAG_INCLUDE_PREFIX, NULL, NULL, &size);
	if (rv != ERROR_BUFFER_OVERFLOW) {
		Logger::logError("Failed to deduce local ip: GetAdaptersAddresses() failed.");
		return List<Transport::Address>();
	}
	adapter_addresses = (PIP_ADAPTER_ADDRESSES)malloc(size);

	rv = GetAdaptersAddresses(AF_UNSPEC, GAA_FLAG_INCLUDE_PREFIX, NULL, adapter_addresses, &size);
	if (rv != ERROR_SUCCESS) {
		Logger::logError("Failed to deduce local ip: GetAdaptersAddresses() failed on second call.");
		free(adapter_addresses);
		return List<Transport::Address>();
	}

	List<Transport::Address> addrs;
	for (aa = adapter_addresses; aa != NULL; aa = aa->Next) {
		for (ua = aa->FirstUnicastAddress; ua != NULL; ua = ua->Next) {
			char buf[BUFSIZ];
			// TODO: Only use IPv4
			//int family = ua->Address.lpSockaddr->sa_family;
			//printf("\t%s ", family == AF_INET ? "IPv4" : "IPv6");
			memset(buf, 0, BUFSIZ);
			getnameinfo(ua->Address.lpSockaddr, ua->Address.iSockaddrLength, buf, sizeof(buf), NULL, 0, NI_NUMERICHOST);
			if (strlen(buf) > 0) {
				addrs.add(Transport::Address(buf, 0));
			}
		}
	}

	free(adapter_addresses);

	return addrs;
}
