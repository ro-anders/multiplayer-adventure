

#ifndef Portcullis_hpp
#define Portcullis_hpp

#include <stdio.h>

#include "GameObject.hpp"

class ROOM;
class PortcullisStateAction;

class Portcullis: public OBJECT {
public:
    
    /** The x-coord you come out at when you leave a castle. */
    static const int EXIT_X;
    
    /** The y-coord you come out at when you leave a castle. */
    static const int EXIT_Y;
    
    /** The x-coordinate of where the portcullis is placed. */
    static const int PORT_X;
    
    /** The y-coordinate of where the portcullis is placed. */
    static const int PORT_Y;
    
    /** True if touching the gate will take you inside the castle.  False if gate is locked. */
    bool isActive;
    
    /** Room that the gate takes you to */
    int insideRoom;
    
    /** The key that unlocks this gate */
    OBJECT* key;
    
    static const int OPEN_STATE;
    static const int CLOSED_STATE;
    
    /**
     * label - unique name only used for logging and debugging
     * outsideRoom - index of the room the portal will be placed in
     * insideRoom - index of the room the portal leads to
     * lastInsideRoom - if there are multiple rooms inside this castle, they need to be all have contiguous
     *                  indexes starting with the room you enter.  This should be the last index of the rooms behind the gate.
     *                  Optional.  If unspecified will assume castle is a one room castle with only insideRoom.
     * key - the key that opens this gate.
     */
    Portcullis(const char* label, int outsideRoom, ROOM* insideRoom, OBJECT* key);
    
    virtual ~Portcullis();
    
    /**
     * If there are multiple rooms inside this castle, this will add this room to the portcullis's list of inside rooms.
     * This list is used for things like randomizing objects for game 3 and making sure all objects can still be reached.
     * The 'insideRoom' passed into the constructor is already added to this list.
     */
    void addRoom(ROOM* room);
    
    void setState(int newState, bool isActive);
    
    void updateState();
    
    void keyTouch();
    
    /**
     * See if this portcullis has been locked or unlocked.
     * Caller is responsible for releasing memory of returned remote action.
     */
    PortcullisStateAction* processTurn();
    
    void openFromInside();
    
    /**
     * Called when a player enters a gate that is not completely open.
     */
    void forceOpen();
    
    /**
     * Returns whether the room passed in is somewhere behind this gate.
     */
    bool containsRoom(int room);
    
private:
    
    /** Array of rooms inside this castle.  If NULL then it means only the insideRoom is inside this castle.  If not NULL,
     * the inside room will be included in the list.
     */
    int* allInsideRooms;
    
    /** Number entries in allInsideRooms array. */
    int numInsideRooms;
};



#endif /* Portcullis_hpp */
