#pragma once

#ifndef WinTransport_h
#define WinTransport_h

#include <stdio.h>
#include <winsock2.h>

#include "..\engine\Transport.hpp"

class WinTransport: public Transport {
public:

	WinTransport();

	~WinTransport();

	void connect();

	int sendPacket(const char* packetData);

	int getPacket(char* buffer, int bufferLength);

	static void WinTransport::testSockets();

private:

	SOCKET ClientSocket = INVALID_SOCKET;

	int openServerSocket(const char* portStr);

	int openClientSocket(const char* ip, const char* portStr);

	/**
	 * Send a message to the console
	 */
	void msg(const char* msg);

};

#endif /* WinTransport_h */
