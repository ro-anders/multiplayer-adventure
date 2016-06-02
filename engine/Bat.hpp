
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

    Bat(int color);
    
    virtual ~Bat();
    
    void moveOneTurn(Sync* sync, BALL* thisPlayer);
    
    void lookForNewObject();
    
    /**
     * A bat can process BatMoveActions and BatPickupActions and update its internal state accordingly.
     */
    void handleAction(RemoteAction* action, BALL* objectBall);

private:
    
    static int MAX_FEDUP;
    
    int batFedUpTimer;
    
    void pickupObject(int object, Sync* sync);
    
    void broadcastMoveAction(Sync* sync, BALL* thisPlayer);
};

#endif /* Bat_hpp */
