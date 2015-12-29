

#include "WinTransport.h"

#ifndef WIN32_LEAN_AND_MEAN
#define WIN32_LEAN_AND_MEAN
#endif

#include <windows.h>
#include <ws2tcpip.h>
#include <iphlpapi.h>
#include <stdio.h>
#include <stdlib.h>

#pragma comment(lib, "Ws2_32.lib")


// Need to link with Ws2_32.lib
#pragma comment (lib, "Ws2_32.lib")
// #pragma comment (lib, "Mswsock.lib")

#define DEFAULT_BUFLEN 512
#define DEFAULT_PORT "27015"

WinTransport::WinTransport() :
	Transport(),
	ClientSocket(INVALID_SOCKET)
{
	winSetup();
}

WinTransport::WinTransport(int port) :
	Transport(port),
	ClientSocket(INVALID_SOCKET)
{
	winSetup();
}

WinTransport::WinTransport(char* ip, int port) :
	Transport(ip, port),
	ClientSocket(INVALID_SOCKET)
{
	winSetup();
}

WinTransport::~WinTransport() {
	if (ClientSocket != INVALID_SOCKET) {
		closesocket(ClientSocket);
		WSACleanup();
	}
	free(portStr);
}

void WinTransport::winSetup() {
	portStr = (char*)malloc(10 * sizeof(char));
	sprintf(portStr, "%d", port);
}


int WinTransport::writeData(const char* packetData, int numBytes) {
	int iResult = send(ClientSocket, packetData, numBytes, 0);
	// TODO: Shouldn't we call WSAGetLastError() and return the real error code?
	return iResult;
}

int WinTransport::readData(char* buffer, int bufferLength) {
	int iResult = recv(ClientSocket, buffer, bufferLength, 0);
	// TODO: Shouldn't we call WSAGetLastError() and return the real error code?
	return iResult;
}

int WinTransport::openServerSocket() {
	logError("Opening server socket\n");
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
		logError(errorMessage);
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
		logError(errorMessage);
		WSACleanup();
		return 1;
	}

	// Create a SOCKET for connecting to server
	ListenSocket = socket(result->ai_family, result->ai_socktype, result->ai_protocol);
	if (ListenSocket == INVALID_SOCKET) {
		sprintf(errorMessage, "socket failed with error: %ld\n", WSAGetLastError());
		logError(errorMessage);
		freeaddrinfo(result);
		WSACleanup();
		return 1;
	}

	// Setup the TCP listening socket
	iResult = bind(ListenSocket, result->ai_addr, (int)result->ai_addrlen);
	if (iResult == SOCKET_ERROR) {
		sprintf(errorMessage, "bind failed with error: %d\n", WSAGetLastError());
		logError(errorMessage);
		freeaddrinfo(result);
		closesocket(ListenSocket);
		WSACleanup();
		return 1;
	}

	freeaddrinfo(result);

	iResult = listen(ListenSocket, SOMAXCONN);
	if (iResult == SOCKET_ERROR) {
		sprintf(errorMessage, "listen failed with error: %d\n", WSAGetLastError());
		logError(errorMessage);
		closesocket(ListenSocket);
		WSACleanup();
		return 1;
	}

	// Accept a client socket
	ClientSocket = accept(ListenSocket, NULL, NULL);
	if (ClientSocket == INVALID_SOCKET) {
		sprintf(errorMessage, "accept failed with error: %d\n", WSAGetLastError());
		logError(errorMessage);
		closesocket(ListenSocket);
		WSACleanup();
		return 1;
	}

	// No longer need server socket
	closesocket(ListenSocket);

	sprintf(errorMessage, "Connected.  Listening on port %s\n", portStr);
	logError(errorMessage);
}

int WinTransport::openClientSocket() {
	logError("Opening client socket\n");
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
		logError(errorMessage);
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
		logError(errorMessage);
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
			logError(errorMessage);
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

	freeaddrinfo(result);

	if (ClientSocket == INVALID_SOCKET) {
		logError("Unable to connect to server!\n");
		WSACleanup();
		return 1;
	}

	sprintf(errorMessage, "Connected to %s on port %s\n", ip, portStr);
	logError(errorMessage);

}

void WinTransport::testSockets() {
	int NUM_MESSAGES = 10;
	char logMessage[1000];

	WinTransport* t = new WinTransport();
	t->connect();
	if (t->getConnectNumber() == 1) {
		for (int ctr = 0; ctr<NUM_MESSAGES; ++ctr) {
			sprintf(logMessage, "Sending message %d\n", (ctr + 1));
			t->logError(logMessage);
			char message[256];
			sprintf(message, "Message %d\n\0", (ctr + 1));
			int charsSent = t->sendPacket(message);
			if (charsSent <= 0) {
				t->logError("Error sending packet");
			}
			if (ctr == (NUM_MESSAGES / 2)) {
				t->logError("Pausing 5 seconds\n");
				Sleep(5000);
			}
		}
	}
	else {
		// We wait a second for the sender to send some stuff
		t->logError("Pausing 2 seconds\n");
		Sleep(2000);
		for (int ctr = 0; ctr<NUM_MESSAGES; ++ctr) {
			char buffer[256];
			int charsReceived = t->getPacket(buffer, 256);
			if (charsReceived < 0) {
				t->logError("Error receiving packet");
			}
			else if (charsReceived == 0) {
				sprintf(logMessage, "Receive %d got no data.\n", (ctr + 1));
				t->logError(logMessage);
			}
			else {
				sprintf(logMessage, "Receive %d got: %s\n",(ctr+1), buffer);
				t->logError(logMessage);
			}
		}
	}
	// Pause
	t->logError("Exiting");
	delete t;
	exit(0);

}

void WinTransport::logError(const char* message) {
	char buffer[1000];
	sprintf(buffer, "%d: %s", (connectNumber + 1), message);
	OutputDebugString(buffer);
	//perror(message);
}