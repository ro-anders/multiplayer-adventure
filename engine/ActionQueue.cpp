

#include <stdlib.h>
#include <string.h>
#include "ActionQueue.hpp"

ActionQueue::ActionQueue() {
    first = 0;
    last = -1;
    arrayLength = 4;
    array = (RemoteAction**)malloc(arrayLength*sizeof(RemoteAction*));
}

ActionQueue::~ActionQueue() {
    delete array;
}

void ActionQueue::resizeQ(int newSize) {
    // newSize better be greater than old size or this won't work.
    if (newSize <= arrayLength) {
        newSize = arrayLength + 1;
    }
    
    RemoteAction** newArray = (RemoteAction**)malloc(newSize*sizeof(RemoteAction*));
    // May need to copy data as two segments
    if (last == -1) {
        // The list is empty.  Don't need to do anything.
        first = 0;
        last = -1;
    } else if (first <= last) {
        // Data does not wrap across the end of the array.  Just copy as one
        // chunk.
        memcpy(newArray, array+first, (last-first+1) * sizeof(RemoteAction*));
        last = last-first;
        first = 0;
    } else {
        // Data wraps across the end of the array.  Copy in two chunks.
        memcpy(newArray, array+first, (arrayLength-first) * sizeof(RemoteAction*));
        memcpy(newArray, array+arrayLength-first, (last+1) * sizeof(RemoteAction*));
        last = arrayLength-first+last+1;
        first = 0;
    }
    delete array;
    array = newArray;
    arrayLength = newSize;
}

void ActionQueue::enQ(RemoteAction* action) {
    
    if ((last >= 0) && ((last+1) % arrayLength == first)) {
        resizeQ(2*arrayLength);
    }
    
    last = last+1 % arrayLength;
    array[last] = action;
}

RemoteAction* ActionQueue::deQ() {
    RemoteAction* result = array[first];
    if (first == last) {
        // The queue is now empty
        first = 0;
        last = -1;
    } else {
        first = (first + 1) % arrayLength;
    }
    return result;
}

int ActionQueue::isEmpty() {
    return (last == -1);
}

int ActionQueue::numEntries() {
    if (last == -1) {
        return 0;
    } else if (last > first) {
        return last - first + 1;
    } else {
        return arrayLength - first + last + 1;
    }
    
}
