#pragma once


#ifndef WinUdpSocket_hpp
#define WinUdpSocket_hpp

#include <stdio.h>
#include <winsock2.h>
#include "..\engine\UdpSocket.hpp"

class WinUdpSocket : public UdpSocket {

public:


	WinUdpSocket();

	~WinUdpSocket();


	/**
	* Creates an OS specific socket address.
	*/
	sockaddr_in* createAddress(Transport::Address source, bool dnsLookup=false);

	/**
	* Delete a OS specific socket address.
	*/
	void deleteAddress(sockaddr_in* socketAddress);

	/**
	* Bind to the server socket.
	*/
	int bind(int port);

	/**
	* Whether or not this socket should block.
	*/
	void setBlocking(bool shouldBlock);

	/**
	* If blocking, how long to listen before aborting.  Negative number means wait forever.
	*/
	void setTimeout(int seconds);

	/**
	* Send data on the socket.
	* data - data to send
	* numBytes - number of bytes to send (does not assume data is null terminated)
	* recipient - the address to send it to
	*/
	int writeData(const char* data, int numBytes, sockaddr_in* recipient);

	int readData(char* buffer, int bufferLength, Transport::Address* source);

	/**
	* Return a list of all IP4 addresses that this machine is using.
	*/
    List<Transport::Address> getLocalIps();


private:

	int socketFd; // UDP Socket to send and receive

	struct sockaddr_in* remaddrs; // Destinations we are sending to

	struct sockaddr_in  sender; // The sender of a message we just received

};


#endif /* WinUdpSocket_hpp */
