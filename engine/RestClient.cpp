
#include "RestClient.hpp"

#include <string.h>

StringMap::StringMap() :
numEntries(0),
spaceAllocated(16) {
    keys = new const char*[spaceAllocated];
    values = new const char*[spaceAllocated];
}

StringMap::~StringMap() {
    for(int ctr=0; ctr<numEntries; ++ctr) {
        delete[] keys[ctr];
        delete[] values[ctr];
    }
    delete[] keys;
    delete[] values;
}

const char* StringMap::get(const char* key) {
    int foundAt = findIndex(key);
    const char* found = (foundAt < 0 ? NULL : values[foundAt]);
    return found;
}

void StringMap::put(const char* key, const char* value) {
    int insertInSlot = findIndex(key);
    if (insertInSlot > 0) {
        delete[] values[insertInSlot];
        values[insertInSlot] = copyString(value);
    } else {
        if (spaceAllocated == numEntries) {
            allocateMoreSpace();
        }
        keys[numEntries] = copyString(key);
        values[numEntries] = copyString(value);
    }
}

void StringMap::remove(const char* key) {
    int insertInSlot = findIndex(key);
    if (insertInSlot > 0) {
        delete[] keys[insertInSlot];
        delete[] values[insertInSlot];
        // Shift everything down
        memcpy(keys+insertInSlot, keys+insertInSlot+1, numEntries-insertInSlot-1);
    }
}

int StringMap::findIndex(const char* key) {
    int found = -1;
    for(int ctr=0; (ctr<numEntries) && (found < 0); ++ctr) {
        if (strcmp(key, keys[ctr])==0) {
            found = ctr;
        }
    }
    return found;
}
