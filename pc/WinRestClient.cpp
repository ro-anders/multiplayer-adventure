
#include "WinRestClient.h"

#ifndef WIN32_LEAN_AND_MEAN
#define WIN32_LEAN_AND_MEAN
#endif

// Socket includes
#include <windows.h>
#include <ws2tcpip.h>
#include <iphlpapi.h>
// End socket includes

#include <iostream>
#include <stdio.h>
#include <stdlib.h>
#include "..\engine\Sys.hpp"

// Need to link with Ws2_32.lib
#pragma comment (lib, "Ws2_32.lib")

int WinRestClient::request(const char* path, const char* message, char* responseBuffer, int bufferLength) {

	char portStr[10];
	sprintf(portStr, "%d", REST_PORT);

	// Open the HTTP connnection to the server
	Sys::log("Opening client socket\n");
	WSADATA wsaData;
	SOCKET ClientSocket = INVALID_SOCKET;

	struct addrinfo *result = NULL,
		*ptr = NULL,
		hints;
	int iResult;
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
	/*
	server = gethostbyname(BROKER_SERVER);
	if (server == NULL) {
		Sys::log("ERROR, no such http host\n");
		return -2;
	}
	*/
	// Resolve the server address and port
	iResult = getaddrinfo(BROKER_SERVER, portStr, &hints, &result);
	if (iResult != 0) {
		sprintf(errorMessage, "getaddrinfo failed with error: %d\n", iResult);
		Sys::log(errorMessage);
		WSACleanup();
		return -2;
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
			return -3;
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
		Sys::log("Unable to connect to server!\n");
		WSACleanup();
		return -3;
	}

	sprintf(errorMessage, "Sending http request:\n%s\n", message);
	Sys::log(errorMessage);

	// Send the HTTP request
	int messageLen = strlen(message);
	iResult = send(ClientSocket, message, messageLen, 0);
	if (iResult < 0) {
		Sys::log("ERROR sending http request");
		return -4;
	}
	else if (iResult<messageLen) {
		// TODO: Is this a fatal error?
		Sys::log("ERROR http request only partially sent");
	}

	Sys::log("Reading http response\n");

	// Read the HTTP response
	int charsInBuffer = 0;
	int charsRead = 0;
	bool keepGoing = true;
	while (keepGoing) {
		charsRead = recv(ClientSocket, responseBuffer, bufferLength, 0);
		// TODO: Sites like Google return negative number when you've read all the data off the socket, but Play just returns 0.
		// Figure this out.
		if (charsRead <= 0) {
			keepGoing = false;
			if (charsInBuffer == 0) {
				Sys::log("Error reading http response");
				return -5;
			}
		}
		else {
			charsInBuffer += charsRead;
			if (charsInBuffer >= bufferLength) {
				Sys::log("ERROR http response too big.  Truncated.");
				Sys::log(responseBuffer);
				keepGoing = false;
			}
		}
		// After the first read we don't block
		u_long nbflag = 1;
		iResult = ioctlsocket(ClientSocket, FIONBIO, &nbflag);
		if (iResult != 0) {
			sprintf(errorMessage, "ioctlsocket failed with error: %d\n", WSAGetLastError());
			Sys::log(errorMessage);
			WSACleanup();
			return -6;
		}
	}
	// Null terminate the response
	responseBuffer[charsInBuffer] = '\0';

	std::cout << "Response = \"" << responseBuffer << "\"" << std::endl;

	// Cleanup resources
	closesocket(ClientSocket);
	WSACleanup();

	charsInBuffer = stripOffHeaders(responseBuffer, charsInBuffer);

	std::cout << "Response Body = \"" << responseBuffer << "\"" << std::endl;

	return charsInBuffer;
}
