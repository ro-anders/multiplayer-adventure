//
//  UdpTransport.cpp
//  MacAdventure
//

#include "UdpTransport.hpp"

#include <stdlib.h>
#include <string.h>
#include "Sys.hpp"
#include "UdpSocket.hpp"

const char* UdpTransport::NOT_YET_INITIATED = "UX";
const char* UdpTransport::RECVD_NOTHING = "UA";
const char* UdpTransport::RECVD_MESSAGE = "UB";
const char* UdpTransport::RECVD_ACK = "UC";

UdpTransport::UdpTransport(UdpSocket* inSocket, bool useDynamicSetup) :
Transport(useDynamicSetup),
socket(inSocket),
myExternalAddr(Address()),
theirAddrs(NULL),
myInternalPort(0),
states(new const char*[2]), // We always create space for two even though we may only use one.
numOtherMachines(0),
remaddrs(new sockaddr_in*[2]) // We always create space for two even though we may only use one.
{
    remaddrs[0] = remaddrs[1] = NULL;
    if (useDynamicSetup) {
        myInternalPort = DEFAULT_PORT;
    }
    
}

UdpTransport::~UdpTransport() {
    delete[] theirAddrs;
    delete[] states;
    if (remaddrs != NULL) {
        for(int ctr=0; ctr<2; ++ctr) {
            if (remaddrs[ctr] != NULL) {
                socket->deleteAddress(remaddrs[ctr]);
            }
        }
        delete[] remaddrs;
    }
}

/**
 * The ip address to tell other machines to use to talk to this machine.
 */
void UdpTransport::setExternalAddress(const Address& myExternalAddrIn) {
    myExternalAddr = myExternalAddrIn;
}

void UdpTransport::setInternalPort(int port) {
    myInternalPort = port;
}

void UdpTransport::addOtherPlayer(const Address & theirAddr) {
    if (numOtherMachines < 2) {
        Address* oldAddrs = theirAddrs;
        
        ++numOtherMachines;
        theirAddrs = new Address[numOtherMachines];
        theirAddrs[numOtherMachines-1]=theirAddr;
        states[numOtherMachines-1] = NOT_YET_INITIATED;
        remaddrs[numOtherMachines-1] = socket->createAddress(theirAddr);
        
        // Copy old data
        if (numOtherMachines == 2) {
            theirAddrs[0] = oldAddrs[0];
            delete[] oldAddrs;
        }
    }
}



void UdpTransport::connect() {
    if (getDynamicSetupNumber() == NOT_YET_DETERMINED) {
        // Try the default setup (using DEFAULT_PORT to talk to localhost on DEFAULT_PORT + 1)
        // If that is busy, switch them.
        // In a test, the only thing setup would be the internal port.  So all other attributes need to be
        // specified once a port is chosen.
        int busy = openSocket();
        if (busy == Transport::TPT_OK) {
            myExternalAddr = Address(LOCALHOST_IP, DEFAULT_PORT);
            addOtherPlayer(Address(LOCALHOST_IP, DEFAULT_PORT+1));
        } else if (busy == Transport::TPT_BUSY) {
            myExternalAddr = Address(LOCALHOST_IP, DEFAULT_PORT+1);
            addOtherPlayer(Address(LOCALHOST_IP, DEFAULT_PORT));
            myInternalPort = DEFAULT_PORT+1;
            openSocket();
        }
    } else {
        openSocket();
    }
    
    states[0] = RECVD_NOTHING;
    states[1] = (numOtherMachines > 1 ? RECVD_NOTHING : RECVD_ACK);
    // We need a big random integer.
    randomNum = Sys::random() * 1000000;
    
    printf("Attempting to punch hole to other machine.");
    punchHole();
}

int UdpTransport::openSocket() {
    
    // Create the server socket and bind to it
    int status = socket->bind(myInternalPort);

    if (status == Transport::TPT_OK) {
        printf("Bound to port %d.\n", myInternalPort);
        socket->setBlocking(false);
    } else {
        printf("Failed to bind to port %d.\n", myInternalPort);
    }
    
    
    return status;
}


bool UdpTransport::isConnected() {
    if (!connected) {
            punchHole();
    }
    return connected;
}

int UdpTransport::getPacket(char* buffer, int bufferLength) {
    if (connected && !hasDataInBuffer()) {
        // Now that setup is done, we can stop buffering data.
        return socket->readData(buffer, bufferLength);
    } else {
        return Transport::getPacket(buffer, bufferLength);
    }
}

int UdpTransport::readData(char* buffer, int bufferLength) {
    return socket->readData(buffer, bufferLength);
}

int UdpTransport::writeData(const char* data, int numBytes) {
    int leastCharsWritten = 1000000;
    for(int ctr=0; ctr<numOtherMachines; ++ctr) {
        int charsWritten = socket->writeData(data, numBytes, remaddrs[ctr]);
        if (charsWritten < leastCharsWritten) {
            leastCharsWritten = charsWritten;
        }
    }
    return leastCharsWritten;
}


void UdpTransport::punchHole() {
    
    // Since this is UDP and NATs may be involved, our messages may be blocked until the other game sends
    // packets to us.  So we must enter a loop constantly sending a message and checking if we are getting their
    // messages.  The owner of this class handles the looping, but this is one instance of the loop.
    
    if (!connected) {

        char sendBuffer[16] = "";
        const int READ_BUFFER_LENGTH = 200;
        char recvBuffer[READ_BUFFER_LENGTH];
        bool justAcked[2] = {false, false};
    
        // See what messages we have received.
        int bytes = socket->readData(recvBuffer, READ_BUFFER_LENGTH);
        while (bytes > 0) {
            // This could be non-setup messages, which need to be put in the base class's stream buffer
            // until the setup is complete.
            if (recvBuffer[0] != RECVD_NOTHING[0]) { // Only UDP setup messages begin with 'U'
                appendDataToBuffer(recvBuffer, bytes);
            } else {
                if (bytes != 10) {
                    Sys::log("Read packet of unexpected length.");
                    printf("Message=%s.  Bytes read=%d.\n", recvBuffer, bytes);
                }
                // Figure out the sender.  The third character will be the machine number.
                char senderChar = recvBuffer[2];
                // Two machine games don't need this so the number is always 0.
                // Three machine games need to map a 0, 1, or 2 to their array of machines 0 or 1.
                int senderInt = senderChar - '0';
                int senderIndex = (senderInt <= transportNum ? senderInt : senderInt-1);
                
                if (senderIndex >= 0) {
                    if (states[senderIndex] == RECVD_NOTHING) {
                        states[senderIndex] = RECVD_MESSAGE;
                    }
                    // See if they have received ours and this is the first time we have seen them receive ours
                    if ((recvBuffer[1] != RECVD_NOTHING[1]) && (states[senderIndex] != RECVD_ACK)) {
                        // They have received our message, and now we have received their's.  So ACK.
                        states[senderIndex] = RECVD_ACK;
                        justAcked[senderIndex] = true;
                        connected = states[1-senderIndex] == RECVD_ACK;
                        char logMsg[1000];
                        sprintf(logMsg, "Connected with %s:%d\n", theirAddrs[senderIndex].ip(), theirAddrs[senderIndex].port());
                        Sys::log(logMsg);
                        // If this is a test case, figure out who is player one.
                        if (getDynamicSetupNumber() == NOT_YET_DETERMINED) {
                            compareNumbers(randomNum, recvBuffer, senderIndex);
                        }
                    }
                }
            }
            
            // Check for more messages.
            bytes = socket->readData(recvBuffer, READ_BUFFER_LENGTH);
        }
        
        // Now send a packet to each other machine.  Don't send if we're not initialized or we're all connected.
        // Note, we may look all connected, but if we just got acknowledged we need to send one more message.
        for(int ctr=0; ctr<numOtherMachines; ++ctr) {
            if (justAcked[ctr] || ((states[ctr] != NOT_YET_INITIATED) && (states[ctr] != RECVD_ACK))) {
                sprintf(sendBuffer, "%s%d%ld", states[ctr], transportNum, randomNum);
                sendPacket(sendBuffer);
            }
        }
    }
}

void UdpTransport::compareNumbers(int myRandomNumber, char* theirMessage, int otherIndex) {
    // Whoever has the lowest number is given connect number 0.  In the one in a
    // million chance that they picked the same number, use alphabetically order of
    // the IP and port.
    
    long theirRandomNumber;
    // Pull out the number and figure out the connect number.
    char temp1, temp2, temp3;
    int parsed = sscanf(theirMessage, "%c%c%c%ld", &temp1, &temp2, &temp3, &theirRandomNumber);
    if (parsed < 3) {
		Sys::log("Parsed packet of unexpected length.");
        printf("Message=%s.  Number=%ld.  Parsed=%d.\n", theirMessage, theirRandomNumber, parsed);
    }
    
    if (myRandomNumber < theirRandomNumber) {
        setDynamicSetupNumber(0);
    } else if (theirRandomNumber < myRandomNumber) {
        setDynamicSetupNumber(1);
    } else {
        int ipCmp = strcmp(myExternalAddr.ip(), theirAddrs[otherIndex].ip());
        if (ipCmp < 0) {
            setDynamicSetupNumber(0);
        } else if (ipCmp > 0) {
            setDynamicSetupNumber(1);
        } else {
            // If IP's are equal then ports can't be equal.
            setDynamicSetupNumber(myExternalAddr.port() < theirAddrs[otherIndex].port() ? 0 : 1);
        }
    }
}

int UdpTransport::reservePort() {
    return -1;
}




