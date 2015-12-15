
#ifndef Ball_hpp
#define Ball_hpp

#include <stdio.h>
#include "adventure_sys.h"

class BALL
{
public:
    int playerNum;              // 0-2.  Which player this is.
    int room;                   // room
    int x;                      // x position
    int y;                      // y position
    int previousX;              // previous x position
    int previousY;              // previous y position
    int velx;					// Current horizontal speed (walls notwithstanding).  Positive = right.  Negative = left.
    int vely;					// Current vertical speed (walls notwithstanding).  Positive = right.  Negative = down.
    int linkedObject;           // index of linked (carried) object
    int linkedObjectX;          // X value representing the offset from the ball to the object being carried
    int linkedObjectY;          // Y value representing the offset from the ball to the object being carried
    bool hitX;                  // the ball hit something on the X axis
    bool hitY;                  // the ball hit something on the Y axis
    int hitObject;              // the object that the ball hit
    const byte* gfxData;		// graphics data for ball
   
    BALL(int numPlayer);
    
    ~BALL();
};

#endif /* Ball_hpp */
