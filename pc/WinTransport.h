#pragma once

#ifndef WinTransport_h
#define WinTransport_h

#include <stdio.h>
#include <winsock2.h>

#include "..\engine\Transport.hpp"

class WinTransport: public Transport {
public:

	/**
	* Create a socket to this machine on the default port.  First try to open
	* a server socket, but if the port is already busy open up a client socket.
	* Useful for testing.
	*/
	WinTransport();

	/**
	* Create a server socket.
	* port - the port to listen on.  If 0, will listen on the default port.
	*/
	WinTransport(int port);

	/**
	* Connect a socket to another machine.
	* ip - the ip of the machine to connect to
	* port - the port to connect to.  If 0, will listen on the default port.
	*/
	WinTransport(char* ip, int port);

	~WinTransport();

	/**
	* Report an error - different OS's have different behavior
	*/
	void logError(const char* msg);

	static void WinTransport::testSockets();

protected:

	/**
     * Open a server socket.
     */
	int openServerSocket();

    /**
     * Open a client socket.
     */
	int openClientSocket();

    /**
     * Send data on the socket.
     */
    int writeData(const char* data, int numBytes);
    
    /**
     * Pull data off the socket - non-blocking
     */
    int readData(char* buffer, int bufferLength);


private:

	char* portStr;

	SOCKET ClientSocket = INVALID_SOCKET;

	/**
	* Setup transport.
	* Code common to all three constructors.
	*/
	void winSetup();

};

#endif /* WinTransport_h */
