
#ifndef ActionQueue_hpp
#define ActionQueue_hpp

#include <stdio.h>

class RemoteAction;

/**
 * A simple Queue for holding actions waiting to be processed.
 */
class ActionQueue {
public:
    
    ActionQueue();
    
    ~ActionQueue();
    
    void enQ(RemoteAction* action);
    
    RemoteAction* deQ();
    
    int isEmpty();
    
    int numEntries();


private:
    int first;
    int last; // One after the last actually.  Where the next one should go.
    
    RemoteAction** array;
    
    int arrayLength;
    
    void resizeQ(int newSize);

};

#endif /* ActionQueue_hpp */
