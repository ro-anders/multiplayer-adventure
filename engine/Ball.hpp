
#ifndef Ball_hpp
#define Ball_hpp

#include <stdio.h>
#include "adventure_sys.h"

class Portcullis;

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
    Portcullis* homeGate;       // The gate of the castle you start at
   
    BALL(int numPlayer, Portcullis* homeGate);
    
    ~BALL();
    
    /**
     * The distance from ball to a point.  Takes into account Ball is on a 2x resolution than everything else.
     */
    int distanceTo(int x, int y);
    
    bool isGlowing();
    
    void setGlowing(bool nowIsGlowing);
    
private:
    /** During the gauntlet, once you reach the black castle you flash like the chalise until you reset or you reach the
     * gold castle where you win. */
    bool glowing;

};

#endif /* Ball_hpp */
