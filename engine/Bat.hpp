
#ifndef Bat_hpp
#define Bat_hpp

#include <stdio.h>
#include "GameObject.hpp"

class BALL;
class RemoteAction;
class Sync;

class Bat: public OBJECT {
public:
    int linkedObject;           // index of linked (carried) object
    int linkedObjectX;
    int linkedObjectY;

    Bat(int color, int inRoom, int inX, int inY);
    
    virtual ~Bat();
    
    void moveOneTurn(Sync* sync, BALL* thisPlayer);
    
    void lookForNewObject();
    
private:
    
    static int MAX_FEDUP;
    
    int batFedUpTimer;
    
    void pickupObject(int object, Sync* sync);
    
    void broadcastMoveAction(Sync* sync, BALL* thisPlayer);

    /**
     * A bat can process BatMoveActions and BatPickupActions and update its internal state accordingly.
     */
    void handleAction(RemoteAction* action, BALL* objectBall);
};

#endif /* Bat_hpp */
