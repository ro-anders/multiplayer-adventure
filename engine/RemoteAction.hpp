

#ifndef RemoteAction_hpp
#define RemoteAction_hpp

#include <stdio.h>

class RemoteAction {
public:
    int sender;				// The number of the player sending this action (1-3)
    RemoteAction();
    RemoteAction(int inSender);
    virtual ~RemoteAction();
    
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
    
    MoveAction();
    
    MoveAction(int inSender, int inRoom, int inPosx, int inPosy, int inVelx, int inVely);
    
    virtual ~MoveAction();
    
    virtual int serialize(char* buffer, int bufferLength) = 0;
    
    virtual void deserialize(const char* message) = 0;
};

class PlayerMoveAction: public MoveAction {
public:
    PlayerMoveAction();
    
    PlayerMoveAction(int inSender, int inRoom, int inPosx, int inPosy, int inVelx, int inVely);
    
    ~PlayerMoveAction();
    
    int serialize(char* buffer, int bufferLength);
    
    void deserialize(const char* message);
};

class DragonMoveAction: public MoveAction {
public:
    int dragonNum;          // 0=Rhindle, 1=Yorgle, 2=Grindle
    int distance;           // Distance from player reporting position
    
    DragonMoveAction();
    
    DragonMoveAction(int inSender, int inRoom, int inPosx, int inPosy, int inVelx, int inVely,
                     int inDragonNum, int inDistance);
    
    ~DragonMoveAction();
    
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
    
    PlayerPickupAction();
    
    PlayerPickupAction(int inSender, int inPickupObject, int inPickupX, int inPickupY, int dropObject, int inRoom, int dropX, int dropY);
    
    ~PlayerPickupAction();
    
    void setPickup(int inPickupObject, int inPickupX, int inPickupY);
    
    void setDrop(int inDropObject, int inDropRoom, int inDropX, int inDropY);
    
    int serialize(char* buffer, int bufferLength);
    
    void deserialize(const char* message);
};

#endif /* RemoteAction_hpp */
