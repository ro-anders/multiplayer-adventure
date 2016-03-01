

#ifndef Portcullis_hpp
#define Portcullis_hpp

#include <stdio.h>

#include "GameObject.hpp"

class Portcullis: public OBJECT {
public:
    
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
    Portcullis(const char* label, int outsideRoom, int insideRoom, OBJECT* key);
    
    virtual ~Portcullis();
    
    /**
     * If there are multiple rooms inside this castle, this will add these rooms to the portcullis's list of inside rooms.
     * This list is used for things like randomizing objects for game 3 and making sure all objects can still be reached.
     * The 'insideRoom' passed into the constructor is already added to this list.
     * If a second argument is given, all rooms between the indices are added.
     */
    void addRoom(int firstRoom, int lastRoom=-1);
    
    void setState(int newState, bool isActive);
    
    void updateState();
    
    void keyTouch();
    
    void openFromInside();
    
    /**
     * Called when a player enters a gate that is not completely open.
     */
    void forceOpen();
    
    /**
     * Returns whether the room passed in is somewhere behind this gate.
     */
    bool isRoomInCastle(int room);
    
private:
    
    /** Array of rooms inside this castle.  If NULL then it means only the insideRoom is inside this castle.  If not NULL,
     * the inside room will be included in the list.
     */
    int* allInsideRooms;
    
    /** Number entries in allInsideRooms array. */
    int numInsideRooms;
};



#endif /* Portcullis_hpp */
