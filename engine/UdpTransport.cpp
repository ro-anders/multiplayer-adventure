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
theirAddrs(new Address[1]),
myInternalPort(DEFAULT_PORT),
states(new const char*[1]),
numOtherMachines(1)
{
    theirAddrs[0] = Address(LOCALHOST_IP, DEFAULT_PORT+1);
    states[0] = NOT_YET_INITIATED;
}

UdpTransport::UdpTransport(const Address& inMyExternalAddr,  const Address& inTheirAddr) :
Transport(false),
myExternalAddr(inMyExternalAddr),
theirAddrs(new Address[1]),
// We use the default port internally unless the other side is also on the same machine.
myInternalPort(strcmp(inTheirAddr.ip(), LOCALHOST_IP)==0 ? inMyExternalAddr.port() : DEFAULT_PORT),
states(new const char*[1]),
numOtherMachines(1)
{
    theirAddrs[0] = inTheirAddr;
    states[0] = NOT_YET_INITIATED;
}

UdpTransport::UdpTransport(const Address& inMyExternalAddr,  const Address& other1, const Address& other2) :
Transport(false),
myExternalAddr(inMyExternalAddr),
theirAddrs(new Address[2]),
// We use the default port internally unless the other machines also on the same machine.
// We don't yet support having two games on one machine and a third game on a second machine
myInternalPort((strcmp(other1.ip(), LOCALHOST_IP)==0) && (strcmp(other1.ip(), LOCALHOST_IP)==0) ?
               inMyExternalAddr.port() : DEFAULT_PORT),
states(new const char*[1]),
numOtherMachines(2)
{
    theirAddrs[0] = other1;
    theirAddrs[1] = other2;
    states[0] = NOT_YET_INITIATED;
    states[1] = NOT_YET_INITIATED;
}

UdpTransport::~UdpTransport() {
    delete[] theirAddrs;
    delete[] states;
}

void UdpTransport::connect() {
    if (getTestSetupNumber() == NOT_YET_DETERMINED) {
        // Try the default setup (using DEFAULT_PORT to talk to localhost on DEFAULT_PORT + 1)
        // If that is busy, switch them.
        int busy = openSocket();
        if (busy == Transport::TPT_BUSY) {
            myExternalAddr = Address(LOCALHOST_IP, DEFAULT_PORT+1);
            theirAddrs[0] = Address(LOCALHOST_IP, DEFAULT_PORT);
            myInternalPort = DEFAULT_PORT+1;
            openSocket();
        }
    } else {
        openSocket();
    }
    
    states[0] = RECVD_NOTHING;
    states[1] = (numOtherMachines > 1 ? RECVD_NOTHING : RECVD_ACK);
    printf("Bound to socket.  Initiating handshake.\n");
    // We need a big random integer.
    randomNum = Sys::random() * 1000000;
    
    punchHole();
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
        return readData(buffer, bufferLength, NULL);
    } else {
        return Transport::getPacket(buffer, bufferLength);
    }
}

/**
 * Read a pacet off the UDP port and throw away the sender information.
 */
int UdpTransport::readData(char* buffer, int bufferLength) {
    return readData(buffer, bufferLength, NULL);
}

int UdpTransport::writeData(const char* data, int numBytes) {
    return writeData(data, numBytes, -1);
}


void UdpTransport::punchHole() {
    
    // Since this is UDP and NATs may be involved, our messages may be blocked until the other game sends
    // packets to us.  So we must enter a loop constantly sending a message and checking if we are getting their
    // messages.  The owner of this class handles the looping, but this is one instance of the loop.
    
    if (!connected) {

        char sendBuffer[16] = "";
        const int READ_BUFFER_LENGTH = 200;
        char recvBuffer[READ_BUFFER_LENGTH];
        Address sender;
        bool justAcked[2] = {false, false};
    
        printf("Checking for messages.\n");
        // See what messages we have received.
        int bytes = readData(recvBuffer, READ_BUFFER_LENGTH, &sender);
        while (bytes > 0) {
            printf("Got message: %s.\n", recvBuffer);
            // This could be non-setup messages, which need to be put in the base class's stream buffer
            // until the setup is complete.
            if (recvBuffer[0] != RECVD_NOTHING[0]) { // Only UDP setup messages begin with 'U'
                appendDataToBuffer(recvBuffer, bytes);
            } else {
                if (bytes != 9) {
                    Sys::log("Read packet of unexpected length.");
                    printf("Message=%s.  Bytes read=%d.\n", recvBuffer, bytes);
                }
                int senderIndex = -1;
                if (sender == theirAddrs[0]) {
                    senderIndex = 0;
                } else if ((numOtherMachines > 1) && (sender == theirAddrs[1])) {
                    senderIndex = 1;
                } else {
                    Sys::log("Received message for unknown sender.\n");
                    printf("%s:%d\n", sender.ip(), sender.port());
                }
                
                if (senderIndex > 0) {
                    if (states[senderIndex] == RECVD_NOTHING) {
                        states[senderIndex] = RECVD_MESSAGE;
                    }
                    if (recvBuffer[1] != RECVD_NOTHING[1]) {
                        // They have received our message, and now we have received their's.  So ACK.
                        states[senderIndex] = RECVD_ACK;
                        justAcked[senderIndex] = true;
                        connected = states[1-senderIndex] == RECVD_ACK;
                        // If this is a test case, figure out who is player one.
                        if (getTestSetupNumber() == NOT_YET_DETERMINED) {
                            compareNumbers(randomNum, recvBuffer, senderIndex);
                        }
                    }
                }
            }
            
            // Check for more messages.
            printf("Checking for init messages.\n");
            bytes = readData(recvBuffer, READ_BUFFER_LENGTH, &sender);
        }
        
        printf("Sending init message.\n");
        // Now send a packet to each other machine.  Don't send if we're not initialized or we're all connected.
        // Note, we may look all connected, but if we just got acknowledged we need to send one more message.
        for(int ctr=0; ctr<numOtherMachines; ++ctr) {
            if (justAcked[ctr] || ((states[ctr] != NOT_YET_INITIATED) && (states[ctr] != RECVD_ACK))) {
                sprintf(sendBuffer, "%s%ld", states[ctr], randomNum);
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
        int ipCmp = strcmp(myExternalAddr.ip(), theirAddrs[otherIndex].ip());
        if (ipCmp < 0) {
            setTestSetupNumber(0);
        } else if (ipCmp > 0) {
            setTestSetupNumber(1);
        } else {
            // If IP's are equal then ports can't be equal.
            setTestSetupNumber(myExternalAddr.port() < theirAddrs[otherIndex].port() ? 0 : 1);
        }
    }
}



