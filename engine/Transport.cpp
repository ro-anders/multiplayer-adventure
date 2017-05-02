//
//  Transport.cpp

#include "Transport.hpp"

#include <stdlib.h>
#include <string.h>
#include "Logger.hpp"
#include "Sys.hpp"

Transport::Address::Address() :
  _ip(copyIp("")),
  _port(0) {}

Transport::Address::Address(const char* inIp, int inPort) :
  _ip(copyIp(inIp)),
  _port(inPort) {}

Transport::Address::Address(const Address& other) :
  _ip(copyIp(other._ip)),
  _port(other._port) {}

Transport::Address::~Address() {
    delete[] _ip;
}

Transport::Address& Transport::Address::operator=(const Transport::Address &other) {
    char* tmp = copyIp(other._ip);
    delete[] _ip;
    _ip = tmp;
    _port = other._port;
    return *this;
}

bool Transport::Address::operator==(const Transport::Address &other) {
    return ((_port == other._port) && (strcmp(_ip, other._ip)==0));
}

const char* Transport::Address::ip() const {
    return _ip;
}

int Transport::Address::port() const {
    return _port;
}

bool Transport::Address::isValid() const {
    return (_port > 0) && (strlen(_ip) > 0);
}

char* Transport::Address::copyIp(const char* inIp) {
    char* newIp = new char[strlen(inIp)+1];
    strcpy(newIp, inIp);
    return newIp;
}

const int Transport::DEFAULT_PORT = 5678;

const int Transport::TPT_ERROR = -1;
const int Transport::TPT_OK = 0;
const int Transport::TPT_BUSY = 1;

const int Transport::NOT_DYNAMIC_PLAYER_SETUP = -1;
const int Transport::PLAYER_NOT_YET_DETERMINED = -2;

const char* Transport::LOCALHOST_IP = "127.0.0.1";


Transport::Transport(bool useDynamicSetup) :
transportNum(0),
connected(false) {
    streamBufferSize = 1024;
    streamBuffer = new char[streamBufferSize]; // TODO: Make this more dynamic.
    charsInStreamBuffer = 0;
    dynamicPlayerSetupNumber = (useDynamicSetup ? PLAYER_NOT_YET_DETERMINED : NOT_DYNAMIC_PLAYER_SETUP);
}

Transport::~Transport()
{
    delete [] streamBuffer;
}

bool Transport::isConnected() {
    return connected;
}

int Transport::getDynamicPlayerSetupNumber() {
    return dynamicPlayerSetupNumber;
}
void Transport::setDynamicPlayerSetupNumber(int newNum) {
    dynamicPlayerSetupNumber = newNum;
}

void Transport::setTransportNum(int inTransportNum) {
    transportNum = inTransportNum;
}

int Transport::sendPacket(const char* packetData) {
	int n = writeData(packetData, strlen(packetData)+1); // +1 to include the \0
	if (n < 0) {
		Logger::logError("ERROR writing to socket");
	}
	else {
        // Logger::log() << "Sent \"" << packetData << "\"" << Logger:EOM;
    }
	return n;
}


int Transport::getPacket(char* buffer, int bufferLength) {
    int hitError = 0;
    int ranOutOfData = 0;
    int delimeterIndex = -1;
    int startOfNewData = 0;
    while ((delimeterIndex < 0) && !hitError && !ranOutOfData) {
        
        // Search through the new data for a delimeter.
        for(int ctr=startOfNewData; (delimeterIndex < 0) && (ctr<charsInStreamBuffer); ++ctr) {
            delimeterIndex = (streamBuffer[ctr] == '\0' ? ctr : -1);
        }
        
        // Detect if we've run out of buffer.
        if ((delimeterIndex < 0) && (charsInStreamBuffer >= streamBufferSize)) {
			Logger::logError("ERROR reading from socket.  Packet too big for buffer.  Truncating.");
            streamBuffer[streamBufferSize-1] = '\0';
            delimeterIndex = streamBufferSize-1;
        }
        
        if (delimeterIndex < 0) {
            // If we don't have delimeter, pull more data off the socket
            startOfNewData = charsInStreamBuffer;
            int charsToRead = streamBufferSize-charsInStreamBuffer;
            int n = readData(streamBuffer+charsInStreamBuffer, charsToRead);
            if (n == -1) {
                // Socket has no more data.  Return no data even if we have some sitting in the stream buffer.
                ranOutOfData = 1;
            }
            else if (n < 0) {
                hitError = n;
            } else {
                charsInStreamBuffer += n;
            }
        }
    }
    
    int charsInPacket = 0;
    if (hitError) {
		Logger::logError("ERROR reading from socket");
    } else if (ranOutOfData) {
        charsInPacket = 0;
        buffer[0] = '\0';
    } else {
        // Copy the data into the passed in buffer.
        charsInPacket = delimeterIndex; // We don't copy the delimeter
        if (delimeterIndex >= bufferLength) {
			Logger::logError("ERROR reading from socket.  Packet too big for buffer.  Truncating.");
            charsInPacket = bufferLength-1;
        }
        memcpy(buffer, streamBuffer, charsInPacket * sizeof(char));
        buffer[charsInPacket] = '\0';
        
        // Remove the characters from the stream buffer
        memmove(streamBuffer, streamBuffer+delimeterIndex+1, (charsInStreamBuffer-delimeterIndex-1)*sizeof(char));
        charsInStreamBuffer = charsInStreamBuffer-delimeterIndex-1;
        Logger::log() << "Received message: \"" << buffer << "\"" << Logger::EOM;
    }
    
    return (hitError ? hitError : charsInPacket);
}

void Transport::appendDataToBuffer(const char *data, int dataLength) {
    // TODO: Should really resize buffer rather than truncate.
    bool truncating = false;
    if (charsInStreamBuffer + dataLength > streamBufferSize) {
        Logger::logError("ERROR reading from socket.  Packet too big for buffer.  Truncating.");
        dataLength = streamBufferSize-charsInStreamBuffer;
        truncating = true;
    }
    memcpy(streamBuffer+charsInStreamBuffer, data, dataLength * sizeof(char));
    charsInStreamBuffer += dataLength;
    if (truncating) {
        streamBuffer[streamBufferSize-1] = '\0';
    }
}

void Transport::testTransport(Transport& t) {
    int NUM_MESSAGES = 10;
    t.connect();
    while (!t.isConnected()) {
        Sys::sleep(1000);
    }
    // Make sure test roles are negotiated.
    if (t.dynamicPlayerSetupNumber != NOT_DYNAMIC_PLAYER_SETUP) {
        t.setTransportNum(t.dynamicPlayerSetupNumber);
    }
    
    if (t.transportNum == 1) {
        printf("Test setup.  Sending packets.\n");
        int numSent = 0;
        for(int ctr=0; ctr<NUM_MESSAGES; ++ctr) {
            char message[256];
            sprintf(message, "Message %d%c", ctr+1, '\0');
            int charsSent = t.sendPacket(message);
            if (charsSent <= 0) {
                perror("Error sending packet");
            } else {
                ++numSent;
            }
            if (ctr == (NUM_MESSAGES/2)) {
                printf("Pausing\n");
                Sys::sleep(5000);
            }
        }
        printf("Sent %d messages.  %s.\n", numSent, (numSent == 10 ? "PASS" : "FAIL"));
    } else {
        printf("Test setup.  Receiving packets.\n");
        int numReceived = 0;
        // We wait a second for the sender to send some stuff
        Sys::sleep(2000);
        for(int ctr=0;ctr<NUM_MESSAGES;++ctr) {
            char buffer[256];
            int charsReceived = t.getPacket(buffer, 256);
            if (charsReceived < 0) {
                perror("Error receiving packet");
            } else if (charsReceived == 0) {
                printf("Received no data.\n");
            } else if (buffer[0] == 'U') {
                // Part of the UDP setup.  Ignore.
            } else {
                ++numReceived;
            }
        }
        printf("Received %d messages.  %s.\n", numReceived, (numReceived == 6 ? "PASS" : "FAIL"));
    }
    // Pause
    printf("Hit Return to exit");
    char tmpBuffer[256];
    fgets(tmpBuffer,255,stdin);
    printf("Exiting");
    exit(0);

}



/**
 * Parse an socket address of the form 127.0.0.1:5678.
 * Port may be omitted, in which case the port will be 0.
 */
Transport::Address Transport::parseUrl(const char* socketAddr) {
    char temp[256];
    // TODO: Isn't there a find() defined somewhere?
    int colonIndex = -1;
    for(int ctr=0; (colonIndex == -1) && (socketAddr[ctr] != '\0'); ++ctr) {
        if (socketAddr[ctr] == ':') {
            colonIndex = ctr;
        }
    }
    if (colonIndex > 0) {
        strcpy(temp, socketAddr);
        temp[colonIndex] = '\0';
        int port = atoi((socketAddr+colonIndex+1));
        return Address(temp, port);
    } else {
        return Address(socketAddr, 0);
    }
}

