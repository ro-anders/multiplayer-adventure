#pragma once

#ifndef WinTcpTransport_h
#define WinTcpTransport_h

#include <stdio.h>
#include <winsock2.h>

#include "..\engine\TcpTransport.hpp"

class WinTcpTransport: public TcpTransport {
public:

	/**
	* Create a socket to this machine on the default port.  First try to open
	* a server socket, but if the port is already busy open up a client socket.
	* Useful for testing.
	*/
	WinTcpTransport();

	/**
	* Create a server socket.
	* port - the port to listen on.  If 0, will listen on the default port.
	*/
	WinTcpTransport(int port);

	/**
	* Connect a socket to another machine.
	* ip - the ip of the machine to connect to
	* port - the port to connect to.  If 0, will listen on the default port.
	*/
	WinTcpTransport(char* ip, int port);

	~WinTcpTransport();

	static void WinTcpTransport::testSockets();

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

#endif /* WinTcpTransport_h */
