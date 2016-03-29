//
//  UdpTransport.cpp
//  MacAdventure
//

#include "UdpTransport.hpp"

#include <stdlib.h>
#include <string.h>
#include "Sys.hpp"

const char* UdpTransport::NOT_YET_INITIATED = "UX";
const char* UdpTransport::RECVD_NOTHING = "UA";
const char* UdpTransport::RECVD_MESSAGE = "UB";
const char* UdpTransport::RECVD_ACK = "UC";


UdpTransport::UdpTransport() :
Transport(true),
myExternalAddr(LOCALHOST_IP, DEFAULT_PORT),
theirAddr(LOCALHOST_IP, DEFAULT_PORT+1),
myInternalPort(DEFAULT_PORT),
state(NOT_YET_INITIATED)
{}

UdpTransport::UdpTransport(const Address& inMyExternalAddr,  const Address& inTheirAddr) :
Transport(false),
myExternalAddr(inMyExternalAddr),
theirAddr(inTheirAddr),
// We use the default port internally unless the other side is also on the same machine.
myInternalPort(strcmp(inTheirAddr.ip(), LOCALHOST_IP)==0 ? inMyExternalAddr.port() : DEFAULT_PORT),
state(NOT_YET_INITIATED)
{}

UdpTransport::~UdpTransport() {
}

void UdpTransport::connect() {
    if (getTestSetupNumber() == NOT_YET_DETERMINED) {
        // Try the default setup (using DEFAULT_PORT to talk to localhost on DEFAULT_PORT + 1)
        // If that is busy, switch them.
        int busy = openSocket();
        if (busy == Transport::TPT_BUSY) {
            myExternalAddr = Address(LOCALHOST_IP, DEFAULT_PORT+1);
            theirAddr = Address(LOCALHOST_IP, DEFAULT_PORT);
            myInternalPort = DEFAULT_PORT+1;
            openSocket();
        }
    } else {
        openSocket();
    }
    
    state = RECVD_NOTHING;
    printf("Bound to socket.  Initiating handshake.\n");
    // We need a big random integer.
    randomNum = Sys::random() * 1000000;
    
    punchHole();
}

bool UdpTransport::isConnected() {
    if ((!connected) && (state != NOT_YET_INITIATED)) {
            punchHole();
    }
    return connected;
}



void UdpTransport::punchHole() {
    
    // Since this is UDP and NATs may be involved, our messages may be blocked until the other game sends
    // packets to us.  So we must enter a loop constantly sending a message and checking if we are getting their
    // messages.  The owner of this class handles the looping, but this is one instance of the loop.
    
    char sendBuffer[16] = "";
    const int READ_BUFFER_LENGTH = 200;
    char recvBuffer[READ_BUFFER_LENGTH];
    
    if (state != RECVD_ACK) {
        printf("Checking for messages.\n");
        // See what messages we have received.
        int bytes = getPacket(recvBuffer, READ_BUFFER_LENGTH);
        while (bytes > 0) {
            printf("Got message: %s.\n", recvBuffer);
            if (bytes != 8) {
                Sys::log("Read packet of unexpected length.");
                printf("Message=%s.  Bytes read=%d.\n", recvBuffer, bytes);
            }
            if (state == RECVD_NOTHING) {
                state = RECVD_MESSAGE;
            }
            if (recvBuffer[1] != RECVD_NOTHING[1]) {
                // They have received our message, and now we have received their's.  So ACK.
                state = RECVD_ACK;
                connected = true;
                // If this is a test case, figure out who is player one.
                if (getTestSetupNumber() == NOT_YET_DETERMINED) {
                        compareNumbers(randomNum, recvBuffer);
                }
            }
            
            // Check for more messages.
            printf("Checking for init messages.\n");
            bytes = getPacket(recvBuffer, READ_BUFFER_LENGTH);
        }
        
        printf("Sending init message.\n");
        // Now send a packet
        sprintf(sendBuffer, "%s%ld", state, randomNum);
        sendPacket(sendBuffer);
    }
}

void UdpTransport::compareNumbers(int myRandomNumber, char* theirMessage) {
    // Whoever has the lowest number is given connect number 0.  In the one in a
    // million chance that they picked the same number, use alphabetically order of
    // the IP and port.
    
    long theirRandomNumber;
    // Pull out the number and figure out the connect number.
    char temp1, temp2;
    int parsed = sscanf(theirMessage, "%c%c%ld", &temp1, &temp2, &theirRandomNumber);
    if (parsed < 3) {
		Sys::log("Parsed packet of unexpected length.");
        printf("Message=%s.  Number=%ld.  Parsed=%d.\n", theirMessage, theirRandomNumber, parsed);
    }
    
    if (myRandomNumber < theirRandomNumber) {
        setTestSetupNumber(0);
    } else if (theirRandomNumber < myRandomNumber) {
        setTestSetupNumber(1);
    } else {
        int ipCmp = strcmp(myExternalAddr.ip(), theirAddr.ip());
        if (ipCmp < 0) {
            setTestSetupNumber(0);
        } else if (ipCmp > 0) {
            setTestSetupNumber(1);
        } else {
            // If IP's are equal then ports can't be equal.
            setTestSetupNumber(myExternalAddr.port() < theirAddr.port() ? 0 : 1);
        }
    }
    
    
}



