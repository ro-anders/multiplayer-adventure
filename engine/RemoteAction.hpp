

#ifndef RemoteAction_hpp
#define RemoteAction_hpp

#include <stdio.h>

class RemoteAction {
public:
    int sender;				// The number of the player sending this action (1-3)
    const char* typeCode;
    RemoteAction(const char* inCode);

    virtual ~RemoteAction();
    
    void setSender(int inSender);
    
    virtual int serialize(char* buffer, int bufferLength) = 0;
    virtual void deserialize(const char* message) = 0;
};

class MoveAction: public RemoteAction {
public:
    int room;				// The room the player was in
    int posx;				// The x-coordinate of the player in the room
    int posy;				// The y-coordinate of the player in the room
    int velx;				// -1 for moving left, 1 for right, and 0 for still or just up/down
    int vely;				// -1 for down, 1 for up, and 0 for still or just left/right
    
    MoveAction(const char* inCode);
    
    MoveAction(const char* inCode, int inRoom, int inPosx, int inPosy, int inVelx, int inVely);
    
    virtual ~MoveAction();
    
    virtual int serialize(char* buffer, int bufferLength) = 0;
    
    virtual void deserialize(const char* message) = 0;
};

class PlayerMoveAction: public MoveAction {
public:

    static const char* CODE;

    PlayerMoveAction();
    
    PlayerMoveAction(int inRoom, int inPosx, int inPosy, int inVelx, int inVely);
    
    ~PlayerMoveAction();
    
    int serialize(char* buffer, int bufferLength);
    
    void deserialize(const char* message);
};

class PlayerPickupAction: public RemoteAction {
public:
    int pickupObject;
    int pickupX;
    int pickupY;
    int dropObject;
    int dropRoom;
    int dropX;
    int dropY;
    
    static const char* CODE;

    PlayerPickupAction();
    
    PlayerPickupAction(int inPickupObject, int inPickupX, int inPickupY, int dropObject, int inRoom, int dropX, int dropY);
    
    ~PlayerPickupAction();
    
    void setPickup(int inPickupObject, int inPickupX, int inPickupY);
    
    void setDrop(int inDropObject, int inDropRoom, int inDropX, int inDropY);
    
    int serialize(char* buffer, int bufferLength);
    
    void deserialize(const char* message);
};

class PlayerResetAction: public RemoteAction {
public:
    
    static const char* CODE;
    
    PlayerResetAction();
    
    ~PlayerResetAction();
    
    int serialize(char* buffer, int bufferLength);
    
    void deserialize(const char* message);
};

class PlayerWinAction: public RemoteAction {
public:
    int winInRoom;
    
    static const char* CODE;
    
    PlayerWinAction();
    
    PlayerWinAction(int winInRoom);
    
    ~PlayerWinAction();
    
    int serialize(char* buffer, int bufferLength);
    
    void deserialize(const char* message);
};


class DragonMoveAction: public MoveAction {
public:
    int dragonNum;          // 0=Rhindle, 1=Yorgle, 2=Grindle
    int distance;           // Distance from player reporting position
    
    static const char* CODE;
    
    DragonMoveAction();
    
    DragonMoveAction(int inRoom, int inPosx, int inPosy, int inVelx, int inVely,
                     int inDragonNum, int inDistance);
    
    ~DragonMoveAction();
    
    int serialize(char* buffer, int bufferLength);
    
    void deserialize(const char* message);
    
};

class DragonStateAction: public RemoteAction {
public:
    int dragonNum;          // 0=Rhindle, 1=Yorgle, 2=Grindle
    int newState;
    int room;
    int posx;
    int posy;
    int velx;
    int vely;
    
    static const char* CODE;
    
    DragonStateAction();
    
    DragonStateAction(int inDragonNum, int inState, int inRoom, int inPosx, int inPosy, int inVelx, int inVely);
    
    ~DragonStateAction();
    
    int serialize(char* buffer, int bufferLength);
    
    void deserialize(const char* message);
    
};

class PortcullisStateAction: public RemoteAction {
public:
    int portPkey; // The Object::pkey of the portcullis, nothing to do with the key that unlocks it
    int newState;
    bool allowsEntry;
    
    static const char* CODE;
    
    PortcullisStateAction();
    
    PortcullisStateAction(int portPkey, int newState, bool allowsEntry);
    
    ~PortcullisStateAction();
    
    int serialize(char* buffer, int bufferLength);
    
    void deserialize(const char* message);
};

class BatMoveAction: public MoveAction {
public:
    
    int distance;           // Distance from player reporting position

    static const char* CODE;

    BatMoveAction();
    
    BatMoveAction(int inRoom, int inPosx, int inPosy, int inVelx, int inVely, int inDistance);
    
    ~BatMoveAction();
    
    int serialize(char* buffer, int bufferLength);
    
    void deserialize(const char* message);
};

class BatPickupAction: public RemoteAction {
public:
    int pickupObject;
    int pickupX;
    int pickupY;
    int dropObject;
    int dropRoom;
    int dropX;
    int dropY;
    
    static const char* CODE;
    
    BatPickupAction();
    
    BatPickupAction(int inPickupObject, int inPickupX, int inPickupY, int dropObject, int inRoom, int dropX, int dropY);

    ~BatPickupAction();
    
    int serialize(char* buffer, int bufferLength);
    
    void deserialize(const char* message);
};

/**
 * This places an object.  It is used when an object touches a gate and in level 3 when one client randomly
 * assigns the objects and then communicates the positions to the other clients.  It can be used to move
 * the dragons and bat, too.
 */
class ObjectMoveAction: public RemoteAction {
public:
    int object;
    int room;
    int x;
    int y;
    
    static const char* CODE;
    
    ObjectMoveAction();
    
    ObjectMoveAction(int inObject, int inRoom, int x, int y);
    
    ~ObjectMoveAction();
    
    int serialize(char* buffer, int bufferLength);
    
    void deserialize(const char* message);
};

class PingAction: public RemoteAction {
public:
    static const char* CODE;

    PingAction();
    
    ~PingAction();
    
    int serialize(char* buffer, int bufferLength);
    
    void deserialize(const char* message);
};





#endif /* RemoteAction_hpp */
