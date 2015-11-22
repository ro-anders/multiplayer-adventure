#pragma once

#ifndef WinTransport_h
#define WinTransport_h

#include <stdio.h>
#include "..\engine\Transport.hpp"

class WinTransport: public Transport {
public:
	void connect();

	int sendPacket(const char* packetData);

	int getPacket(char* buffer, int bufferLength);

protected:
	int connectNumber = 0;

};

#endif /* WinTransport_h */
