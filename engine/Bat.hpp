
#ifndef Bat_hpp
#define Bat_hpp

#include <stdio.h>
#include "GameObject.hpp"

class RemoteAction;
class Sync;

class Bat: public OBJECT {
public:
    int linkedObject;           // index of linked (carried) object
    int linkedObjectX;
    int linkedObjectY;

    Bat(int color, int inRoom, int inX, int inY);
    
    virtual ~Bat();
    
    /**
     * A bat can process BatMoveActions and BatPickupActions and update its internal state accordingly.
     */
    OBJECT* handleAction(RemoteAction* action);
    
    void moveOneTurn(Sync* sync);
    
    void lookForNewObject();
    
private:
    
    void pickupObject(int object, Sync* sync);
    
    void broadcastMoveAction(Sync* sync);

    int batFedUpTimer;
    
    static int MAX_FEDUP;
    
};

#endif /* Bat_hpp */
