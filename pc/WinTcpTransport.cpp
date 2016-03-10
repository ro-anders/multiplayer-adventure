

#include "WinTcpTransport.h"

#ifndef WIN32_LEAN_AND_MEAN
#define WIN32_LEAN_AND_MEAN
#endif

#include <windows.h>
#include <ws2tcpip.h>
#include <iphlpapi.h>
#include <stdio.h>
#include <stdlib.h>
#include "..\engine\Sys.hpp"

#pragma comment(lib, "Ws2_32.lib")


// Need to link with Ws2_32.lib
#pragma comment (lib, "Ws2_32.lib")
// #pragma comment (lib, "Mswsock.lib")

#define DEFAULT_BUFLEN 512
#define DEFAULT_PORT "27015"

WinTcpTransport::WinTcpTransport() :
	TcpTransport(),
	ClientSocket(INVALID_SOCKET)
{
	winSetup();
}

WinTcpTransport::WinTcpTransport(int port) :
	TcpTransport(port),
	ClientSocket(INVALID_SOCKET)
{
	winSetup();
}

WinTcpTransport::WinTcpTransport(char* ip, int port) :
	TcpTransport(ip, port),
	ClientSocket(INVALID_SOCKET)
{
	winSetup();
}

WinTcpTransport::~WinTcpTransport() {
	if (ClientSocket != INVALID_SOCKET) {
		closesocket(ClientSocket);
		WSACleanup();
	}
	delete[] portStr;
}

void WinTcpTransport::winSetup() {
	portStr = new char[10];
	sprintf(portStr, "%d", port);
}


int WinTcpTransport::writeData(const char* packetData, int numBytes) {
	int iResult = send(ClientSocket, packetData, numBytes, 0);
	// TODO: Shouldn't we call WSAGetLastError() and return the real error code?
	return iResult;
}

int WinTcpTransport::readData(char* buffer, int bufferLength) {
	int iResult = recv(ClientSocket, buffer, bufferLength, 0);
	// TODO: Shouldn't we call WSAGetLastError() and return the real error code?
	return iResult;
}

int WinTcpTransport::openServerSocket() {
	Sys::log("Opening server socket\n");
	WSADATA wsaData;
	int iResult;
	char errorMessage[2000];

	SOCKET ListenSocket = INVALID_SOCKET;
	ClientSocket = INVALID_SOCKET;

	struct addrinfo *result = NULL;
	struct addrinfo hints;

	int iSendResult;
	char recvbuf[DEFAULT_BUFLEN];
	int recvbuflen = DEFAULT_BUFLEN;

	// Initialize Winsock
	iResult = WSAStartup(MAKEWORD(2, 2), &wsaData);
	if (iResult != 0) {
		sprintf(errorMessage, "WSAStartup failed with error: %d\n", iResult);
		Sys::log(errorMessage);
		return 1;
	}

	ZeroMemory(&hints, sizeof(hints));
	hints.ai_family = AF_INET;
	hints.ai_socktype = SOCK_STREAM;
	hints.ai_protocol = IPPROTO_TCP;
	hints.ai_flags = AI_PASSIVE;

	// Resolve the server address and port
	iResult = getaddrinfo(NULL, portStr, &hints, &result);
	if (iResult != 0) {
		sprintf(errorMessage, "getaddrinfo failed with error: %d\n", iResult);
		Sys::log(errorMessage);
		WSACleanup();
		return 1;
	}

	// Create a SOCKET for connecting to server
	ListenSocket = socket(result->ai_family, result->ai_socktype, result->ai_protocol);
	if (ListenSocket == INVALID_SOCKET) {
		sprintf(errorMessage, "socket failed with error: %ld\n", WSAGetLastError());
		Sys::log(errorMessage);
		freeaddrinfo(result);
		WSACleanup();
		return 1;
	}

	// Setup the TCP listening socket
	iResult = bind(ListenSocket, result->ai_addr, (int)result->ai_addrlen);
	if (iResult == SOCKET_ERROR) {
		sprintf(errorMessage, "bind failed with error: %d\n", WSAGetLastError());
		Sys::log(errorMessage);
		freeaddrinfo(result);
		closesocket(ListenSocket);
		WSACleanup();
		return 1;
	}

	freeaddrinfo(result);

	iResult = listen(ListenSocket, SOMAXCONN);
	if (iResult == SOCKET_ERROR) {
		sprintf(errorMessage, "listen failed with error: %d\n", WSAGetLastError());
		Sys::log(errorMessage);
		closesocket(ListenSocket);
		WSACleanup();
		return 1;
	}

	// Accept a client socket
	ClientSocket = accept(ListenSocket, NULL, NULL);
	if (ClientSocket == INVALID_SOCKET) {
		sprintf(errorMessage, "accept failed with error: %d\n", WSAGetLastError());
		Sys::log(errorMessage);
		closesocket(ListenSocket);
		WSACleanup();
		return 1;
	}

	// Set the socket to non-blocking
	u_long nbflag = 1;
	iResult = ioctlsocket(ClientSocket, FIONBIO, &nbflag);
	if (iResult != 0) {
		sprintf(errorMessage, "ioctlsocket failed with error: %d\n", WSAGetLastError());
		Sys::log(errorMessage);
		WSACleanup();
		return 1;
	}



	// No longer need server socket
	closesocket(ListenSocket);

	sprintf(errorMessage, "Connected.  Listening on port %s\n", portStr);
	Sys::log(errorMessage);
}

int WinTcpTransport::openClientSocket() {
	Sys::log("Opening client socket\n");
	WSADATA wsaData;
	ClientSocket = INVALID_SOCKET;
	
	struct addrinfo *result = NULL,
		*ptr = NULL,
		hints;
	char *sendbuf = "this is a test";
	char recvbuf[DEFAULT_BUFLEN];
	int iResult;
	int recvbuflen = DEFAULT_BUFLEN;
	char errorMessage[2000];

	// Initialize Winsock
	iResult = WSAStartup(MAKEWORD(2, 2), &wsaData);
	if (iResult != 0) {
		sprintf(errorMessage, "WSAStartup failed with error: %d\n", iResult);
		Sys::log(errorMessage);
		return 1;
	}

	ZeroMemory(&hints, sizeof(hints));
	hints.ai_family = AF_UNSPEC;
	hints.ai_socktype = SOCK_STREAM;
	hints.ai_protocol = IPPROTO_TCP;

	// Resolve the server address and port
	iResult = getaddrinfo(ip, portStr, &hints, &result);
	if (iResult != 0) {
		sprintf(errorMessage, "getaddrinfo failed with error: %d\n", iResult);
		Sys::log(errorMessage);
		WSACleanup();
		return 1;
	}

	// Attempt to connect to an address until one succeeds
	for (ptr = result; ptr != NULL; ptr = ptr->ai_next) {

		// Create a SOCKET for connecting to server
		ClientSocket = socket(ptr->ai_family, ptr->ai_socktype,
			ptr->ai_protocol);
		if (ClientSocket == INVALID_SOCKET) {
			sprintf(errorMessage, "socket failed with error: %ld\n", WSAGetLastError());
			Sys::log(errorMessage);
			WSACleanup();
			return 1;
		}

		// Connect to server.
		iResult = ::connect(ClientSocket, ptr->ai_addr, (int)ptr->ai_addrlen);
		if (iResult == SOCKET_ERROR) {
			closesocket(ClientSocket);
			ClientSocket = INVALID_SOCKET;
			continue;
		}
		break;
	}

	// Set the socket to non-blocking
	u_long nbflag = 1;
	iResult = ioctlsocket(ClientSocket, FIONBIO, &nbflag);
	if (iResult != 0) {
		sprintf(errorMessage, "ioctlsocket failed with error: %d\n", WSAGetLastError());
		Sys::log(errorMessage);
		WSACleanup();
		return 1;
	}

	freeaddrinfo(result);

	if (ClientSocket == INVALID_SOCKET) {
		Sys::log("Unable to connect to server!\n");
		WSACleanup();
		return 1;
	}

	sprintf(errorMessage, "Connected to %s on port %s\n", ip, portStr);
	Sys::log(errorMessage);

}

void WinTcpTransport::testSockets() {
	WinTcpTransport t;
	Transport::testTransport(t);
}
