//
//  UdpTransport.cpp
//  MacAdventure
//
#include "sys.h"
#include "UdpTransport.hpp"

#include <stdlib.h>
#include <string.h>
#include "Logger.hpp"
#include "Sys.hpp"
#include "UdpSocket.hpp"

const char* UdpTransport::NOT_YET_INITIATED = "UX";
const char* UdpTransport::RECVD_NOTHING = "UA";
const char* UdpTransport::RECVD_MESSAGE = "UB";
const char* UdpTransport::RECVD_ACK = "UC";


UdpTransport::Client::Client() {}

UdpTransport::Client::~Client() {}

UdpTransport::UdpTransport(UdpSocket* inSocket, bool useDynamicSetup) :
Transport(useDynamicSetup),
socket(inSocket),
otherMachines(new Client[2]), // We always create space for two even though we may only use one.
myInternalPort(0),
states(new const char*[2]), // We always create space for two even though we may only use one.
numOtherMachines(0),
internalIps(NULL),
numInternalIps(0),
socketBound(false)
{
    if (useDynamicSetup) {
        myInternalPort = DEFAULT_PORT;
    }
    
}

UdpTransport::~UdpTransport() {
    for(int ctr=0; ctr<numInternalIps; ++ctr) {
        delete[] internalIps[ctr];
    }
    delete[] internalIps;
    delete[] states;
    for(int ctr=0; ctr<numOtherMachines; ++ctr) {
        List<struct sockaddr_in*>& socketList = otherMachines[ctr].sockaddrs;
        for(int ctr2=0; ctr2<socketList.size(); ++ctr2) {
            socket->deleteAddress(socketList.get(ctr2));
            socketList.set(ctr2, NULL);
        }
    }
    delete[] otherMachines;
}

void UdpTransport::setInternalPort(int port) {
    myInternalPort = port;
}

void UdpTransport::addOtherPlayer(const Transport::Address &theirAdddr) {
    addOtherPlayer(&theirAdddr, 1);
}

void UdpTransport::addOtherPlayer(const Address * addresses, int numAddresses) {
    if (numOtherMachines < 2) {
        ++numOtherMachines;

        // We only set the possible addresses.  We let the hole punching determine which of those to use.
        for(int ctr=0; ctr<numAddresses; ++ctr) {
            otherMachines[numOtherMachines-1].possibleAddrs.add(addresses[ctr]);
            otherMachines[numOtherMachines-1].sockaddrs.add(socket->createAddress(addresses[ctr]));
        }
        states[numOtherMachines-1] = NOT_YET_INITIATED;
    }
}


void UdpTransport::connect() {
    
    if (!socketBound) {
        if (getDynamicPlayerSetupNumber() == PLAYER_NOT_YET_DETERMINED) {
            // Try the default setup (using DEFAULT_PORT to talk to localhost on DEFAULT_PORT + 1)
            // If that is busy, switch them.
            // In a test, the only thing setup would be the internal port.  So all other attributes need to be
            // specified once a port is chosen.
            reservePort();
            if (myInternalPort == DEFAULT_PORT) {
                addOtherPlayer(Address(LOCALHOST_IP, DEFAULT_PORT+1));
            } else {
                addOtherPlayer(Address(LOCALHOST_IP, DEFAULT_PORT));
            }
        } else {
            reservePort();
        }
    }
    
    socket->setBlocking(false);
    
    states[0] = RECVD_NOTHING;
    states[1] = (numOtherMachines > 1 ? RECVD_NOTHING : RECVD_ACK);
    // We need a big random integer.
    randomNum = Sys::random() * 1000000;
    
    Logger::log("Attempting to punch hole to other machine.");
    punchHole();
}

UdpSocket& UdpTransport::reservePort() {
    if (getDynamicPlayerSetupNumber() == PLAYER_NOT_YET_DETERMINED) {
        // Try the default setup (using DEFAULT_PORT to talk to localhost on DEFAULT_PORT + 1)
        // If that is busy, switch them.
        int busy = openSocket();
        if (busy == Transport::TPT_BUSY) {
            myInternalPort = DEFAULT_PORT+1;
            openSocket();
        }
    } else if (myInternalPort == 0) {
        // Just try to find some port that is open.
        bool found = false;
        myInternalPort = DEFAULT_PORT;
        while (!found) {
            int busy = openSocket();
            if (busy == Transport::TPT_BUSY) {
                ++myInternalPort;
            } else {
                found = true;
            }
        }
    } else {
        openSocket();
    }
    return *socket;
}


int UdpTransport::openSocket() {
    
    // Create the server socket and bind to it
    int status = socket->bind(myInternalPort);

    if (status == Transport::TPT_OK) {
        Logger::log() << "Bound to port " << myInternalPort << Logger::EOM;
        socketBound = true;
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
    return writeData(data, numBytes, -1);
}

int UdpTransport::writeData(const char* data, int numBytes, int recipient) {
    int firstRecipient = (recipient == -1 ? 0 : recipient);
    int lastRecipient = (recipient == -1 ? numOtherMachines-1 : recipient);
    
    // We want to return the characters sent to the client and, in the case that there are multiple
    // clients and some problem causes fewer characters to be sent to one client, we report
    // the smaller number.
    // If a client has multiple IPs, we use the number of characters sent to the most
    // succesful IP.
    int leastCharsWritten = 1000000;
    for(int ctr=firstRecipient; ctr<=lastRecipient; ++ctr) {
        List<struct sockaddr_in*>& socketList = otherMachines[ctr].sockaddrs;
        int mostCharsWrittenToClient = -1;
        for(int ctr2=0; ctr2<socketList.size(); ++ctr2) {
            int charsWritten = socket->writeData(data, numBytes, socketList.get(ctr2));
            if (charsWritten > mostCharsWrittenToClient) {
                mostCharsWrittenToClient = charsWritten;
            }
        }
        if (mostCharsWrittenToClient < leastCharsWritten) {
            leastCharsWritten = mostCharsWrittenToClient;
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
        Address from;
        int bytes = socket->readData(recvBuffer, READ_BUFFER_LENGTH, from);
        while (bytes > 0) {
            // This could be non-setup messages, which need to be put in the base class's stream buffer
            // until the setup is complete.
            if (recvBuffer[0] != RECVD_NOTHING[0]) { // Only UDP setup messages begin with 'U'
                appendDataToBuffer(recvBuffer, bytes);
            } else {
                if (bytes != 10) {
                    Logger::logError() << "Read packet of unexpected length.\n" <<
                    "Message=" << recvBuffer << ".  Bytes read=" << bytes << Logger::EOM;
                }
                // Figure out the sender.  The third character will be the machine number.
                char senderChar = recvBuffer[2];
                // Two machine games don't need this so the number is always 0.
                // Three machine games need to map a 0, 1, or 2 to their array of machines 0 or 1.
                int senderInt = senderChar - '0';
                if ((senderInt == transportNum) && (getDynamicPlayerSetupNumber() == NOT_DYNAMIC_PLAYER_SETUP)) {
                    Logger::logError("Received UDP setup message trying to acquire same slot as this game.");
                } else {
                    int senderIndex = (senderInt <= transportNum ? senderInt : senderInt-1);
                    
                    if (senderIndex >= 0) {
                        
                        Client& senderMachine = otherMachines[senderIndex];
                        // If we still have multiple possible addresses for this sender
                        // note the address from which this message came
                        if (senderMachine.possibleAddrs.size() > 1) {
                            reduceClientToOneAddress(senderInt, senderMachine, from);
                        }
                        
                        if (states[senderIndex] == RECVD_NOTHING) {
                            states[senderIndex] = RECVD_MESSAGE;
                            Logger::log() << "Received message from " << from.ip() << ":" <<
                            from.port() << ".  Waiting for ack." << Logger::EOM;
                        }
                        // See if they have received ours and this is the first time we have seen them receive ours
                        if ((recvBuffer[1] != RECVD_NOTHING[1]) && (states[senderIndex] != RECVD_ACK)) {
                            // They have received our message, and now we have received their's.  So ACK.
                            states[senderIndex] = RECVD_ACK;
                            justAcked[senderIndex] = true;
                            connected = states[1-senderIndex] == RECVD_ACK;
                            Logger::log() << "Connected with " << from.ip() << ":" <<
                            from.port() << Logger::EOM;
                            // If this is a test case, figure out who is player one.
                            if (getDynamicPlayerSetupNumber() == PLAYER_NOT_YET_DETERMINED) {
                                compareNumbers(randomNum, recvBuffer, senderIndex);
                            }
                        }
                    }
                }
            }
            
            // Check for more messages.
            bytes = socket->readData(recvBuffer, READ_BUFFER_LENGTH, from);
        }
        
        // Now send a packet to each other machine.  Don't send if we're not initialized or we're all connected.
        // Note, we may look all connected, but if we just got acknowledged we need to send one more message.
        for(int ctr=0; ctr<numOtherMachines; ++ctr) {
            if (justAcked[ctr] || ((states[ctr] != NOT_YET_INITIATED) && (states[ctr] != RECVD_ACK))) {
                sprintf(sendBuffer, "%s%d%06ld", states[ctr], transportNum, randomNum);
                writeData(sendBuffer, strlen(sendBuffer)+1, ctr);
            }
        }
    }
}

/**
 * Remove address and socket entries in this client that don't match the passed in one.
 * clientNum - the number of the client - used only for logging and debugging
 * client - the client to operate on
 * from - the address to keep.  NOTE: This should not be a reference to an Address in the passed in Client as
 * it will not be valid after this call.
 */
void UdpTransport::reduceClientToOneAddress(int clientNum, Client& otherMachine, Transport::Address& from) {
    // Figure out which slot has the right address.
    int found = -1;
    for(int ctr=0; (found < 0) && (ctr<otherMachine.possibleAddrs.size()); ++ctr) {
        found = (otherMachine.possibleAddrs.get(ctr) == from ? ctr : -1);
    }
    if (found < 0) {
        Logger::log() << "Received message for " << clientNum << " from unexpected address "  << from.ip() <<
            ":" << from.port() << Logger::EOM;
    } else {
        struct sockaddr_in* keep = otherMachine.sockaddrs.get(found);
        for(int ctr=0; ctr<otherMachine.sockaddrs.size(); ++ctr) {
            if (ctr != found) {
                socket->deleteAddress(otherMachine.sockaddrs.get(ctr));
            }
        }
        otherMachine.sockaddrs.clear();
        otherMachine.possibleAddrs.clear();
        otherMachine.possibleAddrs.add(from);
        otherMachine.sockaddrs.add(keep);
    }
}

void UdpTransport::compareNumbers(int myRandomNumber, char* theirMessage, int otherIndex) {
    // Whoever has the lowest number is given connect number 0.  In the one in a
    // million chance that they picked the same number, just abort - dynamic player setup is only used in testing.
    
    long theirRandomNumber;
    // Pull out the number and figure out the connect number.
    char temp1, temp2, temp3;
    int parsed = sscanf(theirMessage, "%c%c%c%ld", &temp1, &temp2, &temp3, &theirRandomNumber);
    if (parsed < 3) {
		Logger::logError() << "Parsed packet of unexpected length." <<
        "Message=" << theirMessage << ".  Number=" << theirRandomNumber << ".  Parsed=" << parsed << Logger::EOM;
    }
    
    if (myRandomNumber < theirRandomNumber) {
        setDynamicPlayerSetupNumber(0);
    } else if (theirRandomNumber < myRandomNumber) {
        setDynamicPlayerSetupNumber(1);
    } else {
        Logger::logError("Cannot dynamically determine player number - both machines used same random number");
        abort();
    }
}

/**
 * Deduce all the IPs that this machine is using.  Does not include localhost.
 */
List<Transport::Address> UdpTransport::determineThisMachineIPs() {
    List<Transport::Address> addrs = socket->getLocalIps();

    // Add ports to all addresses
    for(int ctr=0; ctr<addrs.size(); ++ctr) {
        addrs.set(ctr, Transport::Address(addrs.get(ctr).ip(), myInternalPort));
    }
                  
    return addrs;
}




