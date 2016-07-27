//
//  Transport.hpp

#ifndef Transport_hpp
#define Transport_hpp

#include <stdio.h>

class Transport {
public:
    
    /** A simple class to hold an ip and port */
    /* There's gotta be a class that does this already. */
    class Address {
    public:
        Address();
        Address(const char* ip, int port);
        Address(const Address& other);
        ~Address();
        Address& operator=(const Address& other);
        bool operator==(const Address& other);
        const char* ip() const;
        int port() const;
        bool isValid() const;
    private:
        const char* _ip;
        int _port;
        static char* copyIp(const char* ip);
    };
    
    static const int DEFAULT_PORT;
    
    static const int NOT_A_TEST;
    
    /**
     * Create a transport. 
     * @param inATest whether this is a test scenario and transport should figure out
     * who is player 1 and player 2.
     */
    Transport(bool inATest);
    
    virtual ~Transport();
    
    /**
     * Connect to the other machine.  This is asynchronous.  You need to poll isConnecting() to
     * determine if a connection was made. 
     */
    virtual void connect() = 0;
    
    /**
     * Whether this transport has successfully connected to the other machine.
     */
    virtual bool isConnected();
    
    /**
     * Send a packet to a client.  Assumes the packet is \0 terminated.
     * Returns the number of bytes sent.
     */
    virtual int sendPacket(const char* packetData);
    
    /**
     * Polls the client for a message.  If found fills the buffer with the message
     * and returns the number of bytes in the message.  If no message, returns 0 and
     * leaves the buffer untouched.
     */
    virtual int getPacket(char* buffer, int bufferLength);
    
    /**
     * Often when testing we want to quickly launch two ends of a socket and let them
     * figure out which one will use which ports and who should be player one vs player two.
     * The will return 0 for player 1 and will return 1 for player 2.  Does not work with three players.
     * If this transport has not been setup for a quick test, will return NOT_A_TEST.
     */
    int getTestSetupNumber();
    
    /**
     * Parse an socket address of the form 127.0.0.1:5678 into an ip address and a port.
     */
    static Address parseUrl(const char* socketAddress);
    
    /**
     * This runs a test - assuming another transport has been setup to talk with. 
     */
    static void testTransport(Transport& tpt);
    
    
protected:
    
    bool hasDataInBuffer() {return charsInStreamBuffer > 0;}
    
    void appendDataToBuffer(const char* data, int dataLength);
    
    void setTestSetupNumber(int num);

    static const int NOT_YET_DETERMINED;
    
    bool connected;
    
    /** Return codes from connection methods */
    static const int TPT_ERROR;
    static const int TPT_OK;
    static const int TPT_BUSY;
    
    static const char* LOCALHOST_IP;
    
    /**
     * Send data on the socket.
     */
    virtual int writeData(const char* data, int numBytes) = 0;
    
    /**
     * Pull data off the socket - non-blocking
     */
    int readData(char* buffer, int bufferLength);
    
private:
    
    /** In a test setup, 0 = player 1, 1 = player 2.  Until a 0 or 1 is chosen will be NOT_YET_DETERMINED.
     * Will be NOT_A_TEST in a non-test setup. */
    int testSetupNumber;
    
    /** Buffer to store data until end of packet is reached. */
    char* streamBuffer;
    
    /** Size of stream buffer */
    int streamBufferSize;
    
    /** Number of characters read into stream buffer */
    int charsInStreamBuffer;
    
};

#endif /* Transport_hpp */
