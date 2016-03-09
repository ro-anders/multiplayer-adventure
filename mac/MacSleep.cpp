//
//  MacSleep.cpp
//  MacAdventure
//
//  Created by Robert Antonucci on 3/8/16.
//
//

#include "MacSleep.hpp"

#include <unistd.h>

MacSleep::MacSleep() {}

MacSleep::~MacSleep() {}

void MacSleep::sleep(int seconds) {
    ::sleep(seconds);
}